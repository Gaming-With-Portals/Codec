namespace Codec.Avalonia.Services
{
    using System.IO.Abstractions;
    using Codec.Avalonia.Models;
    using Entry = Codec.Archives.NestedFileSystemManager.Entry;

    public sealed class EntryTypeDetector
    {
        public EntryType Detect(IFileSystem fs, Entry entry)
        {
            if (entry.CanEnumerateEntries && !entry.CanOpen)
            {
                return EntryType.Folder;
            }

            if (entry.CanEnumerateEntries)
            {
                return EntryType.Archive;
            }

            return fs.Path.GetExtension(entry.Path).ToUpperInvariant() switch
            {
                ".BMP" or
                ".CTXR" or
                ".GIF" or
                ".TIF" or ".TIFF" or
                ".PCX" or
                ".PNG" or
                ".JPG" or ".JPEG" or
                ".WEBP" => EntryType.Image,

                ".AVI" or
                ".MOV" or
                ".MP4" or
                ".MKV" or
                ".WEBM" => EntryType.Video,

                ".MID" or ".MIDI" or
                ".MP3" or
                ".OGG" or
                ".WAV" => EntryType.Audio,

                _ => EntryType.File,
            };
        }
    }
}
