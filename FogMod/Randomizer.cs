using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using static FogMod.AnnotationData;
using SoulsIds;
using YamlDotNet.Serialization;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public class Randomizer
    {
        public ItemReader.Result Randomize(RandomizerOptions opt, FromGame game, string gameDir)
        {
            Console.WriteLine($"Seed: {opt.DisplaySeed}. Options: {string.Join(" ", opt.GetEnabled())}");
            gameDir = gameDir ?? ForGame(game).GameDir;

            IDeserializer deserializer = new DeserializerBuilder().Build();
            Annotations ann;
            using (var f = File.OpenText("dist/fog.txt")) ann = deserializer.Deserialize<Annotations>(f);
            Graph g = new Graph();
            g.Construct(opt, ann);
            ItemReader.Result item = new ItemReader().FindItems(opt, ann, g, gameDir, game);
            Console.WriteLine(item.Randomized ? $"Key item hash: {item.ItemHash}" : "No key items randomized");
            Console.WriteLine();
            new GraphConnector().Connect(opt, g, ann);
            if (opt["dryrun"])
            {
                Console.WriteLine("Success (dry run)");
                return item;
            }
            Console.WriteLine();
            new GameDataWriter().Write(opt, ann, g, gameDir, game);
            return item;
        }
    }
}
