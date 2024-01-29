// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Autofac;
using Nethermind.Abi;
using Nethermind.Api.Extensions;
using Nethermind.Blockchain;
using Nethermind.Blockchain.Blocks;
using Nethermind.Blockchain.Filters;
using Nethermind.Blockchain.Find;
using Nethermind.Blockchain.FullPruning;
using Nethermind.Blockchain.Receipts;
using Nethermind.Blockchain.Services;
using Nethermind.Config;
using Nethermind.Consensus;
using Nethermind.Consensus.Comparers;
using Nethermind.Consensus.Processing;
using Nethermind.Consensus.Producers;
using Nethermind.Consensus.Rewards;
using Nethermind.Consensus.Validators;
using Nethermind.Core;
using Nethermind.Core.Authentication;
using Nethermind.Core.PubSub;
using Nethermind.Core.Specs;
using Nethermind.Core.Timers;
using Nethermind.Crypto;
using Nethermind.Db;
using Nethermind.Db.Blooms;
using Nethermind.Evm.TransactionProcessing;
using Nethermind.Facade;
using Nethermind.Facade.Eth;
using Nethermind.Grpc;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.JsonRpc.Modules.Eth.GasPrice;
using Nethermind.JsonRpc.Modules.Subscribe;
using Nethermind.KeyStore;
using Nethermind.Logging;
using Nethermind.Monitoring;
using Nethermind.Network;
using Nethermind.Network.P2P.Analyzers;
using Nethermind.Network.Rlpx;
using Nethermind.Serialization.Json;
using Nethermind.State;
using Nethermind.State.Repositories;
using Nethermind.Synchronization;
using Nethermind.Synchronization.ParallelSync;
using Nethermind.Synchronization.Peers;
using Nethermind.Trie.Pruning;
using Nethermind.TxPool;
using Nethermind.Wallet;
using Nethermind.Sockets;
using Nethermind.Specs.ChainSpecStyle;
using Nethermind.Stats;

namespace Nethermind.Api
{
    public class NethermindApi : INethermindApi
    {
        public NethermindApi(ILifetimeScope container)
        {
            BaseContainer = container;
        }

        public IBlockchainBridge CreateBlockchainBridge()
        {
            ReadOnlyBlockTree readOnlyTree = BlockTree!.AsReadOnly();

            // TODO: reuse the same trie cache here
            ReadOnlyTxProcessingEnv readOnlyTxProcessingEnv = new(
                WorldStateManager!,
                readOnlyTree,
                SpecProvider,
                LogManager);

            IMiningConfig miningConfig = ConfigProvider.GetConfig<IMiningConfig>();
            IBlocksConfig blocksConfig = ConfigProvider.GetConfig<IBlocksConfig>();

            return new BlockchainBridge(
                readOnlyTxProcessingEnv,
                TxPool,
                ReceiptFinder,
                FilterStore,
                FilterManager,
                EthereumEcdsa,
                Timestamper,
                LogFinder,
                SpecProvider!,
                blocksConfig,
                miningConfig.Enabled
            );
        }

        public IAbiEncoder AbiEncoder { get; } = Nethermind.Abi.AbiEncoder.Instance;
        public IBlobTxStorage BlobTxStorage => BaseContainer.Resolve<IBlobTxStorage>();
        public IBlockchainProcessor? BlockchainProcessor { get; set; }
        public CompositeBlockPreprocessorStep BlockPreprocessor { get; } = new();
        public IBlockProcessingQueue? BlockProcessingQueue { get; set; }
        public IBlockProcessor? MainBlockProcessor { get; set; }
        public IBlockProducer? BlockProducer { get; set; }
        public IBlockTree? BlockTree { get; set; }
        public IBlockValidator? BlockValidator { get; set; }
        public IBloomStorage? BloomStorage { get; set; }
        public IChainLevelInfoRepository? ChainLevelInfoRepository { get; set; }
        public IConfigProvider ConfigProvider => BaseContainer.Resolve<IConfigProvider>();
        public ICryptoRandom CryptoRandom => BaseContainer.Resolve<ICryptoRandom>();
        public IDbProvider DbProvider => BaseContainer.Resolve<IDbProvider>();
        public IDisconnectsAnalyzer? DisconnectsAnalyzer { get; set; }
        public IDiscoveryApp? DiscoveryApp { get; set; }
        public ISigner? EngineSigner { get; set; }
        public ISignerStore? EngineSignerStore { get; set; }
        public IEnode? Enode => BaseContainer.Resolve<IEnode>();
        public IEthereumEcdsa? EthereumEcdsa { get; set; }
        public IFileSystem FileSystem => BaseContainer.Resolve<IFileSystem>();
        public IFilterStore? FilterStore { get; set; }
        public IFilterManager? FilterManager { get; set; }
        public IUnclesValidator? UnclesValidator { get; set; }
        public IGrpcServer? GrpcServer { get; set; }
        public IHeaderValidator? HeaderValidator { get; set; }

        public IManualBlockProductionTrigger ManualBlockProductionTrigger { get; set; } =
            new BuildBlocksWhenRequested();

