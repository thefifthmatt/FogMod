using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsIds;
using SoulsFormats;
using YamlDotNet.Serialization;
using static SoulsIds.Events;
using static SoulsIds.GameSpec;
using static FogMod.AnnotationData;
using static FogMod.EventConfig;
using static FogMod.Util;

namespace FogMod
{
    public class GenerateConfig
    {
        public void FogConfig()
        {
            AnnotationData ret = new AnnotationData();
            ret.SetGame(FromGame.DS3);
            GameEditor editor = new GameEditor(FromGame.DS3);
            Dictionary<string, MSB3> maps = editor.Load(@"map\MapStudio", path => ret.Specs.ContainsKey(GameEditor.BaseName(path)) ? MSB3.Read(path) : null, "*.msb.dcx");
            ret.Entrances = new List<Entrance>();
            foreach (KeyValuePair<string, MSB3> entry in maps)
            {
                if (!ret.Specs.TryGetValue(entry.Key, out MapSpec spec)) continue;
                // Console.WriteLine(spec.Name);
                MSB3 msb = entry.Value;
                Dictionary<string, MSB3.Part.Object> objs = new Dictionary<string, MSB3.Part.Object>();
                foreach (MSB3.Part.Object e in msb.Parts.Objects)
                {
                    objs[e.Name] = e;
                    int model = int.Parse(e.ModelName.Substring(1));
                    if (model >= spec.Start && model <= spec.End)
                    {
                        ret.Entrances.Add(new Entrance
                        {
                            Area = spec.Name,
                            Name = e.Name,
                            ID = e.EntityID,
                            Text = "Between",
                            Tags = "pvp boss",
                            ASide = new Side { Area = spec.Name },
                            BSide = new Side { Area = spec.Name },
                        });
                    }
                }
            }
            ISerializer serializer = new SerializerBuilder().DisableAliases().Build();
            using (TextWriter writer = File.CreateText("fog.txt"))
            {
                serializer.Serialize(writer, ret);
            }
        }

        private static string CharacterName(SortedDictionary<int, string> characterSplits, int id)
        {
            int chType = 0;
            foreach (KeyValuePair<int, string> entry in characterSplits)
            {
                if (entry.Key > id)
                {
                    break;
                }
                chType = entry.Key;
            }
            string name = characterSplits[chType];
            return name == "UNUSED" ? $"Human NPC {id}" : name;
        }

