#region Imports

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Xna.Framework.Content;

#endregion Imports

namespace XnaContentZipper
{
    /// <summary>
    /// Reads a zip file created with the program and creates a content manager to work with it. You should use the .cs
    /// file generated from the ZipArchiveCreator tool to ensure you have the correct resource name.
    /// </summary>
    public class ZippedContent : ContentManager, IEnumerable<KeyValuePair<string, long>>
    {
        #region Helper classes

        private struct ContentNode : IComparable<ContentNode>
        {
            public string Name;
            public long Offset;

            public ContentNode(string name, long offset)
            {
                Name = name;
                Offset = offset;
            }

            public int CompareTo(ContentNode node)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(Name, node.Name);
            }
        }

        #endregion Helper classes

        #region Private variables

        private const int intSize = 4;
        private FileStream zipFile;
        private BinaryReader reader;
        private long indexOffset;
        private int count;

        #endregion Private variables

        #region Private methods

        private byte[] ReadBytes(string assetName)
        {
            int length;
            Stream stream = GetStream(assetName, out length);
            if (stream == null)
            {
                throw new IOException("Cannot get bytes for asset name " + assetName);
            }
            byte[] bytes = new byte[length];
            stream.Read(bytes, 0, length);
            return bytes;
        }

        private Stream GetStream(string assetName, out int length)
        {
            length = -1;
            int count;
            string key;
            ushort hash = GetHashCode(assetName);
            zipFile.Position = indexOffset + (hash * intSize);
            long offset = reader.ReadInt32();
            if (offset == int.MinValue)
            {
                return null;
            }
            zipFile.Position = indexOffset + offset;
            count = (int)reader.ReadByte();
            for (int i = 0; i < count; i++)
            {
                key = reader.ReadString();
                offset = reader.ReadInt64();
                if (key.Equals(assetName, StringComparison.OrdinalIgnoreCase))
                {
                    zipFile.Position = offset;
                    length = reader.ReadInt32();
                    return new DeflateStream(zipFile, CompressionMode.Decompress, true);
                }
            }
            return null;
        }

        private static void GetXactNameAndComment(string text, int index, out string name, out string comment)
        {
            name = null;
            comment = null;
            text = text.Substring(index).TrimStart();
            using (StringReader reader = new StringReader(text))
            {
                string line;
                int pos;
                while ((line = reader.ReadLine()) != null && (line = line.Trim()).Length > 0)
                {
                    if (line.StartsWith("Name = ", StringComparison.OrdinalIgnoreCase))
                    {
                        pos = line.IndexOf(" = ");
                        name = FieldEncode(line.Substring(pos + 3).TrimEnd(';'));
                    }
                    else if (line.StartsWith("Comment = ", StringComparison.OrdinalIgnoreCase))
                    {
                        pos = line.IndexOf(" = ");
                        comment = IntellisenseEncode(line.Substring(pos + 3).TrimEnd(';'));
                    }
                }
            }
        }

        private static string FieldEncode(string text)
        {
            return Regex.Replace(text, "[^a-zA-Z0-9_\\\\]", string.Empty);
        }

        private static string IntellisenseEncode(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        /// <summary>
        /// Gets a 16 bit hash code for text
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Hash code</returns>
        private static ushort GetHashCode(string text)
        {
            int hashCode = 5381;
            char c;
            text = text.Normalize().Trim();
            for (int i = 0; i < text.Length; i++)
            {
                c = text[i];
                if (char.IsLower(c))
                {
                    c = char.ToUpperInvariant(c);
                }
                hashCode = ((hashCode << 5) + hashCode) + c;
            }
            return (ushort)(hashCode & 0x0000FFFF);
        }

        #endregion Private methods

        #region Protected methods

        /// <summary>
        /// Gets a zip stream that will decompress an asset
        /// </summary>
        /// <param name="assetName">Asset name</param>
        /// <returns>Stream</returns>
        protected override Stream OpenStream(string assetName)
        {
            int length;
            Stream stream = GetStream(assetName, out length);
            if (stream == null)
            {
                stream = base.OpenStream(assetName);
            }
            return stream;
        }

        /// <summary>
        /// Disposes and closes the zip archive
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (zipFile != null)
            {
                zipFile.Close();
                zipFile = null;
            }
        }

        #endregion Protected methods

        #region Public methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="zipFileName">Path to zip file name</param>
        /// <param name="serviceProvider">Service provider</param>
        public ZippedContent(string zipFileName, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            zipFile = File.OpenRead(zipFileName);
            reader = new BinaryReader(zipFile, Encoding.UTF8);
            zipFile.Position = zipFile.Length - (intSize + intSize);
            indexOffset = reader.ReadInt64();
            zipFile.Position = indexOffset + (ushort.MaxValue * intSize) + intSize;
            count = reader.ReadInt32();
        }

