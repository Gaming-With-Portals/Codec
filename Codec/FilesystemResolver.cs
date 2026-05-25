namespace Codec
{
    using System;
    using System.IO.Abstractions;

    public delegate IFileSystem FileSystemCreator(IFileSystem fs, string subPath);

    public delegate FileSystemCreator? FileSystemResolver(IServiceProvider s, string file, IFileSystem fs, string fsPath);
}
