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
            Dictionary<string, Node> graph = g.graph;
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
                visited.Add(area);
                config[area] = Expr.TRUE;
                foreach (string item in graph[area].Items)
                {
                    config[item] = Expr.TRUE;
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
            bool getDepRecord(string dep, out NodeRecord nr)
            {
                if (g.itemAreas.TryGetValue(dep, out string itemArea)) dep = itemArea;
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
                    foreach (string dep in depVisit)
                    {
                        if (!getDepRecord(dep, out NodeRecord nr)) throw new Exception($"Dependency in edge {e} not visited: {dep}");
                        string depArea = g.itemAreas.TryGetValue(dep, out string itemArea) ? itemArea : dep;
                        preVisit.Add(depArea);
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
            List<string> unvisited = graph.Keys.Except(visited).Except(g.areas.Values.Where(a => a.HasTag("optional")).Select(a => a.Name)).ToList();

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
