using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WMS.Controllers
{
    class CanNotOverQuAdjustBarcodeException : Exception
    {
        /// <summary>
        /// 例外说明
        /// </summary>
        public override string Message
        {
            get
            {
                return "未启用跨区调整功能";
            }
        }
    }
}
