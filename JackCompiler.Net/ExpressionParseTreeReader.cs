using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace JackCompiler.Net
{
    public static class ExpressionTree
    {
        public static XmlDocument ConvertToXmlDocument(IList<string> expressionTree)
        {
            var xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(expressionTree.Aggregate((current, next) => current + next));

            return xmlDocument;
        }

        public static int CountExpressions(IList<string> expressionList)
        {
            return ConvertToXmlDocument(expressionList).ChildNodes.Count;
        }
    }
}
