// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using GMWare.M2.Psb;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;

    public sealed class PsbVirtualFileSystem : FileSystemBase
    {
        private readonly string filePath;
        private readonly IFileSystem fileSystem;
        private readonly List<string> index;

        public PsbVirtualFileSystem(string filePath, IFileSystem? fileSystem = null)
        {
            fileSystem ??= new FileSystem();
            using var fs = fileSystem.File.OpenRead(filePath);
            this.index = Index(PsbDecode(fs));
            this.filePath = filePath;
            this.fileSystem = fileSystem;
            this.IndexFileName = fileSystem.Path.ChangeExtension(fileSystem.Path.GetFileName(filePath), ".json")!;

            this.Directory = new DirectoryProvider(this);
            this.File = new FileProvider(this);
        }

        private static List<string> Index(PsbReader psbReader)
        {
            var root = psbReader.Root;
            psbReader.LoadAllStreamData();
            return [.. psbReader.StreamCache.Keys.Select(k => $"_stream.{k}")];
        }

        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<FileSystemResolver>((serviceProvider, fullPath, parentRelativePath, parent, parentPath) =>
            {
                if (string.Equals(parent.Path.GetExtension(parentRelativePath), ".psb", StringComparison.OrdinalIgnoreCase))
                {
                    var seed = serviceProvider.GetRequiredService<ArchiveOptions>().Key;
                    return (fullPath, parentRelativePath, parent, parentPath) => new PsbVirtualFileSystem(parentRelativePath, parent);
                }

                return null;
            });
        }

        public string IndexFileName { get; }

        internal static PsbReader PsbDecode(Stream decompStream)
        {
            var memoryStream = new MemoryStream();
            decompStream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return new PsbReader(memoryStream, filter: null);
        }

        private class DirectoryProvider(PsbVirtualFileSystem parent) : DirectoryBase(parent)
        {
            protected override IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption, bool files = true, bool directories = true)
            {
                if (path != string.Empty)
                {
                    throw new DirectoryNotFoundException();
                }

                var glob = PathExtensions.GlobToRegex(searchPattern);
                if (files)
                {
                    if (glob.IsMatch(parent.IndexFileName))
                    {
                        yield return parent.IndexFileName;
                    }

                    foreach (var entry in parent.index)
                    {
                        if (glob.IsMatch(entry))
                        {
                            yield return entry;
                        }
                    }
                }
            }
        }

        private class FileProvider(PsbVirtualFileSystem parent) : FileBase(parent)
        {
            public override bool Exists([NotNullWhen(true)] string? path) =>
                string.Equals(path, parent.IndexFileName, StringComparison.OrdinalIgnoreCase) ||
                parent.index.Contains(path, StringComparer.OrdinalIgnoreCase);

            public override FileSystemStream OpenRead(string path)
            {
                if (!this.Exists(path))
                {
                    throw new FileNotFoundException();
                }

                using var stream = parent.fileSystem.File.OpenRead(parent.filePath);
                var psb = PsbDecode(stream);
                var root = psb.Root;
                MemoryStream memoryStream;
                if (path == parent.IndexFileName)
                {
                    memoryStream = new MemoryStream();
                    using var sw = new StreamWriter(memoryStream, leaveOpen: true) { AutoFlush = true };
                    using var jw = new JsonTextWriter(sw) { CloseOutput = false, Formatting = Formatting.Indented };
                    root.WriteTo(jw);
                }
                else
                {
                    var id = uint.Parse(Regex.Match(path, "^_stream\\.(\\d+)$").Groups[1].Value, CultureInfo.InvariantCulture);
                    memoryStream = new MemoryStream(psb.StreamCache[id].BinaryData);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return new StreamWrapper(
                    memoryStream,
                    path,
                    false);
            }
        }
    }
}
