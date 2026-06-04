namespace Codec.Users.GamingWithPortals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


    // GamingWithPortals "MGS2AudioTool"


    public static class MGS2_SDT
    {
        public static Dictionary<uint, string> SDTExtensionMap = new Dictionary<uint, string>()
        {
            { 0x00000001, ".genh" },
            { 0x00000002, ".dmx" },
            { 0x00000003, ".nrm" },
            { 0x00000004, ".pacb" },
            { 0x00000005, ".dmx" },
            { 0x00000006, ".bpx" },
            { 0x0000000c, ".pac" },
            { 0x0000000d, ".pac" },
            { 0x0000000e, ".pss" },
            { 0x0000000f, ".ipu" },
            { 0x00000020, ".m2v" },
            { 0x00010001, ".sdx_1" },
            { 0x00010004, ".sub_en" },
            { 0x00020001, ".sdx_2" },
            { 0x00020004, ".sub_fr" },
            { 0x00030001, ".msf" },
            { 0x00030004, ".sub_de" },
            { 0x00040001, ".xwma" },
            { 0x00040004, ".sub_it" },
            { 0x00050001, ".9tav" },
            { 0x00050004, ".sub_es" },
            { 0x00060004, ".sub_jp" },
            { 0x00070004, ".sub_jp" },
            { 0x00100001, ".vag" },
            { 0x00110001, ".mtaf" }

        };

        public struct SDTChunk
        {
            public uint resourceID;
            public uint size;
            public uint u_8;
            public uint streamID;

            public SDTChunk()
            {
            }

            public void Read(BinaryReader r)
            {
                resourceID = r.ReadUInt32();
                size = r.ReadUInt32();
                u_8 = r.ReadUInt32();
                streamID = r.ReadUInt32();
            }

        };

        public struct SDTStream
        {
            public List<long> positions;
            public List<long> sizes;

            public SDTStream()
            {
                positions = new List<long>();
                sizes = new List<long>();
            }
        };

        public static void FixupXWMAHeader(SDTChunk headerChunk, MemoryStream stream)
        {
            byte[] xwmaData = stream.ToArray();
            stream.SetLength(0);

            using var reader = new BinaryReader(new MemoryStream(xwmaData));
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            uint magic = reader.ReadUInt32();
            int codec = reader.ReadInt32();
            int channels = reader.ReadInt32();
            int sampleRate = reader.ReadInt32();
            uint dataSize = reader.ReadUInt32();
            int avgBps = reader.ReadInt32();
            int blockSize = reader.ReadInt32();
            reader.BaseStream.Seek(0x32, SeekOrigin.Begin);
            int seekEntryCount = reader.ReadInt16();

            reader.ReadBytes(4 * seekEntryCount);
            long pos = reader.BaseStream.Position;
            if (pos % 0x10 != 0)
                reader.BaseStream.Seek(pos + 0x10 - (pos % 0x10), SeekOrigin.Begin);


            byte[] audioData = reader.ReadBytes((int)dataSize);

            const int headerSize = 0x2e;
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write((uint)(headerSize + audioData.Length - 8));
            writer.Write(Encoding.ASCII.GetBytes("XWMAfmt "));
            writer.Write((uint)0x12);
            writer.Write((short)codec);
            writer.Write((short)channels);
            writer.Write((uint)sampleRate);
            writer.Write((uint)avgBps);
            writer.Write((short)blockSize);
            writer.Write((short)16);
            writer.Write((short)0);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write((uint)audioData.Length);
            writer.Write(audioData);

        }



    }
}
