namespace ResoniteGameStreamerApp
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
            components = new System.ComponentModel.Container();
            targetWindowTextBox = new System.Windows.Forms.TextBox();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            button1 = new System.Windows.Forms.Button();
            panel1 = new System.Windows.Forms.Panel();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            label1 = new System.Windows.Forms.Label();
            tabControl1 = new System.Windows.Forms.TabControl();
            tabPage1 = new System.Windows.Forms.TabPage();
            colorModeLabel = new System.Windows.Forms.Label();
            colorModeComboBox = new System.Windows.Forms.ComboBox();
            consolePresetComboBox = new System.Windows.Forms.ComboBox();
            label13 = new System.Windows.Forms.Label();
            canvasWidthLabel = new System.Windows.Forms.Label();
            canvasWidthTextBox = new System.Windows.Forms.TextBox();
            canvasHeightLabel = new System.Windows.Forms.Label();
            canvasHeightTextBox = new System.Windows.Forms.TextBox();
            label11 = new System.Windows.Forms.Label();
            borderWidthTextBox = new System.Windows.Forms.TextBox();
            previewPixelsChangedCountLabel = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            rowExpansionCheckBox = new System.Windows.Forms.CheckBox();
            publishedFPSLabel = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            titleBarHeightTextBox = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            previewCheckBox = new System.Windows.Forms.CheckBox();
            label8 = new System.Windows.Forms.Label();
            fullFrameIntervalTextBox = new System.Windows.Forms.TextBox();
            checkBox4 = new System.Windows.Forms.CheckBox();
            checkBox3 = new System.Windows.Forms.CheckBox();
            label6 = new System.Windows.Forms.Label();
            targetFramerateTextBox = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            brightnessTextBox = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            checkBox1 = new System.Windows.Forms.CheckBox();
            label2 = new System.Windows.Forms.Label();
            tabPage2 = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            SuspendLayout();
            // 
            // targetWindowTextBox
            // 
            targetWindowTextBox.Location = new System.Drawing.Point(146, 433);
            targetWindowTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            targetWindowTextBox.Name = "targetWindowTextBox";
            targetWindowTextBox.Size = new System.Drawing.Size(116, 23);
            targetWindowTextBox.TabIndex = 0;
            targetWindowTextBox.Text = "mGBA";
            targetWindowTextBox.TextChanged += targetWindowTextBox_TextChanged;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(7, 182);
            button1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(88, 27);
            button1.TabIndex = 2;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Location = new System.Drawing.Point(9, 60);
            panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(233, 115);
            panel1.TabIndex = 3;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new System.Drawing.Point(534, 60);
            pictureBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(299, 277);
            pictureBox1.TabIndex = 4;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(9, 212);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(134, 15);
            label1.TabIndex = 5;
            label1.Text = "Input Websocket Status:";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new System.Drawing.Point(0, 0);
            tabControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(920, 507);
            tabControl1.TabIndex = 6;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(colorModeLabel);
            tabPage1.Controls.Add(colorModeComboBox);
            tabPage1.Controls.Add(consolePresetComboBox);
            tabPage1.Controls.Add(label13);
            tabPage1.Controls.Add(canvasWidthLabel);
            tabPage1.Controls.Add(canvasWidthTextBox);
            tabPage1.Controls.Add(canvasHeightLabel);
            tabPage1.Controls.Add(canvasHeightTextBox);
            tabPage1.Controls.Add(label11);
            tabPage1.Controls.Add(borderWidthTextBox);
            tabPage1.Controls.Add(previewPixelsChangedCountLabel);
            tabPage1.Controls.Add(label12);
            tabPage1.Controls.Add(rowExpansionCheckBox);
            tabPage1.Controls.Add(publishedFPSLabel);
            tabPage1.Controls.Add(label10);
            tabPage1.Controls.Add(label9);
            tabPage1.Controls.Add(titleBarHeightTextBox);
            tabPage1.Controls.Add(label7);
            tabPage1.Controls.Add(previewCheckBox);
            tabPage1.Controls.Add(label8);
            tabPage1.Controls.Add(fullFrameIntervalTextBox);
            tabPage1.Controls.Add(checkBox4);
            tabPage1.Controls.Add(checkBox3);
            tabPage1.Controls.Add(label6);
            tabPage1.Controls.Add(targetFramerateTextBox);
            tabPage1.Controls.Add(label4);
            tabPage1.Controls.Add(brightnessTextBox);
            tabPage1.Controls.Add(label3);
            tabPage1.Controls.Add(checkBox1);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(targetWindowTextBox);
            tabPage1.Controls.Add(pictureBox1);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(panel1);
            tabPage1.Controls.Add(button1);
            tabPage1.Location = new System.Drawing.Point(4, 24);
            tabPage1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPage1.Size = new System.Drawing.Size(912, 479);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            tabPage1.Click += tabPage1_Click;
            // 
            // colorModeLabel
            // 
            colorModeLabel.AutoSize = true;
            colorModeLabel.Location = new System.Drawing.Point(715, 400);
            colorModeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            colorModeLabel.Name = "colorModeLabel";
            colorModeLabel.Size = new System.Drawing.Size(70, 15);
            colorModeLabel.TabIndex = 39;
            colorModeLabel.Text = "Color Mode";
            // 
            // colorModeComboBox
            // 
            colorModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            colorModeComboBox.FormattingEnabled = true;
            colorModeComboBox.Location = new System.Drawing.Point(681, 433);
            colorModeComboBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            colorModeComboBox.Name = "colorModeComboBox";
            colorModeComboBox.Size = new System.Drawing.Size(140, 23);
            colorModeComboBox.TabIndex = 38;
            colorModeComboBox.SelectedIndexChanged += colorModeComboBox_SelectedIndexChanged;
            // 
            // consolePresetComboBox
            // 
            consolePresetComboBox.FormattingEnabled = true;
            consolePresetComboBox.Items.AddRange(new object[] { "Gameboy" });
            consolePresetComboBox.Location = new System.Drawing.Point(4, 432);
            consolePresetComboBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            consolePresetComboBox.Name = "consolePresetComboBox";
            consolePresetComboBox.Size = new System.Drawing.Size(116, 23);
            consolePresetComboBox.TabIndex = 37;
            consolePresetComboBox.SelectedIndexChanged += consolePresetComboBox_SelectedIndexChanged;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(9, 400);
            label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(85, 15);
            label13.TabIndex = 36;
            label13.Text = "Console Preset";
            // 
            // canvasWidthLabel
            // 
            canvasWidthLabel.AutoSize = true;
            canvasWidthLabel.Location = new System.Drawing.Point(9, 262);
            canvasWidthLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            canvasWidthLabel.Name = "canvasWidthLabel";
            canvasWidthLabel.Size = new System.Drawing.Size(80, 15);
            canvasWidthLabel.TabIndex = 35;
            canvasWidthLabel.Text = "Canvas Width";
            // 
            // canvasWidthTextBox
            // 
            canvasWidthTextBox.Location = new System.Drawing.Point(4, 284);
            canvasWidthTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            canvasWidthTextBox.Name = "canvasWidthTextBox";
            canvasWidthTextBox.Size = new System.Drawing.Size(116, 23);
            canvasWidthTextBox.TabIndex = 32;
            canvasWidthTextBox.Text = "160";
            canvasWidthTextBox.TextChanged += canvasWidthTextBox_TextChanged;
            // 
            // canvasHeightLabel
            // 
            canvasHeightLabel.AutoSize = true;
            canvasHeightLabel.Location = new System.Drawing.Point(9, 322);
            canvasHeightLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            canvasHeightLabel.Name = "canvasHeightLabel";
            canvasHeightLabel.Size = new System.Drawing.Size(84, 15);
            canvasHeightLabel.TabIndex = 33;
            canvasHeightLabel.Text = "Canvas Height";
            // 
            // canvasHeightTextBox
            // 
            canvasHeightTextBox.Location = new System.Drawing.Point(4, 344);
            canvasHeightTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            canvasHeightTextBox.Name = "canvasHeightTextBox";
            canvasHeightTextBox.Size = new System.Drawing.Size(116, 23);
            canvasHeightTextBox.TabIndex = 33;
            canvasHeightTextBox.Text = "144";
            canvasHeightTextBox.TextChanged += canvasHeightTextBox_TextChanged;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(315, 202);
            label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(77, 15);
            label11.TabIndex = 31;
            label11.Text = "Border Width";
            // 
            // borderWidthTextBox
            // 
            borderWidthTextBox.Location = new System.Drawing.Point(309, 224);
            borderWidthTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            borderWidthTextBox.Name = "borderWidthTextBox";
            borderWidthTextBox.Size = new System.Drawing.Size(116, 23);
            borderWidthTextBox.TabIndex = 30;
            borderWidthTextBox.Text = "8";
            borderWidthTextBox.TextChanged += borderWidthTextBox_TextChanged;
            // 
            // previewPixelsChangedCountLabel
            // 
            previewPixelsChangedCountLabel.AutoSize = true;
            previewPixelsChangedCountLabel.Location = new System.Drawing.Point(393, 48);
            previewPixelsChangedCountLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            previewPixelsChangedCountLabel.Name = "previewPixelsChangedCountLabel";
            previewPixelsChangedCountLabel.Size = new System.Drawing.Size(13, 15);
            previewPixelsChangedCountLabel.TabIndex = 29;
            previewPixelsChangedCountLabel.Text = "0";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(298, 48);
            label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(91, 15);
            label12.TabIndex = 28;
            label12.Text = "Pixels Changed:";
            // 
            // rowExpansionCheckBox
            // 
            rowExpansionCheckBox.AutoSize = true;
            rowExpansionCheckBox.Location = new System.Drawing.Point(301, 162);
            rowExpansionCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            rowExpansionCheckBox.Name = "rowExpansionCheckBox";
            rowExpansionCheckBox.Size = new System.Drawing.Size(106, 19);
            rowExpansionCheckBox.TabIndex = 27;
            rowExpansionCheckBox.Text = "Row Expansion";
            rowExpansionCheckBox.UseVisualStyleBackColor = true;
            // 
            // publishedFPSLabel
            // 
            publishedFPSLabel.AutoSize = true;
            publishedFPSLabel.Location = new System.Drawing.Point(393, 18);
            publishedFPSLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            publishedFPSLabel.Name = "publishedFPSLabel";
            publishedFPSLabel.Size = new System.Drawing.Size(13, 15);
            publishedFPSLabel.TabIndex = 26;
            publishedFPSLabel.Text = "0";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(298, 18);
            label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(84, 15);
            label10.TabIndex = 25;
            label10.Text = "Published FPS:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(315, 262);
            label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(88, 15);
            label9.TabIndex = 24;
            label9.Text = "Title Bar Height";
            // 
            // titleBarHeightTextBox
            // 
            titleBarHeightTextBox.Location = new System.Drawing.Point(309, 284);
            titleBarHeightTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            titleBarHeightTextBox.Name = "titleBarHeightTextBox";
            titleBarHeightTextBox.Size = new System.Drawing.Size(116, 23);
            titleBarHeightTextBox.TabIndex = 23;
            titleBarHeightTextBox.Text = "30";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(149, 400);
            label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(86, 15);
            label7.TabIndex = 22;
            label7.Text = "Target Window";
            // 
            // previewCheckBox
            // 
            previewCheckBox.AutoSize = true;
            previewCheckBox.Checked = true;
            previewCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            previewCheckBox.Location = new System.Drawing.Point(717, 357);
            previewCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            previewCheckBox.Name = "previewCheckBox";
            previewCheckBox.Size = new System.Drawing.Size(68, 34);
            previewCheckBox.TabIndex = 21;
            previewCheckBox.Text = "Preview\r\nEnabled";
            previewCheckBox.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(298, 322);
            label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(158, 15);
            label8.TabIndex = 20;
            label8.Text = "Full Frame Interval (seconds)";
            // 
            // fullFrameIntervalTextBox
            // 
            fullFrameIntervalTextBox.Location = new System.Drawing.Point(309, 353);
            fullFrameIntervalTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            fullFrameIntervalTextBox.Name = "fullFrameIntervalTextBox";
            fullFrameIntervalTextBox.Size = new System.Drawing.Size(116, 23);
            fullFrameIntervalTextBox.TabIndex = 19;
            fullFrameIntervalTextBox.Text = "30";
            fullFrameIntervalTextBox.TextChanged += fullFrameIntervalTextBox_TextChanged;
            // 
            // checkBox4
            // 
            checkBox4.AutoSize = true;
            checkBox4.Location = new System.Drawing.Point(301, 120);
            checkBox4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBox4.Name = "checkBox4";
            checkBox4.Size = new System.Drawing.Size(174, 34);
            checkBox4.TabIndex = 18;
            checkBox4.Text = "Confirm Render from Server\r\n(for testing)";
            checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            checkBox3.AutoSize = true;
            checkBox3.Checked = true;
            checkBox3.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBox3.Location = new System.Drawing.Point(301, 78);
            checkBox3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBox3.Name = "checkBox3";
            checkBox3.Size = new System.Drawing.Size(123, 34);
            checkBox3.TabIndex = 17;
            checkBox3.Text = "Await Client \r\nRender Confirmed";
            checkBox3.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(306, 400);
            label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(95, 15);
            label6.TabIndex = 14;
            label6.Text = "Target Framerate";
            // 
            // targetFramerateTextBox
            // 
            targetFramerateTextBox.Location = new System.Drawing.Point(309, 433);
            targetFramerateTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            targetFramerateTextBox.Name = "targetFramerateTextBox";
            targetFramerateTextBox.Size = new System.Drawing.Size(116, 23);
            targetFramerateTextBox.TabIndex = 13;
            targetFramerateTextBox.Text = "36";
            targetFramerateTextBox.TextChanged += targetFramerateTextBox_TextChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(556, 400);
            label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(62, 15);
            label4.TabIndex = 11;
            label4.Text = "Brightness";
            // 
            // brightnessTextBox
            // 
            brightnessTextBox.Location = new System.Drawing.Point(534, 433);
            brightnessTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            brightnessTextBox.Name = "brightnessTextBox";
            brightnessTextBox.Size = new System.Drawing.Size(116, 23);
            brightnessTextBox.TabIndex = 9;
            brightnessTextBox.Text = "1";
            brightnessTextBox.TextChanged += brightnessTextBox_TextChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(142, 22);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(117, 15);
            label3.TabIndex = 0;
            label3.Text = "Capturable Windows";
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new System.Drawing.Point(556, 357);
            checkBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new System.Drawing.Size(68, 34);
            checkBox1.TabIndex = 7;
            checkBox1.Text = "Server\r\nEnabled";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(625, 22);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(92, 15);
            label2.TabIndex = 6;
            label2.Text = "Canvas Preview:";
            // 
            // tabPage2
            // 
            tabPage2.Location = new System.Drawing.Point(4, 24);
            tabPage2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPage2.Size = new System.Drawing.Size(912, 479);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(933, 519);
            Controls.Add(tabControl1);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "MainForm";
            Text = "Resonite Game Streamer App";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox targetWindowTextBox;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox brightnessTextBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox targetFramerateTextBox;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox fullFrameIntervalTextBox;
        private System.Windows.Forms.CheckBox previewCheckBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox titleBarHeightTextBox;
        private System.Windows.Forms.Label publishedFPSLabel;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox rowExpansionCheckBox;
        private System.Windows.Forms.Label previewPixelsChangedCountLabel;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox borderWidthTextBox;
        private System.Windows.Forms.Label canvasWidthLabel;
        private System.Windows.Forms.TextBox canvasWidthTextBox;
        private System.Windows.Forms.Label canvasHeightLabel;
        private System.Windows.Forms.TextBox canvasHeightTextBox;
        private System.Windows.Forms.ComboBox consolePresetComboBox;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label colorModeLabel;
        private System.Windows.Forms.ComboBox colorModeComboBox;
    }
}

