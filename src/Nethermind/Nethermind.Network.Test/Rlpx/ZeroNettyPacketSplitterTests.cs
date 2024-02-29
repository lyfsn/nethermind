// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Test.Builders;
using Nethermind.Network.P2P.Subprotocols.Eth.V62.Messages;
using Nethermind.Network.Rlpx;
using Nethermind.Network.Test.Rlpx.TestWrappers;
using NUnit.Framework;

namespace Nethermind.Network.Test.Rlpx
{
    [TestFixture]
    public class ZeroNettyPacketSplitterTests
    {
        private IByteBuffer _input;
        private IByteBuffer _output;

        [SetUp]
        public void Setup()
        {
            _input = ReferenceCountUtil.ReleaseLater(PooledByteBufferAllocator.Default.Buffer(16 * 1024));
            _output = ReferenceCountUtil.ReleaseLater(PooledByteBufferAllocator.Default.Buffer(16 * 1024));
        }

        [TearDown]
        public void TearDown()
        {
            _input.Release();
            _output.Release();
        }

        [TestCase(1, "000002c280800000000000000000000002000000000000000000000000000000")]
        [TestCase(2, "000400c580018204020000000000000002000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002c280010000000000000000000000000000000000000000000000000000")]
        [TestCase(3, "000400c580018208020000000000000002000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400c280010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002c280010000000000000000000000000000000000000000000000000000")]
        public void Splits_packet_into_frames(int framesCount, string outputHex)
        {
            Packet packet = new("eth", 2, new byte[(framesCount - 1) * Frame.DefaultMaxFrameSize + 1]);
            _input.WriteByte(packet.PacketType);
            _input.WriteBytes(packet.Data);
            ZeroPacketSplitterTestWrapper packetSplitter = new();
            _output = packetSplitter.Encode(_input);

            byte[] outputBytes = new byte[_output.ReadableBytes];
            _output.ReadBytes(outputBytes);

            Assert.That(outputBytes.ToHexString(false), Is.EqualTo(outputHex));
        }

        [Test]
        public void Single_frame_is_handled_properly()
        {
            Packet packet = new("eth", 2, new byte[Frame.DefaultMaxFrameSize / 2]);
            _input.WriteByte(packet.PacketType);
            _input.WriteBytes(packet.Data);

            ZeroPacketSplitterTestWrapper packetSplitter = new();
            _output = packetSplitter.Encode(_input);

            byte[] outputBytes = new byte[_output.ReadableBytes];
            _output.ReadBytes(outputBytes);

            string outputHex = outputBytes.ToHexString(false);
            Assert.That(outputHex, Is.EqualTo("000201c2808000000000000000000000020000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"));
        }

        [Test]
        public void Block_is_handled()
        {
            Transaction a = Build.A.Transaction.TestObject;
            Transaction b = Build.A.Transaction.TestObject;
            Block block = Build.A.Block.WithTransactions(a, b).TestObject;
            using NewBlockMessage newBlockMessage = new();
            newBlockMessage.Block = block;

            NewBlockMessageSerializer newBlockMessageSerializer = new();
            Packet packet = new("eth", 7, newBlockMessageSerializer.Serialize(newBlockMessage));

            _input.WriteByte(packet.PacketType);
            _input.WriteBytes(packet.Data);
            ZeroPacketSplitterTestWrapper packetSplitter = new();
            _output = packetSplitter.Encode(_input);

            byte[] outputBytes = new byte[_output.ReadableBytes];
            _output.ReadBytes(outputBytes);

            string outputHex = outputBytes.ToHexString(false);
            Assert.That(outputHex, Is.EqualTo("000247c280800000000000000000000007f90243f9023ff901f9a0ff483e972a04a9a62bb4b7d04ae403c615604e4090521ecc5bb7af67f71be09ca01dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347940000000000000000000000000000000000000000a056e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421a01afbbda2cfebd56d2d0d1288617084931eb82bc346c678cac5eeff7c7a078e36a056e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421b9010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000830f424080833d090080830f424083010203a02ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e28800000000000003e8f840df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080c080000000000000000000"));
        }

