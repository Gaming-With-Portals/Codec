namespace Codec.Users.GamingWithPortals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Buffers.Binary;
    using System.Reactive;

    public static class MGS2_SDX
    {
        static readonly double[] PosTable = [0.0, 60.0 / 64, 115.0 / 64, 98.0 / 64, 122.0 / 64];
        static readonly double[] NegTable = [0.0, 0.0, -52.0 / 64, -55.0 / 64, -60.0 / 64];

        public static short[] DecodeSpuAdpcm(byte[] data) // from MGS2 SDX Tool by Gaming With Portals, originally from someone else but i forgor
        {
            var samples = new List<short>();
            double hist1 = 0.0, hist2 = 0.0;

            int numBlocks = data.Length / 16;

            for (int blockIdx = 0; blockIdx < numBlocks; blockIdx++)
            {
                int blockOffset = blockIdx * 16;
                byte shiftFilter = data[blockOffset];
                byte flags = data[blockOffset + 1];

                int shift = shiftFilter & 0x0F;
                int filterIdx = (shiftFilter >> 4) & 0x0F;
                if (filterIdx > 4) filterIdx = 0;

                double pos = PosTable[filterIdx];
                double neg = NegTable[filterIdx];

                for (int i = blockOffset + 2; i < blockOffset + 16; i++)
                {
                    byte b = data[i];
                    foreach (int nibbleShift in new[] { 0, 4 })
                    {
                        int nibble = (b >> nibbleShift) & 0x0F;
                        if (nibble >= 8) nibble -= 16;

                        double raw = nibble * (1 << (12 - shift));
                        double sample = raw + pos * hist1 + neg * hist2;
                        hist2 = hist1;
                        hist1 = sample;

                        samples.Add((short)Math.Clamp((int)sample, -32768, 32767));
                    }
                }

                if ((flags & 0x01) != 0) break;
            }

            return samples.ToArray();
        }

        public struct NoteParameters
        {
            public uint addrLe;
            public byte sampleNote;
            public byte sampleTune;
            public byte attackMode;
            public byte attackRate;
            public byte decayRate;
            public byte sustainMode;
            public byte sustainRate;
            public byte sustainLevel;
            public byte releaseMode;
            public byte releaseRate;
            public byte pan;
            public byte decVolume;

            public NoteParameters(BinaryReader r)
            {
                addrLe = r.ReadUInt32();
                sampleNote = r.ReadByte();
                sampleTune = r.ReadByte();
                attackMode = r.ReadByte();
                attackRate = r.ReadByte();
                decayRate = r.ReadByte();
                sustainMode = r.ReadByte();
                sustainRate = r.ReadByte();
                sustainLevel = r.ReadByte();
                releaseMode = r.ReadByte();
                releaseRate = r.ReadByte();
                pan = r.ReadByte();
                decVolume = r.ReadByte();
            }


        }

        public struct SpuData
        {
            public uint spuOffset;
            public uint spuSize;
            public uint dataStart;

            public SpuData(BinaryReader r)
            {
                spuOffset = BinaryPrimitives.ReadUInt32BigEndian(r.ReadBytes(4));
                spuSize = BinaryPrimitives.ReadUInt32BigEndian(r.ReadBytes(4));
                r.ReadBytes(8);
                dataStart = (uint)r.BaseStream.Position;
            }

            public byte[] GetAudioData(BinaryReader r, uint offset)
            {
                r.BaseStream.Seek(dataStart+(offset-spuOffset)+0x10, SeekOrigin.Begin);
                MemoryStream memstream = new MemoryStream();

                while (true) {
                    byte[] frame = r.ReadBytes(0x10);

                    if (frame.Length < 0x10 || frame.All(b => b == 0))
                        break;

                    memstream.Write(frame, 0, frame.Length);

                }


                return memstream.ToArray();
            }



        }

    }
}
