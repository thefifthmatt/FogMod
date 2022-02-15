using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public static class Program
    {
        // https://stackoverflow.com/questions/7198639/c-sharp-application-both-gui-and-commandline
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            if (args.Length > 0 && !args.Contains("/gui"))
            {
                AttachConsole(-1);
                RandomizerOptions opt = new RandomizerOptions { Seed = new Random().Next() };
                foreach (string arg in args)
                {
                    if (uint.TryParse(arg, out uint s))
                    {
                        opt.Seed = (int)s;
                    }
                    else
                    {
                        opt[arg] = true;
                    }
                }
                FromGame game = args.Contains("ptde") ? FromGame.DS1 : FromGame.DS1R;
                game = FromGame.DS3;
                opt.Game = game;
                string gameDir = ForGame(game).GameDir;
                if (game == FromGame.DS3)
                {
                    new Randomizer().Randomize(opt, game, opt["mergemods"] ? gameDir + @"\randomizer" : null, gameDir + @"\fog");
                }
                else
                {
                    new Randomizer().Randomize(opt, game, gameDir, gameDir);
                }
            }
            else
            {
#if DEBUG
                AttachConsole(-1);
#endif
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // Application.Run(new MainForm());
                Application.Run(new MainForm3());
            }
        }
    }
}
