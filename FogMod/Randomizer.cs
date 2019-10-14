using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using static FogMod.AnnotationData;
using SoulsIds;
using YamlDotNet.Serialization;

namespace FogMod
{
    public class Randomizer
    {
        public void Randomize(RandomizerOptions opt, string gameDir=null)
        {
            Console.WriteLine($"Seed: {opt.DisplaySeed}. Options: {string.Join(" ", opt.GetEnabled())}");
            Random random = new Random(opt.Seed);

            GameEditor editor = new GameEditor(GameSpec.FromGame.DS1R);
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
            new GameDataWriter().Write(opt, editor, ann, g, gameDir);
        }
    }
}
