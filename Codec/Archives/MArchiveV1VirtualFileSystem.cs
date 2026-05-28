// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using GMWare.M2.Models;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class MArchiveV1VirtualFileSystem : FileSystemBase
    {
        private readonly ArchiveV1 index;
        private Stream sourceStream;

        public MArchiveV1VirtualFileSystem(string binPath, string key, IFileSystem? fileSystem = null)
        {
            fileSystem ??= new FileSystem();
            this.index = ReadIndex(fileSystem, fileSystem.Path.ChangeExtension(binPath, ".psb.m"), key, 64);
            this.sourceStream = fileSystem.File.OpenRead(binPath);

            this.Directory = new DirectoryProvider(this);
            this.File = new FileProvider(this);
        }

        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<FileSystemResolver>((serviceProvider, fullPath, parentRelativePath, parent, parentPath) =>
            {
                if (string.Equals(parent.Path.GetExtension(parentRelativePath), ".bin", StringComparison.OrdinalIgnoreCase) &&
                    parent.File.Exists(parent.Path.ChangeExtension(parentRelativePath, ".psb.m")))
                {
                    var key = serviceProvider.GetRequiredService<ArchiveOptions>().Key;
                    return (fullPath, parentRelativePath, parent, parentPath) => new MArchiveV1VirtualFileSystem(parentRelativePath, key, parent);
                }

                return null;
            });
        }

        private static ArchiveV1 ReadIndex(IFileSystem fileSystem, string indexPath, string seed, int keyLength)
        {
            using var fs = fileSystem.File.OpenRead(indexPath);
            using var decompStream = MVirtualFileSystem.ReadMArchive(fs, seed, keyLength, out var length) ?? throw new ArgumentException("Invalid archive format.", nameof(indexPath));
            return PsbVirtualFileSystem.PsbDecode(decompStream).Root.ToObject<ArchiveV1>();
        }

        protected override void Dispose(bool disposing)
        {
            this.sourceStream?.Dispose();
            this.sourceStream = null!;
        }

        private bool TryGetIndexInfo(string? path, [NotNullWhen(true)] out (long Start, long End) span)
        {
            if (path != null)
            {
                path = string.Join("/", path.Split(PathExtensions.Separators, StringSplitOptions.RemoveEmptyEntries));
                if (this.index.FileInfo.TryGetValue(path, out var list))
                {
                    span = (list[0], list[1]);
                    return true;
                }
            }

            span = default;
            return false;
        }

        private Stream GetStreamSpan(string path)
        {
            if (this.TryGetIndexInfo(path, out var span))
            {
                return new OffsetStreamSpan(this.sourceStream, span.Start, span.End);
            }

            var ex = new FileNotFoundException();
            throw new FileNotFoundException(ex.Message, path);
        }

        private class DirectoryProvider(MArchiveV1VirtualFileSystem parent) : DirectoryBase(parent)
        {
            protected override IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption, bool files = false, bool directories = false)
            {
                if (searchOption != SearchOption.TopDirectoryOnly)
                {
                    throw new NotImplementedException();
                }

                var glob = PathExtensions.GlobToRegex(searchPattern);
                var parts = path.Split(PathExtensions.Separators, StringSplitOptions.RemoveEmptyEntries);

                var flag = directories ? (files ? (bool?)null : true) : false;

                var pathExists = false;
                var seen = new HashSet<string>();
                foreach (var entry in parent.index.FileInfo.Keys)
                {
                    var entryParts = entry.Split(PathExtensions.Separators, StringSplitOptions.RemoveEmptyEntries);
                    if (entryParts.Length <= parts.Length || !PathExtensions.PrefixMatch(parts, entryParts))
                    {
                        continue;
                    }

                    var segmentIndex = parts.Length;
                    var nextSegment = entryParts[segmentIndex];
                    if (!seen.Add(nextSegment))
                    {
                        continue;
                    }

                    pathExists = true;
                    var nextSegmentIsDirectory = entryParts.Length > segmentIndex + 1;
                    if (!(nextSegmentIsDirectory != flag) && glob.IsMatch(nextSegment))
                    {
                        yield return string.Concat(parts.Select(p => p + "/")) + nextSegment;
                    }
                }

                if (!pathExists)
                {
                    throw new DirectoryNotFoundException();
                }
            }
        }

        private class FileProvider(MArchiveV1VirtualFileSystem parent) : FileBase(parent)
        {
            public override bool Exists([NotNullWhen(true)] string? path) => parent.TryGetIndexInfo(path, out _);

            public override FileSystemStream OpenRead(string path) => new StreamWrapper(parent.GetStreamSpan(path), path, isAsync: false);
        }
    }
}
