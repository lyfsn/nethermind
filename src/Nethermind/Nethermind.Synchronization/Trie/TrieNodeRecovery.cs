// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nethermind.Blockchain;
using Nethermind.Blockchain.Synchronization;
using Nethermind.Logging;
using Nethermind.Synchronization.FastSync;
using Nethermind.Synchronization.Peers;

namespace Nethermind.Synchronization.Trie;

public abstract class TrieNodeRecovery<TRequest>
{
    private readonly ISyncPeerPool _syncPeerPool;
    private readonly IBlockTree _blockTree;
    protected readonly ILogger _logger;
    private const int MaxPeersForRecovery = 30;

    protected TrieNodeRecovery(ISyncPeerPool syncPeerPool, IBlockTree blockTree, ILogManager? logManager)
    {
        _syncPeerPool = syncPeerPool;
        _blockTree = blockTree;
        _logger = logManager?.GetClassLogger<TrieNodeRecovery<TRequest>>() ?? NullLogger.Instance;
    }

    public async Task<byte[]?> Recover(TRequest request)
    {
        if (_logger.IsWarn) _logger.Warn($"Missing trie node, trying to recover from network");
        byte[]? checkKeyRecoveriesResults = await RecoverCore(request);
        if (checkKeyRecoveriesResults is not null) return checkKeyRecoveriesResults;

        // One more try
        await Task.Delay(Timeouts.Eth);
        return await RecoverCore(request);
    }

    private async Task<byte[]?> RecoverCore(TRequest request)
    {
        using CancellationTokenSource cts = new(Timeouts.Eth);
        List<Recovery> keyRecoveries = GenerateKeyRecoveries(request, cts);
        byte[]? checkKeyRecoveriesResults = await CheckKeyRecoveriesResults(keyRecoveries, cts);
        return checkKeyRecoveriesResults;
    }

    protected async Task<byte[]?> CheckKeyRecoveriesResults(List<Recovery> keyRecoveries, CancellationTokenSource cts)
    {
        while (keyRecoveries.Count > 0)
        {
            Task<byte[]> task = await Task.WhenAny(keyRecoveries.Select(kr => kr.Task!));
            byte[]? result = await task;
            int index = keyRecoveries.FindIndex(r => r.Task == task);
            if (result is null)
            {
                _logger.Warn($"Got empty response from peer {keyRecoveries[index].Peer}");
                keyRecoveries.RemoveAt(index);
            }
            else
            {
                _logger.Warn($"Successfully recovered from peer {keyRecoveries[index].Peer} with {result.Length} bytes!");
                cts.Cancel();
                return result;
            }
        }

        return null;
    }

    protected List<Recovery> GenerateKeyRecoveries(TRequest requestedHashes, CancellationTokenSource cts)
    {
        List<Recovery> keyRecoveries = AllocatePeers();
        if (_logger.IsWarn) _logger.Warn($"Allocated {keyRecoveries.Count} peers (out of {_syncPeerPool!.InitializedPeers.Count()} initialized peers)");
        foreach (Recovery keyRecovery in keyRecoveries)
        {
            keyRecovery.Task = RecoverRlpFromPeer(keyRecovery.Peer, requestedHashes, cts);
        }

        return keyRecoveries;
    }

    private List<Recovery> AllocatePeers()
    {
        List<Recovery> syncPeerAllocations = new(MaxPeersForRecovery);

        foreach (ISyncPeer peer in _syncPeerPool!.InitializedPeers.Select(p => p.SyncPeer))
        {
            bool canAllocatePeer = CanAllocatePeer(peer);
            if (canAllocatePeer)
            {
                syncPeerAllocations.Add(new Recovery { Peer = peer });
            }
            else
            {
                _logger.Warn($"Peer {peer} can not be allocated with eth{peer.ProtocolVersion}");
            }

            if (syncPeerAllocations.Count >= MaxPeersForRecovery)
            {
                break;
            }
        }

        return syncPeerAllocations;
    }

    protected virtual bool CanAllocatePeer(ISyncPeer peer) => peer.HeadNumber >= (_blockTree.Head?.Number ?? 0);

    private async Task<byte[]?> RecoverRlpFromPeer(ISyncPeer peer, TRequest request, CancellationTokenSource cts)
    {
        try
        {
            return await RecoverRlpFromPeerBase(peer, request, cts);
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsWarn) _logger.Warn($"Cancelled recovering RLP from peer {peer}");
        }
        catch (Exception e)
        {
            if (_logger.IsError) _logger.Error($"Could not recover from {peer}", e);
        }

        return null;
    }

    protected abstract Task<byte[]?> RecoverRlpFromPeerBase(ISyncPeer peer, TRequest request, CancellationTokenSource cts);

    protected class Recovery
    {
        public ISyncPeer Peer { get; init; } = null!;
        public Task<byte[]?>? Task { get; set; }
    }
}
