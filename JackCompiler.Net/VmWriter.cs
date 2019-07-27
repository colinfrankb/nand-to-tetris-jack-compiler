using System.Collections.Generic;
using System.Xml;

namespace JackCompiler.Net
{
    public class VMWriter : VmWriterBase
    {
        public IList<string> WriteFunction(string functionName, int nLocals)
        {
            return new List<string>
            {
                $"function {functionName} {nLocals}"
            };
        }

        public IList<string> WriteCall(string functionName, int nArgs)
        {
            var instructions =  new List<string>
            {
                $"call {functionName} {nArgs}"
            };

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

        public string WriteArithmetic(XmlNode operatorNode)
        {
            return _arithmeticDictionary[operatorNode.InnerText.Trim()];
        }
    }
}
