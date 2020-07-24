using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using static FogMod.Graph;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public class AnnotationData
    {
        public List<ConfigAnnotation> Options { get; set; }
        public float HealthScaling { get; set; }
        public float DamageScaling { get; set; }
        public List<Area> Areas { get; set; }
        public List<Item> KeyItems { get; set; }
        public List<Entrance> Warps { get; set; }
        public List<Entrance> Entrances { get; set; }
        public List<GameObject> Objects { get; set; }
        public List<CustomStart> CustomStarts { get; set; }
        public Dictionary<string, float> DefaultCost { get; set; }
        public List<EnemyCol> Enemies { get; set; }
        public Dictionary<int, string> LotLocations { get; set; }
        public Dictionary<string, int> DefaultFlagCols { get; set; }
        [YamlIgnore]
        public FogLocations Locations { get; set; }

        [YamlIgnore]
        public Dictionary<string, MapSpec> Specs { get; set; }
        [YamlIgnore]
        public Dictionary<string, MapSpec> NameSpecs { get; set; }

        public void SetGame(FromGame game)
        {
            List<MapSpec> allSpecs = game == FromGame.DS3 ? DS3Specs : DS1Specs;
            Specs = allSpecs.ToDictionary(s => s.Map, s => s);
            NameSpecs = allSpecs.ToDictionary(s => s.Name, s => s);
        }

        public class ConfigAnnotation
        {
            // Not used rn... currently just for informational purposes
            public string Opt { get; set; }
            public string TrueOpt { get; set; }
            public void UpdateOptions(RandomizerOptions options)
            {
                if (Opt != null)
                {
                    options[Opt] = false;
                }
                if (TrueOpt != null)
                {
                    options[TrueOpt] = true;
                }
            }
        }
        // Abuse OOP in a bad way
        public abstract class Taggable
        {
            public string Tags {
                get => tags;
                set
                {
                    tags = value;
                    if (tags == null)
                    {
                        TagList = new List<string>();
                    }
                    else
                    {
                        TagList = Tags.Split(' ').ToList();
                    }
                }
            }
            [YamlIgnore]
            private string tags = null;
            [YamlIgnore]
            public List<string> TagList = new List<string>();
            public bool HasTag(string tag) => TagList.Contains(tag);
        }
        public class Area : Taggable
        {
            public string Name { get; set; }
            public string Text { get; set; }
            public string ScalingBase { get; set; }
            public int BossTrigger { get; set; }
            public int DefeatFlag { get; set; }
            public int TrapFlag { get; set; }
            public List<Side> To { get; set; }
        }
        public class EnemyCol
        {
            public string Col { get; set; }
            public string Area { get; set; }
            public List<string> Includes { get; set; }
        }
        public class Item : Taggable
        {
            public string Name { get; set; }
            public string ID { get; set; }
            public string Area { get; set; }
        }
        public class CustomStart
        {
            public string Name { get; set; }
            public string Area { get; set; }
            public string Respawn { get; set; }
        }
        public class Entrance : Taggable
        {
            // Part name
            public string Name { get; set; }
            // Entity id
            public int ID { get; set; }
            // Map
            public string Area { get; set; }
            // Display text
            public string Text { get; set; }
            // Internal comment
            public string Comment { get; set; }
            // If tagged as a door, the condition for opening the door
            public string DoorCond { get; set; }
            // How much to raise the fog gate's y coordinate to make it level with the ground, for warp points and trigger regions
            public float AdjustHeight { get; set; }
            // Sides
            public Side ASide { get; set; }
            public Side BSide { get; set; }
            [YamlIgnore]
            public bool IsFixed { get; set; }
            [YamlIgnore]
            public string FullName => Area + "_" + ID.ToString();  // Name == null ? ID.ToString() : Area + "_" + Name;
            public List<Side> Sides()
            {
                List<Side> sides = new List<Side>();
                if (ASide != null) sides.Add(ASide);
                if (BSide != null) sides.Add(BSide);
                return sides;
            }
        }
        public class Side : Taggable
        {
            public string Area { get; set; }
            // Documentation, usually lowercase gerund or noun
            public string Text { get; set; }
            // In DS1, flag to escape
            public int Flag { get; set; }
            // In DS1, if escape flag is set, ignore until this flag normally traps the player in the arena
            public int TrapFlag { get; set; }
            // In DS1, flag required to enter, if region is currently inaccessible
            public int EntryFlag { get; set; }
            // To set before warping
            public int BeforeWarpFlag { get; set; }
            // In DS1, region to trigger a boss fight, if needs to be applied to other fog gate entrances as well
            public int BossTrigger { get; set; }
            // In DS1, an additional trigger region to add
            public string BossTriggerArea { get; set; }
            // In DS1, flag to use to initiate the warp
            public int WarpFlag { get; set; }
            // DS3 variants of Flag/TrapFlag/BossTrigger, but applied to the names of areas with these flags defined, not the ids themselves.
            public string BossDefeatName { get; set; }
            public string BossTrapName { get; set; }
            public string BossTriggerName { get; set; }
            // Cutscene to play, if the destination is a warp region. This is unused, mainly for information.
            public int Cutscene { get; set; }
            // Condition for traversing to/from this point and the given area
            public string Cond { get; set; }
            // A custom location to put the warp instead of calculated from the entrance id
            public string CustomWarp { get; set; }
            // In DS1, custom fog gate width, for wider fog gates
            public int CustomActionWidth { get; set; }
            // The last col stepped on before warping. Used for connect cols (currently not used)
            public string Col { get; set; }
            // In DS1, pre-existing trigger region, if the automatic one doesn't work
            public int ActionRegion { get; set; }
            // Don't include the side as a random entrance/exit if the given entrance is not randomized either
            public string ExcludeIfRandomized { get; set; }
            // The map to add the warp into, if different from the entrance's map
            public string DestinationMap { get; set; }
            // Height adjust on only this side
            public float AdjustHeight { get; set; }
            [YamlIgnore]
            public Expr Expr { get; set; }
            [YamlIgnore]
            public WarpPoint Warp { get; set; }
        }
        public class GameObject : Taggable
        {
            public string Area { get; set; }
            public string ID { get; set; }
            public string Text { get; set; }
        }

        public static readonly List<MapSpec> DS1Specs = new List<MapSpec>
        {
            MapSpec.Of("m10_00_00_00", "depths", 1400, 1420),
            MapSpec.Of("m10_01_00_00", "parish", 1403, 1421),
            MapSpec.Of("m10_02_00_00", "firelink", 0, 0),
            MapSpec.Of("m11_00_00_00", "paintedworld", 1600, 1604),
            MapSpec.Of("m12_00_00_01", "darkroot", 2900, 2908),
            MapSpec.Of("m12_01_00_00", "dlc", 2909, 2920),
            MapSpec.Of("m13_00_00_00", "catacombs", 3951, 3954),
            MapSpec.Of("m13_01_00_00", "totg", 3961, 3964),
            MapSpec.Of("m13_02_00_00", "greathollow", 3970, 3971),
            MapSpec.Of("m14_00_00_00", "blighttown", 4950, 4956),
            MapSpec.Of("m14_01_00_00", "demonruins", 4960, 4973),
            MapSpec.Of("m15_00_00_00", "sens", 5150, 5153),
            MapSpec.Of("m15_01_00_00", "anorlondo", 5860, 5871),
            MapSpec.Of("m16_00_00_00", "newlondo", 6601, 6606),
            MapSpec.Of("m17_00_00_00", "dukes", 7900, 7908),
            MapSpec.Of("m18_00_00_00", "kiln", 8050, 8051),
            MapSpec.Of("m18_01_00_00", "asylum", 8950, 8953),
        };
        public static readonly List<MapSpec> DS3Specs = new List<MapSpec>
        {
            MapSpec.Of("m30_00_00_00", "highwall", 400, 402),
            MapSpec.Of("m30_01_00_00", "lothric", 400, 402),
            MapSpec.Of("m31_00_00_00", "settlement", 400, 402),
            MapSpec.Of("m32_00_00_00", "archdragon", 400, 402),
            MapSpec.Of("m33_00_00_00", "farronkeep", 400, 402),
            MapSpec.Of("m34_01_00_00", "archives", 400, 402),
            MapSpec.Of("m35_00_00_00", "cathedral", 400, 402),
            MapSpec.Of("m37_00_00_00", "irithyll", 400, 402),
            MapSpec.Of("m38_00_00_00", "catacombs", 400, 402),
            MapSpec.Of("m39_00_00_00", "dungeon", 400, 402),
            MapSpec.Of("m40_00_00_00", "firelink", 400, 402),
            MapSpec.Of("m41_00_00_00", "kiln", 400, 402),
            MapSpec.Of("m45_00_00_00", "ariandel", 400, 402),
            MapSpec.Of("m50_00_00_00", "dregheap", 400, 402),
            MapSpec.Of("m51_00_00_00", "ringedcity", 400, 402),
            MapSpec.Of("m51_01_00_00", "filianore", 400, 402),
        };

        public class MapSpec
        {
            public static MapSpec Of(string Map, string Name, int Start, int End) => new MapSpec { Map = Map, Name = Name, Start = Start, End = End };
            public string Map { get; set; }
            public string Name { get; set; }
            public int Start { get; set; }
            public int End { get; set; }
        }

        public static Expr ParseExpr(string s)
        {
            // Non-recursive for now... keep it simple
            if (s == null) return null;
            string[] words = s.Split(' ');
            if (words.Length == 1) return Expr.Named(words[0]);
            else if (words.Length > 1 && words[0] == "AND") return new Expr(words.Skip(1).Select(w => Expr.Named(w)).ToList(), true);
            else if (words.Length > 1 && words[0] == "OR") return new Expr(words.Skip(1).Select(w => Expr.Named(w)).ToList(), false);
            else throw new Exception($"Unknown cond {s}");
        }
        public class Expr
        {
            public static readonly Expr TRUE = new Expr(new List<Expr>(), true, null);
            public static readonly Expr FALSE = new Expr(new List<Expr>(), false, null);
            private readonly List<Expr> exprs;
            private readonly bool every;
            private readonly string name;
            public Expr(List<Expr> exprs, bool every = true, string name = null)
            {
                if (exprs.Count() > 0 && name != null) throw new Exception("Incorrect construction");
                this.exprs = exprs;
                this.every = every;
                this.name = name;
            }
            public static Expr Named(string name)
            {
                return new Expr(new List<Expr>(), true, name);
            }
            public bool IsTrue()
            {
                return name == null && exprs.Count() == 0 && every;
            }
            public bool IsFalse()
            {
                return name == null && exprs.Count() == 0 && !every;
            }
            public SortedSet<string> FreeVars()
            {
                if (name != null)
                {
                    return new SortedSet<string> { name };
                }
                return new SortedSet<string>(exprs.SelectMany(e => e.FreeVars()));
            }
            public bool Needs(string check)
            {
                if (check == name)
                {
                    return true;
                }
                if (every)
                {
                    return exprs.Any(e => e.Needs(check));
                }
                else
                {
                    return exprs.All(e => e.Needs(check));
                }
            }
            public Expr Substitute(Dictionary<string, Expr> config)
            {
                if (name != null)
                {
                    if (config.ContainsKey(name))
                    {
                        return config[name];
                    }
                    return this;
                }
                return new Expr(exprs.Select(e => e.Substitute(config)).ToList(), every);
            }
            public Expr Flatten(Func<string, IEnumerable<string>> nameMapper)
            {
                if (name != null)
                {
                    // public Expr(List<Expr> exprs, bool every=true, string name=null)
                    return new Expr(nameMapper(name).Select(n => Expr.Named(n)).ToList(), true);
                }
                return null;
            }
            public int Count(Func<string, int> func)
            {
                if (name != null)
                {
                    return func(name);
                }
                IEnumerable<int> subcounts = exprs.Select(e => e.Count(func));
                return every ? subcounts.Sum() : subcounts.Max();
            }
            public (List<string>, float) Cost(Func<string, float> cost)
            {
                if (name != null)
                {
                    return (new List<string> { name }, cost(name));
                }
                else if (exprs.Count == 0)
                {
                    return (new List<string>(), 0);
                }
                IEnumerable<(List<string>, float)> subcosts = exprs.Select(e => e.Cost(cost));
                return every ? subcosts.Aggregate((c1, c2) => (c1.Item1.Concat(c2.Item1).ToList(), c1.Item2 + c2.Item2)) : subcosts.OrderBy(c => c.Item2).First();
            }
            public Expr Simplify()
            {
                if (name != null)
                {
                    return this;
                }
                List<Expr> newExprs = new List<Expr>();
                HashSet<string> seen = new HashSet<string>();
                foreach (Expr e in exprs)
                {
                    Expr expr = e.Simplify();
                    if (expr.name != null)
                    {
                        if (seen.Contains(expr.name)) continue;
                        seen.Add(expr.name);
                        newExprs.Add(expr);
                    }
                    else if (every == expr.every)
                    {
                        newExprs.AddRange(expr.exprs);
                    }
                    else
                    {
                        if (expr.exprs.Count() == 0)
                        {
                            // false in AND condition, or true in OR condition, overrides everything else
                            return expr.every ? TRUE : FALSE;
                        }
                        newExprs.Add(expr);
                    }
                }
                if (newExprs.Count() == 1)
                {
                    return newExprs[0];
                }
                return new Expr(newExprs, every);
            }
            public override string ToString()
            {
                if (name != null)
                {
                    return name;
                }
                if (exprs.Count() == 0)
                {
                    return every ? "true" : "false";
                }
                if (every)
                {
                    return "(" + string.Join(" AND ", exprs) + ")";
                }
                else
                {
                    return "(" + string.Join(" OR ", exprs) + ")";
                }
            }
        }

        // Separate config for locations of items/enemies other than fog gates
        public class FogLocations
        {
            public List<KeyItemLoc> Items = new List<KeyItemLoc>();
            public List<EnemyLoc> Enemies = new List<EnemyLoc>();
        }
        public class KeyItemLoc
        {
            // Item randomizer key for this location, just for use in generating it
            public string Key { get; set; }
            // Explanatory info
            public List<string> DebugText { get; set; }
            // The area the item appears. May include other areas or dependent items, which are space-separated.
            public string Area { get; set; }
            // Space-separated list of base lot ids.
            public string Lots { get; set; }
            // Space-separated list of shop ids.
            public string Shops { get; set; }
        }
        public class EnemyLoc
        {
            // 
            public string ID { get; set; }
            public string DebugText { get; set; }
            public string Area { get; set; }
        }
    }
}
