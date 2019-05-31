using System;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler.Net
{
    public enum TokenType
    {
        Keyword = 0,
        Symbol = 1,
        Identifier = 2,
        StringConstant = 3,
        IntegerConstant = 4
    }
}
