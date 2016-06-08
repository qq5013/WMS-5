using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Transactions;
using WMS.Models;

namespace WMS.Controllers
{    
    /// <summary>
    /// 分货拣货
    /// done:1、得到当天的波次；
    /// done:2、选择波次对应的checi；
    /// done:3、列出该checi对应的商品拣货
    /// done:4、拣货确认时修改wms_cutgds中的ckr, chkflg, chkdat,qty；
    /// done:5、拣货确认时，发现数量小于应拣货数量，要循环扣除对应的stkotdtl里面的qty
    /// </summary>    
    public class CheckedRetrieveController : SsnController
    {
        /// <summary>
        /// 设置模块信息
        /// </summary>
        protected override void SetModuleInfo()
        {

            Mdlid = "CheckedRetrieve";
            Mdldes = "分货拣货模块";
        }
        /// <summary>
        /// 得到当天的波次
        /// </summary>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_分货查询, pwrdes = "分货查询")]
        public ActionResult GetCurrBoci()
        {
            string curdat = GetCurrentDay();
            var qry = from e in WmsDc.wms_cutgds
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      where e.bocidat == curdat
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e1.dptid)
                      group e by new { e.wmsno, e.bllid, e.bocino, e.clsid, e.bocidat } into g
                      select new
                      {
                          wmsno = g.Key.wmsno,
                          bllid = g.Key.bllid,
                          bocino = g.Key.bocino,
                          clsid = g.Key.clsid,
                          bocidat = g.Key.bocidat,
                          checi = from e1 in WmsDc.wms_cutgds
                                  where
                                  e1.wmsno == g.Key.wmsno
                                  && e1.bocino == g.Key.bocino
                                  && e1.bocidat == g.Key.bocidat
                                  && e1.clsid == g.Key.clsid
                                  group e1 by e1.checi.Trim() into g1
                                  select g1.Key,
                          checkedall = g.Count(e => e.chkflg == GetN()) == 0 ? GetY() : GetN()
                      };

            return RSucc("成功", qry.ToArray(), "S0055");
        }


        /// <summary>
        /// 统计信息
        /// </summary>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_分货查询, pwrdes = "分货查询")]
        public ActionResult GetCurrBociSummary(string wmsno)
        {
            string curdat = GetCurrentDay();
            var qry = from e in WmsDc.wms_cutgds
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      where e.bocidat == curdat
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e1.dptid)
                      && e.wmsno == wmsno.Trim()
                      group e by new { e.wmsno, e.bllid, e.bocino, e.clsid, e.bocidat } into g
                      select new
                      {
                          wmsno = g.Key.wmsno,
                          bllid = g.Key.bllid,
                          bocino = g.Key.bocino,
                          clsid = g.Key.clsid,
                          bocidat = g.Key.bocidat,
                          checi = from e1 in WmsDc.wms_cutgds
                                  where
                                  e1.wmsno == g.Key.wmsno
                                  && e1.bocino == g.Key.bocino
                                  && e1.bocidat == g.Key.bocidat
                                  && e1.clsid == g.Key.clsid
                                  group e1 by e1.checi.Trim() into g1
                                  select g1.Key,
                          checkedall = g.Count(e => e.chkflg == GetN()) == 0 ? GetY() : GetN(),
                          unchked = g.Count(e => e.chkflg == GetN()),
                          chked = g.Count(e => e.chkflg == GetY())
                      };

