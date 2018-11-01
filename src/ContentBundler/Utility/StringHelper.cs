using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ContentBundler.Utility
{
    public static class StringHelper
    {
        public static string Sanitize(string fileName)
        {
            var rgx = new Regex("[^a-zA-Z0-9_]");
            return rgx.Replace(fileName, "");
        }

        public static string GetParent(string fileName)
        {
            return Path.GetDirectoryName(fileName).Replace('\\', '/');
        }

        public static string GetLevelUp(string directory)
        {
            int index = directory.Trim('/', '\\').LastIndexOfAny(new char[] { '\\', '/' });

            return (index >= 0) ? directory.Remove(index) : string.Empty;
        }

        public static bool IsRoot(string directory)
        {
            int index = directory.LastIndexOfAny(new char[] { '\\', '/' });
            return index < 0;
        }

        public static string GetFileName(string filename)
        {
            return Sanitize(Path.GetFileNameWithoutExtension(filename)).ToVarName();
        }

        public static string GetDirectory(string fileName)
        {
            var name = GetDirName(fileName);
            return Sanitize(name);
        }

        public static string GetDirName(string fileName)
        {
            var tmp = fileName.Substring(0, fileName.Length - 1);
            return tmp.Split('/').Last();
        }

        public static string ToVarName(this string s)
        {
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
