using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace JackCompiler.Net
{
    public class VmWriterBase
    {
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

        public bool IsVariable(XmlNode termNode)
        {
            return termNode.Attributes["kind"].Value == "variable";
        }

        public bool IsSubroutineCall(XmlNode termNode)
        {
            return termNode.Attributes["kind"].Value == "subroutineCall";
        }

        public bool IsExpression(XmlNode termNode)
        {
            return termNode.FirstChild.Name == "expression";
        }

        public bool IsArithmeticNegation(XmlNode termNode)
        {
            return termNode.FirstChild.Name == "symbol";
        }

        public int GetIntegerValue(XmlNode termNode)
        {
            return Convert.ToInt32(termNode.FirstChild.InnerText.Trim());
        }

        public bool IsInteger(XmlNode termNode)
        {
            return termNode.FirstChild.Name == TokenType.IntegerConstant;
        }
    }
}
