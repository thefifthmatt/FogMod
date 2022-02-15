using System;
using System.Linq;
using System.Collections.Generic;
using static SoulsIds.GameSpec;
using static FogMod.Util;
using static FogMod.AnnotationData;

namespace FogMod
{
    public class Graph
    {
        // All areas from config
        public Dictionary<string, Area> Areas { get; set; }
        // All entrances from config, by id
        public Dictionary<string, Entrance> EntranceIds { get; set; }
        // Item areas - either from config or from param lookup.
        // An item can be in a preexisting area after visiting a different area.
        // Sometimes these are defined as their own areas, but try to do something more automatic for DS3 handmaiden shops.
        public Dictionary<string, List<string>> ItemAreas { get; set; }

        // All nodes in the constructed graph
        public Dictionary<string, Node> Nodes { get; set; }
        // Start point, or asylum if unspecified
        public CustomStart Start { get; set; }
        // All sides (entrance id, logical area name) to skip outputting, if an optional area is inaccessible
        public List<(string, string)> Ignore { get; set; }
        // Filled in after connecting the graph, used by scaling.
        // For each area, the enemy health ratio and damage ratio.
        public Dictionary<string, (float, float)> AreaRatios { get; set; }

        public (Edge, Edge) AddNode(Side side, Entrance e, bool from, bool to)
        {
            string name = e?.FullName;
            string text = (e == null ? side.Text ?? (side.HasTag("hard") ? "hard skip" : $"in map") : e.Text);
            bool isFixed = e == null ? true : e.IsFixed;
            Edge exit = from ? new Edge { Expr = side.Expr, From = side.Area, Name = name, Text = text, IsFixed = isFixed, Side = side, Type = EdgeType.Exit } : null;
            Edge entrance = to ? new Edge { Expr = side.Expr, To = side.Area, Name = name, Text = text, IsFixed = isFixed, Side = side, Type = EdgeType.Entrance } : null;
            if (entrance != null)
            {
                entrance.Pair = exit;
                Nodes[side.Area].From.Add(entrance);
            }
            if (exit != null)
            {
                exit.Pair = entrance;
                Nodes[side.Area].To.Add(exit);
            }
            return (exit, entrance);
        }

        public void Connect(Edge exit, Edge entrance)
        {
            if (exit.To != null || entrance.From != null || exit.Link != null || entrance.Link != null) throw new Exception("Already matched");
            exit.To = entrance.To;
            entrance.From = exit.From;
            entrance.Link = exit;
            exit.Link = entrance;
            Expr combined;
            if (entrance.Expr == null) combined = exit.Expr;
            else if (exit.Expr == null) combined = entrance.Expr;
            else
            {
                combined = exit.Expr == entrance.Expr ? exit.Expr : new Expr(new List<Expr> { exit.Expr, entrance.Expr }, true).Simplify();
            }
            entrance.LinkedExpr = exit.LinkedExpr = combined;
            if (exit == entrance.Pair) return;
            if (exit.Pair != null)
            {
                if (exit.Pair.From != null || exit.Pair.Link != null) throw new Exception("Already matched pair");
                exit.Pair.From = exit.To;
            }
            if (entrance.Pair != null)
            {
                if (entrance.Pair.To != null || entrance.Pair.Link != null) throw new Exception("Already matched pair");
                entrance.Pair.To = entrance.From;
            }
            if (exit.Pair != null && entrance.Pair != null)
            {
                exit.Pair.Link = entrance.Pair;
                entrance.Pair.Link = exit.Pair;
                exit.Pair.LinkedExpr = entrance.Pair.LinkedExpr = combined;
            }
        }

        public void Disconnect(Edge exit, bool forPair = false)
        {
            Edge entrance = exit.Link;
            // Usually indicates partial connection bugs
            if (entrance == null) throw new Exception($"Can't disconnect {exit}{(forPair ? " as pair" : "")}");
            exit.Link = null;
            exit.To = null;
            entrance.Link = null;
            entrance.From = null;
            entrance.LinkedExpr = exit.LinkedExpr = null;
            if (!forPair && exit.Pair != null && entrance.Pair != null && exit.Pair != entrance)
            {
                Disconnect(entrance.Pair, true);
            }
        }

