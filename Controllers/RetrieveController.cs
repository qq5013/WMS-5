using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using WMS.Models;
using System.Transactions;

namespace WMS.Controllers
{
    /// <summary>
    /// 捡货单模块    
    /// </summary>
    public class RetrieveController : SsnController
    {

        /// <summary>
        /// 得到当前日期的拣货单
        /// </summary>
        /// <param name="bdat">开始时间</param>
        /// <param name="edat">结束时间</param>
        /// <param name="lnkbllid">连接订单（为空/206分店拣货单、501外销单）</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_拣货查询, pwrdes = "拣货查询")]
        public ActionResult GetRetrieveBll(String bdat, String edat, String lnkbllid)
        {
            if (lnkbllid == null)
            {
                lnkbllid = "206";
            }

            if (bdat == null)
            {
                bdat = GetCurrentDay();
            }
            if(edat ==null){
                edat = GetNextDay();
            }

            //分店拣货单
            var qry = from e in WmsDc.wms_cang
                      where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                      && e.lnkbllid == lnkbllid
                      && ( e.savdptid == LoginInfo.DefSavdptid || e.savdptid==LoginInfo.DefCsSavdptid)
                      && e.mkedat.CompareTo(bdat)>=0 && e.mkedat.CompareTo(edat)<0
                      && qus.Contains(e.qu.Trim())
                      && !(from ee in WmsDc.wms_bzcnv where ee.lnkbllid == "207" && ee.wmsbllid == "103" && ee.wmsno == e.wmsno select ee).Any()    //不在分货拣货中
                      select new
                      {
                          e.bllid,
                          e.brief,
                          e.chkdat,
                          e.chkflg,
                          e.ckr,
                          e.lnkbllid,
                          e.lnkbocidat,
                          e.lnkbocino,
                          e.lnkbrief,
                          e.lnkno,
                          e.mkedat,
                          e.mkr,
                          e.opr,
                          e.prvid,
                          e.qu,
                          e.rcvdptid,
                          e.savdptid,
                          e.times,
                          e.wmsno,
                          prvdes = ""
                      };
            if (lnkbllid == "501")
            {
                //外销单
                qry = from e in WmsDc.wms_cang
                      join e1 in WmsDc.cus on e.prvid equals e1.cusid
                      where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                      && e.lnkbllid == lnkbllid
                       && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                       && e.mkedat.CompareTo(bdat) >= 0 && e.mkedat.CompareTo(edat) < 0
                      && qus.Contains(e.qu.Trim())
                      select new
                      {
                          e.bllid,
                          e.brief,
                          e.chkdat,
                          e.chkflg,
                          e.ckr,
                          e.lnkbllid,
                          e.lnkbocidat,
                          e.lnkbocino,
                          e.lnkbrief,
                          e.lnkno,
                          e.mkedat,
                          e.mkr,
                          e.opr,
                          e.prvid,
                          e.qu,
                          e.rcvdptid,
                          e.savdptid,
                          e.times,
                          e.wmsno,
                          prvdes = e1.cusdes
                      };
            }

            var arrqry = qry.ToArray();
            if(arrqry.Length<=0){
                return RNoData("N0169");
            }
            return RSucc("成功", arrqry, "S0159") ;
        }

        

