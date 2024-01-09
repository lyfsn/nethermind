// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Core;
using Nethermind.Api;
using Nethermind.Api.Extensions;
using Nethermind.Config;
using Nethermind.Core;
using Nethermind.JsonRpc.Data;
using Nethermind.Logging;
using Nethermind.Runner.Modules;
using Nethermind.Serialization.Json;
using Nethermind.Specs.ChainSpecStyle;

namespace Nethermind.Runner.Ethereum.Api
{
    public class ApiBuilder
    {
        private readonly IConfigProvider _configProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IProcessExitSource _processExitSource;
        private readonly ILogManager _logManager;
        private readonly ILogger _logger;
        private readonly IInitConfig _initConfig;

        public ApiBuilder(IConfigProvider configProvider, IProcessExitSource processExitSource, ILogManager logManager)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _processExitSource = processExitSource ?? throw new ArgumentNullException(nameof(processExitSource));
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            _logger = _logManager.GetClassLogger();
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _initConfig = configProvider.GetConfig<IInitConfig>();
            _jsonSerializer = new EthereumJsonSerializer();
        }

        public INethermindApi Create(params IConsensusPlugin[] consensusPlugins) =>
            Create((IEnumerable<IConsensusPlugin>)consensusPlugins);

        public INethermindApi Create(IEnumerable<IConsensusPlugin> consensusPlugins)
        {
            ChainSpec chainSpec = LoadChainSpec(_jsonSerializer);
            bool wasCreated = Interlocked.CompareExchange(ref _apiCreated, 1, 0) == 1;
            if (wasCreated)
            {
                throw new NotSupportedException("Creation of multiple APIs not supported.");
            }

            string engine = chainSpec.SealEngineType;
            IConsensusPlugin? enginePlugin = consensusPlugins.FirstOrDefault(p => p.SealEngineType == engine);

            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new BaseModule(
                _configProvider,
                _processExitSource,
                chainSpec,
                _jsonSerializer,
                _logManager
            ));
            containerBuilder.RegisterModule(new NetworkModule());
            containerBuilder.RegisterModule(new KeyStoreModule());
            IModule? consensusModule = enginePlugin?.GetEarlyModule();
            if (consensusModule != null) containerBuilder.RegisterModule(consensusModule);
            return containerBuilder.Build().Resolve<INethermindApi>();
        }

        private int _apiCreated;

        private ChainSpec LoadChainSpec(IJsonSerializer ethereumJsonSerializer)
        {
            bool hiveEnabled = Environment.GetEnvironmentVariable("NETHERMIND_HIVE_ENABLED")?.ToLowerInvariant() == "true";
            bool hiveChainSpecExists = File.Exists(_initConfig.HiveChainSpecPath);

            string chainSpecFile;
            if (hiveEnabled && hiveChainSpecExists)
                chainSpecFile = _initConfig.HiveChainSpecPath;
            else
                chainSpecFile = _initConfig.ChainSpecPath;

            if (_logger.IsDebug) _logger.Debug($"Loading chain spec from {chainSpecFile}");

            ThisNodeInfo.AddInfo("Chainspec    :", $"{chainSpecFile}");

            IChainSpecLoader loader = new ChainSpecLoader(ethereumJsonSerializer);
            ChainSpec chainSpec = loader.LoadEmbeddedOrFromFile(chainSpecFile, _logger);
            TransactionForRpc.DefaultChainId = chainSpec.ChainId;
            return chainSpec;
        }
    }
}
