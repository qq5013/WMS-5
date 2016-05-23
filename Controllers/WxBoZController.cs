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
    /// 外销播种
    /// </summary>
    public class WxBoZController : SsnController
    {
        /// <summary>
        /// 设置模块信息
        /// </summary>
        protected override void SetModuleInfo()
        {

            Mdlid = "WxBoZ";
            Mdldes = "外销播种模块";
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
        /// 审核播种单
        /// </summary>
        /// <param name="wmsno">播种单单号</param>
        /// <returns></returns>
        /*public ActionResult BokBozBll(String wmsno)
        {
            
            wms_bzmst bzmst = null;
            wms_bzmst[] bzmsts = (from e in WmsDc.wms_bzmst
                                where e.wmsno == wmsno                                
                                select e).ToArray();
            var bzdtls = (from e in WmsDc.wms_bzdtl
                                  join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                                  join e2 in WmsDc.bizdep on new { savdptid=LoginInfo.DefSavdptid, e1.dptid } equals new {e2.savdptid, e2.dptid}
                                  where e.wmsno == wmsno
                                  orderby e1.dptid, e1.gdsid
                                  select new{
                                      e.bcd,
                                      e.bkr,
                                      e.bllid,
                                      e.bokdat,
                                      e.bokflg,
                                      e.brief,
                                      e.bthno,
                                      e.cusid,
                                      e.gdsid,
                                      e.gdstype,
                                      e.pkgid,
                                      e.pkgqty,
                                      e.preqty,
                                      e.qty,
                                      e.rcdidx,
                                      e.rcvdptid,
                                      e.vlddat,
                                      e.wmsno,
                                      e1.dptid,
                                      e2.depid
                                  }).ToArray();
            #region 有效性验证
            //是否找到单据
            if (bzmsts.Length <= 0)
            {
                return RInfo("单号有误请检查");
            }
            bzmst = bzmsts[0];
            //检查该单据是否已经审核，已审核将不能重复审核
            if (bzmst.chkflg == GetY())
            {
                return RInfo("单据已经审核，请不要重复审核");
            }            
            //查看是否有查看该单据的权限
            if (LoginInfo.DatPwrs.Where(w => w.qu == bzmst.qu).Count() <= 0)
            {
                return RInfo("你没有审核该单据的权限");
            }
            //查看明细是否有信息
            if (bzdtls.Length <= 0)
            {
                return RInfo("单据没有明细信息");
            }
            #endregion

            #region 检查播种明细审核的数量，并生成播种损溢单            
            String curFscprdid = GetCurrentFscprdid();  //当前会计期间
            List<stkindtl> lstcangdtl = new List<stkindtl>();
            int i = 0;
            
            foreach (var bzdtl in bzdtls)
            {
                //检查播种单明细
                if (bzdtl.qty > bzdtl.preqty)
                {
                    return RInfo(bzdtl.gdsid + " 的实收" + bzdtl.qty + "大于应收" + bzdtl.preqty);
                }
                //如果播种明细小于应收数量， 生成损溢数量                
                if (bzdtl.qty < bzdtl.preqty)
                {
                    bool? ret = null;
                    //得到损溢单单号
                    String sybllno = (from e in WmsDc.get_wms_sy_bllno(curFscprdid, bzdtl.dptid, bzmst.savdptid)
                                     select e.Column1).Single();
                    WmsDc.get_wms_sy_dtl(curFscprdid, sybllno, bzdtl.dptid, bzmst.savdptid, bzdtl.gdsid, bzdtl.qty - bzdtl.preqty, ref ret);                    
                    i+=0;
                }
            }
            #endregion
            
            ///done: 修改配送单数量 

            //done: 修改审核标记
            return RSucc("成功",null);
        }*/

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
            if (gdsid == null)
            {
                return RInfo("货号无效！");
            }

            String Dat = GetCurrentDay();
            /*
             var qry = from e in WmsDc.sivc
                      from e1 in e.sivcdtl
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      where e.wmsno == wmsno
                      && e.bllid == WMSConst.BLL_TYPE_WXDISPATCH
                      && dpts.Contains(e.dptid)
                      && e.savdptid == LoginInfo.DefSavdptid
                      && e.rcvdptid == rcvdptid                      
                      && e1.gdsid == gdsid                      
                      && e1.rcdidx == rcdidx
                      select e;
            */

            var qry = from e in WmsDc.sivc
                      where e.sivcno == stkouno
                      && e.bllid == WMSConst.BLL_TYPE_WXDISPATCH
                      && dpts.Contains(e.dptid.Trim())
                      && e.savdptid == LoginInfo.DefSavdptid
                      && e.cusid == rcvdptid
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("未找到需要播种的单据");
            }
            var stkotgds = arrqry[0];
            if (wmsno == null)
            {
                wmsno = stkotgds.wmsno;
            }
            if (stkotgds.chkflg == GetY())
            {
                return RInfo("单据已经审核，不能重复播种");
            }
            /*if (stkotgds.bzflg == GetY())
            {
                return RInfo("单据已经播种，不能重复播种");
            }*/
            var qrydtl = from e in stkotgds.sivcdtl
                         where e.gdsid.Trim() == gdsid.Trim() && e.rcdidx == rcdidx
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("未找到需要播种的单据");
            }
            sivcdtl stkdtl = arrqrydtl[0];

            double? preqty = stkdtl.preqty;
            if (stkdtl.preqty == null)              ///如果应收数量为空，就把qty中的数量填入其中
            {
                stkdtl.preqty = stkdtl.qty;
                preqty = stkdtl.qty;
            }
            if (preqty < qty)       //如果实收数量大于应收数量就退出
            {
                return RInfo("实收数量大于应收数量");
            }
            if (preqty != qty)
            {
                GetRealteQuResult qu = GetRealteQu(stkotgds.dptid, LoginInfo.DefSavdptid);
                Log.i(LoginInfo.Usrid, Mdlid, stkotgds.sivcno, stkotgds.bllid, "播种审核",
                    gdsid.Trim() + ":应播:" + preqty + ";实播:" + qty,
                        qu.qu, qu.savdptid);
            }

            #region 检查参数有效性
            if (arrqry == null)
            {
                return RInfo("未找到播种单");
            }
            if (stkdtl == null)
            {

                return RInfo("该播种单，未找到该商品明细");
            }

            //查看该商品是否已经被非本人确认
            if (stkdtl.bzflg == GetY() && stkdtl.bzr != LoginInfo.Usrid)
            {
                return RInfo("该订单已被" + stkdtl.bzr + "确认");
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
                //stkdtl.stotcstamt = Math.Round(qty * stkdtl.stotcstprc, 4);

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
                                 .sivcdtl
                                 .Where(e => e.bzflg == GetY() && Math.Round(e.qty,2,MidpointRounding.AwayFromZero) != 0)
                                 .Count();
                double spreqtycnt = stkotgds
                                    .sivcdtl
                                    .Where(e => Math.Round(e.qty, 2, MidpointRounding.AwayFromZero) != 0)
                                    .Count();
                if (sqtycnt == spreqtycnt)
                {
                    CkBzFlg(stkotgds);

                    //查看有没有明细为空的单据，直接修改播种标记
                    var qryZeroBz = from e in WmsDc.sivcdtl
                                    where e.sivc.wmsno == wmsno && e.sivc.wmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                                    group e by e.sivcno into g
                                    select new
                                    {
                                        sivcno = g.Key,
                                        sqty = g.Sum(e => e.qty)
                                    };
                    qryZeroBz = qryZeroBz.Where(e => e.sqty == 0);
                    var qryZeroBzmst = from e in WmsDc.sivc
                                       join e1 in qryZeroBz on e.sivcno equals e1.sivcno
                                       where e.chkflg != GetY()
                                       select e;
                    foreach (var q in qryZeroBzmst)
                    {
                        CkBzFlg(q);

                        foreach (var d in q.sivcdtl)
                        {
                            d.bzflg = GetY();
                            d.bzdat = GetCurrentDate();
                            d.bzr = LoginInfo.Usrid;
                        }
                    }
                }

                WmsDc.SubmitChanges();
                return RSucc("成功", null);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
            }
        }        

        private void CkBzFlg(sivc q)
        {
            //盘点是否有为空的明细
            var qrydtl = q.sivcdtl.Where(e => e.qty == 0 && e.bzflg == GetN());
            foreach (sivcdtl d in qrydtl)
            {
                d.bzflg = GetY();
                d.bzr = LoginInfo.Usrid;
                d.bzdat = GetCurrentDate();
            }

            WmsDc.SubmitChanges();

            //修改播种标记
            q.bzflg = GetY();
            //审核配送单
            q.chkflg = GetY();
            q.chkdat = GetCurrentDay();
            q.ckr = LoginInfo.Usrid;

            //记账gdsbs
            List<gdsbs> lstGdsbs = new List<gdsbs>();
            foreach (var t in q.sivcdtl)
            {
                gdsbs g = new gdsbs();
                //g.actid = t.actid;
                //g.brief = t.brief;
                g.srcbllno = t.sivcno;
                g.srcrcdidx = t.rcdidx;
                g.fscprdid = GetCurrentFscprdid();
                g.bllid = t.sivc.bllid;
                g.dptid = t.sivc.dptid;
                g.depid = t.depid;
                g.empid = LoginInfo.Usrid;
                g.gdsid = t.gdsid;
                g.actdat = GetCurrentDay();
                g.dbtcrt = '1';
                g.qty = t.qty;
                g.prc = t.prc;
                g.amt = t.amt;
                g.bthno = t.bthno;
                g.vlddat = t.vlddat;
                g.bcd = t.bcd;
                g.mctortrust = t.mctortrust;
                g.prvid = t.prvid;
                g.dlvprc = t.dlvprc;
                g.taxflg = t.taxflg;
                g.branchid = GetBranchid(t.sivc.savdptid);
                lstGdsbs.Add(g);
            }
            WmsDc.gdsbs.InsertAllOnSubmit(lstGdsbs);
            
            stklst astklst = new stklst();
            astklst.stkouno = q.sivcno;
            WmsDc.stklst.InsertOnSubmit(astklst);
        }

        /// <summary>
        /// 播种单摘要信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllSummary(String wmsno)
        {
            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.sivc
                      join e1 in WmsDc.sivcdtl on e.sivcno equals e1.sivcno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      where e.wmsno == wmsno
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_WXDISPATCH
                      && Math.Round(e1.qty, 2, MidpointRounding.AwayFromZero) != 0
                      select new { e, e1, e2 };
            var qrygrpall = from e in qry
                            group e by new { e.e.wmsno, e.e1.gdsid } into g
                            select new { g.Key.wmsno, g.Key.gdsid, ssum = g.Sum(e => e.e1.preqty != null ? e.e1.preqty : e.e1.qty) };
            var qrygrpunbk = from e in qry
                             where (e.e1.bzflg == GetN() || e.e1.bzflg == null)
                             group e by new { e.e.wmsno, e.e1.gdsid } into g
                             select new { g.Key.wmsno, g.Key.gdsid, ssum = g.Sum(e => e.e1.qty) };
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
                return RNoData("未找到该商品的播种信息！");
            }

            return RSucc("成功!", wmsno1);
        }

        /// <summary>
        /// 选择未播种的20条记录
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllTop20(String wmsno)
        {
            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.sivc
                      join e1 in WmsDc.sivcdtl on e.sivcno equals e1.sivcno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.cus on e.cusid equals e3.cusid
                      where e.wmsno == wmsno
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_WXDISPATCH
                      //&& (e1.bzflg == GetN() || e1.bzflg == null)
                      orderby e1.gdsid
                      group e1 by new { e.wmsno, e1.gdsid, e2.gdsdes, e2.spc, e2.bsepkg, e.cusid, e3.cusdes, e1.bzflg } into g
                      select new
                      {
                          g.Key.wmsno,
                          g.Key.gdsid,
                          g.Key.gdsdes,
                          g.Key.spc,
                          g.Key.bsepkg,
                          g.Key.cusid,
                          g.Key.cusdes,
                          g.Key.bzflg,
                          bcd = (from eb in WmsDc.bcd
                                 where eb.gdsid == g.Key.gdsid
                                 select eb.bcd1).Max(),
                          sqty = g.Sum(eqty => Math.Round(eqty.qty, 2, MidpointRounding.AwayFromZero)),
                          sqty1 = g.Sum(eqty => Math.Round(eqty.qty, 2, MidpointRounding.AwayFromZero))
                      };
            qry = qry.Where(e => e.sqty1 > 0);
            var q = from e2 in qry
                    join e3 in WmsDc.v_wms_pkg on new { e2.gdsid } equals new { e3.gdsid }
                    into joinPkg from e4 in joinPkg.DefaultIfEmpty()
                    select new
                    {
                        e2.bcd,e2.bsepkg,e2.bzflg,e2.cusdes,e2.cusid,e2.gdsdes,e2.gdsid,e2.spc,e2.sqty,e2.sqty1,e2.wmsno,
                        pkg03=GetPkgStr(e2.sqty,e4.cnvrto,e4.pkgdes),
                        pkg03pre = GetPkgStr(e2.sqty1, e4.cnvrto, e4.pkgdes),
                    };
            var wmsno1 = qry.OrderBy(e => e.bzflg).OrderBy(e => e.gdsid).Take(20).ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("未找到该商品的播种信息！");
            }

            return RSucc("成功！", wmsno1);
        }

        /// <summary>
        /// 根据商品条码和拣货单号查找播种单
        /// </summary>
        /// <param name="gdsid">条码/货号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllByGdsid(String wmsno, String gdsid)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            if (gdsid == null)
            {
                return RInfo("货号无效！");
            }

            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.sivc
                      join e1 in WmsDc.sivcdtl on e.sivcno equals e1.sivcno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.cus  on e.cusid equals e3.cusid                      
                      where e.wmsno == wmsno
                      && e1.gdsid == gdsid
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_WXDISPATCH
                      && (e.bzflg == GetN() || e.bzflg == null)
                      orderby e.cusid, e1.gdsid, e1.qty
                      select new
                      {
                          stkouno = e.sivcno,
                          rcvdptid = e.cusid,
                          dptdes = e3.cusdes,
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
                          preqty = e1.preqty == null ? e1.qty : e1.preqty
                      };
            qry = qry.Where(e => e.qty > 0);
            var wmsno1 = qry.ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("未找到该商品的播种信息");
            }

            return RSucc("成功", wmsno1);
        }

        /// <summary>
        /// 根据日程查询播种批次单据
        /// </summary>        
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetCurrBoZBll()
        {            
            String Fscprdid = GetCurrentFscprdid();     //得到会计期间
            var qry = from e in WmsDc.sivc
                      join e1 in WmsDc.wms_cang on new { e.wmsno, bllid = e.wmsbllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.cus on e.cusid equals e2.cusid
                      where //e.sivcno.Substring(0, 4) == Fscprdid
                      e.sivcno.StartsWith(Fscprdid)
                      && e.bzflg == GetN() && e.zdflg == GetY() && e1.chkflg == GetY()
                      && e.wmsno != null && e.savdptid == LoginInfo.DefSavdptid
                      && e.wmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_WXDISPATCH
                      group new { e.wmsno, e.wmsbllid, e.zddat, e1.qu, e2.cusid,e2.cusdes } by new { e.wmsno, e.wmsbllid, e.zddat, e1.qu,e2.cusid,e2.cusdes } into g
                      select new
                      {
                          g.Key.wmsno,
                          g.Key.wmsbllid,
                          g.Key.zddat,
                          g.Key.qu,
                          g.Key.cusid,
                          g.Key.cusdes
                      };

            var arrqrymst = qry.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到可播种配送单据");
            }

            return RSucc("成功", arrqrymst);
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
                return RInfo("该波次无商品信息");
            }
            return RSucc("成功", arrqry);
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
                return RInfo("仓位码" + barcode.Trim() + "无效");
            }
            var arrqrymst = FindBllFromCangMst(WMSConst.BLL_TYPE_BZ, begindat, enddat, wmsno, gdsid, barcode);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到符合条件的单据");
            }
            return RSucc("成功", arrqrymst);
        }

    }
}
