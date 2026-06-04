namespace Codec.Archives
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;

    public class NestedFileSystemManager
    {
        private readonly Dictionary<string, FileSystemFactory?> nestedFactories = new();
        private readonly Dictionary<string, IFileSystem> fileSystems = new();
        private readonly FileSystemHandler[] handlers;

        public NestedFileSystemManager(IFileSystem fs, params FileSystemHandler[] handlers)
        {
            this.handlers = handlers;
            this.fileSystems.Add(string.Empty, fs);
            this.RootEntry = new(string.Empty, false, false);
        }

        public Entry RootEntry { get; }

        public bool TryFindParentFileSystem(string path, out string parentRelativePath, [NotNullWhen(true)] out IFileSystem? parent, [NotNullWhen(true)] out string? parentPath)
        {
            var found = this.fileSystems.TryGetValue(path, out parent);
            if (found && parent != null)
            {
                parentPath = path;
                parentRelativePath = string.Empty;
                return true;
            }

            if (PathExtensions.GetDirectoryName(path) is string directoryName && this.TryFindParentFileSystem(directoryName, out var relativePath, out parent, out parentPath))
            {
                parentRelativePath = parent.Path.Combine(relativePath, directoryName == string.Empty ? path : parent.Path.GetRelativePath(directoryName, path));
                if (!found && this.GetOrAddFactory(path, parentRelativePath, parent, parentPath, out var factory))
                {
                    if (parent.File.Exists(parentRelativePath))
                    {
                        var newParent = factory(path, parentRelativePath, parent, parentPath);
                        this.fileSystems.Add(path, newParent);
                        this.nestedFactories.Remove(path);
                        if (newParent != null)
                        {
                            parent = newParent;
                            parentPath = path;
                            parentRelativePath = string.Empty;
                        }
                    }
                }

                return true;
            }

            parent = null;
            parentPath = null;
            parentRelativePath = path;
            return false;
        }

        public IEnumerable<Entry> EnumerateEntries(string path, bool recursive = false)
        {
            var stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                foreach (var entry in this.EnumerateEntries(stack.Pop()))
                {
                    yield return entry;

                    if (recursive && entry.CanEnumerateEntries)
                    {
                        stack.Push(entry.Path);
                    }
                }
            }
        }

        public IEnumerable<Entry> EnumerateFiles(string path, string searchPattern, bool recursive = false)
        {
            var glob = PathExtensions.GlobToRegex(searchPattern);
            foreach (var entry in this.EnumerateEntries(path, recursive))
            {
                if (!entry.CanOpen || !glob.IsMatch(Path.GetFileName(entry.Path)))
                {
                    continue;
                }

                yield return entry;
            }
        }

        public bool FileExists(string path)
        {
            if (!this.TryFindParentFileSystem(path, out var parentRelativePath, out var parent, out _))
            {
                throw new FileNotFoundException(path);
            }

            return parent.File.Exists(parentRelativePath);
        }

        public Stream OpenRead(string path)
        {
            if (!this.TryFindParentFileSystem(path, out var parentRelativePath, out var parent, out _))
            {
                throw new FileNotFoundException(path);
            }

            return parent.File.OpenRead(parentRelativePath);
        }

        private IEnumerable<Entry> EnumerateEntries(string path)
        {
            if (this.TryFindParentFileSystem(path, out var parentRelativePath, out var parent, out var parentPath))
            {
                if (string.IsNullOrEmpty(parentRelativePath) || parent.Directory.Exists(parentRelativePath))
                {
                    foreach (var d in parent.Directory.EnumerateDirectories(parentRelativePath))
                    {
                        yield return new(parent.Path.CombineIgnoringAbsolute(parentPath, d), false, true);
                    }

                    foreach (var f in parent.Directory.EnumerateFiles(parentRelativePath))
                    {
                        var p = parent.Path.CombineIgnoringAbsolute(parentPath, f);
                        yield return new(p, true, this.IsNestedFileSystem(p, f, parent, parentPath));
                    }
                }
            }
        }

        private bool IsNestedFileSystem(string file, string parentRelativePath, IFileSystem parent, string parentPath)
        {
            if (this.fileSystems.ContainsKey(file))
            {
                return true;
            }

            return this.GetOrAddFactory(file, parentRelativePath, parent, parentPath, out _);
        }

        private bool GetOrAddFactory(string file, string parentRelativePath, IFileSystem parent, string parentPath, [NotNullWhen(true)] out FileSystemFactory? factory)
        {
            if (!this.nestedFactories.TryGetValue(file, out factory))
            {
                this.nestedFactories[file] = factory = this.GetNestedFactory(file, parentRelativePath, parent, parentPath);
            }

            return factory is not null;
        }

        private FileSystemFactory? GetNestedFactory(string fullPath, string parentRelativePath, IFileSystem parent, string parentPath) =>
            this.handlers.Select(h => h(fullPath, parentRelativePath, parent, parentPath)).FirstOrDefault(f => f is not null);

        public bool TryGetEntry(string path, out Entry entry)
        {
            if (this.TryFindParentFileSystem(path, out var parentRelativePath, out var parent, out var parentPath))
            {
                if (parentRelativePath == string.Empty)
                {
                    entry = new Entry(parentPath, true, true);
                }
                else
                {
                    path = parent.Path.CombineIgnoringAbsolute(parentPath, parentRelativePath);
                    entry = new Entry(path, parent.File.Exists(parentRelativePath), parent.Directory.Exists(parentRelativePath) || this.IsNestedFileSystem(path, parentRelativePath, parent, parentPath));
                }

                return true;
            }

            entry = null!;
            return false;
        }

        public record Entry(string Path, bool CanOpen, bool CanEnumerateEntries);
    }
}
