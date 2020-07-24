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
        // These events are automatically (and manually, in the case of the last 4) added by enemy randomizer, so preserve them.
        private HashSet<int> enemyRandoEvents = new HashSet<int>(new[] {
            11009000,
            11019000,
            11029000,
            11109000,
            11209000,
            11219000,
            11309000,
            11319000,
            11329000,
            11409000,
            11419000,
            11509000,
            11519000,
            11609000,
            11709000,
            11809000,
            11819000,
        }.SelectMany(i => new[] { i - 200, i, i + 200 }).Concat(new[] {
            11211600,
            11505600,
            11515600,
            11700800,
        }));
        public void Write(RandomizerOptions opt, AnnotationData ann, Graph g, string gameDir, FromGame game)
        {
            GameEditor editor = new GameEditor(game);
            bool remastered = game == FromGame.DS1R;
            string distBase = $@"dist\{game}";
            editor.Spec.GameDir = distBase;
            Dictionary<string, MSB1> baseMaps = editor.Load(@"map\MapStudio", path => ann.Specs.ContainsKey(Path.GetFileNameWithoutExtension(path)) ? MSB1.Read(path) : null, "*.msb");
            Dictionary<string, PARAM.Layout> layouts = editor.LoadLayouts();
            Dictionary<string, PARAM> baseParams = editor.LoadParams(layouts);

            editor.Spec.GameDir = gameDir;
            Dictionary<string, MSB1> maps = editor.Load(@"map\MapStudio", path => ann.Specs.ContainsKey(Path.GetFileNameWithoutExtension(path)) ? MSB1.Read(path) : null, "*.msb");
            Dictionary<string, PARAM> Params = editor.LoadParams(layouts);
            string dcxExt = remastered ? ".dcx" : "";
            string fmgEvent = remastered ? "Event_text_" : "イベントテキスト";
            Dictionary<string, FMG> fmgs = editor.LoadBnd($@"{editor.Spec.GameDir}\msg\{opt.Language}\menu.msgbnd{dcxExt}", (data, name) => name == fmgEvent ? FMG.Read(data) : null);

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
                if (!ann.Specs.TryGetValue(entry.Key, out MapSpec spec)) continue;
                MSB1 msb = entry.Value;
                string name = spec.Name;
                msbs[name] = msb;
                players[name] = 0;
                // Preprocess them to remove any changes made by previous runs
                msb.Regions.Regions.RemoveAll(r =>
                {
                    if (r.Name.StartsWith("Boss start for ") || r.Name.StartsWith("FR: ") || r.Name.StartsWith("BR: ") || r.Name.StartsWith("Region for "))
                    {
                        if (opt["msbinfo"]) Console.WriteLine($"Removing region in {name}: {r.Name} #{r.EntityID}");
                        return true;
                    }
                    return false;
                });
                msb.Parts.Players.RemoveAll(p =>
                {
                    if (p.Name.StartsWith("c0000_") && int.TryParse(p.Name.Substring(6), out int res) && res >= 50)
                    {
                        if (opt["msbinfo"]) Console.WriteLine($"Removing player in {name}: {p.Name} #{p.EntityID}");
                        return true;
                    }
                    return false;
                });
            }

            // Do scaling
            HashSet<string> excludeScaling = new HashSet<string> { "c1000" };
            if (opt["dumpcols"])
            {
                Dictionary<string, string> models = editor.LoadNames("ModelName", n => n);
                Dictionary<int, string> chrs = editor.LoadNames("CharaInitParam", n => int.Parse(n));
                List<EnemyCol> cols = new List<EnemyCol>();
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
                        cols.Add(new EnemyCol
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
            Dictionary<(string, string), List<EnemyCol>> rankCols = new Dictionary<(string, string), List<EnemyCol>>();
            foreach (EnemyCol enemy in ann.Enemies)
            {
                string[] parts = enemy.Col.Split(' ');
                if (!ann.NameSpecs.ContainsKey(parts[0])) throw new Exception($"Unknown map in {enemy.Col}");
                if (!g.Areas.ContainsKey(enemy.Area)) throw new Exception($"Unknown area {enemy.Area} in {enemy.Col}");
                if (parts.Length == 1)
                {
                    rankDefaults[parts[0]] = enemy.Area;
                    continue;
                }
                if (parts.Length != 2) throw new Exception($"Bad format {enemy.Col}");
                AddMulti(rankCols, (parts[0], parts[1]), enemy);
            }

            // Clear out previous values
            Params["SpEffectParam"].Rows = Params["SpEffectParam"].Rows.Where(r => r.ID < 7200 || r.ID >= 7400).ToList();
            // We want to undo our previous work, if we need to change/remove scaling. However, don't also undo the clone bosses from enemy rando.
            HashSet<int> enemyRandoBossCopies = new HashSet<int>(new[] {
                223000, 223100, 223200, 224000, 225000, 232000, 236000, 236001, 273000, 323000, 332000, 343000, 347100, 410000,
                450000, 451000, 520000, 521000, 522000, 525000, 526000, 527000, 527100, 528000, 529000, 532000, 535000, 537000, 539001
            }.Select(c => c + 50));
            Dictionary<int, PARAM.Row> baseNpcs = baseParams["NpcParam"].Rows.ToDictionary(r => (int)r.ID, r => r);
            Params["GameAreaParam"] = baseParams["GameAreaParam"];
            foreach (KeyValuePair<string, MSB1> entry in msbs)
            {
                string map = entry.Key;
                MSB1 msb = entry.Value;
                foreach (MSB1.Part.Enemy e in msb.Parts.Enemies)
                {
                    int npc = e.NPCParamID;
                    if (npc >= 1000 && !baseNpcs.ContainsKey(npc) && !enemyRandoBossCopies.Contains(npc))
                    {
                        // If the NPC doesn't exist anymore, try to find what the original value was
                        // We leave ourselves a hint in a probably unused field.
                        PARAM.Row original = Params["NpcParam"][npc];
                        if (original != null && (byte)original["pcAttrB"].Value != 0)
                        {
                            int attempt = BitConverter.ToInt32("BWLR".Select(c => (byte)original[$"pcAttr{c}"].Value).ToArray(), 0);
                            if (baseNpcs.ContainsKey(attempt))
                            {
                                e.NPCParamID = attempt;
                                continue;
                            }
                        }
                        // Or if the enemy isn't randomized, we can try to use the value from the original map
                        MSB1.Part.Enemy enemy = baseMaps[ann.NameSpecs[map].Map].Parts.Enemies.Find(c => c.Name == e.Name);
                        if (enemy != null && e.ModelName == enemy.ModelName)
                        {
                            e.NPCParamID = enemy.NPCParamID;
                            continue;
                        }
                        // Otherwise, just leave it buggy, probably.
                        // It could work to decrement until we find a valid value, but that may produce weird variants of the enemy, or a different one entirely.
                    }
                }
            }
            Params["NpcParam"].Rows.RemoveAll(r => !baseNpcs.ContainsKey((int)r.ID) && !enemyRandoBossCopies.Contains((int)r.ID));

            if (opt["scale"])
            {
                Dictionary<int, PARAM.Row> newNpcs = Params["NpcParam"].Rows.ToDictionary(r => (int)r.ID, r => r);
                // Add new scalings
                HashSet<string> rankCells = new HashSet<string>(
                    ("maxHpRate maxStaminaRate physicsAttackPowerRate magicAttackPowerRate fireAttackPowerRate thunderAttackPowerRate " +
                    "physicsDiffenceRate magicDiffenceRate fireDiffenceRate thunderDiffenceRate staminaAttackRate").Split(' '));
                HashSet<string> dmgCells = new HashSet<string>(
                    "physicsAttackPowerRate magicAttackPowerRate fireAttackPowerRate thunderAttackPowerRate".Split(' '));
                // 7001 is 1 physattackpower. 7015 is 2.5. (defense scales from 1 to 3. stamina attack rate scales from 1 to 2.)
                Dictionary<int, PARAM.Row> baseRanks = baseParams["SpEffectParam"].Rows.Where(r => r.ID >= 7001 && r.ID <= 7015).ToDictionary(r => (int)r.ID, r => r);
                PARAM.Row baseRank = baseRanks[7015];
                List<float> rankRatios = new List<float>();
                const int range = 40;
                const int middle = range / 2;
                List<float> dmgMults = new List<float> { 1, 1.15f, 1.3f, 0.9f, 0.8f };
                for (int d = 0; d < dmgMults.Count; d++)
                {
                    for (int i = 0; i <= range; i++)
                    {
                        PARAM.Row rank = new PARAM.Row(7200 + i + range * d, null, Params["SpEffectParam"].AppliedParamdef); // SP_EFFECT_PARAM_ST
                        // float ratio = 0.3f + (3 - 0.3f) * i / range;  // arithmetic ranking
                        float ratio = (float)Math.Pow(4, 1.0 * (i - middle) / middle);  // logarithmic ranking
                        rankRatios.Add(ratio);
                        foreach (PARAM.Cell cell in rank.Cells)
                        {
                            PARAM.Cell baseCell = baseRank[cell.Def.InternalName];
                            if (rankCells.Contains(cell.Def.InternalName))
                            {
                                // if ratio is 1, value should be 1.
                                // if ratio is 2.5, value should be the same as base row.
                                float baseRatio = (float)baseCell.Value / 2.5f;
                                float rankValue = ratio;
                                if (rankValue >= 1) rankValue *= baseRatio;
                                else rankValue /= baseRatio;
                                if (dmgCells.Contains(cell.Def.InternalName)) rankValue *= dmgMults[d];
                                cell.Value = rankValue;
                            }
                            else
                            {
                                cell.Value = baseCell.Value;
                            }
                        }
                        Params["SpEffectParam"].Rows.Add(rank);
                    }
                }

                int findSpEffect(float val)
                {
                    for (int i = 0; i < rankRatios.Count; i++)
                    {
                        float ratio = rankRatios[i];
                        if (ratio >= val) return 7200 + i;
                    }
                    return 7200 + rankRatios.Count - 1;
                }
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
                        if (rankCols.TryGetValue((map, e.CollisionName), out List<EnemyCol> enemies))
                        {
                            area = enemies[0].Area;
                            if (enemies.Count > 1)
                            {
                                foreach (EnemyCol enemy in enemies)
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
                        (float ratio, float dmgRatio) = area != null && g.AreaRatios.TryGetValue(area, out (float, float) val) ? val : (1, 1);
                        if (Math.Abs(ratio - 1) < 0.01f) continue;
                        float initialRatio = 1;
                        int baseSp = -1;
                        // If it's not from the base game, don't otherwise touch it
                        if (!baseNpcs.TryGetValue(npcID, out PARAM.Row baseNpc))
                        {
                            continue;
                        }
                        if (npcID >= 120000)
                        {
                            baseSp = (int)baseNpc["spEffectID4"].Value;
                            if (baseRanks.TryGetValue(baseSp, out PARAM.Row spRow))
                            {
                                initialRatio = (float)spRow["physicsAttackPowerRate"].Value;
                            }
                        }
                        float ratioFromBase = ratio * initialRatio;
                        int sp = findSpEffect(ratioFromBase);
                        PARAM.Row newNpc;
                        // Quelaag broked under certain circumstances
                        if (npcID == 528000 && sp < 7200 + middle)
                        {
                            sp = 7200 + middle;
                        }
                        else if (dmgRatio / ratio > 1.15)
                        {
                            // Use more damage dealing version if meets threshhold (due to being scaled down)
                            sp += range;
                            if (dmgRatio / ratio > 1.3)
                            {
                                sp += range;
                            }
                        }
                        else if (dmgRatio / ratio < 0.9)
                        {
                            sp += range * 3;
                            if (dmgRatio / range < 0.8)
                            {
                                sp += range;
                            }
                        }
                        if (npcType.Value.Count == 1)
                        {
                            // If only one NPC, replace it
                            newNpc = newNpcs[npcID];
                            if (npcID == 528000 && ratioFromBase < 1)
                            {
                                newNpc["hp"].Value = (uint)((uint)baseNpc["hp"].Value * ratio);
                            }
                        }
                        else
                        {
                            // Make a new one otherwise
                            int newID = npcID;
                            while (newNpcs.ContainsKey(newID)) newID++;
                            newNpc = new PARAM.Row(newID, null, Params["NpcParam"].AppliedParamdef);  // NPC_PARAM_ST
                            foreach (PARAM.Cell cell in newNpc.Cells)
                            {
                                cell.Value = baseNpc[cell.Def.InternalName].Value;
                            }
                            newNpcs[newID] = newNpc;
                            // Use an unused field to refer to the original value
                            byte[] o = BitConverter.GetBytes(npcID);
                            for (int i = 0; i < o.Length; i++)
                            {
                                newNpc[$"pcAttr{"BWLR"[i]}"].Value = o[i];
                            }
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
                                    PARAM.Row gameArea = baseParams["GameAreaParam"][enemy.EntityID];
                                    if (gameArea != null)
                                    {
                                        souls = (uint)gameArea["bonusSoul_single"].Value;
                                        newSouls = getNewSouls(souls, ratio);
                                        PARAM.Row newArea = Params["GameAreaParam"][enemy.EntityID];
                                        newArea["bonusSoul_single"].Value = newSouls;
                                        newArea["bonusSoul_multi"].Value = newSouls;
                                    }
                                }
                            }
                        }
                        if (opt["debugscale"])
                        {
                            Console.WriteLine($"Change for {npcID} {area}: {ratio} * {initialRatio} = {ratio * initialRatio}, dmg ratio {dmgRatio / ratio}, sp {baseSp} -> {sp}. "
                                + $"Souls {souls} -> {newSouls}. {(npcType.Value.Count == 1 ? " UNIQUE" : "")} {(newSouls > 50000 ? "BIG" : "")}");
                        }
                    }
                }
            }
            else
            {
                // If not scaling, undo all scales
                foreach (PARAM.Row row in Params["NpcParam"].Rows)
                {
                    if (baseNpcs.TryGetValue((int)row.ID, out PARAM.Row baseNpc))
                    {
                        row["getSoul"].Value = baseNpc["getSoul"].Value;
                        // Replace our hacky speffect with the real one
                        if (row.ID >= 120000)
                        {
                            row[$"spEffectID4"].Value = baseNpc[$"spEffectID4"].Value;
                        }
                        else
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                int sp = (int)row[$"spEffectID{i}"].Value;
                                if (sp >= 7200 && sp < 7400)
                                {
                                    row[$"spEffectID{i}"].Value = baseNpc[$"spEffectID{i}"].Value;
                                }
                            }
                        }
                    }
                }
            }

            // Write stuff
            int mk = 1815800;
            int slot = 0;
            (byte, byte) GetDest(string map)
            {
                MapSpec spec = ann.NameSpecs[map];
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
                    if (g.Ignore.Contains((e.Name, side.Area))) continue;
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
            string[] respawnParts = g.Start.Respawn.Split(' ');
            string startMap = respawnParts[0];
            int startRespawn = int.Parse(respawnParts[1]);
            {
                MSB1 msb = msbs[startMap];
                MSB1.Part.Player p = new MSB1.Part.Player();
                p.Name = $"c0000_{50 + players[startMap]++:d4}";
                p.ModelName = "c0000";
                p.EntityID = softWarp;
                MSB1.Event.SpawnPoint spawn = msb.Events.SpawnPoints.Find(e => e.EntityID == startRespawn);
                if (spawn == null) throw new Exception($"Bad custom start {g.Start.Respawn}, can't find spawn point {startRespawn}");
                MSB1.Region region = msb.Regions.Regions.Find(e => e.Name == spawn.SpawnPointName);
                if (region == null) throw new Exception($"Bad custom start {g.Start.Respawn}, can't find region {spawn.SpawnPointName}");
                p.Position = region.Position;
                p.Rotation = region.Rotation;
                p.Scale = new Vector3(1, 1, 1);
                msb.Parts.Players.Add(p);
            }
            // Various MSB edits. Lots of making collisions behave nicely on save+quit.
            List<int> playRegions = new List<int>();
            Dictionary<string, string> colNames = new Dictionary<string, string>();
            foreach (KeyValuePair<string, MSB1> entry in msbs)
            {
                string map = entry.Key;
                MSB1 msb = entry.Value;
                foreach (MSB1.Part.Collision col in msb.Parts.Collisions)
                {
                    if (col.PlayRegionID < 10)
                    {
                        // Editing some play regions. Not sure which of these are still needed, but some prevent unstable ground after warps.
                        if (map == "firelink" && col.Name == "h0017B2_0000")
                        {
                            col.PlayRegionID = -69696968 - 10;
                        }
                        if (false && map == "demonruins" && col.Name == "h0005B1")
                        {
                            col.PlayRegionID = 141000;
                        }
                        else if (map == "firelink" && (col.Name == "h0017B2_0000" || col.Name == "h0015B2_0000"))
                        {
                            col.PlayRegionID = -2;
                        }
                        else if (ann.DefaultFlagCols.TryGetValue($"{map}_{col.Name}", out int bossFlag))
                        {
                            int colFlag = playRegions.IndexOf(bossFlag);
                            if (colFlag == -1)
                            {
                                colFlag = playRegions.Count;
                                playRegions.Add(bossFlag);
                            }
                            col.PlayRegionID = -(tempColEventBase + colFlag) - 10;
                        }
                        else if (col.PlayRegionID < -10)
                        {
                            // If this is not a region we know about, leave it alone and hope it doesn't cause trouble.
                            // In all cases I've tried, all of the above cases cover it.
                        }
                    }
                }
                // Misc map changes
                if (map == "demonruins")
                {
                    msb.Parts.Collisions = msb.Parts.Collisions.Where(c => c.Name != "h9950B1").ToList();
                }
                else if (map == "dlc")
                {
                    msb.Parts.Collisions = msb.Parts.Collisions.Where(c => c.Name != "h7800B1").ToList();
                }
                else if (map == "totg")
                {
                    // Nudge the outside fog gate region to be inside & detect boss battle mode
                    MSB1.Region r = msb.Regions.Regions.Find(c => c.EntityID == 1312998);
                    r.Position = new Vector3(-118.186f, -250.591f, -31.893f);
                    if (!(r.Shape is MSB1.Shape.Box box)) throw new Exception("Unexpected region");
                    box.Width = 10;
                    box.Height = 5;
                }
                else if (map == "kiln")
                {
                    // This option does not get enabled from anywhere currently
                    if (opt["patchkiln"])
                    {
                        MSB1.Part.Player player = msb.Parts.Players.Find(p => p.Name == "c0000_0000");
                        player.Position = new Vector3(50.3f, -63.27f, 106.1f);
                        player.Rotation = new Vector3(0, -105, 0);
                    }
                }
                else if (map == "anorlondo")
                {
                    MSB1.Part.Object obj = msb.Parts.Objects.Find(e => e.Name == "o0500_0006");
                    // If the area after the broken window is part of logic, use it, otherwise move it back
                    if (g.EntranceIds["o5869_0000"].IsFixed)
                    {
                        // Default properties
                        obj.Position = new Vector3(448.490f, 144.110f, 269.420f);
                        obj.Rotation = new Vector3(11, 83, -4);
                        obj.CollisionName = "h0111B1_0000";
                        obj.UnkT0C = 33; // initial animation
                    }
                    else
                    {
                        obj.Position = new Vector3(444.106f, 160.258f, 255.887f);
                        obj.Rotation = new Vector3(-3, -90, 0);
                        obj.CollisionName = "h0025B1_0000";
                        obj.UnkT0C = 50; // initial animation
                    }
                }
                else if (map == "depths")
                {
                    MSB1.Event.ObjAct oa = msb.Events.ObjActs.Find(o => o.ObjActEntityID == 11000120);
                    if (g.Start.Area == "depths")
                    {
                        // No key required if starting at bonfire
                        oa.ObjActParamID = -1;
                    }
                    else
                    {
                        // Default value
                        oa.ObjActParamID = 11315;
                    }
                }
                else if (map == "asylum")
                {
                    // Move asylum demon trigger region
                    MSB1.Region r = msb.Regions.Regions.Find(e => e.EntityID == 1812998);
                    r.Position = new Vector3(3.2f, 209f, -33.089f);
                    // Move starter objects
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
                    // Add estus treasure if it does not exist
                    if (!msb.Parts.Objects.Any(p => p.Name == "o0500_0050"))
                    {
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
                }
                else if (map == "dukes")
                {
                    if (!msb.Parts.Objects.Any(p => p.Name == "o7500_0001"))
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
                    }

                    MSB1.Region spawnRegion = msb.Regions.Regions.Find(r => r.Name == "復活ポイント（一時用）");
                    spawnRegion.EntityID = 1702901;
                }
            }

            int connectColId = 10;
            int traverseFlag = 6920;
            HashSet<string> vanillaEntrances = new HashSet<string>();
            foreach (Node node in g.Nodes.Values)
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
                        Entrance e = g.EntranceIds[exit.Name];
                        if (vanillaEntrances.Contains(e.FullName)) continue;
                        if (e.HasTag("pvp"))
                        {
                            AddWarpEvent(1, e.Area, new List<int> { e.ID, e.ID + 1 });
                            vanillaEntrances.Add(e.FullName);
                            continue;
                        }
                        else if (e.HasTag("world") && exit.Pair != null)
                        {
                            WarpPoint aPair = exit.Pair.Side.Warp;
                            WarpPoint bPair = entrance.Pair.Side.Warp;
                            if (aPair == null || bPair == null) throw new Exception($"Missing warp info - {a == null} {b == null} for {exit.Pair} -> {entrance.Pair}");
                            AddWarpEvent(3, e.Area, new List<int> { e.ID, e.ID + 1, traverseFlag, a.Action, bPair.Action });
                            traverseFlag++;
                            vanillaEntrances.Add(e.FullName);
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
                        con.Name = $"{baseCol.ModelName}_{connectColId++:d4}";
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
            // Enemy randomizer changes
            // Add gravity to undead dragon (done in fog emevd)
            // Remove move/force animation from 11810310, when not present there
            // 11205382, 11015382, 11015396 have no modifications in fog rando, so they can be copied over, as they are conditionally applied there.
            // Other events are changes in the dkscript map files.
            List<int> copyRandoEvents = new List<int> {
                // Moonlight butterfly start
                11205382,
                // Gargoyle 2
                11015382,
                11015396,
                // Seath invincibility
                11705396,
                // Bed of chaos start
                11415392,
                // Bed of chaos part invincibility
                11415397,
                // Bed of chaos object invulnerabilities
                11410250,
                // Skeleton respawning in catacombs. Parameterized
                11305100,
            };
            foreach (string path in Directory.GetFiles($@"{distBase}\event", "*.emevd*"))
            {
                string name = GameEditor.BaseName(path);
                EMEVD evd = EMEVD.Read(path);
                string evPath = $@"{editor.Spec.GameDir}\event\{Path.GetFileName(path)}";
                EMEVD gameEvd = EMEVD.Read(evPath);
                List<EMEVD.Instruction> inits = new List<EMEVD.Instruction>();
                Dictionary<int, EMEVD.Event> eventsToCopy = new Dictionary<int, EMEVD.Event>();
                bool asylumDemonRandomized = false;
                // Preprocess things from enemy randomizer. This should be idempotent.
                foreach (EMEVD.Event evt in gameEvd.Events)
                {
                    if (enemyRandoEvents.Contains((int)evt.ID))
                    {
                        evd.Events.Add(evt);
                    }
                    else if (copyRandoEvents.Contains((int)evt.ID))
                    {
                        eventsToCopy[(int)evt.ID] = evt;
                    }
                    else if (evt.ID == 11810310)
                    {
                        asylumDemonRandomized = !evt.Instructions.Any(instr => instr.Bank == 2003 && instr.ID == 18);
                    }
                    else if (evt.ID == 0)
                    {
                        for (int i = evt.Instructions.Count - 1; i >= 0; i--)
                        {
                            EMEVD.Instruction instr = evt.Instructions[i];
                            if (instr.Bank == 2004 && instr.ID == 2)
                            {
                                inits.Add(instr);
                            }
                            else if (instr.Bank == 2000 && instr.ID == 0)
                            {
                                List<object> initArgs = instr.UnpackArgs(Enumerable.Repeat(ArgType.Int32, instr.ArgData.Length / 4));
                                if (enemyRandoEvents.Contains((int)initArgs[1]))
                                {
                                    inits.Add(instr);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                foreach (EMEVD.Event evt in evd.Events)
                {
                    // Copy stuff from enemy randomizer
                    if (evt.ID == 0)
                    {
                        inits.Reverse();
                        evt.Instructions.AddRange(inits);
                    }
                    if (eventsToCopy.TryGetValue((int)evt.ID, out EMEVD.Event other))
                    {
                        evt.Instructions = other.Instructions;
                        evt.Parameters = other.Parameters;
                    }
                    if (evt.ID == 11810310 && asylumDemonRandomized)
                    {
                        evt.Instructions.RemoveAll(instr => (instr.Bank == 2003 && instr.ID == 18) || (instr.Bank == 2004 && instr.ID == 41));
                    }
                    // Add our own stuff
                    if (evt.ID == 0 && name == "common")
                    {
                        foreach (List<object> init in events)
                        {
                            evt.Instructions.Add(new EMEVD.Instruction(2000, 0, init));
                        }
                        (byte area, byte block) = GetDest(startMap);
                        evt.Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { 0, (uint)8900, area, block, softWarp }));
                        for (int i = 0; i < playRegions.Count; i++)
                        {
                            evt.Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { i, (uint)8901, tempColEventBase + i, playRegions[i] }));
                        }
                        if (opt["start"])
                        {
                            evt.Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { 0, (uint)8950, area, block, softWarp, startRespawn }));
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
                Console.WriteLine("Writing " + evPath);
                evd.Write(evPath);
            }

            foreach (KeyValuePair<string, MSB1> entry in msbs)
            {
                string map = ann.NameSpecs[entry.Key].Map;
                string path = $@"{editor.Spec.GameDir}\map\MapStudio\{map}.msb";
                Console.WriteLine("Writing " + path);
                entry.Value.Write(path);
            }
            Console.WriteLine($@"Writing {editor.Spec.GameDir}\{editor.Spec.ParamFile}");
            editor.OverrideBnd($@"{editor.Spec.GameDir}\{editor.Spec.ParamFile}", @"param\GameParam", Params, f => f.Write());
            Console.WriteLine($@"Copying ESDs to {editor.Spec.GameDir}\{editor.Spec.EsdDir}");
            foreach (string path in Directory.GetFiles($@"{distBase}\{editor.Spec.EsdDir}", "*.talkesdbnd*"))
            {
                File.Copy(path, $@"{editor.Spec.GameDir}\{editor.Spec.EsdDir}\{Path.GetFileName(path)}", true);
            }
            // Hardcode some messages
            Console.WriteLine($@"Writing messages to {editor.Spec.GameDir}\msg\{opt.Language}");
            fmgs[fmgEvent][15000280] = $"Return to {g.Start.Name}";
            fmgs[fmgEvent][15000281] = "Sealed in New Londo Ruins";
            fmgs[fmgEvent][15000282] = "Fog Gate Randomizer breaks when online.\nChange Launch setting in Network settings and then reload.";
            fmgs[fmgEvent][15000283] = g.EntranceIds["1702901"].IsFixed ? "Go to jail" : "Go to jail (randomized warp)";
            editor.OverrideBnd($@"{editor.Spec.GameDir}\msg\{opt.Language}\menu.msgbnd{dcxExt}", $@"msg\{opt.Language}", fmgs, fmg => fmg.Write());
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