        [Test]
        public void Big_block_is_handled_when_framing_enabled()
        {
            Transaction[] a = Build.A.Transaction.TestObjectNTimes(64);
            Block block = Build.A.Block.WithTransactions(a).TestObject;
            using NewBlockMessage newBlockMessage = new();
            newBlockMessage.Block = block;

            NewBlockMessageSerializer newBlockMessageSerializer = new();
            Packet packet = new("eth", 7, newBlockMessageSerializer.Serialize(newBlockMessage));

            _input.WriteByte(packet.PacketType);
            _input.WriteBytes(packet.Data);
            ZeroPacketSplitterTestWrapper packetSplitter = new();
            _output = packetSplitter.Encode(_input);

            byte[] outputBytes = new byte[_output.ReadableBytes];
            _output.ReadBytes(outputBytes);

            string outputHex = outputBytes.ToHexString(false);
            TestContext.Out.WriteLine(outputHex);
            Assert.That(outputHex, Is.EqualTo("000400c58001820a080000000000000007f90a04f90a00f901f9a0ff483e972a04a9a62bb4b7d04ae403c615604e4090521ecc5bb7af67f71be09ca01dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347940000000000000000000000000000000000000000a056e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421a0e56d912c1a3a6640cd5168501e756477c48037f73d7755074fed5c2b9ace030ba056e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421b9010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000830f424080833d090080830f424083010203a02ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e28800000000000003e8f90800df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000400c2800100000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000208c2800100000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080c0800000000000000000"));
        }

        [Test]
        public void Big_block_is_handled_when_framing_disabled()
        {
            Transaction[] a = Build.A.Transaction.TestObjectNTimes(64);
            Block block = Build.A.Block.WithTransactions(a).TestObject;
            using NewBlockMessage newBlockMessage = new();
            newBlockMessage.Block = block;

            NewBlockMessageSerializer newBlockMessageSerializer = new();
            Packet packet = new("eth", 7, newBlockMessageSerializer.Serialize(newBlockMessage));

            _input.WriteByte(packet.PacketType);
            _input.WriteBytes(packet.Data);
            ZeroPacketSplitterTestWrapper packetSplitter = new();
            packetSplitter.DisableFraming();
            _output = packetSplitter.Encode(_input);

            byte[] outputBytes = new byte[_output.ReadableBytes];
            _output.ReadBytes(outputBytes);

            string outputHex = outputBytes.ToHexString(false);
            TestContext.Out.WriteLine(outputHex);
            Assert.That(outputHex, Is.EqualTo("000a08c280800000000000000000000007f90a04f90a00f901f9a0ff483e972a04a9a62bb4b7d04ae403c615604e4090521ecc5bb7af67f71be09ca01dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347940000000000000000000000000000000000000000a056e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421a0e56d912c1a3a6640cd5168501e756477c48037f73d7755074fed5c2b9ace030ba056e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421b9010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000830f424080833d090080830f424083010203a02ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e28800000000000003e8f90800df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080df80018252089400000000000000000000000000000000000000000180808080c0800000000000000000"));
        }

        [Test]
        public void Splits_packet_into_two_frames()
        {
            Packet packet = new("eth", 2, new byte[Frame.DefaultMaxFrameSize + 1]);
            _input.WriteByte(packet.PacketType);
            _input.WriteBytes(packet.Data);

            ZeroPacketSplitterTestWrapper packetSplitter = new();
            _output = packetSplitter.Encode(_input);

            byte[] outputBytes = new byte[_output.ReadableBytes];
            _output.ReadBytes(outputBytes);

            string outputHex = outputBytes.ToHexString(false);
            Assert.That(outputHex, Is.EqualTo("000400c580018204020000000000000002000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002c280010000000000000000000000000000000000000000000000000000"));
        }

        [Test]
        public void Padding_is_done_after_adding_packet_size()
        {
            Packet packet = new("eth", 2, new byte[Frame.DefaultMaxFrameSize - 1]);
            _input.WriteByte(packet.PacketType);
            _input.WriteBytes(packet.Data);

            ZeroPacketSplitterTestWrapper packetSplitter = new();
            _output = packetSplitter.Encode(_input);

            byte[] outputBytes = new byte[_output.ReadableBytes];
            _output.ReadBytes(outputBytes);

            Assert.That(outputBytes.ToHexString(false), Is.EqualTo("000400c280800000000000000000000002000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"));
        }
    }
}
