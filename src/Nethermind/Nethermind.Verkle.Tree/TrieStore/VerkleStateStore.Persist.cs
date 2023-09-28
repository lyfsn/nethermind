// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core.Extensions;
using Nethermind.Core.Verkle;
using Nethermind.Trie.Pruning;
using Nethermind.Verkle.Tree.TrieNodes;
using Nethermind.Verkle.Tree.Utils;
using Nethermind.Verkle.Tree.VerkleDb;

namespace Nethermind.Verkle.Tree.TrieStore;

public partial class VerkleStateStore
{
    private bool _lastPersistedReachedReorgBoundary;
    private long _latestPersistedBlockNumber;
    public long LastPersistedBlockNumber
    {
        get => _latestPersistedBlockNumber;
        private set
        {
            if (value != _latestPersistedBlockNumber)
            {
                _latestPersistedBlockNumber = value;
                _lastPersistedReachedReorgBoundary = false;
            }
        }
    }
    private long LatestCommittedBlockNumber { get; set; }

    private VerkleCommitment? PersistedStateRoot { get;  set; }

    // This method is called at the end of each block to flush the batch changes to the storage and generate forward and reverse diffs.
    // this should be called only once per block, right now it does not support multiple calls for the same block number.
    // if called multiple times, the full state would be fine - but it would corrupt the diffs and historical state will be lost
    // TODO: add capability to update the diffs instead of overwriting if Flush(long blockNumber)
    //   is called multiple times for the same block number, but do we even need this?
    public void InsertBatch(long blockNumber, VerkleMemoryDb batch)
    {
        if (_logger.IsDebug)
            _logger.Debug(
                $"VSS: Flushing:{blockNumber} InternalDb:{batch.InternalTable.Count} LeafDb:{batch.LeafTable.Count}");

        if (blockNumber == 0)
        {
            if (_logger.IsDebug)
                _logger.Debug($"VSS: Special case for block 0, Persisting");
            PersistBlockChanges(batch.InternalTable, batch.LeafTable, Storage);
            UpdateStateRoot();
            PersistedStateRoot = StateRoot;
            LatestCommittedBlockNumber = LastPersistedBlockNumber = 0;
            StateRootToBlocks[StateRoot] = blockNumber;
        }
        else
        {
            if (blockNumber <= LatestCommittedBlockNumber)
                throw new InvalidOperationException("Cannot flush for same block number `multiple times");

            // create a sorted set for leaves - for snap sync
            // TODO: create this sorted set while inserting into the batch - will help reducing allocations
            ReadOnlyVerkleMemoryDb cacheBatch = new()
            {
                InternalTable = batch.InternalTable,
                LeafTable = new SortedDictionary<byte[], byte[]?>(batch.LeafTable, Bytes.Comparer)
            };

            bool shouldPersistBlock;
            ReadOnlyVerkleMemoryDb changesToPersist;
            long blockNumberToPersist;
            if (BlockCache is null)
            {
                shouldPersistBlock = true;
                changesToPersist = cacheBatch;
                blockNumberToPersist = blockNumber;
            }
            else
            {
                shouldPersistBlock = !BlockCache.EnqueueAndReplaceIfFull((blockNumber, cacheBatch),
                    out (long, ReadOnlyVerkleMemoryDb) element);
                changesToPersist = element.Item2;
                blockNumberToPersist = element.Item1;
            }

            if (shouldPersistBlock)
            {
                if (_logger.IsDebug)
                    _logger.Debug($"VSS: BlockCache is full - got forwardDiff BlockNumber:{blockNumberToPersist} IN:{changesToPersist.InternalTable.Count} LN:{changesToPersist.LeafTable.Count}");
                VerkleCommitment root = GetStateRoot(changesToPersist.InternalTable) ?? (new VerkleCommitment(Storage.GetInternalNode(RootNodeKey)?.Bytes ?? throw new ArgumentException()));
                if (_logger.IsDebug) _logger.Debug($"VSS: StateRoot after persisting forwardDiff: {root}");

                // TODO: add a flag to check if we even need history here and then get reverseDiff accordingly
                PersistBlockChanges(changesToPersist.InternalTable, changesToPersist.LeafTable, Storage, out VerkleMemoryDb reverseDiff);
                InsertBatchCompleted?.Invoke(this, new InsertBatchCompleted(blockNumberToPersist, changesToPersist, reverseDiff));

                PersistedStateRoot = root;
                LastPersistedBlockNumber = blockNumberToPersist;
            }
            UpdateStateRoot();
            StateRootToBlocks[StateRoot] = LatestCommittedBlockNumber = blockNumber;
            if (_logger.IsDebug)
                _logger.Debug(
                $"VSS: Completed Flush: PersistedStateRoot:{PersistedStateRoot} LastPersistedBlockNumber:{LastPersistedBlockNumber} LatestCommittedBlockNumber:{LatestCommittedBlockNumber} StateRoot:{StateRoot} blockNumber:{blockNumber}");
        }
        AnnounceReorgBoundaries();
    }

