namespace Codec.Avalonia.Converters
{
    using System;
    using System.Globalization;
    using global::Avalonia.Data.Converters;
    using Codec.Avalonia.Models;
    using IconPacks.Avalonia.FontAwesome;

    public sealed class FileIconEntryConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value switch
            {
                EntryType.Folder => PackIconFontAwesomeKind.FolderOpenSolid,
                EntryType.Archive => PackIconFontAwesomeKind.BoxArchiveSolid,
                EntryType.Image => PackIconFontAwesomeKind.FileImageSolid,
                EntryType.Audio => PackIconFontAwesomeKind.FileAudioSolid,
                EntryType.Video => PackIconFontAwesomeKind.FileVideoSolid,
                _ => PackIconFontAwesomeKind.FileSolid,
            };

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
