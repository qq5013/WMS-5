using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;
using WMS.Common;

namespace WMS.Controllers
{
    /// <summary>
    /// 仓库内调出库播种
    /// </summary>
    public class AdjustOutBozController : SsnController
    {
        /// <summary>
        /// 设置模块信息
        /// </summary>
        protected override void SetModuleInfo()
        {

            Mdlid = "AdjustOutBoz";
            Mdldes = "仓库内调出库播种模块";
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
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种确认, pwrdes = "播种确认")]
        public ActionResult BokBozBllGds(String wmsno, String stkouno, String rcvdptid, String gdsid, double qty, int? rcdidx)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            ////正在生成拣货单，请稍候重试
            //string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0064" );
            //}

            if (gdsid == null)
            {
                return RInfo( "I0065" );
            }

            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkin
                      from e2 in e.stkindtl
                      join e1 in WmsDc.sftdtl on e.stkinno equals e1.stkinno
                      where //e.stkinno == stkouno && 
                      e.outwmsno == wmsno &&
                      e.bllid == WMSConst.BLL_TYPE_INNERADJ && e.outwmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                      && dpts.Contains(e.dptid.Trim())
                      && savdpts.Contains(e1.sft_sdtout)
                          //&& e1.sft_sdtout == rcvdptid                      
                      && e2.gdsid == gdsid
                      && e.savdptid == rcvdptid
                      select e;
            var arrqry = qry.Distinct().ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0037");
            }
            var stkotgds = arrqry[0];
            if (wmsno == null)
            {
                wmsno = stkotgds.outwmsno;
            }
            if (stkotgds.chkflg == GetY())
            {
                return RInfo( "I0066" );
            }
            /*if (stkotgds.bzflg == GetY())
            {
                return RInfo( "I0067" );
            }*/
            var qrydtl = from e in arrqry
                         from e1 in e.stkindtl
                         where e1.gdsid.Trim() == gdsid.Trim() //&& e.rcdidx == rcdidx
                         select e1;
            var arrqrydtl = qrydtl.ToArray();
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0038");
            }
            var stkdtl = arrqrydtl[0];            

            double? preqty = arrqrydtl.Sum(e=>e.qty);

            if (preqty < qty)       //如果实收数量大于应收数量就退出
            {
                return RInfo( "I0068" );
            }
            if (preqty != qty)
            {
                GetRealteQuResult qu = GetRealteQu(stkotgds.dptid, LoginInfo.DefSavdptid);
                Log.i(LoginInfo.Usrid, Mdlid, stkotgds.stkinno, stkotgds.bllid, "播种审核",
                    gdsid.Trim() + ":应播:" + preqty + ";实播:" + qty,
                        qu.qu, qu.savdptid);
            }

            #region 检查参数有效性
            if (arrqry == null)
            {
                return RInfo( "I0069" );
            }
            if (stkdtl == null)
            {

                return RInfo( "I0070" );
            }

            //查看该商品是否已经被非本人确认
            if (stkdtl.bzflg == GetY() && stkdtl.bzr != LoginInfo.Usrid)
            {
                return RInfo( "I0071",stkdtl.bzr  );
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
                //stkdtl.qty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
                //stkdtl.pkgqty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
                foreach (stkindtl s in arrqrydtl)
                {
                    s.bzdat = GetCurrentDate();
                    s.bzr = LoginInfo.Usrid;
                    s.bzflg = GetY();
                }
                //stkdtl.taxamt = Math.Round(qty * stkdtl.prc * stkdtl.taxrto, 4, MidpointRounding.AwayFromZero);
                //stkdtl.amt = Math.Round(qty * stkdtl.prc, 4);
                //stkdtl.salamt = qty * stkdtl.salprc;
                //stkdtl.patamt = Math.Round(qty * stkdtl.taxprc, 4);
                //stkdtl.stotcstamt = Math.Round(qty * stkdtl.stotcstprc.Value, 4);

                //判断改单据是否已经全部商品已经确认，全部确认后的，实收商品总数和应收商品总数相同就直接修改主单的审核标记
                /*double? sqty = stkotgds
                                .sivcdtl
                                .Where(e=>e.bzflg==GetY())
                                .Sum(e=>e.qty==null?0:e.qty);
                double? spreqty = stkotgds.sivcdtl.Sum(e=>e.preqty==null?e.qty:e.preqty);
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
                                 .stkindtl
                                 .Where(e => e.bzflg == GetY() && Math.Round(e.qty, 2, MidpointRounding.AwayFromZero) != 0)
                                 .Count();
                double spreqtycnt = stkotgds
                                    .stkindtl
                                    .Where(e => Math.Round(e.qty, 2, MidpointRounding.AwayFromZero) != 0)
                                    .Count();
                if (sqtycnt == spreqtycnt)
                {
                    CkBzFlg(stkotgds);

                    //查看有没有明细为空的单据，直接修改播种标记
                    var qryZeroBz = from e in WmsDc.stkindtl
                                    where e.stkin.outwmsno == wmsno && e.stkin.outwmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                                    group e by e.stkinno into g
                                    select new
                                    {
                                        sivcno = g.Key,
                                        sqty = g.Sum(e => e.qty)
                                    };
                    qryZeroBz = qryZeroBz.Where(e => e.sqty == 0);
                    var qryZeroBzmst = from e in WmsDc.stkin
                                       join e1 in qryZeroBz on e.stkinno equals e1.sivcno
                                       where e.chkflg != GetY()
                                       select e;
                    foreach (var q in qryZeroBzmst)
                    {
                        CkBzFlg(q);

                        foreach (var d in q.stkindtl)
                        {                            
                            d.bzflg = GetY();
                            d.bzdat = GetCurrentDate();
                            d.bzr = LoginInfo.Usrid;
                        }
                    }
                }

                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0025");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0009");
            }
        }

        private void CkBzFlg(stkin q)
        {
            //盘点是否有为空的明细
            var qrydtl = q.stkindtl.Where(e => e.qty == 0 && e.bzflg == GetN());
            foreach (stkindtl d in qrydtl)
            {
                d.bzflg = GetY();
                d.bzr = LoginInfo.Usrid;
                d.bzdat = GetCurrentDate();
            }

            WmsDc.SubmitChanges();

            //修改播种标记
            q.outflg = GetY();
            q.outdat = GetCurrentDay();
            //审核配送单
            q.chkflg = GetY();
            q.chkdat = GetCurrentDay();
            q.ckr = LoginInfo.Usrid;

            //记账gdsbs
            //记账gdsbs
            List<gdsbs> lstGdsbs = new List<gdsbs>();
            foreach (var t in q.stkindtl)
            {
                gdsbs g = new gdsbs();
                //g.actid = t.actid;
                //g.brief = t.brief;
                g.srcbllno = t.stkinno;
                g.srcrcdidx = t.rcdidx;
                g.fscprdid = GetCurrentFscprdid();
                g.bllid = t.stkin.bllid;
                g.dptid = t.stkin.dptid;
                g.depid = t.depid;
                g.empid = LoginInfo.Usrid;
                g.gdsid = t.gdsid;
                g.actdat = GetCurrentDay();
                g.dbtcrt = '0';
                g.qty = t.qty;
                g.prc = t.prc.Value;
                g.amt = t.amt.Value;
                g.bthno = t.bthno;
                g.vlddat = t.vlddat;
                g.bcd = t.bcd;
                g.mctortrust = t.mctortrust;
                g.prvid = t.prvid;
                g.dlvprc = t.dlvprc;
                g.taxflg = t.taxflg;
                g.branchid = GetBranchid(t.stkin.savdptid);
                lstGdsbs.Add(g);
            }
            var qrysftdtl = from e in WmsDc.sftdtl
                            where e.stkinno == q.stkinno
                            select e;
            sftdtl sd = qrysftdtl.ToArray()[0];
            foreach (var t in q.stkindtl)
            {
                gdsbs g = new gdsbs();
                //g.actid = t.actid;
                //g.brief = t.brief;
                g.srcbllno = t.stkinno;
                g.srcrcdidx = t.rcdidx;
                g.fscprdid = GetCurrentFscprdid();
                g.bllid = t.stkin.bllid;
                g.dptid = sd.sft_dptout;
                g.depid = sd.sft_depout;
                g.empid = LoginInfo.Usrid;
                g.gdsid = t.gdsid;
                g.actdat = GetCurrentDay();
                g.dbtcrt = '1';
                g.qty = t.qty;
                g.prc = t.prc.Value;
                g.amt = t.amt.Value;
                g.bthno = t.bthno;
                g.vlddat = t.vlddat;
                g.bcd = t.bcd;
                g.mctortrust = t.mctortrust;
                g.prvid = t.prvid;
                g.dlvprc = t.dlvprc;
                g.taxflg = t.taxflg;
                g.branchid = GetBranchid(sd.sft_sdtout);
                lstGdsbs.Add(g);
            }
            WmsDc.gdsbs.InsertAllOnSubmit(lstGdsbs);

            stklst astklst = new stklst();
            astklst.stkouno = q.stkinno;
            WmsDc.stklst.InsertOnSubmit(astklst);
        }

        /// <summary>
        /// 播种单摘要信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllSummary(String wmsno, String savdptid)
        {
            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkin
                      join e1 in WmsDc.stkindtl on e.stkinno equals e1.stkinno                      
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.sftdtl on e.stkinno equals e3.stkinno
                      where e.outwmsno == wmsno                      
                      && e.savdptid == savdptid
                      && savdpts.Contains( e3.sft_sdtout)
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_INNERADJ
                      && Math.Round(e1.qty, 2, MidpointRounding.AwayFromZero) != 0
                      select new { e, e1, e2 };
            var qrygrpall = from e in qry
                            group e by new { e.e.outwmsno, e.e1.gdsid } into g
                            select new { wmsno = g.Key.outwmsno, g.Key.gdsid, ssum = g.Sum(e => e.e1.qty != null ? e.e1.qty : e.e1.qty) };
            var qrygrpunbk = from e in qry
                             where (e.e1.bzflg == GetN() || e.e1.bzflg == null)
                             group e by new { e.e.outwmsno, e.e1.gdsid } into g
                             select new { wmsno = g.Key.outwmsno, g.Key.gdsid, ssum = g.Sum(e => e.e1.qty) };
            var qrygrp = from e in qrygrpall
                         join e1 in qrygrpunbk on new { e.wmsno, e.gdsid } equals new { e1.wmsno, e1.gdsid }
                         into joinDefunbk
                         from e2 in joinDefunbk.DefaultIfEmpty()
                         group new { e, e2 } by e.wmsno into g
                         select new
                         {
                             wmsno = g.Key,
                             bkcount = g.Count(),
                             unbkcount = g.Count(a => a.e2.gdsid != null)
                         };

            var wmsno1 = qrygrp.ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0039");
            }

            return RSucc("成功!", wmsno1, "S0026");
        }

        /// <summary>
        /// 选择未播种的20条记录
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllTop20(String wmsno, String savdptid)
        {
            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkin
                      join e1 in WmsDc.stkindtl on e.stkinno equals e1.stkinno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.sftdtl on e.stkinno equals e3.stkinno
                      where e.outwmsno == wmsno
                      && savdpts.Contains(e3.sft_sdtout)
                      && dpts.Contains(e.dptid.Trim())
                      && e.savdptid==savdptid
                      && e.bllid == WMSConst.BLL_TYPE_INNERADJ
                      //&& (e1.bzflg == GetN() || e1.bzflg == null)
                      group e1 by new { wmsno = e.outwmsno, e1.gdsid, e2.gdsdes, e2.spc, e2.bsepkg, e1.bzflg } into g                      
                      select new
                      {
                          g.Key.wmsno,
                          g.Key.gdsid,
                          g.Key.gdsdes,
                          g.Key.spc,
                          g.Key.bsepkg,
                          g.Key.bzflg,
                          bcd = (from eb in WmsDc.bcd
                                 where eb.gdsid == g.Key.gdsid
                                 select eb.bcd1).Max(),
                          sqty = g.Sum(eqty => Math.Round(eqty.qty, 2, MidpointRounding.AwayFromZero)),
                          sqtypre = g.Sum(eqty => Math.Round(eqty.qty, 2, MidpointRounding.AwayFromZero))                          
                      };
            var q = from e2 in qry
                    join e3 in WmsDc.wms_pkg on new { e2.gdsid } equals new { e3.gdsid }
                    into joinPkg
                    from e4 in joinPkg.DefaultIfEmpty()
                    select new
                    {
                        e2.wmsno,
                        e2.gdsid,
                        e2.gdsdes,
                        e2.spc,
                        e2.bsepkg,
                        e2.bzflg,
                        e2.bcd,
                        e2.sqty,
                        e2.sqtypre,/*
                        bzedall = (from bz in WmsDc.stkindtl
                                   where bz.stkin.outwmsno == wmsno && bz.stkin.bllid == WMSConst.BLL_TYPE_INNERADJ
                                   && bz.bzflg == GetN() && bz.gdsid == e2.gdsid
                                   select bz).Count() == 0 ? GetY() : GetN(),*/
                        e4.cnvrto,
                        pkgdes = e4.pkgdes.Trim(),
                        pkg03 = GetPkgStr(e2.sqty, e4.cnvrto, e4.pkgdes),
                        pkg03pre = GetPkgStr(e2.sqtypre, e4.cnvrto, e4.pkgdes)
                    };
            var q1 = q.ToArray();
            var qrybzedall = (from bz in WmsDc.stkindtl
                                   where bz.stkin.outwmsno == wmsno && bz.stkin.bllid == WMSConst.BLL_TYPE_INNERADJ
                                   && bz.stkin.savdptid == savdptid
                                   && bz.bzflg==GetN() 
                                   group bz by new{ bz.stkin.outwmsno, bz.stkin.bllid, bz.gdsid } into g                                   
                                   select new{
                                       wmsno = g.Key.outwmsno, g.Key.bllid, g.Key.gdsid    
                                   }).ToArray();
            var q2 = from e in q1
                     join e1 in qrybzedall on new { e.wmsno, e.gdsid } equals new { e1.wmsno, e1.gdsid }
                     into Qbzall
                     from e2 in Qbzall.DefaultIfEmpty()
                     select new
                     {
                         e.wmsno,
                         e.gdsid,
                         e.gdsdes,
                         e.spc,
                         e.bsepkg,
                         e.bzflg,
                         e.bcd,
                         e.sqty,
                         e.sqtypre,
                         e.cnvrto,
                         e.pkgdes,
                         e.pkg03,
                         e.pkg03pre,
                         bzedall = e2 == null ? GetY() : GetN()
                     };
            var q3 = q2.Where(e => e.sqtypre > 0).OrderBy(e => e.gdsid).OrderBy(e => e.bzedall);
            var wmsno1 = q3.Take(20).ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0040");
            }

            return RSucc("成功！", wmsno1, "S0027");
        }

        /// <summary>
        /// 根据商品条码和拣货单号查找播种单
        /// </summary>
        /// <param name="gdsid">条码/货号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllByGdsid(String wmsno, String savdptid, String gdsid)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            if (gdsid == null)
            {
                return RInfo( "I0072" );
            }

            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkin
                      join e1 in WmsDc.stkindtl on e.stkinno equals e1.stkinno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid                      
                      join e3 in WmsDc.dpt on e.rcv equals e3.dptid
                      into joinDpt from e3i in joinDpt.DefaultIfEmpty()
                      join e4 in WmsDc.sftdtl on e.stkinno equals e4.stkinno
                      where e.outwmsno == wmsno
                      && e1.gdsid == gdsid
                      && savdpts.Contains(e4.sft_sdtout)
                      && e.savdptid == savdptid
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_INNERADJ
                      && (e.outflg == GetN() || e.outflg == null)
                      orderby e1.gdsid, e1.qty
                      select new
                      {
                          stkouno = e.stkinno,
                          rcvdptid = e4.sft_sdtout,
                          dptdes = e3i.dptdes.Trim(),
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
                          e1.bzflg,
                          e1.bzdat,
                          e1.bzr,
                          preqty = e1.qty == null ? e1.qty : e1.qty
                      };
            qry = qry.Where(e => e.qty > 0);
            var q = from e2 in qry
                    join e3 in WmsDc.wms_pkg on new { e2.gdsid } equals new { e3.gdsid }
                    into joinPkg
                    from e4 in joinPkg.DefaultIfEmpty()
                    select new
                    {
                        e2.bsepkg,
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
                        pkgdes = e4.pkgdes.Trim(),
                        pkg03 = GetPkgStr(e2.qty, e4.cnvrto, e4.pkgdes),
                        pkg03pre = GetPkgStr(e2.preqty, e4.cnvrto, e4.pkgdes)
                    };
            var wmsno1 = q.ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0041");
            }

            return RSucc("成功", wmsno1, "S0028");
        }

        /// <summary>
        /// 根据日程查询播种批次单据
        /// </summary>        
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetCurrBoZBll(String ckflg)
        {
            String Fscprdid = GetCurrentFscprdid();     //得到会计期间
            var qry = from e in WmsDc.stkin
                      join e1 in WmsDc.wms_cang on new { wmsno = e.outwmsno, bllid = e.outwmsbllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.sftdtl on e.stkinno equals e2.stkinno
                      join e3 in WmsDc.emp on e1.mkr equals e3.empid
                      join e4 in WmsDc.dpt on e.savdptid equals e4.dptid
                      where //e.sivcno.Substring(0, 4) == Fscprdid
                      e.mkedat.Substring(2, 4) == Fscprdid
                          //e.mkedat.Substring(0,8) == GetCurrentDay()
                      && e.outflg == GetN() && e.outzdflg == GetY() && e1.chkflg == GetY()
                      && e.outwmsno != null && savdpts.Contains(e2.sft_sdtout.Trim())
                      && e.outwmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_INNERADJ
                      && !(from ebz in WmsDc.wms_bzcnv
                           where ebz.sndtmd.Substring(2, 4) == Fscprdid && ebz.savdptid == LoginInfo.DefSavdptid
                               && ebz.wmsno == e1.wmsno && ebz.wmsbllid == e1.bllid
                           select ebz).Any()
                      group new { e.outwmsno, e.outwmsbllid, e.outzddat, e1.qu, e3.empdes, e1.chkflg, e.savdptid, e4.dptdes } by new { e.outwmsno, e.outwmsbllid, e.outzddat, e1.qu, e3.empdes, e1.chkflg, e.savdptid, e4.dptdes } into g
                      select new
                      {
                          wmsno = g.Key.outwmsno,
                          wmsbllid = g.Key.outwmsbllid,
                          zddat = g.Key.outzddat,
                          g.Key.qu,
                          g.Key.chkflg,
                          mkrdes = g.Key.empdes.Trim(),
                          savdptid = g.Key.savdptid.Trim(),
                          savdptdes = g.Key.dptdes.Trim()
                      };
            if (string.IsNullOrEmpty(ckflg))
            {
                qry = qry.Where(e => e.chkflg == 'y');
            }
            
            var arrqrymst = qry.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0042");
            }

            return RSucc("成功", arrqrymst, "S0029");
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
                      where (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && dpts.Contains(e2.dptid)
                      && e.wmsno == wmsno
                      orderby e2.dptid, e1.gdsid
                      select e1;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RInfo( "I0073" );
            }
            return RSucc("成功", arrqry, "S0030");
        }*/


        /// <summary>
        /// 仓位调整查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="wmsno">调整单单号</param>
        /// <param name="gdsid">商品货号、条码</param>
        /// <param name="barcode">仓位</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult FindBll(String begindat, String enddat, String wmsno, String gdsid, String barcode)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo( "I0074",barcode.Trim()  );
            }
            var arrqrymst = FindBllFromCangMst(WMSConst.BLL_TYPE_BZ, begindat, enddat, wmsno, gdsid, barcode);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0043");
            }
            return RSucc("成功", arrqrymst, "S0031");
        }

    }
}
