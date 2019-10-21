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
    public partial class MainForm : Form
    {
        private static string defaultPath = @"C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS REMASTERED\DarkSoulsRemastered.exe";
        private static string defaultPath2 = @"C:\Program Files (x86)\Steam\steamapps\common\Dark Souls Prepare to Die Edition\DATA\DARKSOULS.exe";
        private RandomizerOptions options = new RandomizerOptions();
        private FromGame game = FromGame.UNKNOWN;
        private string languageToSet = null;
        private static List<string> defaultLang = new List<string> { "ENGLISH" };
        private static List<string> ptdeLang = new List<string> { "ENGLISH", "FRENCH", "GERMAN", "ITALIAN", "JAPANESE", "KOREAN", "POLISH", "RUSSIAN", "SPANISH", "TCHINESE" };
        private static List<string> ds1rLang = new List<string> { "ENGLISH", "FRENCH", "GERMAN", "ITALIAN", "JAPANESE", "KOREAN", "NSPANISH", "POLISH", "PORTUGUESE", "RUSSIAN", "SCHINESE", "SPANISH", "TCHINESE" };

        public MainForm()
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.Language))
            {
                languageToSet = Properties.Settings.Default.Language;
            }
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
            else if (File.Exists(defaultPath2))
            {
                exe.Text = defaultPath2;
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

        private void UpdateExePath()
        {
            bool valid = true;
            string gamePath = null;
            try
            {
                gamePath = Path.GetDirectoryName(exe.Text);
                if (exe.Text.Trim() == "" || !Directory.Exists(gamePath))
                {
                    valid = false;
                }
                string exeName = Path.GetFileName(exe.Text).ToLower();
                if (exeName == "darksoulsremastered.exe")
                {
                    game = FromGame.DS1R;
                }
                else if (exeName == "darksouls.exe")
                {
                    game = FromGame.DS1;
                }
                else
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
                game = FromGame.UNKNOWN;
                restoreButton.Enabled = false;
                restoreL.Text = "";
                language.DataSource = defaultLang;
                language.Enabled = false;
                return;
            }
            Properties.Settings.Default.Exe = exe.Text;
            Properties.Settings.Default.Save();
            List<string> languages = game == FromGame.DS1R ? ds1rLang : ptdeLang;
            language.DataSource = languages;
            language.Enabled = true;
            if (languageToSet != null && languages.Contains(languageToSet))
            {
                language.SelectedIndex = languages.IndexOf(languageToSet);
                languageToSet = null;
            }
            List<string> files = GameDataWriter.GetAllBaseFiles(game);
            if (files.Count == 0)
            {
                randb.Enabled = false;
                setStatus($@"Error: FogMod dist\{game} subdirectory is missing", true);
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
            exeDialog.Title = "Select Dark Souls install location";
            exeDialog.Filter = "Dark Souls exe|DarkSoulsRemastered.exe;DARKSOULS.exe";
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

        private void setStatus(string msg, bool error=false)
        {
            statusL.Text = msg;
            statusStrip1.BackColor = error ? Color.IndianRed : SystemColors.Control;
        }

        // Horrible hack, winforms sucks
        private bool working = false;
        private async void Randomize(object sender, EventArgs e)
        {
            if (working) return;
            ReadControlFlags(this);
            RandomizerOptions rand = options.Copy();
            rand.Language = (string)language.SelectedValue ?? "ENGLISH";
            if (!File.Exists(exe.Text) || game == FromGame.UNKNOWN)
            {
                setStatus("Game exe not set", true);
                return;
            }
            string gameDir = Path.GetDirectoryName(exe.Text);
            if (!File.Exists($@"{gameDir}\map\MapStudio\m10_02_00_00.msb"))
            {
                setStatus("Did not find unpacked installation at game path", true);
                return;
            }
            if (rand["start"] && !rand["boss"] && !rand["world"])
            {
                setStatus("Cannot start outside of Asylum if no Asylum fog gates are randomized", true);
                return;
            }
            if (fixedseed.Text.Trim() != "")
            {
                if (uint.TryParse(fixedseed.Text.Trim(), out uint seed))
                {
                    rand.Seed = (int)seed;
                }
                else
                {
                    setStatus("Invalid fixed seed", true);
                    return;
                }
            }
            else
            {
                rand.Seed = new Random().Next();
            }
            working = true;
            randomizeL.Text = $"Seed: {rand.Seed}";
            randb.Text = $"Randomizing...";
            setStatus("Randomizing...");
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
                    ItemReader.Result itemInfo = randomizer.Randomize(rand, game, gameDir);
                    setStatus($"Done. Info in {runId}" + (itemInfo.Randomized ? $" | Key item hash: {itemInfo.ItemHash}" : ""));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    setStatus($"Error! See error message in {runId}", true);
                }
                finally
                {
                    log.Close();
                    Console.SetOut(stdout);
                }
            });
            randb.Enabled = true;
            randb.Text = $"Randomize!";
            randb.BackColor = SystemColors.Control;
            working = false;
            UpdateExePath();
        }

        private void UpdateFile(object sender, EventArgs e)
        {
            UpdateExePath();
        }

        private void UpdateOptions(object sender, EventArgs e)
        {
            ReadControlFlags(this);
            Properties.Settings.Default.Options = string.Join(" ", options.GetEnabled());
            Properties.Settings.Default.Save();
        }

        private void UpdateLanguage(object sender, EventArgs e)
        {
            Properties.Settings.Default.Language = (string)language.SelectedValue;
            Properties.Settings.Default.Save();
        }

        private void Restore(object sender, EventArgs e)
        {
            if (working) return;
            string gamePath = Path.GetDirectoryName(exe.Text);
            if (exe.Text.Trim() == "" || !Directory.Exists(gamePath) || game == FromGame.UNKNOWN)
            {
                return;
            }
            List<string> files = GameDataWriter.GetAllBaseFiles(game);
            List<string> desc = new List<string>();
            foreach (string file in files)
            {
                string path = $@"{gamePath}\{file}";
                string bak = path + ".bak";
                if (File.Exists(bak)) desc.Add(path);
            }
            string fullRestore = game == FromGame.DS1R
                ? "\n\nTo completely ensure restoration of vanilla files, also use Properties -> Local Files -> Verify Integrity Of Game Files in Steam."
                : "";  // Can't really recommend anything for PTDE
            DialogResult dialogResult = MessageBox.Show(string.Join("\n", desc) + fullRestore, "Restore these files?", MessageBoxButtons.YesNo);
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
            UpdateExePath();
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
