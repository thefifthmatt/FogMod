using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SoulsIds.GameSpec;

namespace FogMod
{
    public partial class MainForm3 : Form
    {
        private static string defaultDir = @"C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game";
        private static string defaultPath = @"C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game\randomizer\Data0.bdt";
        private RandomizerOptions options = new RandomizerOptions { Game = FromGame.DS3 };

        public MainForm3()
        {
            InitializeComponent();
            errorL.Text = "";
            string defaultExe = Properties.Settings.Default.Exe;
            if (!string.IsNullOrWhiteSpace(defaultExe) && defaultExe.ToLowerInvariant().EndsWith("data0.bdt") && File.Exists(defaultExe))
            {
                exe.Text = defaultExe;
            }
            else if (File.Exists(defaultPath))
            {
                exe.Text = defaultPath;
            }
            options["dryrun"] = false;
            string defaultOpts = Properties.Settings.Default.Options;
            if (string.IsNullOrWhiteSpace(defaultOpts))
            {
                ReadControlFlags(this);
            }
            else
            {
                // TODO: Set seed from this
                List<string> optSet = defaultOpts.Split(' ').ToList();
                SetControlFlags(this, optSet);
                foreach (string opt in optSet)
                {
                    if (uint.TryParse(opt, out uint seed))
                    {
                        if (seed != 0) fixedseed.Text = seed.ToString();
                        break;
                    }
                }
            }
        }

        private void UpdateExePath()
        {
            if (exe.Text.Trim() == "")
            {
                // This is fine
            }
            else
            {
                bool valid = true;
                string gamePath = null;
                try
                {
                    gamePath = Path.GetDirectoryName(exe.Text);
                    if (!Directory.Exists(gamePath))
                    {
                        valid = false;
                    }
                    string exeName = Path.GetFileName(exe.Text).ToLowerInvariant();
                    if (exeName != "data0.bdt")
                    {
                        valid = false;
                    }
                }
                catch (ArgumentException)
                {
                    valid = false;
                }
                if (!valid)
                {
                    return;
                }
            }
            Properties.Settings.Default.Exe = exe.Text;
            Properties.Settings.Default.Save();
        }

        private void OpenExe(object sender, EventArgs e)
        {
            OpenFileDialog exeDialog = new OpenFileDialog();
            exeDialog.Title = "Select Data0.bdt of other mod";
            exeDialog.Filter = "Modded params|Data0.bdt|All files|*.*";
            exeDialog.RestoreDirectory = true;
            try
            {
                if (Directory.Exists(exe.Text))
                {
                    exeDialog.InitialDirectory = exe.Text;
                }
                else
                {
                    string gamePath = Path.GetDirectoryName(exe.Text);
                    if (Directory.Exists(gamePath))
                    {
                        exeDialog.InitialDirectory = gamePath;
                    }
                    else if (Directory.Exists(defaultDir))
                    {
                        exeDialog.InitialDirectory = defaultDir;
                    }
                }
            }
            catch (ArgumentException) { }
            if (exeDialog.ShowDialog() == DialogResult.OK)
            {
                exe.Text = exeDialog.FileName;
            }
        }

        private void setStatus(string msg, bool error = false, bool success = false)
        {
            statusL.Text = msg;
            statusStrip1.BackColor = error ? Color.IndianRed : (success ? Color.PaleGreen : SystemColors.Control);
        }