        /// <summary>
        /// Loads a resource by using the id from a constants.cs file
        /// </summary>
        /// <typeparam name="T">Type of resource to load</typeparam>
        /// <param name="assetName">Id of the resource to load (should be a number)</param>
        /// <returns>Resource</returns>
        public override T Load<T>(string assetName)
        {
            return base.Load<T>(assetName);
        }

        /// <summary>
        /// Loads raw bytes from the file by using the id from a constants.cs file
        /// </summary>
        /// <param name="assetName">Asset name</param>
        /// <returns>Bytes</returns>
        public byte[] LoadBytes(string assetName)
        {
            return ReadBytes(assetName);
        }

        /// <summary>
        /// Loads the bytes for a resource into a stream
        /// </summary>
        /// <param name="assetName">Asset name</param>
        /// <returns>Stream</returns>
        public Stream LoadStream(string assetName)
        {
            return new MemoryStream(LoadBytes(assetName));
        }

        /// <summary>
        /// Loads a string from the file (assumes the file was in utf-8) by using the id from a constants.cs file
        /// </summary>
        /// <param name="assetName">Asset name</param>
        /// <returns>String</returns>
        public string LoadString(string assetName)
        {
            return Encoding.UTF8.GetString(LoadBytes(assetName));
        }

        /// <summary>
        /// Enumerates all resource names and their offsets into the file
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<KeyValuePair<string, long>> GetEnumerator()
        {
            zipFile.Position = indexOffset + (ushort.MaxValue * intSize);
            int count = reader.ReadInt32();
            int i = 0;
            int subCount;
            while (i < count)
            {
                subCount = (int)reader.ReadByte();
                for (int j = 0; j < subCount; j++)
                {
                    yield return new KeyValuePair<string, long>(reader.ReadString(), reader.ReadInt64());
                }
                count += subCount;
            }
        }

