using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;
using WMS.Common;
using System.Transactions;

namespace WMS.Controllers
{
    public class CheckedBozController : SsnController
    {
        /// <summary>
        /// 设置模块信息
        /// </summary>
        protected override void SetModuleInfo()
        {

            Mdlid = "CheckedBoz";
            Mdldes = "分货播种模块";
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="requestContext">HTTP请求对象</param>
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
        }

        /// <summary>
        /// 得到当天的波次
        /// </summary>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetCurrBoci()
        {
            string curdat = GetCurrentDay();
            var qry = from e in WmsDc.wms_cutgds
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      where e.bocidat == curdat
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
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
                                  where e1.bocino == g.Key.bocino
                                  && e1.bocidat == g.Key.bocidat
                                  && e1.clsid == g.Key.clsid
                                  group e1 by e1.checi.Trim() into g1
                                  select g1.Key,
                          checkedall = g.Count(e => e.chkflg == GetN()) == 0 ? GetY() : GetN(),
                          bzedall = from e in WmsDc.stkot
                                    join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                                    where e.wmsno == g.Key.wmsno && e.wmsbllid == g.Key.bllid
                                    && e1.bzflg==GetN()
                                    group e1 by new { e.wmsno, e.wmsbllid } into g1
                                    select g1.Count() == 0 ? GetY() : GetN()
                      };

            return RSucc("成功", qry.ToArray(), "S0046");
        }

        /// <summary>
        /// 检查wmsno的单号是否已经审核
        /// </summary>
        /// <param name="bzmst">主单</param>
        /// <returns></returns>
        private bool ChkHasAdt(wms_bzmst bzmst)
        {
            return bzmst.chkflg == GetY();
        }


