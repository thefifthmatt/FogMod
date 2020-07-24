using System;
using System.Collections.Generic;
using System.Linq;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public class RandomizerOptions
    {
        private SortedDictionary<string, bool> opt = new SortedDictionary<string, bool> { { "v4", true } };

        // TODO: Make this work
        public RandomizerOptions Copy()
        {
            return new RandomizerOptions
            {
                opt = new SortedDictionary<string, bool>(opt),
                Seed = Seed,
                Game = Game,
            };
        }

        public bool this[string name]
        {
            get { return opt.ContainsKey(name) ? opt[name] : false; }
            set { opt[name] = value; }
        }
        public string Language = "ENGLISH";
        public int Seed { get; set; }
        public FromGame Game { get; set; }
        public uint DisplaySeed => (uint)Seed;
        public SortedSet<string> GetEnabled()
        {
            return new SortedSet<string>(opt.Where(e => e.Value).Select(e => e.Key));
        }
        public override string ToString() => $"{string.Join(" ", GetEnabled())} {DisplaySeed}";
        public string ConfigHash() => (JavaStringHash($"{string.Join(" ", GetEnabled())}") % 99999).ToString().PadLeft(5, '0');

        public static uint JavaStringHash(string s)
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
