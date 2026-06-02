namespace Codec.Avalonia.Models
{
    using Codec.Archives;

    public sealed record EntryItem(
        NestedFileSystemManager.Entry Entry,
        string DisplayName,
        EntryType EntryType);
}
