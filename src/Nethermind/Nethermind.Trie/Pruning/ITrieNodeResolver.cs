// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Crypto;

namespace Nethermind.Trie.Pruning
{
    public interface ITrieNodeResolver
    {
        /// <summary>
        /// Returns a cached and resolved <see cref="TrieNode"/> or a <see cref="TrieNode"/> with Unknown type
        /// but the hash set. The latter case allows to resolve the node later. Resolving the node means loading
        /// its RLP data from the state database.
        /// </summary>
        /// <param name="hash">Keccak hash of the RLP of the node.</param>
        /// <returns></returns>
        TrieNode FindCachedOrUnknown(Hash256 hash);

        /// <summary>
        /// Loads RLP of the node.
        /// </summary>
        byte[]? LoadRlp(Hash256 hash, ReadFlags flags = ReadFlags.None);

        /// <summary>
        /// Loads RLP of the node.
        /// </summary>
        byte[]? TryLoadRlp(Hash256 hash, ReadFlags flags = ReadFlags.None);

        /// <summary>
        /// Loads RLP of the node.
        /// </summary>
        byte[]? GetByHash(ReadOnlySpan<byte> key, ReadFlags flags = ReadFlags.None);
    }
}
