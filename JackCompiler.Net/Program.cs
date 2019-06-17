using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JackCompiler.Net
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileAttributes = File.GetAttributes(args[0]);
            var fileDirectoryName = Path.GetDirectoryName(args[0]);
            var filePaths = new List<string>();
            var assemblyProgramFilePathWithoutExtension = string.Empty;

            if ((fileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                assemblyProgramFilePathWithoutExtension = $"{args[0]}\\{args[0].Split("\\").Last()}";
                filePaths.AddRange(Directory.GetFiles(args[0], "*.jack"));
            }
            else
            {
                assemblyProgramFilePathWithoutExtension = $"{fileDirectoryName}\\{fileDirectoryName.Split("\\").Last()}";
                filePaths.Add(args[0]);
            }

            Console.WriteLine("The following files will be read:");
            Console.WriteLine(string.Join(Environment.NewLine, filePaths));

            Console.WriteLine("The following files were generated:");
            foreach (var filePath in filePaths)
            {
                var jackTokenizer = new JackTokenizer(File.ReadAllText(filePath));

                var tokens = jackTokenizer.Analyze();

                string directoryPath = Path.GetDirectoryName(filePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                string outputTokensFilePath = $"{directoryPath}\\{fileNameWithoutExtension}T.mine.xml";
                string outputLanguageConstructsFilePath = $"{directoryPath}\\{fileNameWithoutExtension}.mine.xml";

                Console.WriteLine($"{outputTokensFilePath}");
                Console.WriteLine($"{outputLanguageConstructsFilePath}");

                File.WriteAllText($"{outputTokensFilePath}", GenerateXml(tokens));

                var compilationEngine = new CompilationEngine();

                var languageConstructs = compilationEngine.Compile(tokens);

                File.WriteAllLines($"{outputLanguageConstructsFilePath}", languageConstructs);
            }
        }

        private static string GenerateXml(IEnumerable<Token> tokens)
        {
            var xml = new StringBuilder();

            xml.AppendLine("<tokens>");

            foreach (var token in tokens)
            {
                if (token != null)
                {
                    xml.AppendLine($"<{token.TokenType}> {XmlEncoder.EncodeTokenValue(token.Value)} </{token.TokenType}>");
                }
            }

            xml.AppendLine("</tokens>");

            return xml.ToString();
        }
    }
}
