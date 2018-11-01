using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ContentBundler.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ContentBundler
{
    public sealed class ContentGenerator
    {

        public FieldDeclarationSyntax CreateField(string name, string value)
        {
            var varName = SyntaxFactory.Identifier(name);
            var typeString = SyntaxFactory.ParseTypeName("string");
            var literal = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));

            var varDecl = SyntaxFactory.VariableDeclaration(typeString).AddVariables(SyntaxFactory.VariableDeclarator(varName, null, SyntaxFactory.EqualsValueClause(literal)));
            return SyntaxFactory.FieldDeclaration(varDecl)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }

        public ClassDeclarationSyntax CreateClass(string name)
        {
            var @class = SyntaxFactory.ClassDeclaration(name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            return @class;
        }

        public NamespaceDeclarationSyntax CreateRoot(ContentSettings settings, ClassDeclarationSyntax rootClass)
        {
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(settings.Namespace)).NormalizeWhitespace();
            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));

            return @namespace.AddMembers(rootClass);
        }

        public NamespaceDeclarationSyntax Create(ContentSettings settings, IEnumerable<ZipArchiveEntry> entries)
        {
            var rootClass = CreateClass(settings.ClassName);

            var dirs = entries
                .Where(x => x.IsDirectory())
                .ToDictionary(
                x => x.FullName.Trim('/', '\\'),
                x => CreateClass(StringHelper.GetDirectory(x.FullName).ToVarName()));

            foreach (var file in entries.Where(x => !x.IsDirectory()))
            {
                var varName = StringHelper.GetFileName(file.Name);

                var dir = StringHelper.GetParent(file.FullName);
                var @class = dirs[dir];
                dirs[dir] = dirs[dir].AddMembers(CreateField(varName, file.FullName));
            }

            var keys = dirs.Keys.OrderByDescending(p => p.Length).ToArray();
            foreach (var dir in keys)
            {                
                if (StringHelper.IsRoot(dir))
                {
                    rootClass = rootClass.AddMembers(dirs[dir]);
                }
                else
                {
                    var parentDir = StringHelper.GetLevelUp(dir);
                    dirs[parentDir] = dirs[parentDir].AddMembers(dirs[dir]);
                }
            }

            return CreateRoot(settings, rootClass);
        }
    }
}