        public void SwapConnectedEdges(Edge oldExitEdge, Edge newEntranceEdge)
        {
            // If oldExitEdge is going from (main a) -> b and newEntranceEdge is going from c -> (main d), connect it so that a -> d. And also b -> c.
            // The new/old phrasing is mainly in the context of reaching a previously unreachable area.
            // Where old exit leads to doesn't matter, because it was already reachable, and where new exit comes from doesn't matter, because it was inaccessible anyway.
            Edge newEntrance = newEntranceEdge; // entrance edge
            Edge newExit = newEntranceEdge.Link;
            Edge oldExit = oldExitEdge; // exit edge
            Edge oldEntrance = oldExitEdge.Link;
            Disconnect(newExit);
            Disconnect(oldExit);
            // Ton of logic to deal with self edges
            if (newEntrance == newExit.Pair && oldEntrance == oldExit.Pair)
            {
                // Both are self edges for some strange reason, so just link them
                Connect(oldExit, newEntrance);
            }
            else if (newEntrance == newExit.Pair)
            {
                // Leave one of the old entrances or exits to self-link, to join old and new
                if (oldEntrance.Pair != null)
                {
                    Connect(oldEntrance.Pair, oldEntrance);
                    Connect(oldExit, newEntrance);
                }
                else if (oldExit.Pair != null)
                {
                    Connect(oldExit, oldExit.Pair);
                    Connect(newExit, oldEntrance);
                }
                else throw new Exception($"Bad seed: Can't find edge to self-link to reach {newEntrance}");
            }
            else if (oldEntrance == oldExit.Pair)
            {
                // Leave one of the new entrances to self-link, since at least the new exit will be linked to old. Or vice versa
                if (newEntrance.Pair != null)
                {
                    Connect(newEntrance.Pair, newEntrance);
                    Connect(newExit, oldEntrance);
                }
                else if (newExit.Pair != null)
                {
                    Connect(newExit, newExit.Pair);
                    Connect(oldExit, newEntrance);
                }
                else throw new Exception($"Bad seed: Can't find edge to self-link to reach {newEntrance}");
            }
            else
            {
                Connect(oldExit, newEntrance);
                Connect(newExit, oldEntrance);
            }
        }

        public void SwapConnectedAreas(string name1, string name2)
        {
            // Swap non-fixed paired edges between 1 and 2, so that entrances to 1 go to 2 instead
            Node node1 = Nodes[name1];
            Node node2 = Nodes[name2];
            for (int i = 0; i <= 1; i++)
            {
                bool unpaired = i == 0;
                List<Edge> entrances1 = node1.From.Where(e => !e.IsFixed && (e.Pair == null) == unpaired).ToList();
                List<Edge> entrances2 = node2.From.Where(e => !e.IsFixed && (e.Pair == null) == unpaired).ToList();
                // Zip in opposite order maybe, since an entrance is added to two adjacent areas together, to avoid vanilla gates
                entrances2.Reverse();
                for (int j = 0; j < Math.Min(entrances1.Count, entrances2.Count); j++)
                {
                    Edge entrance1 = entrances1[j];
                    Edge entrance2 = entrances2[j];
                    SwapConnectedEdges(entrance1.Link, entrance2);
                }
            }
        }

