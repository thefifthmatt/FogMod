using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static FogMod.AnnotationData;
using SoulsIds;
using SoulsFormats;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public class ItemReader
    {
        public Result FindItems(RandomizerOptions opt, AnnotationData ann, Graph g, Events events, string gameDir, FromGame game)
        {
            Dictionary<string, string> itemsById = ann.KeyItems.ToDictionary(item => item.ID, item => item.Name);
            Dictionary<string, List<string>> itemAreas = g.ItemAreas;

            if (ann.LotLocations != null)
            {
                GameEditor editor = new GameEditor(game);
                editor.Spec.GameDir = gameDir;
                Dictionary<string, PARAM.Layout> layouts = editor.LoadLayouts();
                Dictionary<string, PARAM> Params = editor.LoadParams(layouts);

                Dictionary<int, PARAM.Row> lots = Params["ItemLotParam"].Rows.ToDictionary(r => (int)r.ID, r => r);
                foreach (KeyValuePair<int, string> entry in ann.LotLocations)
                {
                    int lot = entry.Key;
                    if (!g.Areas.ContainsKey(entry.Value)) throw new Exception($"Internal error in lot config for {entry.Key}: {entry.Value} does not exist");
                    while (true)
                    {
                        // It's also fine to not have a lot defined as long as all key items are found
                        if (!lots.TryGetValue(lot, out PARAM.Row row)) break;
                        for (int i = 1; i <= 8; i++)
                        {
                            int item = (int)row[$"lotItemId0{i}"].Value;
                            if (item == 0) continue;
                            int category = (int)row[$"lotItemCategory0{i}"].Value;
                            category = Universe.LotTypes.TryGetValue((uint)category, out int value) ? value : -1;
                            if (category == -1) continue;
                            string id = $"{category}:{item}";
                            if (opt["debuglots"]) Console.WriteLine($"{entry.Key} in {entry.Value} has {id}");
                            if (itemsById.TryGetValue(id, out string name))
                            {
                                if (itemAreas[name].Count > 0 && itemAreas[name][0] != entry.Value) throw new Exception($"Item {name} found in both {itemAreas[name][0]} and {entry.Value}");
                                itemAreas[name] = new List<string> { entry.Value };
                            }
                        }
                        lot++;
                    }
                }
            }
            if (ann.Locations != null)
            {
                // It's a bit hacky, but should work from anywhere, probably
                GameEditor editor = new GameEditor(game);
                editor.Spec.GameDir = $@"fogdist";
                editor.Spec.LayoutDir = $@"fogdist\Layouts";
                editor.Spec.NameDir = $@"fogdist\Names";

                Dictionary<string, PARAM.Layout> layouts = editor.LoadLayouts();

                int dragonFlag = -1;
                Dictionary<string, PARAM> Params = editor.LoadParams(@"fogdist\Base\Data0.bdt", layouts, true);
                if (gameDir != null)
                {
                    string paramPath = $@"{gameDir}\Data0.bdt";
                    if (File.Exists(paramPath))
                    {
                        Params = editor.LoadParams(paramPath, layouts, true);
                    }

                    string commonEmevdPath = $@"{gameDir}\event\common.emevd.dcx";
                    if (File.Exists(commonEmevdPath))
                    {
                        EMEVD commonEmevd = EMEVD.Read(commonEmevdPath);
                        EMEVD.Event flagEvent = commonEmevd.Events.Find(e => e.ID == 13000904);
                        if (flagEvent != null)
                        {
                            Events.Instr check = events.Parse(flagEvent.Instructions[1]);
                            dragonFlag = (int)check[3];
                            if (opt["debuglots"]) Console.WriteLine($"Dragon flag: {dragonFlag}");
                        }
                    }
                }

                Dictionary<int, PARAM.Row> lots = Params["ItemLotParam"].Rows.ToDictionary(r => (int)r.ID, r => r);
                Dictionary<int, PARAM.Row> shops = Params["ShopLineupParam"].Rows.ToDictionary(r => (int)r.ID, r => r);

                void setArea(string itemName, List<string> areas)
                {
                    if (opt["debuglots"]) Console.WriteLine($"-- name: {itemName}");
                    if (itemAreas[itemName].Count > 0 && !itemAreas[itemName].SequenceEqual(areas))
                    {
                        throw new Exception($"Item {itemName} found in both {string.Join(",", itemAreas[itemName])} and {string.Join(",", areas)}");
                    }
                    itemAreas[itemName] = areas;
                }

                foreach (KeyItemLoc loc in ann.Locations.Items)
                {
                    List<string> areas = loc.Area.Split(' ').ToList();
                    if (!areas.All(a => g.Areas.ContainsKey(a) || itemAreas.ContainsKey(a)))
                    {
                        // Currently happens with multi-area intersection lots/shops
                        throw new Exception($"Warning: Areas not found for {loc.Area} - {loc.DebugText[0]}");
                    }
                    List<int> lotIds = loc.Lots == null ? new List<int>() : loc.Lots.Split(' ').Select(i => int.Parse(i)).ToList();
                    foreach (int baseLot in lotIds)
                    {
                        int lot = baseLot;
                        while (true)
                        {
                            // It's also fine to not have a lot defined as long as all key items are found
                            if (!lots.TryGetValue(lot, out PARAM.Row row))
                            {
                                break;
                            }
                            for (int i = 1; i <= 8; i++)
                            {
                                int item = (int)row[$"ItemLotId{i}"].Value;
                                if (item == 0) continue;
                                uint category = (uint)row[$"LotItemCategory0{i}"].Value;
                                if (!Universe.LotTypes.TryGetValue(category, out int catVal)) continue;
                                string id = $"{catVal}:{item}";
                                if (opt["debuglots"]) Console.WriteLine($"lot {lot} in {loc.Area} has {id}");
                                if (itemsById.TryGetValue(id, out string name))
                                {
                                    setArea(name, areas);
                                }
                            }
                            if (dragonFlag > 0 && (int)row["getItemFlagId"].Value == dragonFlag)
                            {
                                setArea("pathofthedragon", areas);
                            }
                            lot++;
                        }
                    }
                    List<int> shopIds = loc.Shops == null ? new List<int>() : loc.Shops.Split(' ').Select(i => int.Parse(i)).ToList();
                    foreach (int shopId in shopIds)
                    {
                        // Not as fine for a shop to be missing, but also whatever
                        if (!shops.TryGetValue(shopId, out PARAM.Row row)) continue;
                        int item = (int)row["EquipId"].Value;
                        int catVal = (byte)row["equipType"].Value;
                        string id = $"{catVal}:{item}";
                        if (opt["debuglots"]) Console.WriteLine($"shop {shopId} in {loc.Area} has {id}");
                        if (itemsById.TryGetValue(id, out string name))
                        {
                            setArea(name, areas);
                        }
                        if (dragonFlag > 0 && (int)row["EventFlag"].Value == dragonFlag)
                        {
                            setArea("pathofthedragon", areas);
                        }
                    }
                }
            }
            // lots:.*[1-9]\r
            // Iterative approach for items which depend simply on other items
            // Recursion would look a lot nicer but lazy
            bool itemExpanded;
            do
            {
                itemExpanded = false;
                foreach (KeyValuePair<string, List<string>> entry in itemAreas)
                {
                    foreach (string dep in entry.Value.ToList())
                    {
                        if (itemAreas.TryGetValue(dep, out List<string> deps))
                        {
                            entry.Value.Remove(dep);
                            entry.Value.AddRange(deps);
                            itemExpanded = true;
                        }
                    }
                }
            }
            while (itemExpanded);

            if (opt["explain"] || opt["debuglots"])
            {
                foreach (Item item in ann.KeyItems)
                {
                    Console.WriteLine($"{item.Name} {item.ID}: default {item.Area}, found [{string.Join(", ", itemAreas[item.Name])}]");
                }
            }
            // Collect items in graph
            SortedSet<string> itemRecord = new SortedSet<string>();
            bool randomized = false;
            foreach (Item item in ann.KeyItems)
            {
                if (itemAreas[item.Name].Count == 0)
                {
                    if (item.HasTag("randomonly"))
                    {
                        itemAreas[item.Name] = new List<string> { item.Area };
                    }
                    else if (item.HasTag("hard") && !opt["hard"])
                    {
                        continue;
                    }
                    else throw new Exception($"Couldn't find {item.Name} in item lots");
                }
                List<string> areas = itemAreas[item.Name];
                foreach (string area in areas)
                {
                    g.Nodes[area].Items.Add(item.Name);
                }
                if (!item.HasTag("randomonly"))
                {
                    if (areas.Count > 1 || areas[0] != item.Area) randomized = true;
                    itemRecord.Add($"{item.Name}={string.Join(",", areas)}");
                }
            }
            return new Result
            {
                Randomized = randomized,
                ItemHash = (RandomizerOptions.JavaStringHash($"{string.Join(";", itemRecord)}") % 99999).ToString().PadLeft(5, '0')
            };
        }

        public class Result
        {
            public bool Randomized { get; set; }
            public string ItemHash { get; set; }
        }
    }
}
