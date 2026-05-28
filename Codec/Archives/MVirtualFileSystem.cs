// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using GMWare.M2.MArchive;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class MVirtualFileSystem : FileSystemBase
    {
        private static readonly Dictionary<uint, IMArchiveCodec> CodecLookup = new IMArchiveCodec[] { new ZStandardCodec(), new ZlibCodec(), new FastLzCodec(), }.ToDictionary(c => c.Magic);
        private readonly string filePath;
        private readonly string seed;
        private readonly int keyLength;
        private readonly IFileSystem fileSystem;

        public MVirtualFileSystem(string filePath, string seed, int keyLength, IFileSystem? fileSystem = null)
        {
            fileSystem ??= new FileSystem();
            this.filePath = filePath;
            this.seed = seed;
            this.keyLength = keyLength;
            this.fileSystem = fileSystem;
            this.FileName = fileSystem.Path.GetFileNameWithoutExtension(filePath);

            this.Directory = new DirectoryProvider(this);
            this.File = new FileProvider(this);
        }

        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<FileSystemResolver>((serviceProvider, fullPath, parentRelativePath, parent, parentPath) =>
            {
                if (string.Equals(parent.Path.GetExtension(parentRelativePath), ".m", StringComparison.OrdinalIgnoreCase))
                {
                    var seed = serviceProvider.GetRequiredService<ArchiveOptions>().Key;
                    return (fullPath, parentRelativePath, parent, parentPath) => new MVirtualFileSystem(parentRelativePath, seed, 64, parent);
                }

                return null;
            });
        }

        public string FileName { get; }

        internal static Stream? ReadMArchive(FileSystemStream fs, string seed, int keyLength, out int decompressedLength)
        {
            var br = new BinaryReader(fs);
            var magic = br.ReadUInt32();
            if (!CodecLookup.TryGetValue(magic, out var codec))
            {
                decompressedLength = 0;
                return null;
            }

            decompressedLength = br.ReadInt32();
            var cs = new MArchiveCryptoStream(fs, fs.Name, seed, keyLength);
            return codec.GetDecompressionStream(cs, decompressedLength);
        }

        private class DirectoryProvider(MVirtualFileSystem parent) : DirectoryBase(parent)
        {
            protected override IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption, bool files = false, bool directories = false)
            {
                if (path != string.Empty)
                {
                    throw new DirectoryNotFoundException();
                }

                var glob = PathExtensions.GlobToRegex(searchPattern);
                if (files && glob.IsMatch(parent.FileName))
                {
                    yield return parent.FileName;
                }
            }
        }

        private class FileProvider(MVirtualFileSystem parent) : FileBase(parent)
        {
            public override bool Exists([NotNullWhen(true)] string? path) => string.Equals(path, parent.FileName, StringComparison.OrdinalIgnoreCase);

            public override FileSystemStream OpenRead(string path)
            {
                if (!this.Exists(path))
                {
                    throw new FileNotFoundException();
                }

                return new StreamWrapper(
                    ReadMArchive(parent.fileSystem.File.OpenRead(parent.filePath), parent.seed, parent.keyLength, out _) ?? throw new InvalidDataException("Not a valid M archive."),
                    path,
                    false);
            }
        }
    }
}
