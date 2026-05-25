// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec
{
    using System;
    using System.IO.Abstractions;
    using Codec.Archives;
    using DiscUtils.Iso9660;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            BrfDatVirtualFileSystem.Register(services);
            StageDirVirtualFileSystem.Register(services);
            MArchiveV1VirtualFileSystem.Register(services);
            services.AddSingleton<FileSystemResolver>((s, file, fs, fsPath) =>
            {
                if (fs is MArchiveV1VirtualFileSystem &&
                    string.Equals(fs.Path.GetExtension(file), ".bin", StringComparison.OrdinalIgnoreCase) &&
                    fs.Path.GetFileName(fs.Path.GetDirectoryName(file)) == "roms")
                {
                    return static (fs, subPath) =>
                    {
                        var file = fs.File.OpenRead(subPath);
                        var cdSector = new CDSectorStream(file, CDSectorStream.XAForm1);
                        var cdReader = new CDReader(cdSector, joliet: false);
                        var subFs = new CDReaderVFSAdapter(cdReader);
                        return subFs;
                    };
                }

                return null;
            });
        }
    }
}