        /// <summary>
        /// 得到拣货单明细
        /// </summary>
        /// <param name="wmsno">拣货单号</param>
        /// <param name="lnkbllid">连接订单（为空/206分店拣货单、501外销单）</param>
        /// <param name="parity">奇偶(all, true=奇, false=偶)</param>
        /// <param name="channel">通道</param>
        /// <param name="ceng">层(all, true=高层, false=低层)</param>
        /// <param name="sxjx">升序降序(true, false)</param>        
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_拣货查询, pwrdes = "拣货查询")]
        public ActionResult GetRetriveBllDtl(String wmsno, String lnkbllid, string parity, string channel, string ceng, string sxjx, string barcode, string gdsid)
        {
            if (lnkbllid == null)
            {
                lnkbllid = "206";
            }
            var qus = (from e in LoginInfo.DatPwrs
                       select e.qu).ToArray();
            //分店拣货单
            var qry = from e in WmsDc.wms_cang
                      where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                      && e.lnkbllid == lnkbllid
                       && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && qus.Contains(e.qu.Trim())
                      && e.wmsno == wmsno
                      select new
                      {
                          e.bllid,
                          e.brief,
                          e.chkdat,
                          e.chkflg,
                          e.ckr,
                          e.lnkbllid,
                          e.lnkbocidat,
                          e.lnkbocino,
                          e.lnkbrief,
                          e.lnkno,
                          e.mkedat,
                          e.mkr,
                          e.opr,
                          e.prvid,
                          e.qu,
                          e.rcvdptid,
                          e.savdptid,
                          e.times,
                          e.wmsno,
                          prvdes = ""
                      };
            if (lnkbllid == "501")
            {
                //外销单
                qry = from e in WmsDc.wms_cang
                      join e1 in WmsDc.cus on e.prvid equals e1.cusid
                           where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                           && e.lnkbllid == lnkbllid
                            && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                           && qus.Contains(e.qu.Trim())
                           && e.wmsno == wmsno
                           select new
                           {
                               e.bllid,
                               e.brief,
                               e.chkdat,
                               e.chkflg,
                               e.ckr,
                               e.lnkbllid,
                               e.lnkbocidat,
                               e.lnkbocino,
                               e.lnkbrief,
                               e.lnkno,
                               e.mkedat,
                               e.mkr,
                               e.opr,
                               e.prvid,
                               e.qu,
                               e.rcvdptid,
                               e.savdptid,
                               e.times,
                               e.wmsno,
                               prvdes = e1.cusdes
                           };
            }
           
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0170", wmsno);
            }
            var qrydtl1 = from e in WmsDc.wms_cangdtl
                         join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                         join e2 in
                             WmsDc.wms_pkg on new { e1.gdsid } equals new { e2.gdsid }
                         into joinPkg from e3 in joinPkg.DefaultIfEmpty()
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                         select new
                         {
                             e.barcode,
                             e.bcd,
                             e.bkr,
                             e.bllid,
                             e.bokdat,
                             e.bokflg,
                             e.bthno,
                             e.gdsid,
                             e.gdstype,
                             e.oldbarcode,
                             e.pkgid,
                             e.pkgqty,
                             e.preqty,
                             qty = Math.Round(e.qty, 4, MidpointRounding.AwayFromZero),
                             e.rcdidx,
                             e.tpcode,
                             e.vlddat,
                             e.wmsno,
                             e1.gdsdes,
                             e1.spc,
                             e1.bsepkg,
                             e3.cnvrto,
                             pkgdes = e3.pkgdes.Trim(),
                             pkg03 = GetPkgStr(Math.Round(e.qty, 4, MidpointRounding.AwayFromZero),e3.cnvrto,e3.pkgdes),
                             pkg03pre = GetPkgStr(Math.Round(e.preqty.Value, 4, MidpointRounding.AwayFromZero), e3.cnvrto, e3.pkgdes)
                         };            
            //如果是拣货单，需要判断tpcode==GetY(),表示不能修改
            //if (lnkbllid == "206")
            //{
            qrydtl1 = qrydtl1.Where(e => e.tpcode.ToLower() == "y");
            //}

            


            var qrydtl = qrydtl1;

            //得到未拣货通道数目
            var arrUnRetrieveTongdao = (from e in qrydtl
                                        join e1 in WmsDc.wms_cangwei on e.barcode equals e1.barcode
                                        where e.bokflg == GetN()
                                        group e1 by e1.tongdao into g
                                        select g.Key);


            //barode是否为空
            if (!string.IsNullOrEmpty(barcode))
            {
                qrydtl = qrydtl.Where(e => e.barcode == barcode.Trim());
            }
            //gdsid是否为空
            if (!string.IsNullOrEmpty(barcode) && !string.IsNullOrEmpty(gdsid))
            {
                qrydtl = qrydtl.Where(e => e.gdsid == gdsid.Trim());
                if (qrydtl.Where(e => e.bokflg == GetN() && e.tpcode == "y").Count() == 0)
                {
                    //看看是哪个审核的
                    string[] whoAdt = (from e in WmsDc.wms_cangdtl
                                       join e1 in WmsDc.emp on e.bkr equals e1.empid
                                       where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                                       && e.gdsid == gdsid.Trim()
                                       && e.barcode == barcode.Trim()
                                           //&& e.gdstype == gdstype                                    
                                       && e.wmsno == wmsno.Trim()
                                       && e.bokflg == GetY()
                                       select e1.empdes).ToArray();
                    if (whoAdt.Count() > 0)
                    {
                        return RInfo("I0125", string.Join(",", whoAdt));
                    }
                }
            }

            //判断奇偶
            if (!string.IsNullOrEmpty(parity))
            {
                if (parity.Trim().ToLower() == "true") //奇
                {
                    qrydtl = qrydtl.Where(e => Convert.ToInt32(e.barcode.Substring(4, 5)) % 2 == 1);
                }
                else if (parity.Trim().ToLower() == "false")    // 偶
                {
                    qrydtl = qrydtl.Where(e => Convert.ToInt32(e.barcode.Substring(4, 5)) % 2 == 0);
                }
            }


            //判断通道
            if (!string.IsNullOrEmpty(channel))
            {                
                qrydtl = qrydtl.Where(e => e.barcode.Substring(2, 2) == channel.Trim());
            }

            //判断层
            if (!string.IsNullOrEmpty(ceng))
            {
                if (ceng.Trim().ToLower() == "true") //高层
                {
                    qrydtl = from e in qrydtl
                             join e1 in WmsDc.wms_cangwei on e.barcode equals e1.barcode
                             where e1.tjflg == GetY()
                             select e;                    
                }
                else if (ceng.Trim().ToLower() == "false") //低层 
                {
                    qrydtl = from e in qrydtl
                             join e1 in WmsDc.wms_cangwei on e.barcode equals e1.barcode
                             where e1.tjflg == GetN()
                             select e;               
                }
            }

            //得到未拣货总数
            int unRetrieve = (from e in qrydtl
                              where e.bokflg == GetN()
                              select e).Count();

            //得到已拣货总数
            int hasRetrieved = (from e in qrydtl
                                where e.bokflg == GetY()
                                select e).Count();

            //判断升序还是降序
            if (!string.IsNullOrEmpty(sxjx))
            {
                if (sxjx == "true")  //升序
                {
                    qrydtl = qrydtl
                            .OrderBy(e => e.barcode);
                }
                else if (sxjx == "false")
                {
                    qrydtl = qrydtl
                            .OrderByDescending(e => e.barcode);
                }
                qrydtl = qrydtl.Where(e => e.bokflg == GetN()).Take(20);
            }

            var qrydtl2 = from e in qrydtl
                          group e by new { e.barcode, e.bkr, e.bllid, e.bokflg, e.bsepkg, e.cnvrto, e.gdsdes, e.gdsid, e.pkgdes, e.pkgid, e.spc, e.wmsno }
                              into g
                              select new
                              {
                                  g.Key.barcode,
                                  bcd = "",
                                  g.Key.bkr,
                                  g.Key.bllid,
                                  g.Key.bokflg,
                                  g.Key.bsepkg,
                                  g.Key.cnvrto,
                                  g.Key.gdsdes,
                                  g.Key.gdsid,
                                  g.Key.pkgdes,
                                  g.Key.pkgid,
                                  pkgqty = g.Sum(ee => ee.pkgqty),
                                  g.Key.spc,
                                  g.Key.wmsno,
                                  oldbarcode = "",
                                  rcdidx = 0,
                                  tpcode = "",
                                  vlddat = "",
                                  gdstype = "",
                                  pkg03 = g.Sum(ee => ee.qty),
                                  prepkg03 = g.Sum(ee => ee.preqty),
                                  qty = g.Sum(ee => ee.qty),
                                  preqty = g.Sum(ee => ee.preqty)
                              };

            
            


            var arrqrydtl = qrydtl.ToArray();
            if (arrqrydtl.Length <= 0)
            {
                string p = "";
                foreach (string k in Request.Params.Keys)
                {
                    p += k + Request[k] + "&";
                }
                i(wmsno, WMSConst.BLL_TYPE_RETRIEVE, p, "N0171", "", LoginInfo.DefSavdptid);
                return RNoData("N0171");  // 该拣货单未找到符合条件的明细信息
            }

            if (!string.IsNullOrEmpty(barcode))
            {
                JsonResult jr = (JsonResult)GetCurrdayAllRetrieveByBarcode(barcode, gdsid);
                ResultMessage rm = (ResultMessage)jr.Data;
                if (rm.ResultCode == ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return RSucc("成功", arrqrydtl, new
                    {
                        UnRetrieveTongdao = arrUnRetrieveTongdao,
                        UnRetrieveCount = unRetrieve,
                        HasRetrieved = hasRetrieved,
                        UnRetrieveBarcodeGds = rm.ResultObject
                    }, "S0173");
                }
                else
                {
                    return RSucc("成功", arrqrydtl, new
                    {
                        UnRetrieveTongdao = arrUnRetrieveTongdao,
                        UnRetrieveCount = unRetrieve,
                        HasRetrieved = hasRetrieved
                    }, "S0173");
                }
            }

            return RSucc("成功", arrqrydtl, new
            {
                UnRetrieveTongdao = arrUnRetrieveTongdao,
                UnRetrieveCount = unRetrieve,
                HasRetrieved = hasRetrieved
            }, "S0160");
        }

        public ActionResult DoDecStkQty(string wmsno, string checi, string bllid, string gdsid, string bthno, string vlddat, double qty)
        {
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
                return RNoData("N0173");
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
                return RNoData("N0174");
            }
            wms_cang[] wms_cang = arrqrycang;
            //得到wms_cang的信息
            var qrycangdtl = from e in WmsDc.wms_cangdtl
                             where e.wmsno == wmsno.Trim()
                             && e.bllid == bllid.Trim()
                             && e.gdsid == gdsid.Trim()
                             && e.vlddat == vlddat.Trim()
                             && e.bthno == bthno.Trim()
                             select e;
            var arrqrycangdtl = qrycangdtl.ToArray();
            if (arrqrycangdtl.Length <= 0)
            {
                return RNoData("N0175");
            }
            wms_cangdtl[] wms_cangdtl = arrqrycangdtl;            

            // done: 取消对 5、分货确认时，发现数量小于应分货数量，要循环扣除对应的stkotdtl里面的qty 的注释

            if (qty < stkotdtl.Sum(e => e.qty))
            {
                Log.i(LoginInfo.Usrid, Mdlid, wmsno, bllid, Mdldes,
                    gdsid.Trim() + ":应拣:" + Math.Round(stkotdtl.Sum(e => e.qty), 4, MidpointRounding.AwayFromZero)
                    + ";实拣:" + Math.Round(qty, 4, MidpointRounding.AwayFromZero),
                    "", LoginInfo.DefSavdptid);
                double diff = stkotdtl.Sum(e => e.qty) - qty;

                //扣减stkotdtl里面的库存
                RedcStkotQty(stkotdtl.ToArray(), diff);
            }
            return RSucc("成功", null, "{{}}");   //todo 编码
        }

        private ActionResult BokRetrieveP(String wmsno, String bllid, String bocino, String clsid, String checi, String gdsid, double qty)
        {
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
            if (arrqry.Length <= 0)
            {
                return RNoData("N0172");
            }
            wms_cutgds cutgds = arrqry[0];

            #region 取消对 5、分货确认时，发现数量小于应分货数量，要循环扣除对应的stkotdtl里面的qty 的注释
            // 如果分货标记为Y，表示该商品已经分货确认
            if (cutgds.chkflg == GetY())
            {
                return RInfo("I0368");
            }
            //扣减sktot的数量
            /*ActionResult ar = DoDecStkQty(wmsno, checi, bllid, gdsid, qty);
            JsonResult jr = (JsonResult)ar;
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode == ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return ar;
            }*/
            #endregion 取消对 5、分货确认时，发现数量小于应分货数量，要循环扣除对应的stkotdtl里面的qty 的注释

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
                return RErr(ex.Message, "E0049");
            }

            return RSucc("成功", null, "S0161");
        }

        /// <summary>
        /// 捡货单商品审核(同一商品一起拣货)
        /// </summary>
        /// <param name="wmsno">收货单单号</param>
        /// <param name="barcode">仓位码</param>
        /// <param name="gdsid">商品编码</param>        
        /// <param name="qty">实收数量</param>
        /// <returns>wms_blldtl, wms_blltp</returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_拣货确认, pwrdes = "拣货确认")]
        public ActionResult BokRetrieveGdss(String wmsno, String barcode, String gdsid, double qty)
        {
            using (TransactionScope scop = new TransactionScope(TransactionScopeOption.Required, options))
            {
                //检索主表、明细表
                var qrymst = from e in WmsDc.wms_cang
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.wmsno == wmsno
                             select e;
                var arrmst = qrymst.ToArray();
                var qrydtl1 = from e in WmsDc.wms_cangdtl                              
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.gdsid == gdsid.Trim()                             
                             && e.barcode == barcode.Trim()
                             && e.wmsno == wmsno.Trim()
                             && e.tpcode == "y"
                             //&& e.bokflg == 'n'
                             orderby e.gdstype, e.vlddat, e.bthno
                             select e;
                var qrydtl = from e in qrydtl1
                             where e.bokflg == GetN()
                             select e;
                //判断哪些人已经拣了多少货
                var qryHasRetrive = from e in qrydtl1
                                    join e1 in WmsDc.emp on e.bkr equals e1.empid
                                    where e.bokflg == GetY()
                                    select new
                                    {
                                        e.bkr,
                                        bkrdes = e1.empdes.Trim(),
                                        e.qty
                                    };
                if (qryHasRetrive.Count() > 0)
                {
                    double dHasRetivedQty = qryHasRetrive.Sum(e => e.qty);
                    double dAllRetirveingQty = (dHasRetivedQty + qty);
                    double dAllShouldRetriveQty = qrydtl1.Sum(e => e.preqty).Value;
                    if ( dAllRetirveingQty > dAllShouldRetriveQty )
                    {
                        //得到哪些人拣了货
                        string strRetriveBkrdes = string.Join(",", qryHasRetrive.Select(e => e.bkrdes).ToArray());
                        return RInfo("I0487", strRetriveBkrdes, dHasRetivedQty, dAllShouldRetriveQty - dHasRetivedQty);
                    }
                }
                             
                var arrdtl = qrydtl.ToArray();
                //得到拣货的差异数
                double diff = qrydtl1.Where(e => e.bokflg == 'n').Sum(e => e.qty) - qty;
                if (qrydtl1.Where(e => e.bokflg == 'y').Any())
                {
                    if (diff < 0)
                    {
                        string[] whoAdt = (from e in WmsDc.emp
                                           where qrydtl1.Where(ee => ee.bokflg == 'y').Select(ee => ee.bkr).Contains(e.empid)
                                           select e.empdes.Trim()).ToArray();
                        return RInfo("I0492", string.Join(",", whoAdt),
                            qrydtl1.Where(e => e.bokflg == 'y').Sum(e => e.qty),
                            qrydtl1.Where(e => e.bokflg == 'n').Sum(e => e.qty));
                    }
                }
                else if (diff < 0)
                {
                    return RInfo("I0480");
                }
                
                //循环审核拣货明细
                foreach (wms_cangdtl dtl in arrdtl)
                {
                    //如果差异数量大于本次明细数量
                    if (diff > dtl.qty)
                    {
                        diff -= dtl.qty;
                        JsonResult jr = (JsonResult)BokRetrieveGds(wmsno, barcode, gdsid, dtl.gdstype, dtl.bthno, dtl.vlddat, 0);
                        ResultMessage rm = (ResultMessage)jr.Data;
                        if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                        {
                            return jr;
                        }
                    }
                    else  //如果差异数量小于等于本次明细数量
                    {
                        JsonResult jr = (JsonResult)BokRetrieveGds(wmsno, barcode, gdsid, dtl.gdstype, dtl.bthno, dtl.vlddat, dtl.qty - diff);
                        ResultMessage rm = (ResultMessage)jr.Data;
                        if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                        {
                            return jr;
                        }
                        diff = 0;
                    }

                }
                try
                {
                    WmsDc.SubmitChanges();
                    scop.Complete();
                    return RSucc("成功", null, "S0222");
                    
                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0070");
                }
            }
        }

        /// <summary>
        /// 捡货单商品审核
        /// </summary>
        /// <param name="wmsno">收货单单号</param>
        /// <param name="barcode">仓位码</param>
        /// <param name="gdsid">商品编码</param>
        /// <param name="qty">单据商品序号</param>
        /// <param name="qty">实收数量</param>
        /// <returns>wms_blldtl, wms_blltp</returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_拣货确认, pwrdes = "拣货确认")]
        public ActionResult BokRetrieveGds(String wmsno, String barcode, String gdsid, String gdstype, String bthno, String vlddat, double qty)
        {
            ////正在生成拣货单，请稍候重试
            //string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0369" );
            //}

            
                //检索主表、明细表
                var qrymst = from e in WmsDc.wms_cang
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.wmsno == wmsno
                             select e;
                var arrmst = qrymst.ToArray();
                var qrydtl = from e in WmsDc.wms_cangdtl
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.gdsid == gdsid
                             && e.gdstype == gdstype
                             && e.bthno == bthno.Trim()
                             && e.vlddat == vlddat.Trim()
                             && e.barcode == barcode
                             && e.wmsno == wmsno
                             && e.tpcode == "y"
                             && e.bokflg == 'n'
                             select e;
                var arrdtl = qrydtl.ToArray();

                #region 检查输入参数
                if (arrmst.Length <= 0)
                {
                    return RNoData("N0243");

                }
                if (arrdtl.Length <= 0)
                {
                    return RNoData("N0244");

                }
                wms_cang mst = arrmst[0];
                wms_cangdtl dtl = arrdtl[0];
                //是否捡货单已经审核
                if (mst!=null && mst.chkflg == GetY())
                {
                    return RInfo("I0455");

                }
                #endregion

                #region 商品登帐
                //判断是否已经被审核
                WmsDc.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, dtl);
                if (dtl.bokflg == GetY())
                {
                    //看看是哪个审核的
                    string[] whoAdt = (from e in WmsDc.emp
                                       where e.empid == dtl.bkr
                                       select e.empdes).ToArray();
                    if (whoAdt.Count() > 0)
                    {
                        return RInfo("I0125", string.Join(",", whoAdt));
                    }
                }

                if (dtl.bokflg == GetY() && dtl.bkr.Trim() != LoginInfo.Usrid)
                {
                    return RInfo("I0456", dtl.bkr);

                }

                if (dtl.bokflg == GetN() && dtl.qty != null)
                {
                    //dtl.preqty = dtl.qty; 
                }
                if (dtl.tpcode == "n")
                {
                    return RInfo("I0370");
                }
                if (dtl.preqty < qty && dtl.tpcode.Trim() == "y")
                {
                    return RInfo("I0371");
                }
                
                //确认了商品后就

                //如果是206的单据，同一个商品的最后一条确认完后就不能再修改
                var qryallbygdsidN1 = from e in WmsDc.wms_cangdtl
                                      where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                                      && e.gdsid == gdsid
                                          //&& e.gdstype == gdstype                                    
                                      && e.wmsno == wmsno
                                      && e.bokflg == GetN() && e.tpcode == "y"
                                      select e;
                int iCnt = qryallbygdsidN1.Count();
                if (mst.lnkbllid.Trim() == "206" && iCnt == 0)
                {
                    return RInfo("I0372");
                }
                dtl.qty = Math.Round(qty, 4);
                dtl.pkgqty = Math.Round(qty, 4);
                dtl.bokflg = GetY();
                dtl.bokdat = DateTime.Now.ToString("yyyyMMddHHmmss");
                dtl.bkr = LoginInfo.Usrid;

                try
                {
                    WmsDc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    if (ex.Message.IndexOf("牺牲品") > 0)
                    {
                        return RInfo("E0075");
                    }
                    else
                    {
                        return RInfo("E0076", ex.Message);
                    }
                }
                if (dtl.preqty != dtl.qty)
                {
                    i(wmsno, WMSConst.BLL_TYPE_RETRIEVE, "拣货商品明细确认", "应拣数量：" + dtl.preqty + "，实拣数量:" + dtl.qty, mst.qu, mst.savdptid);
                }
                #endregion

                #region 如果是206配送拣货的单据，在同一个拣货单里面同一商品确认完后，写入分货表(边拣边播)
                // 修改分货按分店                
                if ( (!IsCutgds(mst.qu)|| thqus.Contains(mst.qu.Trim()) ) && mst.lnkbllid.Trim() == "206")
                {
                    var qryallbygdsidN = from e in WmsDc.wms_cangdtl
                                         where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                                         && e.gdsid == gdsid
                                             //&& e.gdstype == gdstype                                    
                                         && e.wmsno == wmsno
                                         && e.bokflg == GetN() && e.tpcode == "y"
                                         select e;
                    iCnt = qryallbygdsidN.Count();

                    #region 如果拣货的数量不够的话,要去修改配送单的数量和金额
                    var qryAllByGdsidCang = from e in WmsDc.wms_cangdtl
                                            join e1 in WmsDc.wms_cang on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                                            where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                                            && e.tpcode == "y"
                                            && e.bokflg == GetY()
                                            && e.barcode == barcode.Trim()
                                            && e.gdstype == gdstype.Trim()
                                            && e.vlddat == vlddat.Trim()
                                            && e.bthno == bthno.Trim()
                                            && e.gdsid == gdsid.Trim()
                                            && e.wmsno == wmsno.Trim()
                                            group e by new
                                            {
                                                e1.savdptid,
                                                e1.rcvdptid,
                                                e1.wmsno,
                                                e1.bllid,
                                                e1.lnkbocino,
                                                e1.lnkbocidat,
                                                e1.times,
                                                e.gdsid
                                            } into g
                                            select new
                                            {
                                                savdptid = g.Key.savdptid,
                                                wmsno = g.Key.wmsno,
                                                bllid = g.Key.bllid,
                                                bocino = g.Key.lnkbocino,
                                                bocidat = g.Key.lnkbocidat,
                                                clsid = g.Key.times,
                                                checi = (from e2 in WmsDc.psSndGds_dpt_dtl
                                                         where e2.dptid == g.Key.rcvdptid && e2.dh == g.Key.lnkbocino
                                                         select e2.busid.Substring(e2.busid.Trim().Length - 1, 1)).FirstOrDefault(),
                                                gdsid = g.Key.gdsid,
                                                qty = g.Sum(e => e.qty),
                                                preqty = g.Sum(e => e.preqty),
                                                ckr = "",
                                                chkflg = GetN(),
                                                chkdat = ""
                                            };

                    var cutgds = qryAllByGdsidCang.FirstOrDefault();
                    var qrystkdtl = from e in WmsDc.stkotdtl
                                    where e.stkot.wmsbllid == cutgds.bllid
                                    && e.stkot.wmsno == cutgds.wmsno
                                    && e.gdsid == cutgds.gdsid
                                    && e.bzflg == 'n'
                                    orderby Convert.ToInt32(e.stkot.rcvdptid), e.qty descending
                                    select e;
                    //double q = qrystkdtl.Sum(e => e.qty) - cutgds.qty;
                    double q = cutgds.preqty.Value - cutgds.qty;
                    var stkotdtl = qrystkdtl.ToArray();

                    //扣减stkotdtl里面的库存
                    RedcStkotQty(stkotdtl, q);

                    try
                    {
                        WmsDc.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.IndexOf("牺牲品") > 0)
                        {
                            return RInfo("E0075");
                        }
                        else
                        {
                            return RInfo("E0076", ex.Message);
                        }
                    }
                    #endregion

                    if (iCnt == 0)
                    {
                        //如果是分货播种就写入分货表
                        if (IsCutgds(mst.qu))
                        {
                            // 写入分货表
                            #region 写入分货表
                            // 如果不是残损区的就写分货表
                            if (!thqus.Contains(mst.qu))
                            {
                                var qryAllByGdsid = from e in WmsDc.stkotdtl
                                                    join e1 in WmsDc.wms_cang on new { e.stkot.wmsno, e.stkot.wmsbllid } equals new { e1.wmsno, wmsbllid = e1.bllid }
                                                    where e.stkot.wmsbllid == cutgds.bllid
                                                    && e.stkot.wmsno == cutgds.wmsno
                                                    && e.gdsid == cutgds.gdsid
                                                    && e.qty != 0
                                                    group e by new
                                                    {
                                                        e1.savdptid,
                                                        e.stkot.rcvdptid,
                                                        e1.wmsno,
                                                        e1.bllid,
                                                        e1.lnkbocino,
                                                        e1.lnkbocidat,
                                                        e1.times,
                                                        e.gdsid
                                                    } into g
                                                    select new
                                                    {
                                                        savdptid = g.Key.savdptid,
                                                        wmsno = g.Key.wmsno,
                                                        bllid = g.Key.bllid,
                                                        bocino = g.Key.lnkbocino,
                                                        bocidat = g.Key.lnkbocidat,
                                                        clsid = g.Key.times,
                                                        checi = (from e2 in WmsDc.psSndGds_dpt_dtl
                                                                 where e2.dptid == g.Key.rcvdptid && e2.dh == g.Key.lnkbocino
                                                                 select e2.busid.Substring(e2.busid.Trim().Length - 1, 1)).FirstOrDefault(),
                                                        gdsid = g.Key.gdsid,
                                                        qty = g.Sum(e => e.qty),
                                                        preqty = g.Sum(e => e.qty),
                                                        ckr = "",
                                                        chkflg = GetN(),
                                                        chkdat = ""
                                                    };
                                //i(wmsno, "", "拣货确认", qryAllByGdsid.ToString(), "", LoginInfo.DefSavdptid);                        
                                var arrQryAllByGdsid = qryAllByGdsid.ToArray();
                                foreach (var a in arrQryAllByGdsid)
                                {
                                    if (a.checi == null)
                                    {
                                        iFile(cutgds.bllid + "    " + cutgds.wmsno + "  " + cutgds.gdsid + "  ");
                                    }
                                }

                                var qryAllByGdsidSum = from e in arrQryAllByGdsid
                                                       group e by new
                                                       {
                                                           e.savdptid,
                                                           e.wmsno,
                                                           e.bllid,
                                                           e.bocino,
                                                           e.bocidat,
                                                           e.clsid,
                                                           e.checi,
                                                           e.gdsid,
                                                           e.ckr,
                                                           e.chkflg,
                                                           e.chkdat
                                                       } into g
                                                       select new
                                                       {
                                                           g.Key.savdptid,
                                                           g.Key.wmsno,
                                                           g.Key.bllid,
                                                           g.Key.bocino,
                                                           g.Key.bocidat,
                                                           g.Key.clsid,
                                                           g.Key.checi,
                                                           g.Key.gdsid,
                                                           g.Key.ckr,
                                                           g.Key.chkflg,
                                                           g.Key.chkdat,
                                                           qty = g.Sum(e => e.qty),
                                                           preqty = g.Sum(e => e.preqty)
                                                       };

                                List<wms_cutgds> lstCg = new List<wms_cutgds>();
                                foreach (var tcg in qryAllByGdsidSum)
                                {
                                    wms_cutgds cg = new wms_cutgds();
                                    cg.bllid = tcg.bllid;
                                    cg.bocidat = tcg.bocidat;
                                    cg.bocino = tcg.bocino;
                                    cg.checi = tcg.checi;
                                    cg.chkdat = tcg.chkdat;
                                    cg.chkflg = tcg.chkflg;
                                    cg.ckr = tcg.ckr;
                                    cg.clsid = tcg.clsid;
                                    cg.gdsid = tcg.gdsid;
                                    cg.preqty = tcg.preqty;
                                    cg.qty = tcg.qty;
                                    cg.savdptid = tcg.savdptid;
                                    cg.wmsno = tcg.wmsno;
                                    lstCg.Add(cg);
                                    WmsDc.wms_cutgds.InsertOnSubmit(cg);
                                    try
                                    {
                                        WmsDc.SubmitChanges();
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex.Message.IndexOf("牺牲品") > 0)
                                        {
                                            return RInfo("E0075");
                                        }
                                        else
                                        {
                                            return RInfo("E0076", ex.Message);
                                        }
                                    }

                                    /// 自动分货设置
                                    /// val1=y-自动,val1=n-人工
                                    bool b = IsAutoCutCheck(mst.qu);
                                    if (b)
                                    {
                                        ActionResult ar = BokRetrieveP(cg.wmsno, cg.bllid, cg.bocino, cg.clsid, cg.checi, cg.gdsid, cg.qty);
                                        JsonResult jr = (JsonResult)ar;
                                        ResultMessage rm = (ResultMessage)jr.Data;
                                        if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                                        {
                                            return ar;
                                        }
                                        //ckRtrv.BokRetrieve(cg.wmsno, cg.bllid, cg.bocino, cg.clsid, cg.checi, cg.gdsid, cg.qty);                                    
                                    }
                                }
                                //WmsDc.wms_cutgds.InsertAllOnSubmit(lstCg);
                                try
                                {
                                    WmsDc.SubmitChanges();
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.IndexOf("牺牲品") > 0)
                                    {
                                        return RInfo("E0075");
                                    }
                                    else
                                    {
                                        return RInfo("E0076", ex.Message);
                                    }
                                }
                            }


                            #endregion 写入分货表
                        }


                    }
                }
                #endregion

                #region 如果是206配送拣货单已经拣货的数量满足一个堆堆，就按堆堆应分数量升序写入分货表（按堆堆播种）
                if (IsCutgds(mst.qu) && mst.lnkbllid.Trim() == "206")
                {
                    //
                    //得到该商品已经拣货的数量
                    var qryHasRetrive = from e in WmsDc.wms_cang
                                        join e1 in WmsDc.wms_cangdtl on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                                        where e.wmsno == wmsno && e.bllid == "103" && e.lnkbllid == "206"
                                        && e1.gdsid == gdsid
                                        && e1.bokflg == GetY()
                                        select e1;
                    double? dHasRetrive = qryHasRetrive.Sum(e => e.qty);

                    //得到该商品已经写入分货表中的数量
                    var qryHasCutgds = from e in WmsDc.wms_cutgds
                                       where e.wmsno == wmsno && e.bllid == "103"
                                       && e.gdsid == gdsid
                                       select e;
                    //得到该商品已经拣货但是未写入分货表的数量
                    double? dHasCutGds = 0;
                    if (qryHasCutgds.Count() > 0)
                    {
                        dHasCutGds = qryHasCutgds.Sum(e => e.qty == null ? 0 : e.qty);
                    }
                    double dNoInCutgds = dHasRetrive.Value - dHasCutGds.Value;

                    //得到该商品每个堆堆配送单该商品需要配送的数据, 并按从小打到的顺序排序
                    var qryShouldDoCheci = from e in WmsDc.wms_boci
                                           join e1 in WmsDc.view_pssndgds on new { e.dh, e.sndtmd, e.savdptid, e.qu } equals new { e1.dh, e1.sndtmd, e1.savdptid, e1.qu }
                                           join e5 in WmsDc.wms_set on new { setid = "001", e1.savdptid, e1.qu, isvld = GetY() } equals new { e5.setid, savdptid = e5.val3, qu = e5.val1, e5.isvld }
                                           join e2 in WmsDc.wms_cang on new { e.savdptid, e.dh, e.sndtmd, e.qu } equals new { e2.savdptid, dh = e2.lnkbocino, sndtmd = e2.lnkbocidat, e2.qu }
                                           join e3 in WmsDc.stkot on new { e2.wmsno, wmsbllid = e2.bllid, e1.rcvdptid, e1.savdptid, dptid = e5.val2 } equals new { e3.wmsno, e3.wmsbllid, e3.rcvdptid, e3.savdptid, e3.dptid }
                                           join e4 in WmsDc.stkotdtl on new { e3.stkouno } equals new { e4.stkouno }
                                           where e2.wmsno == wmsno && e2.bllid == "103" && e2.lnkbllid == "206"                                           
                                           && e4.gdsid == gdsid
                                           group e4 by new
                                           {
                                               e1.savdptid,
                                               e2.wmsno,
                                               e2.bllid,
                                               bocino = e2.lnkbocino,
                                               bocidat = e2.lnkbocidat,
                                               e1.clsid,
                                               checi = e1.busid.Trim().Substring(e1.busid.Trim().Length - 1, 1),
                                               e1.qu,
                                               e4.gdsid                                               
                                           } into g                                           
                                           orderby g.Sum(e => e.qty)
                                           select new
                                           {
                                               savdptid = g.Key.savdptid,
                                               wmsno = g.Key.wmsno,
                                               bllid = g.Key.bllid,
                                               bocino = g.Key.bocino,
                                               bocidat = g.Key.bocidat,
                                               clsid = g.Key.clsid,
                                               checi = g.Key.checi.Trim(),
                                               gdsid = g.Key.gdsid.Trim(),
                                               qu = g.Key.qu.Trim(),
                                               qty = g.Sum(e => e.qty),
                                               preqty = g.Sum(e => e.qty)
                                           };

                    var arrShouldDoCheci = qryShouldDoCheci.ToArray();
                    //得到需要分配的当前堆堆
                    var aShouldDoCheci = arrShouldDoCheci[0];
                    double dSumHasCutgds = 0;

                    //如果有拣货就计算
                    if (dHasRetrive.Value > 0)
                    {
                        
                        //如果未分货数量大于0就分货
                        if (dNoInCutgds > 0)
                        {
                            
                            
                            foreach (var aCheci in arrShouldDoCheci)
                            {
                                aShouldDoCheci = aCheci;
                                //循环统计应播数，是否大于等于已经写入cutgds中的数量
                                if (dSumHasCutgds >= dHasCutGds)
                                {
                                    //如果是边拣边播就执行这个
                                    if (IsCutgds(aShouldDoCheci.qu))
                                    {
                                        //判断该待分货的堆堆的数量是否小于等于已拣货为分货数量, 如果条件成立就开始分货
                                        if (aShouldDoCheci.qty <= dNoInCutgds)
                                        {
                                            wms_cutgds cutgds = new wms_cutgds();
                                            cutgds.savdptid = aShouldDoCheci.savdptid;
                                            cutgds.wmsno = aShouldDoCheci.wmsno;
                                            cutgds.bllid = aShouldDoCheci.bllid;
                                            cutgds.bocino = aShouldDoCheci.bocino;
                                            cutgds.bocidat = aShouldDoCheci.bocidat;
                                            cutgds.clsid = aShouldDoCheci.clsid;
                                            cutgds.checi = aShouldDoCheci.checi;
                                            cutgds.gdsid = aShouldDoCheci.gdsid;
                                            cutgds.qty = aShouldDoCheci.qty;
                                            dNoInCutgds -= aShouldDoCheci.qty;
                                            cutgds.preqty = aShouldDoCheci.preqty;
                                            /// 自动分货设置
                                            /// val1=y-自动,val1=n-人工
                                            bool b = IsAutoCutCheck(mst.qu);
                                            if (b)
                                            {
                                                cutgds.ckr = LoginInfo.Usrid;
                                                cutgds.chkflg = GetY();
                                                cutgds.chkdat = GetCurrentDate();
                                            }
                                            else
                                            {
                                                cutgds.ckr = "";
                                                cutgds.chkflg = GetN();
                                                cutgds.chkdat = "";
                                            }
                                            WmsDc.wms_cutgds.InsertOnSubmit(cutgds);

                                            dHasCutGds += cutgds.qty;

                                            try
                                            {
                                                WmsDc.SubmitChanges();
                                            }
                                            catch (Exception ex)
                                            {
                                                if (ex.Message.IndexOf("牺牲品") > 0)
                                                {
                                                    return RInfo("E0075");
                                                }
                                                else
                                                {
                                                    return RInfo("E0076", ex.Message);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }// end if (aShouldDoCheci.qty <= dNoInCutgds)


                                    } //end if (IsCutgdsOneByOne(aShouldDoCheci.qu))


                                }
                                dSumHasCutgds += aShouldDoCheci.qty;
                            }

                            #region 扣减sktot配送单的数量 DoDecStkQty
                            //扣减sktot的数量
                            //判断一个分店是否已经拣货完毕，如果已经拣货完毕就去扣减该分店的拣货
                            //if (aShouldDoCheci.qty <= dNoInCutgds)
                            //{
                            //    ActionResult ar = DoDecStkQtyByCheci(wmsno, aShouldDoCheci.checi, aShouldDoCheci.bllid, gdsid,qty);
                            //    JsonResult jr = (JsonResult)ar;
                            //    ResultMessage rm = (ResultMessage)jr.Data;
                            //    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                            //    {
                            //        return ar;
                            //    }
                            //}
                            #endregion 扣减sktot配送单的数量 DoDecStkQty

                            
                        }
                    }

                    #region 判断此次拣货是否是该商品的最后一次拣货
                    if (IsCutgds(aShouldDoCheci.qu))
                    {
                        //判断此次拣货是否是该商品的最后一次拣货                                
                        //得到当前需要写入分货表的堆堆
                        var qryIsLastBarcodeInWmsno = from e in WmsDc.wms_cang
                                                      join e1 in WmsDc.wms_cangdtl on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                                                      where e.bllid == "103" && e.wmsno == wmsno.Trim()
                                                      && e1.gdsid == gdsid
                                                      && e1.tpcode == "y"
                                                      && e1.bokflg == GetN()
                                                      select e1;
                        //如果该商品不存在没有拣货未确认的，就是该商品的最后一次拣货
                        if (!qryIsLastBarcodeInWmsno.Any())
                        {
                            //将已拣货未达到最后一次应拣数量的，写入aShouldDoCheci分货表
                            if (dNoInCutgds > 0 && (dNoInCutgds < aShouldDoCheci.qty))
                            {
                                wms_cutgds cutgds = new wms_cutgds();
                                cutgds.savdptid = aShouldDoCheci.savdptid;
                                cutgds.wmsno = aShouldDoCheci.wmsno;
                                cutgds.bllid = aShouldDoCheci.bllid;
                                cutgds.bocino = aShouldDoCheci.bocino;
                                cutgds.bocidat = aShouldDoCheci.bocidat;
                                cutgds.clsid = aShouldDoCheci.clsid;
                                cutgds.checi = aShouldDoCheci.checi;
                                cutgds.gdsid = aShouldDoCheci.gdsid;
                                cutgds.qty = dNoInCutgds;
                                cutgds.preqty = aShouldDoCheci.preqty;
                                /// 自动分货设置
                                /// val1=y-自动,val1=n-人工
                                bool b = IsAutoCutCheck(mst.qu);
                                if (b)
                                {
                                    cutgds.ckr = LoginInfo.Usrid;
                                    cutgds.chkflg = GetY();
                                    cutgds.chkdat = GetCurrentDate();
                                }
                                else
                                {
                                    cutgds.ckr = "";
                                    cutgds.chkflg = GetN();
                                    cutgds.chkdat = "";
                                }
                                WmsDc.wms_cutgds.InsertOnSubmit(cutgds);
                                try
                                {
                                    WmsDc.SubmitChanges();
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.IndexOf("牺牲品") > 0)
                                    {
                                        return RInfo("E0075");
                                    }
                                    else
                                    {
                                        return RInfo("E0076", ex.Message);
                                    }
                                }
                            }

                            //将该商品未分货的堆堆全部写入分货表，数量为0
                            var qryNoInCutgds = qryShouldDoCheci.Where(e =>
                                    !WmsDc.wms_cutgds.Where(e1 => e1.wmsno == e.wmsno
                                           && e1.bllid == e.bllid && e1.savdptid == e.savdptid
                                           && e1.bocino == e.bocino && e1.bocidat == e.bocidat
                                           && e1.gdsid == e.gdsid
                                           && e1.checi == e.checi
                                    ).Any()
                                  );
                            var arrQryNoInCutgds = qryNoInCutgds.ToArray();
                            foreach (var nc in arrQryNoInCutgds)
                            {
                                wms_cutgds cutgds = new wms_cutgds();
                                cutgds.savdptid = nc.savdptid;
                                cutgds.wmsno = nc.wmsno;
                                cutgds.bllid = nc.bllid;
                                cutgds.bocino = nc.bocino;
                                cutgds.bocidat = nc.bocidat;
                                cutgds.clsid = nc.clsid;
                                cutgds.checi = nc.checi;
                                cutgds.gdsid = nc.gdsid;
                                if (dNoInCutgds >= nc.qty)
                                {
                                    cutgds.qty = nc.qty;
                                    dNoInCutgds -= nc.qty;
                                }
                                else
                                {
                                    cutgds.qty = 0;
                                }
                                cutgds.preqty = nc.preqty;
                                /// 自动分货设置
                                /// val1=y-自动,val1=n-人工
                                bool b = IsAutoCutCheck(mst.qu);
                                if (b)
                                {
                                    cutgds.ckr = LoginInfo.Usrid;
                                    cutgds.chkflg = GetY();
                                    cutgds.chkdat = GetCurrentDate();
                                }
                                else
                                {
                                    cutgds.ckr = "";
                                    cutgds.chkflg = GetN();
                                    cutgds.chkdat = "";
                                }
                                WmsDc.wms_cutgds.InsertOnSubmit(cutgds);
                            }
                            try
                            {
                                WmsDc.SubmitChanges();
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.IndexOf("牺牲品") > 0)
                                {
                                    return RInfo("E0075");
                                }
                                else
                                {
                                    return RInfo("E0076", ex.Message);
                                }
                            }

                            //修改最后一个确认的堆堆对应，分店的stkotdtl表中数据
                            var qryNotEqualCutgds = qryHasCutgds.Where(e => e.qty != e.preqty).Select(e => e);
                            var arrQryNotEqualCutgds = qryNotEqualCutgds.ToArray();
                            if (arrQryNotEqualCutgds.Length > 0)
                            {
                                //重新计算stkotdtl
                                foreach (var nec in arrQryNotEqualCutgds)
                                {
                                    var qryStkotdtl = from e in WmsDc.stkotdtl
                                                      where
                                                      e.stkot.chkflg == GetN() &&
                                                      e.bzflg == GetN() &&
                                                      e.gdsid == gdsid &&
                                                      (
                                                        from e1 in WmsDc.view_pssndgds
                                                        join e2 in WmsDc.wms_set on new { setid = "001", isvld = GetY(), e1.qu, e1.savdptid }
                                                            equals new { e2.setid, e2.isvld, qu = e2.val1, savdptid = e2.val3 }
                                                        join e3 in WmsDc.wms_cang on new { e1.savdptid, e1.qu, e1.dh, e1.sndtmd } equals new { e3.savdptid, e3.qu, dh = e3.lnkbocino, sndtmd = e3.lnkbocidat }
                                                        where e.stkot.wmsno == e3.wmsno && e.stkot.wmsbllid == e3.bllid
                                                        && e2.val2 == e.stkot.dptid && e1.rcvdptid == e.stkot.rcvdptid
                                                        && e1.sndtmd == nec.bocidat
                                                        && e1.dh == nec.bocino
                                                        && e3.bllid == nec.bllid
                                                        && e3.wmsno == nec.wmsno
                                                        && e1.savdptid == nec.savdptid
                                                        && e1.busid.Trim().Substring(e1.busid.Trim().Length - 1, 1) == nec.checi
                                                        select e1
                                                      ).Any()
                                                      select e;
                                    RedcStkotQty(qryStkotdtl.ToArray(), nec.preqty - nec.qty);
                                    nec.preqty = nec.qty;
                                }
                                try
                                {
                                    WmsDc.SubmitChanges();
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.IndexOf("牺牲品") > 0)
                                    {
                                        return RInfo("E0075");
                                    }
                                    else
                                    {
                                        return RInfo("E0076", ex.Message);
                                    }
                                }
                            }
                        }
                    }
                    #endregion 判断此次拣货是否是该商品的最后一次拣货

                    
                }
                #endregion 如果是206配送拣货单已经拣货的数量满足一个堆堆，就按堆堆应分数量升序写入分货表

                #region 判断是否是该拣货单下的配送单有没有已经播种完了的单据（包括为0的商品），有就修改改配送单下的明细为0的播种标记，和主单播种标记
                //将配送单下修改为0的配送明细，直接修改其播种标记
                var qryStkotdtlZero = from e in WmsDc.stkotdtl
                                      where e.qty <= 0 && e.bzflg == GetN()
                                      && e.stkot.wmsno == wmsno && e.stkot.wmsbllid == "103"
                                      select e;
                var arrQryStkotdtlZero = qryStkotdtlZero.ToArray();
                foreach (var dz in arrQryStkotdtlZero)
                {
                    dz.bzflg = GetY();
                    dz.bzdat = GetCurrentDate();
                    dz.bzr = LoginInfo.Usrid;
                }
                try
                {
                    WmsDc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    if (ex.Message.IndexOf("牺牲品") > 0)
                    {
                        return RInfo("E0075");
                    }
                    else
                    {
                        return RInfo("E0076", ex.Message);
                    }
                }
                //判断配送单据下的所有商品是否都已经播种，播种了就直接修改主单播种标记
                var qryStkotdtlZero1 = from e in WmsDc.stkotdtl
                                       where e.stkot.wmsno == wmsno && e.stkot.wmsbllid == "103" && e.stkot.bllid == "206"
                                       group e by new { e.stkouno } into g
                                       select new
                                       {
                                           g.Key.stkouno,
                                           allCnt = g.Count(),
                                           hasBzCnt = g.Count(e => e.bzflg == GetY())
                                       };
                var arrQryStkotdtlZero1 = qryStkotdtlZero1.Where(e=>e.allCnt==e.hasBzCnt).Select(e=>e.stkouno.Trim()).ToArray();
                var qryHasAllBz = from e in WmsDc.stkot
                                  where e.chkflg == GetN()
                                  && e.bzflg == GetN()
                                  && arrQryStkotdtlZero1.Contains(e.stkouno)
                                  select e;
                foreach(var hbz in qryHasAllBz){                    
                    //修改播种标记
                    hbz.bzflg = GetY();
                    //审核配送单
                    hbz.chkflg = GetY();
                    hbz.chkdat = GetCurrentDay();
                    hbz.ckr = LoginInfo.Usrid;

                    //写入dtrlog
                    //查看是否dtrlog已经有单据,没有就插入
                    var qry = WmsDc.dtrlog
                                .Where(e => e.rcvdptid == hbz.rcvdptid && e.bllno == hbz.stkouno && e.bllid == hbz.bllid)
                                .Select(e => e.bllno);
                    var arrqry = qry.ToArray();
                    if (arrqry.Length <= 0)
                    {
                        dtrlog dl = new dtrlog();
                        dl.bllid = hbz.bllid;
                        dl.bllno = hbz.stkouno;
                        dl.rcvdptid = hbz.rcvdptid;
                        WmsDc.dtrlog.InsertOnSubmit(dl);
                    }

                    stklst astklst = new stklst();
                    astklst.stkouno = hbz.stkouno;
                    WmsDc.stklst.InsertOnSubmit(astklst);
                }

                try
                {
                    WmsDc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    if (ex.Message.IndexOf("牺牲品") > 0)
                    {
                        return RInfo("E0075");
                    }
                    else
                    {
                        return RInfo("E0076", ex.Message);
                    }
                }
                #endregion 判断是否是该拣货单下的配送单有没有已经播种完了的单据（包括为0的商品），有就修改改配送单下的明细为0的播种标记，和主单播种标记

                try
                {
                    try
                    {
                        WmsDc.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.IndexOf("牺牲品") > 0)
                        {
                            return RInfo("E0075");
                        }
                        else
                        {
                            return RInfo("E0076", ex.Message);
                        }
                    }                 
                    return RSucc("成功", null, "S0222");

                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0070");

                }
            
        }

        private ActionResult DoDecStkQtyByCheci(string wmsno, string checi, string bllid, string gdsid, string bthno, string vlddat, double qty)
        {
            //如果一个车次的拣完了，就计算该车次的拣货和配送是否一致
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
                return RNoData("N0173");
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
                return RNoData("N0174");
            }
            wms_cang[] wms_cang = arrqrycang;
            //得到wms_cang的信息
            var qrycangdtl = from e in WmsDc.wms_cangdtl
                             where e.wmsno == wmsno.Trim()
                             && e.bllid == bllid.Trim()
                             && e.gdsid == gdsid.Trim()
                             && e.bthno == bthno.Trim()
                             && e.vlddat == vlddat.Trim()
                             select e;
            var arrqrycangdtl = qrycangdtl.ToArray();
            if (arrqrycangdtl.Length <= 0)
            {
                return RNoData("N0175");
            }
            wms_cangdtl[] wms_cangdtl = arrqrycangdtl;

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
            return RSucc("成功", null, "S0233");   //todo 编码
        }

        

        private bool IsAutoCutCheck(String qu)
        {
            wms_set set = (from e in WmsDc.wms_set
                           where e.setid == "015" && e.isvld == GetY()
                           && e.val2 == qu.Trim()
                           && e.val3 == LoginInfo.DefStoreid
                           select e).FirstOrDefault();
            bool b = (set != null && set.val1 == "y");
            return b;
        }

        /// <summary>
        /// 捡货单审核
        /// </summary>
        /// <param name="wmsno">捡货单号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_拣货审核, pwrdes = "拣货审核")]
        public ActionResult BokRetrieve(String wmsno)
        {
            using (TransactionScope scop = new TransactionScope(TransactionScopeOption.Required, options))
            {
                Rm.ResultObject = null;
                //检索捡货单主表、明细表
                var qrymst = from e in WmsDc.wms_cang
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.wmsno == wmsno
                             select e;
                var arrmst = qrymst.ToArray();
                var qrydtl = from e in WmsDc.wms_cangdtl
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.wmsno == wmsno
                             select e;
                var arrdtl = qrydtl.ToArray();

                #region 检查输入参数
                if (arrmst.Length <= 0)
                {
                    return RNoData("N0245");

                }
                if (arrdtl.Length <= 0)
                {
                    return RNoData("N0246");

                }
                wms_cang mst = arrmst[0];
                ////正在生成拣货单，请稍候重试
                //string quRetrv = mst.qu;
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0373" );
                //}
                foreach (wms_cangdtl d in arrdtl)
                {
                    //是否捡货单商品已经审核
                    if(d.bokflg==GetN() && d.tpcode=="y")
                    {
                        return RInfo("I0457" ,d.gdsid );

                    }
                }
                if (mst!=null && mst.chkflg == GetY())
                {
                    return RInfo("I0458");

                }
                #endregion

                #region 播种单查询
                ////播种单主表，明细表
                var qrybzmst = from e in WmsDc.wms_bzmst
                               where e.bllid == WMSConst.BLL_TYPE_BZ
                               && e.lnkbocidat == mst.lnkbocidat
                               && e.lnkbocino == mst.lnkbocino
                               && e.qu == mst.qu
                               && e.savdptid == mst.savdptid
                               select e;
                var arrbzmst = qrybzmst.ToArray();
                if (arrbzmst.Length <= 0)
                {
                    return RNoData("N0247");

                }
                wms_bzmst bzmst = arrbzmst[0];
                //播种单明细表
                var qrybzdtl = from e in WmsDc.wms_bzdtl
                               where e.wmsno == bzmst.wmsno
                               && arrdtl.Select(e1 => e1.gdsid).Contains(e.gdsid.Trim())
                               select e;
                wms_bzdtl[] arrbzdtls = qrybzdtl.ToArray();
                #endregion

                #region 差异数量生成捡货损溢单，记日志

                //差异数量生成捡货损溢单，记日志
                #region 差异数量生成捡货损溢单，记日志
                //生成捡货损溢单
                JsonResult jr = (JsonResult)MakeNewBllNo(this.LoginInfo.DefSavdptid, mst.qu, WMSConst.BLL_TYPE_RETRIEVE_PROFIT_LOSS, (bllno) =>
                {
                    //生成损溢单主表
                    wms_cang sycang = new wms_cang();
                    sycang.wmsno = bllno;
                    sycang.bllid = WMSConst.BLL_TYPE_RETRIEVE_PROFIT_LOSS;
                    sycang.savdptid = mst.savdptid;
                    sycang.prvid = mst.prvid;
                    sycang.qu = mst.qu;
                    sycang.rcvdptid = mst.rcvdptid;
                    sycang.times = mst.times;
                    sycang.lnkbocino = mst.lnkbocino;
                    sycang.lnkbocidat = mst.lnkbocidat;
                    sycang.mkr = LoginInfo.Usrid;
                    sycang.mkedat = DateTime.Now.ToString("yyyyMMdd");
                    sycang.mkedat2 = GetCurrentDate();
                    sycang.ckr = LoginInfo.Usrid;
                    sycang.chkflg = GetY();
                    sycang.chkdat = DateTime.Now.ToString("yyyyMMddHHmmss");
                    sycang.opr = LoginInfo.Usrid;
                    sycang.brief = "";
                    sycang.lnkbllid = mst.bllid;
                    sycang.lnkno = mst.wmsno;
                    sycang.lnkbrief = mst.brief;

                    //生成损溢单明细
                    List<wms_cangdtl> lstsydtl = new List<wms_cangdtl>();
                    foreach (wms_cangdtl dt in arrdtl)
                    {
                        if (dt.qty != dt.preqty)
                        {
                            wms_cangdtl sydtl = new wms_cangdtl();
                            sydtl.wmsno = bllno;
                            sydtl.bllid = WMSConst.BLL_TYPE_RETRIEVE_PROFIT_LOSS;
                            sydtl.rcdidx = dt.rcdidx;
                            sydtl.oldbarcode = dt.oldbarcode;
                            sydtl.barcode = dt.barcode;
                            sydtl.gdsid = dt.gdsid;
                            sydtl.pkgid = dt.pkgid;
                            sydtl.pkgqty = dt.pkgqty;
                            sydtl.qty = dt.preqty.Value - dt.qty;
                            sydtl.gdstype = dt.gdstype;
                            sydtl.bthno = dt.bthno;
                            sydtl.vlddat = dt.vlddat;
                            sydtl.bcd = dt.bcd;
                            sydtl.tpcode = dt.tpcode;
                            sydtl.bkr = LoginInfo.Usrid;
                            sydtl.bokflg = GetY();
                            sydtl.bokdat = DateTime.Now.ToString("yyyyMMddHHmmss");
                            sydtl.preqty = dt.preqty.Value - dt.qty;
                            lstsydtl.Add(sydtl);
                            //记录日志
                            Log.i(LoginInfo.Usrid, Mdlid, bllno, WMSConst.BLL_TYPE_RETRIEVE_PROFIT_LOSS, "损溢单生成", dt.gdsid.Trim() + ":应收:" + dt.preqty.Value + ";实收:" + dt.qty, mst.qu, mst.savdptid);
                            //记账并删除为0的仓位信息。
                            var cwggdsbs = WmsDc.wms_cwgdsbs
                                .Where(e => e.barcode == sydtl.barcode && e.bcd == sydtl.bcd && e.savdptid == mst.savdptid && e.gdsid == sydtl.gdsid && e.gdstype == sydtl.gdstype)
                                .Select(e => e).Single();
                            if (cwggdsbs != null)
                            {
                                cwggdsbs.qty -= sydtl.qty;
                                if (cwggdsbs.qty <= 0)
                                {
                                    WmsDc.wms_cwgdsbs.DeleteOnSubmit(cwggdsbs);
                                    iDelCwgdsbs(new wms_cwgdsbs[] { cwggdsbs });
                                }
                            }
                        }
                    }
                    WmsDc.wms_cang.InsertOnSubmit(sycang);
                    WmsDc.wms_cangdtl.InsertAllOnSubmit(lstsydtl);
                    
                    return RRSucc("成功", null, "{{succ}}");
                });


                Rm = (ResultMessage)jr.Data;
                if (Rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return jr;
                }
                #endregion


                //拣货数量和应拣数量判断是不是相等， 不相等就改播种单数量
                #region 拣货数量和应拣数量判断是不是相等， 不相等就改播种单数量
                var qsum = arrdtl
                    .GroupBy(g => new { g.gdsid, g.gdstype })
                    .Select(g => new { gdsid = g.Key.gdsid, gdstype = g.Key.gdstype, sumqty = g.Sum(e => e.qty) });
                foreach (var q in qsum)
                {
                    //查找播种明细商品汇总数量是否和捡货单的商品汇总数量一致
                    var qbzsum = arrbzdtls
                        .Where(e => e.gdsid == q.gdsid && e.gdstype == q.gdstype)
                        .GroupBy(g => new { g.gdsid, g.gdstype })
                        .Select(g => new { gdsid = g.Key.gdsid, gdstype = g.Key.gdstype, qsum = g.Sum(e => e.qty) }).Single();
                    if (qbzsum.qsum < q.sumqty)    //如果数量不一致就生成捡货损溢单，并记日志
                    {
                        return RInfo("I0459");

                    }
                    else if (qbzsum.qsum >= q.sumqty)    //如果播种的数量小于捡货单的数量
                    {
                        double remainqty = q.sumqty;
                        var arrbzdtlsGdstyp = arrbzdtls.Where(e => e.gdstype == q.gdstype && e.gdsid == q.gdsid);
                        //-----------分派数量----------
                        //按照播种数量从大到小的顺序   |
                        //从小到大开始播种             |
                        //播种到最后就播0              |
                        //-----------------------------
                        /*String cmdsql = "declare @remainqty deciaml(18,4)\r\n"
                        + "declare @wmsno varchar(20), @gdstype varchar(20), @rcdidx int, @gdsid varchar(10), @rcvdptid varchar(10), @qty decimal\r\n"
                        + "set @remainqty={0}\r\n"
                        + "set @gdsid={1}\r\n"
                        + "set @gdstype={2}\r\n"
                        + "set @wmsno={3}\r\n"
                        + "declare cur1 cursor for\r\n"
                        + "    select rcvdptid, qty, rcdidx from wms_bzdtl where wmsno=@wmsno and gdstype=@gdstype and gdsid=@gdsid order by qty desc, rcdidx \r\n"
                        + "open cur1\r\n"
                        + "fetch next from cur1 into @rcvdptid, @qty, @rcdidx\r\n"
                        + "while @@fetch_status=0\r\n"
                        + "begin\r\n"
                        + "    if @remainqty-@qty>0\r\n"
                        + "    begin\r\n"
                        + "        update wms_bzdtl set qty = @qty where wmsno=@wmsno and gdstype=@gdstype and gdsid=@gdsid and rcdidx=@rcdidx\r\n"
                        + "    end\r\n"
                        + "    else if @remainqty>0 and @remainqty-@qty<=0\r\n"
                        + "    begin\r\n"
                        + "        update wms_bzdtl set qty = @remainqty where wmsno=@wmsno and gdstype=@gdstype and gdsid=@gdsid and rcdidx=@rcdidx\r\n"
                        + "    end\r\n"
                        + "    else\r\n"
                        + "    begin\r\n"
                        + "        update wms_bzdtl set qty = 0 where wmsno=@wmsno and gdstype=@gdstype and gdsid=@gdsid and rcdidx=@rcdidx\r\n"
                        + "    end\r\n"
                        + "    set @remainqty = @remainqty-@qty\r\n"
                        + "    fetch next from cur1 into @rcvdptid, @qty, @rcdidx\r\n"
                        + "end\r\n"
                        + "close cur1\r\n"
                        + "deallocate cur1";*/ 
                        /*
                         * preqty = preqty==null ? qty : preqty
                         * 
                         * 公式：taxamt = qty*prc*taxrto
                         * amt = qty*prc
                         * salamt = qty*salprc
                         * patamt = qty*taxprc
                         * stotcstamt = qty*stotcstprc
                         * 
                         */

                        String cmdsql = "";
                        if (mst.lnkbllid.Trim()=="206"||mst.lnkbllid.Trim()=="501")
                        {
                            cmdsql = "declare @remainqty deciaml(18,4)\r\n"
                             + "declare @wmsno varchar(20), @gdstype varchar(20), @rcdidx int, @gdsid varchar(10), @rcvdptid varchar(10), @qty decimal, @stkouno varchar(30)\r\n"
                             + "set @remainqty={0}\r\n"
                             + "set @gdsid={1}\r\n"
                             + "set @wmsno={2}\r\n"
                             + "declare cur1 cursor for\r\n"
                             // 排序规则 1、先减去为小数的；2、减去件规为不慢整件的； 3、数量倒序
                             + "  select top 100 percent b.rcvdptid, a.qty, a.rcdidx, b.stkouno, typ='1' from stkotdtl a inner join stkot b on a.stkouno=b.stkouno\r\n"                             
                             + "          where b.wmsno=@wmsno and b.wmsbllid='" + WMSConst.BLL_TYPE_RETRIEVE + "' and a.gdsid=@gdsid order by qty desc, rcdidx \r\n"
                             + "open cur1\r\n"
                             + "fetch next from cur1 into @rcvdptid, @qty, @rcdidx, @stkouno\r\n"
                             + "while @@fetch_status=0\r\n"
                             + "begin\r\n"
                             + "     update stkotdtl set preqty=case when preqty is null then qty else preqty end where stkouno=@stkouno and wmsbllid='" + WMSConst.BLL_TYPE_RETRIEVE + "' and gdsid=@gdsid and rcvidx=@rcvidx  \r\n"
                             + "    if @remainqty-@qty>0\r\n"
                             + "    begin\r\n"
                             + "        update stkotdtl set qty = @qty where stkouno=@stkouno and gdsid=@gdsid and rcdidx=@rcdidx\r\n"
                             + "    end\r\n"
                             + "    else if @remainqty>0 and @remainqty-@qty<=0\r\n"
                             + "    begin\r\n"
                             + "        update stkotdtl set qty = @remainqty where stkouno=@stkouno and gdsid=@gdsid and rcdidx=@rcdidx\r\n"
                             + "    end\r\n"
                             + "    else\r\n"
                             + "    begin\r\n"
                             + "        update stkotdtl set qty = 0 where stkouno=@stkouno and gdsid=@gdsid and rcdidx=@rcdidx\r\n"
                             + "    end\r\n"
                             + "    update stkotdtl set amt=qty*prc, salamt=qty*salprc, patamt=qty*taxprc, stotcstamt=qty*stotcstprc where stkouno=@stkouno and gdsid=@gdsid and rcdidx=@rcdidx \r\n"
                             + "    set @remainqty = @remainqty-@qty\r\n"
                             + "    fetch next from cur1 into @rcvdptid, @qty, @rcdidx, @stkouno\r\n"
                             + "end\r\n"
                             + "close cur1\r\n"
                             + "deallocate cur1";
                            //WmsDc.ExecuteCommand(cmdsql, new object[] { remainqty, q.gdsid, bzmst.wmsno });
                        }
                        
                    }
                }
                #endregion

                #endregion
                
                #region 修改审核标记
                //修改审核标记
                mst.chkflg = GetY();
                mst.chkdat = DateTime.Now.ToString("yyyyMMddHHmmss");
                mst.ckr = LoginInfo.Usrid;
                #endregion
                
                try
                {
                    WmsDc.SubmitChanges();
                    return RSucc("成功", null, "S0223");

                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0071");

                }

            }
        }

        /// <summary>
        /// 拣货查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="bllid">单据类型</param>
        /// <param name="barcode">仓位编码</param>
        /// <param name="bkr">拣货确认人</param>
        /// <param name="gdsid">商品编码</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_拣货查询, pwrdes = "拣货查询")]
        public ActionResult FindBll(String begindat, String enddat, String barcode, String bllid,String bkr, String gdsid)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo( "I0374",barcode.Trim()  );
            }

            var arrqrymst = FindBllFromCangMst103(begindat, enddat, barcode, bllid, bkr, gdsid);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0176");
            }
            return RSucc("成功", arrqrymst, "S0162");
        }


        [PWR(Pwrid = WMSConst.WMS_BACK_拣货查询, pwrdes = "拣货查询")]
        public ActionResult FindAllRetrieve(String gdsid, String zdr)
        {
            if (string.IsNullOrEmpty(gdsid.Trim()))
            {
                return RInfo("I0481");
            }
            gdsid = GetGdsidByGdsidOrBcd(gdsid.Trim());
            if (string.IsNullOrEmpty(gdsid))
            {
                return RInfo("I0482");
            }

            //得到当天的正常拣货
            var qry1 = from e in WmsDc.wms_cang
                       join e1 in WmsDc.wms_cangdtl on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                       where e.mkedat.Substring(0, 8) == GetCurrentDay()
                       && e.bllid == "103"
                       && e1.gdsid == gdsid.Trim()
                       select new
                       {
                           e.wmsno,
                           e.bllid,
                           e.savdptid,
                           e.prvid,
                           e.qu,
                           e.times,
                           e.lnkbocino,
                           e.lnkbocidat,
                           e.mkr,
                           e.mkedat,
                           e.ckr,
                           e.chkflg,
                           e.chkdat,
                           e.opr,
                           e.brief,
                           e.lnkbllid,
                           e.lnkno,
                           e.lnkbrief,
                           e.mkedat2,
                           e1.rcdidx,
                           e1.oldbarcode,
                           e1.barcode,
                           e1.gdsid,
                           e1.pkgid,
                           e1.pkgqty,
                           e1.qty,
                           e1.gdstype,
                           e1.bthno,
                           e1.vlddat,
                           e1.bcd,
                           e1.tpcode,
                           e1.bkr,
                           e1.bokflg,
                           e1.bokdat,
                           e1.preqty,
                           e1.losreason,
                           rcvdptid = "",
                           checi = "",
                           blltype = "206"
                       };
            //得到当天的摘果拣货
            var qry2 = from e in WmsDc.wms_cang_115
                       join e1 in WmsDc.wms_cangdtl_115 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                       where e.mkedat.Substring(0, 8) == GetCurrentDay()
                       && e1.gdsid == gdsid.Trim()
                       && e.bllid == "115"
                       select new
                       {
                           e.wmsno,
                           e.bllid,
                           e.savdptid,
                           e.prvid,
                           e.qu,
                           e.times,
                           e.lnkbocino,
                           e.lnkbocidat,
                           e.mkr,
                           e.mkedat,
                           e.ckr,
                           e.chkflg,
                           e.chkdat,
                           e.opr,
                           e.brief,
                           e.lnkbllid,
                           e.lnkno,
                           e.lnkbrief,
                           e.mkedat2,
                           e1.rcdidx,
                           e1.oldbarcode,
                           e1.barcode,
                           e1.gdsid,
                           e1.pkgid,
                           e1.pkgqty,
                           e1.qty,
                           e1.gdstype,
                           e1.bthno,
                           e1.vlddat,
                           e1.bcd,
                           e1.tpcode,
                           e1.bkr,
                           e1.bokflg,
                           e1.bokdat,
                           e1.preqty,
                           e1.losreason,
                           e1.rcvdptid,
                           e1.checi,
                           blltype = "115"
                       };
            //合并当天的摘果拣货和正常拣货
            var qrydtl = qry1.Union(qry2);
            var qryblldes = from e in WmsDc.wms_set 
                            where e.setid=="997" && 
                            ((e.typedes=="wms_cang" && e.val1=="lnkbllid" && e.val2=="206" )
                            ||(e.typedes=="wms_cang_115" && e.val1=="bllid" && e.val2=="115"))
                            select e;
            var qry = from e in qrydtl
                      join e1 in WmsDc.wms_pkg on e.gdsid equals e1.gdsid
                      join e2 in qryblldes on e.blltype equals e2.val2
                      join e3 in WmsDc.gds on e.gdsid equals e3.gdsid
                      join e4 in WmsDc.emp on e.bkr equals e4.empid
                      into joinBkr from e5 in joinBkr.DefaultIfEmpty() 
                      join e6 in WmsDc.emp on e.mkr equals e6.empid
                      select new
                      {
                          e.wmsno,
                            e.bllid,
                            blltypedes = e2.brief,
                            e.gdsid,
                            e.mkr,
                            mkrdes = e6.empdes,
                            e3.gdsdes,
                            e3.spc,
                            e3.bsepkg,
                            e1.pkgdes,
                            e1.cnvrto,                            
                            bkremp = e5.empdes,
                            e.barcode,
                            e.bokflg,
                            e.bokdat,
                            e.qty,
                            e.preqty,
                            pkg03 = GetPkgStr(e.qty, e1.cnvrto, e1.pkgdes),
                            pkg03pre = GetPkgStr(e.preqty, e1.cnvrto, e1.pkgdes)
                      };
            if (!string.IsNullOrEmpty(zdr))
            {
                qry = qry.Where(e => e.mkrdes.Contains(zdr) || e.mkr == zdr);
            }

            return RSucc("成功", qry.ToArray(), "S0162");
        }



        protected override void SetModuleInfo()
        {
            Mdlid = "Retrieve";
            Mdldes = "捡货单模块";
        }
    }
}
