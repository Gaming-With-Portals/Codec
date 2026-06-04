namespace Codec.Archives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Codec.Users.GamingWithPortals;
    using CueSharp;
    using GMWare.M2.Psb;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using static Codec.Users.GamingWithPortals.MGS2_SDT;
    using NAudio.Wave;
    using Entry = (string FileName, uint type, Codec.Users.GamingWithPortals.MGS2_SDT.SDTChunk);

    public sealed partial class SdtVirtualFileSystem : IndexedFileSystem<Entry>
    {
        private Dictionary<uint, SDTStream> streamDatas = new Dictionary<uint, SDTStream>();
        private string parentRelativePath;
        private IFileSystem parent;

        public SdtVirtualFileSystem(string parentRelativePath, IFileSystem parent)
        {
            this.parentRelativePath = parentRelativePath;
            this.parent = parent;
        }
 

        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<FileSystemResolver>((serviceProvider, fullPath, parentRelativePath, parent, parentPath) =>
            {
                if (string.Equals(parent.Path.GetExtension(parentRelativePath), ".sdt", StringComparison.OrdinalIgnoreCase))
                {
                    return static (fullPath, parentRelativePath, parent, parentPath) =>
                    {

                        var file = parent.File.OpenRead(parentRelativePath);
                        return new SdtVirtualFileSystem(parentRelativePath, parent);
                    };
                }

                return null;
            });
        }

        protected override IEnumerable<Entry> ReadIndex() {
            var result = new List<Entry>();
            using var stream = this.parent.File.OpenRead(this.parentRelativePath);
            using var reader = new BinaryReader(stream);

            reader.BaseStream.Seek(0, SeekOrigin.End);
            long fileSize = reader.BaseStream.Position;
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            int index = 0;
            while (reader.BaseStream.Position < fileSize)
            {
                long start = reader.BaseStream.Position;
                SDTChunk chunk = new SDTChunk();
                chunk.Read(reader);

                if (chunk.resourceID == 0x10) // Indicate New Resource
                {
                    if (SDTExtensionMap.ContainsKey(chunk.streamID))
                    {
                        result.Add(new Entry(index.ToString() + SDTExtensionMap[chunk.streamID], chunk.streamID, chunk));
                    }
                    else
                    {
                        result.Add(new Entry(index.ToString() + ".bin", chunk.streamID, chunk));
                    }
                    index++;

                    streamDatas[chunk.streamID] = new SDTStream();
                }
                else if (chunk.resourceID == 0xF0) // NO-OP Chunk
                {
                    continue;
                }
                else
                {
                    uint readSize = chunk.size - 0x10;
                    uint dataSize = readSize;

                    if (chunk.resourceID == 0x00040001)
                    {
                        if (chunk.streamID != 0)
                        {
                            dataSize = chunk.streamID;
                        }

                    }

                    streamDatas[chunk.resourceID].positions.Add(start + 0x10);
                    streamDatas[chunk.resourceID].sizes.Add(dataSize);
                }


                reader.BaseStream.Seek(start + chunk.size, SeekOrigin.Begin);
            }




            return result;
        }

        protected override string GetEntryName(Entry entry) =>
            entry.FileName;


        protected override Stream OpenRead(Entry entry)
        {
            using var stream = this.parent.File.OpenRead(this.parentRelativePath);
            using var reader = new BinaryReader(stream);
            reader.BaseStream.Seek(0, 0);

            var memStream = new MemoryStream();

            // Demux
            for (int i = 0; i < streamDatas[entry.Item3.streamID].positions.Count; i++)
            {
                reader.BaseStream.Seek(streamDatas[entry.Item3.streamID].positions[i], 0);
                memStream.Write(reader.ReadBytes((int)streamDatas[entry.Item3.streamID].sizes[i]));
            }

            if (entry.Item3.streamID == 0x00040001)
            {


                FixupXWMAHeader(entry.Item3, memStream);

            }

            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
        }

        [GeneratedRegex("^_stream\\.(\\d+)$")]
        private static partial Regex StreamIndexExtractor();
    }
}
