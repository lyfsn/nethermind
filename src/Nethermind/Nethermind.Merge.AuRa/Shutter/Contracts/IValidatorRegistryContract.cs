// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Threading.Tasks;
using Nethermind.Core;
using Nethermind.Int256;
using Nethermind.TxPool;

namespace Nethermind.Merge.AuRa.Shutter.Contracts;

public interface IValidatorRegistryContract
{
    /// <summary>
    /// Removes a validator from the validator registry.
    /// </summary>
    /// <param name="blockHeader"></param>
    ValueTask<AcceptTxResult?> Deregister(BlockHeader blockHeader, ulong validatorIndex);

    /// <summary>
    /// Adds a validator to the validator registry.
    /// </summary>
    /// <param name="blockHeader"></param>
    ValueTask<AcceptTxResult?> Register(BlockHeader blockHeader, ulong validatorIndex);

    /// <summary>
    /// Returns the number of previous updates to the registry.
    /// </summary>
    /// <param name="blockHeader"></param>
    UInt256 GetNumUpdates(BlockHeader blockHeader);

    /// <summary>
    /// Retrieves the ith update to the registry.
    /// </summary>
    /// <param name="blockHeader"></param>
    /// <param name="i"></param>
    Update GetUpdate(BlockHeader blockHeader, in UInt256 i);

    struct Update
    {
        public byte[] Message;
        public byte[] Signature;
    }
}
