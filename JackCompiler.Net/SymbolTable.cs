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

        public int DefineIdentifier(string name, string type, string kind)
        {
            var index = -1;
            var symbol = new Symbol
            {
                Name = name,
                Type = type,
                Kind = kind
            };

            if (Regex.IsMatch(symbol.Kind, "(static|field)"))
            {
                if(!_classScopeIdentifiers.Contains(symbol))
                {
                    _classScopeIdentifiers.Add(symbol);

                    index = _classScopeIdentifiers.IndexOf(symbol);
                }
            }
            else // kind is (var|argument)
            {
                if (!_subroutineScopeIdentifiers.Contains(symbol))
                {
                    _subroutineScopeIdentifiers.Add(symbol);

                    index = _subroutineScopeIdentifiers.IndexOf(symbol);
                }
            }

            return index;
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

        public Symbol GetSymbolByName(string identifierName)
        {
            var symbol = _subroutineScopeIdentifiers.FirstOrDefault(x => x.Name == identifierName);

            return symbol ?? _classScopeIdentifiers.FirstOrDefault(x => x.Name == identifierName);
        }
    }
}
