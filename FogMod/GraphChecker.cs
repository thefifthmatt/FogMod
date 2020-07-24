using System;
using System.Collections.Generic;
using System.Linq;
using static FogMod.Util;
using static FogMod.AnnotationData;
using static FogMod.Graph;

namespace FogMod
{
    public class GraphChecker
    {
        public class CheckRecord
        {
            public Dictionary<string, NodeRecord> Records { get; set; }
            public List<string> Unvisited { get; set; }
        }
        public class NodeRecord
        {
            public string Area { get; set; }
            public float Dist { get; set; }
            public List<string> Visited = new List<string>();
            public Dictionary<Edge, float> InEdge = new Dictionary<Edge, float>();
        }
        public CheckRecord Check(RandomizerOptions opt, Graph g, string start)
        {
            Dictionary<string, Node> graph = g.Nodes;
            foreach (Node node in graph.Values)
            {
                foreach (Edge edge in node.To)
                {
                    if (edge.Link == null) throw new Exception($"Unlinked {edge} leaving {node.Area}");
                }
                foreach (Edge edge in node.From)
                {
                    if (edge.Link == null) throw new Exception($"Unlinked {edge} entering {node.Area}");
                }
            }
            Dictionary<string, Expr> config = new Dictionary<string, Expr>();
            HashSet<string> visited = new HashSet<string>();
            void visit(string area)
            {
                if (!graph.ContainsKey(area)) throw new Exception($"Unknown area to visit {area}");
                visited.Add(area);
                config[area] = Expr.TRUE;
                // Items can be in a weird chain depending on other items, so do this iterative approach in lieu of actual recursive dependencies
                foreach (string item in graph[area].Items)
                {
                    List<string> reqAreas = g.ItemAreas[item];
                    if (reqAreas.All(a => visited.Contains(a)))
                    {
                        config[item] = Expr.TRUE;
                    }
                }
            }
            HashSet<Edge> seenEdge = new HashSet<Edge>();
            Dictionary<string, NodeRecord> recs = new Dictionary<string, NodeRecord>();
            visit(start);
            recs[start] = new NodeRecord { Area = start, Dist = 0 };
            bool lastLoop = false;
            Dictionary<Edge, float> duplicatePaths = new Dictionary<Edge, float>();
            Dictionary<string, int> areaCost = graph.Values.ToDictionary(n => n.Area, n => n.Cost);
            Dictionary<string, float> extraAreaCost = graph.Values.ToDictionary(n => n.Area, n => 0f);
            List<string> getAreas(string dep)
            {
                return g.ItemAreas.TryGetValue(dep, out List<string> itemAreas) ? itemAreas : new List<string> { dep };
            }
            bool getDepRecord(string dep, out NodeRecord nr)
            {
                nr = null;
                if (g.ItemAreas.ContainsKey(dep))
                {
                    List<string> itemAreas = getAreas(dep);
                    if (itemAreas.Count == 1) return recs.TryGetValue(itemAreas[0], out nr);
                    // For items, make up a node record on the spot, since this method is only used for analyzing dependencies.
                    // These could be areas in their own right, but, there is a lot of them, so maybe this will work. And most of them only depend on firelink, which should be pretty early.
                    nr = new NodeRecord { Area = dep };
                    foreach (string itemArea in itemAreas)
                    {
                        if (!recs.TryGetValue(itemArea, out NodeRecord subNr)) return false;
                        nr.Dist = Math.Max(nr.Dist, subNr.Dist);
                        nr.Visited.AddRange(subNr.Visited);
                    }
                    nr.Visited = nr.Visited.Distinct().ToList();  // ?
                    return true;
                }
                return recs.TryGetValue(dep, out nr);
            }
            float addEdge(Edge e, string scalingBase)
            {
                if (!recs.TryGetValue(e.From, out NodeRecord from)) throw new Exception($"Internal error: {e} cannot calculate distance");
                if (!recs.TryGetValue(e.To, out NodeRecord to))
                {
                    to = recs[e.To] = new NodeRecord { Area = e.To };
                }
                List<string> visitedCand;
                if (e.LinkedExpr == null)
                {
                    visitedCand = from.Visited.ToList();
                    if (e.From != e.To) visitedCand.Add(e.From);
                }
                else
                {
                    HashSet<string> preVisit = new HashSet<string>(from.Visited);
                    List<string> depVisit = e.LinkedExpr.Cost(area => getDepRecord(area, out NodeRecord nr) ? nr.Dist : 10000000).Item1;
                    // Is this correct? Otherwise only conds are looked at, not the from area
                    depVisit.Add(from.Area);
                    foreach (string dep in depVisit)
                    {
                        if (!getDepRecord(dep, out NodeRecord nr)) throw new Exception($"Dependency in edge {e} not visited: {dep}");
                        // Console.WriteLine($"for adding {e} dep {dep}, adding [{string.Join(",", getAreas(dep))}] and [{string.Join(",", nr.Visited)}]");
                        preVisit.UnionWith(getAreas(dep));
                        preVisit.UnionWith(nr.Visited);
                    }
                    visitedCand = preVisit.ToList();
                }
                float candDist = visitedCand.Select(c => areaCost[c] + extraAreaCost[c]).Sum();
                if ((to.Dist == 0 && to.Area != start) || candDist < to.Dist)
                {
                    to.Dist = candDist;
                    to.Visited = visitedCand;
                    if (scalingBase != null)
                    {
                        if (!recs.TryGetValue(scalingBase, out NodeRecord scale)) throw new Exception($"Internal error: {e} can't find scaling base {scale}");
                        float scaleDist = scale.Dist;
                        // Allow this solely in the case of ToTG being given too high a ranking, given initial difficulty spike
                        // if (to.Dist > scaleDist) throw new Exception($"Calculated distance {to.Dist} of {e} greater than scaling base {scalingBase} at {scaleDist}; scaling base not needed");
                        extraAreaCost[to.Area] = scaleDist - to.Dist;
                        to.Dist = scaleDist;
                        Console.WriteLine($"TTT Setting scaling base of {to.Area} to {scalingBase} {scaleDist}");
                    }
                }
                return candDist;
            }
            while (true)
            {
                if (opt["explain"]) Console.WriteLine("---");
                int diff = visited.Count;
                foreach (string loc in visited.OrderBy(l => recs[l].Dist).ToList())
                {
                    Node node = graph[loc];
                    // Implicit condition on every edge
                    foreach (Edge edge in node.To)
                    {
                        // Ideally shouldn't happen
                        if (edge.To == null) continue;
                        // This won't work if one-way is paired with two-way in the future
                        if (seenEdge.Contains(edge) || seenEdge.Contains(edge.Link.Pair)) continue;
                        if (edge.LinkedExpr != null)
                        {
                            Expr simp = edge.LinkedExpr.Substitute(config).Simplify();
                            if (!simp.IsTrue())
                            {
                                if (lastLoop && opt["explain"]) Console.WriteLine($"Missing: {loc} -> {edge.To}. Condition {edge.LinkedExpr} -> {simp}");
                                continue;
                            }
                        }
                        Node toNode = graph[edge.To];
                        if (toNode.ScalingBase != null && !visited.Contains(toNode.ScalingBase))
                        {
                            if (lastLoop && opt["explain"]) Console.WriteLine($"Missing: {loc} -> {edge.To}. Condition {toNode.ScalingBase} for scaling");
                            continue;
                        }
                        float dist = addEdge(edge, toNode.ScalingBase);
                        seenEdge.Add(edge);
                        recs[edge.To].InEdge[edge] = dist;
                        if (visited.Contains(edge.To))
                        {
                            if (edge.Name != null && opt["explain"]) Console.WriteLine($"    Additional path: {loc} -> {edge.To} ({dist}) - {edge.Text} -> {(edge.Link == null ? "" : edge.Link.Text)}");
                        }
                        else
                        {
                            visit(edge.To);
                            if (opt["explain"]) Console.WriteLine($"{loc} -> {edge.To} ({dist}) ({edge.Text} -> {(edge.Link == null ? "" : edge.Link.Text)}{(edge.LinkedExpr == null ? "" : $", if {edge.LinkedExpr}")})");
                        }
                    }
                }
                if (lastLoop)
                {
                    break;
                }
                else if (visited.Count == diff)
                {
                    lastLoop = true;
                }
            }
            List<string> unvisited = graph.Keys.Except(visited).Except(g.Areas.Values.Where(a => a.HasTag("optional")).Select(a => a.Name)).ToList();

            if (opt["explain"]) Console.WriteLine($"Not visited: [{string.Join(", ", unvisited)}]");
            if (opt["explain"]) Console.WriteLine();

            return new CheckRecord
            {
                Records = recs,
                Unvisited = unvisited,
            };
        }
    }
}
