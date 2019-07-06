using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace JackCompiler.Net
{
    public class VmWriterBase
    {
        protected readonly string _className;
        protected IDictionary<string, string> _arithmeticDictionary = new Dictionary<string, string>
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

        public VmWriterBase(string className)
        {
            _className = className;
        }

        protected string WriteArithmetic(XmlNode operatorNode)
        {
            return _arithmeticDictionary[operatorNode.InnerText.Trim()];
        }

        protected bool IsVariable(XmlNode termNode)
        {
            return termNode.Attributes["kind"].Value == "variable";
        }

        protected bool IsSubroutineCall(XmlNode termNode)
        {
            return termNode.Attributes["kind"].Value == "subroutineCall";
        }

        protected bool IsExpression(XmlNode termNode)
        {
            return termNode.FirstChild.Name == "expression";
        }

        protected bool IsArithmeticNegation(XmlNode termNode)
        {
            return termNode.FirstChild.Name == "symbol";
        }

        protected int GetIntegerValue(XmlNode termNode)
        {
            return Convert.ToInt32(termNode.FirstChild.InnerText.Trim());
        }

        protected bool IsInteger(XmlNode termNode)
        {
            return termNode.FirstChild.Name == TokenType.IntegerConstant;
        }
    }
}
