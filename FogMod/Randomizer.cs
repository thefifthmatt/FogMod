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
        public void Randomize(RandomizerOptions opt, FromGame game, string gameDir)
        {
            Console.WriteLine($"Seed: {opt.DisplaySeed}. Options: {string.Join(" ", opt.GetEnabled())}");
            Random random = new Random(opt.Seed);

            IDeserializer deserializer = new DeserializerBuilder().Build();
            Annotations ann;
            using (var f = File.OpenText("dist/fog.txt")) ann = deserializer.Deserialize<Annotations>(f);
            Graph g = new Graph();
            g.Construct(opt, ann);
            new GraphConnector().Connect(opt, g, ann, random);
            if (opt["dryrun"])
            {
                Console.WriteLine("Success (dry run)");
                return;
            }
            new GameDataWriter().Write(opt, ann, g, gameDir, game);
        }
    }
}
