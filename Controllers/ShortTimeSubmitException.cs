using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WMS.Controllers
{
    class ShortTimeSubmitException : Exception
    {
        /// <summary>
        /// 例外说明
        /// </summary>
        public override string Message
        {
            get
            {
                return "请不要短时间重复提交请求";
            }
        } 
    }
}
