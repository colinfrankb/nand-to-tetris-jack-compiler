using System.Collections.Generic;
using System.Linq;

namespace JackCompiler.Net.Extensions
{
    public static class StackExtensions
    {
        public static IList<Token> Pop(this Stack<Token> tokens, int count)
        {
            var result = new List<Token>();
            var tokensThreshold = tokens.Count - count;

            while (tokens.Count > tokensThreshold)
            {
                result.Add(tokens.Pop());
            }

            return result;
        }

        public static IList<Token> PopParameterList(this Stack<Token> tokens)
        {
            var result = new List<Token>();
            var lastTokenValue = string.Empty;

            while (lastTokenValue != ")")
            {
                var token = tokens.Pop();

                lastTokenValue = token.Value;

                result.Add(token);
            }

            return result;
        }
    }
}
