using System;
using System.Collections.Generic;
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
        public Result FindItems(RandomizerOptions opt, Annotations ann, Graph g, string gameDir, FromGame game)
        {
            GameEditor editor = new GameEditor(game);
            editor.Spec.GameDir = gameDir;
            Dictionary<string, PARAM.Layout> layouts = editor.LoadLayouts();
            Dictionary<string, PARAM> Params = editor.LoadParams(layouts);

            Dictionary<int, PARAM.Row> lots = Params["ItemLotParam"].Rows.ToDictionary(r => (int)r.ID, r => r);
            Dictionary<string, string> itemsById = ann.KeyItems.ToDictionary(item => item.ID, item => item.Name);
            Dictionary<string, string> itemAreas = g.itemAreas;
            foreach (KeyValuePair<int, string> entry in ann.LotLocations)
            {
                int lot = entry.Key;
                if (!g.areas.ContainsKey(entry.Value)) throw new Exception($"Internal error in lot config for {entry.Key}: {entry.Value} does not exist");
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
                            if (itemAreas[name] != null && itemAreas[name] != entry.Value) throw new Exception($"Item {name} found in both {itemAreas[name]} and {entry.Value}");
                            itemAreas[name] = entry.Value;
                        }
                    }
                    lot++;
                }
            }
            if (opt["explain"])
            {
                foreach (Item item in ann.KeyItems)
                {
                    Console.WriteLine($"{item.Name} {item.ID}: default {item.Area}, found {itemAreas[item.Name] ?? "?????"}");
                }
            }
            // Collect items in graph
            SortedSet<string> itemRecord = new SortedSet<string>();
            bool randomized = false;
            foreach (Item item in ann.KeyItems)
            {
                if (itemAreas[item.Name] == null)
                {
                    if (item.HasTag("randomonly"))
                    {
                        itemAreas[item.Name] = item.Area;
                    }
                    else if (item.HasTag("hard") && !opt["hard"])
                    {
                        continue;
                    }
                    else throw new Exception($"Couldn't find {item.Name} in item lots");
                }
                string area = itemAreas[item.Name];
                g.graph[area].Items.Add(item.Name);
                if (!item.HasTag("randomonly"))
                {
                    if (area != item.Area) randomized = true;
                    itemRecord.Add($"{item.Name}={area}");
                }
            }
            return new Result
            {
                Randomized = randomized,
                ItemHash = (RandomizerOptions.JavaStringHash($"{string.Join(",", itemRecord)}") % 99999).ToString().PadLeft(5, '0')
            };
        }

        public class Result
        {
            public bool Randomized { get; set; }
            public string ItemHash { get; set; }
        }
    }
}
