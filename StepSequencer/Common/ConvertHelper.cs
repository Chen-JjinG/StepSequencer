using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace StepSequencer.Common
{
    public class ConvertHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj">需要转换的对象</param>
        /// <param name="exRep">是否需要在异常时抛出异常</param>
        /// <returns></returns>
        public static string ToString(object obj, bool exRep = false)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            try
            {
                return Convert.ToString(obj);
            }
            catch (Exception ex)
            {
                if (exRep)
                {
                    throw ex;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public static int ToInt(object obj, bool exRep = false)
        {
            if (obj == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(obj);
            }
            catch (Exception ex)
            {
                if (exRep)
                {
                    throw ex;
                }
                else
                {
                    return 0;
                }
            }
        }
    }

}