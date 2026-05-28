// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec.Files
{
    using System;
    using System.Buffers.Binary;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.DependencyInjection;

    internal class CtxrFile
    {
        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<FileHandlerResolver<Bitmap>>((serviceProvider, fullPath, parentRelativePath, parent, parentPath) =>
            {
                if (string.Equals(parent.Path.GetExtension(parentRelativePath), ".ctxr", StringComparison.OrdinalIgnoreCase))
                {
                    return (fullPath, parentRelativePath, parent, parentPath) =>
                    {
                        using var input = parent.File.OpenRead(parentRelativePath);
                        return Load(input);
                    };
                }

                return null;
            });
        }

        public static Bitmap Load(string path)
        {
            using var stream = File.OpenRead(path);
            return Load(stream);
        }

        public static Bitmap Load(Stream stream)
        {
            var header = Header.Read(stream);
            var bitmap = new Bitmap(header.Width, header.Height);

            BitmapData? bmpData = null;
            try
            {
                bmpData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                var buffer = new byte[bmpData.Width * 4];
                var scan = bmpData.Scan0;
                for (var y = 0; y < bmpData.Height; y++, scan += bmpData.Stride)
                {
                    stream.ReadExactly(buffer, 0, buffer.Length);
                    for (var x = 0; x < bmpData.Width; x++)
                    {
                        var ix = x * 4;
                        (buffer[ix], buffer[ix + 1], buffer[ix + 2], buffer[ix + 3]) = (buffer[ix + 1], buffer[ix + 2], buffer[ix + 3], (byte)Math.Clamp(buffer[ix] * 2, 0, 255));
                    }

                    Marshal.Copy(buffer, 0, scan, buffer.Length);
                }
            }
            finally
            {
                if (bmpData is not null)
                {
                    bitmap.UnlockBits(bmpData);
                }
            }

            return bitmap;
        }

        private struct Header
        {
            public ushort Width;
            public ushort Height;
            public ushort Depth;
            public byte MipMapsCount;

            public Header()
            {
            }

            public static Header Read(Stream stream)
            {
                var buffer = new byte[127];
                stream.ReadExactly(buffer);

                var signature = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan()[0..4]);
                if (signature != 0x54585452)
                {
                    throw new FormatException();
                }

                var version = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan()[4..8]);
                if (version != 7)
                {
                    throw new FormatException();
                }

                var width = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan()[8..10]);
                var height = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan()[10..12]);
                var depth = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan()[12..14]);

                var mipMapCount = buffer[38];

                return new Header
                {
                    Width = width,
                    Height = height,
                    Depth = depth,
                    MipMapsCount = mipMapCount,
                };
            }
        }
    }
}
