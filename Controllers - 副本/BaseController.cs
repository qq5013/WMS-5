using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;
using System.Web.Routing;
using WMS;
using WMS.Common;
using System.Transactions;

namespace WMS.Controllers
{
    /// <summary>
    /// 业务基础类
    /// </summary>
    [HandleError]
    public class BaseController : Controller
    {
        protected String Flg = "ny";
        public Char GetY()
        {
            //return Flg.Substring(0, 1);
            return Flg.ToArray()[1];
        }
        public Char GetN()
        {
            //return Flg.Substring(1);
            return Flg.ToArray()[0];
        }

        protected String GetPkgStr(double? qty, double? cvt, String pkgdes)
        {
            if (qty == null)
            {
                return "0件+0";
            }
            if (cvt == null)
            {
                return "0件+" + qty.Value;
            }
            double yu = qty.Value % cvt.Value;
            int dv = (int)(qty / cvt);
            String rtn = dv + pkgdes + "+" + yu;
            return rtn;
        }
        /// <summary>
        /// 数据操作上下文
        /// </summary>
        protected WMSDcDataContext WmsDc = null;
        /// <summary>
        /// 请求结果描述
        /// </summary>
        protected ResultMessage Rm = null;
        /// <summary>
        /// 用户ID
        /// </summary>
        protected String UsrId = null;
        /// <summary>
        /// 用户登录信息
        /// </summary>
        protected LoginInfo LoginInfo = null;
        /// <summary>
        /// 模块ID
        /// </summary>
        protected String Mdlid = null;
        /// <summary>
        /// 模块名称
        /// </summary>
        protected String Mdldes = null;
        /// <summary>
        /// 日志操作对象
        /// </summary>
        protected Log Log = null;

        protected wms_set[] set996 = null;

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 初始化Controller
        /// </summary>
        /// <param name="requestContext"></param>
        private void Init(RequestContext requestContext)
        {
            //1.初始化变量
            WmsDc = new WMSDcDataContext();
            
            Rm = new ResultMessage();
            Rm.ResultCode = ResultMessage.RESULTMESSAGE_SUCCESS;
            Rm.ResultDesc = "成功！";
            
            //2.初始化分页信息
            Pagination pgnt = new Pagination();
            pgnt.PageCount = 0;
            pgnt.Pageid = 1;
            pgnt.PageSize = 20;
            pgnt.RecordCount = 0;
            Rm.PaginationObj = pgnt;

            set996 = (from e in WmsDc.wms_set
                      where e.setid == "996"
                      && e.isvld == 'y'
                      select e).ToArray();
        }

        protected string GetStoreidBySavdptid(String savdptid)
        {
            var qry = from e in WmsDc.wms_set
                      where e.setid == "008" && e.isvld == 'y'
                      && e.val1 == savdptid.Trim()
                      select e;
            wms_set st = qry.FirstOrDefault();
            if (st != null)
            {
                return st.val3.Trim();
            }
            return null;
        }

        protected String GetCsQuByDptid(String dptid, String storeid)
        {
            var qry = from e in WmsDc.wms_set
                      join e1 in WmsDc.wms_set on new { qu = e.val1, savdptid = e.val3, setid = "006" } equals new { qu = e1.val1, savdptid = e1.val3, e1.setid }
                      join e2 in WmsDc.wms_set on new { savdptid = e1.val3, setid = "008" } equals new { savdptid = e2.val1, e2.setid }
                      where e.setid == "001" && e2.val3 == storeid.Trim()
                      && e.val2 == dptid.Trim()
                      && e.isvld == 'y' && e1.isvld == 'y' && e2.isvld == 'y'
                      && e1.val2 == "3"
                      select e.val1.Trim();
            string[] arrQry = qry.ToArray();
            if (arrQry.Length > 0)
            {
                return arrQry.FirstOrDefault();
            }
            return null;
        }

