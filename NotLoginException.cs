using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS
{
    /// <summary>
    /// 未登录例外
    /// </summary>
    public class NotLoginException : Exception
    {
        /// <summary>
        /// 例外说明
        /// </summary>
        public override string Message
        {
            get
            {
                return "尚未登录";
            }
        }
    }

    /// <summary>
    /// 重复提交
    /// </summary>
    public class ReRequestException : Exception
    {
        /// <summary>
        /// 例外说明
        /// </summary>
        public override string Message
        {
            get
            {
                return "请不要重复提交";
            }
        }
    }

    /// <summary>
    /// 没有权限例外
    /// </summary>
    public class HasNonPwrException : Exception
    {
        /// <summary>
        /// 例外说明
        /// </summary>
        public override string Message
        {
            get
            {
                return "没有权限";
            }
        } 
    }
}