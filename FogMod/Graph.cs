using System;
using System.Linq;
using System.Collections.Generic;
using static FogMod.Util;
using static FogMod.AnnotationData;

namespace FogMod
{
    public class Graph
    {
        public Dictionary<string, Area> areas;
        public List<(string, string)> ignore;
        public Dictionary<string, Node> graph;
        public Dictionary<string, float> areaRatios;
        public Dictionary<string, Entrance> entranceIds;

        public (Edge, Edge) addNode(Side side, Entrance e, bool from, bool to)
        {
            string name = e?.EdgeName;
            string text = (e == null ? (side.HasTag("hard") ? "hard jump" : "in map") : e.Text);
            bool isFixed = e == null ? true : e.IsFixed;
            Edge exit = from ? new Edge { Expr = side.Expr, From = side.Area, Name = name, Text = text, IsFixed = isFixed, Side = side, Type = EdgeType.Exit } : null;
            Edge entrance = to ? new Edge { Expr = side.Expr, To = side.Area, Name = name, Text = text, IsFixed = isFixed, Side = side, Type = EdgeType.Entrance } : null;
            if (entrance != null)
            {
                entrance.Pair = exit;
                graph[side.Area].From.Add(entrance);
            }
            if (exit != null)
            {
                exit.Pair = entrance;
                graph[side.Area].To.Add(exit);
            }
            return (exit, entrance);
        }
        public void connect(Edge exit, Edge entrance)
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
        public void disconnect(Edge exit, bool forPair = false)
        {
            Edge entrance = exit.Link;
            // Usually indicates partial connection bugs
            if (entrance == null) throw new Exception("Not connected");
            exit.Link = null;
            exit.To = null;
            entrance.Link = null;
            entrance.From = null;
            entrance.LinkedExpr = exit.LinkedExpr = null;
            if (!forPair && exit.Pair != null && entrance.Pair != null && exit.Pair != entrance)
            {
                disconnect(entrance.Pair, true);
            }
        }

