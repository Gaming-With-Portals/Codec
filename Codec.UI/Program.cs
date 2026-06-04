// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec.UI
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Windows.Forms;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    internal static partial class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            var rootCommand = new RootCommand();

            ArchiveOptions.Attach(rootCommand);
            EnvironmentOptions.Attach(rootCommand);

            var browseCommand1 = new Command("browse", "Browse Files");
            browseCommand1.AddAlias("browser");
            rootCommand.Add(browseCommand1);

            static void InstallSharedConfiguration(InvocationContext context, IServiceCollection services)
            {
                EnvironmentOptions.Bind(context, services);
                ServiceRegistration.Register(services);
            }

            browseCommand1.SetHandler(
                context =>
                {
                    var builder = Host.CreateDefaultBuilder(args);
                    builder.ConfigureServices(services =>
                    {
                        InstallSharedConfiguration(context, services);
                        ArchiveOptions.Bind(context, services);
                    });

                    using var host = builder.Build();
                    ApplicationConfiguration.Initialize();
                    Application.Run(host.Services.GetRequiredService<Browser>());
                });

            return rootCommand.Invoke(args);
        }
    }
}