        protected String GetQuByDptid(String dptid, String storeid)
        {
            var qry = from e in WmsDc.wms_set
                      join e1 in WmsDc.wms_set on new { qu = e.val1, savdptid = e.val3, setid = "006" } equals new { qu = e1.val1, savdptid = e1.val3, e1.setid }
                      join e2 in WmsDc.wms_set on new { savdptid = e1.val3, setid = "008" } equals new { savdptid = e2.val1, e2.setid }
                      where e.setid == "001" && e2.val3 == storeid.Trim()
                      && e.val2 == dptid.Trim()
                      && (e1.val2 == "7")
                      && e.isvld == 'y' && e1.isvld == 'y' && e2.isvld == 'y'
                      select e.val1.Trim();
            string[] arrQry = qry.ToArray();
            if (arrQry.Length > 0)
            {
                return arrQry.FirstOrDefault();
            }
            return null;
        }

        protected String GetDtQuByDptid(String dptid, String storeid)
        {
            var qry = from e in WmsDc.wms_set
                      join e1 in WmsDc.wms_set on new { qu = e.val1, savdptid = e.val3, setid = "006" } equals new { qu = e1.val1, savdptid = e1.val3, e1.setid }
                      join e2 in WmsDc.wms_set on new { savdptid = e1.val3, setid = "008" } equals new { savdptid = e2.val1, e2.setid }
                      where e.setid == "001" && e2.val3 == storeid.Trim()
                      && e.val2 == dptid.Trim()
                      && (e1.val2 == "5")
                      && e.isvld == 'y' && e1.isvld == 'y' && e2.isvld == 'y'
                      select e.val1.Trim();
            string[] arrQry = qry.ToArray();
            if (arrQry.Length > 0)
            {
                return arrQry.FirstOrDefault();
            }
            return null;
        }



        protected String GetDescByCode(string code)
        {
            var qry = from e in set996
                      where e.setid.Trim() == "996"
                      && e.isvld == 'y'
                      && e.val1.Trim() == code
                      select e;
            wms_set st = qry.FirstOrDefault();
            if (st != null)
            {
                return st.brief.Trim();
            }
            return "{0}";
        }

        /// <summary>
        /// 初始化Initialize
        /// </summary>
        /// <param name="requestContext"></param>
        protected override void Initialize(RequestContext requestContext)
        {
            Init(requestContext);
            base.Initialize(requestContext);            
        }

        #region 返回函数 返回ActionResult
        /// <summary>
        /// 公共返回函数
        /// </summary>
        /// <returns></returns>
        protected ActionResult ReturnResult(){
            return Json(Rm, "application/json", JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 公共返回函数
        /// </summary>
        /// <returns></returns>
        protected ActionResult ReturnResult(String code, String desc)
        {
            Rm.ResultCode = code;
            Rm.ResultDesc = desc;
            return ReturnResult();
        }
        //返回警告信息
        /// <summary>
        /// 返回警告信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ActionResult RInfo(string code, params object[] parms)
        {
            String desc = "";
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, parms);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            return ReturnResult(ResultMessage.RESULTMESSAGE_INFO, desc);
        }
        //返回警告信息
        /// <summary>
        /// 返回警告信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ActionResult RInfo( string code)
        {
            string desc = "";
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            return ReturnResult(ResultMessage.RESULTMESSAGE_INFO, desc);
        }
        //返回错误信息
        /// <summary>
        /// 返回错误信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ActionResult RErr(String desc, String code)
        {
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            String code1 = ResultMessage.RESULTMESSAGE_ERRORS;
            if (desc.IndexOf("牺牲品") > 0)
            {
                code1 = ResultMessage.RESULTMESSAGE_DEALTHREAD;
                desc = "数据提交异常，请重新提交";
            }
            return ReturnResult(code, desc);
        }

        protected ActionResult RSucc(String desc, Object obj, Object extObj, string code)
        {
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ExtObject = extObj;
            Rm.ExtCode = code;
            return RSucc(desc, obj, "S0033");
        }

        //返回成功信息
        /// <summary>
        /// 返回成功信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ActionResult RSucc(String desc, Object obj, string code)
        {
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ResultObject = obj;
            Rm.ExtCode = code;
            if (obj!=null && obj.GetType() == typeof(Array))
            {
                Array arr = (Array)obj;
                if (arr.Length==0)
                {
                    return ReturnResult(ResultMessage.RESULTMESSAGE_NODATA, "未找到数据");
                }
            }
            return ReturnResult(ResultMessage.RESULTMESSAGE_SUCCESS, desc);
        }
        //返回未找到信息
        /// <summary>
        /// 返回未找到信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ActionResult RNoData(string code, params string[] parms)
        {
            string desc = "";
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, parms);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            return ReturnResult(ResultMessage.RESULTMESSAGE_NODATA, desc);

        }
        //返回未找到信息
        /// <summary>
        /// 返回未找到信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ActionResult RNoData(string code)
        {
            string desc = "";
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            return ReturnResult(ResultMessage.RESULTMESSAGE_NODATA, desc);

        }
        #endregion  返回函数 返回ActionResult


