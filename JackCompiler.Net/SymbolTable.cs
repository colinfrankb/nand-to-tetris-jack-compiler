using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JackCompiler.Net
{
    public class SymbolTable
    {
        private IList<Symbol> _classScopeIdentifiers;
        private IList<Symbol> _subroutineScopeIdentifiers;

        public SymbolTable()
        {
            _classScopeIdentifiers = new List<Symbol>();
            _subroutineScopeIdentifiers = new List<Symbol>();
        }

        public void DefineIdentifier(string name, string type, string kind)
        {
            if (Regex.IsMatch(kind, "(static|field)"))
            {
                _classScopeIdentifiers.Add(new Symbol
                {
                    Name = name,
                    Type = type,
                    Kind = kind
                });
            }
            else // kind is (var|argument)
            {
                _subroutineScopeIdentifiers.Add(new Symbol
                {
                    Name = name,
                    Type = type,
                    Kind = kind
                });
            }
        }

        public int IndexOf(string name)
        {
            var symbol = new Symbol
            {
                Name = name
            };

            var indexInSubroutineScopeIdentifiers = _subroutineScopeIdentifiers.IndexOf(symbol);

            return indexInSubroutineScopeIdentifiers > -1 ? indexInSubroutineScopeIdentifiers : _classScopeIdentifiers.IndexOf(symbol);
        }
    }
}