        /// <summary>
        /// 审核播种商品
        /// </summary>
        /// <param name="wmsno">播种单单号</param>
        /// <param name="stkouno">配送单单号</param>
        /// <param name="gdsid">商品货号</param>      
        /// <param name="rcvdptid">发送分店</param>
        /// <param name="qty">实际播种数量</param>
        /// <param name="rcdidx">配送中单据中的序号</param>
        /// <param name="checi">车次</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种确认, pwrdes = "播种确认")]
        public ActionResult BokBozBllGds(String wmsno, String stkouno, String rcvdptid, String gdsid, double qty, int? rcdidx, String checi)
        {
            using (TransactionScope scop = new TransactionScope())
            {
                // done 删除临时调试日志
                //d(wmsno, WMSConst.BLL_TYPE_DISPATCH, "审核播种商品", "wmsno=" + wmsno + "&stkouno=" + stkouno + "&rcvdptid=" + rcvdptid + "&gdsid=" + gdsid + "&qty=" + qty + "&rcdidx=" + rcdidx + "&checi=" + checi, "", "");

                gdsid = GetGdsidByGdsidOrBcd(gdsid);
                //正在生成拣货单，请稍候重试
                //string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0089" );
                //}

                if (gdsid == null)
                {
                    return RInfo( "I0090" );
                }

                String Dat = GetCurrentDay();

                //检查该单子的分单是否结束(拣货确认一条，就可以播种一条)
                var qryfd = from e in WmsDc.wms_cutgds
                            where e.wmsno == wmsno && e.gdsid == gdsid && e.checi == checi
                            && e.chkflg == GetN()
                            select e;
                foreach (wms_cutgds c in qryfd)
                {
                    return RInfo( "I0091" );
                }

                /*
                 var qry = from e in WmsDc.stkot
                          from e1 in e.stkotdtl
                          join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                          where e.wmsno == wmsno
                          && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                          && dpts.Contains(e.dptid)
                          && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                          && e.rcvdptid == rcvdptid                      
                          && e1.gdsid == gdsid                      
                          && e1.rcdidx == rcdidx
                          select e;
                */

                var qry = from e in WmsDc.stkot
                          join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }
                          join e4 in WmsDc.wms_boci on new { dh = e3.lnkbocino, sndtmd = e3.lnkbocidat, e3.qu } equals new { e4.dh, e4.sndtmd, e4.qu }
                          join e5 in WmsDc.view_pssndgds on new { e4.dh, e4.clsid, e4.sndtmd, e.rcvdptid, e4.qu } equals new { e5.dh, e5.clsid, e5.sndtmd, e5.rcvdptid , e5.qu}
                          where e.stkouno == stkouno
                          && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                          && dpts.Contains(e.dptid.Trim())
                          && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                          && e.rcvdptid == rcvdptid
                          && e5.busid.Trim().Substring(e5.busid.Trim().Length - 1, 1) == checi
                          select e;
                var arrqry = qry.Distinct().ToArray();
                if (arrqry.Length <= 0)
                {
                    return RNoData("N0056");
                }
                var stkotgds = arrqry[0];
                if (wmsno == null)
                {
                    wmsno = stkotgds.wmsno;
                }
                if (stkotgds.chkflg == GetY())
                {
                    return RInfo( "I0092" );
                }
                /*if (stkotgds.bzflg == GetY())
                {
                    return RInfo( "I0093" );
                }*/
                var qrydtl = from e in stkotgds.stkotdtl
                             where e.gdsid.Trim() == gdsid.Trim() && e.rcdidx == rcdidx &&
                             (from e1 in WmsDc.wms_cutgds where e1.wmsno == stkotgds.wmsno && e1.bllid == stkotgds.wmsbllid && e1.gdsid == e.gdsid && e1.checi == checi select e1).Any()
                             select e;
                var arrqrydtl = qrydtl.ToArray();
                if (arrqrydtl.Length <= 0)
                {
                    return RNoData("N0057");
                }
                stkotdtl stkdtl = arrqrydtl[0];
                double? preqty = stkdtl.preqty;
                if (stkdtl.preqty == null)              ///如果应收数量为空，就把qty中的数量填入其中
                {
                    stkdtl.preqty = stkdtl.qty;
                    preqty = stkdtl.qty;
                }
                if (preqty < qty)       //如果实收数量大于应收数量就退出
                {
                    return RInfo( "I0094" );
                }
                if (preqty != qty)
                {
                    GetRealteQuResult qu = GetRealteQu(stkotgds.dptid, LoginInfo.DefSavdptid);
                    Log.i(LoginInfo.Usrid, Mdlid, stkotgds.stkouno, stkotgds.bllid, "播种审核",
                        gdsid.Trim() + ":应播:" + preqty + ";实播:" + qty,
                            qu.qu, qu.savdptid);
                }

                //查看该商品是否已经被非本人确认
                if (stkdtl.bzflg == GetY() && stkdtl.bzr != LoginInfo.Usrid)
                {
                    return RInfo( "I0095",stkdtl.bzr  );
                }

                #region 检查参数有效性
                if (arrqry == null)
                {
                    return RInfo( "I0096" );
                }
                if (stkdtl == null)
                {

                    return RInfo( "I0097" );
                }

                #endregion

                //修改审核标记
                try
                {
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
                    stkdtl.qty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
                    stkdtl.pkgqty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
                    stkdtl.bzdat = GetCurrentDate();
                    stkdtl.bzr = LoginInfo.Usrid;
                    stkdtl.bzflg = GetY();
                    stkdtl.taxamt = Math.Round(qty * stkdtl.prc * stkdtl.taxrto, 4);
                    stkdtl.amt = Math.Round(qty * stkdtl.prc, 4);
                    stkdtl.salamt = qty * stkdtl.salprc;
                    stkdtl.patamt = Math.Round(qty * stkdtl.taxprc, 4);
                    stkdtl.stotcstamt = Math.Round(qty * stkdtl.stotcstprc.Value, 4);

                    //判断改单据是否已经全部商品已经确认，全部确认后的，实收商品总数和应收商品总数相同就直接修改主单的审核标记
                    /*double? sqty = stkotgds
                                    .stkotdtl
                                    .Where(e=>e.bzflg==GetY())
                                    .Sum(e=>e.qty==null?0:e.qty);
                    double? spreqty = stkotgds.stkotdtl.Sum(e=>e.preqty==null?e.qty:e.preqty);
                    if(sqty==spreqty){
                        stkotgds.chkflg = GetY();
                        stkotgds.chkdat = Dat;
                        stkotgds.ckr = LoginInfo.Usrid;
                        stklst astklst = new stklst();
                        astklst.stkouno = stkotgds.stkouno;
                        WmsDc.stklst.InsertOnSubmit(astklst);
                    }*/

                    WmsDc.SubmitChanges();

                    ///如果明细全部播种完
                    ///就修改审核标记
                    ///和播种标记
                    double sqtycnt = stkotgds
                                     .stkotdtl
                                     .Where(e => e.bzflg == GetY() && Math.Round(e.qty, 2, MidpointRounding.AwayFromZero) != 0)
                                     .Count();
                    double spreqtycnt = stkotgds
                                        .stkotdtl
                                        .Where(e => Math.Round(e.qty, 2, MidpointRounding.AwayFromZero) != 0)
                                        .Count();
                    d(wmsno, WMSConst.BLL_TYPE_UPBLL, "审核播种商品", "sqtycnt=" + sqtycnt + "&spreqtycnt=" + spreqtycnt, "", LoginInfo.DefSavdptid);
                    if (sqtycnt == spreqtycnt)
                    {
                        CkBzFlg(stkotgds);

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
                        foreach (var q in qryZeroBzmst)
                        {
                            CkBzFlg(q);
                            foreach (var dtl in q.stkotdtl)
                            {
                                dtl.bzflg = GetY();
                                dtl.bzdat = GetCurrentDate();
                                dtl.bzr = LoginInfo.Usrid;
                            }
                        }

                    }

                    WmsDc.SubmitChanges();
                    scop.Complete();
                    return RSucc("成功", null, "S0047");
                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0013");
                }
            }
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

