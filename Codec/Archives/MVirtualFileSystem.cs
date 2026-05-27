// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using GMWare.M2.MArchive;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Win32.SafeHandles;

    public sealed class MVirtualFileSystem : IFileSystem, IDisposable
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
            this.Path = new PathProvider(this);
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

        public IDirectory Directory { get; }

        public IDirectoryInfoFactory DirectoryInfo => throw new NotImplementedException();

        public IDriveInfoFactory DriveInfo => throw new NotImplementedException();

        public IFile File { get; }

        public IFileInfoFactory FileInfo => throw new NotImplementedException();

        public IFileStreamFactory FileStream => throw new NotImplementedException();

        public IFileSystemWatcherFactory FileSystemWatcher => throw new NotImplementedException();

        public IPath Path { get; }

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

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
        }

        private class DirectoryProvider : IDirectory
        {
            private MVirtualFileSystem parent;

            public DirectoryProvider(MVirtualFileSystem parent)
            {
                this.parent = parent;
            }

            public IFileSystem FileSystem => throw new NotImplementedException();

            public IDirectoryInfo CreateDirectory(string path) => throw new NotImplementedException();

            public IDirectoryInfo CreateDirectory(string path, UnixFileMode unixCreateMode) => throw new NotImplementedException();

            public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget) => throw new NotImplementedException();

            public IDirectoryInfo CreateTempSubdirectory(string? prefix = null) => throw new NotImplementedException();

            public void Delete(string path) => throw new NotImplementedException();

            public void Delete(string path, bool recursive) => throw new NotImplementedException();

            public IEnumerable<string> EnumerateDirectories(string path) => this.EnumerateDirectories(path, "*");

            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => this.EnumerateDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);

            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption) => this.EnumerateFileSystemEntries(path, searchPattern, searchOption, files: false);

            public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();

            public IEnumerable<string> EnumerateFiles(string path) => this.EnumerateFiles(path, "*");

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => this.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => this.EnumerateFileSystemEntries(path, searchPattern, searchOption, directories: false);

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => throw new NotImplementedException();

            public IEnumerable<string> EnumerateFileSystemEntries(string path) => this.EnumerateFileSystemEntries(path, "*");

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) => this.EnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => this.EnumerateFileSystemEntries(path, searchPattern, searchOption, files: true, directories: true);

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => this.EnumerateDirectories(path, searchPattern, enumerationOptions).Concat(this.EnumerateFiles(path, searchPattern, enumerationOptions));

            private IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption, bool files = true, bool directories = true)
            {
                if (path != string.Empty)
                {
                    throw new DirectoryNotFoundException();
                }

                var glob = PathExtensions.GlobToRegex(searchPattern);
                if (files && glob.IsMatch(this.parent.FileName))
                {
                    yield return this.parent.FileName;
                }
            }

            public bool Exists([NotNullWhen(true)] string? path) => throw new NotImplementedException();

            public DateTime GetCreationTime(string path) => throw new NotImplementedException();

            public DateTime GetCreationTimeUtc(string path) => throw new NotImplementedException();

            public string GetCurrentDirectory() => throw new NotImplementedException();

            public string[] GetDirectories(string path) => [.. this.EnumerateDirectories(path)];

            public string[] GetDirectories(string path, string searchPattern) => [.. this.EnumerateDirectories(path, searchPattern)];

            public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) => [.. this.EnumerateDirectories(path, searchPattern, searchOption)];

            public string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => [.. this.EnumerateDirectories(path, searchPattern, enumerationOptions)];

            public string GetDirectoryRoot(string path) => throw new NotImplementedException();

            public string[] GetFiles(string path) => [.. this.EnumerateFiles(path)];

            public string[] GetFiles(string path, string searchPattern) => [.. this.EnumerateFiles(path, searchPattern)];

            public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [.. this.EnumerateFiles(path, searchPattern, searchOption)];

            public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => [.. this.EnumerateFiles(path, searchPattern, enumerationOptions)];

            public string[] GetFileSystemEntries(string path) => [.. this.EnumerateFileSystemEntries(path)];

            public string[] GetFileSystemEntries(string path, string searchPattern) => [.. this.EnumerateFileSystemEntries(path, searchPattern)];

            public string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => [.. this.EnumerateFileSystemEntries(path, searchPattern, searchOption)];

            public string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => [.. this.EnumerateFileSystemEntries(path, searchPattern, enumerationOptions)];

            public DateTime GetLastAccessTime(string path) => throw new NotImplementedException();

            public DateTime GetLastAccessTimeUtc(string path) => throw new NotImplementedException();

            public DateTime GetLastWriteTime(string path) => throw new NotImplementedException();

            public DateTime GetLastWriteTimeUtc(string path) => throw new NotImplementedException();

            public string[] GetLogicalDrives() => throw new NotImplementedException();

            public IDirectoryInfo? GetParent(string path) => throw new NotImplementedException();

            public void Move(string sourceDirName, string destDirName) => throw new NotImplementedException();

            public IFileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget) => throw new NotImplementedException();

            public void SetCreationTime(string path, DateTime creationTime) => throw new NotImplementedException();

            public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => throw new NotImplementedException();

            public void SetCurrentDirectory(string path) => throw new NotImplementedException();

            public void SetLastAccessTime(string path, DateTime lastAccessTime) => throw new NotImplementedException();

            public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => throw new NotImplementedException();

            public void SetLastWriteTime(string path, DateTime lastWriteTime) => throw new NotImplementedException();

            public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => throw new NotImplementedException();
        }

        private class FileProvider : IFile
        {
            private MVirtualFileSystem parent;

            public FileProvider(MVirtualFileSystem parent)
            {
                this.parent = parent;
            }

            public IFileSystem FileSystem => throw new NotImplementedException();

            public void AppendAllLines(string path, IEnumerable<string> contents) => throw new NotImplementedException();

            public void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding) => throw new NotImplementedException();

            public Task AppendAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public Task AppendAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public void AppendAllText(string path, string? contents) => throw new NotImplementedException();

            public void AppendAllText(string path, string? contents, Encoding encoding) => throw new NotImplementedException();

            public Task AppendAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public Task AppendAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public StreamWriter AppendText(string path) => throw new NotImplementedException();

            public void Copy(string sourceFileName, string destFileName) => throw new NotImplementedException();

            public void Copy(string sourceFileName, string destFileName, bool overwrite) => throw new NotImplementedException();

            public FileSystemStream Create(string path) => throw new NotImplementedException();

            public FileSystemStream Create(string path, int bufferSize) => throw new NotImplementedException();

            public FileSystemStream Create(string path, int bufferSize, FileOptions options) => throw new NotImplementedException();

            public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget) => throw new NotImplementedException();

            public StreamWriter CreateText(string path) => throw new NotImplementedException();

            public void Decrypt(string path) => throw new NotImplementedException();

            public void Delete(string path) => throw new NotImplementedException();

            public void Encrypt(string path) => throw new NotImplementedException();

            public bool Exists([NotNullWhen(true)] string? path) => string.Equals(path, this.parent.FileName, StringComparison.OrdinalIgnoreCase);

            public FileAttributes GetAttributes(string path) => throw new NotImplementedException();

            public FileAttributes GetAttributes(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetCreationTime(string path) => throw new NotImplementedException();

            public DateTime GetCreationTime(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetCreationTimeUtc(string path) => throw new NotImplementedException();

            public DateTime GetCreationTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetLastAccessTime(string path) => throw new NotImplementedException();

            public DateTime GetLastAccessTime(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetLastAccessTimeUtc(string path) => throw new NotImplementedException();

            public DateTime GetLastAccessTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetLastWriteTime(string path) => throw new NotImplementedException();

            public DateTime GetLastWriteTime(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public DateTime GetLastWriteTimeUtc(string path) => throw new NotImplementedException();

            public DateTime GetLastWriteTimeUtc(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public UnixFileMode GetUnixFileMode(string path) => throw new NotImplementedException();

            public UnixFileMode GetUnixFileMode(SafeFileHandle fileHandle) => throw new NotImplementedException();

            public void Move(string sourceFileName, string destFileName) => throw new NotImplementedException();

            public void Move(string sourceFileName, string destFileName, bool overwrite) => throw new NotImplementedException();

            public FileSystemStream Open(string path, FileMode mode) => throw new NotImplementedException();

            public FileSystemStream Open(string path, FileMode mode, FileAccess access) => throw new NotImplementedException();

            public FileSystemStream Open(string path, FileMode mode, FileAccess access, FileShare share) => throw new NotImplementedException();

            public FileSystemStream Open(string path, FileStreamOptions options) => throw new NotImplementedException();

            public FileSystemStream OpenRead(string path)
            {
                if (!this.Exists(path))
                {
                    throw new FileNotFoundException();
                }

                return new StreamWrapper(
                    ReadMArchive(this.parent.fileSystem.File.OpenRead(this.parent.filePath), this.parent.seed, this.parent.keyLength, out var decompressedLength) ?? throw new InvalidDataException("Not a valid M archive."),
                    path,
                    false);
            }

            public StreamReader OpenText(string path) => throw new NotImplementedException();

            public FileSystemStream OpenWrite(string path) => throw new NotImplementedException();

            public byte[] ReadAllBytes(string path) => throw new NotImplementedException();

            public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public string[] ReadAllLines(string path) => throw new NotImplementedException();

            public string[] ReadAllLines(string path, Encoding encoding) => throw new NotImplementedException();

            public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public Task<string[]> ReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public string ReadAllText(string path) => throw new NotImplementedException();

            public string ReadAllText(string path, Encoding encoding) => throw new NotImplementedException();

            public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public IEnumerable<string> ReadLines(string path) => throw new NotImplementedException();

            public IEnumerable<string> ReadLines(string path, Encoding encoding) => throw new NotImplementedException();

            public IAsyncEnumerable<string> ReadLinesAsync(string path, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public IAsyncEnumerable<string> ReadLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName) => throw new NotImplementedException();

            public void Replace(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors) => throw new NotImplementedException();

            public IFileSystemInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget) => throw new NotImplementedException();

            public void SetAttributes(string path, FileAttributes fileAttributes) => throw new NotImplementedException();

            public void SetAttributes(SafeFileHandle fileHandle, FileAttributes fileAttributes) => throw new NotImplementedException();

            public void SetCreationTime(string path, DateTime creationTime) => throw new NotImplementedException();

            public void SetCreationTime(SafeFileHandle fileHandle, DateTime creationTime) => throw new NotImplementedException();

            public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => throw new NotImplementedException();

            public void SetCreationTimeUtc(SafeFileHandle fileHandle, DateTime creationTimeUtc) => throw new NotImplementedException();

            public void SetLastAccessTime(string path, DateTime lastAccessTime) => throw new NotImplementedException();

            public void SetLastAccessTime(SafeFileHandle fileHandle, DateTime lastAccessTime) => throw new NotImplementedException();

            public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => throw new NotImplementedException();

            public void SetLastAccessTimeUtc(SafeFileHandle fileHandle, DateTime lastAccessTimeUtc) => throw new NotImplementedException();

            public void SetLastWriteTime(string path, DateTime lastWriteTime) => throw new NotImplementedException();

            public void SetLastWriteTime(SafeFileHandle fileHandle, DateTime lastWriteTime) => throw new NotImplementedException();

            public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => throw new NotImplementedException();

            public void SetLastWriteTimeUtc(SafeFileHandle fileHandle, DateTime lastWriteTimeUtc) => throw new NotImplementedException();

            public void SetUnixFileMode(string path, UnixFileMode mode) => throw new NotImplementedException();

            public void SetUnixFileMode(SafeFileHandle fileHandle, UnixFileMode mode) => throw new NotImplementedException();

            public void WriteAllBytes(string path, byte[] bytes) => throw new NotImplementedException();

            public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public void WriteAllLines(string path, string[] contents) => throw new NotImplementedException();

            public void WriteAllLines(string path, IEnumerable<string> contents) => throw new NotImplementedException();

            public void WriteAllLines(string path, string[] contents, Encoding encoding) => throw new NotImplementedException();

            public void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding) => throw new NotImplementedException();

            public Task WriteAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public void WriteAllText(string path, string? contents) => throw new NotImplementedException();

            public void WriteAllText(string path, string? contents, Encoding encoding) => throw new NotImplementedException();

            public Task WriteAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public Task WriteAllTextAsync(string path, string? contents, Encoding encoding, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private class PathProvider : IPath
        {
            private MVirtualFileSystem parent;

            public PathProvider(MVirtualFileSystem parent)
            {
                this.parent = parent;
            }

            public char AltDirectorySeparatorChar => throw new NotImplementedException();

            public char DirectorySeparatorChar => '/';

            public char PathSeparator => throw new NotImplementedException();

            public char VolumeSeparatorChar => throw new NotImplementedException();

            public IFileSystem FileSystem => throw new NotImplementedException();

            [return: NotNullIfNotNull("path")]
            public string? ChangeExtension(string? path, string? extension) => PathExtensions.ChangeExtension(path, extension);

            public string Combine(string path1, string path2) => this.CombineWithSeparator(this.DirectorySeparatorChar, path1, path2);

            public string Combine(string path1, string path2, string path3) => throw new NotImplementedException();

            public string Combine(string path1, string path2, string path3, string path4) => throw new NotImplementedException();

            public string Combine(params string[] paths) => throw new NotImplementedException();

            public bool EndsInDirectorySeparator(ReadOnlySpan<char> path) => throw new NotImplementedException();

            public bool EndsInDirectorySeparator(string path) => throw new NotImplementedException();

            public bool Exists([NotNullWhen(true)] string? path) => throw new NotImplementedException();

            public ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path) => throw new NotImplementedException();

            public string? GetDirectoryName(string? path) => PathExtensions.GetDirectoryName(path);

            public ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path) => throw new NotImplementedException();

            [return: NotNullIfNotNull("path")]
            public string? GetExtension(string? path) => PathExtensions.GetExtension(path);

            public ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path) => throw new NotImplementedException();

            [return: NotNullIfNotNull("path")]
            public string? GetFileName(string? path) => PathExtensions.GetFileName(path);

            public ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path) => throw new NotImplementedException();

            [return: NotNullIfNotNull("path")]
            public string? GetFileNameWithoutExtension(string? path) => PathExtensions.GetFileNameWithoutExtension(path);

            public string GetFullPath(string path) => throw new NotImplementedException();

            public string GetFullPath(string path, string basePath) => throw new NotImplementedException();

            public char[] GetInvalidFileNameChars() => throw new NotImplementedException();

            public char[] GetInvalidPathChars() => throw new NotImplementedException();

            public ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path) => throw new NotImplementedException();

            public string? GetPathRoot(string? path) => PathExtensions.GetPathRoot(path);

            public string GetRandomFileName() => throw new NotImplementedException();

            public string GetRelativePath(string relativeTo, string path) => this.GetRelativePath(this.DirectorySeparatorChar, relativeTo, path);

            public string GetTempFileName() => throw new NotImplementedException();

            public string GetTempPath() => throw new NotImplementedException();

            public bool HasExtension(ReadOnlySpan<char> path) => throw new NotImplementedException();

            public bool HasExtension([NotNullWhen(true)] string? path) => throw new NotImplementedException();

            public bool IsPathFullyQualified(ReadOnlySpan<char> path) => throw new NotImplementedException();

            public bool IsPathFullyQualified(string path) => throw new NotImplementedException();

            public bool IsPathRooted(ReadOnlySpan<char> path) => System.IO.Path.IsPathRooted(path);

            public bool IsPathRooted([NotNullWhen(true)] string? path) => System.IO.Path.IsPathRooted(path);

            public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2) => throw new NotImplementedException();

            public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3) => throw new NotImplementedException();

            public string Join(string? path1, string? path2) => throw new NotImplementedException();

            public string Join(string? path1, string? path2, string? path3) => throw new NotImplementedException();

            public string Join(params string?[] paths) => throw new NotImplementedException();

            public string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4) => throw new NotImplementedException();

            public string Join(string? path1, string? path2, string? path3, string? path4) => throw new NotImplementedException();

            public ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) => throw new NotImplementedException();

            public string TrimEndingDirectorySeparator(string path) => throw new NotImplementedException();

            public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten) => throw new NotImplementedException();

            public bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten) => throw new NotImplementedException();
        }
    }
}
