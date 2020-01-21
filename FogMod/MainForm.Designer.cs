namespace FogMod
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.minor = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.major = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lordvessel = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.warp = new System.Windows.Forms.CheckBox();
            this.bossL = new System.Windows.Forms.Label();
            this.boss = new System.Windows.Forms.CheckBox();
            this.worldL = new System.Windows.Forms.Label();
            this.world = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.start = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.unconnected = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.bboc = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.scale = new System.Windows.Forms.CheckBox();
            this.hard = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lords = new System.Windows.Forms.CheckBox();
            this.pacifist = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.fixedseed = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.randb = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.exe = new System.Windows.Forms.TextBox();
            this.restoreButton = new System.Windows.Forms.Button();
            this.restoreL = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusL = new System.Windows.Forms.ToolStripStatusLabel();
            this.randomizeL = new System.Windows.Forms.TextBox();
            this.language = new System.Windows.Forms.ComboBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.minor);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.major);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.lordvessel);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.warp);
            this.groupBox1.Controls.Add(this.bossL);
            this.groupBox1.Controls.Add(this.boss);
            this.groupBox1.Controls.Add(this.worldL);
            this.groupBox1.Controls.Add(this.world);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.groupBox1.Location = new System.Drawing.Point(16, 14);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(451, 255);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Randomized entrances";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label3.Location = new System.Drawing.Point(24, 194);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(367, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Enable and randomize invasion fog gates usually separating off smaller areas";
            // 
            // minor
            // 
            this.minor.AutoSize = true;
            this.minor.Location = new System.Drawing.Point(7, 173);
            this.minor.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.minor.Name = "minor";
            this.minor.Size = new System.Drawing.Size(147, 20);
            this.minor.TabIndex = 8;
            this.minor.Text = "Minor PvP fog gates";
            this.minor.UseVisualStyleBackColor = true;
            this.minor.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label4.Location = new System.Drawing.Point(24, 157);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(310, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Enable and randomize invasion fog gates separating major areas";
            // 
            // major
            // 
            this.major.AutoSize = true;
            this.major.Location = new System.Drawing.Point(7, 136);
            this.major.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.major.Name = "major";
            this.major.Size = new System.Drawing.Size(148, 20);
            this.major.TabIndex = 6;
            this.major.Text = "Major PvP fog gates";
            this.major.UseVisualStyleBackColor = true;
            this.major.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label5.Location = new System.Drawing.Point(25, 232);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(328, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Randomize golden fog gates, in which case they are never dispelled";
            // 
            // lordvessel
            // 
            this.lordvessel.AutoSize = true;
            this.lordvessel.Location = new System.Drawing.Point(8, 209);
            this.lordvessel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lordvessel.Name = "lordvessel";
            this.lordvessel.Size = new System.Drawing.Size(131, 20);
            this.lordvessel.TabIndex = 10;
            this.lordvessel.Text = "Lordvessel gates";
            this.lordvessel.UseVisualStyleBackColor = true;
            this.lordvessel.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label2.Location = new System.Drawing.Point(24, 121);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(274, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Randomize warp destinations, like to/from Painted World";
            // 
            // warp
            // 
            this.warp.AutoSize = true;
            this.warp.Checked = true;
            this.warp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.warp.Location = new System.Drawing.Point(7, 98);
            this.warp.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.warp.Name = "warp";
            this.warp.Size = new System.Drawing.Size(159, 20);
            this.warp.TabIndex = 4;
            this.warp.Text = "Warps between areas";
            this.warp.UseVisualStyleBackColor = true;
            this.warp.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // bossL
            // 
            this.bossL.AutoSize = true;
            this.bossL.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.bossL.Location = new System.Drawing.Point(24, 82);
            this.bossL.Name = "bossL";
            this.bossL.Size = new System.Drawing.Size(199, 13);
            this.bossL.TabIndex = 3;
            this.bossL.Text = "Randomize fog gates to and from bosses";
            // 
            // boss
            // 
            this.boss.AutoSize = true;
            this.boss.Checked = true;
            this.boss.CheckState = System.Windows.Forms.CheckState.Checked;
            this.boss.Location = new System.Drawing.Point(7, 62);
            this.boss.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.boss.Name = "boss";
            this.boss.Size = new System.Drawing.Size(117, 20);
            this.boss.TabIndex = 2;
            this.boss.Text = "Boss fog gates";
            this.boss.UseVisualStyleBackColor = true;
            this.boss.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // worldL
            // 
            this.worldL.AutoSize = true;
            this.worldL.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.worldL.Location = new System.Drawing.Point(25, 44);
            this.worldL.Name = "worldL";
            this.worldL.Size = new System.Drawing.Size(149, 13);
            this.worldL.TabIndex = 1;
            this.worldL.Text = "Randomize two-way fog gates";
            // 
            // world
            // 
            this.world.AutoSize = true;
            this.world.Checked = true;
            this.world.CheckState = System.Windows.Forms.CheckState.Checked;
            this.world.Location = new System.Drawing.Point(8, 23);
            this.world.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.world.Name = "world";
            this.world.Size = new System.Drawing.Size(227, 20);
            this.world.TabIndex = 0;
            this.world.Text = "Traversable fog gates (non-boss)";
            this.world.UseVisualStyleBackColor = true;
            this.world.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label12);
            this.groupBox2.Controls.Add(this.start);
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.unconnected);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.bboc);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.scale);
            this.groupBox2.Controls.Add(this.hard);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.lords);
            this.groupBox2.Controls.Add(this.pacifist);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.groupBox2.Location = new System.Drawing.Point(475, 14);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(451, 296);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Options";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label12.Location = new System.Drawing.Point(24, 265);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(335, 13);
            this.label12.TabIndex = 25;
            this.label12.Text = "Immediately warp away from Asylum, returning later through a fog gate";
            // 
            // start
            // 
            this.start.AutoSize = true;
            this.start.Location = new System.Drawing.Point(7, 244);
            this.start.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.start.Name = "start";
            this.start.Size = new System.Drawing.Size(215, 20);
            this.start.TabIndex = 24;
            this.start.Text = "Random start outside of Asylum";
            this.start.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label11.Location = new System.Drawing.Point(24, 229);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(365, 13);
            this.label11.TabIndex = 23;
            this.label11.Text = "If enabled, entering a fog gate you just exited can send you somewhere else";
            // 
            // unconnected
            // 
            this.unconnected.AutoSize = true;
            this.unconnected.Location = new System.Drawing.Point(7, 208);
            this.unconnected.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.unconnected.Name = "unconnected";
            this.unconnected.Size = new System.Drawing.Size(169, 20);
            this.unconnected.TabIndex = 22;
            this.unconnected.Text = "Disconnected fog gates";
            this.unconnected.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label9.Location = new System.Drawing.Point(24, 193);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(280, 13);
            this.label9.TabIndex = 21;
            this.label9.Text = "BoC floor no longer crumbles. Not related to randomization";
            // 
            // bboc
            // 
            this.bboc.AutoSize = true;
            this.bboc.Location = new System.Drawing.Point(7, 172);
            this.bboc.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bboc.Name = "bboc";
            this.bboc.Size = new System.Drawing.Size(155, 20);
            this.bboc.TabIndex = 20;
            this.bboc.Text = "No-Fall Bed of Chaos";
            this.bboc.UseVisualStyleBackColor = true;
            this.bboc.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label1.Location = new System.Drawing.Point(24, 156);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(357, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "Various glitches may be required, similar to Race Mode+ in item randomizer";
            // 
            // scale
            // 
            this.scale.AutoSize = true;
            this.scale.Checked = true;
            this.scale.CheckState = System.Windows.Forms.CheckState.Checked;
            this.scale.Location = new System.Drawing.Point(7, 22);
            this.scale.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.scale.Name = "scale";
            this.scale.Size = new System.Drawing.Size(191, 20);
            this.scale.TabIndex = 12;
            this.scale.Text = "Scale enemies and bosses";
            this.scale.UseVisualStyleBackColor = true;
            this.scale.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // hard
            // 
            this.hard.AutoSize = true;
            this.hard.Location = new System.Drawing.Point(7, 135);
            this.hard.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.hard.Name = "hard";
            this.hard.Size = new System.Drawing.Size(108, 20);
            this.hard.TabIndex = 18;
            this.hard.Text = "Glitched logic";
            this.hard.UseVisualStyleBackColor = true;
            this.hard.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label8.Location = new System.Drawing.Point(24, 43);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(371, 13);
            this.label8.TabIndex = 13;
            this.label8.Text = "Increase or decrease enemy health and damage based on distance from start";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label6.Location = new System.Drawing.Point(23, 119);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(251, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "Allow escaping boss fights without defeating bosses";
            // 
            // lords
            // 
            this.lords.AutoSize = true;
            this.lords.Checked = true;
            this.lords.CheckState = System.Windows.Forms.CheckState.Checked;
            this.lords.Location = new System.Drawing.Point(5, 62);
            this.lords.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lords.Name = "lords";
            this.lords.Size = new System.Drawing.Size(142, 20);
            this.lords.TabIndex = 14;
            this.lords.Text = "Require Lord Souls";
            this.lords.UseVisualStyleBackColor = true;
            this.lords.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // pacifist
            // 
            this.pacifist.AutoSize = true;
            this.pacifist.Location = new System.Drawing.Point(5, 98);
            this.pacifist.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pacifist.Name = "pacifist";
            this.pacifist.Size = new System.Drawing.Size(108, 20);
            this.pacifist.TabIndex = 16;
            this.pacifist.Text = "Pacifist Mode";
            this.pacifist.UseVisualStyleBackColor = true;
            this.pacifist.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label7.Location = new System.Drawing.Point(23, 82);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(225, 13);
            this.label7.TabIndex = 15;
            this.label7.Text = "Require opening the kiln door to access Gwyn";
            // 
            // fixedseed
            // 
            this.fixedseed.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.fixedseed.Location = new System.Drawing.Point(161, 379);
            this.fixedseed.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.fixedseed.Name = "fixedseed";
            this.fixedseed.Size = new System.Drawing.Size(153, 22);
            this.fixedseed.TabIndex = 5;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.label10.Location = new System.Drawing.Point(18, 382);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(137, 16);
            this.label10.TabIndex = 3;
            this.label10.Text = "Fixed seed (optional):";
            // 
            // randb
            // 
            this.randb.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.randb.ForeColor = System.Drawing.SystemColors.ControlText;
            this.randb.Location = new System.Drawing.Point(794, 379);
            this.randb.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.randb.Name = "randb";
            this.randb.Size = new System.Drawing.Size(121, 27);
            this.randb.TabIndex = 6;
            this.randb.Text = "Randomize!";
            this.randb.UseVisualStyleBackColor = false;
            this.randb.Click += new System.EventHandler(this.Randomize);
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.button2.Location = new System.Drawing.Point(794, 318);
            this.button2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(121, 27);
            this.button2.TabIndex = 3;
            this.button2.Text = "Select game exe";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.OpenExe);
            // 
            // exe
            // 
            this.exe.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.exe.Location = new System.Drawing.Point(16, 320);
            this.exe.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.exe.Name = "exe";
            this.exe.Size = new System.Drawing.Size(770, 22);
            this.exe.TabIndex = 2;
            this.exe.TextChanged += new System.EventHandler(this.UpdateFile);
            // 
            // restoreButton
            // 
            this.restoreButton.Enabled = false;
            this.restoreButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.restoreButton.Location = new System.Drawing.Point(794, 349);
            this.restoreButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.restoreButton.Name = "restoreButton";
            this.restoreButton.Size = new System.Drawing.Size(121, 27);
            this.restoreButton.TabIndex = 4;
            this.restoreButton.Text = "Restore backups";
            this.restoreButton.UseVisualStyleBackColor = true;
            this.restoreButton.Click += new System.EventHandler(this.Restore);
            // 
            // restoreL
            // 
            this.restoreL.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.restoreL.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.restoreL.Location = new System.Drawing.Point(320, 347);
            this.restoreL.Name = "restoreL";
            this.restoreL.Size = new System.Drawing.Size(466, 27);
            this.restoreL.TabIndex = 9;
            this.restoreL.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusL});
            this.statusStrip1.Location = new System.Drawing.Point(0, 428);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(932, 22);
            this.statusStrip1.TabIndex = 10;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // statusL
            // 
            this.statusL.Name = "statusL";
            this.statusL.Size = new System.Drawing.Size(131, 17);
            this.statusL.Text = "Created by thefifthmatt";
            // 
            // randomizeL
            // 
            this.randomizeL.BackColor = System.Drawing.SystemColors.Control;
            this.randomizeL.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.randomizeL.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.randomizeL.Location = new System.Drawing.Point(796, 410);
            this.randomizeL.Name = "randomizeL";
            this.randomizeL.ReadOnly = true;
            this.randomizeL.Size = new System.Drawing.Size(119, 14);
            this.randomizeL.TabIndex = 11;
            this.randomizeL.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // language
            // 
            this.language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.language.Enabled = false;
            this.language.FormattingEnabled = true;
            this.language.Location = new System.Drawing.Point(161, 349);
            this.language.Name = "language";
            this.language.Size = new System.Drawing.Size(153, 24);
            this.language.TabIndex = 12;
            this.language.SelectedIndexChanged += new System.EventHandler(this.UpdateLanguage);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.label13.Location = new System.Drawing.Point(19, 354);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(108, 16);
            this.label13.TabIndex = 13;
            this.label13.Text = "Game language:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.label14.Location = new System.Drawing.Point(19, 275);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(382, 32);
            this.label14.TabIndex = 14;
            this.label14.Text = "Runs usually take 4-8 hours to complete depending on options. \r\nSee documentation" +
    " to learn more!";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(932, 450);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.language);
            this.Controls.Add(this.randomizeL);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.restoreL);
            this.Controls.Add(this.restoreButton);
            this.Controls.Add(this.exe);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.randb);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.fixedseed);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "DS1 Fog Gate Randomizer v0.3";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox world;
        private System.Windows.Forms.Label bossL;
        private System.Windows.Forms.CheckBox boss;
        private System.Windows.Forms.Label worldL;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox minor;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox major;
        private System.Windows.Forms.CheckBox lordvessel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox warp;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox scale;
        private System.Windows.Forms.CheckBox hard;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox lords;
        private System.Windows.Forms.CheckBox pacifist;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox bboc;
        private System.Windows.Forms.TextBox fixedseed;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button randb;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox exe;
        private System.Windows.Forms.Button restoreButton;
        private System.Windows.Forms.Label restoreL;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusL;
        private System.Windows.Forms.TextBox randomizeL;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox unconnected;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.CheckBox start;
        private System.Windows.Forms.ComboBox language;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
    }
}

