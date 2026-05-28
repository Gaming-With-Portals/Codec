namespace Codec.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using DiscUtils.Iso9660;

    internal class CDReaderVFSAdapter : FileSystemBase
    {
        private readonly CDReader cdReader;

        public CDReaderVFSAdapter(CDReader cdReader)
        {
            this.cdReader = cdReader;
            this.Directory = new DirectoryProvider(this);
            this.File = new FileProvider(this);
            this.Path = new PathProvider(this);
        }

        private static string[] FilterNames(string[] names)
        {
            return Array.ConvertAll(names, name =>
            {
                var lastSemicolon = name.LastIndexOf(";", StringComparison.OrdinalIgnoreCase);
                if (lastSemicolon >= 0)
                {
                    return name[..lastSemicolon];
                }
                else
                {
                    return name;
                }
            });
        }

        private class DirectoryProvider(CDReaderVFSAdapter parent) : DirectoryBase(parent)
        {
            public override void Delete(string path) => parent.cdReader.DeleteDirectory(path);

            public override void Delete(string path, bool recursive) => parent.cdReader.DeleteDirectory(path, recursive);

            public override IEnumerable<string> EnumerateDirectories(string path) => this.GetDirectories(path);

            public override IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => this.GetDirectories(path, searchPattern);

            public override IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption) => this.GetDirectories(path, searchPattern, searchOption);

            public override IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => this.GetDirectories(path, searchPattern, enumerationOptions);

            public override IEnumerable<string> EnumerateFiles(string path) => this.GetFiles(path);

            public override IEnumerable<string> EnumerateFiles(string path, string searchPattern) => this.GetFiles(path, searchPattern);

            public override IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => this.GetFiles(path, searchPattern, searchOption);

            public override IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => this.GetFiles(path, searchPattern, enumerationOptions);

            public override IEnumerable<string> EnumerateFileSystemEntries(string path) => this.GetFileSystemEntries(path);

            public override IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) => this.GetFileSystemEntries(path, searchPattern);

            public override IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => this.GetFileSystemEntries(path, searchPattern, searchOption);

            public override IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => this.GetFileSystemEntries(path, searchPattern, enumerationOptions);

            public override bool Exists([NotNullWhen(true)] string? path) => parent.cdReader.Exists(path);

            public override DateTime GetCreationTime(string path) => parent.cdReader.GetCreationTime(path);

            public override DateTime GetCreationTimeUtc(string path) => parent.cdReader.GetCreationTimeUtc(path);

            public override string[] GetDirectories(string path) => parent.cdReader.GetDirectories(path);

            public override string[] GetDirectories(string path, string searchPattern) => parent.cdReader.GetDirectories(path, searchPattern);

            public override string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) => parent.cdReader.GetDirectories(path, searchPattern, searchOption);

            public override string[] GetFiles(string path) => FilterNames(parent.cdReader.GetFiles(path));

            public override string[] GetFiles(string path, string searchPattern) => FilterNames(parent.cdReader.GetFiles(path, searchPattern));

            public override string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => FilterNames(parent.cdReader.GetFiles(path, searchPattern, searchOption));

            public override string[] GetFileSystemEntries(string path) => FilterNames(parent.cdReader.GetFileSystemEntries(path));

            public override string[] GetFileSystemEntries(string path, string searchPattern) => FilterNames(parent.cdReader.GetFileSystemEntries(path, searchPattern));

            public override string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => searchOption == SearchOption.TopDirectoryOnly ? FilterNames(parent.cdReader.GetFileSystemEntries(path, searchPattern)) : throw new NotSupportedException();

            public override DateTime GetLastAccessTime(string path) => parent.cdReader.GetLastAccessTime(path);

            public override DateTime GetLastAccessTimeUtc(string path) => parent.cdReader.GetLastAccessTimeUtc(path);

            public override DateTime GetLastWriteTime(string path) => parent.cdReader.GetLastWriteTime(path);

            public override DateTime GetLastWriteTimeUtc(string path) => parent.cdReader.GetLastWriteTimeUtc(path);

            public override void SetCreationTime(string path, DateTime creationTime) => parent.cdReader.SetCreationTime(path, creationTime);

            public override void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => parent.cdReader.SetCreationTimeUtc(path, creationTimeUtc);

            public override void SetLastAccessTime(string path, DateTime lastAccessTime) => parent.cdReader.SetLastAccessTime(path, lastAccessTime);

            public override void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => parent.cdReader.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

            public override void SetLastWriteTime(string path, DateTime lastWriteTime) => parent.cdReader.SetLastWriteTime(path, lastWriteTime);

            public override void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => parent.cdReader.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
        }

        private class FileProvider(CDReaderVFSAdapter parent) : FileBase(parent)
        {
            public override bool Exists([NotNullWhen(true)] string? path) => parent.cdReader.Exists(path);

            public override FileAttributes GetAttributes(string path) => parent.cdReader.GetAttributes(path);

            public override DateTime GetCreationTime(string path) => parent.cdReader.GetCreationTime(path);

            public override DateTime GetCreationTimeUtc(string path) => parent.cdReader.GetCreationTimeUtc(path);

            public override DateTime GetLastAccessTime(string path) => parent.cdReader.GetLastAccessTime(path);

            public override DateTime GetLastAccessTimeUtc(string path) => parent.cdReader.GetLastAccessTimeUtc(path);

            public override DateTime GetLastWriteTime(string path) => parent.cdReader.GetLastWriteTime(path);

            public override DateTime GetLastWriteTimeUtc(string path) => parent.cdReader.GetLastWriteTimeUtc(path);

            public override FileSystemStream Open(string path, FileMode mode) => new StreamWrapper(parent.cdReader.OpenFile(path, mode), path, isAsync: false);

            public override FileSystemStream Open(string path, FileMode mode, FileAccess access) => new StreamWrapper(parent.cdReader.OpenFile(path, mode, access), path, isAsync: false);

            public override FileSystemStream OpenRead(string path) => new StreamWrapper(parent.cdReader.OpenFile(path, FileMode.Open), path, isAsync: false);

            public override void SetAttributes(string path, FileAttributes fileAttributes) => parent.cdReader.SetAttributes(path, fileAttributes);

            public override void SetCreationTime(string path, DateTime creationTime) => parent.cdReader.SetCreationTime(path, creationTime);

            public override void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => parent.cdReader.SetCreationTimeUtc(path, creationTimeUtc);

            public override void SetLastAccessTime(string path, DateTime lastAccessTime) => parent.cdReader.SetLastAccessTime(path, lastAccessTime);

            public override void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => parent.cdReader.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

            public override void SetLastWriteTime(string path, DateTime lastWriteTime) => parent.cdReader.SetLastWriteTime(path, lastWriteTime);

            public override void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => parent.cdReader.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
        }

        private class PathProvider(CDReaderVFSAdapter parent) : PathBase(parent)
        {
            public override char DirectorySeparatorChar => '\\';
        }
    }
}
