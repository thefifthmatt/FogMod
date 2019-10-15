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
            if (args.Length > 0 && !args.Contains("/gui"))
            {
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
                new Randomizer().Randomize(opt, args.Contains("ptde") ? FromGame.DS1 : FromGame.DS1R, null);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