        public void WriteEventConfig(AnnotationData ann, Events events, RandomizerOptions opt)
        {
            GameEditor editor = new GameEditor(FromGame.DS3);

            editor.Spec.GameDir = "fogdist";
            Dictionary<string, MSB3> maps = editor.Load(@"Base", path => ann.Specs.ContainsKey(GameEditor.BaseName(path)) ? MSB3.Read(path) : null, "*.msb.dcx");
            Dictionary<string, EMEVD> emevds = editor.Load(@"Base", path => ann.Specs.ContainsKey(GameEditor.BaseName(path)) || path.Contains("common") ? EMEVD.Read(path) : null, "*.emevd.dcx");
            void deleteEmpty<K,V>(Dictionary<K,V> d)
            {
                foreach (K key in d.Keys.ToList())
                {
                    if (d[key] == null) d.Remove(key);
                }
            }
            // Should this be in GameEditor?
            deleteEmpty(maps);
            deleteEmpty(emevds);

            editor.Spec.NameDir = @"fogdist\Names";
            Dictionary<string, string> modelNames = editor.LoadNames("ModelName", n => n);
            SortedDictionary<int, string> chars = new SortedDictionary<int, string>(editor.LoadNames("CharaInitParam", n => int.Parse(n)));

            Dictionary<string, List<string>> description = new Dictionary<string, List<string>>();
            Dictionary<int, string> entityNames = new Dictionary<int, string>();
            Dictionary<int, List<int>> groupIds = new Dictionary<int, List<int>>();
            Dictionary<(string, string), MSB3.Event.ObjAct> objacts = new Dictionary<(string, string), MSB3.Event.ObjAct>();

            HashSet<int> highlightIds = new HashSet<int>();
            HashSet<int> selectIds = new HashSet<int>();

            foreach (Entrance e in ann.Warps.Concat(ann.Entrances))
            {
                int id = e.ID;
                AddMulti(description, id.ToString(), (ann.Warps.Contains(e) ? "" : "fog gate ") + e.Text);
                selectIds.Add(e.ID);
                highlightIds.Add(e.ID);

            }
            HashSet<string> gameObjs = new HashSet<string>();
            foreach (GameObject obj in ann.Objects)
            {
                if (int.TryParse(obj.ID, out int id))
                {
                    AddMulti(description, id.ToString(), obj.Text);
                    selectIds.Add(id);
                    highlightIds.Add(id);
                }
                else
                {
                    gameObjs.Add($"{obj.Area}_{obj.ID}");
                }
            }

            Dictionary<string, Dictionary<string, FMG>> fmgs = new GameEditor(FromGame.DS3).LoadBnds($@"msg\engus", (data, name) => FMG.Read(data), ext: "*_dlc2.msgbnd.dcx");
            void addFMG(FMG fmg, string desc)
            {
                foreach (FMG.Entry e in fmg.Entries)
                {
                    if (e.ID > 25000 && !string.IsNullOrWhiteSpace(e.Text))
                    {
                        highlightIds.Add(e.ID);
                        AddMulti(description, e.ID.ToString(), desc + " " + "\"" + e.Text.Replace("\r", "").Replace("\n", "\\n") + "\"");
                    }
                }
            }
            addFMG(fmgs["item_dlc2"]["NPC名"], "name");
            addFMG(fmgs["menu_dlc2"]["イベントテキスト"], "text");

            foreach (KeyValuePair<string, MSB3> entry in maps)
            {
                string map = ann.Specs[entry.Key].Name;
                MSB3 msb = entry.Value;

                foreach (MSB3.Part e in msb.Parts.GetEntries())
                {
                    string shortName = $"{map}_{e.Name}";
                    if (modelNames.TryGetValue(e.ModelName, out string modelDesc))
                    {
                        if (e is MSB3.Part.Enemy en && modelDesc == "Human NPC" && en.CharaInitID > 0)
                        {
                            modelDesc = CharacterName(chars, en.CharaInitID);
                        }
                        else if (e is MSB3.Part.Player)
                        {
                            modelDesc = "Warp Point";
                        }
                        AddMulti(description, shortName, modelDesc);
                    }
                    AddMulti(description, shortName, $"{map} {e.GetType().Name.ToString().ToLowerInvariant()} {e.Name}");  // {(e.EntityID > 0 ? $" {e.EntityID}" : "")}
                    if (e.EntityID > 10)
                    {
                        highlightIds.Add(e.EntityID);
                        string idStr = e.EntityID.ToString();
                        if (description.ContainsKey(idStr))
                        {
                            AddMulti(description, shortName, description[idStr]);
                        }
                        description[idStr] = description[shortName];
                        if (e is MSB3.Part.Player || e.ModelName == "o000100")
                        {
                            selectIds.Add(e.EntityID);
                        }
                        if (selectIds.Contains(e.EntityID))
                        {
                            gameObjs.Add(shortName);
                        }

                        foreach (int id in e.EntityGroups)
                        {
                            if (id > 0)
                            {
                                AddMulti(groupIds, id, e.EntityID);
                                highlightIds.Add(id);
                            }
                        }
                    }
                }
                foreach (MSB3.Region r in msb.Regions.GetEntries())
                {
                    if (r.EntityID < 1000000) continue;
                    AddMulti(description, r.EntityID.ToString(), $"{map} {r.GetType().Name.ToLowerInvariant()} region {r.Name}");
                    highlightIds.Add(r.EntityID);
                }
                foreach (MSB3.Event e in msb.Events.GetEntries())
                {
                    if (e is MSB3.Event.ObjAct oa)
                    {
                        // It can be null, basically for commented out objacts
                        string part = oa.PartName ?? oa.ObjActPartName;
                        if (part == null) continue;
                        string desc = description.TryGetValue($"{map}_{part}", out List<string> p) ? string.Join(" - ", p) : throw new Exception($"{map} {oa.Name}");
                        objacts[(map, part)] = oa;
                        Dictionary<string, int> things = new Dictionary<string, int>
                        {
                            { "ObjAct", oa.EntityID },
                            { "ObjAct param", oa.ObjActParamID },
                            { "ObjAct entity", oa.ObjActEntityID },
                            { "ObjAct event flag", oa.EventFlagID },
                        };
                        foreach (KeyValuePair<string, int> thing in things)
                        {
                            int id = thing.Value;
                            if (id > 1000)
                            {
                                highlightIds.Add(id);
                                AddMulti(description, id.ToString(), $"{map} {thing.Key} {oa.Name} for [{desc}]");
                                if (gameObjs.Contains($"{map}_{part}"))
                                {
                                    selectIds.Add(id);
                                }
                            }
                        }
                        if (e.EntityID > 0)
                        {
                            highlightIds.Add(e.EntityID);
                        }
                        if (oa.ObjActParamID > 0)
                        {
                            highlightIds.Add(oa.ObjActParamID);
                        }
                    }
                    else
                    {
                        if (e.EntityID > 0)
                        {
                            AddMulti(description, e.EntityID.ToString(), $"{map} {e.Name}");
                            highlightIds.Add(e.EntityID);
                        }
                    }
                }
            }
            // Just for documenting High Wall warp
            selectIds.Add(13000000);
            highlightIds.Add(13000000);

            // Some validation
            foreach (Entrance e in ann.Warps.Concat(ann.Entrances))
            {
                if (!highlightIds.Contains(e.ID)) throw new Exception($"Unknown id {e.ID}");
            }
            foreach (GameObject obj in ann.Objects)
            {
                if (int.TryParse(obj.ID, out int id))
                {
                    if (!highlightIds.Contains(id)) throw new Exception($"Unknown id {id}");
                }
                else
                {
                    // Maybe check it exists, doesn't matter really though?
                }
            }

            // bonfire: o000100
            HashSet<int> bossesWithMultiFogGates = new HashSet<int>
            {
                3010800, 3200800, 3200850, 3300850, 3300801, 3700850, 3700800,
                4000800, 4000830, 5000801, 5000802,
                // Also Halflight, but there is no AI enable event there, aside from event 15105703 using flag 15105805, which is managed by the fog gate.
            };
            HashSet<string> highlightInstrs = new HashSet<string>
            {
                "Set Player Respawn Point", "Warp Player", "Play Cutscene, Change Map Ceremony and Warp Player", "Play Cutscene and Warp Player", "Play Ongoing Cutscene and Warp Player",
                "Play Cutscene and Warp Player + UNKNOWN 2002[12]"
            };
            SortedDictionary<int, EventDebug> eventInfos = events.GetHighlightedEvents(emevds, highlightIds, instr =>
            {
                // Ad hoc checks for events that may need modifications
                if (highlightInstrs.Contains(instr.Name)) return true;
                if (instr.Name == "Set Character AI State" && instr[1].ToString() == "1" && instr[0] is int chrId && bossesWithMultiFogGates.Contains(chrId)) return true;
                return false;
            });
            // TODO: Here can remove events already in the event config

            string quickId(int id)
            {
                if (description.TryGetValue(id.ToString(), out List<string> desc))
                {
                    return $"{id} - {string.Join(" - ", desc)}";
                }
                if (groupIds.ContainsKey(id))
                {
                    return $"{id} group [{string.Join(", ", groupIds[id].Select(i => quickId(i)))}]";
                }
                return $"{id} unknown";
            }
            bool isEligible(int entityId)
            {
                return selectIds.Contains(entityId);
            }
            EventSpec produceSpec()
            {
                return new EventSpec();
            }

            HashSet<int> processEventsOverride = new HashSet<int> { 15115860 };
            HashSet<int> processEntitiesOverride = new HashSet<int> { };

            List<EventSpec> specs = events.CreateEventConfig(eventInfos, isEligible, produceSpec, quickId, processEventsOverride, processEntitiesOverride);

            ISerializer serializer = new SerializerBuilder().DisableAliases().Build();
            if (opt["eventsyaml"])
            {
                using (var writer = File.CreateText("newevents.txt"))
                {
                    serializer.Serialize(writer, specs);
                }
            }
            else
            {
                serializer.Serialize(Console.Out, specs);
            }
        }
    }
}
