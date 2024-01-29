// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.IO;
using Autofac;
using FluentAssertions;
using Nethermind.Api;
using Nethermind.Blockchain.Synchronization;
using Nethermind.Config;
using Nethermind.Db;
using Nethermind.Db.FullPruning;
using Nethermind.Db.Rocks;
using Nethermind.Db.Rocks.Config;
using Nethermind.Logging;
using Nethermind.Runner.Modules;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.Runner.Test.Modules;

public class DatabaseModuleTests
{
    private string _folderWithDbs = null!;

    [OneTimeSetUp]
    public void Initialize()
    {
        _folderWithDbs = Guid.NewGuid().ToString();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InitializerTests_MemDbProvider(bool useReceipts)
    {
        using IContainer container = InitializeStandardDb(useReceipts, DiagnosticMode.MemDb, "mem");
        IDbProvider dbProvider = container.Resolve<IDbProvider>();
        Type receiptsType = GetReceiptsType(useReceipts, typeof(MemColumnsDb<ReceiptsColumns>));
        AssertStandardDbs(dbProvider, typeof(MemDb), receiptsType);
        dbProvider.StateDb.Should().BeOfType(typeof(FullPruningDb));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InitializerTests_RocksDbProvider(bool useReceipts)
    {
        using IContainer container = InitializeStandardDb(useReceipts, DiagnosticMode.None, $"rocks_{useReceipts}");
        IDbProvider dbProvider = container.Resolve<IDbProvider>();
        Type receiptsType = GetReceiptsType(useReceipts);
        AssertStandardDbs(dbProvider, typeof(DbOnTheRocks), receiptsType);
        dbProvider.StateDb.Should().BeOfType(typeof(FullPruningDb));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InitializerTests_ReadonlyDbProvider(bool useReceipts)
    {
        using IContainer container = InitializeStandardDb(useReceipts, DiagnosticMode.ReadOnlyDb, $"readonly_{useReceipts}");
        IDbProvider dbProvider = container.Resolve<IDbProvider>();
        AssertStandardDbs(dbProvider, typeof(ReadOnlyDb), typeof(ReadOnlyColumnsDb<ReceiptsColumns>));
        dbProvider.StateDb.Should().BeOfType(typeof(ReadOnlyDb));
    }

    [Test]
    public void DbProvider_ThrowExceptionOnGettingNotRegisteredDb()
    {
        using IContainer container = InitializeStandardDb(true, DiagnosticMode.ReadOnlyDb, $"throw_on_unregistered");
        IDbProvider dbProvider = container.Resolve<IDbProvider>();
        Assert.Throws<ArgumentException>(() => dbProvider.GetColumnDb<ReceiptsColumns>("differentdb"));
    }

    [Test]
    public void InitializerTests_WithPruning()
    {
        using IContainer container = InitializeStandardDb(false, DiagnosticMode.None, "pruning");
        IDbProvider dbProvider = container.Resolve<IDbProvider>();
        dbProvider.StateDb.Should().BeOfType<FullPruningDb>();
    }

    private IContainer InitializeStandardDb(bool useReceipts, DiagnosticMode diagnosticMode, string path)
    {
        IConfigProvider configProvider = Substitute.For<IConfigProvider>();
        InitConfig initConfig = new InitConfig()
        {
            DiagnosticMode = diagnosticMode,
            BaseDbPath = path,
            StoreReceipts = useReceipts
        };
        SyncConfig syncConfig = new SyncConfig()
        {
            DownloadReceiptsInFastSync = useReceipts
        };
        configProvider.GetConfig<IDbConfig>().Returns(new DbConfig());
        configProvider.GetConfig<IInitConfig>().Returns(initConfig);
        configProvider.GetConfig<ISyncConfig>().Returns(syncConfig);

        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterInstance(configProvider);
        builder.RegisterInstance(LimboLogs.Instance).AsImplementedInterfaces();
        builder.RegisterModule(new BaseModule());
        builder.RegisterModule(new DatabaseModule(configProvider));
        return builder.Build();
    }

    private static Type GetReceiptsType(bool useReceipts, Type receiptType = null) => useReceipts ? receiptType ?? typeof(ColumnsDb<ReceiptsColumns>) : typeof(ReadOnlyColumnsDb<ReceiptsColumns>);

    private void AssertStandardDbs(IDbProvider dbProvider, Type dbType, Type receiptsDb)
    {
        dbProvider.BlockInfosDb.Should().BeOfType(dbType);
        dbProvider.BlocksDb.Should().BeOfType(dbType);
        dbProvider.BloomDb.Should().BeOfType(dbType);
        dbProvider.ChtDb.Should().BeOfType(dbType);
        dbProvider.HeadersDb.Should().BeOfType(dbType);
        dbProvider.ReceiptsDb.Should().BeOfType(receiptsDb);
        dbProvider.CodeDb.Should().BeOfType(dbType);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folderWithDbs))
            Directory.Delete(_folderWithDbs, true);
    }
}
