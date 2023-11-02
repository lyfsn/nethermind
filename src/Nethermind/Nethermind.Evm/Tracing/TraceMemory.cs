// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using Nethermind.Core.Extensions;

namespace Nethermind.Evm.Tracing;

public readonly struct TraceMemory
{
    public ulong Size { get; }
    private readonly ReadOnlyMemory<byte> _memory;

    public TraceMemory(ulong size, ReadOnlyMemory<byte> memory)
    {
        Size = size;
        _memory = memory;
    }

    public List<string> ToHexWordList()
    {
        List<string> memory = new(((int)Size / EvmPooledMemory.WordSize) + ((Size % EvmPooledMemory.WordSize == 0) ? 0 : 1));
        int traceLocation = 0;

        while ((ulong)traceLocation < Size)
        {
            int sizeAvailable = Math.Min(EvmPooledMemory.WordSize, _memory.Length - traceLocation);
            if (sizeAvailable > 0)
            {
                ReadOnlySpan<byte> bytes = _memory.Slice(traceLocation, sizeAvailable).Span;
                memory.Add(bytes.ToHexString());
            }
            else // Memory might not be initialized
            {
                memory.Add(Bytes.Zero32.ToHexString());
            }

            traceLocation += EvmPooledMemory.WordSize;
        }

        return memory;
    }

    public ReadOnlySpan<byte> Slice(int start, int length)
    {
        if ((ulong)start + (ulong)length > Size)
        {
            throw new IndexOutOfRangeException("Requested memory range is out of bounds.");
        }

        ReadOnlySpan<byte> span = _memory.Span;

        if (start + length > _memory.Length)
        {
            byte[] result = new byte[length];
            for (int i = 0, index = start; index < _memory.Length; i++, index++)
            {
                result[i] = span[index];
            }

            return result;
        }

        return span.Slice(start, length);
    }
}