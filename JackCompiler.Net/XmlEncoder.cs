namespace JackCompiler.Net
{
    public class XmlEncoder
    {
        public static string EncodeTokenValue(string tokenValue)
        {
            switch (tokenValue)
            {
                case ">":
                    return "&gt;";
                case "<":
                    return "&lt;";
                case "&":
                    return "&amp;";
                default:
                    return tokenValue;
            }
        }
    }
}
