// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Antlr4.Runtime.Misc;
using Autofac;
using FluentAssertions;
using Nethermind.Api;
using Nethermind.Config;
using Nethermind.Logging;
using Nethermind.Runner.Modules;
using Nethermind.Serialization.Json;
using Nethermind.Specs.ChainSpecStyle;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Runner.Test.Modules;

public class BaseModuleTests
{
    [Test]
    public void Can_Resolve()
    {
        IConfigProvider configProvider = Substitute.For<IConfigProvider>();
        IProcessExitSource processExitSource = Substitute.For<IProcessExitSource>();
        ChainSpec chainSpec = new();
        IJsonSerializer jsonSerializer = new EthereumJsonSerializer();
        ILogManager logManager = Substitute.For<ILogManager>();

        configProvider.GetConfig(typeof(IInitConfig)).Returns(new InitConfig());
        logManager.GetClassLogger<TestClass>().Returns(LimboLogs.Instance.GetClassLogger<TestClass>());

        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterModule(new BaseModule(
            configProvider,
            processExitSource,
            chainSpec,
            jsonSerializer,
            logManager
        ));

        using IContainer container = builder.Build();

        TestClass testObj = container.Resolve<TestClass>();
        testObj.Logger.Should().NotBeNull();
        testObj.InitConfig.Should().NotBeNull();
    }

    private class TestClass
    {
        public IInitConfig InitConfig;
        public ILogger Logger;

        public TestClass(IInitConfig initConfig, ILogger<TestClass> logger)
        {
            InitConfig = initConfig;
            Logger = logger;
        }
    }
}
