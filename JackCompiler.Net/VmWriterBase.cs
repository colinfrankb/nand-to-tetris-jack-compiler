using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
            { "~", "not" },
            { "=", "eq" },
            { ">", "gt" },
            { "<", "lt" },
            { "&", "and" },
            { "|", "or" }
        };

        public bool IsVariable(XmlNode termNode)
        {
            return GetTermNodeKind(termNode) == "variable" || termNode.FirstChild.Name == "identifier";
        }

        public bool IsSubroutineCall(XmlNode termNode)
        {
            return GetTermNodeKind(termNode) == "subroutineCall";
        }

        private string GetTermNodeKind(XmlNode termNode)
        {
            return termNode.Attributes["kind"]?.Value ?? string.Empty;
        }

        public bool IsExpression(XmlNode termNode)
        {
            return termNode.FirstChild.Name == "expression";
        }

        public bool IsNegation(XmlNode termNode)
        {
            return termNode.FirstChild.Name == "symbol";
        }

        public int GetIntegerValue(XmlNode termNode)
        {
            return Convert.ToInt32(termNode.FirstChild.InnerText.Trim());
        }

        public bool GetBooleanValue(XmlNode termNode)
        {
            return Convert.ToBoolean(termNode.FirstChild.InnerText.Trim());
        }

        public bool IsInteger(XmlNode termNode)
        {
            return termNode.FirstChild.Name == TokenType.IntegerConstant;
        }

        public bool IsString(XmlNode termNode)
        {
            return termNode.FirstChild.Name == TokenType.StringConstant;
        }

        public bool IsBoolean(XmlNode termNode)
        {
            return termNode.FirstChild.Name == TokenType.Keyword && Regex.IsMatch(termNode.FirstChild.InnerText.Trim(), "(true|false)");
        }

        public bool IsThis(XmlNode termNode)
        {
            return termNode.FirstChild.Name == TokenType.Keyword && termNode.FirstChild.InnerText.Trim() == "this";
        }
    }
}
