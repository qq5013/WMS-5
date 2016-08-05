using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WMS.Models;

namespace WMS
{

    /// <summary>
    /// 日志类
    /// </summary>
    public class Log
    {
        /// <summary>
        /// 数据上下文
        /// </summary>
        public WMSDcDataContext WmsDc { set; get; }
        /// <summary>
        /// 记录人
        /// </summary>
        public String man { get; set; }
        /// <summary>
        /// 模块名称
        /// </summary>
        public String mdlid { get; set; }

        /// <summary>
        /// 日志记录
        /// </summary>
        /// <param name="man"></param>
        /// <param name="mdlid"></param>
        /// <param name="wmsno"></param>
        /// <param name="bllid"></param>
        /// <param name="actid"></param>
        /// <param name="brief"></param>
        /// <param name="qu"></param>
        /// <param name="savdptid"></param>
        public void i( String man, String mdlid, String wmsno, String bllid, String actid, String brief, String qu, String savdptid){
            wms_log log = new wms_log();
            log.brief = brief;
            log.logact = actid;
            log.logdat = DateTime.Now.ToString("yyyyMMddHHmmss");
            log.logman = man;            
            log.logmdl = "[PDA]"+ (mdlid.Length>=5 ? mdlid.Substring(0,5) : mdlid );
            log.qu = qu;
            log.savdptid = savdptid;
            log.wmsno = wmsno;
            log.bllid = bllid;

            WmsDc.wms_log.InsertOnSubmit(log);            
        }
    }
}