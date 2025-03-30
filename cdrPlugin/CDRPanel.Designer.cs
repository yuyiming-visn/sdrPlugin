namespace SDRSharp.CDR
{
    partial class CDRPanel
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.checkBox_enable = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_SpectrumMode = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_TransmissionMode = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox_programSelector = new System.Windows.Forms.GroupBox();
            this.radioButton_Prog2 = new System.Windows.Forms.RadioButton();
            this.radioButton_Prog1 = new System.Windows.Forms.RadioButton();
            this.radioButton_Prog0 = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBox_IQRecord = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.checkBox_AudioRecord = new System.Windows.Forms.CheckBox();
            this.mainTableLayoutPanel.SuspendLayout();
            this.groupBox_programSelector.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 2;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mainTableLayoutPanel.Controls.Add(this.checkBox_enable, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.label1, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.comboBox_SpectrumMode, 1, 1);
            this.mainTableLayoutPanel.Controls.Add(this.label2, 0, 2);
            this.mainTableLayoutPanel.Controls.Add(this.comboBox_TransmissionMode, 1, 2);
            this.mainTableLayoutPanel.Controls.Add(this.label3, 0, 3);
            this.mainTableLayoutPanel.Controls.Add(this.groupBox_programSelector, 1, 3);
            this.mainTableLayoutPanel.Controls.Add(this.label4, 0, 5);
            this.mainTableLayoutPanel.Controls.Add(this.checkBox_IQRecord, 1, 5);
            this.mainTableLayoutPanel.Controls.Add(this.label5, 0, 6);
            this.mainTableLayoutPanel.Controls.Add(this.checkBox_AudioRecord, 1, 6);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 8;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(150, 225);
            this.mainTableLayoutPanel.TabIndex = 0;
            // 
            // checkBox_enable
            // 
            this.checkBox_enable.AutoSize = true;
            this.checkBox_enable.Location = new System.Drawing.Point(3, 3);
            this.checkBox_enable.Name = "checkBox_enable";
            this.checkBox_enable.Size = new System.Drawing.Size(60, 16);
            this.checkBox_enable.TabIndex = 3;
            this.checkBox_enable.Text = "Enable";
            this.checkBox_enable.UseVisualStyleBackColor = true;
            this.checkBox_enable.CheckedChanged += new System.EventHandler(this.checkBox_enable_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "频谱模式";
            // 
            // comboBox_SpectrumMode
            // 
            this.comboBox_SpectrumMode.FormattingEnabled = true;
            this.comboBox_SpectrumMode.Items.AddRange(new object[] {
            "Mode1",
            "Mode2",
            "Mode9",
            "Mode10",
            "Mode21",
            "Mode22"});
            this.comboBox_SpectrumMode.Location = new System.Drawing.Point(78, 25);
            this.comboBox_SpectrumMode.Name = "comboBox_SpectrumMode";
            this.comboBox_SpectrumMode.Size = new System.Drawing.Size(69, 20);
            this.comboBox_SpectrumMode.TabIndex = 0;
            this.comboBox_SpectrumMode.SelectedIndexChanged += new System.EventHandler(this.comboBox_SpectrumMode_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "传输模式";
            // 
            // comboBox_TransmissionMode
            // 
            this.comboBox_TransmissionMode.FormattingEnabled = true;
            this.comboBox_TransmissionMode.Items.AddRange(new object[] {
            "Mode 1",
            "Mode 2",
            "Mode 3"});
            this.comboBox_TransmissionMode.Location = new System.Drawing.Point(78, 51);
            this.comboBox_TransmissionMode.Name = "comboBox_TransmissionMode";
            this.comboBox_TransmissionMode.Size = new System.Drawing.Size(69, 20);
            this.comboBox_TransmissionMode.TabIndex = 2;
            this.comboBox_TransmissionMode.SelectedIndexChanged += new System.EventHandler(this.comboBox_Mode_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "输出音频";
            // 
            // groupBox_programSelector
            // 
            this.groupBox_programSelector.Controls.Add(this.radioButton_Prog2);
            this.groupBox_programSelector.Controls.Add(this.radioButton_Prog1);
            this.groupBox_programSelector.Controls.Add(this.radioButton_Prog0);
            this.groupBox_programSelector.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox_programSelector.Location = new System.Drawing.Point(78, 77);
            this.groupBox_programSelector.Name = "groupBox_programSelector";
            this.groupBox_programSelector.Size = new System.Drawing.Size(69, 74);
            this.groupBox_programSelector.TabIndex = 5;
            this.groupBox_programSelector.TabStop = false;
            // 
            // radioButton_Prog2
            // 
            this.radioButton_Prog2.AutoSize = true;
            this.radioButton_Prog2.Location = new System.Drawing.Point(6, 58);
            this.radioButton_Prog2.Name = "radioButton_Prog2";
            this.radioButton_Prog2.Size = new System.Drawing.Size(59, 16);
            this.radioButton_Prog2.TabIndex = 0;
            this.radioButton_Prog2.TabStop = true;
            this.radioButton_Prog2.Text = "Prog 2";
            this.radioButton_Prog2.UseVisualStyleBackColor = true;
            this.radioButton_Prog2.CheckedChanged += new System.EventHandler(this.Program_CheckedChanged);
            // 
            // radioButton_Prog1
            // 
            this.radioButton_Prog1.AutoSize = true;
            this.radioButton_Prog1.Location = new System.Drawing.Point(6, 36);
            this.radioButton_Prog1.Name = "radioButton_Prog1";
            this.radioButton_Prog1.Size = new System.Drawing.Size(59, 16);
            this.radioButton_Prog1.TabIndex = 0;
            this.radioButton_Prog1.TabStop = true;
            this.radioButton_Prog1.Text = "Prog 1";
            this.radioButton_Prog1.UseVisualStyleBackColor = true;
            this.radioButton_Prog1.CheckedChanged += new System.EventHandler(this.Program_CheckedChanged);
            // 
            // radioButton_Prog0
            // 
            this.radioButton_Prog0.AutoSize = true;
            this.radioButton_Prog0.Location = new System.Drawing.Point(6, 14);
            this.radioButton_Prog0.Name = "radioButton_Prog0";
            this.radioButton_Prog0.Size = new System.Drawing.Size(59, 16);
            this.radioButton_Prog0.TabIndex = 0;
            this.radioButton_Prog0.TabStop = true;
            this.radioButton_Prog0.Text = "Prog 0";
            this.radioButton_Prog0.UseVisualStyleBackColor = true;
            this.radioButton_Prog0.CheckedChanged += new System.EventHandler(this.Program_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 154);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 4;
            this.label4.Text = "录制IQ";
            // 
            // checkBox_IQRecord
            // 
            this.checkBox_IQRecord.AutoSize = true;
            this.checkBox_IQRecord.Location = new System.Drawing.Point(78, 157);
            this.checkBox_IQRecord.Name = "checkBox_IQRecord";
            this.checkBox_IQRecord.Size = new System.Drawing.Size(15, 14);
            this.checkBox_IQRecord.TabIndex = 6;
            this.checkBox_IQRecord.UseVisualStyleBackColor = true;
            this.checkBox_IQRecord.CheckedChanged += new System.EventHandler(this.checkBox_IQRecord_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 174);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 12);
            this.label5.TabIndex = 4;
            this.label5.Text = "录制音频";
            // 
            // checkBox_AudioRecord
            // 
            this.checkBox_AudioRecord.AutoSize = true;
            this.checkBox_AudioRecord.Location = new System.Drawing.Point(78, 177);
            this.checkBox_AudioRecord.Name = "checkBox_AudioRecord";
            this.checkBox_AudioRecord.Size = new System.Drawing.Size(15, 14);
            this.checkBox_AudioRecord.TabIndex = 6;
            this.checkBox_AudioRecord.UseVisualStyleBackColor = true;
            // 
            // CDRPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Name = "CDRPanel";
            this.Size = new System.Drawing.Size(150, 225);
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.mainTableLayoutPanel.PerformLayout();
            this.groupBox_programSelector.ResumeLayout(false);
            this.groupBox_programSelector.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private System.Windows.Forms.ComboBox comboBox_SpectrumMode;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_TransmissionMode;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBox_enable;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox_programSelector;
        private System.Windows.Forms.RadioButton radioButton_Prog2;
        private System.Windows.Forms.RadioButton radioButton_Prog1;
        private System.Windows.Forms.RadioButton radioButton_Prog0;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBox_IQRecord;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox checkBox_AudioRecord;
    }
}