            return RSucc("成功", qry.ToArray(), "S0056");
        }

        /// <summary>
        /// 列出应的商品分货该checi对
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_分货查询, pwrdes = "分货查询")]
        public ActionResult GetCheciByGds(String wmsno, string bllid, String gdsid)
        {
            var qry = from e in WmsDc.wms_cutgds
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      join e4 in WmsDc.emp on e.ckr equals e4.empid
                      into joinEmp
                      from e5 in joinEmp.DefaultIfEmpty()
                      join e2 in
                          WmsDc.v_wms_pkg
                      on new { e.gdsid } equals new { e2.gdsid }
                      into joinPkg
                      from e3 in joinPkg.DefaultIfEmpty()
                      where e.bllid == bllid
                      && e.wmsno == wmsno.Trim()
                      && dpts.Contains(e1.dptid)
                      && e.gdsid == gdsid.Trim()
                      group e by new { e.wmsno, e.bocino, e.bocidat, e.bllid, e.checi, e.gdsid, e1.gdsdes, e1.spc, e1.bsepkg, e3.pkgdes, e3.cnvrto, e.chkflg, e.ckr, e.chkdat, e5.empdes } into g
                      select new
                      {
                          //g.Key.wmsno,g.Key.bocidat,g.Key.bocino,g.Key.bllid,g.Key.checi,g.Key.gdsid
                          g.Key.wmsno,
                          g.Key.bocidat,
                          g.Key.bocino,
                          g.Key.bllid,
                          g.Key.checi,
                          g.Key.gdsid,
                          g.Key.gdsdes,
                          g.Key.spc,
                          g.Key.bsepkg,
                          g.Key.chkflg,
                          g.Key.chkdat,
                          ckrdes = g.Key.empdes,
                          pkg03 = GetPkgStr(g.Sum(ge => ge.qty), g.Key.cnvrto, g.Key.pkgdes),
                          pkg03pre = GetPkgStr(g.Sum(ge => ge.preqty), g.Key.cnvrto, g.Key.pkgdes),
                          qty = g.Sum(ge => ge.qty),
                          preqty = g.Sum(ge => ge.preqty)
                      };
            var arrqry = qry.Distinct().ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0064");
            }

            return RSucc("成功",arrqry, "S0057");
        }

        // done:3、列出该checi对应的商品分货        
        /// <summary>
        /// 列出该checi对应的商品分货
        /// </summary>        
        /// <param name="wmsno"></param>
        /// <param name="bllid"></param>
        /// <param name="bocino"></param>
        /// <param name="clsid"></param>
        /// <param name="checi"></param>
        /// <returns></returns>        
        [PWR(Pwrid = WMSConst.WMS_BACK_分货查询, pwrdes = "分货查询")]
        public ActionResult GetGdsByCheci(String wmsno, String bllid, String bocino, String clsid)
        {
            var qry = from e in WmsDc.wms_cutgds
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      join e2 in
                          WmsDc.v_wms_pkg on new { e.gdsid } equals new { e2.gdsid }
                      into joinPkg
                      from e3 in joinPkg.DefaultIfEmpty()
                      join e4 in WmsDc.emp on e.ckr equals e4.empid
                      into joinEmp from e5 in joinEmp.DefaultIfEmpty()
                      where e.bocino == bocino.Trim() && e.clsid == clsid.Trim()                 
                      && e.bllid == bllid.Trim()
                      && e.savdptid == LoginInfo.DefSavdptid
                      && e.wmsno == wmsno.Trim()
                      && dpts.Contains(e1.dptid)
                      orderby e.chkflg, e.gdsid
                      select new
                      {
                          e.gdsid,
                          e.preqty,
                          e.qty,
                          e1.spc,
                          e1.bsepkg,
                          e.wmsno,
                          e.bocino,
                          e.bocidat,
                          e.bllid,
                          e.checi,
                          e.ckr,
                          ckrdes = e5.empdes.Trim(),
                          e.chkflg,
                          e.chkdat,
                          e1.gdsdes,
                          pkg03 = GetPkgStr(e.qty, e3.cnvrto, e3.pkgdes),
                          pkg03pre = GetPkgStr(e.preqty, e3.cnvrto, e3.pkgdes)
                      };
            var arrqry = qry.Take(20).ToArray();
            if (arrqry.Length < 0)
            {
                return RNoData("N0065");
            }
            return RSucc("成功", arrqry, "S0058");
        }

        // done:4、分货确认时修改wms_cutgds中的ckr, chkflg, chkdat,qty；
        /// <summary>
        /// 确认商品
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="bllid"></param>
        /// <param name="bocino"></param>
        /// <param name="clsid"></param>
        /// <param name="checi"></param>
        /// <param name="gdsid"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_分货确认, pwrdes = "分货确认")]
        public ActionResult BokRetrieve(String wmsno, String bllid, String bocino, String clsid, String checi, String gdsid, double qty)
        {
            //正在生成拣货单，请稍候重试
            //string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0102" );
            //}

            //得到wms_cutgds
            var qry = from e in WmsDc.wms_cutgds
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      where e.bocino == bocino.Trim() && e.clsid == clsid.Trim() && e.checi == checi.Trim()
                      && e.wmsno == wmsno.Trim() && e.bllid == bllid.Trim()
                      && e.gdsid == gdsid.Trim()
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e1.dptid)
                      select e;
            var arrqry = qry.ToArray();            
            if (arrqry.Length < 0)
            {
                return RNoData("N0066");
            }
            wms_cutgds cutgds = arrqry[0];

            //得到对应的stkotdtl里面的未播种，未审核的单据商品信息
            /*var qrystkot = from e in WmsDc.stkotdtl
                           where e.stkot.wmsno == wmsno.Trim()
                           && e.stkot.wmsbllid == bllid.Trim()
                           && e.stkot.chkflg == GetN()
                           && e.stkot.bzflg == GetN()
                           && dpts.Contains(e.stkot.dptid)
                           && e.gdsid == gdsid.Trim()
                           select e;
             */
            var qrystkot = from e in WmsDc.stkot
                           join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                           join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                           join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }
                           join e4 in WmsDc.wms_boci on new { dh = e3.lnkbocino, sndtmd = e3.lnkbocidat, e3.qu } equals new { e4.dh, e4.sndtmd, e4.qu }
                           join e5 in WmsDc.view_pssndgds on new { e4.dh, e4.clsid, e4.sndtmd, e.rcvdptid, e4.qu } equals new { e5.dh, e5.clsid, e5.sndtmd, e5.rcvdptid, e5.qu }
                           join e6 in WmsDc.dpt on e.rcvdptid equals e6.dptid
                           where e.wmsno == wmsno
                           && e.savdptid == LoginInfo.DefSavdptid
                           && dpts.Contains(e.dptid.Trim())
                           && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                           && e5.busid.Trim().Substring(e5.busid.Trim().Length - 1, 1) == checi
                           && e.wmsbllid == bllid.Trim()
                           && e.chkflg == GetN()
                           && e.bzflg == GetN()
                           && e1.gdsid == gdsid.Trim()
                           //&& (e1.bzflg == GetN() || e1.bzflg == null)
                           orderby e1.bzflg, e1.gdsid
                           select e1;
            var arrqrystkot = qrystkot.ToArray();
            if (arrqrystkot.Length <= 0)
            {
                return RNoData("N0067");
            }
            stkotdtl[] stkotdtl = arrqrystkot;
            //得到wms_cang的信息
            var qrycang = from e in WmsDc.wms_cang
                          where e.wmsno == wmsno.Trim()
                          && e.bllid == bllid.Trim()
                          && qus.Contains(e.qu)
                          select e;
            var arrqrycang = qrycang.ToArray();
            if (arrqrycang.Length <= 0)
            {
                return RNoData("N0068");
            }
            wms_cang[] wms_cang = arrqrycang;
            //得到wms_cang的信息
            var qrycangdtl = from e in WmsDc.wms_cangdtl
                          where e.wmsno == wmsno.Trim()
                          && e.bllid == bllid.Trim() 
                          && e.gdsid == gdsid.Trim()                            
                          select e;
            var arrqrycangdtl = qrycangdtl.ToArray();            
            if (arrqrycangdtl.Length <= 0)
            {
                return RNoData("N0069");
            }
            wms_cangdtl[] wms_cangdtl = arrqrycangdtl;

            if (cutgds.chkflg == GetY())
            {
                return RInfo( "I0103" );
            }
            
            // done: 取消对 5、分货确认时，发现数量小于应分货数量，要循环扣除对应的stkotdtl里面的qty 的注释
            if (qty < stkotdtl.Sum(e => e.qty))
            {
                Log.i(LoginInfo.Usrid, Mdlid, wmsno, bllid, Mdldes,
                    gdsid.Trim() + ":应拣:" + Math.Round(stkotdtl.Sum(e => e.qty), 4, MidpointRounding.AwayFromZero)
                    + ";实拣:" + Math.Round(qty, 4, MidpointRounding.AwayFromZero), 
                    "", LoginInfo.DefSavdptid);
                double diff = stkotdtl.Sum(e => e.qty) - qty;

                //扣减stkotdtl里面的库存
                RedcStkotQty(stkotdtl, diff);

            }

            //修改wms_cutgds中的ckr, chkflg, chkdat,qty
            cutgds.ckr = LoginInfo.Usrid;
            cutgds.chkflg = GetY();
            cutgds.qty = qty;
            cutgds.chkdat = GetCurrentDate();
            try
            {                
                WmsDc.SubmitChanges();
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0014");
            }

            return RSucc("成功", null, "S0059");
        }

        /// <summary>
        /// 分货查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="bllid">单据类型</param>
        /// <param name="barcode">仓位编码</param>
        /// <param name="bkr">分货确认人</param>
        /// <param name="gdsid">商品编码</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_分货查询, pwrdes = "分货查询")]
        public ActionResult FindBll(String begindat, String enddat, String barcode, String bllid, String bkr, String gdsid)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo( "I0104",barcode.Trim()  );
            }

            var arrqrymst = FindBllFromCangMst103(begindat, enddat, barcode, bllid, bkr, gdsid);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0070");
            }
            return RSucc("成功", arrqrymst, "S0060");
        }



    }
}
