// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec
{
    using System;
    using Codec.Archives;
    using Codec.Files;
    using DiscUtils.Iso9660;
    using Microsoft.Extensions.DependencyInjection;

    public class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            CtxrFile.Register(services);
            TriFile.Register(services);
            ImageMagickBitmapResolver.Register(services);

            BrfDatVirtualFileSystem.Register(services);
            StageDirVirtualFileSystem.Register(services);
            PsbVirtualFileSystem.Register(services);
            MVirtualFileSystem.Register(services);
            MArchiveV1VirtualFileSystem.Register(services);
            SlotVirtualFileSystem.Register(services);
            services.AddSingleton<FileSystemResolver>((serviceProvider, fullPath, parentRelativePath, parent, parentPath) =>
            {
                if (parent is MArchiveV1VirtualFileSystem &&
                    string.Equals(parent.Path.GetExtension(parentRelativePath), ".bin", StringComparison.OrdinalIgnoreCase) &&
                    parent.Path.GetFileName(parent.Path.GetDirectoryName(parentRelativePath)) == "roms")
                {
                    return static (fullPath, parentRelativePath, parent, parentPath) =>
                    {
                        var file = parent.File.OpenRead(parentRelativePath);
                        var cdSector = new CDSectorStream(file, CDSectorStream.XAForm1);
                        var cdReader = new CDReader(cdSector, joliet: false);
                        return new CDReaderVFSAdapter(cdReader);
                    };
                }

                return null;
            });
        }
    }
}
