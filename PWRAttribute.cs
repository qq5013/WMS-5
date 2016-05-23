using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS
{
    /// <summary>
    /// 权限属性
    /// </summary>
    public class PWRAttribute : Attribute
    {
        /// <summary>
        /// 权限的ID
        /// </summary>
        public String Pwrid { get; set; }
        /// <summary>
        /// 权限的描述
        /// </summary>
        public String pwrdes { get; set; }
    }
}