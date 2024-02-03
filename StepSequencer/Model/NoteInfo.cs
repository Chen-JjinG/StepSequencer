using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepSequencer.Model
{
    public class NoteInfo
    {
        private string noteName;
        /// <summary>
        /// 音名
        /// </summary>
        public string NoteName
        {
            get { return noteName; }
            set { noteName = value; }
        }

        private int noteNumber;
        /// <summary>
        /// 对应的编码，中央C默认是60
        /// </summary>
        public int NoteNumber
        {
            get { return noteNumber; }
            set { noteNumber = value; }
        }

        private Dictionary<int, bool> sequece;
        /// <summary>
        /// 对应音符下的序列
        /// </summary>
        public Dictionary<int, bool> Sequece
        {
            get { return sequece; }
            set { sequece = value; }
        }
    }
}
