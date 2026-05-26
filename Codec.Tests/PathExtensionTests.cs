namespace Codec.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class PathExtensionTests
    {
        public static string[] RootPaths = [
            "",
            "/",
            @"\",
            @"A:",
            @"C:\",
            "b:/",
        ];

        public static string[][] FilesAndDirectories = [
            [@"a",    @"a", @""],
            [@"a\",   @"",  @"a"],
            [@"a\b",  @"b", @"a"],
            [@"a/b",  @"b", @"a"],
            [@"a\b\", @"",  @"a\b"],
            [@"a\b/", @"",  @"a\b"],
            [@"a/b/", @"",  @"a/b"],
            [@"a/b\", @"",  @"a/b"],
            [@"/a",   @"a", @"/"],
            [@"\a",   @"a", @"\"],
            [@"C:\a", @"a", @"C:\"],
            [@"b:/a", @"a", @"b:/"],
            [@"C:\a\b",  @"b", @"C:\a"],
            [@"C:\a/b",  @"b", @"C:\a"],
            [@"C:\a\b\", @"",  @"C:\a\b"],
            [@"C:\a\b/", @"",  @"C:\a\b"],
            [@"C:\a/b/", @"",  @"C:\a/b"],
            [@"C:\a/b\", @"",  @"C:\a/b"],
            [@"C:/a\b",  @"b", @"C:/a"],
            [@"C:/a/b",  @"b", @"C:/a"],
            [@"C:/a\b\", @"",  @"C:/a\b"],
            [@"C:/a\b/", @"",  @"C:/a\b"],
            [@"C:/a/b/", @"",  @"C:/a/b"],
            [@"C:/a/b\", @"",  @"C:/a/b"],
        ];

        [Test]
        public void GetDirectoryName_ReturnsNullForNull()
        {
            Assert.That(PathExtensions.GetDirectoryName(null), Is.Null);
        }

        [Theory]
        [TestCaseSource(nameof(RootPaths))]
        public void GetDirectoryName_ForRootPath_ReturnsEmpty(string rootPath)
        {
            Assert.That(PathExtensions.GetDirectoryName(rootPath), Is.EqualTo(string.Empty));
        }

        [Theory]
        [TestCaseSource(nameof(RootPaths))]
        public void GetFileName_ForRootPath_ReturnsEmpty(string rootPath)
        {
            Assert.That(PathExtensions.GetFileName(rootPath), Is.EqualTo(string.Empty));
        }

        [Theory]
        [TestCaseSource(nameof(FilesAndDirectories))]
        public void GetDirectoryName_ReturnsExpected(string rootPath, string file, string directory)
        {
            Assert.That(PathExtensions.GetDirectoryName(rootPath), Is.EqualTo(directory));
        }

        [Theory]
        [TestCaseSource(nameof(FilesAndDirectories))]
        public void GetFileName_ReturnsExpected(string rootPath, string file, string directory)
        {
            Assert.That(PathExtensions.GetFileName(rootPath), Is.EqualTo(file));
        }
    }
}