        public void Construct(RandomizerOptions opt, Annotations ann)
        {
            areas = ann.Areas.ToDictionary(a => a.Name, a => a);
            foreach (Area area in ann.Areas)
            {
                if (area.To == null) continue;
                foreach (Side side in area.To)
                {
                    if (!areas.ContainsKey(side.Area)) throw new Exception($"{area.Name} goes to nonexistent {side.Area}");
                    side.Expr = ParseExpr(side.Cond);
                }
            }
            // Collect named entrances           
            entranceIds = new Dictionary<string, Entrance>();
            foreach (Entrance e in ann.Entrances.Concat(ann.Warps))
            {
                string id = e.EdgeName;
                if (entranceIds.ContainsKey(id)) throw new Exception($"Duplicate id {id}");
                entranceIds[id] = e;
                if (!e.HasTag("unused") && e.Sides().Count < 2) throw new Exception($"{e.Name} has insufficient sides");
            }
            foreach (Entrance e in ann.Warps)
            {
                if (e.HasTag("unused")) continue;
                if (e.ASide == null || e.BSide == null) throw new Exception($"{e.Name} warp missing both sides");
            }
            // Mark entrances as randomized or not
            Dictionary<string, List<string>> allText = new Dictionary<string, List<string>>();
            foreach (Entrance e in ann.Entrances)
            {
                if (e.HasTag("unused")) continue;
                if (!opt["lordvessel"] && e.HasTag("lordvessel"))
                {
                    e.Tags += " door";
                    e.DoorCond = "AND anorlondo_gwynevere kiln_start";
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
            foreach (Entrance e in ann.Warps)
            {
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
            ignore = new List<(string, string)>();
            foreach (Entrance e in ann.Entrances.Concat(ann.Warps))
            {
                foreach (Side side in e.Sides())
                {
                    if (!areas.ContainsKey(side.Area)) throw new Exception($"{e.Name} goes to nonexistent {side.Area}");
                    side.Expr = ParseExpr(side.Cond);
                    // Condition: if entrance is randomized, then don't include the unwarpable side
                    if (!e.IsFixed && side.ExcludeIfRandomized != null && !entranceIds[side.ExcludeIfRandomized].IsFixed)
                    {
                        ignore.Add((e.Name, side.Area));
                    }
                }
            }
            // Set up graph
            graph = areas.ToDictionary(e => e.Key, e => new Node
            {
                Area = e.Key,
                Cost = e.Value.HasTag("trivial") ? 0 : (e.Value.HasTag("boss") ? 3 : 1),
                ScalingBase = opt["dumpdist"] ? e.Value.ScalingBase : null,
            });
            foreach (Area area in ann.Areas)
            {
                if (area.To == null) continue;
                foreach (Side side in area.To)
                {
                    // It's good this temp flag exists but it's not used for anything
                    if (side.HasTag("temp")) continue;
                    if (side.HasTag("hard") && !opt["hard"]) continue;
                    // This adds an exit for first area and entrance for second area.
                    // If a shortcut, adds an entrance for first area and exit for second area.
                    Side self = new Side { Area = area.Name, Expr = side.Expr, Tags = side.Tags };
                    Side other = new Side { Area = side.Area, Tags = side.HasTag("hard") ? "hard" : null };
                    bool shortcut = side.HasTag("shortcut");
                    if (shortcut)
                    {
                        Expr crossExpr = Expr.Named(area.Name);
                        if (side.Expr != null) crossExpr = new Expr(new List<Expr> { crossExpr, side.Expr }, true).Simplify();
                        other.Expr = crossExpr;
                    }
                    Edge exit = addNode(self, null, from: true, to: shortcut).Item1;
                    Edge entrance = addNode(other, null, from: shortcut, to: true).Item2;
                    connect(exit, entrance);
                }
            }
            Dictionary<Connection, List<(Edge, Edge)>> warpEdges = new Dictionary<Connection, List<(Edge, Edge)>>();
            foreach (Entrance e in ann.Warps)
            {
                // This adds an exit for first area and entrance for second area. Pairs done later, to avoid putting too much info in edges (cutscene and warp target)
                if (e.ASide.HasTag("temp") || e.HasTag("unused")) continue;
                Edge exit = addNode(e.ASide, e, true, false).Item1;
                Edge entrance = addNode(e.BSide, e, false, true).Item2;
                if (e.IsFixed)
                {
                    connect(exit, entrance);
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
                    if (e.HasTag("door")) throw new Exception($"{e.Name} has one-sided door");
                    addNode(sides[0], e, from: true, to: true);
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
                        Expr doorExpr = ParseExpr(e.DoorCond);
                        if (from.Expr != null || to.Expr != null) throw new Exception($"Door cond {doorExpr} and cond {from.Expr} {to.Expr} together for {e.Name}");
                        from.Expr = from.HasTag("dnofts") ? (doorExpr == null ? Expr.Named(to.Area) : new Expr(new List<Expr> { doorExpr, Expr.Named(to.Area) }, true).Simplify()) : doorExpr;
                        to.Expr = to.HasTag("dnofts") ? (doorExpr == null ? Expr.Named(from.Area) : new Expr(new List<Expr> { doorExpr, Expr.Named(from.Area) }, true).Simplify()) : doorExpr;
                        Edge exit = addNode(from, e, from: true, to: true).Item1;
                        Edge entrance = addNode(to, e, from: true, to: true).Item2;
                        connect(exit, entrance);
                    }
                    else if (e.IsFixed || !opt["unconnected"])
                    {
                        // This adds entrance/exit on first side and/or entrance/exit on second side.
                        Edge exit = ignore.Contains((e.Name, to.Area)) ? null : addNode(to, e, from: true, to: true).Item1;
                        Edge entrance = ignore.Contains((e.Name, from.Area)) ? null : addNode(from, e, from: true, to: true).Item2;
                        if (exit != null && entrance != null && e.IsFixed)
                        {
                            connect(exit, entrance);
                        }
                    }
                    else
                    {
                        if (!ignore.Contains((e.Name, to.Area)))
                        {
                            addNode(to, e, true, false);
                            addNode(to, e, false, true);
                        }
                        if (!ignore.Contains((e.Name, from.Area)))
                        {
                            addNode(from, e, true, false);
                            addNode(from, e, false, true);
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
