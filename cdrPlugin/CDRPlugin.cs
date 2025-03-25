using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using SDRSharp.Common;
using SDRSharp.Radio;

namespace SDRSharp.CDR
{
    public class CDRPlugin : ISharpPlugin
    {
        private const string _displayName = "CDR Demodulation";
        private CDRPanel _configGui;
        private CDRProcessor _processor;

        public UserControl Gui
        {
            get { return _configGui; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public void Close()
        {
            _processor.Enabled = false;
        }

        public void Initialize(ISharpControl control)
        {
            _processor = new CDRProcessor(control);
            _processor.Enabled = false;

            _configGui = new CDRPanel(_processor);
        }
    }
}
