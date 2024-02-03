using Microsoft.VisualBasic.Logging;
using NAudio.Midi;
using StepSequencer.Common;
using StepSequencer.Core;
using StepSequencer.Core.MIDIProcess;
using System.Diagnostics;

namespace StepSequencer
{
    public partial class Form1 : Form
    {
        private bool initMode = true;

        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            //GlobalInfo.Instance.MidiInProcess.LoadMidiInDevice();
            MidiProcessInit();
        }

        private void MidiProcessInit() 
        {
            MidiInProcess.Instance.LoadMidiInDevice();
            comboBoxMidiInDevices.DataSource = GlobalInfo.Instance.DeviceDic.ToArray();
            comboBoxMidiInDevices.DisplayMember = "Key";
            comboBoxMidiInDevices.ValueMember = "Value";
        }


        private void comboBoxMidiInDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initMode)
            {
                initMode = false;
                return;
            }
            var deviceKeyName = ConvertHelper.ToString(comboBoxMidiInDevices.Text);
            var deviceIndex = ConvertHelper.ToInt(comboBoxMidiInDevices.SelectedValue);

            Debug.WriteLine($"{deviceKeyName} {deviceIndex}");
            MidiInProcess.Instance.StartMidiIn(deviceKeyName,deviceIndex);
        }
    }
}