            stklst astklst = new stklst();
            astklst.stkouno = p.stkouno;
            WmsDc.stklst.InsertOnSubmit(astklst);
        }

        /// <summary>
        /// 播种单摘要信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="checi"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllSummary(String wmsno, String checi)
        {
            String Dat = GetCurrentDay();


            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }
                      join e4 in WmsDc.wms_boci on new { dh = e3.lnkbocino, sndtmd = e3.lnkbocidat, e3.qu } equals new { e4.dh, e4.sndtmd, e4.qu }
                      join e5 in WmsDc.view_pssndgds on new { e4.dh, e4.clsid, e4.sndtmd, e.rcvdptid, e4.qu } equals new { e5.dh, e5.clsid, e5.sndtmd, e5.rcvdptid ,e5.qu}
                      join e6 in WmsDc.dpt on e.rcvdptid equals e6.dptid
                      where e.wmsno == wmsno
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && e5.busid.Trim().Substring(e5.busid.Trim().Length - 1, 1) == checi
                      && Math.Round(e1.qty, 2, MidpointRounding.AwayFromZero) != 0
                      select new { e, e1, e2 };
            var arrqry = qry.ToArray();
            var qrygrpall = from e in arrqry
                            group e by new { e.e.wmsno, e.e1.gdsid } into g
                            select new { g.Key.wmsno, g.Key.gdsid, ssum = g.Sum(e => e.e1.preqty != null ? e.e1.preqty : e.e1.qty) };
            var arrqrygrpall = qrygrpall.ToArray();
            var qrygrpunbk = from e in arrqry
                             where (e.e1.bzflg == GetN() || e.e1.bzflg == null)
                             group e by new { e.e.wmsno, e.e1.gdsid } into g
                             select new { g.Key.wmsno, g.Key.gdsid, ssum = g.Sum(e => e.e1.qty) };
            var arrqrygrpunbk = qrygrpunbk.ToArray();
            var qrygrp = from e in arrqrygrpall
                         join e1 in arrqrygrpunbk on new { e.wmsno, e.gdsid } equals new { e1.wmsno, e1.gdsid }
                         into joinDefunbk
                         from e2 in joinDefunbk.DefaultIfEmpty()
                         group new { e, e2 } by e.wmsno into g
                         select new
                         {
                             wmsno = g.Key,
                             bkcount = g.Count(),
                             unbkcount = g.Count(a => a.e2 != null && a.e2.gdsid != null)
                         };

            /*
            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno                      
                      where e.wmsno == wmsno
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && Math.Round(e1.qty, 2, MidpointRounding.AwayFromZero) != 0
                      group new { e, e1 } by new { e.wmsno } into g
                      select new
                      {
                          g.Key,
                          bkcount = g.Count(),
                          unbkcount = g.Count(a => a.e1.bzflg == GetN() || a.e1.bzflg == null)
                      };
             */
            var wmsno1 = qrygrp.ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0058");
            }

            return RSucc("成功!", wmsno1, "S0048");
        }

        /// <summary>
        /// 选择未播种的20条记录
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllTop20List(String wmsno, string gdsid, int? pageid, int? pagesize, String checi)
        {
            pageid = pageid == null ? 1 : pageid;
            pagesize = pagesize == null ? 20 : pagesize;

            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }
                      join e4 in WmsDc.wms_boci on new { dh = e3.lnkbocino, sndtmd = e3.lnkbocidat , e3.qu} equals new { e4.dh, e4.sndtmd ,e4.qu}
                      join e5 in WmsDc.view_pssndgds on new { e4.dh, e4.clsid, e4.sndtmd, e.rcvdptid, e4.qu } equals new { e5.dh, e5.clsid, e5.sndtmd, e5.rcvdptid, e5.qu }
                      join e6 in WmsDc.dpt on e.rcvdptid equals e6.dptid
                      where e.wmsno == wmsno
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && (e1.bzflg == GetN() || e1.bzflg == null)
                      && e5.busid.Trim().Substring(e5.busid.Trim().Length-1,1)==checi
                      orderby e1.gdsid
                      group e1 by new { e.wmsno, e1.gdsid, e2.gdsdes, e2.spc, e2.bsepkg } into g
                      select new
                      {
                          g.Key.wmsno,
                          g.Key.gdsid,
                          g.Key.gdsdes,
                          g.Key.spc,
                          g.Key.bsepkg,
                          sqty = g.Sum(eqty => eqty.preqty == null ? eqty.qty : eqty.preqty)
                      };
            var q = from e2 in qry
                    join e3 in WmsDc.v_wms_pkg on new { e2.gdsid } equals new { e3.gdsid }
                    into joinPkg
                    from e4 in joinPkg.DefaultIfEmpty()
                    select new
                    {
                        e2.wmsno,
                        e2.gdsid,
                        e2.gdsdes,
                        e2.spc,
                        e2.bsepkg,
                        e2.sqty,
                        pkg03 = GetPkgStr(e2.sqty, e4.cnvrto, e4.pkgdes),
                        pkg03pre = GetPkgStr(e2.sqty, e4.cnvrto, e4.pkgdes)
                    };
            var wmsno1 = q.Take(20).ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0059");
            }

            return RSucc("成功！", wmsno1, "S0049");
        }

        /// <summary>
        /// 选择未播种的20条记录
        /// </summary>
        /// <param name="wmsno">播种单号</param>
        /// <param name="checi">堆(A、B、C...eg.)</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllTop20(String wmsno, String checi)
        {

            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }
                      join e4 in WmsDc.wms_boci on new { dh = e3.lnkbocino, sndtmd = e3.lnkbocidat, e3.qu } equals new { e4.dh, e4.sndtmd,e4.qu }
                      join e5 in WmsDc.view_pssndgds on new { e4.dh, e4.clsid, e4.sndtmd, e.rcvdptid, e4.qu } equals new { e5.dh, e5.clsid, e5.sndtmd, e5.rcvdptid, e5.qu }
                      join e6 in WmsDc.dpt on e.rcvdptid equals e6.dptid
                      join ee3 in WmsDc.v_wms_pkg on new { e2.gdsid } equals new { ee3.gdsid }
                        into joinPkg
                      from e7 in joinPkg.DefaultIfEmpty()
                      where e.wmsno == wmsno
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && e5.busid.Trim().Substring(e5.busid.Trim().Length - 1, 1) == checi
                      //&& (e1.bzflg == GetN() || e1.bzflg == null)                      
                      group e1 by new
                      {
                          e.wmsno,
                          e.wmsbllid,
                          e1.gdsid,
                          e2.gdsdes,
                          e2.spc,
                          e2.bsepkg,
                          e1.bzflg,
                          checi = e5.busid.Trim().Substring(e5.busid.Trim().Length - 1, 1),
                          e7.cnvrto,
                          e7.pkgdes
                      } into g
                      orderby g.Key.bzflg, g.Key.gdsid
                      select new
                      {
                          wmsno = g.Key.wmsno.Trim(),
                          wmsbllid = g.Key.wmsbllid.Trim(),
                          g.Key.gdsid,
                          g.Key.gdsdes,
                          g.Key.spc,
                          g.Key.bsepkg,
                          g.Key.bzflg,
                          g.Key.checi,
                          //bzedall = g.Count(ee => ee.bzflg == GetN()) == 0 ? GetY() : GetN(),
                          sqty = g.Sum(eqty => Math.Round(eqty.qty, 2, MidpointRounding.AwayFromZero)),
                          sqtypre = g.Sum(eqty => Math.Round(eqty.preqty == null ? eqty.qty : eqty.preqty.Value, 2, MidpointRounding.AwayFromZero)),
                          pkg03 = GetPkgStr(g.Sum(eqty => Math.Round(eqty.qty, 2, MidpointRounding.AwayFromZero)), g.Key.cnvrto, g.Key.pkgdes),
                          pkg03pre = GetPkgStr(g.Sum(eqty => Math.Round(eqty.preqty == null ? eqty.qty : eqty.preqty.Value, 2, MidpointRounding.AwayFromZero)), g.Key.cnvrto, g.Key.pkgdes)
                      };
            var arrqry = qry.ToArray();
            //qry = qry.Where(e => e.sqty1 > 0);
            /*var q = from e2 in qry
                    join e3 in WmsDc.pkg on new { e2.gdsid, iscseorspt = '3' } equals new { e3.gdsid, e3.iscseorspt }
                    into joinPkg
                    from e4 in joinPkg.DefaultIfEmpty()
                    group e2 by new
                    {
                        e2.wmsno,
                        e2.wmsbllid,
                        e2.gdsid,
                        e2.gdsdes,
                        e2.spc,
                        e2.bsepkg,
                        e2.checi,
                        e4.cnvrto,
                        e2.bzflg,
                        e4.pkgdes
                    } into g
                    select new
                    {
                        wmsno = g.Key.wmsno.Trim(),
                        gdsid = g.Key.gdsid.Trim(),
                        gdsdes = g.Key.gdsdes.Trim(),
                        spc = g.Key.spc.Trim(),
                        bsepkg = g.Key.bsepkg.Trim(),
                        sqty = g.Sum(ee => ee.sqty),
                        sqtypre = g.Sum(ee => ee.sqtypre),
                        bzedall = g.Count(ee => ee.bzflg == GetN()) == 0 ? GetY() : GetN(),
                        
                        pkg03 = GetPkgStr(g.Sum(ee => ee.sqty), g.Key.cnvrto, g.Key.pkgdes),
                        pkg03pre = GetPkgStr((double)g.Sum(ee => ee.sqtypre), g.Key.cnvrto, g.Key.pkgdes)
                    };*/
            //qry = qry.OrderBy(e => e.bzedall);
            var q = arrqry.Select(e => new
            {
                bzedall = arrqry.Where(ee => ee.bzflg == GetN() && ee.gdsid == e.gdsid && e.checi == ee.checi && e.pkg03 == ee.pkg03).Count() == 0 ? GetY() : GetN(),
                e.bsepkg,
                e.bzflg,
                e.checi,
                e.gdsdes,
                e.gdsid,
                e.pkg03,
                e.pkg03pre,
                e.spc,
                e.sqty,
                e.sqtypre,
                e.wmsbllid,
                e.wmsno
            }).OrderBy(e => e.bzedall);
            var wmsno1 = q.Take(20).ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0060");
                //return RInfo( "I0098" );
            }

            return RSucc("成功！", wmsno1, "S0050");
        }

        /// <summary>
        /// 根据商品条码和拣货单号查找播种单
        /// </summary>
        /// <param name="gdsid">条码/货号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllByGdsid(String wmsno, String gdsid, String checi)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            if (gdsid == null)
            {
                return RInfo( "I0099" );
            }

            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }
                      join e4 in WmsDc.wms_boci on new { dh = e3.lnkbocino, sndtmd = e3.lnkbocidat, e3.qu } equals new { e4.dh, e4.sndtmd, e4.qu }
                      join e5 in WmsDc.view_pssndgds on new { e4.dh, e4.clsid, e4.sndtmd, e.rcvdptid, e4.qu } equals new { e5.dh, e5.clsid, e5.sndtmd, e5.rcvdptid, e5.qu }
                      join e6 in WmsDc.dpt on e.rcvdptid equals e6.dptid
                      join e7 in WmsDc.v_wms_pkg on e2.gdsid equals e7.gdsid
                      where e.wmsno == wmsno
                      && e1.gdsid == gdsid
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && e5.busid.Trim().Substring(e5.busid.Trim().Length - 1, 1) == checi
                      //&& (e.bzflg == GetN() || e.bzflg == null)                      
                      orderby e1.bzflg, e5.busid.Trim().Substring(e5.busid.Trim().Length - 1, 1), e5.busid.Trim().Substring(0, 3),
                      Convert.ToInt32(e5.rcvdptid), e2.gdsid, e1.qty
                      select new
                      {
                          e.stkouno,
                          e.rcvdptid,
                          e.dptid,
                          e.savdptid,
                          e.mkedat,
                          e1.gdsid,
                          e1.rcdidx,
                          e2.gdsdes,
                          e2.spc,
                          e2.bsepkg,
                          e1.pkgqty,
                          qty = Math.Round(e1.qty, 4, MidpointRounding.AwayFromZero),
                          e5.busid,
                          e6.dptdes,
                          e1.bzflg,
                          e1.bzdat,
                          e1.bzr,
                          e7.cnvrto,
                          e7.pkgdes,
                          preqty = e1.preqty == null ? e1.qty : e1.preqty,
                          pkg03 = GetPkgStr(e1.qty, e7.cnvrto, e7.pkgdes),
                          pkg03pre = GetPkgStr(e1.preqty, e7.cnvrto, e7.pkgdes)
                      };
            qry = qry.Where(e => e.qty > 0);
            /*var q = from e2 in qry
                    join e3 in
                        (from m in
                             WmsDc.v_wms_pkg
                         group m by new { m.gdsid, m.cnvrto,  m.pkgdes } into g
                         select g.Key)
                    on new { e2.gdsid } equals new { e3.gdsid }
                    into joinPkg
                    from e4 in joinPkg.DefaultIfEmpty()
                    orderby e2.bzflg, e2.busid.Trim().Substring(e2.busid.Trim().Length - 1, 1), e2.busid.Trim().Substring(0, 3),
                      Convert.ToInt32(e2.rcvdptid), e2.gdsid, e2.qty
                    select new
                    {
                        e2.bsepkg,
                        e2.busid,
                        e2.bzdat,
                        e2.bzflg,
                        e2.bzr,
                        e2.dptdes,
                        e2.dptid,
                        e2.gdsdes,
                        e2.gdsid,
                        e2.mkedat,
                        e2.pkgqty,
                        e2.preqty,
                        e2.qty,
                        e2.rcdidx,
                        e2.rcvdptid,
                        e2.savdptid,
                        e2.spc,
                        e2.stkouno,
                        e4.cnvrto,
                        e4.pkgdes,
                        pkg03 = GetPkgStr(e2.qty, e4.cnvrto, e4.pkgdes),
                        pkg03pre = GetPkgStr(e2.preqty, e4.cnvrto, e4.pkgdes)
                    };*/
            var wmsno1 = qry.ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0061");
            }

            var extObj = wmsno1.GroupBy(e => new { e.gdsid, e.gdsdes, e.cnvrto, e.pkgdes })
                    .Select(ek => new
                    {
                        sqty = ek.Sum(e1 => e1.qty),
                        ek.Key.gdsid,
                        ek.Key.gdsdes,
                        pkg03 = GetPkgStr(ek.Sum(e1 => e1.qty), ek.Key.cnvrto, ek.Key.pkgdes),
                        pkg03pre = GetPkgStr(ek.Sum(e1 => e1.preqty), ek.Key.cnvrto, ek.Key.pkgdes),
                    });

            return RSucc("成功", wmsno1, extObj, "S0051");
        }

        /// <summary>
        /// 根据日程查询播种批次单据
        /// </summary>        
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetCurrBoZBll()
        {
            String Fscprdid = GetCurrentFscprdid();     //得到会计期间
            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.wms_cang on new { e.wmsno, bllid = e.wmsbllid } equals new { e1.wmsno, e1.bllid }
                      where e.stkouno.StartsWith(Fscprdid)
                          //e.mkedat.Substring(0, 8) == GetCurrentDay()
                      && e.bzflg == GetN() && e.zdflg == GetY() && e1.chkflg == GetY()
                      && e.wmsno != null && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && e.wmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                          //&& dpts.Contains(e.dptid.Trim())
                          //&& WmsDc.dpt.Where(ed=>ed.dptid==e.dptid).Any()                      
                          //&& LoginInfo.DatPwrs.Where(et=>et.dptid==e.dptid).Any()
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      group new { e.wmsno, e.wmsbllid, e.zddat, e1.qu, e.dptid } by new { e.wmsno, e.wmsbllid, e.zddat, e1.qu, e.dptid } into g
                      select new
                      {
                          g.Key.wmsno,
                          g.Key.wmsbllid,
                          g.Key.zddat,
                          g.Key.qu,
                          g.Key.dptid
                      };

            var arrqrymst = qry.ToArray();
            var arrqrymst1 = arrqrymst.Where(e => IsExistsPwrByDptidAndQu(e.dptid, e.qu).Any())
                        .GroupBy(e => new { e.wmsno, e.wmsbllid, e.zddat, e.qu }).ToArray();
            if (arrqrymst1.Length <= 0)
            {
                return RNoData("N0062");
            }

            return RSucc("成功", arrqrymst, "S0052");
        }

        /// <summary>
        /// 得到波次的列表
        /// </summary>
        /// <param name="wmsno">播种单号</param>
        /// <returns></returns>
        /*public ActionResult GetBoZDtls(String wmsno)
        {

            var qry = from e in WmsDc.wms_bzmst
                      join e1 in WmsDc.wms_bzdtl on e.wmsno equals e1.wmsno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      where e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e2.dptid)
                      && e.wmsno == wmsno
                      orderby e2.dptid, e1.gdsid
                      select e1;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RInfo( "I0100" );
            }
            return RSucc("成功", arrqry, "S0053");
        }*/


        /// <summary>
        /// 播种查询
        /// </summary>
        /// <param name="bllid">播种单据类型(206配送、501外销、内调112)</param>
        /// <param name="dat">发货日期</param>
        /// <param name="boci">播种波次</param>
        /// <param name="gdsid">商品货号</param>
        /// <param name="rcvdptid">收货部门</param>
        /// <param name="busid">车次号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult FindBll(String bllid, String dat, String boci, String gdsid, String rcvdptid, String busid)
        {
            if (string.IsNullOrEmpty(dat))
            {
                return RInfo( "I0101" );
            }
            var arrqrymst = FindBllFromCangMst107(bllid, dat, boci, gdsid, rcvdptid, busid);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0063");
            }
            return RSucc("成功", arrqrymst, "S0054");
        }
    }
}
