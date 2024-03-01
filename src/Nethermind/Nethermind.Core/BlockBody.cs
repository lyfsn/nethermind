// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;

namespace Nethermind.Core
{
    public class BlockBody
    {
        public BlockBody(Transaction[]? transactions, Transaction[]? inclusionList, BlockHeader[]? uncles, Withdrawal[]? withdrawals = null)
        {
            Transactions = transactions ?? Array.Empty<Transaction>();
            Uncles = uncles ?? Array.Empty<BlockHeader>();
            Withdrawals = withdrawals;
            InclusionList = inclusionList ?? Array.Empty<Transaction>();
        }

        public BlockBody() : this(null, null, null) { }

        public BlockBody WithChangedTransactions(Transaction[] transactions) => new(transactions, null, Uncles, Withdrawals);

        public BlockBody WithChangedInclusionList(Transaction[] inclusionList) => new(Transactions, inclusionList, Uncles, Withdrawals);

        public BlockBody WithChangedUncles(BlockHeader[] uncles) => new(Transactions, null, uncles, Withdrawals);

        public BlockBody WithChangedWithdrawals(Withdrawal[]? withdrawals) => new(Transactions, null, Uncles, withdrawals);

        public static BlockBody WithOneTransactionOnly(Transaction tx) => new(new[] { tx }, null, null);

        public Transaction[] Transactions { get; internal set; }

        public Transaction[] InclusionList { get; internal set; }

        public BlockHeader[] Uncles { get; }

        public Withdrawal[]? Withdrawals { get; }

        public bool IsEmpty => Transactions.Length == 0 && Uncles.Length == 0 && (Withdrawals?.Length ?? 0) == 0;
    }
}
