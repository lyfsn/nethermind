// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Consensus.Messages;
using Nethermind.Consensus.Validators;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Logging;
using Nethermind.Specs;
using Nethermind.Specs.Test;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Nethermind.Blockchain.Test.Validators
{
    [TestFixture]
    public class BlockValidatorTests
    {
        [Test, Timeout(Timeout.MaxTestTime)]
        public void When_more_uncles_than_allowed_returns_false()
        {
            TxValidator txValidator = new(TestBlockchainIds.ChainId);
            ReleaseSpec releaseSpec = new();
            releaseSpec.MaximumUncleCount = 0;
            ISpecProvider specProvider = new CustomSpecProvider(((ForkActivation)0, releaseSpec));

            BlockValidator blockValidator = new(txValidator, Always.Valid, Always.Valid, specProvider, LimboLogs.Instance);
            bool noiseRemoved = blockValidator.ValidateSuggestedBlock(Build.A.Block.TestObject);
            Assert.True(noiseRemoved);

            bool result = blockValidator.ValidateSuggestedBlock(Build.A.Block.WithUncles(Build.A.BlockHeader.TestObject).TestObject);
            Assert.False(result);
        }

        [Test]
        public void ValidateBodyAgainstHeader_BlockIsValid_ReturnsTrue()
        {
            Block block = Build.A.Block
                .WithTransactions(1, Substitute.For<IReleaseSpec>())
                .WithWithdrawals(1)
                .TestObject;

            Assert.That(
                BlockValidator.ValidateBodyAgainstHeader(block.Header, block.Body),
                Is.True);
        }

        [Test]
        public void ValidateBodyAgainstHeader_BlockHasInvalidTxRoot_ReturnsFalse()
        {
            Block block = Build.A.Block
                .WithTransactions(1, Substitute.For<IReleaseSpec>())
                .WithWithdrawals(1)
                .TestObject;
            block.Header.TxRoot = Keccak.OfAnEmptyString;

            Assert.That(
                BlockValidator.ValidateBodyAgainstHeader(block.Header, block.Body),
                Is.False);
        }


        [Test]
        public void ValidateBodyAgainstHeader_BlockHasInvalidUnclesRoot_ReturnsFalse()
        {
            Block block = Build.A.Block
                .WithTransactions(1, Substitute.For<IReleaseSpec>())
                .WithWithdrawals(1)
                .TestObject;
            block.Header.UnclesHash = Keccak.OfAnEmptyString;

            Assert.That(
                BlockValidator.ValidateBodyAgainstHeader(block.Header, block.Body),
                Is.False);
        }

        [Test]
        public void ValidateBodyAgainstHeader_BlockHasInvalidWithdrawalsRoot_ReturnsFalse()
        {
            Block block = Build.A.Block
                .WithTransactions(1, Substitute.For<IReleaseSpec>())
                .WithWithdrawals(1)
                .TestObject;
            block.Header.WithdrawalsRoot = Keccak.OfAnEmptyString;

            Assert.That(
                BlockValidator.ValidateBodyAgainstHeader(block.Header, block.Body),
                Is.False);
        }

        [Test]
        public void ValidateProcessedBlock_HashesAreTheSame_ReturnsTrue()
        {
            TxValidator txValidator = new(TestBlockchainIds.ChainId);
            ISpecProvider specProvider = Substitute.For<ISpecProvider>();
            BlockValidator sut = new(txValidator, Always.Valid, Always.Valid, specProvider, LimboLogs.Instance);
            Block suggestedBlock = Build.A.Block.TestObject;
            Block processedBlock = Build.A.Block.TestObject;

            Assert.That(sut.ValidateProcessedBlock(
                suggestedBlock,
                Array.Empty<TxReceipt>(),
                processedBlock), Is.True);
        }

        [Test]
        public void ValidateProcessedBlock_HashesAreTheSame_ErrorIsNull()
        {
            TxValidator txValidator = new(TestBlockchainIds.ChainId);
            ISpecProvider specProvider = Substitute.For<ISpecProvider>();
            BlockValidator sut = new(txValidator, Always.Valid, Always.Valid, specProvider, LimboLogs.Instance);
            Block suggestedBlock = Build.A.Block.TestObject;
            Block processedBlock = Build.A.Block.TestObject;
            string? error;

            sut.ValidateProcessedBlock(
                suggestedBlock,
                Array.Empty<TxReceipt>(),
                processedBlock, out error);

            Assert.That(error, Is.Null);
        }

        [Test]
        public void ValidateProcessedBlock_HashesAreNotTheSame_ReturnsFalse()
        {
            TxValidator txValidator = new(TestBlockchainIds.ChainId);
            ISpecProvider specProvider = Substitute.For<ISpecProvider>();
            BlockValidator sut = new(txValidator, Always.Valid, Always.Valid, specProvider, LimboLogs.Instance);
            Block suggestedBlock = Build.A.Block.TestObject;
            Block processedBlock = Build.A.Block.WithStateRoot(Keccak.Zero).TestObject;
            
            sut.ValidateProcessedBlock(
                suggestedBlock,
                Array.Empty<TxReceipt>(),
                processedBlock);

            Assert.That(sut.ValidateProcessedBlock(
                suggestedBlock,
                Array.Empty<TxReceipt>(),
                processedBlock), Is.False);
        }

        [Test]
        public void ValidateProcessedBlock_HashesAreNotTheSame_ErrorIsSet()
        {
            TxValidator txValidator = new(TestBlockchainIds.ChainId);
            ISpecProvider specProvider = Substitute.For<ISpecProvider>();
            BlockValidator sut = new(txValidator, Always.Valid, Always.Valid, specProvider, LimboLogs.Instance);
            Block suggestedBlock = Build.A.Block.TestObject;
            Block processedBlock = Build.A.Block.WithStateRoot(Keccak.Zero).TestObject;
            string? error;

            sut.ValidateProcessedBlock(
                suggestedBlock,
                Array.Empty<TxReceipt>(),
                processedBlock, out error);

            Assert.That(error, Is.Not.Empty);
        }
    }
}
