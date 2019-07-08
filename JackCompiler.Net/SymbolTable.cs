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
        private IDictionary<string, IList<Symbol>> _subroutineScopeIdentifiers;

        public SymbolTable()
        {
            _classScopeIdentifiers = new List<Symbol>();
            _subroutineScopeIdentifiers = new Dictionary<string, IList<Symbol>>();
        }

        public void StartSubroutine(string subroutineName)
        {
            _subroutineScopeIdentifiers[subroutineName] = new List<Symbol>();
        }

        // always one class compiling at a time
        public int DefineIdentifier(string subroutineName, string name, string type, string kind)
        {
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
                    symbol.RunningIndex = GetNextRunningIndex(_classScopeIdentifiers, symbol.Kind);

                    _classScopeIdentifiers.Add(symbol);
                }
            }
            else // kind is (var|argument)
            {
                if (!_subroutineScopeIdentifiers.ContainsKey(subroutineName))
                {
                    _subroutineScopeIdentifiers[subroutineName] = new List<Symbol>();
                }

                if (_subroutineScopeIdentifiers.ContainsKey(subroutineName) && !_subroutineScopeIdentifiers[subroutineName].Contains(symbol))
                {
                    symbol.RunningIndex = GetNextRunningIndex(_subroutineScopeIdentifiers[subroutineName], symbol.Kind);

                    _subroutineScopeIdentifiers[subroutineName].Add(symbol);
                }
            }

            return symbol.RunningIndex;
        }

        private int GetNextRunningIndex(IList<Symbol> scopeIdentifiers, string kind)
        {
            var currentRunningIndex = scopeIdentifiers
                .Where(x => x.Kind == kind)
                .OrderByDescending(x => x.RunningIndex)
                .FirstOrDefault()?.RunningIndex ?? -1;

            currentRunningIndex++;

            return currentRunningIndex;
        }

        public int IndexOf(string subroutineName, string name)
        {
            return GetSymbolByName(subroutineName, name)?.RunningIndex ?? -1;
        }

        public Symbol GetSymbolByName(string subroutineName, string identifierName)
        {
            var symbol = _subroutineScopeIdentifiers[subroutineName].FirstOrDefault(x => x.Name == identifierName);

            return symbol ?? _classScopeIdentifiers.FirstOrDefault(x => x.Name == identifierName);
        }
    }
}
