using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SDRSharp.CDR
{
    public partial class CDRPanel : UserControl
    {
        private CDRProcessor _processor;
        private int _audioProgramID;

        public CDRPanel(CDRProcessor processor)
        {
            InitializeComponent();
            _processor = processor;

            checkBox_enable.Checked = _processor.Enabled;
            comboBox_SpectrumMode.SelectedIndex = 2;
            comboBox_TransmissionMode.SelectedIndex = 0;
        }

        private void comboBox_SpectrumMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("comboBox_SpectrumMode change");
            Debug.WriteLine(comboBox_SpectrumMode.SelectedText);
        }

        private void comboBox_Mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("comboBox_Mode change");
            Debug.WriteLine(comboBox_TransmissionMode.SelectedItem.ToString());
        }

        private void checkBox_enable_CheckedChanged(object sender, EventArgs e)
        {
            checkBox_IQRecord.Checked = false;
            _processor.Enabled = checkBox_enable.Checked;
        }

        private void Program_CheckedChanged(object sender, EventArgs e)
        {
            if (!((RadioButton)sender).Checked)
            {
                return;
            }

            switch (((RadioButton)sender).Name)
            {
                case "_guiProg0":
                    _audioProgramID = 0;
                    break;
                case "_guiProg1":
                    _audioProgramID = 1;
                    break;
                case "_guiProg2":
                    _audioProgramID = 2;
                    break;
            }
            // 清空上一个节目的音频缓存
            // AudioStop();
            // AudioStart();
        }

        private void checkBox_IQRecord_CheckedChanged(object sender, EventArgs e)
        {
            _processor.IQRecordEnable = checkBox_IQRecord.Checked;
        }

        private void checkBox_AudioRecord_CheckedChanged(object sender, EventArgs e)
        {
            _processor.WavRecordEnable = checkBox_AudioRecord.Checked; 
        }
    }
}
