// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Db;
using Nethermind.Int256;
using Nethermind.Serialization.Rlp;

namespace Nethermind.TxPool;

public class BlobTxStorage : ITxStorage
{
    private readonly IDb _fullBlobTxsDb;
    private readonly IDb _lightBlobTxsDb;
    private static readonly TxDecoder _txDecoder = new();
    private static readonly LightTxDecoder _lightTxDecoder = new();

    public BlobTxStorage()
    {
        _fullBlobTxsDb = new MemDb();
        _lightBlobTxsDb = new MemDb();
    }

    public BlobTxStorage(IColumnsDb<BlobTxsColumns> database)
    {
        _fullBlobTxsDb = database.GetColumnDb(BlobTxsColumns.FullBlobTxs);
        _lightBlobTxsDb = database.GetColumnDb(BlobTxsColumns.LightBlobTxs);
    }

    public bool TryGet(ValueKeccak hash, [NotNullWhen(true)] out Transaction? transaction)
    {
        DecodeTimestamp(_lightBlobTxsDb.Get(hash.Bytes), out UInt256 timestamp);
        Span<byte> txHashPrefixed = stackalloc byte[64];
        GetHashPrefixedByTimestamp(timestamp, hash, txHashPrefixed);

        byte[]? txBytes = _fullBlobTxsDb.Get(txHashPrefixed);
        return TryDecodeFullTx(txBytes, out transaction);
    }

    public IEnumerable<LightTransaction> GetAll()
    {
        foreach (byte[] txBytes in _lightBlobTxsDb.GetAllValues())
        {
            if (TryDecodeLightTx(txBytes, out LightTransaction? transaction))
            {
                yield return transaction!;
            }
        }
    }

    public void Add(Transaction transaction)
    {
        if (transaction?.Hash is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        Span<byte> txHashPrefixed = stackalloc byte[64];
        GetHashPrefixedByTimestamp(transaction.Timestamp, transaction.Hash, txHashPrefixed);

        int length = _txDecoder.GetLength(transaction, RlpBehaviors.InMempoolForm);
        RlpStream rlpStream = new(length);
        rlpStream.Encode(transaction, RlpBehaviors.InMempoolForm);

        _fullBlobTxsDb.Set(txHashPrefixed, rlpStream.Data!);
        _lightBlobTxsDb.Set(transaction.Hash, _lightTxDecoder.Encode(transaction));
    }

    public void Delete(ValueKeccak hash)
    {
        DecodeTimestamp(_lightBlobTxsDb.Get(hash.Bytes), out UInt256 timestamp);

        Span<byte> txHashPrefixed = stackalloc byte[64];
        GetHashPrefixedByTimestamp(timestamp, hash, txHashPrefixed);

        _fullBlobTxsDb.Remove(txHashPrefixed);
        _lightBlobTxsDb.Remove(hash.BytesAsSpan);
    }

    private static bool TryDecodeFullTx(byte[]? txBytes, out Transaction? transaction)
    {
        if (txBytes is not null)
        {
            RlpStream rlpStream = new(txBytes);
            transaction = Rlp.Decode<Transaction>(rlpStream, RlpBehaviors.InMempoolForm);
            return true;
        }

        transaction = default;
        return false;
    }

    private static bool TryDecodeLightTx(byte[]? txBytes, out LightTransaction? lightTx)
    {
        if (txBytes is not null)
        {
            lightTx = _lightTxDecoder.Decode(txBytes);
            return true;
        }

        lightTx = default;
        return false;
    }

    private static void DecodeTimestamp(byte[]? txBytes, out UInt256 timestamp)
    {
        timestamp = txBytes is not null ? _lightTxDecoder.DecodeTimestamp(txBytes) : default;
    }

    private void GetHashPrefixedByTimestamp(UInt256 timestamp, ValueKeccak hash, Span<byte> txHashPrefixed)
    {
        timestamp.WriteBigEndian(txHashPrefixed);
        hash.Bytes.CopyTo(txHashPrefixed[32..]);
    }
}

internal static class UInt256Extensions
{
    public static void WriteBigEndian(in this UInt256 value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(0, 8), value.u3);
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(8, 8), value.u2);
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(16, 8), value.u1);
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(24, 8), value.u0);
    }
}
