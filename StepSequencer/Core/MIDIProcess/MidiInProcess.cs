using NAudio.CoreAudioApi;
using NAudio.Midi;
using StepSequencer.Common;
using StepSequencer.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepSequencer.Core.MIDIProcess
{
    public class MidiInProcess: Singleton<MidiInProcess>
    {

        #region Variable Def

        /// <summary>
        /// 写入状态
        /// 0表示选择音符/音高的状态
        /// 1表示选中音符后开始选择序列/拍子
        /// </summary>
        private int writeState = 0;

        //private MidiIn? midiInDevice;
        /// <summary>
        /// Midi输入设备
        /// </summary>
        public MidiIn? MidiInDevice
        {
            get; set;
        }

        #endregion

        public MidiInProcess() 
        {
            
        }

        private int count = 0;

        /// <summary>
        /// 加载设备字典
        /// </summary>
        public void LoadMidiInDevice()
        {
            var gb = GlobalInfo.Instance;
            ClearMidiInDevice();
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                gb.DeviceDic.Add(MidiIn.DeviceInfo(device).ProductName,device);

            }

            //测试数据
            //gb.DeviceDic.Add("Launchpad mini", 0);

            if (!string.IsNullOrEmpty(gb.SelectDeviceKeyName) && gb.DeviceDic != null && gb.DeviceDic.Count > 0)
            {
                if (MidiInDevice != null)
                {
                    MidiInDevice.Dispose();
                    MidiInDevice = null;
                }
                MidiInDevice = new MidiIn(gb.DeviceDic[gb.SelectDeviceKeyName]);
            }
        }

        /// <summary>
        /// 清空设备字典列表
        /// </summary>
        public void ClearMidiInDevice()
        {
            if(GlobalInfo.Instance.DeviceDic!=null && GlobalInfo.Instance.DeviceDic.Count > 0)
            {
                GlobalInfo.Instance.DeviceDic.Clear();
            }
            
        }

        public void StartMidiIn(string deviceKeyName,int deviceIndex)
        {
            DisposeMidIn();
            MidiInDevice = new MidiIn(deviceIndex);
            
            GlobalInfo.Instance.SelectDeviceKeyName = deviceKeyName;
            MidiInDevice.MessageReceived += midiIn_MessageReceived;
            //midiIn.MessageReceived += InputDevice_MessageReceived;
            MidiInDevice.ErrorReceived += midiIn_ErrorReceived;
            MidiInDevice.Start();
        }

        public void DisposeMidIn()
        {
            if (MidiInDevice != null)
            {
                MidiInDevice.Dispose();
                MidiInDevice = null;
            }
        }

        private void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            Debug.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        private void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn)
            {
                var noteEvent = (e.MidiEvent as NoteEvent);
                
                //Debug.WriteLine("MIDI Clock received!");
                // 在这里执行您想要的操作，比如同步节拍
                WriteSequencer(noteEvent);
                Debug.WriteLine(count);
            }
            if (e.MidiEvent.CommandCode == MidiCommandCode.TimingClock)
            {
                count += 1;
                //Debug.WriteLine("MIDI Clock received!");
                // 在这里执行您想要的操作，比如同步节拍
                //ReadSequencer();
                //Debug.WriteLine(count);
            }
            Debug.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        private void ReadSequencer()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 读取Launchpad上的按键输入，写入序列
        /// </summary>
        private void WriteSequencer(NoteEvent noteEvent)
        {
            var noteNumber = noteEvent.NoteNumber;
            var noteName = noteEvent.NoteName;
            if (writeState == 0)
            {
                if (SequenceRec.Instance.RecDic.ContainsKey(noteNumber))
                {
                    writeState = 1;
                    Debug.WriteLine("Just have one");
                }

                SequenceRec.Instance.RecDic.Add(noteNumber, new NoteInfo() { NoteName = noteName,NoteNumber = noteNumber,Sequece = new Dictionary<int, bool>()});
            }
        }

        private static void InputDevice_MessageReceived(object sender, MidiInMessageEventArgs e)
        {

            // 检查消息类型是否为 MIDI 时钟消息
            if (e.MidiEvent.CommandCode == MidiCommandCode.TimingClock)
            {
                Debug.WriteLine("MIDI Clock received!");
                // 在这里执行您想要的操作，比如同步节拍
            }
        }
    }
}
