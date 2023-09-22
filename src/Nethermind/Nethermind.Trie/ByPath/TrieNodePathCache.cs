// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nethermind.Core;
using Nethermind.Core.Collections;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Db;
using Nethermind.Logging;
using Nethermind.Trie.Pruning;

namespace Nethermind.Trie.ByPath;
public class TrieNodePathCache : IPathTrieNodeCache
{
    class NodeVersions : IDisposable
    {
        private readonly SortedSet<NodeAtBlock> _nodes;
        private readonly ReaderWriterLockSlim _lock = new();

        public NodeVersions()
        {
            _nodes = new SortedSet<NodeAtBlock>(new NodeAtBlockComparer());
        }

        public void Add(long blockNumber, TrieNode node)
        {
            try
            {
                _lock.EnterWriteLock();

                NodeAtBlock nad = new(blockNumber, node);
                _nodes.Remove(nad);
                _nodes.Add(nad);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public TrieNode? GetLatestUntil(long blockNumber, bool remove = false)
        {
            try
            {
                _lock.EnterUpgradeableReadLock();

                if (blockNumber < _nodes.Min.BlockNumber)
                    return null;

                SortedSet<NodeAtBlock> viewUntilBlock = _nodes.GetViewBetween(_nodes.Min, new NodeAtBlock(blockNumber));
                NodeAtBlock atLatest = viewUntilBlock?.Max;

                if (remove && viewUntilBlock is not null)
                {
                    try
                    {
                        _lock.EnterWriteLock();
                        viewUntilBlock.Clear();
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }

                return atLatest?.TrieNode;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public TrieNode? GetLatest()
        {
            try
            {
                _lock.EnterReadLock();
                return _nodes.Max?.TrieNode;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public TrieNode? Get(Keccak keccak, long minBlockNumber = -1)
        {
            try
            {
                _lock.EnterReadLock();

                SortedSet<NodeAtBlock> subSet = _nodes;
                if (minBlockNumber >= 0)
                    subSet = _nodes.GetViewBetween(new NodeAtBlock(minBlockNumber), _nodes.Max);

                foreach (NodeAtBlock nodeVersion in subSet)
                {
                    if (nodeVersion.TrieNode.Keccak == keccak)
                    {
                        Pruning.Metrics.LoadedFromCacheNodesCount++;
                        return nodeVersion.TrieNode;
                    }
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int Count => _nodes.Count;

        public void Dispose()
        {
            _lock.Dispose();
        }
    }

    class NodeAtBlock
    {
        public NodeAtBlock(long blockNo) : this(blockNo, null) { }

        public NodeAtBlock(long blockNo, TrieNode? trieNode)
        {
            BlockNumber = blockNo;
            TrieNode = trieNode;
        }
        public long BlockNumber { get; set; }
        public TrieNode? TrieNode { get; set; }
    }

    class NodeAtBlockComparer : IComparer<NodeAtBlock>
    {
        public int Compare(NodeAtBlock? x, NodeAtBlock? y)
        {
            if (x?.BlockNumber is null || y?.BlockNumber is null)
                throw new ArgumentNullException();
            int result = Comparer<long>.Default.Compare(x.BlockNumber, y.BlockNumber);
            return result;
        }
    }

    private SpanConcurrentDictionary<byte, NodeVersions> _nodesByPath = new(Bytes.SpanNibbleEqualityComparer);
    private ConcurrentDictionary<Keccak, HashSet<long>> _rootHashToBlock = new();
    private readonly ITrieStore _trieStore;
    private readonly SpanConcurrentDictionary<byte, long> _removedPrefixes;
    private int _count;
    private readonly ILogger _logger;

    public int Count { get => _count; }
    public int PrefixLength { get; set; }

    public TrieNodePathCache(ITrieStore trieStore, ILogManager? logManager)
    {
        _trieStore = trieStore;
        _removedPrefixes = new SpanConcurrentDictionary<byte, long>(Bytes.SpanNibbleEqualityComparer);
        _logger = logManager?.GetClassLogger<TrieNodePathCache>() ?? throw new ArgumentNullException(nameof(logManager));
        PrefixLength = 66;
    }

    public TrieNode? GetNode(Span<byte> path, Keccak keccak)
    {
        if (_nodesByPath.TryGetValue(path, out NodeVersions nodeVersions))
        {
            long deletedAt = IsDeleted(path);
            return deletedAt >= 0 ? nodeVersions.Get(keccak, deletedAt) : nodeVersions.Get(keccak);
        }
        return null;
    }

    public TrieNode? GetNodeFromRoot(Keccak? rootHash, Span<byte> path)
    {
        if (_nodesByPath.TryGetValue(path, out NodeVersions nodeVersions))
        {
            if (rootHash is null)
            {
                Pruning.Metrics.LoadedFromCacheNodesCount++;
                return nodeVersions.GetLatest();
            }

            if (_rootHashToBlock.TryGetValue(rootHash, out HashSet<long> blocks))
            {
                long blockNo = blocks.Max();

                TrieNode? node = nodeVersions.GetLatestUntil(blockNo);
                if (node is not null)
                    Pruning.Metrics.LoadedFromCacheNodesCount++;

                return node;
            }
        }
        else
        {
            long deletedAt = IsDeleted(path);
            if (deletedAt < 0)
                return null;

            long maxBlockNumber = deletedAt;

            if (rootHash is not null && _rootHashToBlock.TryGetValue(rootHash, out HashSet<long> blocks))
                maxBlockNumber = blocks.Max();

            if (maxBlockNumber >= deletedAt)
                return new TrieNode(NodeType.Leaf, null, true);
        }
        return null;
    }

    public void AddNode(long blockNumber, TrieNode trieNode)
    {
        byte[] path = trieNode.FullPath;

        AddNodeInternal(path, trieNode, blockNumber);
        if (trieNode.IsLeaf && trieNode.PathToNode.Length < 64)
            AddNodeInternal(trieNode.StoreNibblePathPrefix.Concat(trieNode.PathToNode).ToArray(), trieNode, blockNumber);
    }

    private void AddNodeInternal(byte[] pathToNode, TrieNode trieNode, long blockNumber)
    {
        if (_nodesByPath.TryGetValue(pathToNode, out NodeVersions nodeVersions))
        {
            nodeVersions.Add(blockNumber, trieNode);
        }
        else
        {
            nodeVersions = new NodeVersions();
            nodeVersions.Add(blockNumber, trieNode);
            _nodesByPath[pathToNode] = nodeVersions;
        }
        Pruning.Metrics.CachedNodesCount = Interlocked.Increment(ref _count);
        if (_logger.IsTrace) _logger.Trace($"Added node for block {blockNumber} | node version for path {pathToNode.ToHexString()} : {nodeVersions.Count} | Paths cached: {_nodesByPath.Count}");
    }

    public void SetRootHashForBlock(long blockNo, Keccak? rootHash)
    {
        rootHash ??= Keccak.EmptyTreeHash;
        if (_rootHashToBlock.TryGetValue(rootHash, out HashSet<long> blocks))
            blocks.Add(blockNo);
        else
            _rootHashToBlock[rootHash] = new HashSet<long>(new[] { blockNo });
    }

    public void PersistUntilBlock(long blockNumber, IBatch? batch = null)
    {
        List<byte[]> removedPaths = new();
        List<TrieNode> toPersist = new();

        ProcessDestroyed(blockNumber);

        foreach (KeyValuePair<byte[], NodeVersions> nodeVersion in _nodesByPath)
        {
            TrieNode node = nodeVersion.Value.GetLatestUntil(blockNumber, true);
            if (nodeVersion.Value.Count == 0)
                removedPaths.Add(nodeVersion.Key);

            if (node is null)
                continue;

            if (node.FullRlp.IsNull) toPersist.Insert(0, node); else toPersist.Add(node);
        }

        foreach (TrieNode node in toPersist)
        {
            if (node.IsPersisted)
                continue;
            _trieStore.SaveNodeDirectly(blockNumber, node, batch);
            node.IsPersisted = true;
        }

        foreach (byte[] path in removedPaths)
            _nodesByPath.Remove(path, out _);

        List<Keccak> removedRoots = new();
        foreach (KeyValuePair<Keccak, HashSet<long>> kvp in _rootHashToBlock)
        {
            kvp.Value.RemoveWhere(blk => blk <= blockNumber);
            if (kvp.Value.Count == 0)
                removedRoots.Add(kvp.Key);
        }
        foreach (Keccak rootHash in removedRoots)
            _rootHashToBlock.Remove(rootHash, out _);
    }

    private void ProcessDestroyed(long blockNumber)
    {
        List<byte[]> paths = _removedPrefixes.Where(kvp => kvp.Value <= blockNumber).Select(kvp => kvp.Key).ToList();

        foreach (byte[] path in paths)
        {
            (byte[] startKey, byte[] endKey) = TrieStoreByPath.GetDeleteKeyFromNibblePrefix(path);
            _trieStore.DeleteByRange(startKey, endKey);
        }

        foreach (byte[] path in paths)
        {
            _removedPrefixes.Remove(path, out _);
        }
    }

    public void Clear()
    {
        _rootHashToBlock.Clear();
        _nodesByPath.Clear();
    }

    public void AddRemovedPrefix(long blockNumber, ReadOnlySpan<byte> keyPrefix)
    {
        foreach (byte[] path in _nodesByPath.Keys)
        {
            if (path.Length >= keyPrefix.Length && Bytes.AreEqual(path.AsSpan()[0..keyPrefix.Length], keyPrefix))
            {
                TrieNode deletedNode = _nodesByPath[path].GetLatest().CloneNodeForDeletion();
                _nodesByPath[path].Add(blockNumber, deletedNode);
            }
        }

        _removedPrefixes[keyPrefix] = blockNumber;
    }

    public bool IsPathCached(ReadOnlySpan<byte> path)
    {
        return _nodesByPath.ContainsKey(path.ToArray());
    }

    private long IsDeleted(Span<byte> path)
    {
        if (path.Length >= PrefixLength && _removedPrefixes.TryGetValue(path[0..PrefixLength], out long deletedAtBlock))
            return deletedAtBlock;

        return -1;
    }
}
