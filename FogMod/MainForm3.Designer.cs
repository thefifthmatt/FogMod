namespace FogMod
{
    partial class MainForm3
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm3));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.pvp = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.warp = new System.Windows.Forms.CheckBox();
            this.bossL = new System.Windows.Forms.Label();
            this.boss = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label11 = new System.Windows.Forms.Label();
            this.unconnected = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.scale = new System.Windows.Forms.CheckBox();
            this.treeskip = new System.Windows.Forms.CheckBox();
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
            this.errorL = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusL = new System.Windows.Forms.ToolStripStatusLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label13 = new System.Windows.Forms.Label();
            this.earlywarp = new System.Windows.Forms.RadioButton();
            this.instawarp = new System.Windows.Forms.RadioButton();
            this.label9 = new System.Windows.Forms.Label();
            this.latewarp = new System.Windows.Forms.RadioButton();
            this.label12 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.pvp);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.warp);
            this.groupBox1.Controls.Add(this.bossL);
            this.groupBox1.Controls.Add(this.boss);
            this.groupBox1.Controls.Add(this.lords);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.groupBox1.Location = new System.Drawing.Point(16, 14);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(420, 181);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Randomized entrances";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label4.Location = new System.Drawing.Point(24, 117);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(265, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Enable and randomize fog gates separating PvP zones";
            // 
            // pvp
            // 
            this.pvp.AutoSize = true;
            this.pvp.Checked = true;
            this.pvp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.pvp.Location = new System.Drawing.Point(7, 96);
            this.pvp.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pvp.Name = "pvp";
            this.pvp.Size = new System.Drawing.Size(111, 20);
            this.pvp.TabIndex = 6;
            this.pvp.Text = "PvP fog gates";
            this.pvp.UseVisualStyleBackColor = true;
            this.pvp.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label2.Location = new System.Drawing.Point(24, 81);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(208, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Randomize warp destinations, like to DLCs";
            // 
            // warp
            // 
            this.warp.AutoSize = true;
            this.warp.Checked = true;
            this.warp.CheckState = System.Windows.Forms.CheckState.Checked;
            this.warp.Location = new System.Drawing.Point(7, 58);
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
            this.bossL.Location = new System.Drawing.Point(24, 42);
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
            this.boss.Location = new System.Drawing.Point(7, 22);
            this.boss.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.boss.Name = "boss";
            this.boss.Size = new System.Drawing.Size(117, 20);
            this.boss.TabIndex = 2;
            this.boss.Text = "Boss fog gates";
            this.boss.UseVisualStyleBackColor = true;
            this.boss.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.unconnected);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.scale);
            this.groupBox2.Controls.Add(this.treeskip);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.pacifist);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.groupBox2.Location = new System.Drawing.Point(444, 14);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(471, 181);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Misc options";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label11.Location = new System.Drawing.Point(26, 154);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(361, 13);
            this.label11.TabIndex = 23;
            this.label11.Text = "Entering a fog gate you just exited can send you to a different fixed location";
            // 
            // unconnected
            // 
            this.unconnected.AutoSize = true;
            this.unconnected.Location = new System.Drawing.Point(9, 133);
            this.unconnected.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.unconnected.Name = "unconnected";
            this.unconnected.Size = new System.Drawing.Size(169, 20);
            this.unconnected.TabIndex = 22;
            this.unconnected.Text = "Disconnected fog gates";
            this.unconnected.UseVisualStyleBackColor = true;
            this.unconnected.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label1.Location = new System.Drawing.Point(26, 116);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(307, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "Logic assumes you can jump to Firelink Shrine roof from the tree";
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
            // treeskip
            // 
            this.treeskip.AutoSize = true;
            this.treeskip.Location = new System.Drawing.Point(9, 95);
            this.treeskip.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.treeskip.Name = "treeskip";
            this.treeskip.Size = new System.Drawing.Size(84, 20);
            this.treeskip.TabIndex = 18;
            this.treeskip.Text = "Tree skip";
            this.treeskip.UseVisualStyleBackColor = true;
            this.treeskip.CheckedChanged += new System.EventHandler(this.UpdateOptions);
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
            this.label6.Location = new System.Drawing.Point(25, 79);
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
            this.lords.Location = new System.Drawing.Point(7, 132);
            this.lords.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.lords.Name = "lords";
            this.lords.Size = new System.Drawing.Size(179, 20);
            this.lords.TabIndex = 14;
            this.lords.Text = "Require Cinders of a Lord";
            this.lords.UseVisualStyleBackColor = true;
            this.lords.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // pacifist
            // 
            this.pacifist.AutoSize = true;
            this.pacifist.Location = new System.Drawing.Point(7, 58);
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
            this.label7.Location = new System.Drawing.Point(25, 152);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(335, 13);
            this.label7.TabIndex = 15;
            this.label7.Text = "Access to Soul of Cinder via Firelink Shrine and Kiln is not randomized";
            // 
            // fixedseed
            // 
            this.fixedseed.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.fixedseed.Location = new System.Drawing.Point(583, 391);
            this.fixedseed.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.fixedseed.Name = "fixedseed";
            this.fixedseed.Size = new System.Drawing.Size(153, 22);
            this.fixedseed.TabIndex = 15;
            this.fixedseed.TextChanged += new System.EventHandler(this.fixedseed_TextChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.label10.Location = new System.Drawing.Point(534, 394);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(44, 16);
            this.label10.TabIndex = 3;
            this.label10.Text = "Seed:";
            // 
            // randb
            // 
            this.randb.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.randb.ForeColor = System.Drawing.SystemColors.ControlText;
            this.randb.Location = new System.Drawing.Point(742, 389);
            this.randb.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.randb.Name = "randb";
            this.randb.Size = new System.Drawing.Size(173, 27);
            this.randb.TabIndex = 16;
            this.randb.Text = "Randomize!";
            this.randb.UseVisualStyleBackColor = false;
            this.randb.Click += new System.EventHandler(this.Randomize);
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.button2.Location = new System.Drawing.Point(742, 345);
            this.button2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(173, 27);
            this.button2.TabIndex = 13;
            this.button2.Text = "Select other mod to merge";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.OpenExe);
            // 
            // exe
            // 
            this.exe.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.exe.Location = new System.Drawing.Point(12, 347);
            this.exe.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.exe.Name = "exe";
            this.exe.Size = new System.Drawing.Size(724, 22);
            this.exe.TabIndex = 12;
            this.exe.TextChanged += new System.EventHandler(this.UpdateFile);
            // 
            // errorL
            // 
            this.errorL.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.errorL.ForeColor = System.Drawing.Color.Crimson;
            this.errorL.Location = new System.Drawing.Point(444, 211);
            this.errorL.Name = "errorL";
            this.errorL.Size = new System.Drawing.Size(471, 128);
            this.errorL.TabIndex = 9;
            this.errorL.Text = resources.GetString("errorL.Text");
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusL});
            this.statusStrip1.Location = new System.Drawing.Point(0, 438);
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
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label3.Location = new System.Drawing.Point(741, 415);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(180, 13);
            this.label3.TabIndex = 24;
            this.label3.Text = "Leave seed blank for a random seed";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label5.Location = new System.Drawing.Point(740, 372);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(176, 13);
            this.label5.TabIndex = 25;
            this.label5.Text = "Leave blank to run this mod by itself";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.latewarp);
            this.groupBox3.Controls.Add(this.instawarp);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.earlywarp);
            this.groupBox3.Controls.Add(this.label13);
            this.groupBox3.Controls.Add(this.label12);
            this.groupBox3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.groupBox3.Location = new System.Drawing.Point(13, 203);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox3.Size = new System.Drawing.Size(423, 136);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Warping between bonfires";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label13.Location = new System.Drawing.Point(24, 40);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(320, 13);
            this.label13.TabIndex = 13;
            this.label13.Text = "Firelink Shrine and Coiled Sword are routed in early. Balanced start";
            // 
            // earlywarp
            // 
            this.earlywarp.AutoSize = true;
            this.earlywarp.Checked = true;
            this.earlywarp.Location = new System.Drawing.Point(9, 20);
            this.earlywarp.Name = "earlywarp";
            this.earlywarp.Size = new System.Drawing.Size(198, 20);
            this.earlywarp.TabIndex = 14;
            this.earlywarp.TabStop = true;
            this.earlywarp.Text = "Coiled Sword available early";
            this.earlywarp.UseVisualStyleBackColor = true;
            this.earlywarp.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // instawarp
            // 
            this.instawarp.AutoSize = true;
            this.instawarp.Location = new System.Drawing.Point(9, 92);
            this.instawarp.Name = "instawarp";
            this.instawarp.Size = new System.Drawing.Size(180, 20);
            this.instawarp.TabIndex = 16;
            this.instawarp.Text = "Coiled Sword not required";
            this.instawarp.UseVisualStyleBackColor = true;
            this.instawarp.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label9.Location = new System.Drawing.Point(24, 112);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(388, 13);
            this.label9.TabIndex = 15;
            this.label9.Text = "Firelink Shrine, and warping between bonfires, is available immediately. Easy sta" +
    "rt";
            // 
            // latewarp
            // 
            this.latewarp.AutoSize = true;
            this.latewarp.Location = new System.Drawing.Point(7, 56);
            this.latewarp.Name = "latewarp";
            this.latewarp.Size = new System.Drawing.Size(211, 20);
            this.latewarp.TabIndex = 18;
            this.latewarp.Text = "Coiled Sword can be anywhere";
            this.latewarp.UseVisualStyleBackColor = true;
            this.latewarp.CheckedChanged += new System.EventHandler(this.UpdateOptions);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label12.Location = new System.Drawing.Point(22, 76);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(388, 13);
            this.label12.TabIndex = 17;
            this.label12.Text = "Firelink is still early, but Coiled Sword is like Lordvessel in Dark Souls. Slowe" +
    "r start";
            // 
            // MainForm3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(932, 460);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.exe);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.randb);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.fixedseed);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.errorL);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm3";
            this.Text = "DS3 Fog Gate Randomizer v0.1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label bossL;
        private System.Windows.Forms.CheckBox boss;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox pvp;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox warp;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox scale;
        private System.Windows.Forms.CheckBox treeskip;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox lords;
        private System.Windows.Forms.CheckBox pacifist;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox fixedseed;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button randb;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox exe;
        private System.Windows.Forms.Label errorL;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusL;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox unconnected;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.RadioButton latewarp;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.RadioButton instawarp;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.RadioButton earlywarp;
    }
}