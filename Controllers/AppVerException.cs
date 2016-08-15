using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WMS.Controllers
{
    class AppVerException : Exception
    {
        /// <summary>
        /// 例外说明
        /// </summary>
        public override string Message
        {
            get
            {
                return "APP版本与接口版本不一致，请求失败";
            }
        }
    }
}
