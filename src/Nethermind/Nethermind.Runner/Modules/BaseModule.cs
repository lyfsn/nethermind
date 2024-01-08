// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Autofac.Features.ResolveAnything;
using Nethermind.Api;
using Nethermind.Blockchain.Synchronization;
using Nethermind.Config;
using Nethermind.Consensus;
using Nethermind.Core.Specs;
using Nethermind.Core.Timers;
using Nethermind.Crypto;
using Nethermind.Db.Rocks.Config;
using Nethermind.Logging;
using Nethermind.Network.Config;
using Nethermind.Serialization.Json;
using Nethermind.Specs.ChainSpecStyle;
using Nethermind.TxPool;

namespace Nethermind.Runner.Modules;

public class BaseModule : Module
{
    private readonly IConfigProvider _configProvider;
    private readonly IProcessExitSource _processExitSource;
    private readonly ChainSpec _chainSpec;
    private readonly ILogManager _logManager;
    private readonly IJsonSerializer _jsonSerializer;

    public BaseModule(
        IConfigProvider configProvider,
        IProcessExitSource processExitSource,
        ChainSpec chainSpec,
        IJsonSerializer jsonSerializer,
        ILogManager logManager
    )
    {
        _configProvider = configProvider;
        _processExitSource = processExitSource;
        _chainSpec = chainSpec;
        _jsonSerializer = jsonSerializer;
        _logManager = logManager;

        SetLoggerVariables(_chainSpec, _logManager);
    }

    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        builder.RegisterSource(new ConfigRegistrationSource(_configProvider));
        builder.RegisterInstance(_configProvider);
        builder.RegisterInstance(_processExitSource);
        builder.RegisterInstance(_chainSpec);
        builder.RegisterInstance(_jsonSerializer);
        builder.RegisterInstance(_logManager);

        builder.RegisterType<ChainSpecBasedSpecProvider>()
            .As<ISpecProvider>()
            .SingleInstance();

        builder.RegisterType<CryptoRandom>()
            .As<ICryptoRandom>()
            .SingleInstance();

        builder.RegisterType<FollowOtherMiners>()
            .As<IGasLimitCalculator>()
            .SingleInstance();

        builder.RegisterType<NethermindApi>()
            .As<INethermindApi>()
            .SingleInstance();

        builder.RegisterInstance(TimerFactory.Default)
            .As<ITimerFactory>();
    }

    private void SetLoggerVariables(ChainSpec chainSpec, ILogManager logManager)
    {
        logManager.SetGlobalVariable("chain", chainSpec.Name);
        logManager.SetGlobalVariable("chainId", chainSpec.ChainId);
        logManager.SetGlobalVariable("engine", chainSpec.SealEngineType);
    }

    /// <summary>
    /// Dynamically resolve IConfig<T>
    /// </summary>
    private class ConfigRegistrationSource: IRegistrationSource
    {
        private readonly IConfigProvider _configProvider;

        internal ConfigRegistrationSource(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
        {
            IServiceWithType swt = service as IServiceWithType;
            if(swt == null || !typeof(IConfig).IsAssignableFrom(swt.ServiceType))
            {
                // It's not a request for the base handler type, so skip it.
                return Enumerable.Empty<IComponentRegistration>();
            }

            // Dynamically resolve IConfig
            ComponentRegistration registration = new ComponentRegistration(
                Guid.NewGuid(),
                new DelegateActivator(swt.ServiceType, (c, p) => _configProvider.GetConfig(swt.ServiceType)),
                new RootScopeLifetime(),
                InstanceSharing.Shared,
                InstanceOwnership.OwnedByLifetimeScope,
                new [] { service },
                new Dictionary<string, object>());

            return new IComponentRegistration[] { registration };
        }

        public bool IsAdapterForIndividualComponents => false;
    }
}
