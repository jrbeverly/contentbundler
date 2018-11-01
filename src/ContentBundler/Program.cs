using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ContentBundler
{
    public class Program
    {
        public class Options
        {
            [Option('a', "archive", Required = false, HelpText = "The zip file to generate the strongly typed file from.")]
            public string Archive { get; set; }

            [Option('f', "file", Required = false, HelpText = "The file to output the strongly typed file.")]
            public string File { get; set; }

            [Option('c', "class", Required = false, HelpText = "The name of the strongly typed root class.")]
            public string Class { get; set; }

            [Option('n', "namespace", Required = false, HelpText = "The namespace of the strongly typed file.")]
            public string Namespace { get; set; }

        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
               .WithParsed(o =>
               {
                   ContentGenerator generator = new ContentGenerator();
                   ContentSettings settings = new ContentSettings()
                   {
                       Namespace = o.Namespace,
                       ClassName = o.Class
                   };

                   try
                   {                       
                       using (var zipStream = File.Open(o.Archive, FileMode.Open))
                       {
                           using (ZipArchive zip = new ZipArchive(zipStream))
                           {
                               var @namespace = generator.Create(settings, zip.Entries);

                               var code = @namespace
                                   .NormalizeWhitespace()
                                   .ToFullString();

                               File.WriteAllText(o.File, code);
                           }
                       }
                   }
                   catch (Exception exc)
                   {
                       Console.WriteLine(exc.Message);
                   }
               });


        }
    }
}
