namespace Codec.Services
{
    using System.IO.Abstractions;
    using Codec.Archives;
    using Entry = Codec.Archives.NestedFileSystemManager.Entry;

    public sealed class EntryTypeDetector(NestedFileSystemManager fsm)
    {
        public EntryType Detect(Entry entry)
        {
            if (entry.CanEnumerateEntries && !entry.CanOpen)
            {
                return EntryType.Folder;
            }

            if (entry.CanEnumerateEntries)
            {
                return EntryType.Archive;
            }

            if (!fsm.TryFindParentFileSystem(entry.Path, out var subPath, out var fs, out var _))
            {
                return default;
            }

            // TODO: Run through our collection of FileHandlerResolvers here.

            return fs.Path.GetExtension(subPath).ToUpperInvariant() switch
            {
                ".BMP" or
                ".CTXR" or
                ".GIF" or
                ".TIF" or ".TIFF" or
                ".TM2" or
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

        public enum EntryType
        {
            Folder = 0,
            File = 1,
            Archive = 2,
            Image = 3,
            Video = 4,
            Audio = 5,
        }
    }
}
