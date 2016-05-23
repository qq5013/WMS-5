using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS
{
    /// <summary>
    /// 返回统一对象
    /// </summary>
    public class ResultMessage
    {
        /// <summary>
        /// 返回成功
        /// </summary>
        public static String RESULTMESSAGE_SUCCESS = "0000";
        /// <summary>
        /// 返回警告
        /// </summary>
        public static String RESULTMESSAGE_INFO = "0001";
        /// <summary>
        /// 返回错误
        /// </summary>
        public static String RESULTMESSAGE_ERRORS = "0002";
        /// <summary>
        /// 返回未登录
        /// </summary>
        public static String RESULTMESSAGE_NOTLOGIN = "0003";
        /// <summary>
        /// 未找到数据
        /// </summary>
        public static String RESULTMESSAGE_NODATA = "0004";
        /// <summary>
        /// 牺牲品
        /// </summary>
        public static String RESULTMESSAGE_DEALTHREAD = "0005";

        /// <summary>
        /// 返回码
        /// </summary>
        private String resultCode;

        /// <summary>
        /// 返回码
        /// </summary>
        public String ResultCode
        {
            get { return resultCode; }
            set { resultCode = value; }
        }

        /// <summary>
        /// 返回信息描述
        /// </summary>
        private String resultDesc;

        /// <summary>
        /// 返回码描述
        /// </summary>
        public String ResultDesc
        {
            get { return resultDesc; }
            set { resultDesc = value; }
        }

        /// <summary>
        /// 分页信息
        /// </summary>
        private Pagination paginationObj;

        public Pagination PaginationObj
        {
            get { return paginationObj; }
            set { paginationObj = value; }
        }

        /// <summary>
        /// 返回的对象
        /// </summary>
        private Object resultObject;
        /// <summary>
        /// 返回的对象
        /// </summary>
        public Object ResultObject
        {
            get { return resultObject; }
            set { resultObject = value; }
        }

        /// <summary>
        /// 扩展返回对象
        /// </summary>
        private Object extObject;
        /// <summary>
        /// 扩展返回对象
        /// </summary>
        public Object ExtObject
        {
            get { return extObject; }
            set { extObject = value; }
        }

    }

    public class Pagination
    {
        private int recordCount;

        public int RecordCount
        {
            get { return recordCount; }
            set { recordCount = value; }
        }
        private int pageCount;

        public int PageCount
        {
            get { return pageCount; }
            set { pageCount = value; }
        }
        private int pageid;

        public int Pageid
        {
            get { return pageid; }
            set { pageid = value; }
        }

        private int pageSize;

        public int PageSize
        {
            get { return pageSize; }
            set { pageSize = value; }
        }


    }
}