using static Codec.Users.GamingWithPortals.MGS2_SDX;

namespace Codec.Archives
{
    using System;
    using System.Buffers.Binary;
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
    using NAudio.Utils;
    using NAudio.Wave;
    using Newtonsoft.Json;

    
    using Entry = (string FileName, NoteParameters data, uint spuID);

    public sealed partial class SdxVirtualFileSystem : IndexedFileSystem<Entry>
    {
        private string parentRelativePath;
        private IFileSystem parent;

        List<SpuData> soundDatas = new List<SpuData>();


        public SdxVirtualFileSystem(string parentRelativePath, IFileSystem parent)
        {
            this.parentRelativePath = parentRelativePath;
            this.parent = parent;
        }


        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<FileSystemResolver>((serviceProvider, fullPath, parentRelativePath, parent, parentPath) =>
            {
                if (string.Equals(parent.Path.GetExtension(parentRelativePath), ".sdx", StringComparison.OrdinalIgnoreCase))
                {
                    return static (fullPath, parentRelativePath, parent, parentPath) =>
                    {

                        var file = parent.File.OpenRead(parentRelativePath);
                        return new SdxVirtualFileSystem(parentRelativePath, parent);
                    };
                }

                return null;
            });
        }

        static uint Align(uint offset, uint alignment)
        {
            return offset % alignment == 0 ? offset : offset + (alignment - (offset % alignment));
        }



        protected override IEnumerable<Entry> ReadIndex()
        {
            var result = new List<Entry>();
            using var stream = this.parent.File.OpenRead(this.parentRelativePath);
            using var reader = new BinaryReader(stream);

            reader.BaseStream.Seek(0x4, SeekOrigin.Begin);

            bool disableSE2 = false;

            if (reader.ReadUInt32() == 95)
            {
                reader.ReadBytes(4);
                if (reader.ReadUInt32() == 255)
                {
                    disableSE2 = true; // this is a hack
                }
            }



            reader.BaseStream.Seek(2048, SeekOrigin.Begin);

            uint bound = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
            uint size = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
            reader.ReadBytes(8);

            // SE 1
            for (uint i = 0; i < (size / 0x10); i++)
            {
                result.Add(("0_" + i.ToString() + ".wav", new NoteParameters(reader), 0));
            }

            soundDatas.Add(new SpuData(reader));

            if (!disableSE2)
            {
                // is it really a hack if there isn't a better way?
                // thanks bluepoint, i hate this
                reader.BaseStream.Seek(Align(soundDatas[0].dataStart + soundDatas[0].spuSize, bound), SeekOrigin.Begin);


                bound = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
                size = BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));


                reader.ReadBytes(8);

                if (size % 10 != 0 && size < 1000000)
                {
                    for (uint i = 0; i < (size / 0x10); i++)
                    {
                        result.Add(("1_" + i.ToString() + ".wav", new NoteParameters(reader), 1));
                    }

                    soundDatas.Add(new SpuData(reader));
                }


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



            SpuData spu = soundDatas[(int)entry.spuID];

            byte[] adpcm = spu.GetAudioData(reader, entry.data.addrLe);

            short[] pcmSamples = DecodeSpuAdpcm(adpcm);

            var ms = new MemoryStream();
            var format = new WaveFormat(22050, 16, 1); // 16-bit mono
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), format))
            {
                writer.WriteSamples(pcmSamples, 0, pcmSamples.Length);
                writer.Flush();
            };



            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        [GeneratedRegex("^_stream\\.(\\d+)$")]
        private static partial Regex StreamIndexExtractor();
    }
}
