// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace Codec.UI
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ImageMagick;
    using Microsoft.Extensions.DependencyInjection;
    using Codec.Archives;
    using Entry = Codec.Archives.NestedFileSystemManager.Entry;
    using System.ComponentModel;
    using System.Media;
    using NAudio.Wave;
    using Codec.Files;

    internal partial class Browser : Form
    {
        private readonly IServiceProvider serviceProvider;
        private readonly NestedFileSystemManager fsm;
        private readonly List<FileHandlerResolver<Bitmap>> imageResolvers;
        private readonly VirtualImageList<Entry> textureDisplay;
        private readonly SoundPlayer soundPlayer;
        private bool suppressUpdates;

        public Browser(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.fsm = serviceProvider.GetRequiredService<NestedFileSystemManager>();
            this.imageResolvers = [.. serviceProvider.GetServices<FileHandlerResolver<Bitmap>>()];

            this.InitializeComponent();
            this.saveSelectedDialog.InitialDirectory = Environment.ExpandEnvironmentVariables(this.saveSelectedDialog.InitialDirectory);
            this.saveToFolderDialog.InitialDirectory = Environment.ExpandEnvironmentVariables(this.saveToFolderDialog.InitialDirectory);
            this.textureDisplay = new VirtualImageList<Entry>(
                entry =>
                {
                    this.fsm.TryFindParentFileSystem(entry.Path, out var subPath, out var fs, out var fsPath);
                    var resolver = this.imageResolvers.Select(f => f(this.serviceProvider, entry.Path, subPath, fs, fsPath)).FirstOrDefault(f => f is not null);
                    return Task.FromResult(resolver(entry.Path, subPath, fs, fsPath));
                },
                InterpolationMode.NearestNeighbor)
            {
                Dock = DockStyle.Fill,
                Visible = false,
            };
            this.splitContainer.Panel2.Controls.Add(this.textureDisplay);
            this.soundPlayer = new SoundPlayer();

            this.fileTree.Nodes.Add(new TreeNode("root", 0, 0, [this.CreateExpanderDummy()]) { Tag = this.fsm.RootEntry });
            this.Navigate(Path.Combine(serviceProvider.GetRequiredService<EnvironmentOptions>().SteamApps, WellKnownPaths.AllDataBin, WellKnownPaths.CD1Path, WellKnownPaths.StageDirPath));
        }

        private TreeNode CreateExpanderDummy() => new("...");

        private enum FileType
        {
            Folder = 0,
            File = 1,
            Archive = 2,
            Image = 3,
            Video = 4,
            Audio = 5,
        }

        private static FileType DetectFileType(Entry entry) =>
            entry.CanEnumerateEntries && !entry.CanOpen ? FileType.Folder :
            entry.CanEnumerateEntries ? FileType.Archive :
            Path.GetExtension(entry.Path).ToUpperInvariant() switch
            {
                ".BMP" => FileType.Image,
                ".GIF" => FileType.Image,
                ".TIF" => FileType.Image,
                ".TIFF" => FileType.Image,
                ".PCX" => FileType.Image,
                ".PNG" => FileType.Image,
                ".JPG" => FileType.Image,
                ".JPEG" => FileType.Image,
                ".WEBP" => FileType.Image,
                ".AVI" => FileType.Video,
                ".MOV" => FileType.Video,
                ".MP4" => FileType.Video,
                ".MKV" => FileType.Video,
                ".WEBM" => FileType.Video,
                ".MID" => FileType.Audio,
                ".MIDI" => FileType.Audio,
                ".MP3" => FileType.Audio,
                ".OGG" => FileType.Audio,
                ".WAV" => FileType.Audio,
                _ => FileType.File,
            };

        private void Navigate(string path)
        {
            if (this.fsm.TryGetEntry(path, out var entry))
            {
                this.Navigate(entry);
            }
        }

        private void Navigate(Entry entry)
        {
            this.suppressUpdates = true;
            this.pathBox.Tag = entry.Path;
            this.pathBox.Text = entry.Path;

            var currentNode = this.fileTree.Nodes[0];
            foreach (var segment in PathExtensions.SplitPath(entry.Path))
            {
                this.FileTree_BeforeExpand(this, new(currentNode, false, TreeViewAction.Unknown));
                currentNode.Expand();

                static string GetName(string path)
                {
                    var name = PathExtensions.GetFileName(path);
                    return string.IsNullOrEmpty(name) ? path : name;
                }

                var nextNode = currentNode.Nodes.Cast<TreeNode>().Where(n => n.Tag is Entry e && GetName(e.Path) == segment).FirstOrDefault();
                if (nextNode == null)
                {
                    break;
                }

                currentNode = nextNode;
            }

            this.fileTree.SelectedNode = currentNode;
            currentNode.EnsureVisible();

            if (this.fsm.TryFindParentFileSystem(entry.Path, out var subPath, out var fs, out var _))
            {
                var entries = this.fsm.EnumerateEntries(entry.Path);
                var items = entries
                    .Select(e => new ListViewItem(fs.Path.GetFileName(e.Path) switch { "" => e.Path, var x => x }, (int)DetectFileType(e)) { Tag = e })
                    .ToArray();
                this.entryList.Items.Clear();
                this.EntryList_SelectedIndexChanged(this.entryList, EventArgs.Empty);
                this.entryList.Items.AddRange(items);

                this.textureDisplay.Items = entries.Where(e => DetectFileType(e) == FileType.Image);
            }

            this.suppressUpdates = false;
        }

        private void FileTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node?.Tag is Entry entry && e.Node.Nodes is [TreeNode onlyChild] && onlyChild.Text == "..." &&
                this.fsm.TryFindParentFileSystem(entry.Path, out var subPath, out var fs, out var _))
            {
                e.Node.Nodes.Clear();
                var entries = this.fsm.EnumerateEntries(entry.Path).Where(e => e.CanEnumerateEntries);
                e.Node.Nodes.AddRange([.. entries.Select(e => new TreeNode(fs.Path.GetFileName(e.Path) switch { "" => e.Path, var x => x }, 0, 0, [this.CreateExpanderDummy()]) { Tag = e })]);
            }
        }

        private void FileTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!this.suppressUpdates && e.Node?.Tag is Entry entry)
            {
                this.Navigate(entry);
            }
        }

        private void PathBox_Validating(object sender, CancelEventArgs e)
        {
            if (!this.suppressUpdates && !this.pathBox.Text.Equals(this.pathBox.Tag))
            {
                this.Navigate(this.pathBox.Text);
            }
        }

        private void EntryList_ItemActivate(object sender, EventArgs e)
        {
            var item = this.entryList.SelectedItems.OfType<ListViewItem>().FirstOrDefault();
            if (item?.Tag is Entry entry)
            {
                if (entry.CanEnumerateEntries)
                {
                    this.Navigate(entry);
                }
                else if (this.fsm.TryFindParentFileSystem(entry.Path, out var subPath, out var fs, out var fsPath))
                {
                    switch (DetectFileType(entry))
                    {
                        case FileType.Image:
                            {
                                var resolver = this.imageResolvers.Select(f => f(this.serviceProvider, entry.Path, subPath, fs, fsPath)).FirstOrDefault(f => f is not null);
                                if (resolver != null)
                                {
                                    var childForm = new Form();
                                    childForm.Controls.Add(new PictureBox
                                    {
                                        Dock = DockStyle.Fill,
                                        SizeMode = PictureBoxSizeMode.Zoom,
                                        Image = resolver(entry.Path, subPath, fs, fsPath),
                                        BackColor = Color.Black,
                                    });
                                    childForm.Show(this);
                                }
                            }
                            break;
                        case FileType.Audio:
                            {
                                this.soundPlayer.Stop();
                                try
                                {
                                    using var file = fs.File.OpenRead(subPath);
                                    using var reader = new StreamMediaFoundationReader(file);
                                    var memoryStream = new MemoryStream();
                                    WaveFileWriter.WriteWavFileToStream(memoryStream, reader);
                                    memoryStream.Seek(0, SeekOrigin.Begin);
                                    this.soundPlayer.Stream = memoryStream;
                                    this.soundPlayer.Play();
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(this, $"Failed to play audio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            break;
                    }
                }
            }
        }

        private void ListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.entryList.View = View.List;
            this.entryList.Visible = true;
            this.textureDisplay.Visible = false;
            this.listToolStripMenuItem.Checked = true;
            this.smallIconsToolStripMenuItem.Checked = false;
            this.imagePreviewToolStripMenuItem.Checked = false;
        }

        private void SmallIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.entryList.View = View.SmallIcon;
            this.entryList.Visible = true;
            this.textureDisplay.Visible = false;
            this.listToolStripMenuItem.Checked = false;
            this.smallIconsToolStripMenuItem.Checked = true;
            this.imagePreviewToolStripMenuItem.Checked = false;
        }

        private void ImagePreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.entryList.Visible = false;
            this.textureDisplay.Visible = true;
            this.listToolStripMenuItem.Checked = false;
            this.smallIconsToolStripMenuItem.Checked = false;
            this.imagePreviewToolStripMenuItem.Checked = true;
        }

        private void EntryList_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.saveButton.Enabled = this.entryList.SelectedItems.Count >= 1 && this.entryList.SelectedItems.Cast<ListViewItem>().All(i => i.Tag is Entry entry && entry.CanOpen);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (this.entryList.SelectedItems.Count == 1)
            {
                var entry = (Entry)this.entryList.SelectedItems[0]?.Tag!;
                if (!this.fsm.TryFindParentFileSystem(entry.Path, out var subPath, out var fs, out var _))
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

                this.saveSelectedDialog.Filter = fileInfo != null
                    ? "Image Files|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.pcx|All Files|*.*"
                    : "All Files|*.*";

                this.saveSelectedDialog.FileName = Path.GetFileName(entry.Path);
                var result = this.saveSelectedDialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }

                if (fs != null)
                {
                    using var input = fs.File.OpenRead(subPath);
                    var path = this.saveSelectedDialog.FileName;
                    if (Path.GetExtension(path) != Path.GetExtension(subPath))
                    {
                        using var image = new MagickImage(input);
                        image.Write(path);
                    }
                    else
                    {
                        using var output = File.Create(path);
                        input.CopyTo(output);
                    }
                }
            }
            else if (this.entryList.SelectedItems.Count >= 0)
            {
                var entries = this.entryList.SelectedItems.Cast<ListViewItem>().Select(i => (Entry)i.Tag).ToList();

                this.saveToFolderDialog.SelectedPath = string.Empty;
                var result = this.saveToFolderDialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }

                var path = this.saveToFolderDialog.SelectedPath;
                var targetFiles = entries.Select(e => (Source: e.Path, Target: Path.Combine(path, Path.GetFileName(e.Path)))).ToList();
                if (targetFiles.Any(t => File.Exists(t.Target)))
                {
                    var overwriteResult = MessageBox.Show($"The destination path \"{path}\" already contians files with the same name. Do you want to overwrite?", "Confirm Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (overwriteResult != DialogResult.Yes)
                    {
                        return;
                    }
                }

                foreach (var (source, target) in targetFiles)
                {
                    if (this.fsm.TryFindParentFileSystem(source, out var subPath, out var fs, out var _))
                    {
                        using var input = fs.File.OpenRead(subPath);
                        using var output = File.Create(target);
                        input.CopyTo(output);
                    }
                }
            }
        }
    }
}