        // Horrible hack, winforms sucks
        private bool working = false;
        private async void Randomize(object sender, EventArgs e)
        {
            if (working) return;
            ReadControlFlags(this);
            // TODO: Allow this to be empty
            string gameDir = null;
            if (!string.IsNullOrWhiteSpace(exe.Text))
            {
                gameDir = Path.GetDirectoryName(exe.Text);
                if (!File.Exists($@"{gameDir}\Data0.bdt"))
                {
                    SetError("Error: Data0.bdt not found for the mod to merge. Leave it blank to use Fog Gate Randomizer by itself.");
                    return;
                }
                if (new DirectoryInfo(gameDir).FullName == Directory.GetCurrentDirectory())
                {
                    SetError("Error: Data0.bdt is not from a different mod! Leave it blank to use Fog Gate Randomizer by itself.");
                    return;
                }
            }
            if (fixedseed.Text.Trim() != "")
            {
                if (uint.TryParse(fixedseed.Text.Trim(), out uint seed))
                {
                    options.Seed = (int)seed;
                }
                else
                {
                    SetError("Invalid fixed seed");
                    return;
                }
            }
            else
            {
                options.Seed = new Random().Next();
            }
            fixedseed.Text = options.Seed.ToString();
            UpdateOptions(null, null);  // Can split up maybe
            SetError();

            working = true;
            string prevText = randb.Text;
            randb.Text = $"Randomizing...";
            setStatus("Randomizing...");
            RandomizerOptions rand = options.Copy();
            randb.BackColor = Color.LightYellow;
            Randomizer randomizer = new Randomizer();
            await Task.Factory.StartNew(() => {
                Directory.CreateDirectory("spoiler_logs");
                string runId = $@"spoiler_logs\{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}_log_{rand.Seed}_{rand.ConfigHash()}.txt";
                TextWriter log = File.CreateText(runId);
                TextWriter stdout = Console.Out;
                Console.SetOut(log);
                try
                {
                    ItemReader.Result itemInfo = randomizer.Randomize(rand, FromGame.DS3, gameDir, Directory.GetCurrentDirectory());
                    setStatus($"Done. Info in {runId} | Restart your game!" + (itemInfo.Randomized ? $" | Key item hash: {itemInfo.ItemHash}" : ""), success : true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    SetError($"Error encountered: {ex.Message}\r\n\r\nIt may work to try again with a different seed. {(gameDir == null ? "" : "The merged mod might also not be compatible. ")}See most recent file in spoiler_logs directory for the full error.");
                    setStatus($"Error! See error message in {runId}", true);
                }
                finally
                {
                    log.Close();
                    Console.SetOut(stdout);
                }
            });
            randb.Enabled = true;
            randb.Text = prevText;
            randb.BackColor = SystemColors.Control;
            working = false;
            UpdateExePath();
        }

        private void SetError(string text = null)
        {
            errorL.Text = text ?? "";
        }

        private void UpdateFile(object sender, EventArgs e)
        {
            UpdateExePath();
        }

        private void UpdateOptions(object sender, EventArgs e)
        {
            ReadControlFlags(this);
            Properties.Settings.Default.Options = string.Join(" ", options.GetEnabled()) + " " + options.DisplaySeed;
            Properties.Settings.Default.Save();
        }

        private void ReadControlFlags(Control control)
        {
            if (control is RadioButton radio)
            {
                options[control.Name] = radio.Checked;
            }
            else if (control is CheckBox check)
            {
                options[control.Name] = check.Checked;
            }
            else
            {
                foreach (Control sub in control.Controls)
                {
                    ReadControlFlags(sub);
                }
            }
        }

        private void SetControlFlags(Control control, ICollection<string> set)
        {
            // Temp hack: options are diabled if they're not yet implemented, and shouldn't be populated from existing options
            if (!control.Enabled) return;

            if (control is RadioButton radio)
            {
                options[control.Name] = radio.Checked = set.Contains(control.Name);
            }
            else if (control is CheckBox check)
            {
                options[control.Name] = check.Checked = set.Contains(control.Name);
            }
            else
            {
                foreach (Control sub in control.Controls)
                {
                    SetControlFlags(sub, set);
                }
            }
        }

        private void fixedseed_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fixedseed.Text))
            {
                randb.Text = "Randomize!";
            }
            else
            {
                randb.Text = "Run with fixed seed";
            }
        }
    }
}
