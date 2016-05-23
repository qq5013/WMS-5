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
        protected ActionResult RInfo(String desc)
        {
            Rm.ResultObject = null;
            return ReturnResult(ResultMessage.RESULTMESSAGE_INFO, desc);
        }
        //返回错误信息
        /// <summary>
        /// 返回错误信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ActionResult RErr(String desc)
        {
            Rm.ResultObject = null;
            String code = ResultMessage.RESULTMESSAGE_ERRORS;
            if (desc.IndexOf("牺牲品") > 0)
            {
                code = ResultMessage.RESULTMESSAGE_DEALTHREAD;
                desc = "数据提交异常，请重新提交";
            }
            return ReturnResult(code, desc);
        }

        protected ActionResult RSucc(String desc, Object obj, Object extObj)
        {
            Rm.ExtObject = extObj;
            return RSucc(desc, obj);
        }

        //返回成功信息
        /// <summary>
        /// 返回成功信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ActionResult RSucc(String desc, Object obj)
        {
            Rm.ResultObject = obj;
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
        protected ActionResult RNoData(String desc)
        {
            Rm.ResultObject = null;
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
        protected ResultMessage RRInfo(String desc)
        {
            Rm.ResultObject = null;
            return RReturnResult(ResultMessage.RESULTMESSAGE_INFO, desc);
        }
        //返回错误信息
        /// <summary>
        /// 返回错误信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ResultMessage RRErr(String desc)
        {
            Rm.ResultObject = null;
            String code = ResultMessage.RESULTMESSAGE_ERRORS;
            if (desc.IndexOf("牺牲品") > 0)
            {
                code = ResultMessage.RESULTMESSAGE_DEALTHREAD;
                desc = "数据提交异常，请重新提交";
            }
            return RReturnResult(code, desc);
        }

        protected ResultMessage RRSucc(String desc, Object obj, Object extObj)
        {
            Rm.ExtObject = extObj;
            return RRSucc(desc, obj);
        }

        //返回成功信息
        /// <summary>
        /// 返回成功信息
        /// </summary>
        /// <param name="desc">描述</param>
        /// <returns></returns>
        protected ResultMessage RRSucc(String desc, Object obj)
        {
            Rm.ResultObject = obj;
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
        protected ResultMessage RRNoData(String desc)
        {
            Rm.ResultObject = null;
            return RReturnResult(ResultMessage.RESULTMESSAGE_NODATA, desc);

        }
        #endregion 返回函数 返回ResultMessage
    }
}

