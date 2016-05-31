using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;
using System.Transactions;

namespace WMS.Controllers
{
    /// <summary>
    /// 摘果播种拣货
    /// </summary>
    public class FruitPickingRetrieveController : SsnController
    {
        /// <summary>
        /// 是否立即播种
        /// </summary>
        /// <returns></returns>
        private bool ImmBz(String savdptid)
        {
             var qry = from e in WmsDc.wms_set
                       where e.setid=="014" && e.isvld==GetY() && e.val3==savdptid
                       select e;
             var arrqry = qry.ToArray();
             if (arrqry.Length == 0)
             {
                 return false;
             }
             return arrqry[0].val1 == "n" ? false : true;
        }

        /// <summary>
        /// 得到车次信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        public ActionResult GetCheciByWmsno(String wmsno)
        {
            var qry = from e in WmsDc.wms_cang_115
                      join e1 in WmsDc.wms_cangdtl_115 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      where e.bllid==WMSConst.BLL_TYPE_FRUITRETRIEVE && e.wmsno.Trim()==wmsno.Trim()
                      && e1.tpcode == "y"
                      select e1;
            //得到车次有几个门店
            var qrycheci = qry.GroupBy(e => new { e.checi, e.rcvdptid })                           
                .OrderBy(g=>g.Key.checi)
                .Select(g=>new{g.Key.checi, g.Key.rcvdptid, allbokflg=g.Count(), unbokflg=g.Sum(e=>e.bokflg==GetN()?1:0) });
            var arrCheCi = qrycheci.ToArray();
            //车次信息
            var o = arrCheCi
                    .GroupBy(e=>e.checi)                    
                    .Select(e=>new{
                        e.Key,
                        dptcnt = e.Count(),
                        allcomplete = e.Count(a=>a.unbokflg==0)
                    });            

            return RSucc("成功", o, "S0072");
        }

        /// <summary>
        /// 得到车次下面的配送分店信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="checi"></param>
        /// <returns></returns>
        public ActionResult GetDptByWmsnoAndCheci(String wmsno, String checi)
        {
            var qry = from e in WmsDc.wms_cang_115
                      join e1 in WmsDc.wms_cangdtl_115 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.dpt on e1.rcvdptid equals e2.dptid
                      where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE 
                      && e.wmsno.Trim() == wmsno.Trim() && e1.checi.Trim()==checi.Trim()
                      && e1.tpcode=="y"
                      select new{
                          e1.bokflg, e1.rcvdptid, e1.checi, dptdes = e2.dptdes.Trim()
                      };
            //得到车次有几个门店
            var qrycheci = qry.GroupBy(e => new { e.checi, e.rcvdptid, e.dptdes })
                .Select(g => new { g.Key.checi, g.Key.rcvdptid, g.Key.dptdes, allbokflg = g.Count(), unbokflg = g.Sum(e => e.bokflg == GetN() ? 1 : 0) });
            var arrCheci = qrycheci.ToArray();
            return RSucc("成功", arrCheci, "S0073");
        }

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
            var qry = from e in WmsDc.wms_cang_115
                      join e1 in WmsDc.wms_cangdtl_115 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                      && e.lnkbllid == lnkbllid
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && e.mkedat.CompareTo(bdat) >= 0 && e.mkedat.CompareTo(edat) < 0
                      && qus.Contains(e.qu.Trim())
                      && e1.tpcode == "y"
                      //&& !(from ee in WmsDc.wms_bzcnv where ee.lnkbllid == "207" && ee.wmsbllid == "103" && ee.wmsno == e.wmsno select ee).Any()    //不在分货拣货中
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
                qry = from e in WmsDc.wms_cang_115
                      join e1 in WmsDc.cus on e.prvid equals e1.cusid
                      where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
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