    private static void PersistBlockChanges(IDictionary<byte[], InternalNode?> internalStore, IDictionary<byte[], byte[]?> leafStore, VerkleKeyValueDb storage, out VerkleMemoryDb reverseDiff)
    {
        // we should not have any null values in the Batch db - because deletion of values from verkle tree is not allowed
        // nullable values are allowed in MemoryStateDb only for reverse diffs.
        reverseDiff = new();

        foreach (KeyValuePair<byte[], byte[]?> entry in leafStore)
        {
            // in stateless tree - anything can be null
            // Debug.Assert(entry.Value is not null, "nullable value only for reverse diff");
            if (storage.GetLeaf(entry.Key, out byte[]? node)) reverseDiff.LeafTable[entry.Key] = node;
            else reverseDiff.LeafTable[entry.Key] = null;

            storage.SetLeaf(entry.Key, entry.Value);
        }

        foreach (KeyValuePair<byte[], InternalNode?> entry in internalStore)
        {
            // in stateless tree - anything can be null
            // Debug.Assert(entry.Value is not null, "nullable value only for reverse diff");
            if (storage.GetInternalNode(entry.Key, out InternalNode? node)) reverseDiff.InternalTable[entry.Key] = node;
            else reverseDiff.InternalTable[entry.Key] = null;

            storage.SetInternalNode(entry.Key, entry.Value);
        }
        storage.LeafDb.Flush();
        storage.InternalNodeDb.Flush();
    }

    private static void PersistBlockChanges(IDictionary<byte[], InternalNode?> internalStore, IDictionary<byte[], byte[]?> leafStore, VerkleKeyValueDb storage)
    {
        foreach (KeyValuePair<byte[], byte[]?> entry in leafStore)
            storage.SetLeaf(entry.Key, entry.Value);

        foreach (KeyValuePair<byte[], InternalNode?> entry in internalStore)
            storage.SetInternalNode(entry.Key, entry.Value);

        storage.LeafDb.Flush();
        storage.InternalNodeDb.Flush();
    }

    private int _isFirst;
    private void AnnounceReorgBoundaries()
    {
        if (LatestCommittedBlockNumber < 1)
        {
            return;
        }

        bool shouldAnnounceReorgBoundary = false;
        bool isFirstCommit = Interlocked.Exchange(ref _isFirst, 1) == 0;
        if (isFirstCommit)
        {
            if (_logger.IsDebug) _logger.Debug($"Reached first commit - newest {LatestCommittedBlockNumber}, last persisted {LastPersistedBlockNumber}");
            // this is important when transitioning from fast sync
            // imagine that we transition at block 1200000
            // and then we close the app at 1200010
            // in such case we would try to continue at Head - 1200010
            // because head is loaded if there is no persistence checkpoint
            // so we need to force the persistence checkpoint
            long baseBlock = Math.Max(0, LatestCommittedBlockNumber - 1);
            LastPersistedBlockNumber = baseBlock;
            shouldAnnounceReorgBoundary = true;
        }
        else if (!_lastPersistedReachedReorgBoundary)
        {
            // even after we persist a block we do not really remember it as a safe checkpoint
            // until max reorgs blocks after
            if (LatestCommittedBlockNumber >= LastPersistedBlockNumber + MaxNumberOfBlocksInCache)
            {
                shouldAnnounceReorgBoundary = true;
            }
        }

        if (shouldAnnounceReorgBoundary)
        {
            ReorgBoundaryReached?.Invoke(this, new ReorgBoundaryReached(LastPersistedBlockNumber));
            _lastPersistedReachedReorgBoundary = true;
        }
    }
}