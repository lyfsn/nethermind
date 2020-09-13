//  Copyright (c) 2018 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System;
using Nethermind.Core.Crypto;

namespace Nethermind.Trie.Pruning
{
    public class NullTrieStore : ITrieStore
    {
        private NullTrieStore()
        {
        }

        public static NullTrieStore Instance { get; } = new NullTrieStore();

        public void CommitOneNode(long blockNumber, NodeCommitInfo nodeCommitInfo)
        {
        }

        public void FinishBlockCommit(TrieType trieType, long blockNumber, TrieNode? root)
        {
        }

        public TrieNode? FindCachedOrNull(Keccak hash)
        {
            return null;
        }

        public TrieNode FindCachedOrUnknown(Keccak hash)
        {
            return new TrieNode(NodeType.Unknown, hash);
        }

        public byte[] LoadRlp(Keccak hash, bool allowCaching)
        {
            return Array.Empty<byte>();
        }

        public void UndoOneBlock()
        {
        }

        public event EventHandler<BlockNumberEventArgs> SnapshotTaken;
    }
}
