namespace JackCompiler.Net
{
    public class Subroutine
    {
        private readonly string _subroutineName;

        public Subroutine(string subroutineName)
        {
            _subroutineName = subroutineName;
        }

        public bool IsMethod(SymbolTable symbolTable)
        {
            if (_subroutineName.Contains("."))
            {
                //if a symbol exists for the first part of the subroutine, then it
                //is an object, and therefore a method call
                return symbolTable.IndexOf(_subroutineName.Split('.')[0]) > -1;
            }

            //therefore method call within class
            return true;
        }

        public Symbol GetObjectSymbol(SymbolTable symbolTable)
        {
            var objectIdentifier = _subroutineName.Split('.')[0];

            return symbolTable.GetSymbolByName(objectIdentifier);
        }
    }
}