        #region  返回函数 返回ResultMessage
        /// <summary>
        /// 公共返回函数
        /// </summary>
        /// <returns></returns>
        protected ResultMessage RReturnResult()
        {
            return Rm;
        }
        /// <summary>
        /// 公共返回函数
        /// </summary>
        /// <returns></returns>
        protected ResultMessage RReturnResult(String code, String desc)
        {            
            Rm.ResultCode = code;
            Rm.ResultDesc = desc;
            return RReturnResult();
        }
        //返回警告信息
        /// <summary>
        /// 返回警告信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ResultMessage RRInfo(string code, params object[] parms)
        {
            string desc = "";
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, parms);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            return RReturnResult(ResultMessage.RESULTMESSAGE_INFO, desc);
        }
        //返回警告信息
        /// <summary>
        /// 返回警告信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ResultMessage RRInfo( string code)
        {
            string desc = "";
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            return RReturnResult(ResultMessage.RESULTMESSAGE_INFO, desc);
        }
        //返回错误信息
        /// <summary>
        /// 返回错误信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ResultMessage RRErr(String desc, String code)
        {
           
            Rm.ResultObject = null;
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ExtCode = code;
            String code1 = ResultMessage.RESULTMESSAGE_ERRORS;
            if (desc.IndexOf("牺牲品") > 0)
            {
                code1 = ResultMessage.RESULTMESSAGE_DEALTHREAD;
                desc = "数据提交异常，请重新提交";
            }
            return RReturnResult(code1, desc);
        }

        protected ResultMessage RRSucc(String desc, Object obj, Object extObj, string code)
        {
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ExtObject = extObj;
            Rm.ExtCode = code;
            return RRSucc(desc, obj, "S0036");
        }

        //返回成功信息
        /// <summary>
        /// 返回成功信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ResultMessage RRSucc(String desc, Object obj, string code)
        {
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ResultObject = obj;
            Rm.ExtCode = code;
            if (obj != null && obj.GetType() == typeof(Array))
            {
                Array arr = (Array)obj;
                if (arr.Length == 0)
                {
                    return RReturnResult(ResultMessage.RESULTMESSAGE_NODATA, "未找到数据");
                }
            }
            return RReturnResult(ResultMessage.RESULTMESSAGE_SUCCESS, desc);
        }

        //返回未找到信息
        /// <summary>
        /// 返回未找到信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ResultMessage RRNoData(string code, params string[] parms)
        {
            string desc = "";
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, parms);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            return RReturnResult(ResultMessage.RESULTMESSAGE_NODATA, desc);

        }

        //返回未找到信息
        /// <summary>
        /// 返回未找到信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ResultMessage RRNoData(string code)
        {
            string desc = "";
            string desc1 = GetDescByCode(code);
            desc = string.Format(desc1, desc);
            Rm.ResultObject = null;
            Rm.ExtCode = code;
            return RReturnResult(ResultMessage.RESULTMESSAGE_NODATA, desc);

        }
        #endregion 返回函数 返回ResultMessage
    }
}

