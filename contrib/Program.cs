using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using System.IO.Compression;

namespace XnaContentZipper
{
    class Program
    {
        // Sample command line:
        // ZipArchiveCreator.exe c:\game\resources *.xnb c:\game\resources\resources.zip c:\game\ResourceId.cs Game.Resources ResourceId
        public static int Main(string[] args)
        {
            if (args.Length != 5 && args.Length != 6)
            {
                Console.WriteLine(@"Usage: [Input Directory Masks (| delimited, i.e. c:\files\*.xnb|c:\sound\*.ogg)] [Output File Name] [Output constants.cs file name] [Output constants namespace] [Output constants class name] [Optional Xact project file name]");
                return -1;
            }
            Console.WriteLine("Performing content zip archiving...");
            string[] directories = args[0].Split('|');
            Console.WriteLine("Input directories:");
            foreach (string directory in directories)
            {
                Console.WriteLine(" - {0}", directory);
            }
            Console.WriteLine("Output file name: {0}", args[1]);
            Console.WriteLine("Output constants.cs file name: {0}", args[2]);
            Console.WriteLine("Output constants namespace: {0}", args[3]);
            Console.WriteLine("Output constants class name: {0}", args[4]);
            if (args.Length == 6)
            {
                Console.WriteLine("Xact project file name: {0}", args[5]);
            }
            Console.WriteLine("Zipping content, please wait...");
            KeyValuePair<int, long> keyValue = ZippedContent.ZipContent(directories, args[1], args[2], args[3], args[4], args.Length == 5 ? null : args[5]);
            Console.WriteLine("{0} resources zipped, total archive size: {1}", keyValue.Key, keyValue.Value);
            Console.WriteLine("All done!");
            return 0;
        }
    }
}
