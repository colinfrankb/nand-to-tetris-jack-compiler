using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JackCompiler.Net
{
    public class JackTokenizer
    {
        public string FileContent;
        public const string Keyword = "keyword";
        public const string Symbol = "symbol";
        public const string Identifier = "identifier";
        public const string StringConstant = "stringConstant";
        public const string IntegerConstant = "integerConstant";
        public const string SymbolRegexPattern = @"[{|}|\(|\)|\[|\]|.|,|;|+|\-|*|\/|&|\||<|>|=|~]";
        public const string StringConstantRegexPattern = "\"(?<content>.*?)\"";
        private IDictionary<string, string> _stringConstantMap; 


        public JackTokenizer(string fileContent)
        {
            FileContent = fileContent;
            _stringConstantMap = new Dictionary<string, string>();
        }

        public IEnumerable<Token> Analyze()
        {
            RemoveComments();

            ReplaceStringConstants();

            string[] lines = FileContent.Split(" ");

            var tokens = new List<Token>();

            foreach (var line in lines)
            {
                var token = DetermineSingleToken(line);

                if (token == null)
                {
                    //SquareGame.new()
                    //S, 0, 0
                    //q, 1, 0
                    //u, 2, 0
                    //a, 3, 0
                    //r, 4, 0
                    //e, 5, 0
                    //G, 6, 0
                    //a, 7, 0
                    //m, 8, 0
                    //e, 9, 0
                    //., 10, 0
                    //n, 11, 11
                    //(, 12, 11
                    //), 13, 13

                    //(direction
                    //(, 0, 0
                    //d, 1, 1
                    //i, 2,
                    //r, 3,
                    //e, 4,
                    //c, 5,
                    //t, 6,
                    //i, 7,
                    //o, 8,
                    //n, 9,
                    for (int i = 0, j = 0; i < line.Length; i++)
                    {
                        string character = line.Substring(i, 1);

                        if (Regex.IsMatch(character, SymbolRegexPattern))
                        {
                            var lengthOfIdentifier = i - j;

                            if (lengthOfIdentifier > 0)
                            {
                                tokens.Add(DetermineSingleToken(line.Substring(j, lengthOfIdentifier)));
                            }

                            j = i + 1;

                            tokens.Add(new Token
                            {
                                TokenType = "symbol",
                                Value = character
                            });
                        }

                        if ((i + 1) == line.Length)
                        {
                            var tokenValue = line.Substring(j);

                            if (!string.IsNullOrWhiteSpace(tokenValue))
                            {
                                tokens.Add(DetermineSingleToken(line.Substring(j)));
                            }
                        }
                    }
                }
                else
                {
                    tokens.Add(token);
                }
            }

            return tokens;
        }

        private void ReplaceStringConstants()
        {
            FileContent = Regex.Replace(FileContent, StringConstantRegexPattern, MatchEval);
        }

        private string MatchEval(Match match)
        {
            var stringConstantKey = Guid.NewGuid().ToString().Substring(0, 8);

            _stringConstantMap.Add(stringConstantKey, match.Groups["content"].Value);

            return stringConstantKey;
        }

        private void RemoveComments()
        {
            FileContent = Regex.Replace(FileContent, @"\/\/.*", "");

            FileContent = Regex.Replace(FileContent, @"\/\*\*.*?\*\/", "", RegexOptions.Singleline);

            FileContent = Regex.Replace(FileContent, @"(\n|\r)", "");

            FileContent = Regex.Replace(FileContent, @" +", " ");
        }

        private Token DetermineSingleToken(string line)
        {
            if (IsKeyword(line))
            {
                return new Token { TokenType = Keyword, Value = line };
            }
            else if (IsSymbol(line))
            {
                return new Token { TokenType = Symbol, Value = line };
            }
            else if (IsIntegerConstant(line))
            {
                return new Token { TokenType = IntegerConstant, Value = line };
            }
            else if (IsStringConstant(line))
            {
                return new Token { TokenType = StringConstant, Value = _stringConstantMap[line] };
            }
            else if (IsIdentifier(line))
            {
                return new Token { TokenType = Identifier, Value = line };
            }

            return null;
        }

        private bool IsIntegerConstant(string line)
        {
            return Regex.IsMatch(line, @"^[0-9]+$");
        }

        private bool IsStringConstant(string line)
        {
            return _stringConstantMap.ContainsKey(line);
        }

        private bool IsIdentifier(string line)
        {
            return Regex.IsMatch(line, @"^\w+$");
        }

        private bool IsSymbol(string line)
        {
            return Regex.IsMatch(line, $"^{SymbolRegexPattern}$");
        }

        private bool IsKeyword(string line)
        {
            return new string[] 
            {
                "class",
                "constructor",
                "function",
                "method",
                "field",
                "static",
                "var",
                "int",
                "char",
                "boolean",
                "void",
                "true",
                "false",
                "null",
                "this",
                "let",
                "do",
                "if",
                "else",
                "while",
                "return"
            }.Contains(line);
        }
    }
}
