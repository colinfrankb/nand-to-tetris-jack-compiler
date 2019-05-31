using System.Collections.Generic;

namespace JackCompiler.Net.Extensions
{
    public static class DictionaryExtensions
    {
        public static void AddInstruction(this Dictionary<string, IList<string>> dictionary, string key, string instruction)
        {
            if (!dictionary.ContainsKey(key))
            {
                throw new KeyNotFoundException();
            }

            dictionary[key].Add(instruction);
        }

        public static void AddInstructions(this Dictionary<string, IList<string>> dictionary, IDictionary<string, IList<string>> instructions)
        {
            foreach (var keyValuePair in instructions)
            {
                if (!dictionary.ContainsKey(keyValuePair.Key))
                {
                    throw new KeyNotFoundException();
                }

                ((List<string>)dictionary[keyValuePair.Key]).AddRange(keyValuePair.Value);
            }
        }
    }
}
