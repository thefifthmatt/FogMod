using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SoulsIds;
using YamlDotNet.Serialization;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public class Randomizer
    {
        public ItemReader.Result Randomize(RandomizerOptions opt, FromGame game, string gameDir, string outDir)
        {
            Console.WriteLine($"Seed: {opt.DisplaySeed}. Options: {string.Join(" ", opt.GetEnabled())}");

            string file = game == FromGame.DS3 ? @"fogdist\fog.txt" : @"dist\fog.txt";

            IDeserializer deserializer = new DeserializerBuilder().Build();
            AnnotationData ann;
            using (var f = File.OpenText(file)) ann = deserializer.Deserialize<AnnotationData>(f);
            ann.SetGame(game);

            Events events = null;

            if (game == FromGame.DS3)
            {
                using (var f = File.OpenText(@"fogdist\locations.txt")) ann.Locations = deserializer.Deserialize<AnnotationData.FogLocations>(f);
                events = new Events(@"fogdist\Base\ds3-common.emedf.json");
            }

            Graph g = new Graph();
            g.Construct(opt, ann);

            ItemReader.Result item = new ItemReader().FindItems(opt, ann, g, events, gameDir, game);
            Console.WriteLine(item.Randomized ? $"Key item hash: {item.ItemHash}" : "No key items randomized");
            Console.WriteLine();

            new GraphConnector().Connect(opt, g, ann);

            // Actual dryruns stop short of writing modified files
            if (opt["bonedryrun"]) return item;

            Console.WriteLine();
            if (game == FromGame.DS3)
            {
                EventConfig eventConfig;
                using (var f = File.OpenText(@"fogdist\events.txt")) eventConfig = deserializer.Deserialize<EventConfig>(f);

                if (opt["eventsyaml"] || opt["events"])
                {
                    new GenerateConfig().WriteEventConfig(ann, events, opt);
                    return item;
                }
                new GameDataWriter3().Write(opt, ann, g, gameDir, outDir, events, eventConfig);
            }
            else
            {
                if (opt["dryrun"])
                {
                    Console.WriteLine("Success (dry run)");
                    return item;
                }
                new GameDataWriter().Write(opt, ann, g, gameDir, game);
            }
            return item;
        }
    }
}
