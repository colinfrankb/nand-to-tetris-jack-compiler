using System;
using System.Collections.Generic;
using System.Text;

namespace JackCompiler.Net
{
    public class Symbol
    {
        private IDictionary<string, string> _segmentMap = new Dictionary<string, string>()
        {
            { "var", "local" },
            { "argument", "argument" },
            { "field", "this" },
            { "static", "static" }
        };

        public string Name { get; set; }
        public string Type { get; set; }
        public string Kind { get; set; }
        public int RunningIndex { get; set; }

        public string ToSegment()
        {
            return _segmentMap[Kind];
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is Symbol && ((Symbol)obj).Name == Name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