            var arrqry = qry.Distinct().ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0083");
            }
            return RSucc("成功", arrqry, "S0074");
        }

        /// <summary>
        /// 得到拣货单明细
        /// </summary>
        /// <param name="wmsno">拣货单号</param>
        /// <param name="lnkbllid">连接订单（为空/206分店拣货单、501外销单）</param>
        /// <param name="checi">车次</param>
        /// <param name="rcvdptid">收货分店</param>
        /// <param name="srt">排序（A顺序/D倒序）</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_拣货查询, pwrdes = "拣货查询")]
        public ActionResult GetRetriveBllDtl(String wmsno, String lnkbllid, String checi, string rcvdptid, string srt)
        {
            if (lnkbllid == null)
            {
                lnkbllid = "206";
            }
            var qus = (from e in LoginInfo.DatPwrs
                       select e.qu).ToArray();
            //分店拣货单
            var qry = from e in WmsDc.wms_cang_115
                      where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
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
                qry = from e in WmsDc.wms_cang_115
                      join e1 in WmsDc.cus on e.prvid equals e1.cusid
                      where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
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
                return RNoData("N0084", wmsno);
            }
            var qrydtl = from e in WmsDc.wms_cangdtl_115
                         join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                         join e2 in
                             WmsDc.v_wms_pkg on new { e1.gdsid } equals new { e2.gdsid }
                         into joinPkg
                         from e3 in joinPkg.DefaultIfEmpty()
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                         && e.checi == checi.Trim() && e.rcvdptid == rcvdptid.Trim()
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
                             pkg03 = GetPkgStr(Math.Round(e.qty, 4, MidpointRounding.AwayFromZero), e3.cnvrto, e3.pkgdes),
                             pkg03pre = GetPkgStr(Math.Round(e.preqty.Value, 4, MidpointRounding.AwayFromZero), e3.cnvrto, e3.pkgdes)
                         };
            //如果是拣货单，需要判断tpcode==GetY(),表示不能修改
            //if (lnkbllid == "206")
            //{
            qrydtl = qrydtl.Where(e => e.tpcode.ToLower() == "y");

            if (srt == "D")
            {
                qrydtl = qrydtl.OrderByDescending(e => e.barcode).OrderBy(e => e.bokflg);
            }
            else
            {
                qrydtl = qrydtl
                     .OrderBy(e => e.barcode).OrderBy(e => e.bokflg);
            }
            //}            
            var arrqrydtl = qrydtl.ToArray();
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0085");
            }

            return RSucc("成功", arrqrydtl, "S0075");
        }

        private void CkBzFlg(stkot p)
        {
            //盘点是否有为空的明细
            var qrydtl = p.stkotdtl.Where(e => e.qty == 0 && e.bzflg == GetN());
            foreach (stkotdtl d in qrydtl)
            {
                d.bzflg = GetY();
                d.bzr = LoginInfo.Usrid;
                d.bzdat = GetCurrentDate();
            }
            WmsDc.SubmitChanges();

            //修改播种标记
            p.bzflg = GetY();
            //审核配送单
            p.chkflg = GetY();
            p.chkdat = GetCurrentDay();
            p.ckr = LoginInfo.Usrid;
            //WmsDc.SubmitChanges();

            //写入dtrlog
            //查看是否dtrlog已经有单据,没有就插入
            var qry = WmsDc.dtrlog
                        .Where(e => e.rcvdptid == p.rcvdptid && e.bllno == p.stkouno && e.bllid == p.bllid)
                        .Select(e => e.bllno);
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                dtrlog dl = new dtrlog();
                dl.bllid = p.bllid;
                dl.bllno = p.stkouno;
                dl.rcvdptid = p.rcvdptid;
                WmsDc.dtrlog.InsertOnSubmit(dl);
            }
            

            if (!(WmsDc.stklst.Where(e => e.stkouno == p.stkouno)).Any())
            {
                stklst astklst = new stklst();
                astklst.stkouno = p.stkouno;
                WmsDc.stklst.InsertOnSubmit(astklst);
                //WmsDc.SubmitChanges();
            }

            //WmsDc.SubmitChanges();
        }

        /// <summary>
        /// 捡货单商品审核
        /// </summary>
        /// <param name="wmsno">收货单单号</param>
        /// <param name="barcode">仓位码</param>
        /// <param name="gdsid">商品编码</param>
        /// <param name="gdstype">商品类型</param>
        /// <param name="qty">实收数量</param>
        /// <param name="rcvdptid">收货分店</param>
        /// <returns>wms_blldtl, wms_blltp</returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_拣货确认, pwrdes = "拣货确认")]
        public ActionResult BokRetrieveGds(String wmsno, String barcode, String gdsid, String gdstype, double qty, string rcvdptid)
        {
            using (TransactionScope scop = new TransactionScope())
            {
                ////正在生成拣货单，请稍候重试
                //string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0123" );
                //}


                //判断是否为散货，不是散货qty不允许为小数
                if(!(from e in WmsDc.cpngds where e.gdsid == gdsid select 1).Any()){
                    if (qty.ToString().IndexOf(".") > 0)
                    {
                        return RInfo( "I0124" );
                    }
                }
                
                //检索主表、明细表
                var qrymst = from e in WmsDc.wms_cang_115
                             where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                             && e.wmsno == wmsno
                             select e;
                var arrmst = qrymst.ToArray();
                wms_cang_115 mst = arrmst[0];
                

                //如果是206的单据，同一个商品的最后一条确认完后就不能再修改
                var qryallbygdsidN1 = from e in WmsDc.wms_cangdtl_115
                                      where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                                      && e.gdsid == gdsid
                                          //&& e.gdstype == gdstype                                    
                                      && e.wmsno == wmsno
                                      && e.rcvdptid.Trim() == rcvdptid.Trim()
                                      && e.bokflg == GetN() && e.tpcode == "y"
                                      select e;

                var qrydtl = from e in WmsDc.wms_cangdtl_115
                             where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                             && e.gdsid.Trim() == gdsid.Trim()
                             && e.gdstype.Trim() == gdstype.Trim()
                             && e.barcode.Trim() == barcode.Trim()
                             && e.wmsno.Trim() == wmsno.Trim()
                             && e.rcvdptid.Trim() == rcvdptid.Trim()
                             && e.tpcode.Trim() == "y"
                             select e;
                int iCnt = qryallbygdsidN1.Count();
                if (mst.lnkbllid.Trim() == "206" && iCnt == 0)
                {
                    return RInfo( "I0125" );
                }                

                var arrdtl = qrydtl.ToArray();

                #region 检查输入参数
                if (arrmst.Length <= 0)
                {
                    return RNoData("N0233");

                }
                if (arrdtl.Length <= 0)
                {
                    return RNoData("N0234");

                }

                if (arrdtl[0].preqty < qty)
                {
                    return RInfo( "I0126" );
                }
                
                wms_cangdtl_115 dtl = arrdtl[0];
                //是否捡货单已经审核
                if (mst.chkflg == GetY())
                {
                    return RInfo("I0435");

                }
                #endregion

                #region 商品登帐
                if (dtl.bokflg == GetY() && dtl.bkr.Trim() != LoginInfo.Usrid)
                {
                    return RInfo("I0436" ,dtl.bkr );

                }

                if (dtl.bokflg == GetN() && dtl.qty != null)
                {
                    //dtl.preqty = dtl.qty; 
                }
                if (dtl.tpcode == "n")
                {
                    return RInfo( "I0127" );
                }
                if (dtl.preqty < qty && dtl.tpcode == "y")
                {
                    return RInfo( "I0128" );
                }
                dtl.qty = Math.Round(qty, 4);
                dtl.pkgqty = Math.Round(qty, 4);
                dtl.bokflg = GetY();
                dtl.bokdat = DateTime.Now.ToString("yyyyMMddHHmmss");
                dtl.bkr = LoginInfo.Usrid;
                WmsDc.SubmitChanges();

                if (dtl.preqty != dtl.qty)
                {
                    //i(wmsno, WMSConst.BLL_TYPE_FRUITRETRIEVE, "拣货商品明细确认", "应拣数量：" + dtl.preqty + "，实拣数量:" + dtl.qty, mst.qu, mst.savdptid);
                }
                #endregion

                #region 如果是206配送拣货的单据，在同一个拣货单里面同一商品确认完后，写入分货表
                if (mst.lnkbllid.Trim() == "206")
                {

                    var qryallbygdsidN = from e in WmsDc.wms_cangdtl_115
                                         where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                                         && e.gdsid == gdsid
                                             //&& e.gdstype == gdstype                                    
                                         && e.wmsno == wmsno
                                          && e.rcvdptid.Trim() == rcvdptid.Trim()
                                         && e.bokflg == GetN() && e.tpcode == "y"
                                         select e;
                    iCnt = qryallbygdsidN.Count();
                    if (iCnt == 0)
                    {
                        var qryAllByGdsidCang = from e in WmsDc.wms_cangdtl_115
                                                join e1 in WmsDc.wms_cang_115 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                                                where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                                                && e.tpcode == "y"
                                                && e.gdsid == gdsid
                                                && e.wmsno == wmsno
                                                 && e.rcvdptid.Trim() == rcvdptid.Trim()
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
                                                    preqty = g.Sum(e => e.qty),
                                                    ckr = "",
                                                    chkflg = GetN(),
                                                    chkdat = "",
                                                    rcvdptid = g.Key.rcvdptid
                                                };
                        #region 如果拣货的数量不够的话,要去修改配送单的数量和金额
                        var cutgds = qryAllByGdsidCang.FirstOrDefault();
                        var qrystkdtl = (from e in WmsDc.stkotdtl
                                        join e1 in WmsDc.v_wms_pkg on e.gdsid equals e1.gdsid
                                        where e.stkot.wmsbllid == cutgds.bllid
                                        && e.stkot.wmsno == cutgds.wmsno
                                        && e.gdsid == cutgds.gdsid
                                        && e.stkot.rcvdptid == rcvdptid.Trim()                                        
                                        orderby e.qty descending
                                        select e).ToArray();
                        double q = qrystkdtl.Sum(e => e.qty) - cutgds.qty;

                        double diff = q;
                        var stkotdtl = qrystkdtl;
                        //减小数部分
                        #region 减小数部分
                        foreach (stkotdtl d in stkotdtl)
                        {
                            if (d.preqty == null)
                            {
                                d.preqty = d.qty;
                            }
                            double xtmp = d.qty * 10000 % 10000 / 10000;
                            if (diff > 0 && diff >= xtmp)
                            {
                                diff -= xtmp;
                                d.qty -= xtmp;

                                d.qty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.pkgqty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.taxamt = Math.Round(d.qty * d.prc * d.taxrto, 4);
                                d.amt = Math.Round(d.qty * d.prc, 4);
                                d.salamt = d.qty * d.salprc;
                                d.patamt = Math.Round(d.qty * d.taxprc, 4);
                                d.stotcstamt = Math.Round(d.qty * d.stotcstprc.Value, 4);
                            }
                            else if (diff > 0 && diff < xtmp)
                            {
                                d.qty -= diff;
                                diff = 0;

                                d.qty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.pkgqty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.taxamt = Math.Round(d.qty * d.prc * d.taxrto, 4);
                                d.amt = Math.Round(d.qty * d.prc, 4);
                                d.salamt = d.qty * d.salprc;
                                d.patamt = Math.Round(d.qty * d.taxprc, 4);
                                d.stotcstamt = Math.Round(d.qty * d.stotcstprc.Value, 4);
                            }
                        }
                        #endregion 减小数部分
                        //减去零散件规
                        #region 减去零散件规
                        v_wms_pkg[] lsPkg = (from e in WmsDc.v_wms_pkg
                                    where stkotdtl.Select(ee => ee.gdsid.Trim()).Contains(e.gdsid)
                                    select e).ToArray();
                        foreach (stkotdtl d in stkotdtl)
                        {
                            if (d.preqty == null)
                            {
                                d.preqty = d.qty;
                            }
                            double xtmp = (double)lsPkg.Where(e => e.gdsid.Trim() == d.gdsid).Select(e => Convert.ToDecimal(d.qty) % Convert.ToDecimal(e.cnvrto)).FirstOrDefault();                            
                            if (diff > 0 && diff >= xtmp)
                            {
                                diff -= xtmp;
                                d.qty -= xtmp;

                                d.qty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.pkgqty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.taxamt = Math.Round(d.qty * d.prc * d.taxrto, 4);
                                d.amt = Math.Round(d.qty * d.prc, 4);
                                d.salamt = d.qty * d.salprc;
                                d.patamt = Math.Round(d.qty * d.taxprc, 4);
                                d.stotcstamt = Math.Round(d.qty * d.stotcstprc.Value, 4);
                            }
                            else if (diff > 0 && diff < xtmp)
                            {
                                d.qty -= diff;
                                diff = 0;

                                d.qty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.pkgqty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.taxamt = Math.Round(d.qty * d.prc * d.taxrto, 4);
                                d.amt = Math.Round(d.qty * d.prc, 4);
                                d.salamt = d.qty * d.salprc;
                                d.patamt = Math.Round(d.qty * d.taxprc, 4);
                                d.stotcstamt = Math.Round(d.qty * d.stotcstprc.Value, 4);
                            }
                        }
                        #endregion 减去零散件规
                        //减去从大到小的数量
                        #region 减去从大到小的数量
                        foreach (stkotdtl d in stkotdtl)
                        {
                            if (d.preqty == null)
                            {
                                d.preqty = d.qty;
                            }
                            if (diff > 0 && diff >= d.qty)
                            {
                                diff -= d.qty;
                                d.qty = 0;

                                d.qty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.pkgqty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.taxamt = Math.Round(d.qty * d.prc * d.taxrto, 4);
                                d.amt = Math.Round(d.qty * d.prc, 4);
                                d.salamt = d.qty * d.salprc;
                                d.patamt = Math.Round(d.qty * d.taxprc, 4);
                                d.stotcstamt = Math.Round(d.qty * d.stotcstprc.Value, 4);
                            }
                            else if (diff > 0 && diff < d.qty)
                            {
                                d.qty = d.qty - diff;
                                diff = 0;

                                d.qty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.pkgqty = Math.Round(d.qty, 4, MidpointRounding.AwayFromZero);
                                d.taxamt = Math.Round(d.qty * d.prc * d.taxrto, 4);
                                d.amt = Math.Round(d.qty * d.prc, 4);
                                d.salamt = d.qty * d.salprc;
                                d.patamt = Math.Round(d.qty * d.taxprc, 4);
                                d.stotcstamt = Math.Round(d.qty * d.stotcstprc.Value, 4);
                            }
                        }
                        WmsDc.SubmitChanges();
                        #endregion 减去从大到小的数量

                        foreach (stkotdtl d in qrystkdtl)
                        {

                            // 是否立即播种
                            if (ImmBz(mst.savdptid))
                            {
                                //修改商品播种标记
                                d.bzflg = GetY();
                                d.bzr = LoginInfo.Usrid;
                                d.bzdat = GetCurrentDate();                                
                            }
                        }
                        WmsDc.SubmitChanges();

                        if (ImmBz(mst.savdptid))
                        {
                            // done: 判断是否该商品的该单据都已经播种完毕，完毕后就修改主单审核标记 
                            // 选出大于0的商品，并且stkdtl中的商品数量大于1个的
                            var qrystkalldtl1 = from e in WmsDc.stkotdtl
                                                     where e.stkot.wmsbllid == cutgds.bllid
                                                     && e.stkot.wmsno == cutgds.wmsno
                                                     && e.stkot.rcvdptid == rcvdptid.Trim()
                                                     && e.qty != 0                         //未0的不取
                                                     group e by e.stkouno into g
                                                     where g.Count(e => e.bzflg == GetN()) == 0
                                                     select g.Key.Trim();
                            // 选出配送单全单都是0的单据
                            var qrystkalldtl2 = from e in WmsDc.stkotdtl 
                                                where e.stkot.wmsbllid == cutgds.bllid
                                                && e.stkot.wmsno == cutgds.wmsno
                                                && e.stkot.rcvdptid == rcvdptid.Trim()                                                     
                                                group e by e.stkouno into g1
                                                where g1.Sum(ee=>ee.qty) == 0
                                                select g1.Key;

                            string[] qrystkalldtl = qrystkalldtl1.Union(qrystkalldtl2).ToArray();

                            var qrystkall = from e in WmsDc.stkot
                                            where qrystkalldtl.Contains(e.stkouno)
                                            && e.rcvdptid == rcvdptid.Trim()
                                            select e;
                            foreach (stkot d in qrystkall)
                            {
                                CkBzFlg(d);
                            }
                            WmsDc.SubmitChanges();

                            //查看有没有明细为空的单据，直接修改播种标记
                            var qryZeroBz = from e in WmsDc.stkotdtl
                                            where e.stkot.wmsno == wmsno && e.stkot.wmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                                            group e by e.stkouno into g
                                            select new
                                            {
                                                stkouno = g.Key,
                                                sqty = g.Sum(e => e.qty)
                                            };
                            qryZeroBz = qryZeroBz.Where(e => e.sqty == 0);
                            var qryZeroBzmst = from e in WmsDc.stkot
                                               join e1 in qryZeroBz on e.stkouno equals e1.stkouno
                                               where e.chkflg != GetY()
                                               select e;
                            foreach (var q1 in qryZeroBzmst)
                            {
                                CkBzFlg(q1);
                                foreach (var dl in q1.stkotdtl)
                                {
                                    dl.bzflg = GetY();
                                    dl.bzdat = GetCurrentDate();
                                    dl.bzr = LoginInfo.Usrid;
                                }
                            }
                            WmsDc.SubmitChanges();

                        }
                        
                        
                        #endregion

                        // 写入分货表
                        #region 写入分货表
                        //// 如果不是残损区的就写分货表
                        //if (!thqus.Contains(mst.qu))
                        //{
                        //    var qryAllByGdsid = from e in WmsDc.stkotdtl
                        //                        join e1 in WmsDc.wms_cang_115 on new { e.stkot.wmsno, e.stkot.wmsbllid } equals new { e1.wmsno, wmsbllid = e1.bllid }
                        //                        where e.stkot.wmsbllid == cutgds.bllid
                        //                        && e.stkot.wmsno == cutgds.wmsno
                        //                        && e.gdsid == cutgds.gdsid
                        //                        && e.qty != 0
                        //                        group e by new
                        //                        {
                        //                            e1.savdptid,
                        //                            e.stkot.rcvdptid,
                        //                            e1.wmsno,
                        //                            e1.bllid,
                        //                            e1.lnkbocino,
                        //                            e1.lnkbocidat,
                        //                            e1.times,
                        //                            e.gdsid
                        //                        } into g
                        //                        select new
                        //                        {
                        //                            savdptid = g.Key.savdptid,
                        //                            wmsno = g.Key.wmsno,
                        //                            bllid = g.Key.bllid,
                        //                            bocino = g.Key.lnkbocino,
                        //                            bocidat = g.Key.lnkbocidat,
                        //                            clsid = g.Key.times,
                        //                            checi = (from e2 in WmsDc.psSndGds_dpt_dtl
                        //                                     where e2.dptid == g.Key.rcvdptid && e2.dh == g.Key.lnkbocino
                        //                                     select e2.busid.Substring(e2.busid.Trim().Length - 1, 1)).FirstOrDefault(),
                        //                            gdsid = g.Key.gdsid,
                        //                            qty = g.Sum(e => e.qty),
                        //                            preqty = g.Sum(e => e.qty),
                        //                            ckr = "",
                        //                            chkflg = GetN(),
                        //                            chkdat = ""
                        //                        };
                        //    //i(wmsno, "", "拣货确认", qryAllByGdsid.ToString(), "", LoginInfo.DefSavdptid);                        
                        //    var arrQryAllByGdsid = qryAllByGdsid.ToArray();
                        //    foreach (var a in arrQryAllByGdsid)
                        //    {
                        //        if (a.checi == null)
                        //        {
                        //            iFile(cutgds.bllid + "    " + cutgds.wmsno + "  " + cutgds.gdsid + "  ");
                        //        }
                        //    }

                        //    var qryAllByGdsidSum = from e in arrQryAllByGdsid
                        //                           group e by new
                        //                           {
                        //                               e.savdptid,
                        //                               e.wmsno,
                        //                               e.bllid,
                        //                               e.bocino,
                        //                               e.bocidat,
                        //                               e.clsid,
                        //                               e.checi,
                        //                               e.gdsid,
                        //                               e.ckr,
                        //                               e.chkflg,
                        //                               e.chkdat
                        //                           } into g
                        //                           select new
                        //                           {
                        //                               g.Key.savdptid,
                        //                               g.Key.wmsno,
                        //                               g.Key.bllid,
                        //                               g.Key.bocino,
                        //                               g.Key.bocidat,
                        //                               g.Key.clsid,
                        //                               g.Key.checi,
                        //                               g.Key.gdsid,
                        //                               g.Key.ckr,
                        //                               g.Key.chkflg,
                        //                               g.Key.chkdat,
                        //                               qty = g.Sum(e => e.qty),
                        //                               preqty = g.Sum(e => e.preqty)
                        //                           };
                        //    List<wms_cutgds> lstCg = new List<wms_cutgds>();
                        //    foreach (var tcg in qryAllByGdsidSum)
                        //    {
                        //        wms_cutgds cg = new wms_cutgds();
                        //        cg.bllid = tcg.bllid;
                        //        cg.bocidat = tcg.bocidat;
                        //        cg.bocino = tcg.bocino;
                        //        cg.checi = tcg.checi;
                        //        cg.chkdat = tcg.chkdat;
                        //        cg.chkflg = tcg.chkflg;
                        //        cg.ckr = tcg.ckr;
                        //        cg.clsid = tcg.clsid;
                        //        cg.gdsid = tcg.gdsid;
                        //        cg.preqty = tcg.preqty;
                        //        cg.qty = tcg.qty;
                        //        cg.savdptid = tcg.savdptid;
                        //        cg.wmsno = tcg.wmsno;
                        //        lstCg.Add(cg);
                        //    }
                        //    WmsDc.wms_cutgds.InsertAllOnSubmit(lstCg);
                        //}
                        #endregion 写入分货表
                    }
                }
                #endregion

                try
                {
                    WmsDc.SubmitChanges();
                    scop.Complete();
                    return RSucc("成功", null, "S0214");

                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0061");

                }
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
            using (TransactionScope scop = new TransactionScope())
            {
                Rm.ResultObject = null;
                //检索捡货单主表、明细表
                var qrymst = from e in WmsDc.wms_cang_115
                             where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                             && e.wmsno == wmsno
                             select e;
                var arrmst = qrymst.ToArray();
                var qrydtl = from e in WmsDc.wms_cangdtl_115
                             where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                             && e.wmsno == wmsno
                             select e;
                var arrdtl = qrydtl.ToArray();

                #region 检查输入参数
                if (arrmst.Length <= 0)
                {
                    return RNoData("N0235");

                }
                if (arrdtl.Length <= 0)
                {
                    return RNoData("N0236");

                }
                wms_cang_115 mst = arrmst[0];
                ////正在生成拣货单，请稍候重试
                //string quRetrv = mst.qu;
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0129" );
                //}

                foreach (wms_cangdtl_115 d in arrdtl)
                {
                    //是否捡货单商品已经审核
                    if (d.bokflg == GetN() && d.tpcode == "y")
                    {
                        return RInfo("I0437" ,d.gdsid );

                    }
                }
                if (mst.chkflg == GetY())
                {
                    return RInfo("I0438");

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
                    return RNoData("N0237");

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
                JsonResult jr = (JsonResult)MakeNewBllNo(this.LoginInfo.DefSavdptid,mst.qu, WMSConst.BLL_TYPE_RETRIEVE_PROFIT_LOSS, (bllno) =>
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
                    foreach (wms_cangdtl_115 dt in arrdtl)
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
                        return RInfo("I0439");

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
                             + "          where b.wmsno=@wmsno and b.wmsbllid='" + WMSConst.BLL_TYPE_FRUITRETRIEVE + "' and a.gdsid=@gdsid order by qty desc, rcdidx \r\n"
                             + "open cur1\r\n"
                             + "fetch next from cur1 into @rcvdptid, @qty, @rcdidx, @stkouno\r\n"
                             + "while @@fetch_status=0\r\n"
                             + "begin\r\n"
                             + "     update stkotdtl set preqty=case when preqty is null then qty else preqty end where stkouno=@stkouno and wmsbllid='" + WMSConst.BLL_TYPE_FRUITRETRIEVE + "' and gdsid=@gdsid and rcvidx=@rcvidx  \r\n"
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
                    return RSucc("成功", null, "S0215");

                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0062");

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
                return RInfo( "I0130",barcode.Trim()  );
            }

            var arrqrymst = FindBllFromCangMst115(begindat, enddat, barcode, bllid, bkr, gdsid);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0086");
            }
            return RSucc("成功", arrqrymst, "S0076");
        }

        protected override void SetModuleInfo()
        {
            Mdlid = "FruitPickingRetrieve";
            Mdldes = "摘果捡货单模块";
        }

    }
}
