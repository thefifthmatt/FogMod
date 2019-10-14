using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SoulsFormats;

namespace FogMod
{
    public partial class MainForm : Form
    {
        private static string defaultPath = @"C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS REMASTERED\DarkSoulsRemastered.exe";
        private RandomizerOptions options = new RandomizerOptions();

        public MainForm()
        {
            InitializeComponent();
            string defaultExe = Properties.Settings.Default.Exe;
            if (!string.IsNullOrWhiteSpace(defaultExe))
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
                SetControlFlags(this, defaultOpts.Split(' '));
            }
        }

        private void UpdateBackupFiles()
        {
            bool valid = true;
            string gamePath = null;
            try
            {
                gamePath = Path.GetDirectoryName(exe.Text);
                if (exe.Text.Trim() == "" || !Directory.Exists(gamePath) || Path.GetFileName(exe.Text).ToLower() != "darksoulsremastered.exe")
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
                restoreButton.Enabled = false;
                restoreL.Text = "";
                return;
            }
            Properties.Settings.Default.Exe = exe.Text;
            Properties.Settings.Default.Save();
            List<string> files = GameDataWriter.GetAllBaseFiles();
            if (files.Count == 0)
            {
                randb.Enabled = false;
                statusL.Text = @"Error: FogMod dist subdirectory is missing";
            }
            List<string> times = new List<string>();
            foreach (string file in files)
            {
                string path = $@"{gamePath}\{file}";
                string bak = path + ".bak";
                if (File.Exists(bak))
                {
                    times.Add(File.GetLastWriteTime(bak).ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }
            if (times.Count == 0)
            {
                restoreL.Text = "Backups will be created with randomization";
                restoreButton.Enabled = false;
            }
            else
            {
                restoreL.Text = $"{(files.Count == times.Count ? "Backups" : "Partial backups")} from {times.Max()}";
                restoreButton.Enabled = true;
            }
        }

        private void OpenExe(object sender, EventArgs e)
        {
            OpenFileDialog exeDialog = new OpenFileDialog();
            exeDialog.Title = "Select Dark Souls Remastered install location";
            exeDialog.Filter = "DS1R exe|DarkSoulsRemastered.exe";
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
                }
            }
            catch (ArgumentException) { }
            if (exeDialog.ShowDialog() == DialogResult.OK)
            {
                exe.Text = exeDialog.FileName;
            }
        }

        // Horrible hack, winforms sucks
        private bool working = false;
        private async void Randomize(object sender, EventArgs e)
        {
            if (working) return;
            ReadControlFlags(this);
            RandomizerOptions rand = options.Copy();
            if (fixedseed.Text.Trim() != "")
            {
                if (uint.TryParse(fixedseed.Text.Trim(), out uint seed))
                {
                    rand.Seed = (int)seed;
                }
                else
                {
                    statusL.Text = "Invalid fixed seed";
                    return;
                }
            }
            else
            {
                rand.Seed = new Random().Next();
            }
            if (!File.Exists(exe.Text))
            {
                statusL.Text = "Game exe not set";
                return;
            }
            string gameDir = Path.GetDirectoryName(exe.Text);
            if (Path.GetFileName(exe.Text).ToLower() != "darksoulsremastered.exe"
                || !File.Exists($@"{gameDir}\event\common.emevd.dcx")
                || !File.Exists($@"{gameDir}\map\MapStudio\m10_02_00_00.msb"))
            {
                statusL.Text = "Did not find DSR installation at game path";
                return;
            }
            working = true;
            randomizeL.Text = $"Seed: {rand.Seed}";
            randb.Text = $"Randomizing...";
            statusL.Text = "Randomizing...";
            Color original = randb.BackColor;
            randb.BackColor = Color.LightYellow;
            Randomizer randomizer = new Randomizer();
            await Task.Factory.StartNew(() => {
                Directory.CreateDirectory("runs");
                string runId = $@"runs\{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}_log_{rand.Seed}_{rand.ConfigHash()}.txt";
                TextWriter log = File.CreateText(runId);
                TextWriter stdout = Console.Out;
                Console.SetOut(log);
                try
                {
                    randomizer.Randomize(rand, gameDir);
                    statusL.Text = $"Done. Info in {runId}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    statusL.Text = $"Error! See error message in {runId}";
                }
                finally
                {
                    log.Close();
                    Console.SetOut(stdout);
                }
            });
            randb.Enabled = true;
            randb.Text = $"Randomize!";
            randb.BackColor = original;
            working = false;
            UpdateBackupFiles();
        }

        private void UpdateFile(object sender, EventArgs e)
        {
            UpdateBackupFiles();
        }

        private void UpdateOptions(object sender, EventArgs e)
        {
            ReadControlFlags(this);
            Properties.Settings.Default.Options = string.Join(" ", options.GetEnabled());
            Properties.Settings.Default.Save();
        }

        private void Restore(object sender, EventArgs e)
        {
            if (working) return;
            string gamePath = Path.GetDirectoryName(exe.Text);
            if (exe.Text.Trim() == "" || !Directory.Exists(gamePath))
            {
                return;
            }
            List<string> files = GameDataWriter.GetAllBaseFiles();
            List<string> desc = new List<string>();
            foreach (string file in files)
            {
                string path = $@"{gamePath}\{file}";
                string bak = path + ".bak";
                if (File.Exists(bak)) desc.Add(path);
            }
            DialogResult dialogResult = MessageBox.Show(string.Join("\n", desc) + "\n\nTo completely ensure restoration of vanilla files, also use Properties -> Local Files -> Verify Integrity Of Game Files in Steam.", "Restore these files?", MessageBoxButtons.YesNo);
            if (dialogResult != DialogResult.Yes) return;
            foreach (string file in files)
            {
                string path = $@"{gamePath}\{file}";
                string bak = path + ".bak";
                if (File.Exists(bak))
                {
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(bak, path);
                }
            }
            UpdateBackupFiles();
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

    }
}
