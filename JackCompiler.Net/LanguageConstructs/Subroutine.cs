using System;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler.Net.LanguageConstructs
{
    public class Subroutine
    {
        public Class Class { get; set; }
        public string Keyword { get; set; }
        public string Identifier { get; set; }
        public string OpeningSymbol { get; set; }
        public IList<Parameter> Parameters { get; set; }
        public string ClosingSymbol { get; set; }
    }
}
