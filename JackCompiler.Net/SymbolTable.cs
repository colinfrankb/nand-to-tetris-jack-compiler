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
        private int _whileLoopRunningIndex = -1;
        private int _ifStatementRunningIndex = -1;

        public SymbolTable()
        {
            _classScopeIdentifiers = new List<Symbol>();
            _subroutineScopeIdentifiers = new List<Symbol>();
        }

        public void StartSubroutine()
        {
            _subroutineScopeIdentifiers = new List<Symbol>();
            _whileLoopRunningIndex = -1;
            _ifStatementRunningIndex = -1;
        }

        // always one class compiling at a time
        public int DefineIdentifier(string name, string type, string kind)
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
                if (!_subroutineScopeIdentifiers.Contains(symbol))
                {
                    symbol.RunningIndex = GetNextRunningIndex(_subroutineScopeIdentifiers, symbol.Kind);

                    _subroutineScopeIdentifiers.Add(symbol);
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

        public int IndexOf(string name)
        {
            return GetSymbolByName(name)?.RunningIndex ?? -1;
        }

        public Symbol GetSymbolByName(string identifierName)
        {
            var symbol = _subroutineScopeIdentifiers.FirstOrDefault(x => x.Name == identifierName);

            return symbol ?? _classScopeIdentifiers.FirstOrDefault(x => x.Name == identifierName);
        }

        public int GetNextWhileLoopRunningIndex()
        {
            _whileLoopRunningIndex++;

            return _whileLoopRunningIndex;
        }

        public int GetNextIfStatementRunningIndex()
        {
            _ifStatementRunningIndex++;

            return _ifStatementRunningIndex;
        }
    }
}
