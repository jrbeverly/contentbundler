using System.IO.Compression;

namespace ContentBundler.Utility
{
    public static class ZipArchiveEntryExtensions
    {
        public static bool IsDirectory(this ZipArchiveEntry entry)
        {
            return entry.FullName.EndsWith("/");
        }
    }
}
