using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Numerics;
using static FogMod.Util;
using static FogMod.AnnotationData;
using static FogMod.Graph;
using SoulsIds;
using SoulsFormats;
using static SoulsFormats.EMEVD.Instruction;
using YamlDotNet.Serialization;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public class GameDataWriter
    {
        private int tempColEventBase = 5350; // or 5900 might also work
        public void Write(RandomizerOptions opt, Annotations ann, Graph g, string gameDir, FromGame game)
        {
            GameEditor editor = new GameEditor(game);
            bool remastered = game == FromGame.DS1R;
            string distBase = $@"dist\{game}";
            editor.Spec.GameDir = distBase;
            Dictionary<string, MSB1> maps = editor.Load(@"map\MapStudio", path => specs.ContainsKey(Path.GetFileNameWithoutExtension(path)) ? MSB1.Read(path) : null, "*.msb");
            Dictionary<string, PARAM.Layout> layouts = editor.LoadLayouts();
            Dictionary<string, PARAM> Params = editor.LoadParams(layouts);
            string dcxExt = remastered ? ".dcx" : "";
            string fmgEvent = remastered ? "Event_text_" : "イベントテキスト";
            Dictionary<string, FMG> fmgs = editor.LoadBnd($@"{distBase}\msg\ENGLISH\menu.msgbnd{dcxExt}", (data, name) => name == fmgEvent ? FMG.Read(data) : null);

            editor.Spec.GameDir = gameDir ?? ForGame(game).GameDir;

            // Backup
            List<string> files = GetAllBaseFiles(game);
            foreach (string file in files)
            {
                string path = $@"{editor.Spec.GameDir}\{file}";
                if (File.Exists(path)) SFUtil.Backup(path);
            }

            // Load msbs
            Dictionary<string, MSB1> msbs = new Dictionary<string, MSB1>();
            Dictionary<string, int> players = new Dictionary<string, int>();
            foreach (KeyValuePair<string, MSB1> entry in maps)
            {
                if (!specs.TryGetValue(entry.Key, out MapSpec spec)) continue;
                msbs[spec.Name] = entry.Value;
                players[spec.Name] = 0;
            }

            // Do scaling
            HashSet<string> excludeScaling = new HashSet<string> { "c1000" };
            if (opt["dumpcols"])
            {
                Dictionary<string, string> models = editor.LoadNames("ModelName", n => n);
                Dictionary<int, string> chrs = editor.LoadNames("CharaInitParam", n => int.Parse(n));
                List<Enemies> cols = new List<Enemies>();
                int unique = 0;
                foreach (KeyValuePair<string, MSB1> entry in msbs)
                {
                    string map = entry.Key;
                    MSB1 msb = entry.Value;
                    Dictionary<string, List<MSB1.Part.Enemy>> colDesc = new Dictionary<string, List<MSB1.Part.Enemy>>();
                    foreach (MSB1.Part.Enemy e in msb.Parts.Enemies)
                    {
                        if (excludeScaling.Contains(e.ModelName) || e.CollisionName == null) continue;
                        AddMulti(colDesc, e.CollisionName, e);
                    }
                    foreach (KeyValuePair<string, List<MSB1.Part.Enemy>> col in colDesc)
                    {
                        List<string> enemies = col.Value.Select(e =>
                        {
                            string npcDesc = chrs.TryGetValue(e.NPCParamID / 10 * 10, out string name) ? $" {name}" : "";
                            string entityDesc = e.EntityID > 0 ? $" @{e.EntityID}" : "";
                            return $"{e.Name} ({(models.TryGetValue(e.ModelName, out string model) ? model : e.ModelName)}). NPC {e.NPCParamID}{npcDesc}{entityDesc}";
                        }).ToList();
                        cols.Add(new Enemies
                        {
                            Col = $"{map} {col.Key}",
                            Area = map,
                            Includes = enemies
                        });
                    }
                }
                ISerializer serializer = new SerializerBuilder().Build();
                serializer.Serialize(Console.Out, cols);
            }

            // Process and validate the cols part of the config
            Dictionary<string, string> rankDefaults = new Dictionary<string, string>();
            Dictionary<(string, string), List<Enemies>> rankCols = new Dictionary<(string, string), List<Enemies>>();
            foreach (Enemies enemy in ann.Enemies)
            {
                string[] parts = enemy.Col.Split(' ');
                if (!nameSpecs.ContainsKey(parts[0])) throw new Exception($"Unknown map in {enemy.Col}");
                if (!g.areas.ContainsKey(enemy.Area)) throw new Exception($"Unknown area {enemy.Area} in {enemy.Col}");
                if (parts.Length == 1)
                {
                    rankDefaults[parts[0]] = enemy.Area;
                    continue;
                }
                if (parts.Length != 2) throw new Exception($"Bad format {enemy.Col}");
                AddMulti(rankCols, (parts[0], parts[1]), enemy);
            }

            // Add new scalings
            HashSet<string> rankCells = new HashSet<string>(
                ("maxHpRate maxStaminaRate physicsAttackPowerRate magicAttackPowerRate fireAttackPowerRate thunderAttackPowerRate " +
                "physicsDiffenceRate magicDiffenceRate fireDiffenceRate thunderDiffenceRate staminaAttackRate").Split(' '));
            // 7001 is 1 physattackpower. 7015 is 2.5. (defense scales from 1 to 3. stamina attack rate scales from 1 to 2.)
            Dictionary<int, PARAM.Row> baseRanks = Params["SpEffectParam"].Rows.Where(r => r.ID >= 7001 && r.ID <= 7015).ToDictionary(r => (int)r.ID, r => r);
            PARAM.Row baseRank = baseRanks[7015];
            List<float> rankRatios = new List<float>();
            for (int i = 0; i <= 50; i++)
            {
                PARAM.Row rank = new PARAM.Row(7200 + i, null, layouts["SP_EFFECT_PARAM_ST"]);
                // float ratio = 0.3f + (3 - 0.3f) * i / 30;  // arithmetic ranking
                float ratio = (float)Math.Pow(4, (i - 25) / 25.0);  // logarithmic ranking
                rankRatios.Add(ratio);
                foreach (PARAM.Cell cell in rank.Cells)
                {
                    PARAM.Cell baseCell = baseRank[cell.Name];
                    if (rankCells.Contains(cell.Name))
                    {
                        // if ratio is 1, value should be 1.
                        // if ratio is 2.5, value should be the same as base row.
                        float baseRatio = (float)baseCell.Value / 2.5f;
                        float rankValue = ratio;
                        if (rankValue >= 1) rankValue *= baseRatio;
                        else rankValue /= baseRatio;
                        cell.Value = rankValue;
                    }
                    else
                    {
                        cell.Value = baseCell.Value;
                    }
                }
                Params["SpEffectParam"].Rows.Add(rank);
            }

            if (opt["scale"])
            {
                int findSpEffect(float val)
                {
                    for (int i = 0; i < rankRatios.Count; i++)
                    {
                        float ratio = rankRatios[i];
                        if (ratio >= val) return 7200 + i;
                    }
                    return 7200 + rankRatios.Count - 1;
                }
                Dictionary<int, PARAM.Row> baseNpcs = Params["NpcParam"].Rows.ToDictionary(r => (int)r.ID, r => r);
                // By NPC param id and then by logical area
                Dictionary<int, Dictionary<string, List<MSB1.Part.Enemy>>> npcs = new Dictionary<int, Dictionary<string, List<MSB1.Part.Enemy>>>();
                foreach (KeyValuePair<string, MSB1> entry in msbs)
                {
                    string map = entry.Key;
                    MSB1 msb = entry.Value;
                    Dictionary<string, List<MSB1.Part.Enemy>> colDesc = new Dictionary<string, List<MSB1.Part.Enemy>>();
                    foreach (MSB1.Part.Enemy e in msb.Parts.Enemies)
                    {
                        if (excludeScaling.Contains(e.ModelName) || e.CollisionName == null || e.NPCParamID <= 0) continue;
                        string area;
                        if (rankCols.TryGetValue((map, e.CollisionName), out List<Enemies> enemies))
                        {
                            area = enemies[0].Area;
                            if (enemies.Count > 1)
                            {
                                foreach (Enemies enemy in enemies)
                                {
                                    if (enemy.Includes.Any(name => name.Split(' ')[0] == e.Name))
                                    {
                                        area = enemy.Area;
                                        break;
                                    }
                                }
                            }
                        }
                        else if (rankDefaults.TryGetValue(map, out string defaultArea)) area = defaultArea;
                        else area = null;
                        if (!npcs.ContainsKey(e.NPCParamID)) npcs[e.NPCParamID] = new Dictionary<string, List<MSB1.Part.Enemy>>();
                        AddMulti(npcs[e.NPCParamID], area, e);
                    }
                }
                void setScalingSp(PARAM.Row row, int sp)
                {
                    if (row.ID >= 120000)
                    {
                        row["spEffectID4"].Value = sp;
                    }
                    else
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            if ((int)row[$"spEffectID{i}"].Value == 0)
                            {
                                row[$"spEffectID{i}"].Value = sp;
                                break;
                            }
                        }
                    }
                }
                foreach (KeyValuePair<int, Dictionary<string, List<MSB1.Part.Enemy>>> npcType in npcs)
                {
                    int npcID = npcType.Key;
                    foreach (KeyValuePair<string, List<MSB1.Part.Enemy>> npcArea in npcType.Value)
                    {
                        string area = npcArea.Key;
                        float ratio = area != null && g.areaRatios.TryGetValue(area, out float val) ? val : 1;
                        if (Math.Abs(ratio - 1) < 0.01f) continue;
                        float initialRatio = 1;
                        if (npcID >= 120000)
                        {
                            int baseSp = (int)baseNpcs[npcID]["spEffectID4"].Value;
                            if (baseRanks.TryGetValue(baseSp, out PARAM.Row spRow))
                            {
                                initialRatio = (float)spRow["physicsAttackPowerRate"].Value;
                            }
                        }
                        float ratioFromBase = ratio * initialRatio;
                        int sp = findSpEffect(ratioFromBase);
                        PARAM.Row newNpc;
                        // Quelaag broked under certain circumstances
                        if (npcID == 528000 && sp < 7225)
                        {
                            sp = 7225;
                        }
                        if (npcType.Value.Count == 1)
                        {
                            // If only one NPC, replace it
                            newNpc = baseNpcs[npcID];
                            if (npcID == 528000 && ratioFromBase < 1)
                            {
                                newNpc["hp"].Value = (uint)((uint)newNpc["hp"].Value * ratio);
                            }
                        }
                        else
                        {
                            // Make a new one otherwise
                            PARAM.Row baseNpc = baseNpcs[npcID];
                            int newID = npcID;
                            while (baseNpcs.ContainsKey(newID)) newID++;
                            newNpc = new PARAM.Row(newID, null, layouts["NPC_PARAM_ST"]);
                            foreach (PARAM.Cell cell in newNpc.Cells)
                            {
                                cell.Value = baseNpc[cell.Name].Value;
                            }
                            baseNpcs[newID] = newNpc;
                            Params["NpcParam"].Rows.Add(newNpc);
                            foreach (MSB1.Part.Enemy enemy in npcArea.Value)
                            {
                                enemy.NPCParamID = newID;
                            }
                        }
                        setScalingSp(newNpc, sp);
                        uint getNewSouls(uint baseSouls, float baseRatio)
                        {
                            int newVal = Math.Min(60000, (int)Math.Pow(Math.Pow(baseSouls, 1f / 2) * baseRatio, 2));
                            int correct = 0;
                            if (baseSouls % 1000 == 0 && newVal >= 5000) correct = 1000;
                            else if (baseSouls % 500 == 0 && newVal >= 2000) correct = 500;
                            else if (baseSouls % 100 == 0 && newVal >= 300) correct = 100;
                            if (correct > 0) newVal -= newVal % correct;
                            return (uint)newVal;
                        }
                        uint souls = (uint)baseNpcs[npcID]["getSoul"].Value;
                        uint newSouls = 0;
                        if (souls > 0)
                        {
                            newSouls = getNewSouls(souls, ratio);
                            newNpc["getSoul"].Value = newSouls;
                        }
                        else
                        {
                            foreach (MSB1.Part.Enemy enemy in npcArea.Value)
                            {
                                if (enemy.EntityID > 0)
                                {
                                    PARAM.Row gameArea = Params["GameAreaParam"][enemy.EntityID];
                                    if (gameArea != null)
                                    {
                                        souls = (uint)gameArea["bonusSoul_single"].Value;
                                        newSouls = getNewSouls(souls, ratio);
                                        gameArea["bonusSoul_single"].Value = newSouls;
                                        gameArea["bonusSoul_multi"].Value = newSouls;
                                    }
                                }
                            }
                        }
                        if (opt["debugscale"])
                        {
                            Console.WriteLine($"Change for {npcID} {area}: {ratio} * {initialRatio} = {ratio * initialRatio}, sp {sp}. Souls {souls} -> {newSouls}. {(npcType.Value.Count == 1 ? " UNIQUE" : "")} {(newSouls > 50000 ? "BIG" : "")}");
                        }
                    }
                }
            }
            Console.WriteLine($@"Writing {editor.Spec.GameDir}\{editor.Spec.ParamFile}");
            editor.OverrideBnd($@"{distBase}\{editor.Spec.ParamFile}", @"param\GameParam", Params, f => f.Write());

            // Write stuff
            int mk = 1815800;
            int slot = 0;
            (byte, byte) GetDest(string map)
            {
                MapSpec spec = nameSpecs[map];
                return (byte.Parse(spec.Map.Substring(1, 2)), byte.Parse(spec.Map.Substring(4, 2)));
            }
            List<List<object>> events = new List<List<object>>();
            List<float> getPos(string at) => at.Split(' ').Select(c => float.Parse(c, CultureInfo.InvariantCulture)).ToList();
            void AddWarpEvent(byte mode, string toArea, List<int> p, string col=null)
            {
                (byte area, byte block) = GetDest(toArea);
                while (p.Count < 7) p.Add(0);
                events.Add(new List<object> { slot++, (uint)5700, mode, area, block, (byte)(p[6] > 0 ? 1 : 0), p[0], p[1], p[2], p[3], p[4], p[5], Math.Abs(p[6]) });
            }
            Dictionary<string, int> bossTriggerRegions = ann.Entrances.SelectMany(e => e.Sides()).Where(s => s.BossTrigger != 0).ToDictionary(s => s.Area, s => s.BossTrigger);
            Dictionary<int, List<int>> bossTriggerAdd = new Dictionary<int, List<int>>();
            foreach (Entrance e in ann.Entrances)
            {
                if (e.HasTag("unused")) continue;
                if (e.HasTag("door"))
                {
                    AddWarpEvent(1, e.Area, new List<int> { e.ID, e.ID + 1, e.HasTag("lordvessel") ? 11800100 : 0 });
                    continue;
                }
                Vector3 MoveInDirection(Vector3 v, Vector3 r, float dist)
                {
                    float angle = r.Y * (float)Math.PI / 180;
                    return new Vector3(v.X + (float)Math.Sin(angle) * dist, v.Y, v.Z + (float)Math.Cos(angle) * dist);
                }
                for (int i = 0; i <= 1; i++)
                {
                    bool front = i == 0;
                    Side side = front ? e.ASide : e.BSide;
                    if (side == null) continue;

                    string map = side.DestinationMap ?? e.Area;
                    MSB1 msb = msbs[map];
                    MSB1.Part.Object fog = msbs[e.Area].Parts.Objects.Find(o => o.Name == e.Name);

                    if (side.BossTrigger != 0 && side.BossTriggerArea != null)
                    {
                        if (bossTriggerRegions[side.Area] != side.BossTrigger) throw new Exception($"Non-unique boss trigger for {side.Area}");
                        MSB1.Region tr = new MSB1.Region();
                        tr.Name = $"Boss start for {side.Area}";
                        tr.EntityID = mk++;
                        List<float> loc = getPos(side.BossTriggerArea);
                        tr.Position = new Vector3(loc[0], loc[1], loc[2]);
                        tr.Rotation = new Vector3(0, loc[3], 0);
                        MSB1.Shape.Box tbox = new MSB1.Shape.Box();
                        tbox.Width = loc[4];
                        tbox.Height = loc[5];
                        tbox.Depth = loc[6];
                        tr.Shape = tbox;
                        msb.Regions.Regions.Insert(0, tr);
                        AddMulti(bossTriggerAdd, side.BossTrigger, tr.EntityID);
                    }
                    if (g.ignore.Contains((e.Name, side.Area))) continue;
                    Vector3 fogPosition = fog.Position;
                    float warpDist = 1f;
                    // Action trigger
                    MSB1.Region r = new MSB1.Region();
                    MSB1.Shape.Box box = new MSB1.Shape.Box();
                    int actionID;

                    // Find opposite direction
                    float rot = fog.Rotation.Y + 180;
                    rot = rot >= 180 ? rot - 360 : rot;
                    Vector3 opposite = new Vector3(fog.Rotation.X, rot, fog.Rotation.Z);

                    if (side.ActionRegion == 0)
                    {
                        r.EntityID = mk++;
                        actionID = r.EntityID;
                        r.Name = $"{(front ? "FR" : "BR")}: {e.Text}";
                        r.Position = fogPosition;
                        r.Rotation = fog.Rotation;
                        if (!front)
                        {
                            r.Rotation = opposite;
                        }
                        r.Position = MoveInDirection(r.Position, r.Rotation, 1f);
                        r.Position = new Vector3(r.Position.X, r.Position.Y - (e.HasTag("world") ? 0 : 1), r.Position.Z);
                        // x y z
                        box.Width = side.CustomActionWidth == 0 ? 1.5f : side.CustomActionWidth;
                        box.Height = 3;
                        box.Depth = 1.5f;
                        r.Shape = box;
                        msb.Regions.Regions.Insert(0, r);
                    }
                    else
                    {
                        actionID = side.ActionRegion;
                    }
                    // Warp
                    int warpID = mk++;
                    if (side.CustomWarp == null)
                    {
                        Vector3 warpRotation;
                        Vector3 warpMove;
                        if (front)
                        {
                            warpRotation = opposite;
                            warpMove = fog.Rotation;
                        }
                        else
                        {
                            warpRotation = fog.Rotation;
                            warpMove = opposite;
                        }
                        Vector3 warpPosition = MoveInDirection(fogPosition, warpMove, warpDist);
                        if (side.HasTag("higher")) warpPosition = new Vector3(warpPosition.X, warpPosition.Y + 1, warpPosition.Z);

                        MSB1.Part.Player p = new MSB1.Part.Player();
                        p.Name = $"c0000_{50 + players[map]++:d4}";
                        p.ModelName = "c0000";
                        p.EntityID = warpID;
                        p.Position = warpPosition;
                        p.Rotation = warpRotation;
                        p.Scale = new Vector3(1, 1, 1);
                        msb.Parts.Players.Add(p);
                        side.Warp = new WarpPoint { ID = e.ID, Map = map, Action = actionID, Player = warpID };
                    }
                    else
                    {
                        string[] parts = side.CustomWarp.Split(' ');
                        string customArea = parts[0];
                        MSB1 customMsb = msbs[customArea];
                        List<float> pos = parts.Skip(1).Select(c => float.Parse(c, CultureInfo.InvariantCulture)).ToList();

                        MSB1.Part.Player p = new MSB1.Part.Player();
                        p.Name = $"c0000_{50 + players[customArea]++:d4}";
                        // Console.WriteLine(p.Name);
                        p.ModelName = "c0000";
                        p.EntityID = warpID;
                        p.Position = new Vector3(pos[0], pos[1], pos[2]);
                        p.Rotation = new Vector3(0, pos[3], 0);
                        p.Scale = new Vector3(1, 1, 1);
                        customMsb.Parts.Players.Add(p);
                        side.Warp = new WarpPoint { ID = e.ID, Map = customArea, Action = actionID, Player = warpID };
                    }
                    if (side.BossTrigger == 0 && bossTriggerRegions.TryGetValue(side.Area, out int trigger))
                    {
                        AddMulti(bossTriggerAdd, trigger, actionID);
                    }
                }
            }
            foreach (Entrance e in ann.Warps)
            {
                if (e.HasTag("unused") || e.HasTag("norandom")) continue;
                e.ASide.Warp = new WarpPoint { ID = e.ID, Map = e.ASide.DestinationMap ?? e.Area, Cutscene = e.ASide.Cutscene };
                e.BSide.Warp = new WarpPoint { ID = e.ID, Map = e.BSide.DestinationMap ?? e.Area, Cutscene = e.BSide.Cutscene };
                if (e.BSide.HasTag("player"))
                {
                    e.BSide.Warp.Player = e.ID;
                }
                else
                {
                    e.BSide.Warp.Region = e.ID;
                }
            }
            int getPlayer(WarpPoint warp)
            {
                if (warp.Player != 0) return warp.Player;
                MSB1 msb = msbs[warp.Map];
                MSB1.Region region = msb.Regions.Regions.Where(r => r.EntityID == warp.Region).FirstOrDefault();
                if (region == null) throw new Exception($"Cutscene warp destination {warp.Region} not found in {warp.Map}");
                MSB1.Part.Player p = new MSB1.Part.Player();
                p.Name = $"c0000_{50 + players[warp.Map]++:d4}";
                p.ModelName = "c0000";
                p.EntityID = mk++;
                p.Position = region.Position;
                p.Rotation = region.Rotation;
                p.Scale = new Vector3(1, 1, 1);
                msb.Parts.Players.Add(p);
                warp.Player = p.EntityID;
                return warp.Player;
            }
            int getRegion(WarpPoint warp)
            {
                if (warp.Region != 0) return warp.Region;
                MSB1 msb = msbs[warp.Map];
                MSB1.Part.Player p = msb.Parts.Players.Where(r => r.EntityID == warp.Player).FirstOrDefault();
                if (p == null) throw new Exception($"Cutscene warp destination {warp.Player} not found in {warp.Map}");
                MSB1.Region tr = new MSB1.Region();
                tr.Name = $"Region for {warp.Player}";
                tr.EntityID = mk++;
                tr.Position = p.Position;
                tr.Rotation = p.Rotation;
                MSB1.Shape.Box tbox = new MSB1.Shape.Box();
                tbox.Width = 0.1f;
                tbox.Height = 1;
                tbox.Depth = 0.1f;
                tr.Shape = tbox;
                msb.Regions.Regions.Insert(0, tr);
                warp.Region = tr.EntityID;
                return warp.Region;
            }
            // Add warp point for softlock prevention
            int softWarp = mk++;
            {
                MSB1 msb = msbs["asylum"];
                MSB1.Part.Player p = new MSB1.Part.Player();
                p.Name = $"c0000_{50 + players["asylum"]++:d4}";
                p.ModelName = "c0000";
                p.EntityID = softWarp;
                p.Position = new Vector3(33.9f, 193.15f, -25.2f);
                p.Rotation = new Vector3(0, -115, 0);
                p.Scale = new Vector3(1, 1, 1);
                msb.Parts.Players.Add(p);
            }
            // Various MSB edits. Lots of making collisions behave nicely on save+quit.
            List<int> playRegions = new List<int>();
            foreach (KeyValuePair<string, MSB1> entry in msbs)
            {
                string map = entry.Key;
                MSB1 msb = entry.Value;
                foreach (MSB1.Part.Collision col in msb.Parts.Collisions)
                {
                    if (col.PlayRegionID < 10)
                    {
                        if (map == "firelink" && col.Name == "h0017B2_0000")
                        {
                            col.PlayRegionID = -69696969 - 10;
                        }
                        if (false && map == "demonruins" && col.Name == "h0005B1")
                        {
                            col.PlayRegionID = 141000;
                        }
                        else if (col.PlayRegionID < -10)
                        {
                            int bossFlag = -col.PlayRegionID - 10;
                            int colFlag = playRegions.IndexOf(bossFlag);
                            if (colFlag == -1)
                            {
                                colFlag = playRegions.Count;
                                playRegions.Add(bossFlag);
                            }
                            col.PlayRegionID = -(tempColEventBase + colFlag) - 10;
                        }
                        else
                        {
                            if (map == "firelink" && (col.Name == "h0017B2_0000" || col.Name == "h0015B2_0000"))
                            {
                                col.PlayRegionID = -2;
                            }
                            if (map == "demonruins" && (col.Name == "h9950B1"))
                            {
                                col.PlayRegionID = 141000;
                            }
                        }
                    }
                }
                if (map == "demonruins")
                {
                    msb.Parts.Collisions = msb.Parts.Collisions.Where(c => c.Name != "h9950B1").ToList();
                }
                if (map == "dlc")
                {
                    msb.Parts.Collisions = msb.Parts.Collisions.Where(c => c.Name != "h7800B1").ToList();
                }
                if (map == "totg")
                {
                    // Nudge the outside fog gate region to be inside & detect boss battle mode
                    MSB1.Region r = msb.Regions.Regions.Find(c => c.EntityID == 1312998);
                    r.Position = new Vector3(-118.186f, -250.591f, -31.893f);
                    if (!(r.Shape is MSB1.Shape.Box box)) throw new Exception("Unexpected region");
                    box.Width = 10;
                    box.Height = 5;
                }
                if (map == "kiln")
                {
                    if (opt["patchkiln"])
                    {
                        MSB1.Part.Player player = msb.Parts.Players.Find(p => p.Name == "c0000_0000");
                        player.Position = new Vector3(50.3f, -63.27f, 106.1f);
                        player.Rotation = new Vector3(0, -105, 0);
                    }
                }
                if (map == "anorlondo")
                {
                    // Use this unless the area after the broken window becomes its own unique area
                    if (!g.entranceIds["o5869_0000"].IsFixed)
                    {
                        MSB1.Part.Object obj = msb.Parts.Objects.Find(e => e.Name == "o0500_0006");
                        obj.Position = new Vector3(444.106f, 160.258f, 255.887f);
                        obj.Rotation = new Vector3(-3, -90, 0);
                        obj.CollisionName = "h0025B1_0000";
                        obj.UnkT0C = 50; // initial animation
                    }
                }
                if (map == "asylum")
                {
                    HashSet<int> secondary = new HashSet<int> { 1811613, 1811616, 1811619, 1811622 };
                    MSB1.Part.Object trBase = null;
                    foreach (MSB1.Part.Object e in msb.Parts.Objects)
                    {
                        if (secondary.Contains(e.EntityID))
                        {
                            e.Position = new Vector3(8.934f, 202f, 18.512f);
                            e.CollisionName = "h0010B1";
                            trBase = e;
                        }
                    }
                    if (trBase == null) throw new Exception("Can't find asylum treasure to base estus treasure on");
                    MSB1.Part.Object es = new MSB1.Part.Object();
                    CopyAll(trBase, es);
                    es.Name = "o0500_0050";
                    es.ModelName = "o0500";
                    es.UnkT0C = 50; // initial animation
                    es.Position = new Vector3(13.279f, 202.015f, 20.8f);
                    es.Rotation = new Vector3(0, 0, 0);
                    es.EntityID = -1;
                    msb.Parts.Objects.Add(es);
                    MSB1.Event.Treasure t = new MSB1.Event.Treasure();
                    t.EventID = 69;
                    t.EntityID = -1;
                    t.ItemLots[0] = 1082;  // estus flask
                    t.TreasurePartName = "o0500_0050";
                    t.Name = "New Estus";
                    msb.Events.Treasures.Add(t);
                }
                if (map == "dukes")
                {
                    MSB1.Part.Object crystal = msb.Parts.Objects.Find(e => e.EntityID == 1701800);
                    MSB1.Part.Object warpC = new MSB1.Part.Object();
                    CopyAll(crystal, warpC);
                    warpC.Name = "o7500_0001";
                    warpC.Position = new Vector3(284.108f, 388.313f, 520.228f);
                    warpC.EntityID = 1701801;
                    warpC.DrawGroups[0] = 2147483648;
                    warpC.DrawGroups[1] = 15;
                    warpC.DrawGroups[2] = 0;
                    warpC.DrawGroups[3] = 0;
                    msb.Parts.Objects.Insert(0, warpC);

                    MSB1.Region spawnRegion = msb.Regions.Regions.Find(r => r.Name == "復活ポイント（一時用）");
                    spawnRegion.EntityID = 1702901;
                }
            }

            int id = 10;
            int traverseFlag = 6920;
            HashSet<string> vanillaEntrances = new HashSet<string>();
            foreach (Node node in g.graph.Values)
            {
                foreach (Edge exit in node.To)
                {
                    Edge entrance = exit.Link;
                    if (entrance == null) throw new Exception($"Internal error: Unlinked {exit}");
                    WarpPoint a = exit.Side.Warp;
                    WarpPoint b = entrance.Side.Warp;
                    if (a == null || b == null)
                    {
                        // This is not always ok for fixed edges... but many it is
                        if (exit.IsFixed && entrance.IsFixed) continue;
                        throw new Exception($"Missing warps - {a == null} {b == null} for {exit} -> {entrance}");
                    }
                    if (exit.Name == entrance.Name && exit.IsFixed && !opt["alwaysshow"])
                    {
                        Entrance e = g.entranceIds[exit.Name];
                        if (vanillaEntrances.Contains(e.Name)) continue;
                        if (e.HasTag("pvp"))
                        {
                            AddWarpEvent(1, e.Area, new List<int> { e.ID, e.ID + 1 });
                            vanillaEntrances.Add(e.Name);
                            continue;
                        }
                        else if (e.HasTag("world") && exit.Pair != null)
                        {
                            WarpPoint aPair = exit.Pair.Side.Warp;
                            WarpPoint bPair = entrance.Pair.Side.Warp;
                            if (aPair == null || bPair == null) throw new Exception($"Missing warp info - {a == null} {b == null} for {exit.Pair} -> {entrance.Pair}");
                            AddWarpEvent(3, e.Area, new List<int> { e.ID, e.ID + 1, traverseFlag, a.Action, bPair.Action });
                            traverseFlag++;
                            vanillaEntrances.Add(e.Name);
                            continue;
                        }
                    }
                    List<int> warpArgs = new List<int> { 0, getPlayer(b), exit.Side.TrapFlag, 0, exit.Side.Flag, entrance.Side.EntryFlag, entrance.Side.BeforeWarpFlag };
                    string fromMap = exit.Side.Warp.Map;
                    string colDesc = exit.Side.Col;
                    // Experiment to use connect cols and cutscene warps. But it has some downsides over area reload.
                    if (false && colDesc != null)
                    {
                        string col = colDesc;
                        MSB1 colMsb = msbs[fromMap];
                        MSB1.Part.Collision baseCol = colMsb.Parts.Collisions.Find(c => c.Name == col);
                        MSB1.Part.ConnectCollision con = new MSB1.Part.ConnectCollision();
                        CopyAll<MSB1.Part>(baseCol, con);
                        con.Placeholder = null;
                        con.ModelName = baseCol.ModelName;
                        con.Name = $"{baseCol.ModelName}_{id++:d4}";
                        con.CollisionName = col;
                        (byte area, byte block) = GetDest(b.Map);
                        con.MapID[0] = area;
                        con.MapID[1] = block;
                        con.MapID[2] = 0xFF;
                        con.MapID[3] = 0xFF;
                        colMsb.Parts.ConnectCollisions.Add(con);
                        Console.WriteLine($"Adding col {fromMap} -> {b.Map}. {col}");
                        warpArgs[2] = getRegion(b);
                    }
                    if (opt["pacifist"])
                    {
                        warpArgs[2] = 0;
                        warpArgs[4] = 0;
                    }
                    if (a.Action == 0)
                    {
                        warpArgs[0] = exit.Side.WarpFlag;
                        AddWarpEvent(2, b.Map, warpArgs);
                    }
                    else
                    {
                        warpArgs[0] = a.ID;
                        warpArgs[3] = a.Action;
                        AddWarpEvent(0, b.Map, warpArgs);
                    }
                }
            }

            if (events.Count > 400) throw new Exception("Internal error: too many warps");
            foreach (string path in Directory.GetFiles($@"{distBase}\event", "*.emevd*"))
            {
                string name = GameEditor.BaseName(path);
                EMEVD evd = EMEVD.Read(path);
                foreach (EMEVD.Event evt in evd.Events)
                {
                    if (evt.ID == 0 && name == "common")
                    {
                        foreach (List<object> init in events)
                        {
                            evt.Instructions.Add(new EMEVD.Instruction(2000, 0, init));
                        }
                        evt.Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { 0, (uint)8900, softWarp }));
                        for (int i = 0; i < playRegions.Count; i++)
                        {
                            evt.Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { i, (uint)8901, tempColEventBase + i, playRegions[i] }));
                        }
                    }
                    if (evt.ID == 0 && name == "m14_01_00_00" && opt["bboc"])
                    {
                        foreach (EMEVD.Instruction instr in evt.Instructions)
                        {
                            if (instr.Bank != 2000 || instr.ID != 0) continue;
                            int eventNum = BitConverter.ToInt32(instr.ArgData, 4);
                            // 11410200 becomes new event 11410220. Likewise for 201
                            if (eventNum == 11410200 || eventNum == 11410201)
                            {
                                byte[] newVal = BitConverter.GetBytes(eventNum + 20);
                                Array.Copy(newVal, 0, instr.ArgData, 4, 4);
                            }
                        }
                    }
                    int index = 0;
                    List<object> regionArgs = null;
                    foreach (EMEVD.Instruction instr in evt.Instructions)
                    {
                        if (instr.Bank == 3 && instr.ID == 2)
                        {
                            List<object> instrArgs = instr.UnpackArgs(new List<ArgType> { ArgType.SByte, ArgType.Byte, ArgType.Int32, ArgType.Int32 });
                            if (bossTriggerAdd.ContainsKey((int)instrArgs[3]))
                            {
                                regionArgs = instrArgs;
                                break;
                            }
                        }
                        index++;
                    }
                    if (regionArgs != null)
                    {
                        sbyte cond = (sbyte)regionArgs[0];
                        int trigger = (int)regionArgs[3];
                        evt.Instructions.RemoveAt(index);
                        evt.Instructions.Insert(index, new EMEVD.Instruction(0, 0, new List<object> { (sbyte)cond, (byte)1, (sbyte)-7, (byte)0 /* :fatcat: */ }));
                        foreach (int alt in new[] { trigger }.Concat(bossTriggerAdd[trigger]))
                        {
                            evt.Instructions.Insert(index, new EMEVD.Instruction(3, 2, new List<object> { (sbyte)-7, (byte)1, 10000, alt }));
                        }
                    }
                }
                string evPath = $@"{editor.Spec.GameDir}\event\{Path.GetFileName(path)}";
                Console.WriteLine("Writing " + evPath);
                evd.Write(evPath);
            }

            foreach (KeyValuePair<string, MSB1> entry in msbs)
            {
                string map = nameSpecs[entry.Key].Map;
                string path = $@"{editor.Spec.GameDir}\map\MapStudio\{map}.msb";
                Console.WriteLine("Writing " + path);
                entry.Value.Write(path);
            }
            Console.WriteLine($@"Copying ESDs to {editor.Spec.GameDir}\{editor.Spec.EsdDir}");
            foreach (string path in Directory.GetFiles($@"{distBase}\{editor.Spec.EsdDir}", "*.talkesdbnd*"))
            {
                File.Copy(path, $@"{editor.Spec.GameDir}\{editor.Spec.EsdDir}\{Path.GetFileName(path)}", true);
            }
            // Hardcode some messages
            Console.WriteLine($@"Writing messages to {editor.Spec.GameDir}\msg\ENGLISH");
            fmgs[fmgEvent][15000280] = "Return to Asylum";
            fmgs[fmgEvent][15000281] = "Sealed in New Londo Ruins";
            fmgs[fmgEvent][15000282] = "Fog Gate Randomizer breaks when online.\nChange Launch setting in Network settings and then reload.";
            fmgs[fmgEvent][15000283] = "Go to jail";
            editor.OverrideBnd($@"{distBase}\msg\ENGLISH\menu.msgbnd{dcxExt}", @"msg\ENGLISH", fmgs, fmg => fmg.Write());
        }

        public static List<string> GetAllBaseFiles(FromGame game)
        {
            List<string> files = new List<string>();
            string distBase = $@"dist\{game}"; 
            if (!Directory.Exists(distBase)) return files;
            foreach (string path in Directory.GetFiles(distBase, "*.*", SearchOption.AllDirectories))
            {
                if (path.EndsWith(".dcx") || path.EndsWith(".msb") || path.EndsWith(".emevd") || path.EndsWith("bnd")) files.Add(path.Replace($@"{distBase}\", ""));
            }
            return files;
        }
    }
}
