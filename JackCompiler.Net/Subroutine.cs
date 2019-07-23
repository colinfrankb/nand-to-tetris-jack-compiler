namespace JackCompiler.Net
{
    public class Subroutine
    {
        private readonly string _subroutineName;

        public Subroutine(string subroutineName)
        {
            _subroutineName = subroutineName;
        }

        public bool IsMethod()
        {
            //Only method calls will have the dot "." in the subroutineName
            return _subroutineName.Contains(".");
        }

        public Symbol GetObjectSymbol(SymbolTable symbolTable)
        {
            var objectIdentifier = _subroutineName.Split('.')[0];

            return symbolTable.GetSymbolByName(objectIdentifier);
        }
    }
}
