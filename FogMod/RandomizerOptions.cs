using System;
using System.Collections.Generic;
using System.Linq;

namespace FogMod
{
    public class RandomizerOptions
    {
        private SortedDictionary<string, bool> opt = new SortedDictionary<string, bool> { { "v1", true } };

        public RandomizerOptions Copy()
        {
            return new RandomizerOptions
            {
                opt = new SortedDictionary<string, bool>(opt)
            };
        }

        public bool this[string name]
        {
            get { return opt.ContainsKey(name) ? opt[name] : false; }
            set { opt[name] = value; }
        }
        public int Seed { get; set; }
        public uint DisplaySeed => (uint)Seed;
        public SortedSet<string> GetEnabled()
        {
            return new SortedSet<string>(opt.Where(e => e.Value).Select(e => e.Key));
        }
        public override string ToString() => $"{string.Join(" ", GetEnabled())} {DisplaySeed}";
        public string ConfigHash() => (JavaStringHash($"{string.Join(" ", GetEnabled())}") % 99999).ToString().PadLeft(5, '0');
        private static uint JavaStringHash(string s)
        {
            unchecked
            {
                uint hash = 0;
                foreach (char c in s)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
    }
}
