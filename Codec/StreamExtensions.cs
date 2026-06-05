// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal static class StreamExtensions
    {
        public static short ReadInt16BigEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(short)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadInt16BigEndian(b);
        }

        public static ushort ReadUInt16BigEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(ushort)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadUInt16BigEndian(b);
        }

        public static int ReadInt32BigEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(int)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadInt32BigEndian(b);
        }

        public static uint ReadUInt32BigEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(uint)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadUInt32BigEndian(b);
        }

        public static long ReadInt64BigEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(long)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadInt64BigEndian(b);
        }

        public static ulong ReadUInt64BigEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(ulong)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadUInt64BigEndian(b);
        }

        public static float ReadSingleBigEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(float)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadSingleBigEndian(b);
        }

        public static double ReadDoubleBigEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(double)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadDoubleBigEndian(b);
        }

        public static short ReadInt16LittleEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(short)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadInt16LittleEndian(b);
        }

        public static ushort ReadUInt16LittleEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(ushort)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadUInt16LittleEndian(b);
        }

        public static int ReadInt32LittleEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(int)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadInt32LittleEndian(b);
        }

        public static uint ReadUInt32LittleEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(uint)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadUInt32LittleEndian(b);
        }

        public static long ReadInt64LittleEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(long)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadInt64LittleEndian(b);
        }

        public static ulong ReadUInt64LittleEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(ulong)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadUInt64LittleEndian(b);
        }

        public static float ReadSingleLittleEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(float)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadSingleLittleEndian(b);
        }

        public static double ReadDoubleLittleEndian(this Stream s)
        {
            Span<byte> b = stackalloc byte[sizeof(double)];
            s.ReadExactly(b);
            return BinaryPrimitives.ReadDoubleLittleEndian(b);
        }

        public static void ReadExactly(this Stream source, byte[] buffer, int count) => source.ReadExactly(buffer, 0, count);

        public static void CopyTo(this Stream source, Stream destination, long offset, SeekOrigin origin, long count)
        {
            source.Seek(offset, origin);
            int read;
            var buffer = new byte[81920];
            while (count > 0 && (read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, count))) > 0)
            {
                destination.Write(buffer, 0, read);
                count -= read;
            }
        }

        public static bool Contains(this Stream source, byte[] pattern)
        {
            using var memory = new MemoryStream();
            source.CopyTo(memory);
            var subject = memory.ToArray();
            var l = subject.LongLength;
            for (var i = 0L; i < l; i++)
            {
                var found = true;
                for (var j = 0; found && j < pattern.Length && (i + j) < l; j++)
                {
                    if (subject[i + j] != pattern[j])
                    {
                        found = false;
                    }
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        public static T ReadBigEndian<T>(this Stream stream)
            where T : struct =>
            ReadWithEndianness<T>(stream, swapEndianness: BitConverter.IsLittleEndian);

        public static T ReadLittleEndian<T>(this Stream stream)
            where T : struct =>
            ReadWithEndianness<T>(stream, swapEndianness: !BitConverter.IsLittleEndian);

        public static T ReadSytemEndianness<T>(this Stream stream)
            where T : struct =>
            ReadWithEndianness<T>(stream, false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ReadWithEndianness<T>(this Stream stream, bool swapEndianness)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var buffer = size < 64 ? stackalloc byte[size] : new byte[size].AsSpan();
            stream.ReadExactly(buffer);
            if (swapEndianness)
            {
                SwapFields(buffer, typeof(T));
            }

            return MemoryMarshal.Cast<byte, T>(buffer)[0];
        }

        private static void SwapFields(Span<byte> buffer, Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var offset = (int)Marshal.OffsetOf(type, field.Name);
                var fieldType = field.FieldType;

                if (fieldType.IsValueType && !fieldType.IsPrimitive && !fieldType.IsEnum)
                {
                    SwapFields(buffer.Slice(offset, Marshal.SizeOf(fieldType)), fieldType);
                }
                else
                {
                    var fieldSize = Marshal.SizeOf(fieldType);
                    if (fieldSize > 1)
                    {
                        buffer.Slice(offset, fieldSize).Reverse();
                    }
                }
            }
        }
    }
}
