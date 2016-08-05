using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Transactions;
using WMS.Models;

namespace WMS.Controllers
{
    public class SelectedRetrieveController : SsnController
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
            if (edat == null)
            {
                edat = GetNextDay();
            }

            //分店拣货单
            var qry = from e in WmsDc.wms_cang
                      where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                      && e.lnkbllid == lnkbllid
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && e.mkedat.CompareTo(bdat) >= 0 && e.mkedat.CompareTo(edat) < 0
                      && qus.Contains(e.qu.Trim())
                      && (from ee in WmsDc.wms_bzcnv where ee.lnkbllid=="207" && ee.wmsbllid=="103" && ee.wmsno==e.wmsno select ee).Any()
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
            if (arrqry.Length <= 0)
            {
                return RNoData("N0187");
            }
            return RSucc("成功", arrqry, "S0172");
        }

        /// <summary>
        /// 得到拣货单明细
        /// </summary>
        /// <param name="wmsno">拣货单号</param>
        /// <param name="lnkbllid">连接订单（为空/206分店拣货单、501外销单）</param>
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
                return RNoData("N0188", wmsno);
            }
            var qrydtl1 = from e in WmsDc.wms_cangdtl
                         join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                         join e2 in WmsDc.wms_pkg on new { e1.gdsid } equals new { e2.gdsid }
                         into joinPkg
                         from e3 in joinPkg.DefaultIfEmpty()
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
                             pkg03 = GetPkgStr(Math.Round(e.qty, 4, MidpointRounding.AwayFromZero), e3.cnvrto, e3.pkgdes),
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
                if (qrydtl.Where(e => e.bokflg == GetN() && e.tpcode=="y").Count() == 0)
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
                return RNoData("N0171");   // 该拣货单未找到符合条件的明细信息
            }

            if (!string.IsNullOrEmpty(barcode) )
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
            }, "S0173");
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
                var qrydtl = from e in WmsDc.wms_cangdtl
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.gdsid == gdsid.Trim()
                             && e.barcode == barcode.Trim()
                             && e.wmsno == wmsno.Trim()
                             && e.tpcode == "y"
                             //&& e.bokflg == 'n'
                             orderby e.gdstype, e.vlddat, e.bthno
                             select e;
                var arrdtl = qrydtl.ToArray();
                double diff = arrdtl.Where(e => e.bokflg == 'n').Sum(e => e.qty) - qty;
                if (arrdtl.Where(e => e.bokflg == 'y').Any())
                {
                    if (diff < 0)
                    {
                        string[] whoAdt = (from e in WmsDc.emp
                                           where arrdtl.Where(ee => ee.bokflg == 'y').Select(ee => ee.bkr).Contains(e.empid)
                                           select e.empdes.Trim()).ToArray();
                        return RInfo("I0492", string.Join(",", whoAdt),
                            arrdtl.Where(e => e.bokflg == 'y').Sum(e => e.qty),
                            arrdtl.Where(e => e.bokflg == 'n').Sum(e => e.qty));
                    }
                }
                else if(diff<0)
                {
                    return RInfo("I0480");
                }
                //得到拣货的差异数
                
                //如果差异数为负数，说明拣货数量大于应拣数量，不通过
                /*if (diff < 0)
                {
                    return RInfo("I0480");   //todo:
                }*/
                //循环审核拣货明细
                foreach (wms_cangdtl dtl in arrdtl.Where(e=>e.bokflg=='n'))
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

                //检索主表、明细表
                var qrymst = from e in WmsDc.wms_cang
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.wmsno == wmsno
                             select e;
                var arrmst = qrymst.ToArray();
                var qrydtl = from e in WmsDc.wms_cangdtl
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                             && e.gdsid == gdsid.Trim()
                             && e.gdstype == gdstype.Trim()
                             && e.bthno == bthno.Trim()
                             && e.vlddat == vlddat.Trim()
                             && e.barcode == barcode.Trim()
                             && e.wmsno == wmsno
                             select e;
                var arrdtl = qrydtl.ToArray();

                #region 检查输入参数
                if (arrmst.Length <= 0)
                {
                    return RNoData("N0248");

                }
                if (arrdtl.Length <= 0)
                {
                    return RNoData("N0249");

                }
                wms_cang mst = arrmst[0];
                ////正在生成拣货单，请稍候重试
                //string quRetrv = mst.qu;
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0391" );
                //}
                wms_cangdtl dtl = arrdtl[0];
                //是否捡货单已经审核
                if (mst.chkflg == GetY())
                {
                    return RInfo("I0460");

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
                    return RInfo("I0461" ,dtl.bkr );

                }

                if (dtl.bokflg == GetN() && dtl.qty != null)
                {
                    dtl.preqty = dtl.qty;
                }
                // 确认数量大于应拣数量
                if (qty > dtl.preqty)
                {
                    return RInfo("i0473");
                }
                dtl.qty = Math.Round(qty, 4);
                dtl.pkgqty = Math.Round(qty, 4);

                dtl.bokflg = GetY();
                dtl.bokdat = DateTime.Now.ToString("yyyyMMddHHmmss");
                dtl.bkr = LoginInfo.Usrid;
                #endregion

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
                    return RSucc("成功", null, "S0224");

                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0072");

                }
            
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
                    return RNoData("N0250");

                }
                if (arrdtl.Length <= 0)
                {
                    return RNoData("N0251");

                }
                wms_cang mst = arrmst[0];
                ////正在生成拣货单，请稍候重试
                //string quRetrv = mst.qu;
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0392" );
                //}
                foreach (wms_cangdtl d in arrdtl)
                {
                    //是否捡货单商品已经审核
                    if (d.bokflg == GetN() && d.tpcode == "y")
                    {
                        return RInfo("I0462" ,d.gdsid );

                    }
                }
                if (mst.chkflg == GetY())
                {
                    return RInfo("I0463");

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
                    return RNoData("N0252");

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
                var arrdtly = arrdtl
                    .Where(e => e.tpcode == "y")
                    .Select(e => new { e.gdsid, e.gdstype, e.qty });
                var arrdtln = arrdtl
                    .Where(e => e.tpcode == "n")
                    .Select(e => new { e.gdsid, e.gdstype, qty = 0.00 });
                var qsum = arrdtly.Union(arrdtln)
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
                        return RInfo("I0464");

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
                        if (mst.lnkbllid.Trim() == "206" || mst.lnkbllid.Trim() == "501")
                        {
                            cmdsql = "declare @remainqty deciaml(18,4)\r\n"
                             + "declare @wmsno varchar(20), @gdstype varchar(20), @rcdidx int, @gdsid varchar(10), @rcvdptid varchar(10), @qty decimal, @stkouno varchar(30)\r\n"
                             + "set @remainqty={0}\r\n"
                             + "set @gdsid={1}\r\n"
                             + "set @wmsno={2}\r\n"
                             + "declare cur1 cursor for\r\n"
                             + "    select b.rcvdptid, a.qty, a.rcdidx, b.stkouno from stkotdtl a inner join stkot b on a.stkouno=b.stkouno\r\n"
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
                            WmsDc.ExecuteCommand(cmdsql, new object[] { remainqty, q.gdsid, bzmst.wmsno });
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
                    return RSucc("成功", null, "S0225");

                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0073");

                    
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
        public ActionResult FindBll(String begindat, String enddat, String barcode, String bllid, String bkr, String gdsid)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo( "I0393",barcode.Trim()  );
            }

            var arrqrymst = FindBllFromCangMst103(begindat, enddat, barcode, bllid, bkr, gdsid);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0190");
            }
            return RSucc("成功", arrqrymst, "S0174");
        }

        protected override void SetModuleInfo()
        {
            Mdlid = "Retrieve";
            Mdldes = "捡货单模块";
        }

    }
}
