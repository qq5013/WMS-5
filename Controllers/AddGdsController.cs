using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;

namespace WMS.Controllers
{
    /// <summary>
    /// 补货模块
    /// </summary>
    public class AddGdsController : SsnController
    {
        protected override void SetModuleInfo()
        {
            this.Mdlid = "AddGds";
            this.Mdldes = "补货模块";
        }
        
        /// <summary>
        /// 获取当天的补货模块
        /// </summary>
        /// <returns></returns>
        public ActionResult GetCurDayBll()
        {
            var qry = from e in WmsDc.wms_addgds
                      where e.mkedat.Substring(0,8) == GetCurrentDay()
                      && e.bllid == WMSConst.BLL_TYPE_ADDGDSBLL
                      && qus.Contains(e.qu)
                      && savdpts.Contains(e.savdptid)
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length == 0)
            {
                return RNoData("未查找到当天数据");
            }

            return RSucc("成功", arrqry);
        }

        /// <summary>
        /// 得到单号对应的明细信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        public ActionResult GetBlldtl(String wmsno)
        {
            var qry = from e in WmsDc.wms_addgds
                      join e1 in WmsDc.wms_addgdsdtl on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.v_wms_pkg on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.gds on e1.gdsid equals e3.gdsid
                      where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADDGDSBLL
                      && qus.Contains(e.qu)
                      && savdpts.Contains(e.savdptid)
                      select new
                      {
                          e1.wmsno,
                          e1.bllid,
                          e1.rcdidx,
                          e1.gdsid,
                          e1.lowqty,
                          e1.safeqty,
                          e1.qty,
                          e1.qtyper,
                          e1.safeflg,
                          gdsdes = e3.gdsdes.Trim(),
                          e3.spc,
                          e3.bsepkg,
                          pkg03 = GetPkgStr(e1.qty, e2.cnvrto, e2.pkgdes),
                          pkg03pre = GetPkgStr(e1.qty, e2.cnvrto, e2.pkgdes)
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length == 0)
            {
                return RNoData("未查找到当天数据");
            }
            return RSucc("成功", arrqry);
        }

        /// <summary>
        /// 增加补货明细
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="rcdidx"></param>
        /// <param name="gdsid"></param>
        /// <param name="gdstype"></param>
        /// <param name="outbarcode"></param>
        /// <param name="outqty"></param>
        /// <param name="inbarcode"></param>
        /// <param name="inqty"></param>
        /// <returns></returns>
        public ActionResult AddAdj(String wmsno, int rcdidx, String gdsid, String gdstype, String outbarcode, double outqty, String inbarcode, double inqty)
        {
            var qry = from e in WmsDc.wms_addgds
                      join e1 in WmsDc.wms_addgdsdtl on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.v_wms_pkg on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.gds on e1.gdsid equals e3.gdsid
                      where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADDGDSBLL
                      && qus.Contains(e.qu)
                      && savdpts.Contains(e.savdptid)
                      select new
                      {
                          e1.wmsno,
                          e1.bllid,
                          e1.rcdidx,
                          e1.gdsid,
                          e1.lowqty,
                          e1.safeqty,
                          e1.qty,
                          e1.qtyper,
                          e1.safeflg,
                          gdsdes = e3.gdsdes.Trim(),
                          e3.spc,
                          e3.bsepkg,
                          pkg03 = GetPkgStr(e1.qty, e2.cnvrto, e2.pkgdes),
                          pkg03pre = GetPkgStr(e1.qty, e2.cnvrto, e2.pkgdes)
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length == 0)
            {
                return RNoData("未查找到当天数据");
            }
            // 未找到商品信息
            if(!arrqry.Where(e=>e.rcdidx==rcdidx&&e.wmsno==wmsno.Trim()&&e.gdsid==gdsid.Trim()).Any()){
                return RNoData("未找到该单据该商品的信息");
            }
            // 插入新的信息
            wms_addgdsadj aga = new wms_addgdsadj();

            return null;
        }

    }
}
