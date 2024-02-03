using StepSequencer.Common;
using StepSequencer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepSequencer.Core
{
    public class SequenceRec : Singleton<SequenceRec>
    {
        /// <summary>
        /// 储存音符的字典
        /// Key：Note ; Value：是否被激活
        /// </summary>
        public Dictionary<int, NoteInfo> RecDic=new Dictionary<int, NoteInfo>();
    }
}
