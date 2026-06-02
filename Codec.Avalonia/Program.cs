namespace Codec.Avalonia
{
    using System;
    using System.CommandLine;
    using System.Threading.Tasks;
    using global::Avalonia;
    using Codec.Avalonia.Services;
    using Codec.Avalonia.ViewModels;
    using Codec.Avalonia.Views;
    using Microsoft.Extensions.DependencyInjection;

    sealed class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            ArchiveOptions.Attach(rootCommand);
            EnvironmentOptions.Attach(rootCommand);

            var browseCommand1 = new Command("browse", "Browse Files");
            browseCommand1.AddAlias("browser");
            rootCommand.Add(browseCommand1);

            browseCommand1.SetHandler(
                context =>
                {
                    var services = new ServiceCollection();

                    ServiceRegistration.Register(services);
                    EnvironmentOptions.Bind(context, services);
                    ArchiveOptions.Bind(context, services);

                    services.AddSingleton<AudioPlayer>();
                    services.AddSingleton<ImageLoader>();
                    services.AddSingleton<FileSaveService>();
                    services.AddTransient<FileTreeViewModel>();
                    services.AddTransient<EntryListViewModel>();
                    services.AddTransient<BrowserViewModel>();
                    services.AddTransient<BrowserWindow>();

                    using var serviceProvider = services.BuildServiceProvider();
                    BuildAvaloniaApp(serviceProvider).StartWithClassicDesktopLifetime(args);
                });

            return await rootCommand.InvokeAsync(args).ConfigureAwait(true);
        }

        public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
            => AppBuilder.Configure(() => new App(services))
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();
    }
}
