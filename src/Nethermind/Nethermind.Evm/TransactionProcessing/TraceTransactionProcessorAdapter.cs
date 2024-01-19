// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Evm.Tracing;

namespace Nethermind.Evm.TransactionProcessing
{
    public class TraceTransactionProcessorAdapter : ITransactionProcessorAdapter
    {
        private readonly ITransactionProcessor _transactionProcessor;

        public TraceTransactionProcessorAdapter(ITransactionProcessor transactionProcessor)
        {
            _transactionProcessor = transactionProcessor;
        }

        public IVirtualMachine VirtualMachine => _transactionProcessor.VirtualMachine;

        public void Execute(Transaction transaction, in BlockExecutionContext blkCtx, ITxTracer txTracer) =>
            _transactionProcessor.Trace(transaction, in blkCtx, txTracer);
    }
}
