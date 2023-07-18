// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Blockchain;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Logging;
using Nethermind.State;
using Nethermind.Synchronization.Peers;
using Nethermind.Trie.Pruning;

namespace Nethermind.Synchronization.Trie;

public class HealingStorageTreeFactory : IStorageTreeFactory
{
    private ISyncPeerPool? _syncPeerPool;
    private IBlockTree? _blockTree;
    public bool Throw { get; set; }

    public void InitializeNetwork(ISyncPeerPool syncPeerPool, IBlockTree blockTree)
    {
        _syncPeerPool = syncPeerPool;
        _blockTree = blockTree;
    }

    public StorageTree Create(Address address, ITrieStore trieStore, Keccak storageRoot, Keccak stateRoot, ILogManager? logManager)
    {
        HealingStorageTree healingStorageTree = new HealingStorageTree(_blockTree!, trieStore, storageRoot, logManager, address, stateRoot, _syncPeerPool) { Throw = Throw };
        Throw = false;
        return healingStorageTree;
    }
}
