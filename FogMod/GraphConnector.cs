using System;
using System.Linq;
using System.Collections.Generic;
using static FogMod.Util;
using static FogMod.AnnotationData;
using static FogMod.GraphChecker;
using static FogMod.Graph;
using System.IO;

namespace FogMod
{
    public class GraphConnector
    {
        enum EdgeSilo
        {
            PAIRED,
            UNPAIRED
        }
        public void Connect(RandomizerOptions opt, Graph g, Annotations ann, Random random)
        {
            Dictionary<string, Node> graph = g.graph;
            List<Edge> allFroms = graph.Values.SelectMany(node => node.From.Where(e => e.From == null)).ToList();
            List<Edge> allTos = graph.Values.SelectMany(node => node.To.Where(e => e.To == null)).ToList();
            Shuffle(random, allFroms);
            Shuffle(random, allTos);

            // For now, try to connect one-way to one-way and have distinct silos.
            foreach (EdgeSilo siloType in Enum.GetValues(typeof(EdgeSilo)))
            {
                List<Edge> froms = allFroms.Where(e => (e.Pair == null) == (siloType == EdgeSilo.UNPAIRED)).ToList();
                List<Edge> tos = allTos.Where(e => (e.Pair == null) == (siloType == EdgeSilo.UNPAIRED)).ToList();

                while (true)
                {
                    if (opt["vanilla"]) break;
                    Edge from = null;
                    for (int i = 0; i < froms.Count; i++)
                    {
                        from = froms[i];
                        if (from.From != null) throw new Exception($"Connected edge still left: {from}");
                        froms.RemoveAt(i);
                        tos.Remove(from.Pair);
                        break;
                    }
                    if (from == null) break;
                    Edge to = null;
                    if (tos.Count == 0)
                    {
                        if (from.Pair != null)
                        {
                            // Have to connect edge to itself
                            to = from.Pair;
                        }
                        else
                        {
                            throw new Exception($"Ran out of eligible edges");
                        }
                    }
                    for (int i = 0; i < tos.Count; i++)
                    {
                        Edge cand = tos[i];
                        if (cand.To != null) throw new Exception($"Connected edge still left: {cand}");
                        // Avoid connecting to self
                        if (from.Pair == cand) continue;
                        if ((from.Pair == null) != (cand.Pair == null)) continue;
                        to = cand;
                        tos.RemoveAt(i);
                        froms.Remove(to.Pair);
                        break;
                    }
                    if (to == null) break;
                    if (from.IsFixed || to.IsFixed) throw new Exception($"Internal error: found fixed edges in randomization {from} ({from.IsFixed}) and {to} ({to.IsFixed})");
                    g.connect(to, from);
                }
                if (froms.Count > 0 || tos.Count > 0) throw new Exception($"Internal error: unconnected edges after randomization:\nFrom edges: {string.Join(", ", froms)}\nTo edges: {string.Join(", ", tos)}");
            }

            // Massive pile of edge-swapping heuristics incoming
            int tries = 0;
            GraphChecker checker = new GraphChecker();
            CheckRecord check = null;
            bool pairedOnly = true;
            while (tries++ < 100)
            {
                if (opt["explain"]) Console.WriteLine($"------------------------ Try {tries}");
                check = checker.Check(opt, g);
                if (check.Unvisited.Count == 0)
                {
                    break;
                }

                Edge toFind = null;
                List<string> unvisited = check.Unvisited.ToList();
                Shuffle(new Random(opt.Seed + tries), unvisited);
                bool hasCond = true;
                foreach (string area in unvisited)
                {
                    foreach (Edge edge in graph[area].From)
                    {
                        if (!edge.IsFixed && (edge.Pair != null) == pairedOnly)
                        {
                            if (edge.LinkedExpr == null)
                            {
                                toFind = edge;
                                hasCond = false;
                                break;
                            }
                            else if (toFind == null)
                            {
                                toFind = edge;
                            }
                        }
                    }
                    if (toFind != null && !hasCond) break;
                }
                if (toFind == null)
                {
                    if (pairedOnly && opt["warp"])
                    {
                        // Redo but with warp edges instead. Generally only happens with warp-only config.
                        pairedOnly = false;
                        continue;
                    }
                    throw new Exception($"Could not find edge into unreachable areas {string.Join(", ", check.Unvisited)}");
                }

                (Edge, float) victim = (null, 0);
                Edge lastEdge = null;
                int lastCount = 0;
                foreach (NodeRecord rec in check.Records.Values.OrderBy(r => r.Dist))
                {
                    if (opt["explain"]) Console.WriteLine($"{rec.Area}: {rec.Dist}");
                    foreach (KeyValuePair<Edge, float> entry in rec.InEdge.OrderBy(e => e.Value))
                    {
                        Edge e = entry.Key;
                        if (opt["explain"]) Console.WriteLine($"  From {e.From}{(e.IsFixed ? " (world)" : "")}: {entry.Value}");
                    }
                    KeyValuePair<Edge, float> maxEdge = rec.InEdge.OrderBy(e => e.Value).Where(e => !e.Key.IsFixed && (e.Key.Pair != null) == pairedOnly).LastOrDefault();
                    if (maxEdge.Key != null)
                    {
                        int inCount = graph[rec.Area].From.Count;
                        if (inCount > lastCount)
                        {
                            lastEdge = maxEdge.Key;
                            lastCount = inCount;
                        }
                        KeyValuePair<Edge, float> minEdge = rec.InEdge.OrderBy(e => e.Value).First();
                        if (minEdge.Key != maxEdge.Key)
                        {
                            if (opt["explain"]) Console.WriteLine($"  Min {minEdge.Value}, Max editable {maxEdge.Value}");
                            // Maybe max victim isn't always best for overall difficulty - or it depends on which edge to swap with is chosen.
                            if (maxEdge.Value >= victim.Item2)
                            {
                                victim = (maxEdge.Key, maxEdge.Value);
                            }
                        }
                    }
                }
                Edge victimEdge = victim.Item1;
                if (victimEdge == null)
                {
                    // We can't preserve original graph structure really, so just pick arbitrary one to change
                    if (lastEdge != null)
                    {
                        if (opt["explain"]) Console.WriteLine("!!!!!!!!!!! Picking non-redundant edge, but last reachable");
                        victimEdge = lastEdge;
                    }
                    else
                    {
                        // Or, completely pick one indiscriminately even if it goes somewhere important
                        victimEdge = check.Records.Keys.SelectMany(a => graph[a].To).Where(e => !e.IsFixed && (e.Pair != null) == pairedOnly).LastOrDefault();
                        if (opt["explain"]) Console.WriteLine("!!!!!!!!!!! Picking any edge whatsoever");
                        if (victimEdge == null) throw new Exception("No outgoing edge found to change");
                    }
                }
                if (opt["explain"])
                {
                    Console.WriteLine($"Swap unreached: {toFind}");
                    Console.WriteLine($"Swap redundant: {victimEdge}");
                }

                // Swap thos edges
                Edge newEntrance = toFind; // entrance edge
                Edge newExit = toFind.Link;
                Edge oldExit = victimEdge; // exit edge
                Edge oldEntrance = oldExit.Link;
                g.disconnect(newExit);
                g.disconnect(oldExit);
                // Ton of logic to deal with self edges
                if (newEntrance == newExit.Pair && oldEntrance == oldExit.Pair)
                {
                    // Both are self edges for some strange reason, so just link them
                    g.connect(oldExit, newEntrance);
                }
                else if (newEntrance == newExit.Pair)
                {
                    // Leave one of the old entrances or exits to self-link, to join old and new
                    if (oldEntrance.Pair != null)
                    {
                        g.connect(oldEntrance.Pair, oldEntrance);
                        g.connect(oldExit, newEntrance);
                    }
                    else if (oldExit.Pair != null)
                    {
                        g.connect(oldExit, oldExit.Pair);
                        g.connect(newExit, oldEntrance);
                    }
                    else throw new Exception($"Bad seed: Can't find edge to self-link to reach {newEntrance}");
                }
                else if (oldEntrance == oldExit.Pair)
                {
                    // Leave one of the new entrances to self-link, since at least the new exit will be linked to old. Or vice versa
                    if (newEntrance.Pair != null)
                    {
                        g.connect(newEntrance.Pair, newEntrance);
                        g.connect(newExit, oldEntrance);
                    }
                    else if (newExit.Pair != null)
                    {
                        g.connect(newExit, newExit.Pair);
                        g.connect(oldExit, newEntrance);
                    }
                    else throw new Exception($"Bad seed: Can't find edge to self-link to reach {newEntrance}");
                }
                else
                {
                    g.connect(oldExit, newEntrance);
                    g.connect(newExit, oldEntrance);
                }
                pairedOnly = true;
            }
            if (check == null || check.Unvisited.Count > 0) throw new Exception($"Couldn't solve seed {opt.DisplaySeed} - try a different one");

            // Check succeeded, time to calculate scale and dump info
            float max = check.Records.Values.Where(r => !r.Area.StartsWith("kiln")).Select(r => r.Dist).Max();
            Dictionary<string, float> getCumCost(Dictionary<string, float> d)
            {
                Dictionary<string, float> total = new Dictionary<string, float>();
                float cost = 0;
                foreach (KeyValuePair<string, float> entry in d)
                {
                    cost += entry.Value;
                    total[entry.Key] = cost;
                }
                return total;
            }
            Dictionary<string, float> distances = check.Records.Values.OrderBy(r => r.Dist).ToDictionary(r => r.Area, r => Math.Min(r.Dist / max, 1));
            Dictionary<string, float> thisDist = getCumCost(distances);
            Dictionary<string, float> vCost = getCumCost(ann.DefaultCost);
            List<float> vCosts = ann.DefaultCost.Select(t => t.Value).OrderBy(t => t).ToList();
            List<float> ratios = new List<float>();

            // Choose one blacksmith.
            // If any paths have <=4 areas, choose them
            // If any paths have no bosses unique to that path, choose them
            // Otherwise, choose shortest?
            List<NodeRecord> blacksmiths = new[] { "parish_andre", "catacombs", "anorlondo_blacksmith" }.Select(area => check.Records[area]).OrderBy(r => r.Visited.Count).ToList();
            NodeRecord blacksmith;
            if (blacksmiths[0].Visited.Count < 5)
            {
                blacksmith = blacksmiths[0];
            }
            else
            {
                HashSet<string> commonAreas = new HashSet<string>(g.areas.Keys);
                foreach (NodeRecord rec in blacksmiths)
                {
                    commonAreas.IntersectWith(rec.Visited);
                }
                NodeRecord minBoss = blacksmiths.Find(rec => commonAreas.IsSupersetOf(rec.Visited.Where(a => g.areas[a].HasTag("boss"))));
                blacksmith = minBoss == null ? blacksmiths[0] : minBoss;
            }
            HashSet<string> preBlacksmith = new HashSet<string>(blacksmith.Visited);
            
            foreach (string area in new[] { "parish_andre", "catacombs", "anorlondo_blacksmith" }) // "newlondo"
            {
                if (opt["explain"]) Console.WriteLine($"Blacksmith {area}: {string.Join(", ", check.Records[area].Visited)}");
            }
            g.areaRatios = new Dictionary<string, float>();
            int k = 0;
            foreach (NodeRecord rec in check.Records.Values.OrderBy(r => r.Dist))
            {
                float desiredCost = k < vCosts.Count ? vCosts[k] : 1;
                if (!g.areas[rec.Area].HasTag("optional")) k++;
                bool isBoss = g.areas[rec.Area].HasTag("boss");
                bool preBlacksmithBoss = preBlacksmith.Contains(rec.Area) && isBoss;
                if (preBlacksmithBoss && desiredCost > 0.05) desiredCost = 0.05f;
                float ratio = 1;
                if (rec.Area == "kiln_gwyn")
                {
                    // Keep ratio 1
                }
                else if (ann.DefaultCost.TryGetValue(rec.Area, out float defaultCost))
                {
                    // This scaling constant factor is a bit tricky to tune.
                    // Originally used 400-1100, based on HP scaling over the course of a game. This seems to better match expected boss HP, even if damage can be low initially.
                    ratio = (33 + 66 * desiredCost) / (33 + 66 * defaultCost);
                    // If it's randomized to past 70% of the way, don't make it easier.
                    if (ratio < 1 && ((double)k / check.Records.Count) > 0.7)
                    {
                        ratio = 1;
                    }
                    // If it's early enough in vanilla (i.e. before expected access to blacksmith), don't make it easier either.
                    if (defaultCost <= (ann.DefaultCost.TryGetValue("parish_church", out float val) ? val : 0.25) && ratio < 1)
                    {
                        ratio = 1;
                    }
                }
                g.areaRatios[rec.Area] = ratio;
                
                // Print out the connectivity info for spoiler logs
                if (rec.Area == "anorlondo_os") Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                Console.WriteLine($"{rec.Area} {(opt["explain"] ? $"{desiredCost * 100:0.}% " : "")}(scaling: {ratio * 100:0.}%)" + (opt["debugareas"] ? $" [{string.Join(",", new SortedSet<string>(rec.Visited))}]" : "") + (isBoss ? " <----" : ""));
                foreach (KeyValuePair<Edge, float> entry in rec.InEdge.OrderBy(e => e.Value))
                {
                    Edge e = entry.Key;
                    // Don't print entry.Value directly (distance of edge) - hard to visualize
                    Console.WriteLine($"  From {e.From} ({e.Text}) to {rec.Area}" + (e.Text == e.Link.Text ? "" : $" ({e.Link.Text})"));
                }
            }
            if (opt["dumpdist"])
            {
                foreach (KeyValuePair<string, float> entry in distances)
                {
                    if (!g.areas[entry.Key].HasTag("optional")) Console.WriteLine($"{entry.Key}: {entry.Value}  # SL {(int)(10 + 60 * entry.Value)}");
                }
            }
            Console.WriteLine($"Finished {opt.DisplaySeed} at try {tries}");
            if (opt["explain"]) Console.WriteLine($"Pre-Blacksmith areas ({blacksmith.Area}): {string.Join(", ", preBlacksmith)}");

            if (opt["dumpgraph"])
            {
                bool bi = false;
                TextWriter dot = File.CreateText(@"..\fog.dot");
                dot.WriteLine($"{(bi ? "di" : "")}graph {{");
                // dot.WriteLine("  nodesep=0.1; ranksep=0.1; ");
                string escape(object o)
                {
                    if (o == null) return "";
                    return o.ToString().Replace("\n", "\\l").Replace("\"", "\\\"") + "\\l";
                }
                foreach (Node node in graph.Values)
                {
                    string label = node.Area;
                    label = label == "" ? "(empty)" : label;
                    dot.WriteLine($"    \"{node.Area}\" [ shape=box,label=\"{escape(label)}\" ];");
                }
                HashSet<Connection> oneCons = new HashSet<Connection>();
                foreach (Node from in graph.Values)
                {
                    foreach (Edge e in from.To)
                    {
                        Connection con = new Connection(e.From, e.To);
                        if (oneCons.Contains(con)) continue;
                        if (!bi) oneCons.Add(con);
                        // Node to = e.To;
                        string toKey = e.To;
                        string style = "solid";
                        string label = null; // $"{e.LinkedExpr}";
                        dot.WriteLine($"  \"{from.Area}\" -{(bi ? ">" : "-")} \"{toKey}\" [ style={style},labelloc=t,label=\"{escape(label)}\" ];");
                    }
                }
                dot.WriteLine("}");
                dot.Close();
            }
        }
    }
}
