using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace JackCompiler.Net
{
    public class VMWriter : VmWriterBase
    {
        public VMWriter(string className)
            : base(className)
        {
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
            var instructions =  new List<string>
            {
                $"call {functionName} {nArgs}"
            };

            if (functionName.Contains("Output."))
            {
                instructions.AddRange(WritePop("temp", 0));
            }

            return instructions;
        }

        public IList<string> WriteExpression(XmlNode expressionTree, SymbolTable symbolTable)
        {
            var instructions = new List<string>();

            for (int i = 0; i < expressionTree.ChildNodes.Count;)
            {
                if (i == 0) // term
                {
                    var termNode = expressionTree.ChildNodes[i];

                    instructions.AddRange(WriteTerm(termNode, symbolTable));

                    i++;

                    continue;
                }
                else // op term
                {
                    // perform Reverse Polish Notation
                    var operatorNode = expressionTree.ChildNodes[i];
                    var nextTermNode = expressionTree.ChildNodes[i + 1];

                    instructions.AddRange(WriteTerm(nextTermNode, symbolTable));
                    instructions.Add(WriteArithmetic(operatorNode));

                    i += 2;
                }

            }

            return instructions;
        }

        public IList<string> WriteTerm(XmlNode termNode, SymbolTable symbolTable)
        {
            var instructions = new List<string>();

            if (IsInteger(termNode))
            {
                var termValue = GetIntegerValue(termNode);

                instructions.AddRange(WritePush("constant", termValue));
            }
            else if (IsExpression(termNode))
            {
                instructions.AddRange(WriteExpression(termNode.FirstChild, symbolTable));
            }
            else if (IsArithmeticNegation(termNode))
            {
                instructions.AddRange(WriteTerm(termNode.ChildNodes.Item(1), symbolTable));
                instructions.Add("neg");
            }
            else if (IsSubroutineCall(termNode))
            {
                var subroutineName = termNode.FirstChild.InnerText;
                var expressionTreeList = termNode.ChildNodes[1];

                foreach (XmlNode expressionTree in expressionTreeList.ChildNodes)
                {
                    instructions.AddRange(WriteExpression(expressionTree, symbolTable));
                }

                instructions.AddRange(WriteCall(subroutineName, 0));
            }
            else if (IsVariable(termNode))
            {
                var variableName = termNode.FirstChild.InnerText;
                var subroutineName = termNode.Attributes["subroutine"].Value;
                var symbol = symbolTable.GetSymbolByName(subroutineName, variableName);

                instructions.AddRange(WritePush(symbol.ToSegment(), symbol.RunningIndex));
            }

            return instructions;
        }

        /// <summary>
        /// Push onto the global stack from a virtual memory segment
        /// </summary>
        /// <param name="segment">One of [const|arg|local|static|this|that|pointer|temp]</param>
        /// <param name="index"></param>
        /// <returns></returns>
        public IList<string> WritePush(string segment, int index) 
        {
            return new List<string>
            {
                $"push {segment} {index}"
            };
        }

        /// <summary>
        /// Pop from the global stack onto a virtual memory segment
        /// </summary>
        /// <param name="segment">One of [const|arg|local|static|this|that|pointer|temp]</param>
        /// <param name="index"></param>
        /// <returns></returns>
        public IList<string> WritePop(string segment, int index) // CONST|ARG|LOCAL|STATIC|THIS|THAT|POINTER|TEMP
        {
            return new List<string>
            {
                $"pop {segment} {index}"
            };
        }

        public IList<string> WriteLabel(string labelName)
        {
            return new List<string>
            {
                $"label {labelName}"
            };
        }

        public IList<string> WriteIf(string labelName)
        {
            return new List<string>
            {
                $"if-goto {labelName}"
            };
        }

        public IList<string> WriteGoto(string labelName)
        {
            return new List<string>
            {
                $"goto {labelName}"
            };
        }
    }
}
