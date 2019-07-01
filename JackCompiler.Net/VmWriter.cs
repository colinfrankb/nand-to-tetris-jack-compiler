﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace JackCompiler.Net
{
    public class VMWriter
    {
        private readonly string _className;
        private IDictionary<string, string> _arithmeticDictionary = new Dictionary<string, string>
        {
            { "+", "add" },
            { "-", "sub" },
            { "*", "call Math.multiply 2" },
            { "/", "call Math.divide 2" },
            { "~", "neg" },
            { "=", "eq" },
            { ">", "gt" },
            { "<", "lt" },
            { "&", "and" },
            { "|", "or" }
        };

        public VMWriter(string className)
        {
            _className = className;
        }

        public IList<string> WriteFunction(string functionName, int nLocals)
        {
            return new List<string>
            {
                $"function {_className}.{functionName} {nLocals}"
            };
        }

        public IList<string> WriteCall(string functionName, int nArgs)
        {
            return new List<string>
            {
                $"call {functionName} {nArgs}"
            };
        }

        public IList<string> WriteExpression(XmlNode expressionTree)
        {
            var instructions = new List<string>();

            for (int i = 0; i < expressionTree.ChildNodes.Count;)
            {
                if (i == 0) // term
                {
                    var termNode = expressionTree.ChildNodes[i];

                    instructions.AddRange(WriteTerm(termNode));

                    i++;

                    continue;
                }
                else // op term
                {
                    // perform Reverse Polish Notation
                    var operatorNode = expressionTree.ChildNodes[i];
                    var nextTermNode = expressionTree.ChildNodes[i + 1];

                    instructions.AddRange(WriteTerm(nextTermNode));
                    instructions.Add(WriteArithmetic(operatorNode));

                    i += 2;
                }

            }

            return instructions;
        }

        private string WriteArithmetic(XmlNode operatorNode)
        {
            return _arithmeticDictionary[operatorNode.InnerText.Trim()];
        }

        private IList<string> WriteTerm(XmlNode termNode)
        {
            var instructions = new List<string>();

            if (IsInteger(termNode))
            {
                var termValue = GetIntegerValue(termNode);

                instructions.AddRange(WritePush("constant", termValue));
            }
            else if (IsExpression(termNode))
            {
                instructions.AddRange(WriteExpression(termNode.FirstChild));
            }

            return instructions;
        }

        private bool IsExpression(XmlNode termNode)
        {
            return termNode.FirstChild.Name == "expression";
        }

        private int GetIntegerValue(XmlNode termNode)
        {
            return Convert.ToInt32(termNode.FirstChild.InnerText.Trim());
        }

        private bool IsInteger(XmlNode termNode)
        {
            return termNode.FirstChild.Name == TokenType.IntegerConstant;
        }

        public IList<string> WritePush(string segment, int index) // CONST|ARG|LOCAL|STATIC|THIS|THAT|POINTER|TEMP
        {
            return new List<string>
            {
                $"push {segment} {index}"
            };
        }
    }
}
