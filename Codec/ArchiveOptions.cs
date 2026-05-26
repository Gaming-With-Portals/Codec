// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO;
    using System.IO.Abstractions;
    using Codec.Archives;
    using DiscUtils.Complete;
    using Microsoft.Extensions.DependencyInjection;

    public class ArchiveOptions
    {
        public static readonly Option<string> KeyOption = new(
            name: "--key",
            description: "The key to the MGS1 alldata.bin file.")
        {
            IsRequired = true,
        };

        public required string Key { get; set; }

        public static void Attach(Command command)
        {
            command.AddGlobalOption(KeyOption);
        }

        public static void Bind(InvocationContext context, IServiceCollection services)
        {
            var options = new ArchiveOptions
            {
                Key = context.ParseResult.GetValueForOption(KeyOption)!,
            };

            services.AddSingleton(options);

            SetupHelper.SetupComplete();

            services.AddSingleton(s =>
                new NestedFileSystemManager(new RootEnumerableFileSystem(), (file, fs, fsPath) =>
                {
                    foreach (var resolver in s.GetServices<FileSystemResolver>())
                    {
                        if (resolver(s, file, fs, fsPath) is FileSystemCreator creator)
                        {
                            return new Func<IFileSystem, string, IFileSystem>(creator);
                        }
                    }

                    return null;
                }));
        }
    }
}
