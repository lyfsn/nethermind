// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Nethermind.Core;
using Nethermind.Core.Crypto;

namespace Nethermind.TxPool;

public interface ITxStorage
{
    bool TryGet(ValueKeccak hash, [NotNullWhen(true)] out Transaction? transaction);
    IEnumerable<LightTransaction> GetAll();
    void Add(Transaction transaction);
    void Delete(ValueKeccak hash);
}