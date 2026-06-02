namespace Codec.Avalonia.Services
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Avalonia.Controls;
    using global::Avalonia.Platform.Storage;
    using Codec.Archives;
    using Codec.Avalonia.Models;
    using Codec.Avalonia.Views;
    using Codec.Files;
    using ImageMagick;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class FileSaveService(NestedFileSystemManager fsm, IServiceProvider serviceProvider)
    {
        private List<FileHandlerResolver<Bitmap>>? imageResolvers;

        public List<FileHandlerResolver<Bitmap>>? ImageResolvers => this.imageResolvers ??= [.. serviceProvider.GetServices<FileHandlerResolver<Bitmap>>()];

        public async Task SaveSingleAsync(Window owner, EntryItem item)
        {
            var entry = item.Entry;
            if (!fsm.TryFindParentFileSystem(entry.Path, out var subPath, out var fs, out var fsPath))
            {
                return;
            }

            MagickImageInfo? fileInfo = null;
            try
            {
                using var input = fs.File.OpenRead(subPath);
                fileInfo = new MagickImageInfo(input);
            }
            catch (MagickMissingDelegateErrorException)
            {
            }

            var allFiles = new FilePickerFileType("All Files") { Patterns = ["*.*"] };
            var options = new FilePickerSaveOptions
            {
                Title = "Save File",
                SuggestedFileName = fs.Path.GetFileName(subPath),
                FileTypeChoices = fileInfo != null
                    ? [new FilePickerFileType("Image Files") { Patterns = ["*.bmp", "*.gif", "*.jpg", "*.jpeg", "*.png", "*.tif", "*.tiff", "*.pcx"] }, allFiles]
                    : [allFiles],
            };

            var file = await owner.StorageProvider.SaveFilePickerAsync(options).ConfigureAwait(false);
            if (file is null)
            {
                return;
            }

            if (fs != null)
            {
                using var input = fs.File.OpenRead(subPath);
                var path = file.Path.LocalPath;
                var resolver = this.ImageResolvers.Select(f => f(serviceProvider, entry.Path, subPath, fs, fsPath)).FirstOrDefault(f => f is not null);
                if (Path.GetExtension(path) != Path.GetExtension(subPath) && resolver != null)
                {
                    resolver(entry.Path, subPath, fs, fsPath).Save(path);
                }
                else
                {
                    using var output = File.Create(path);
                    input.CopyTo(output);
                }
            }
        }

        public async Task SaveMultipleAsync(Window owner, IEnumerable<EntryItem> selectedItems)
        {
            var entries = selectedItems.Select(e => e.Entry);

            var options = new FolderPickerOpenOptions
            {
                Title = "Save to Folder",
            };
            var folders = await owner.StorageProvider.OpenFolderPickerAsync(options).ConfigureAwait(true);
            if (folders is not [var folder])
            {
                return;
            }

            var path = folder.Path.LocalPath;
            var targetFiles = entries.Select(e => (Source: e.Path, Target: Path.Combine(path, Path.GetFileName(e.Path)))).ToList();
            if (targetFiles.Any(t => File.Exists(t.Target)))
            {
                var confirmed = await ConfirmOverwriteAsync(owner).ConfigureAwait(false);
                if (!confirmed)
                {
                    return;
                }
            }

            foreach (var (source, target) in targetFiles)
            {
                if (fsm.TryFindParentFileSystem(source, out var subPath, out var fs, out var _))
                {
                    using var input = fs.File.OpenRead(subPath);
                    using var output = File.Create(target);
                    await input.CopyToAsync(output).ConfigureAwait(false);
                }
            }
        }

        private static async Task<bool> ConfirmOverwriteAsync(Window owner) =>
            await new ConfirmOverwriteDialog().ShowDialog<bool?>(owner).ConfigureAwait(false) == true;
    }
}