        public IJsonSerializer EthereumJsonSerializer => BaseContainer.Resolve<IJsonSerializer>();
        public IKeyStore KeyStore => BaseContainer.Resolve<IKeyStore>();
        public ILogFinder? LogFinder { get; set; }
        public ILogManager LogManager => BaseContainer.Resolve<ILogManager>();
        public IMessageSerializationService MessageSerializationService { get; } = new MessageSerializationService();
        public IGossipPolicy GossipPolicy { get; set; } = Policy.FullGossip;
        public IMonitoringService MonitoringService { get; set; } = NullMonitoringService.Instance;
        public INodeStatsManager NodeStatsManager => BaseContainer.Resolve<INodeStatsManager>();
        public IPeerManager? PeerManager { get; set; }
        public IPeerPool? PeerPool { get; set; }
        public IProtocolsManager? ProtocolsManager { get; set; }
        public IProtocolValidator? ProtocolValidator { get; set; }
        public IReceiptStorage? ReceiptStorage { get; set; }
        public IWitnessCollector? WitnessCollector { get; set; }
        public IWitnessRepository? WitnessRepository { get; set; }
        public IReceiptFinder? ReceiptFinder { get; set; }
        public IReceiptMonitor? ReceiptMonitor { get; set; }
        public IRewardCalculatorSource? RewardCalculatorSource { get; set; } = NoBlockRewards.Instance;
        public IRlpxHost? RlpxPeer { get; set; }
        public IRpcModuleProvider? RpcModuleProvider { get; set; } = NullModuleProvider.Instance;
        public IRpcAuthentication? RpcAuthentication { get; set; }
        public IJsonRpcLocalStats? JsonRpcLocalStats { get; set; }
        public ISealer? Sealer { get; set; } = NullSealEngine.Instance;
        public string SealEngineType => ChainSpec.SealEngineType;
        public ISealValidator? SealValidator { get; set; } = NullSealEngine.Instance;
        private ISealEngine? _sealEngine;
        public ISealEngine SealEngine
        {
            get
            {
                return _sealEngine ??= new SealEngine(Sealer, SealValidator);
            }

            set
            {
                _sealEngine = value;
            }
        }

        public ISessionMonitor? SessionMonitor { get; set; }
        public ISpecProvider SpecProvider => BaseContainer.Resolve<ISpecProvider>();
        public IPoSSwitcher PoSSwitcher { get; set; } = NoPoS.Instance;
        public ISyncModeSelector SyncModeSelector { get; set; } = null!;

        public ISyncProgressResolver? SyncProgressResolver { get; set; }
        public IBetterPeerStrategy? BetterPeerStrategy { get; set; }
        public IPivot? Pivot { get; set; }
        public ISyncPeerPool? SyncPeerPool { get; set; }
        public IPeerDifficultyRefreshPool? PeerDifficultyRefreshPool { get; set; }
        public ISynchronizer? Synchronizer { get; set; }
        public ISyncServer? SyncServer { get; set; }
        public IWorldState? WorldState { get; set; }
        public IReadOnlyStateProvider? ChainHeadStateProvider { get; set; }
        public IWorldStateManager? WorldStateManager { get; set; }
        public IStateReader? StateReader { get; set; }
        public IStaticNodesManager? StaticNodesManager { get; set; }
        public ITimestamper Timestamper => BaseContainer.Resolve<ITimestamper>();
        public ITimerFactory TimerFactory => BaseContainer.Resolve<ITimerFactory>();
        public ITransactionProcessor? TransactionProcessor { get; set; }
        public ITrieStore? TrieStore { get; set; }
        public ITxSender? TxSender { get; set; }
        public INonceManager? NonceManager { get; set; }
        public ITxPool? TxPool { get; set; }
        public ITxPoolInfoProvider? TxPoolInfoProvider { get; set; }
        public IHealthHintService? HealthHintService { get; set; }
        public IRpcCapabilitiesProvider? RpcCapabilitiesProvider { get; set; }
        public ITxValidator? TxValidator { get; set; }
        public IBlockFinalizationManager? FinalizationManager { get; set; }
        public IGasLimitCalculator GasLimitCalculator => BaseContainer.Resolve<IGasLimitCalculator>();
        public IBlockProducerEnvFactory? BlockProducerEnvFactory { get; set; }
        public IGasPriceOracle? GasPriceOracle { get; set; }

        public IEthSyncingInfo? EthSyncingInfo { get; set; }
        public IBlockProductionPolicy? BlockProductionPolicy { get; set; }
        public IWallet Wallet => BaseContainer.Resolve<IWallet>();
        public IBlockStore? BadBlocksStore { get; set; }
        public ITransactionComparerProvider? TransactionComparerProvider { get; set; }
        public IWebSocketsManager WebSocketsManager { get; set; } = new WebSocketsManager();

        public ISubscriptionFactory? SubscriptionFactory { get; set; }

        public ChainSpec ChainSpec => BaseContainer.Resolve<ChainSpec>();

        // Note: when migrating to dependency injection, the component implementing `IDispose` is automatically disposed
        // on autofac context dispose, so registering to this is no longer required.
        public DisposableStack DisposeStack { get; } = new();
        public IReadOnlyList<INethermindPlugin> Plugins => BaseContainer.Resolve<IReadOnlyList<INethermindPlugin>>();
        public IList<IPublisher> Publishers { get; } = new List<IPublisher>(); // this should be called publishers
        public CompositePruningTrigger PruningTrigger { get; } = new();
        public IProcessExitSource ProcessExit => BaseContainer.Resolve<IProcessExitSource>();
        public CompositeTxGossipPolicy TxGossipPolicy { get; } = new();

        public ILifetimeScope BaseContainer { get; set; }
    }
}
