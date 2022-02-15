using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
using static FogMod.EventConfig;
using static SoulsIds.Events;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public class GameDataWriter3
    {
        private static readonly List<string> extraEmevd = new List<string> { "common", "common_func" };
        public void Write(RandomizerOptions opt, AnnotationData ann, Graph g, string gameDir, string outDir, Events events, EventConfig eventConfig)
        {
            GameEditor editor = new GameEditor(FromGame.DS3);
            editor.Spec.GameDir = $@"fogdist";
            editor.Spec.LayoutDir = $@"fogdist\Layouts";
            editor.Spec.NameDir = $@"fogdist\Names";

            Dictionary<string, PARAM.Layout> layouts = editor.LoadLayouts();

            bool validEmevd(string name) => ann.Specs.ContainsKey(name) || extraEmevd.Contains(name);

            Dictionary<string, PARAM> Params;
            {
                string path = @"fogdist\Base\Data0.bdt";
                string altPath = $@"{gameDir}\Data0.bdt";
                if (gameDir != null && File.Exists(altPath))
                {
                    Console.WriteLine($"Using override {altPath}");
                    path = altPath;
                }
                Params = editor.LoadParams(path, layouts, true);
            }

            Dictionary<string, FMG> menuFMGs;
            {
                string path = @"fogdist\Base\menu_dlc2.msgbnd.dcx";
                string altPath = $@"{gameDir}\msg\engus\menu_dlc2.msgbnd.dcx";
                if (gameDir != null && File.Exists(altPath))
                {
                    Console.WriteLine($"Using override {altPath}");
                    path = altPath;
                }
                menuFMGs = editor.LoadBnd(path, (data, p) => FMG.Read(data));
            }

            // Overrides where only one copy is needed
            Dictionary<string, MSB3> maps = new Dictionary<string, MSB3>();
            foreach (string basePath in Directory.GetFiles($@"fogdist\Base", "*.msb.dcx"))
            {
                string path = basePath;
                string name = GameEditor.BaseName(path);
                if (!ann.Specs.ContainsKey(name)) continue;
                string altPath = $@"{gameDir}\map\mapstudio\{name}.msb.dcx";
                if (gameDir != null && File.Exists(altPath))
                {
                    Console.WriteLine($"Using override {altPath}");
                    path = altPath;
                }
                maps[name] = MSB3.Read(path);
            }

            Dictionary<string, EMEVD> emevds = new Dictionary<string, EMEVD>();
            foreach (string basePath in Directory.GetFiles($@"fogdist\Base", "*.emevd.dcx"))
            {
                string path = basePath;
                string name = GameEditor.BaseName(path);
                if (!validEmevd(name)) continue;
                string altPath = $@"{gameDir}\event\{name}.emevd.dcx";
                if (gameDir != null && File.Exists(altPath))
                {
                    Console.WriteLine($"Using override {altPath}");
                    path = altPath;
                }
                emevds[name] = EMEVD.Read(path);
            }

            Dictionary<string, Dictionary<string, ESD>> esds = new Dictionary<string, Dictionary<string, ESD>>();
            foreach (string basePath in Directory.GetFiles($@"fogdist\Base", "*.talkesdbnd.dcx"))
            {
                string path = basePath;
                string name = GameEditor.BaseName(path);
                if (!ann.Specs.ContainsKey(name)) continue;
                string altPath = $@"{gameDir}\script\talk\{name}.talkesdbnd.dcx";
                if (gameDir != null && File.Exists(altPath))
                {
                    Console.WriteLine($"Using override {altPath}");
                    path = altPath;
                }
                esds[name] = editor.LoadBnd(path, (data, p) => ESD.Read(data));
            }

            // TODO: Backup? Not really modifying the game files, but still in-place modification of something else

            // Load msbs
            Dictionary<string, MSB3> msbs = new Dictionary<string, MSB3>();
            Dictionary<string, int> players = new Dictionary<string, int>();
            foreach (KeyValuePair<string, MSB3> entry in maps)
            {
                if (!ann.Specs.TryGetValue(entry.Key, out MapSpec spec)) continue;
                MSB3 msb = entry.Value;
                string name = spec.Name;
                msbs[name] = msb;
                players[name] = 0;
                // Preprocess them to remove any changes made by previous runs
                msb.Regions.Events.RemoveAll(r =>
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
            Dictionary<string, Ceremony> ceremonyAlias = new Dictionary<string, Ceremony>
            {
                ["firelink"] = new Ceremony
                {
                    Map = "firelink",
                    ID = 0,
                    MapLayer = 1,
                    EventLayer = 1,
                },
                ["untended"] = new Ceremony
                {
                    Map = "firelink",
                    ID = 10,
                    MapLayer = 2,
                    EventLayer = 1022,
                },
            };

            // Write stuff
            int mk = 4105800;
            string mapFromName(string map)
            {
                if (ceremonyAlias.TryGetValue(map, out Ceremony ceremony)) map = ceremony.Map;
                if (!ann.NameSpecs.TryGetValue(map, out MapSpec spec)) throw new Exception($"Unknown map {map}");
                return spec.Map;
            }
            (byte, byte) getDest(string map)
            {
                string full = mapFromName(map);
                return (byte.Parse(full.Substring(1, 2)), byte.Parse(full.Substring(4, 2)));
            }
            MSB3 getMap(string area)
            {
                if (ceremonyAlias.TryGetValue(area, out Ceremony ceremony)) area = ceremony.Map;
                if (!msbs.TryGetValue(area, out MSB3 msb)) throw new Exception($"Unknown area for placing map object {area}");
                return msb;
            }
            string newEntityId(string area, string model)
            {
                if (ceremonyAlias.TryGetValue(area, out Ceremony ceremony)) area = ceremony.Map;
                return $"{model}_{50 + players[area]++:d4}";
            }
            int getNameFlag(string name, string sideArea, Func<Area, int> field)
            {
                if (name == null) return 0;
                if (name == "area") name = sideArea;
                if (g.Areas.TryGetValue(name, out Area areaDef))
                {
                    int val = field(areaDef);
                    if (val == 0) throw new Exception($"Internal error: no flag {name} in {sideArea}");
                    return val;
                }
                else if (int.TryParse(name, out int val)) return val;
                throw new Exception($"Internal error: bad flag name {name} in {sideArea}");
            }

            HashSet<int> nonfixed = new HashSet<int>(ann.Entrances.Concat(ann.Warps).Where(e => !e.HasTag("unused") && !e.IsFixed).Select(e => e.ID));
            Dictionary<string, List<(string, int)>> bossTriggerRegions = new Dictionary<string, List<(string, int)>>();
            foreach (Entrance e in ann.Entrances)
            {
                // unused and norandom have behavior configured elsewhere, either without or with routing relatively
                // door is kept as-is... effectively treated as norandom here
                if (e.HasTag("unused") || e.HasTag("norandom") || e.HasTag("door")) continue;
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
                    MSB3 msb = getMap(map);
                    MSB3.Part.Object fog = getMap(e.Area).Parts.Objects.Find(o => o.Name == e.Name);
                    fog.CollisionName = null;

                    // In Firelink Shrine, firelink is map layer 1 and untended is map layer 2
                    if (fog.EntityID == 4001101 || fog.EntityID == 4001102)
                    {
                        fog.MapStudioLayer = 1;
                    }
                    // Moving Midir fog gate. But it is a dropdown anyway, so just use CustomWarp in the config instead
                    if (fog.EntityID == 5101850)
                    {
                        // fog.Position = new Vector3(-481.853f, -83.045f, -396.253f - 1.5f);
                    }
                    // Make Ancient Wyvern -> Mausoleum fog gate visible from both sides, using the door which opens after the fight
                    if (fog.EntityID == 3201801)
                    {
                        MSB3.Part.Object door = msbs["archdragon"].Parts.Objects.Find(o => o.EntityID == 3201420);
                        if (door != null)
                        {
                            for (int j = 0; j < door.DrawGroups.Length; j++)
                            {
                                fog.DrawGroups[j] |= door.DrawGroups[j];
                            }
                        }
                    }

                    Vector3 fogPosition = fog.Position;
                    float warpDist = 1f;
                    // Action trigger, in this case the fog object itself
                    int actionID;

                    // Find opposite direction
                    float rot = fog.Rotation.Y + 180;
                    rot = rot >= 180 ? rot - 360 : rot;
                    Vector3 opposite = new Vector3(fog.Rotation.X, rot, fog.Rotation.Z);

                    if (front && !e.IsFixed)
                    {
                        // Make an invisible fog gate in the other direction, to make opposite warp work
                        // But only if not fixed... otherwise, it will just be an invisible wall
                        MSB3.Part.Object oppositeFog = (MSB3.Part.Object)fog.DeepCopy();
                        oppositeFog.Name = newEntityId(map, fog.ModelName);
                        oppositeFog.Rotation = opposite;
                        oppositeFog.EntityID = mk++;
                        actionID = oppositeFog.EntityID;
                        msb.Parts.Objects.Add(oppositeFog);
                    }
                    else
                    {
                        actionID = fog.EntityID;
                    }

                    // Warp
                    string playerArea;
                    MSB3.Part.Player p;
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
                        if (side.HasTag("higher"))
                        {
                            warpPosition = new Vector3(warpPosition.X, warpPosition.Y + 1, warpPosition.Z);
                        }
                        if (e.AdjustHeight > 0)
                        {
                            warpPosition = new Vector3(warpPosition.X, warpPosition.Y + e.AdjustHeight, warpPosition.Z);
                        }
                        if (side.AdjustHeight > 0)
                        {
                            warpPosition = new Vector3(warpPosition.X, warpPosition.Y + side.AdjustHeight, warpPosition.Z);
                        }

                        playerArea = map;
                        p = new MSB3.Part.Player();
                        p.Name = newEntityId(playerArea, "c0000");
                        p.MapStudioLayer = uint.MaxValue;
                        p.ModelName = "c0000";
                        p.EntityID = warpID;
                        p.Position = warpPosition;
                        p.Rotation = warpRotation;
                        p.Scale = new Vector3(1, 1, 1);
                        msb.Parts.Players.Add(p);
                        side.Warp = new WarpPoint { ID = e.ID, Map = playerArea, Action = actionID, Player = warpID, ToFront = !front };
                    }
                    else
                    {
                        string[] parts = side.CustomWarp.Split(' ');
                        playerArea = parts[0];
                        MSB3 customMsb = getMap(playerArea);
                        List<float> pos = parts.Skip(1).Select(c => float.Parse(c, CultureInfo.InvariantCulture)).ToList();

                        p = new MSB3.Part.Player();
                        p.Name = newEntityId(playerArea, "c0000");
                        p.MapStudioLayer = uint.MaxValue;
                        p.ModelName = "c0000";
                        p.EntityID = warpID;
                        p.Position = new Vector3(pos[0], pos[1], pos[2]);
                        p.Rotation = new Vector3(0, pos[3], 0);
                        p.Scale = new Vector3(1, 1, 1);
                        customMsb.Parts.Players.Add(p);
                        side.Warp = new WarpPoint { ID = e.ID, Map = playerArea, Action = actionID, Player = warpID, ToFront = !front };
                    }
                    if (side.BossTriggerName != null)
                    {
                        string triggerArea = side.BossTriggerName == "area" ? side.Area : side.BossTriggerName;
                        MSB3.Region.Event r = new MSB3.Region.Event();
                        r.Name = $"Region for {p.Name} {p.EntityID}";
                        r.EntityID = mk++;
                        r.Position = new Vector3(p.Position.X, p.Position.Y - 1, p.Position.Z);
                        r.Rotation = p.Rotation;
                        r.MapStudioLayer = uint.MaxValue;
                        r.Shape = new MSB.Shape.Box
                        {
                            Width = 1.5f,
                            Depth = 1.5f,
                            Height = 4f,
                        };
                        msbs[playerArea].Regions.Events.Add(r);
                        AddMulti(bossTriggerRegions, triggerArea, (playerArea, r.EntityID));
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

            // Preprocess connections for warps, so that they can be replaced in first event pass
            Dictionary<string, Side> warpDests = new Dictionary<string, Side>();
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
                        if (exit.IsFixed && entrance.IsFixed) continue;
                        throw new Exception($"Missing warps - {a == null} {b == null} for {exit} -> {entrance}");
                    }
                    if (a.Action != 0) continue;
                    if (exit.Name == entrance.Name && exit.IsFixed && !opt["alwaysshow"])
                    {
                        continue;
                    }
                    warpDests[exit.Name] = entrance.Side;
                }
            }

            int getPlayer(WarpPoint warp)
            {
                if (warp.Player != 0) return warp.Player;
                MSB3 msb = msbs[warp.Map];
                MSB3.Region region = msb.Regions.GetEntries().Where(r => r.EntityID == warp.Region).FirstOrDefault();
                if (region == null) throw new Exception($"Cutscene warp destination {warp.Region} not found in {warp.Map}");
                MSB3.Part.Player p = new MSB3.Part.Player();
                p.Name = newEntityId(warp.Map, "c0000");
                p.ModelName = "c0000";
                p.EntityID = mk++;
                p.Position = region.Position;
                p.Rotation = region.Rotation;
                p.Scale = new Vector3(1, 1, 1);
                msb.Parts.Players.Add(p);
                warp.Player = p.EntityID;
                return warp.Player;
            }

            Dictionary<int, int> createFirelinkPlayerRegions = new Dictionary<int, int>
            {
                [4000970] = 4000960,  // Firelink
                [4000971] = 4000961,  // Cemetery
                [4000972] = 4000962,  // Iudex
                [4000973] = 4000963,  // Untended
                [4000974] = 4000964,  // Champion
            };
            foreach (KeyValuePair<int, int> copy in createFirelinkPlayerRegions)
            {
                MSB3.Part.Player p = msbs["firelink"].Parts.Players.Find(e => e.EntityID == copy.Key);
                MSB3.Region.Event r = new MSB3.Region.Event();
                r.Name = $"Region for {p.Name} {p.EntityID}";
                r.EntityID = copy.Value;
                r.Position = p.Position;
                r.Rotation = p.Rotation;
                r.MapStudioLayer = uint.MaxValue;
                r.Shape = new MSB.Shape.Sphere(1f);
                msbs["firelink"].Regions.Events.Add(r);
            }

            Dictionary<string, List<string>> badCols = new Dictionary<string, List<string>>
            {
                ["highwall"] = new List<string> { "h900200", "h900402", "h900403", "h900500" },  // h080090 is places
                ["lothric"] = new List<string> { "h900400" },
                ["settlement"] = new List<string> { "h009001", "h009005" },
                ["archdragon"] = new List<string> { "h900500", "h900600" },
                ["farronkeep"] = new List<string> { "h025550", "h025560" },
                ["archives"] = new List<string> { "h900400", "h900430" },
                ["cathedral"] = new List<string> { "h001270" },
                ["irithyll"] = new List<string> { "h900200", "h900500" },
                ["catacombs"] = new List<string> { "h020002", "h020003", "h006002" },  // Last one is a bit out of place, for start of area
                ["dungeon"] = new List<string> { "h009000" },  // ?
                ["firelink"] = new List<string> { "h900200" },
                ["kiln"] = new List<string> { "h900600" },
                ["ariandel"] = new List<string> { "h900770", "h900780" },
                ["dregheap"] = new List<string> { "h900770", "h900780" },
                ["ringedcity"] = new List<string> { "h900800", "h900900" },  // h900200 ? If can't Midir save and quit
                ["filianore"] = new List<string> { "h900770", "h900780" },
            };

            foreach (KeyValuePair<string, List<string>> entry in badCols)
            {
                msbs[entry.Key].Parts.Collisions.RemoveAll(c => entry.Value.Contains(c.Name));
            }

            // Rewrite collision flags in PlayRegionParam, read from base params, write to modified params
            // Also add common_func for it

            // Some misc tasks to double-check
            // Some regions may also need to be made saveable... perhaps if they have -1 play region currently?
            // Any changes to make some starting locations work? like Depths one
            // Giving Ashen Estus at the start
            // Making warps repeatable, such as the ones after boss fights, or to flameless shrine
            // Unconditional event edits: After the first warp, enable bonfire warping (14000101)
            // Also required Coiled Sword to place Coiled Sword, same as item randomizer
            // Give Wolnir drop after Wolnir ends, just because it is missable (or make warp repeatable)

            // common_funcs display: show sfx, set trigger flag on entering region, press A on fog gate and warp to region
            // direct edit for warps: replace warp target
            Dictionary<int, EventSpec> specs = eventConfig.Events.ToDictionary(e => e.ID, e => e);
            HashSet<EventTemplate> usedEdits = new HashSet<EventTemplate>();
            SortedDictionary<int, FogEdit> fogEdits = new SortedDictionary<int, FogEdit>();
            HashSet<string> usedWarps = new HashSet<string>();

            FogEdit getFogEdit(int fog)
            {
                if (!fogEdits.TryGetValue(fog, out FogEdit edit))
                {
                    edit = fogEdits[fog] = new FogEdit();
                }
                return edit;
            }

            foreach (KeyValuePair<string, EMEVD> entry in emevds)
            {
                if (!validEmevd(entry.Key)) continue;
                EMEVD emevd = entry.Value;
                // string name = mapSpec.Name;

                Dictionary<int, EMEVD.Event> fileEvents = entry.Value.Events.ToDictionary(e => (int)e.ID, e => e);

                // Process on an initialization-by-initialization basis. For now, only support remove all, and remove local (one-time)
                for (int evIndex = 0; evIndex < emevd.Events.Count; evIndex++)
                {
                    EMEVD.Event ev = emevd.Events[evIndex];
                    for (int i = 0; i < ev.Instructions.Count; i++)
                    {
                        Instr init = events.Parse(ev.Instructions[i]);
                        if (!init.Init) continue;
                        if (new[] { 20005750, 20005751, 20005752 }.Contains(init.Callee))
                        {
                            // Can blank out invasions - but they work, so it's fine
                            // ev.Instructions[i] = new EMEVD.Instruction(1014, 69);
                            // continue;
                        }
                        if (!specs.TryGetValue(init.Callee, out EventSpec spec) || spec.Template == null) continue;
                        int getArg(string arg)
                        {
                            if (events.ParseArgSpec(arg, out int pos) && pos < init.Offset + init.Args.Count)
                            {
                                arg = init[init.Offset + pos].ToString();
                            }
                            if (int.TryParse(arg, out int val))
                            {
                                return val;
                            }
                            else throw new Exception($"Internal error: Invalid arg spec {arg}");
                        }
                        foreach (EventTemplate t in spec.Template.OrderBy(t => t.CopyTo > 0 ? 0 : 1))
                        {
                            bool removeEvent = false;
                            int warpId = -1;
                            Side warpDest = null;
                            if (t.Fog != null)
                            {
                                int fog = getArg(t.Fog);
                                if (!nonfixed.Contains(fog) && !opt["alwaysshow"]) continue;
                                FogEdit fogEdit = getFogEdit(fog);
                                // Add auxiliary data
                                if (t.Sfx != null) fogEdit.Sfx = getArg(t.Sfx);
                                if (t.SetFlag != null)
                                {
                                    if (t.SetFlagIf == null || t.SetFlagArea == null) throw new Exception($"{init.Callee} has missing values [{t.SetFlag}], [{t.SetFlagIf}], [{t.SetFlagArea}]");
                                    int setFlag = getArg(t.SetFlag);
                                    FlagEdit flagEdit = fogEdit.FlagEdits.Find(f => f.SetFlag == setFlag);
                                    if (flagEdit == null)
                                    {
                                        flagEdit = new FlagEdit { SetFlag = setFlag };
                                        fogEdit.FlagEdits.Add(flagEdit);
                                    }
                                    flagEdit.SetFlagIf = getArg(t.SetFlagIf);
                                    flagEdit.SetFlagArea = getArg(t.SetFlagArea);
                                }
                                if (t.Remove == "event")
                                {
                                    removeEvent = true;
                                }
                            }
                            else if (t.FogSfx != null)
                            {
                                int fog = getArg(t.FogSfx);
                                if (!nonfixed.Contains(fog)) continue;
                                FogEdit fogEdit = getFogEdit(fog);
                                fogEdit.CreateSfx = false;
                            }
                            else if (t.Warp != null)
                            {
                                if (!warpDests.TryGetValue(t.Warp, out Side exit)) continue;
                                warpId = int.Parse(t.Warp.Split('_')[1]);
                                if (t.WarpReplace != null)
                                {
                                    warpDest = exit;
                                }
                                if (t.RepeatWarpObject > 0)
                                {
                                    if (t.RepeatWarpFlag <= 0) throw new Exception($"{init.Callee} has warp object {t.RepeatWarpObject} but no flag");
                                    FogEdit fogEdit = getFogEdit(warpId);
                                    fogEdit.RepeatWarpObject = t.RepeatWarpObject;
                                    fogEdit.RepeatWarpFlag = t.RepeatWarpFlag;
                                }
                            }
                            if (removeEvent)
                            {
                                ev.Instructions[i] = new EMEVD.Instruction(1014, 69);
                            }
                            else
                            {
                                // Edit to event command itself. Only done once per template.
                                if (usedEdits.Contains(t)) continue;
                                usedEdits.Add(t);
                                if (!fileEvents.TryGetValue(init.Callee, out EMEVD.Event ev2)) throw new Exception($"Attempted edit to event not in same file as its instantiation {init}");

                                EventEdits edits = new EventEdits();
                                if (t.Remove != null && t.Remove != "event")
                                {
                                    foreach (string edit in phraseRe.Split(t.Remove))
                                    {
                                        events.RemoveMacro(edits, edit);
                                    }
                                }
                                if (t.Replace != null)
                                {
                                    foreach (string edit in phraseRe.Split(t.Replace))
                                    {
                                        events.ReplaceMacro(edits, edit);
                                    }
                                }
                                if (t.Add != null)
                                {
                                    events.AddMacro(edits, t.Add);
                                }
                                if (warpId > 0 && warpDest != null)
                                {
                                    (byte area, byte block) = getDest(warpDest.Warp.Map);
                                    int toPlayer = getPlayer(warpDest.Warp);
                                    events.RemoveMacro(edits, t.WarpReplace);
                                    if (ceremonyAlias.TryGetValue(warpDest.Warp.Map, out Ceremony ceremony))
                                    {
                                        events.AddMacro(edits, t.WarpReplace, false, $"Set Map Ceremony ({area},{block},{ceremony.ID})");
                                    }
                                    events.AddMacro(edits, t.WarpReplace, false, $"Warp Player ({area},{block},{toPlayer})");
                                    usedWarps.Add(t.Warp);
                                }

                                if (t.CopyTo > 0)
                                {
                                    ev2 = events.CopyEvent(ev2, t.CopyTo);
                                    emevd.Events.Add(ev2);
                                    Instr newInit = events.CopyInit(init, ev2);
                                    newInit.Save();
                                    emevd.Events[0].Instructions.Add(newInit.Val);
                                }

                                OldParams pre = OldParams.Preprocess(ev2);
                                for (int j = 0; j < ev2.Instructions.Count; j++)
                                {
                                    Instr instr = events.Parse(ev2.Instructions[j]);
                                    if (instr.Init) continue;
                                    edits.ApplyEdits(instr, j);
                                    instr.Save();
                                    ev2.Instructions[j] = instr.Val;
                                }
                                events.ApplyAdds(edits, ev2);
                                pre.Postprocess();

                                if (edits.PendingEdits.Count != 0)
                                {
                                    throw new Exception($"{ev2.ID} has unapplied edits: {string.Join("; ", edits.PendingEdits)}");
                                }
                            }
                        }
                    }
                }
            }

#if DEBUG
            // For previewing emevd after enemy rando, which writes malformed stuff, unfortunately
            emevds["common_func"].Events.RemoveAll(ev => ev.ID < 200000);
#endif

            // Add common functions
            Dictionary<string, NewEvent> customEvents = new Dictionary<string, NewEvent>();
            foreach (NewEvent e in eventConfig.NewEvents)
            {
                List<EMEVD.Parameter> ps = new List<EMEVD.Parameter>();
                EMEVD.Event ev = new EMEVD.Event(e.ID, EMEVD.Event.RestBehaviorType.Default);
                for (int i = 0; i < e.Commands.Count; i++)
                {
                    (EMEVD.Instruction instr, List<EMEVD.Parameter> newPs) = events.ParseAddArg(e.Commands[i], i);
                    ev.Instructions.Add(instr);
                    ev.Parameters.AddRange(newPs);
                }
                if (e.Name == null)
                {
                    emevds["common"].Events.Add(ev);
                    emevds["common"].Events[0].Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { 0, (int)ev.ID, 0 }));
                }
                else
                {
                    emevds["common_func"].Events.Add(ev);
                    customEvents[e.Name] = e;
                }
            }

            List<int> colFlags = new List<int>();
            // Unused flag range from kiln. There are a bit over 30 of these
            // TODO: Dump into config instead, per row id
            int colBase = 15105610;
            foreach (PARAM.Row row in Params["PlayRegionParam"].Rows)
            {
                int flag = (int)row["EventFlagId1"].Value;
                if (flag > 0)
                {
                    int index = colFlags.IndexOf(flag);
                    if (index == -1)
                    {
                        colFlags.Add(flag);
                        index = colFlags.Count - 1;
                    }
                    row["EventFlagId1"].Value = (int)(colBase + index);
                    // Console.WriteLine($"{index}. {flag} -> {colBase + index}");
                    // Regions are formatted like 301000, e.g. for Lothric Castle
                    int region = (int)row.ID;
                    int area = region / 10000;
                    int block = (region / 1000) % 10;
                    string mapName = $"m{area:d2}_{block:d2}_00_00";
                    if (!emevds.TryGetValue(mapName, out EMEVD emevd) || emevd == null)
                    {
                        // Console.WriteLine($"Cannot find emevd for {region} -> {mapName} with {area}, {block} - {(region / 100)}");
                        continue;
                    }
                    NewEvent custom = customEvents["makestable"];
                    emevd.Events[0].Instructions.Add(new EMEVD.Instruction(2000, 6, new List<object> { custom.ID, colBase + index, flag }));
                }
            }
            // Making Gundyr arena stable in either ceremony is seriously messed up somehow. Just make it stable :/
            MSB3.Part.Collision gundyrCol = msbs["firelink"].Parts.Collisions.Find(e => e.Name == "h002500");
            gundyrCol.PlayRegionID = 400001;  // Previously 400000

            // Scaling
            // Create scaling speffects in all runs
            // Add a bunch of made-up entity ids just to apply speffects... but maybe in groups
            // This is based on difference between start of run 7000 and end of run 7170.
            // For each type, the max value allowed
            Dictionary<string, (float, float)> scalingMult = new Dictionary<string, (float, float)>
            {
                // HP is 3x max scaling. A bit bigger than the NG range, which is 2x
                ["health"] = (1, ann.HealthScaling),
                // Defense is a bit weirder. Its range is 0 to 1, but it maxes out at 1 by catacombs. See if it matters at all.
                // ["defense"] = 1,
                // Status recovery things also scales a small amount
                ["status"] = (1, 1.3f * ann.HealthScaling / 2),
                // Souls dropped scales with health
                ["souls"] = (1, ann.HealthScaling),
                // Damage is 2x max scaling. A bit smaller than the NG range, 2.5x
                ["damage"] = (1, ann.DamageScaling),
            };
            Dictionary<string, List<string>> scalingFields = new Dictionary<string, List<string>>
            {
                // Note: maxHpRate not part of normal scaling speffects
                ["health"] = new List<string> { "maxHpRate", "maxStaminaCutRate" },
                ["defense"] = new List<string> { "physDefRate", "magicDefRate", "fireDefRate", "thunderDefRate", "darkDefRate", "staminaAttackRate" },
                ["status"] = new List<string> { "staminaAttackRate", "registPoisonChangeRate", "registToxicChangeRate", "registBloodChangeRate", "registCurseChangeRate", "registFrostChangeRate" },
                ["souls"] = new List<string> { "haveSoulRate" },
                ["damage"] = new List<string> { "physAtkPowerRate", "magicAtkPowerRate", "fireAtkPowerRate", "thunderAtkPowerRate", "darkAttackPowerRate" },
            };
            // ratio is from 1/max to max, and defines enemy scaling amounts
            // range is from 0 to 1, and defines speffect index
            float ratioToRange(float ratio, float maxRatio)
            {
                // Change from [1/max,max] to [-1,1] but with log steps
                float range = (float)Math.Log(ratio, maxRatio);
                // Change from [-1,1] to [0,1]
                range = (range + 1) / 2;
                // Clamp to [0,1]
                range = range > 1 ? 1 : (range < 0 ? 0 : range);
                return range;
            }
            float rangeToRatio(float range, float maxRatio)
            {
                // Clamp to [0,1]
                range = range > 1 ? 1 : (range < 0 ? 0 : range);
                // Change from [0,1] to [-1,1]
                range = range * 2 - 1;
                // Change from [-1,1] to [1/max,max] but with log steps
                range = (float)Math.Pow(maxRatio, range);
                return range;
            }
            int spCount = 40;
            int healthSpBase = 7900;
            int damageSpBase = 7950;
            PARAM.Row baseSp = Params["SpEffectParam"][7000];
            for (int spType = 0; spType <= 1; spType++)
            {
                bool health = spType == 0;
                List<string> cats = health ? new List<string> { "health", "status", "souls" } : new List<string> { "damage" };
                int spBase = health ? healthSpBase : damageSpBase;
                float maxRatio = health ? ann.HealthScaling : ann.DamageScaling;
                for (int i = 0; i <= spCount; i++)
                {
                    PARAM.Row newSp = new PARAM.Row(spBase + i, null, Params["SpEffectParam"].AppliedParamdef);
                    GameEditor.CopyRow(baseSp, newSp);
                    Params["SpEffectParam"].Rows.Add(newSp);
                    // Also, slight weird behavior, make defense behave in a custom way matching the base game more closely. In the first part of the game
                    float defMult = health ? Math.Min(i / (spCount * 0.3f), 1f) : 1f;
                    if (opt["dumpscale"]) Console.WriteLine($"{i} defense: {defMult}");
                    foreach (string field in scalingFields["defense"])
                    {
                        newSp[field].Value = Math.Min(defMult, 1f);
                    }
                    foreach (string cat in cats)
                    {
                        (float min, float max) = scalingMult[cat];
                        List<string> fields = scalingFields[cat];
                        float range = (float)i / spCount;
                        float ratio = rangeToRatio(range, maxRatio);
                        float mult = (max / maxRatio) * ratio;
                        if (opt["dumpscale"]) Console.WriteLine($"{i} {cat}: {mult}");
                        foreach (string field in fields)
                        {
                            newSp[field].Value = mult;
                        }
                    }
                }
            }
            Dictionary<(string, string), string> enemyAreas = new Dictionary<(string, string), string>();
            foreach (EnemyLoc eloc in ann.Locations.Enemies)
            {
                if (eloc.Area == null) throw new Exception(eloc.ID);
                string[] spec = eloc.ID.Split(' ');
                enemyAreas[(spec[0], spec[1])] = eloc.Area;
                enemyAreas[(spec[0], spec[2])] = eloc.Area;
            }
            if (opt["scale"])
            {
                foreach (KeyValuePair<string, MSB3> entry in msbs)
                {
                    string name = entry.Key;
                    MSB3 msb = entry.Value;
                    EMEVD.Event constr = emevds[mapFromName(name)].Events[0];
                    foreach (MSB3.Part.Enemy e in msb.Parts.Enemies)
                    {
                        if (!enemyAreas.TryGetValue((name, e.Name), out string area))
                        {
                            if (e.CollisionName == null || !enemyAreas.TryGetValue((name, e.CollisionName), out area)) continue;
                        }
                        int entityId = e.EntityID;
                        if (entityId <= 0)
                        {
                            // Hope this works
                            entityId = e.EntityID = mk++;
                        }
                        if (g.AreaRatios.TryGetValue(area, out (float, float) val))
                        {
                            (float ratio, float dmgRatio) = val;
                            float healthRange = ratioToRange(ratio, 3);
                            float damageRange = ratioToRange(dmgRatio, 2);
                            int getSp(float range)
                            {
                                int spVal = (int)Math.Round(range * spCount);
                                spVal = spVal < 0 ? 0 : (spVal > spCount ? spCount : spVal);
                                return spVal;
                            }
                            constr.Instructions.Add(new EMEVD.Instruction(2000, 6, new List<object> {
                            customEvents["scale"].ID, entityId, healthSpBase + getSp(healthRange), damageSpBase + getSp(healthRange)
                        }));
                        }
                    }
                }
            }

            // Open doors on fog gates to avoid phasing through fog gates
            // Should probably have this in config
            EMEVD.Event commonFlags = emevds["common"].Events[0];
            if (!g.EntranceIds["catacombs_3801800"].IsFixed) commonFlags.Instructions.Add(events.ParseAdd("Set Event Flag (63800561,1)"));
            if (!g.EntranceIds["cathedral_3501850"].IsFixed) commonFlags.Instructions.Add(events.ParseAdd("Set Event Flag (63500202,1)"));
            if (!g.EntranceIds["settlement_3101780"].IsFixed) commonFlags.Instructions.Add(events.ParseAdd("Set Event Flag (63100400,1)"));

            // Experiments with Firelink Shrine warping. Unfortunately, this is pretty inflexible
            // commonFlags.Instructions.Add(events.ParseAdd("Set Event Flag (14000101,1)"));
            //Params["BonfireWarpParam"][0]["IsDisableQuickwarp"].Value = (byte)0;
            // Params["BonfireWarpParam"][1]["IsDisableQuickwarp"].Value = (byte)0;
            // Params["BonfireWarpParam"].Rows.Sort((a, b) => a.ID.CompareTo(b.ID));

            HashSet<string> firelinkBonfires = new HashSet<string> { "t400000", "t400001", "t400002" };
            HashSet<string> untendedBonfires = new HashSet<string> { "t400003", "t400004" };
            foreach (KeyValuePair<string, Dictionary<string, ESD>> entry in esds)
            {
                foreach (KeyValuePair<string, ESD> esdEntry in entry.Value)
                {
                    string esdName = esdEntry.Key;
                    ESD esd = esdEntry.Value;
                    if (!(esdName.StartsWith("t") && int.TryParse(esdName.Substring(4), out int esdSuffix) && esdSuffix < 10)) continue;
                    // Scan through the entire ESD and replace flag 
                    void rewriteCondition(ESD.Condition cond, Func<byte[], byte[]> rewriteExpr)
                    {
                        cond.Evaluator = rewriteExpr(cond.Evaluator);
                        cond.PassCommands.ForEach(c => rewriteCommand(c, rewriteExpr));
                        cond.Subconditions.ForEach(c => rewriteCondition(c, rewriteExpr));
                    }
                    void rewriteCommand(ESD.CommandCall cmd, Func<byte[], byte[]> rewriteExpr)
                    {
                        cmd.Arguments = cmd.Arguments.Select(rewriteExpr).ToList();
                    }
                    byte[] findReplace(byte[] b, byte[] find, byte[] replace, int search)
                    {
                        if (search == -1) return b;
                        byte[] newB = new byte[b.Length - find.Length + replace.Length];
                        // From (source, index) to (dest, index)
                        Array.Copy(b, 0, newB, 0, search);
                        Array.Copy(replace, 0, newB, search, replace.Length);
                        Array.Copy(b, search + find.Length, newB, search + replace.Length, b.Length - (search + find.Length));
                        // Console.WriteLine(string.Join(" ", newB.Select(p => $"{p:x2}")));
                        return newB;
                    }
                    byte[] firelinkCond = new byte[]
                    {
                            0x4f, // GetEventStatus
                            0x82, 0xe5, 0x9f, 0xd5, 0x00, // 14000101
                            0x85, // 1 arg call
                            0x41, // 1
                            0x95, // ==
                    };
                    byte[] notFirelinkCond = firelinkCond.Concat(new byte[] { 0xa1 }).ToArray();
                    notFirelinkCond[notFirelinkCond.Length - 2] = 0x96;  // !=
                    byte[] trueCond = new byte[] { 0x41 };
                    // Override to make nestling always available
                    // notFirelinkCond = new byte[] { 0x41, 0xa1 };
                    // 15000150 Travel
                    byte[] travelId = new byte[] { 0x82, 0x56, 0xe2, 0xe4, 0x00, 0xa1 };
                    // 13000000 High Wall bonfire flag - little endian 405dc600
                    byte[] highwallFlag = new byte[] { 0x82, 0x40, 0x5d, 0xc6, 0x00, 0xa1 };
                    foreach (KeyValuePair<long, Dictionary<long, ESD.State>> machine in esd.StateGroups)
                    {
                        bool hasWarpMenu = false;
                        ESD.State condState = null;
                        foreach (KeyValuePair<long, ESD.State> stateEntry in machine.Value)
                        {
                            ESD.State state = stateEntry.Value;
                            bool hasWarpTalk = false;
                            ESD.CommandCall respawn = null;
                            foreach (ESD.CommandCall cmd in state.EntryCommands)
                            {
                                // AddTalkListDataIf
                                if (cmd.CommandBank == 5 && cmd.CommandID == 19)
                                {
                                    if (SearchBytes(cmd.Arguments[0], notFirelinkCond) != -1) break;  // If already added alternate cond, nothing to do here
                                    int search = SearchBytes(cmd.Arguments[0], firelinkCond);
                                    if (search != -1)
                                    {
                                        if (!opt["instawarp"] && cmd.Arguments[2].SequenceEqual(travelId))
                                        {
                                            hasWarpTalk = hasWarpMenu = true;
                                        }
                                        else
                                        {
                                            cmd.Arguments[0] = findReplace(cmd.Arguments[0], firelinkCond, trueCond, search);
                                        }
                                    }
                                }
                                // ShowShopMessage
                                else if (hasWarpMenu && cmd.CommandBank == 1 && cmd.CommandID == 10)
                                {
                                    condState = state;
                                }
                                // UpdatePlayerRespawnPoint
                                else if (cmd.CommandBank == 1 && cmd.CommandID == 101)
                                {
                                    respawn = cmd;
                                }
                                else if (cmd.CommandBank == 1 && cmd.CommandID == 11)
                                {
                                    if (g.EntranceIds["firelink_3000980"].HasTag("unused") && cmd.Arguments[0].SequenceEqual(highwallFlag))
                                    {
                                        // Turn off Highwall bonfire instead of on, if not part of routing
                                        cmd.Arguments[1] = new byte[] { 0x40, 0xa1 };
                                    }
                                }
                            }
                            if (respawn != null)
                            {
                                if (firelinkBonfires.Contains(esdName))
                                {
                                    // Clear Firelink event flag 239 = ef000000 little endian
                                    state.EntryCommands.Add(new ESD.CommandCall(1, 11, new byte[] { 0x82, 0xef, 0x00, 0x00, 0x00, 0xa1 }, new byte[] { 0x40, 0xa1 }));
                                }
                                else if (untendedBonfires.Contains(esdName))
                                {
                                    state.EntryCommands.Add(new ESD.CommandCall(1, 11, new byte[] { 0x82, 0xef, 0x00, 0x00, 0x00, 0xa1 }, new byte[] { 0x41, 0xa1 }));
                                }
                            }
                            if (hasWarpTalk)
                            {
                                // 10010404 Leave through the white light = 24bf9800 little endian
                                // 10010504 Nestle in coffin - 88bf9800 little endian
                                // 15000150 Travel - travelId
                                // 10010712 This bonfire is cut off from the others. Cannot warp. - 58c09800 little endian
                                state.EntryCommands.Insert(0, new ESD.CommandCall(5, 19,
                                    notFirelinkCond,
                                    new byte[] { 0x48, 0xa1 },
                                    travelId,
                                    new byte[] { 0x3f, 0xa1 }));
                            }
                        }
                        byte[] intArg(int arg)
                        {
                            if (arg >= -64 && arg < 63)
                            {
                                return new byte[] { (byte)(0x40 + arg), 0xa1 };
                            }
                            byte[] b = new byte[] { 0x82, 0x00, 0x00, 0x00, 0x00, 0xa1 };
                            byte[] i = BitConverter.GetBytes(arg);
                            Array.Copy(i, 0, b, 1, 4);
                            return b;
                        }
                        // Finally, add the condition state
                        if (condState != null)
                        {
                            // Start from last state, actually doing the warp
                            machine.Value[50] = new ESD.State();
                            // Set warp event flag 14005159 = a7b3d500 little endian
                            machine.Value[50].EntryCommands.Add(new ESD.CommandCall(1, 11, intArg(14005159), intArg(1)));

                            // Failsafe, wait 5 seconds and restart
                            machine.Value[50].Conditions.Add(new ESD.Condition(1, new byte[] {
                                0x82, 0x67, 0x00, 0x00, 0x00, // 103
                                0x84, // call
                                0x45, // 5
                                0x92, // >=
                                0xa1, // end
                            }));

                            // Opening dialogue
                            // CheckSpecificPersonGenericDialogIsOpen(0) - func 58, id 0x7a
                            // GetGenericDialogButtonResult() == 1 - func 22, id 0x56
                            // 10010712 This bonfire is cut off from the others. Cannot warp. - 58c09800 little endian
                            // OpenGenericDialog(8, action1, 3, 4, 2) - id 17
                            machine.Value[51] = new ESD.State();
                            machine.Value[51].EntryCommands.Add(new ESD.CommandCall(1, 17, intArg(8), intArg(10010712), intArg(1), intArg(2), intArg(2)));
                            machine.Value[51].Conditions.Add(new ESD.Condition(50, new byte[] {
                                0x7a, 0x40, 0x85, // call 58(0)
                                0x40, 0x95, // == false
                                0x56, 0x84, // call 22
                                0x41, 0x95, // == true
                                0x98, // &&
                                0xa1, // end
                            }));
                            machine.Value[51].Conditions.Add(new ESD.Condition(1, new byte[] {
                                0x7a, 0x40, 0x85, // call 58(0)
                                0x40, 0x95, // == false
                                0xa1, // end
                            }));

                            condState.Conditions.Add(new ESD.Condition(51, new byte[] { 0xaf, 0x48, 0x95, 0xa1 }));
                        }
                    }
                }
            }

            // Fog gates

            // Just abuse these I guess
            int toAsideOld = 3301800;  // default; angle 0
            int toAsideNew = 3301870;
            List<int> actionHeights = new List<int> { -5, -1, 1, 3 };
            int getActionForHeight(float height)
            {
                List<int> heightIndices = actionHeights.Select((h, i) => i).Where(i => actionHeights[i] < height).ToList();
                int index = heightIndices.Count == 0 ? 1 : heightIndices.Max();
                return toAsideNew + index;
            }
            for (int i = 0; i < actionHeights.Count; i++)
            {
                PARAM.Row baseSide = Params["ActionButtonParam"][toAsideOld];
                int side = toAsideNew + i;
                PARAM.Row act = new PARAM.Row(side, null, Params["ActionButtonParam"].AppliedParamdef);
                GameEditor.CopyRow(baseSide, act);
                act["InvalidFlag"].Value = -1;
                act["height"].Value = 4f;
                act["baseHeightOffset"].Value = (float)actionHeights[i];
                act["AllowAngle"].Value = 90;
                act["depth"].Value = 1.5f;
                act["width"].Value = 1.5f;
                Params["ActionButtonParam"].Rows.Add(act);
            }
            Params["ActionButtonParam"].Rows.Sort((a, b) => a.ID.CompareTo(b.ID));

            // Misc fix for Wolnir repeatable, while mucking with ActionButtonParam
            // Just do this always, since it won't be possible if Wolnir defeat flag on anyway
            Params["ActionButtonParam"][9322]["InvalidFlag"].Value = -1;
            // Archdragon fog gate
            Params["ActionButtonParam"][3201850]["InvalidFlag"].Value = -1;

            PARAM.Row coiledPlace = Params["ActionButtonParam"][9351];
            if (opt["instawarp"])
            {
                // Disable behavior from item randomizer, if present
                coiledPlace["grayoutFlag"].Value = -1;
            }
            else if ((int)coiledPlace["grayoutFlag"].Value <= 0)
            {
                // Copy behavior from item randomizer
                coiledPlace["grayoutFlag"].Value = 14005108;

                EMEVD.Event swordEvent = new EMEVD.Event(14005107, EMEVD.Event.RestBehaviorType.Restart);
                swordEvent.Instructions.AddRange(new string[]
                {
                    "Set Event Flag (14005108,1)",
                    "IF Player Has/Doesn't Have Item (0,3,2137,1)",
                    "Set Event Flag (14005108,0)",
                }
                .Select(t => events.ParseAdd(t)));
                EMEVD emevd = emevds[mapFromName("firelink")];
                emevd.Events.Add(swordEvent);
                emevd.Events[0].Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { 0, (uint)14005107, (uint)0 }));
            }    

            // Also make Hollow Manservant a lot harder to miss
            PARAM.Row hollow = Params["ActionButtonParam"][3100000];
            hollow["regionType"].Value = (byte)0;
            hollow["Radius"].Value = 5f;
            hollow["Angle"].Value = 180;
            hollow["depth"].Value = 0f;
            hollow["width"].Value = 0f;
            hollow["height"].Value = 15f;
            hollow["baseHeightOffset"].Value = -10f;
            hollow["dummyPoly1"].Value = -1;
            hollow["angleCheckType"].Value = (byte)0;
            hollow["AllowAngle"].Value = 180;

            Dictionary<int, int> startFlagCeremonies = new Dictionary<int, int>();
            foreach (Area area in g.Areas.Values)
            {
                if (area.BossTrigger > 0)
                {
                    string name = area.Name.Split('_')[0];
                    if (ceremonyAlias.TryGetValue(name, out Ceremony ceremony))
                    {
                        startFlagCeremonies[area.BossTrigger] = ceremony.ID;
                    }
                }
            }
            foreach (KeyValuePair<int, FogEdit> entry in fogEdits)
            {
                int id = entry.Key;
                FogEdit fogEdit = entry.Value;
                Entrance ent = ann.Entrances.Find(e => e.ID == id);
                if (ent == null)
                {
                    // Okay to not exist if warp
                    if (ann.Warps.Any(e => e.ID == id)) continue;
                    throw new Exception($"Unknown fog edit {id}");
                }
                EMEVD emevd = emevds[mapFromName(ent.Area)];
                if (fogEdit.CreateSfx)
                {
                    NewEvent custom = customEvents["showsfx"];
                    int sfx = fogEdit.Sfx;
                    // TODO: Use fogSfx map
                    if (sfx == 0) sfx = 3;
                    emevd.Events[0].Instructions.Add(new EMEVD.Instruction(2000, 6, new List<object> { custom.ID, id, sfx }));
                    // Console.WriteLine($"{id} create SFX: {fogEdit.Sfx}");
                }
                if (fogEdit.FlagEdits.Count > 0)
                {
                    NewEvent custom = customEvents["startboss"];
                    foreach (FlagEdit flagEdit in fogEdit.FlagEdits)
                    {
                        int ceremony = startFlagCeremonies.TryGetValue(flagEdit.SetFlag, out int c) ? c : -1;
                        emevd.Events[0].Instructions.Add(new EMEVD.Instruction(2000, 6, new List<object> { custom.ID, flagEdit.SetFlagIf, flagEdit.SetFlagArea, flagEdit.SetFlag, ceremony }));
                    }
                    // Console.WriteLine($"{id} set flag: {fogEdit.SetFlag} if {fogEdit.SetFlagIf}");
                }
            }
            foreach (KeyValuePair<string, List<(string, int)>> entry in bossTriggerRegions)
            {
                Area area = g.Areas[entry.Key];
                int defeatFlag = area.DefeatFlag;
                int triggerFlag = area.BossTrigger;
                if (defeatFlag == 0 || triggerFlag == 0) throw new Exception($"Internal error: no flag for {entry.Key}");
                NewEvent custom = customEvents["startboss"];
                foreach ((string, int) region in entry.Value)
                {
                    EMEVD emevd = emevds[mapFromName(region.Item1)];
                    int ceremony = startFlagCeremonies.TryGetValue(triggerFlag, out int c) ? c : -1;
                    emevd.Events[0].Instructions.Add(new EMEVD.Instruction(2000, 6, new List<object> { custom.ID, defeatFlag, region.Item2, triggerFlag, ceremony }));
                }
            }

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
                        // In this case, preserve the vanilla behavior of the entrance.
                        // However, everything is preserved by default, so there is nothing to do.
                        vanillaEntrances.Add(e.FullName);
                        continue;
                    }

                    FogEdit repeatWarp = null;
                    if (a.Action == 0)
                    {
                        if (!usedWarps.Contains(exit.Name)) throw new Exception($"Did not add warp {entrance.Name} in events pass (found {string.Join(", ", usedWarps)})");
                        fogEdits.TryGetValue(g.EntranceIds[exit.Name].ID, out repeatWarp);
                        if (repeatWarp == null || repeatWarp.RepeatWarpObject == 0) continue;
                    }

                    int player = getPlayer(b);
                    string fromMap = a.Map;
                    string toMap = b.Map;
                    EMEVD emevd = emevds[mapFromName(fromMap)];
                    int exitFlag = getNameFlag(exit.Side.BossDefeatName, exit.Side.Area, ar => ar.DefeatFlag);
                    int trapFlag = getNameFlag(exit.Side.BossTrapName, exit.Side.Area, ar => ar.TrapFlag);
                    if (opt["pacifist"])
                    {
                        exitFlag = 0;
                        trapFlag = 0;
                    }

                    NewEvent custom = customEvents["fogwarp"];
                    (byte area, byte block) = getDest(toMap);
                    int fromCeremony = ceremonyAlias.TryGetValue(fromMap, out Ceremony ceremony) ? ceremony.ID : -1;
                    int toCeremony = ceremonyAlias.TryGetValue(toMap, out ceremony) ? ceremony.ID : -1;
                    List<object> args;
                    if (repeatWarp == null)
                    {
                        float height = exit.Side.AdjustHeight + g.EntranceIds[exit.Name].AdjustHeight;
                        int actionParam = exit.Name == "archdragon_3201850" ? 3201850 : getActionForHeight(height);
                        // Console.WriteLine($"for {exit} -> {entrance}: height {height}, id {actionParam}");
                        args = new List<object> { custom.ID, a.Action, actionParam, player, area, block, toCeremony, exitFlag, trapFlag, fromCeremony };
                    }
                    else
                    {
                        args = new List<object> { custom.ID, repeatWarp.RepeatWarpObject, 9340, player, area, block, toCeremony, repeatWarp.RepeatWarpFlag, 0, fromCeremony };
                    }
                    // We used to use ceremony.EventLayer to initialize this in a ceremony, but with Oceiros->Untended
                    // transition this could result in the wrong event running on initialization.
                    emevd.Events[0].Instructions.Add(new EMEVD.Instruction(2000, 6, args));
                }
            }
            // Enable Archdragon cols which are normally only for Nameless King
            if (!g.EntranceIds["archdragon_3201850"].IsFixed)
            {
                foreach (MSB3.Part.Collision col in msbs["archdragon"].Parts.Collisions)
                {
                    if (new[] { "h005002", "h000600", "h000610" }.Contains(col.Name))
                    {
                        col.MapStudioLayer = uint.MaxValue;
                    }
                }
            }

            MSB3.Part.Enemy lesserWyvern = msbs["archdragon"].Parts.Enemies.Find(e => e.EntityID == 3200300);
            // Copy from the bois below
            lesserWyvern.MapStudioLayer = 4294967295;

            // Give an entity id to the bell for warping
            MSB3.Part.Object dummyBell = msbs["archdragon"].Parts.Objects.Find(e => e.Name == "o324010_1000");
            dummyBell.EntityID = 3201458;

            MSB3.Part.MapPiece dummyThrone = msbs["firelink"].Parts.MapPieces.Find(e => e.Name == "m000400_1000");
            dummyThrone.EntityID = 4001405;

            // Make enemy activation area a bit higher, since it's used for Ancient Wyvern
            MSB3.Region.ActivationArea ladderArea = msbs["archdragon"].Regions.ActivationAreas.Find(e => e.EntityID == 3202270);
            ((MSB.Shape.Box)ladderArea.Shape).Height = 4 + 4;

            // Move starting point to be less annoying
            MSB3.Part.Player coffin = msbs["firelink"].Parts.Players.Find(e => e.EntityID == 4000110);
            coffin.Position = new Vector3(112.369f, -49.240f, 498.478f);
            coffin.Rotation = new Vector3(0, 180 + 20.280f, 0);

            // Make existing Dancer region a bit larger (10 longer, 5 shifted)
            MSB3.Region.Event triggerRegion = msbs["highwall"].Regions.Events.Find(e => e.EntityID == 3002896);
            ((MSB.Shape.Box)triggerRegion.Shape).Depth = 26.400f + 10;
            triggerRegion.Position = new Vector3(27.580f, -9.920f, 143.990f + 5);

            // Make Crystal Sage a bit smaller, to avoid clipping into path to Cathedral
            triggerRegion = msbs["farronkeep"].Regions.Events.Find(e => e.EntityID == 3302850);
            ((MSB.Shape.Box)triggerRegion.Shape).Depth = 36.100f - 10;
            triggerRegion.Position = new Vector3(-178.135f, -248.860f, -423.790f);

            // Make catacombs ladder fall from below. Previously firebomb
            emevds[mapFromName("catacombs")].Events[0].Instructions.Add(new EMEVD.Instruction(2000, 6, new List<object> { customEvents["ladderfall"].ID, 3802352, 3801403 }));

            // Emma teleport is not part of logic, but make it more consistent within that, always to the same position
            MSB3.Region.Event outsideEmma = msbs["highwall"].Regions.Events.Find(e => e.EntityID == 3002890);
            MSB3.Region.Event dancerMpWarp = msbs["highwall"].Regions.Events.Find(e => e.EntityID == 3002893);
            outsideEmma.Position = dancerMpWarp.Position;
            outsideEmma.Rotation = dancerMpWarp.Rotation;

            // Move object which overlaps with region
            MSB3.Part.Object spearObj = msbs["archdragon"].Parts.Objects.Find(e => e.EntityID == 3201480);
            spearObj.Position = new Vector3(-15.413f, 69.113f, 197.680f);

            if (opt["cheat"])
            {
                for (int i = 0; i < 10; i++)
                {
                    PARAM.Row row = Params["CharaInitParam"][3000 + i];
                    foreach (string stat in new List<string> { "Vit", /* vigor */ "Wil", "End", "Str", "Dex", "Mag", "Fai", "Luc", "Durability" /* vit */ })
                    {
                        row[$"base{stat}"].Value = (sbyte)90;
                    }
                    PARAM reinforce = Params["ReinforceParamWeapon"];
                    HashSet<int> reinforceLevels = new HashSet<int>(reinforce.Rows.Select(r => (int)r.ID));
                    foreach (string wep in new List<string> { "equip_Wep_Right", "equip_Subwep_Right", "equip_Wep_Left", "equip_Subwep_Left" })
                    {
                        int id = (int)row[wep].Value;
                        if (id != -1)
                        {
                            id = id - (id % 100);
                            PARAM.Row item = Params["EquipParamWeapon"][id];
                            if (item == null) continue;
                            int reinforceId = (short)item["reinforceTypeId"].Value;
                            while (reinforceLevels.Contains(reinforceId + 5))
                            {
                                reinforceId += 5;
                                id += 5;
                            }
                            if (item.ID == 16000000) id += 200;  // Sharp
                            row[wep].Value = id;
                        }
                    }
                }
            }

            // Also edit message in English to be slightly more suitable I guess.
            // This is for TK :fatcat:
            // 10010712 This bonfire is cut off from the others. Cannot warp.
            menuFMGs["イベントテキスト"][10010712] = "Until the Coiled Sword is placed, this bonfire is cut off\nfrom the others. Return to Cemetery?";

            if (opt["dryrun"])
            {
                Console.WriteLine("Success (dry run)");
                return;
            }

            List<string> outPaths = new List<string>();
            {
                string basePath = $@"fogdist\Base\Data0.bdt";
                string altPath = $@"{gameDir}\Data0.bdt";
                if (File.Exists(altPath))
                {
                    basePath = altPath;
                }
                bool encrypted = true;
                string path = encrypted ? $@"{outDir}\Data0.bdt" : $@"{outDir}\param\gameparam\gameparam.parambnd.dcx";
                AddModFile(outPaths, path);
                editor.OverrideBndRel(basePath, path, Params, f => f.Write());
            }

            {
                string basePath = $@"fogdist\Base\menu_dlc2.msgbnd.dcx";
                string altPath = $@"{gameDir}\msg\engus\menu_dlc2.msgbnd.dcx";
                if (File.Exists(altPath))
                {
                    basePath = altPath;
                }
                string path = $@"{outDir}\msg\engus\menu_dlc2.msgbnd.dcx";
                AddModFile(outPaths, path);
                editor.OverrideBndRel(basePath, path, menuFMGs, f => f.Write());
            }

            foreach (KeyValuePair<string, EMEVD> entry in emevds)
            {
                if (!validEmevd(entry.Key)) continue;
                string path = $@"{outDir}\event\{entry.Key}.emevd.dcx";
                AddModFile(outPaths, path);
                entry.Value.Write(path);
            }
            foreach (KeyValuePair<string, MSB3> entry in msbs)
            {
                string map = ann.NameSpecs[entry.Key].Map;
                string path = $@"{outDir}\map\mapstudio\{map}.msb.dcx";
                AddModFile(outPaths, path);
                entry.Value.Write(path);
            }
            foreach (KeyValuePair<string, Dictionary<string, ESD>> entry in esds)
            {
                string basePath = $@"fogdist\Base\{entry.Key}.talkesdbnd.dcx";
                string altPath = $@"{gameDir}\script\talk\{entry.Key}.talkesdbnd.dcx";
                if (File.Exists(altPath))
                {
                    basePath = altPath;
                }
                string path = $@"{outDir}\script\talk\{entry.Key}.talkesdbnd.dcx";
                AddModFile(outPaths, path);
                editor.OverrideBndRel(basePath, path, entry.Value, e => e.Write());
            }

            MergeMods(outPaths, gameDir, outDir);
        }

        private static readonly Regex phraseRe = new Regex(@"\s*;\s*");

        public class Ceremony
        {
            public string Map { get; set; }
            public int ID { get; set; }
            public int MapLayer { get; set; }
            public int EventLayer { get; set; }
        }

        // SFX to use if none is specified, e.g. because the fog gate is normally unused
        // Right now not used? Harder to actually find the model id when it's needed
        private static readonly Dictionary<int, int> fogSfx = new Dictionary<int, int>
        {
            { 400, 2 },
            { 401, 3 },
            { 402, 3 },
        };

        public class FogEdit
        {
            // Recreating sfx, if the original event doing it is removed.
            public bool CreateSfx = true;
            public int Sfx { get; set; }
            // Boss flag triggers to preserve. There are two of these for the same gate for firelink/untended.
            public List<FlagEdit> FlagEdits = new List<FlagEdit>();
            // Object id to repeat a one-time warp.
            public int RepeatWarpObject { get; set; }
            // Required flag for completing warp
            public int RepeatWarpFlag { get; set; }
        }
        public class FlagEdit
        {
            public int SetFlag { get; set; }
            public int SetFlagIf { get; set; }
            public int SetFlagArea { get; set; }
        }

        // Lifted directly from DS3/Sekiro Item Randomizer
        private static readonly List<string> fileLangs = new List<string>
        {
            "deude", "engus", "frafr", "itait", "jpnjp", "korkr", "polpl", "porbr", "rusru", "spaar", "spaes", "thath", "zhocn", "zhotw",
        };
        private static readonly List<string> fileDirs = new List<string>
        {
            @".",
            @"action",
            @"action\script",
            @"chr",
            @"cutscene",
            @"event",
            @"map\mapstudio",
            @"menu",
            @"menu\hi",
            @"menu\hi\mapimage",
            @"menu\low",
            @"menu\low\mapimage",
            @"menu\knowledge",
            @"menu\$lang",
            @"msg\$lang",
            @"mtd",
            @"obj",
            @"other",
            @"param\drawparam",
            @"param\gameparam",
            @"param\graphicsconfig",
            @"parts",
            @"script",
            @"script\talk",
            @"sfx",
            @"shader",
            @"sound",
        }.SelectMany(t => t.Contains("$lang") ? fileLangs.Select(l => t.Replace("$lang", l)) : new[] { t }).ToList();
        private static List<string> extensions = new List<string>
        {
            ".hks", ".dcx", ".gfx", ".dds", ".fsb", ".fev", ".itl", ".tpf", ".entryfilelist", ".hkxbdt", ".hkxbhd", "Data0.bdt"
        };
        private static Regex extensionRe = new Regex(string.Join("|", extensions.Select(e => e + "$")));
        private static List<string> GetGameFiles(string dir)
        {
            List<string> allFiles = new List<string>();
            foreach (string subdir in fileDirs)
            {
                string fulldir = $@"{dir}\{subdir}";
                if (Directory.Exists(fulldir))
                {
                    foreach (string path in Directory.GetFiles(fulldir))
                    {
                        if (extensionRe.IsMatch(path))
                        {
                            string filename = Path.GetFileName(path);
                            allFiles.Add($@"{subdir}\{filename}");
                        }
                    }
                }
            }
            return allFiles;
        }

        private static string FullName(string path)
        {
            return new FileInfo(path).FullName;
        }

        private void AddModFile(List<string> writtenFiles, string path)
        {
            path = FullName(path);
            Console.WriteLine($"Writing {path}");
            writtenFiles.Add(path);
        }

        private void MergeMods(List<string> writtenFiles, string modDir, string outPath)
        {
            Console.WriteLine("Processing extra mod files...");
            bool work = false;
            if (modDir != null)
            {
                foreach (string gameFile in GetGameFiles(modDir))
                {
                    string source = FullName($@"{modDir}\{gameFile}");
                    string target = FullName($@"{outPath}\{gameFile}");
                    if (writtenFiles.Contains(target)) continue;
                    Console.WriteLine($"Copying {source}");
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(source, target, true);
                    writtenFiles.Add(target);
                    work = true;
                }
            }
            foreach (string gameFile in GetGameFiles(outPath))
            {
                string target = FullName($@"{outPath}\{gameFile}");
                if (writtenFiles.Contains(target)) continue;
                Console.WriteLine($"Found extra file (delete it if you don't want it): {target}");
                work = true;
            }
            if (!work) Console.WriteLine("No extra files found");
        }
    }
}