        public void Construct(RandomizerOptions opt, AnnotationData ann)
        {
            // Collect areas and items
            Areas = ann.Areas.ToDictionary(a => a.Name, a => a);
            ItemAreas = ann.KeyItems.ToDictionary(item => item.Name, item => new List<string>());
            // Some validation
            Expr getExpr(string cond)
            {
                Expr expr = ParseExpr(cond);
                if (expr == null) return null;
                foreach (string free in expr.FreeVars())
                {
                    if (!Areas.ContainsKey(free) && !ItemAreas.ContainsKey(free)) throw new Exception($"Condition {cond} has unknown variable {free}");
                }
                return expr;
            }
            foreach (Area area in ann.Areas)
            {
                if (area.To == null) continue;
                foreach (Side side in area.To)
                {
                    if (!Areas.ContainsKey(side.Area)) throw new Exception($"{area.Name} goes to nonexistent {side.Area}");
                    side.Expr = getExpr(side.Cond);
                }
            }
            EntranceIds = new Dictionary<string, Entrance>();
            foreach (Entrance e in ann.Entrances.Concat(ann.Warps))
            {
                string id =  e.FullName;
                if (EntranceIds.ContainsKey(id)) throw new Exception($"Duplicate id {id}");
                EntranceIds[id] = e;
                if (!e.HasTag("unused") && e.Sides().Count < 2) throw new Exception($"{e.FullName} has insufficient sides");
            }
            foreach (Entrance e in ann.Warps)
            {
                if (e.HasTag("unused")) continue;
                if (e.ASide == null || e.BSide == null) throw new Exception($"{e.FullName} warp missing both sides");
            }
            // Mark entrances as randomized or not
            Dictionary<string, List<string>> allText = new Dictionary<string, List<string>>();
            foreach (Entrance e in ann.Entrances)
            {
                if (e.HasTag("unused")) continue;
                if (opt.Game == FromGame.DS3)
                {
                    if (e.HasTag("norandom"))
                    {
                        e.IsFixed = true;
                    }
                    else if (e.HasTag("door"))
                    {
                        e.IsFixed = true;
                    }
                    else if (opt["lords"] && e.HasTag("kiln"))
                    {
                        e.IsFixed = true;
                    }
                    else if (!opt["dlc1"] && e.HasTag("dlc1"))
                    {
                        e.IsFixed = true;
                    }
                    else if (!opt["dlc2"] && e.HasTag("dlc2"))
                    {
                        e.IsFixed = true;
                    }
                    else if (!opt["boss"] && e.HasTag("boss"))
                    {
                        e.IsFixed = true;
                        if (opt["dumptext"]) AddMulti(allText, "boss", e.Text);
                    }
                    else if (!opt["pvp"] && e.HasTag("pvp"))
                    {
                        e.IsFixed = true;
                        if (opt["dumptext"]) AddMulti(allText, "pvp", e.Text);
                    }
                }
                else
                {
                    if (!opt["lordvessel"] && e.HasTag("lordvessel"))
                    {
                        e.Tags += " door";
                        e.DoorCond = "AND lordvessel kiln_start";
                        if (opt["dumptext"]) AddMulti(allText, "lordvessel", e.Text);
                    }
                    if (e.HasTag("door"))
                    {
                        e.IsFixed = true;
                    }
                    else if (opt["lords"] && e.Area == "kiln")
                    {
                        e.IsFixed = true;
                    }
                    else if (!opt["world"] && e.HasTag("world"))
                    {
                        e.IsFixed = true;
                        if (opt["dumptext"]) AddMulti(allText, "world", e.Text);
                    }
                    else if (!opt["boss"] && e.HasTag("boss"))
                    {
                        e.IsFixed = true;
                        if (opt["dumptext"]) AddMulti(allText, "boss", e.Text);
                    }
                    else if (!opt["minor"] && e.HasTag("pvp") && !e.HasTag("major"))
                    {
                        e.IsFixed = true;
                        if (opt["dumptext"]) AddMulti(allText, "minor", e.Text);
                    }
                    else if (!opt["major"] && e.HasTag("pvp") && e.HasTag("major"))
                    {
                        e.IsFixed = true;
                        if (opt["dumptext"]) AddMulti(allText, "major", e.Text);
                    }
                }
            }
            foreach (Entrance e in ann.Warps)
            {
                if (e.HasTag("highwall"))
                {
                    // This one is tricky. Enable it only if it would be a softlock otherwise
                    if (!opt["pvp"] && !opt["boss"])
                    {
                        e.TagList.Add("norandom");
                    }
                    else
                    {
                        e.TagList.Add("unused");
                    }
                }
                if (e.HasTag("unused")) continue;
                if (e.HasTag("norandom"))
                {
                    e.IsFixed = true;
                }
                else if (!opt["warp"])
                {
                    e.IsFixed = true;
                    if (opt["dumptext"]) AddMulti(allText, "warp", e.Text);
                }
                if (opt["lords"] && e.HasTag("kiln"))
                {
                    e.IsFixed = true;
                }
                else if (!opt["dlc1"] && e.HasTag("dlc1"))
                {
                    e.IsFixed = true;
                }
                else if (!opt["dlc2"] && e.HasTag("dlc2"))
                {
                    e.IsFixed = true;
                }
            }
            if (opt["dumptext"] && allText.Count > 0)
            {
                foreach (KeyValuePair<string, List<string>> entry in allText)
                {
                    Console.WriteLine(entry.Key);
                    foreach (string text in entry.Value)
                    {
                        Console.WriteLine($"- {text}");
                    }
                    Console.WriteLine();
                }
            }
            // Process connection metadata
            Ignore = new List<(string, string)>();
            foreach (Entrance e in ann.Entrances.Concat(ann.Warps))
            {
                foreach (Side side in e.Sides())
                {
                    if (!Areas.ContainsKey(side.Area)) throw new Exception($"{e.FullName} goes to nonexistent {side.Area}");
                    side.Expr = getExpr(side.Cond);
                    // Condition: if entrance is randomized, then don't include the unwarpable side
                    if (!e.IsFixed && side.ExcludeIfRandomized != null && !EntranceIds[side.ExcludeIfRandomized].IsFixed)
                    {
                        Ignore.Add((e.FullName, side.Area));
                            
                    }
                }
            }
            // Set up graph
            int areaCost(Area a)
            {
                // Different from DS1, where area is 1 by default.
                if (a.HasTag("trivial")) return 0;
                else if (a.HasTag("small")) return 1;
                else return 3;
            }
            Nodes = Areas.ToDictionary(e => e.Key, e => new Node
            {
                Area = e.Key,
                Cost = areaCost(e.Value),
                ScalingBase = (opt["dumpdist"] || opt["scalingbase"]) ? e.Value.ScalingBase : null,
            });
            foreach (Area area in ann.Areas)
            {
                if (area.To == null) continue;
                foreach (Side side in area.To)
                {
                    // It's good this temp flag exists but it's not used for anything
                    if (side.HasTag("temp")) continue;
                    if (side.HasTag("hard") && !opt["hard"]) continue;
                    if (side.HasTag("treeskip") && !opt["treeskip"]) continue;
                    if (side.HasTag("instawarp") && !opt["instawarp"]) continue;
                    // This adds an exit for first area and entrance for second area.
                    // If a shortcut, adds an entrance for first area and exit for second area.
                    Side self = new Side { Area = area.Name, Text = side.Text, Expr = side.Expr, Tags = side.Tags };
                    Side other = new Side { Area = side.Area, Text = side.Text, Tags = side.HasTag("hard") ? "hard" : null };
                    bool shortcut = side.HasTag("shortcut");
                    if (shortcut)
                    {
                        Expr crossExpr = Expr.Named(area.Name);
                        if (side.Expr != null) crossExpr = new Expr(new List<Expr> { crossExpr, side.Expr }, true).Simplify();
                        other.Expr = crossExpr;
                    }
                    Edge exit = AddNode(self, null, from: true, to: shortcut).Item1;
                    Edge entrance = AddNode(other, null, from: shortcut, to: true).Item2;
                    Connect(exit, entrance);
                }
            }
            Dictionary<Connection, List<(Edge, Edge)>> warpEdges = new Dictionary<Connection, List<(Edge, Edge)>>();
            foreach (Entrance e in ann.Warps)
            {
                // This adds an exit for first area and entrance for second area. Pairs done later, to avoid putting too much info in edges (cutscene and warp target)
                if (e.ASide.HasTag("temp") || e.HasTag("unused")) continue;
                Edge exit = AddNode(e.ASide, e, true, false).Item1;
                Edge entrance = AddNode(e.BSide, e, false, true).Item2;
                if (e.IsFixed)
                {
                    Connect(exit, entrance);
                }
                else if (!e.HasTag("unique"))
                {
                    AddMulti(warpEdges, new Connection(e.ASide.Area, e.BSide.Area), (exit, entrance));
                }
            }
            foreach (KeyValuePair<Connection, List<(Edge, Edge)>> entry in warpEdges)
            {
                if (entry.Value.Count != 2) throw new Exception($"Bidirectional warp expected for {entry.Key} - non-bidirectional should be marked unique");
                // Don't actually connect, just validate
                if (opt["unconnected"]) continue;
                (Edge exit1, Edge entrance1) = entry.Value[0];
                (Edge exit2, Edge entrance2) = entry.Value[1];
                if (exit1.From == exit2.From) throw new Exception($"Duplicate warp {exit1} and {exit2} - should be marked unique");
                if (exit1.From != entrance2.To) throw new Exception($"Internal error: warp {exit1} and {entrance2} not equivalent");
                exit1.Pair = entrance2;
                entrance2.Pair = exit1;
                exit2.Pair = entrance1;
                entrance1.Pair = exit2;
            }
            HashSet<Connection> doorConds = new HashSet<Connection>();
            foreach (Entrance e in ann.Entrances)
            {
                if (e.HasTag("unused")) continue;
                List<Side> sides = e.Sides();
                if (sides.Count == 1)
                {
                    if (e.HasTag("door")) throw new Exception($"{e.FullName} has one-sided door");
                    AddNode(sides[0], e, from: true, to: true);
                }
                else
                {
                    Side from = sides[0];
                    Side to = sides[1];
                    if (e.HasTag("door"))
                    {
                        // This adds exits and entrances on both sides.
                        // Tricky thing is calculating conditions.
                        Connection con = new Connection(from.Area, to.Area);
                        if (doorConds.Contains(con)) continue;
                        doorConds.Add(con);
                        Expr doorExpr = getExpr(e.DoorCond);
                        if (from.Expr != null || to.Expr != null) throw new Exception($"Door cond {doorExpr} and cond {from.Expr} {to.Expr} together for {e.FullName}");
                        from.Expr = from.HasTag("dnofts") ? (doorExpr == null ? Expr.Named(to.Area) : new Expr(new List<Expr> { doorExpr, Expr.Named(to.Area) }, true).Simplify()) : doorExpr;
                        to.Expr = to.HasTag("dnofts") ? (doorExpr == null ? Expr.Named(from.Area) : new Expr(new List<Expr> { doorExpr, Expr.Named(from.Area) }, true).Simplify()) : doorExpr;
                        Edge exit = AddNode(from, e, from: true, to: true).Item1;
                        Edge entrance = AddNode(to, e, from: true, to: true).Item2;
                        Connect(exit, entrance);
                    }
                    else if (e.IsFixed || !opt["unconnected"])
                    {
                        // This adds entrance/exit on first side and/or entrance/exit on second side.
                        Edge exit = Ignore.Contains((e.FullName, to.Area)) ? null : AddNode(to, e, from: true, to: true).Item1;
                        Edge entrance = Ignore.Contains((e.FullName, from.Area)) ? null : AddNode(from, e, from: true, to: true).Item2;
                        if (exit != null && entrance != null && e.IsFixed)
                        {
                            Connect(exit, entrance);
                        }
                    }
                    else
                    {
                        if (!Ignore.Contains((e.FullName, to.Area)))
                        {
                            AddNode(to, e, true, false);
                            AddNode(to, e, false, true);
                        }
                        if (!Ignore.Contains((e.FullName, from.Area)))
                        {
                            AddNode(from, e, true, false);
                            AddNode(from, e, false, true);
                        }
                    }
                }
            }
        }
        public class WarpPoint
        {
            // Pointers
            public int ID { get; set; }
            public string Map { get; set; }
            // Warp away
            public int Action { get; set; }
            public int Cutscene { get; set; }
            // If going from B side to A side
            public bool ToFront { get; set; }
            // The destination height, usually 0
            public float Height { get; set; }
            // Warp destination
            public int Player { get; set; }
            public int Region { get; set; }
        }
        public class Connection
        {
            public string A { get; set; }
            public string B { get; set; }
            public Connection(string a, string b)
            {
                this.A = a.CompareTo(b) < 0 ? a : b;
                this.B = a.CompareTo(b) < 0 ? b : a;
            }
            public override bool Equals(object obj) => obj is Connection o && Equals(o);
            public bool Equals(Connection o) => A == o.A && B == o.B;
            public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();
            public override string ToString() => $"({A}, {B})";
        }
        public class Node
        {
            public string Area { get; set; }
            public int Cost { get; set; }
            public string ScalingBase { get; set; }
            public List<string> Items = new List<string>();
            // To mostly other nodes
            public List<Edge> To = new List<Edge>();
            // All edges leading here.
            public List<Edge> From = new List<Edge>();
        }
        // Outgoing edge
        public class Edge
        {
            public EdgeType Type { get; set; }
            // Optional while graph is partly unconnected
            public string From { get; set; }
            public string To { get; set; }
            // Condition for getting to/from area
            public Expr Expr { get; set; }
            // If link is present, exprs of both sides
            public Expr LinkedExpr { get; set; }
            // Can this edge be edited or is it hardcoded?
            public bool IsFixed { get; set; }
            // Name of entrance/warp
            public string Name { get; set; }
            public Side Side { get; set; }
            // Informational
            public string Text { get; set; }
            // If applicable, the other way for this edge (exit if entrance and vice versa).
            // Used to keep edges bidirectional.
            private Edge pair;
            public Edge Pair
            {
                get => pair;
                set
                {
                    if (value != null && Type == value.Type) throw new Exception($"Cannot pair {this} and {value}");
                    pair = value;
                }
            }
            // An edge belonging to another node with the same from and to as this node.
            private Edge link;
            public Edge Link
            {
                get => link;
                set
                {
                    if (value != null && Type == value.Type) throw new Exception($"Cannot link {this} and {value}");
                    link = value;
                }
            }
            public override string ToString() => $"Edge[Name={Name}, From={From}, To={To}, Expr={Expr}]";
        }
        public enum EdgeType { Unknown, Exit, Entrance }
    }
}