        /// <summary>
        /// Enumerates all resource names and their offsets into the file
        /// </summary>
        /// <returns>Enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Zips all content in a directory into a file (including sub directories)
        /// </summary>
        /// <param name="directoryMasks">Directoriy masks (i.e. c:\files\*.xnb)</param>
        /// <param name="outputFileName">Output zip file (pass this file name into the constructor)</param>
        /// <param name="constantsFileName">File name for constants.cs file - each resource may have a matching .description.txt file name
        /// containing a description for the resource, these descriptions will be put in the .xml documentation for the constant.</param>
        /// <param name="constantsNamespace">Namespace for constants file</param>
        /// <param name="constantsClassName">Class name for constants file</param>
        /// <param name="xactProjectFileName">File name for an Xact project (can be null or empty for none)</param>
        /// <returns>Key value pair, key is the number of assets zipped, value is the size of the zip file</returns>
        public static KeyValuePair<int, long> ZipContent(string[] directoryMasks, string outputFileName,
            string constantsFileName, string constantsNamespace, string constantsClassName, string xactProjectFileName)
        {
            File.Delete(outputFileName);
            long size;
            int count = 0;
            long indexPos;
            long hashOffset;
            ushort hash;
            string subFileName;
            string descriptionFileName;
            string variableName;
            string directory;
            string extension;
            FileInfo[] files;
            StringBuilder b = new StringBuilder();
            List<ContentNode> subList;
            List<ContentNode>[] hashArray = new List<ContentNode>[ushort.MaxValue];
            using (FileStream zipStream = File.Create(outputFileName))
            {
                BinaryWriter binWriter = new BinaryWriter(zipStream, Encoding.UTF8);
                using (StringWriter writer = new StringWriter(b))
                {
                    writer.WriteLine("// *** Auto-generated from ZipArchiveCreator program *** //");
                    writer.WriteLine();
                    writer.WriteLine("#region Imports");
                    writer.WriteLine();
                    writer.WriteLine("using System;");
                    writer.WriteLine();
                    writer.WriteLine("#endregion Imports");
                    writer.WriteLine();
                    writer.WriteLine("namespace {0}", constantsNamespace);
                    writer.WriteLine("{");
                    writer.WriteLine("\t/// <summary>");
                    writer.WriteLine("\t/// Constants for resources in file {0}", Path.GetFileName(outputFileName));
                    writer.WriteLine("\t/// </summary>");
                    writer.WriteLine("\tpublic class {0}", constantsClassName);
                    writer.WriteLine("\t{");
                    foreach (string directoryMask in directoryMasks)
                    {
                        directory = Path.GetDirectoryName(directoryMask);
                        extension = Path.GetFileName(directoryMask);
                        DirectoryInfo info = new DirectoryInfo(directory);
                        files = info.GetFiles(extension, SearchOption.AllDirectories);
                        foreach (FileInfo file in files)
                        {
                            // .description files and .zip files can never be added to the archive
                            if (file.FullName.IndexOf(".description", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            byte[] bytes = File.ReadAllBytes(file.FullName);
                            subFileName = Path.GetFileNameWithoutExtension(file.Name).Replace(".", string.Empty);
                            variableName = FieldEncode(subFileName);
                            descriptionFileName = file.FullName + ".description.txt";
                            writer.WriteLine("\t\t/// <summary>");
                            if (File.Exists(descriptionFileName))
                            {
                                writer.Write("\t\t/// ");
                                writer.WriteLine(IntellisenseEncode(File.ReadAllText(descriptionFileName)) + "<br/>");
                            }
                            writer.WriteLine("\t\t/// [Original file = &apos;{0}&apos;]", file.FullName.Substring(info.FullName.Length));
                            writer.WriteLine("\t\t/// </summary>");
                            writer.WriteLine("\t\tpublic const string {0} = \"{1}\";", variableName, subFileName);
                            hash = GetHashCode(subFileName);
                            if (hashArray[hash] == null)
                            {
                                hashArray[hash] = new List<ContentNode>(new ContentNode[] { new ContentNode(subFileName, zipStream.Length) });
                            }
                            else
                            {
                                hashArray[hash].Add(new ContentNode(subFileName, zipStream.Length));
                            }
                            writer.WriteLine();
                            binWriter.Write(bytes.Length);
                            using (DeflateStream stream = new DeflateStream(zipStream, CompressionMode.Compress, true))
                            {
                                stream.Write(bytes, 0, bytes.Length);
                            }
                            count++;
                        }
                    }
                    if (!string.IsNullOrEmpty(xactProjectFileName))
                    {
                        string name;
                        string comment;
                        string xactText = File.ReadAllText(xactProjectFileName, Encoding.UTF8);
                        MatchCollection matches = Regex.Matches(xactText, @"\r\n +Cue\r\n +\{", RegexOptions.IgnoreCase); // 
                        if (matches.Count > 0)
                        {
                            foreach (Match match in matches)
                            {
                                GetXactNameAndComment(xactText, match.Index, out name, out comment);
                                if (!string.IsNullOrEmpty(name))
                                {
                                    writer.WriteLine("\t\t /// <summary>");
                                    if (!string.IsNullOrEmpty(comment))
                                    {
                                        writer.WriteLine("\t\t/// {0}", comment);
                                    }
                                    else
                                    {
                                        writer.WriteLine("\t\t/// No comment / notes specified for this xact cue");
                                    }
                                    writer.WriteLine("\t\t/// </summary>");
                                    writer.WriteLine("\t\tpublic const string {0} = \"{1}\";", "XactCue_" + name, name);
                                    writer.WriteLine();
                                }
                            }
                        }
                    }
                    writer.WriteLine("\t}");
                    writer.WriteLine("}");
                }
                indexPos = zipStream.Length;
                zipStream.SetLength(zipStream.Length + (ushort.MaxValue * intSize) + intSize);
                zipStream.Position = zipStream.Length;
                binWriter.Write(count);
                hashOffset = zipStream.Length;
                for (int i = 0; i < hashArray.Length; i++)
                {
                    zipStream.Position = indexPos + (i * intSize);
                    if ((subList = hashArray[i]) == null)
                    {
                        binWriter.Write(int.MinValue);
                        continue;
                    }
                    binWriter.Write((int)(zipStream.Length - indexPos));
                    if (subList.Count > byte.MaxValue)
                    {
                        throw new ApplicationException("Too many items in sub list");
                    }
                    subList.Sort();
                    zipStream.Position = hashOffset;
                    binWriter.Write((byte)subList.Count);
                    foreach (ContentNode node in subList)
                    {
                        binWriter.Write(node.Name);
                        binWriter.Write(node.Offset);
                    }
                    hashOffset = zipStream.Position;
                }
                zipStream.Position = zipStream.Length;
                binWriter.Write(indexPos);
                size = zipStream.Length;
                if (!File.Exists(constantsFileName) || b.ToString() != File.ReadAllText(constantsFileName, Encoding.UTF8))
                {
                    File.WriteAllText(constantsFileName, b.ToString(), Encoding.UTF8);
                }
            }
            return new KeyValuePair<int, long>(count, size);
        }

        #endregion Public methods
    }
}
