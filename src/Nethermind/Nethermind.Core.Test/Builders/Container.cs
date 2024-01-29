// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Autofac;
using Nethermind.Api;
using Nethermind.Logging;
using Nethermind.Runner.Modules;

namespace Nethermind.Core.Test.Builders;

public partial class Build
{
    public ContainerBuilder BasicTestContainerBuilder()
    {
        ContainerBuilder builder = new ContainerBuilder();
        builder.RegisterInstance(LimboLogs.Instance).AsImplementedInterfaces();
        builder.RegisterModule(new BaseModule());
        builder.RegisterModule(new DatabaseModule(storeReceipts: true, diagnosticMode: DiagnosticMode.MemDb));
        builder.RegisterModule(new CoreModule());
        return builder;
    }
}
