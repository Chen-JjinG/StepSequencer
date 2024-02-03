using StepSequencer.Core.MIDIProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepSequencer.Core
{
    public class GlobalInfo
    {
        #region CreateInstance
        private static object _lock = new object();
        private static GlobalInfo _instance;
        public static GlobalInfo Instance
        {
            get
            {
                lock (_lock)
                {
                    if(_instance==null)
                    {
                        _instance = new GlobalInfo();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Variable Def
        //public MidiInInfo MidiInProcess { get;set; }

        private Dictionary<string,int> deviceDic = new Dictionary<string, int>();
        /// <summary>
        /// 设备字典
        /// </summary>
        public Dictionary<string,int> DeviceDic
        {
            get { return deviceDic; }
            set { deviceDic = value; }
        }

        private string selectDeviceKeyName;
        /// <summary>
        /// 选择的设备索引
        /// </summary>
        public string SelectDeviceKeyName
        {
            get { return selectDeviceKeyName; }
            set 
            { 
                selectDeviceKeyName = value;

            }
        }

        #endregion

        private GlobalInfo() 
        {

            //if (deviceDic != null)
            //{
            //    MidiInProcess.MidiInDevice = new NAudio.Midi.MidiIn(deviceDic[selectDeviceKeyName]);
            //}
        }

         
    }
}
