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
    /// <summary>
    /// 勾单播种
    /// </summary>
    public class SelectedBozController : SsnController
    {
        /// <summary>
        /// 设置模块信息
        /// </summary>
        protected override void SetModuleInfo()
        {

            Mdlid = "SelectedBoz";
            Mdldes = "勾单播种模块";
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
        public ActionResult BokBozBllGds(String wmsno,/* String stkouno,*/ String rcvdptid, String gdsid, double qty/*, int? rcdidx*/)
        {
            /*
             * 1、得到wmsno、rcvdptid对应的所有单据
             * 2、得到wmsno、rcvdptid和gdsid对应的所有明细
             * 3、判断这些主单是否已经有一张以上的单据已经审核
             * 4、判断该商品是否是本人播种             
             * 5、判断这些单据的所有明细是否都已经播种完毕，播种完毕后就修改单据的审核标记，包括修改无货商品的数量
             */
            using (TransactionScope scop = new TransactionScope())          // 事务逻辑开始
            {
            // 得到货号
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            ////正在生成拣货单，请稍候重试
            //string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0375" );
            //}
            if (gdsid == null)  //货号为空，表示未找到该商品
            {
                return RInfo( "I0376" );
            }
            String Dat = GetCurrentDay();   //得到当前日期

            
                try
                {
                    #region 1、得到wmsno、rcvdptid、gdsid对应的所有单据

                    //得到wmsno、rcvdptid、gdsid对应的所有单据
                    stkot[] stkots = GetStkots(wmsno, rcvdptid, gdsid);

                    //未找到单据
                    if (stkots.Length == 0)
                    {
                        return RNoData("N0177");
                    }
                    #endregion 1、得到wmsno、rcvdptid对应的所有单据

                    #region 2、得到wmsno、rcvdptid和gdsid对应的所有明细
                    string[] stkounos = stkots.Select(e => e.stkouno.Trim()).ToArray();
                    //得到相关单据号和商品编码对应的明细
                    stkotdtl[] stkotdtls = GetStkdtlsByStkounos(wmsno, gdsid, stkounos).OrderByDescending(e => e.preqty).ToArray();

                    //未找到对应的明细信息
                    if (stkotdtls.Length == 0)
                    {
                        return RNoData("N0178");
                    }
                    //判断preqty是否为null，为null就填qty
                    foreach (stkotdtl dtl in stkotdtls)
                    {
                        if (dtl.preqty == null)
                        {
                            dtl.preqty = dtl.qty;
                        }
                    }
                    WmsDc.SubmitChanges();
                    #endregion 2、得到wmsno、rcvdptid和gdsid对应的所有明细

                    #region 3、判断这些主单是否已经有一张以上的单据已经审核
                    foreach (stkot s in stkots)
                    {
                        if (s.chkflg == GetY())
                        {
                            return RInfo( "I0377",s.stkouno  );
                        }
                    }
                    #endregion 3、判断这些主单是否已经有一张以上的单据已经审核

                    #region 4、判断该商品是否是本人播种
                    //查看该商品是否已经被非本人确认
                    foreach (stkotdtl stkdtl in stkotdtls)
                    {
                        if (stkdtl.bzflg == GetY() && stkdtl.bzr != LoginInfo.Usrid)
                        {
                            return RInfo( "I0378",stkdtl.bzr  );
                        }
                    }
                    #endregion 4、判断该商品是否是本人播种

                    #region 修改这些明细的播种标记
                    //计算这些单据的应该播种数量是否大于实播种数量
                    #region 计算这些单据的应该播种数量是否大于实播种数量
                    double? preqty = stkotdtls.Sum(e => e.preqty);
                    if (preqty < qty)
                    { //如果实际播种数量大于应播数量就退出，不能继续
                        return RInfo( "I0379",qty ,preqty );
                    }
                    else if (preqty > qty)
                    {   //如果实际播种数量小于应播数量，就修改实际的播种数量
                        double difQty = preqty.Value - qty;   //得到差异数量
                        foreach (stkotdtl dtl in stkotdtls)
                        {
                            if (difQty > 0)
                            {
                                if (dtl.preqty >= difQty)
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
                                    double qty1 = dtl.preqty.Value - difQty;
                                    dtl.qty = Math.Round(qty1, 4, MidpointRounding.AwayFromZero);
                                    dtl.pkgqty = Math.Round(qty1, 4, MidpointRounding.AwayFromZero);
                                    dtl.taxamt = Math.Round(qty * dtl.prc * dtl.taxrto, 4);
                                    dtl.amt = Math.Round(qty * dtl.prc, 4);
                                    dtl.salamt = qty * dtl.salprc;
                                    dtl.patamt = Math.Round(qty * dtl.taxprc, 4);
                                    dtl.stotcstamt = Math.Round(qty * dtl.stotcstprc.Value, 4);
                                    difQty = 0;

                                    //记录差异
                                    GetRealteQuResult qu = GetRealteQu(dtl.stkot.dptid, LoginInfo.DefSavdptid);
                                    Log.i(LoginInfo.Usrid, Mdlid, dtl.stkot.stkouno, dtl.stkot.bllid, "播种审核",
                                        gdsid.Trim() + ":应播:" + dtl.preqty.Value + ";实播:" + qty1,
                                            qu.qu, qu.savdptid);                                    
                                }
                                else if (dtl.preqty < difQty)
                                {
                                    dtl.qty = 0;
                                    dtl.pkgqty = 0;
                                    dtl.taxamt = 0;
                                    dtl.amt = 0;
                                    dtl.salamt = 0;
                                    dtl.patamt = 0;
                                    dtl.stotcstamt = 0;
                                    difQty -= dtl.preqty.Value;

                                    //记录差异
                                    GetRealteQuResult qu = GetRealteQu(dtl.stkot.dptid, LoginInfo.DefSavdptid);
                                    Log.i(LoginInfo.Usrid, Mdlid, dtl.stkot.stkouno, dtl.stkot.bllid, "播种审核",
                                        gdsid.Trim() + ":应播:" + dtl.preqty.Value + ";实播:" + 0,
                                            qu.qu, qu.savdptid);
                                }
                            }
                        }

                        WmsDc.SubmitChanges();
                    }
                    #endregion 计算这些单据的应该播种数量是否大于实播种数量

                    //修改稿明细的播种标记
                    #region 修改稿明细的播种标记
                    foreach (stkotdtl dtl in stkotdtls)
                    {
                        dtl.bzdat = GetCurrentDate();
                        dtl.bzr = LoginInfo.Usrid;
                        dtl.bzflg = GetY();
                    }
                    WmsDc.SubmitChanges();
                    #endregion 修改稿明细的播种标记
                    #endregion 修改这些明细的播种标记

                    #region 5、判断这些单据的所有明细是否都已经播种完毕，播种完毕后就修改单据的审核标记，包括修改无货商品的数量
                    //判断这些单据的所有明细是否都已经播种完毕
                    foreach (stkot s in stkots)
                    {
                        bool hasAllBz = !(from e1 in WmsDc.stkotdtl
                                          where e1.stkouno == s.stkouno && e1.bzflg == GetN() && e1.qty > 0
                                          select 1).Any();
                        if (hasAllBz)       //如果都已经播种完了
                        {
                            CkBzFlg(s);
                            i(s.stkouno, s.bllid, "stkouno:" + s.stkouno + ", chkflg:" + s.chkflg + ", bzflg:" + s.bzflg, "", "", "");
                        }
                    }
                    WmsDc.SubmitChanges();
                    scop.Complete();
                    return RSucc("成功", null, "S0163");
                    #endregion 5、判断这些单据的所有明细是否都已经播种完毕，播种完毕后就修改单据的审核标记，包括修改无货商品的数量

                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0050");
                }
            }                                                           // 事务逻辑结束

            #region 老的播种逻辑
            //using (TransactionScope scop = new TransactionScope())
            //{
            //    gdsid = GetGdsidByGdsidOrBcd(gdsid);
            //    if (gdsid == null)
            //    {
            //        return RInfo( "I0380" );
            //    }

            //    String Dat = GetCurrentDay();

            //    var qry = from e in WmsDc.stkot
            //              where e.stkouno == stkouno
            //              && e.bllid == WMSConst.BLL_TYPE_DISPATCH
            //              && dpts.Contains(e.dptid.Trim())
            //              && e.savdptid == LoginInfo.DefSavdptid
            //              && e.rcvdptid == rcvdptid
            //              select e;
            //    var arrqry = qry.ToArray();
            //    if (arrqry.Length <= 0)
            //    {
            //        return RNoData("N0179");
            //    }
            //    var stkotgds = arrqry[0];
            //    if (wmsno == null)
            //    {
            //        wmsno = stkotgds.wmsno;
            //    }
            //    if (stkotgds.chkflg == GetY())
            //    {
            //        return RInfo( "I0381" );
            //    }
            //    /*if (stkotgds.bzflg == GetY())
            //    {
            //        return RInfo( "I0382" );
            //    }*/
            //    var qrydtl = from e in stkotgds.stkotdtl
            //                 where e.gdsid.Trim() == gdsid.Trim() && e.rcdidx == rcdidx
            //                 select e;
            //    var arrqrydtl = qrydtl.ToArray();
            //    if (arrqrydtl.Length <= 0)
            //    {
            //        return RNoData("N0180");
            //    }
            //    stkotdtl stkdtl = arrqrydtl[0];
            //    double? preqty = stkdtl.preqty;
            //    if (stkdtl.preqty == null)              ///如果应收数量为空，就把qty中的数量填入其中
            //    {
            //        stkdtl.preqty = stkdtl.qty;
            //        preqty = stkdtl.qty;
            //    }
            //    if (preqty < qty)       //如果实收数量大于应收数量就退出
            //    {
            //        return RInfo( "I0383" );
            //    }
            //    if (preqty != qty)
            //    {
            //        GetRealteQuResult qu = GetRealteQu(stkotgds.dptid, LoginInfo.DefSavdptid);
            //        Log.i(LoginInfo.Usrid, Mdlid, stkotgds.stkouno, stkotgds.bllid, "播种审核",
            //            gdsid.Trim() + ":应播:" + preqty + ";实播:" + qty,
            //                qu.qu, qu.savdptid);
            //    }

            //    //查看该商品是否已经被非本人确认
            //    if (stkdtl.bzflg == GetY() && stkdtl.bzr != LoginInfo.Usrid)
            //    {
            //        return RInfo( "I0384",stkdtl.bzr  );
            //    }

            //    #region 检查参数有效性
            //    if (arrqry == null)
            //    {
            //        return RInfo( "I0385" );
            //    }
            //    if (stkdtl == null)
            //    {

            //        return RInfo( "I0386" );
            //    }

            //    #endregion

            //    //修改审核标记
            //    try
            //    {
            //        /*
            //            * preqty = preqty==null ? qty : preqty
            //            * 
            //            * 公式：taxamt = qty*prc*taxrto
            //            * amt = qty*prc
            //            * salamt = qty*salprc
            //            * patamt = qty*taxprc
            //            * stotcstamt = qty*stotcstprc
            //            * 
            //        */
            //        stkdtl.qty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            //        stkdtl.pkgqty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            //        stkdtl.bzdat = GetCurrentDate();
            //        stkdtl.bzr = LoginInfo.Usrid;
            //        stkdtl.bzflg = GetY();
            //        stkdtl.taxamt = Math.Round(qty * stkdtl.prc * stkdtl.taxrto, 4);
            //        stkdtl.amt = Math.Round(qty * stkdtl.prc, 4);
            //        stkdtl.salamt = qty * stkdtl.salprc;
            //        stkdtl.patamt = Math.Round(qty * stkdtl.taxprc, 4);
            //        stkdtl.stotcstamt = Math.Round(qty * stkdtl.stotcstprc.Value, 4);

            //        //判断改单据是否已经全部商品已经确认，全部确认后的，实收商品总数和应收商品总数相同就直接修改主单的审核标记
            //        /*double? sqty = stkotgds
            //                        .stkotdtl
            //                        .Where(e=>e.bzflg==GetY())
            //                        .Sum(e=>e.qty==null?0:e.qty);
            //        double? spreqty = stkotgds.stkotdtl.Sum(e=>e.preqty==null?e.qty:e.preqty);
            //        if(sqty==spreqty){
            //            stkotgds.chkflg = GetY();
            //            stkotgds.chkdat = Dat;
            //            stkotgds.ckr = LoginInfo.Usrid;
            //            stklst astklst = new stklst();
            //            astklst.stkouno = stkotgds.stkouno;
            //            WmsDc.stklst.InsertOnSubmit(astklst);
            //        }*/

            //        WmsDc.SubmitChanges();

            //        ///如果明细全部播种完
            //        ///就修改审核标记
            //        ///和播种标记
            //        double sqtycnt = stkotgds
            //                         .stkotdtl
            //                         .Where(e => e.bzflg == GetY() && Math.Round(e.qty, 2, MidpointRounding.AwayFromZero) != 0)
            //                         .Count();
            //        double spreqtycnt = stkotgds
            //                            .stkotdtl
            //                            .Where(e => Math.Round(e.qty, 2, MidpointRounding.AwayFromZero) != 0)
            //                            .Count();
            //        d(wmsno, WMSConst.BLL_TYPE_UPBLL, "审核播种商品", "sqtycnt=" + sqtycnt + "&spreqtycnt=" + spreqtycnt, "", LoginInfo.DefSavdptid);
            //        if (sqtycnt == spreqtycnt)
            //        {
            //            CkBzFlg(stkotgds);

            //            //查看有没有明细为空的单据，直接修改播种标记
            //            var qryZeroBz = from e in WmsDc.stkotdtl
            //                            where e.stkot.wmsno == wmsno && e.stkot.wmsbllid == WMSConst.BLL_TYPE_RETRIEVE
            //                            group e by e.stkouno into g
            //                            select new
            //                            {
            //                                stkouno = g.Key,
            //                                sqty = g.Sum(e => e.qty)
            //                            };
            //            qryZeroBz = qryZeroBz.Where(e => e.sqty == 0);
            //            var qryZeroBzmst = from e in WmsDc.stkot
            //                               join e1 in qryZeroBz on e.stkouno equals e1.stkouno
            //                               where e.chkflg != GetY()
            //                               select e;
            //            foreach (var q in qryZeroBzmst)
            //            {
            //                CkBzFlg(q);
            //                foreach (var dl in q.stkotdtl)
            //                {
            //                    dl.bzflg = GetY();
            //                    dl.bzdat = GetCurrentDate();
            //                    dl.bzr = LoginInfo.Usrid;
            //                }
            //            }

            //        }

            //        WmsDc.SubmitChanges();
            //        scop.Complete();
            //        return RSucc("成功", null, "S0164");
            //    }
            //    catch (Exception ex)
            //    {
            //        return RErr(ex.Message, "E0051");
            //    }
            //}
            #endregion 老的播种逻辑
        }

        private stkotdtl[] GetStkdtlsByStkounos(String wmsno, String gdsid, string[] stkounos)
        {
            var qry = from e in WmsDc.stkotdtl
                      where stkounos.Contains(e.stkouno)
                      && e.stkot.wmsbllid == WMSConst.BLL_TYPE_RETRIEVE && e.stkot.wmsno == wmsno
                      && e.gdsid == gdsid
                      select e;
            stkotdtl[] stkotdtls = qry.ToArray();
            return stkotdtls;
        }

        private stkot[] GetStkots(String wmsno, String rcvdptid, String gdsid)
        {
            var qry = from e in WmsDc.stkot
                      where e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && dpts.Contains(e.dptid.Trim())
                      && e.savdptid == LoginInfo.DefSavdptid
                      && e.wmsno == wmsno && e.wmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                      && (from ed in e.stkotdtl where ed.gdsid == gdsid select 1).Any()
                      && e.rcvdptid == rcvdptid
                      select e;
            stkot[] stkots = qry.ToArray();
            return stkots;
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
            WmsDc.SubmitChanges();

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

            WmsDc.SubmitChanges();

            stklst astklst = new stklst();
            astklst.stkouno = p.stkouno;
            WmsDc.stklst.InsertOnSubmit(astklst);

            WmsDc.SubmitChanges();
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


            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      where e.wmsno == wmsno
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
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
                      && e.savdptid == LoginInfo.DefSavdptid
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
                return RNoData("N0181");
            }

            return RSucc("成功!", wmsno1, "S0165");
        }

        /// <summary>
        /// 选择未播种的20条记录
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllTop20List(String wmsno, string gdsid, int? pageid, int? pagesize)
        {
            pageid = pageid == null ? 1 : pageid;
            pagesize = pagesize == null ? 20 : pagesize;

            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }                      
                      join e6 in WmsDc.dpt on e.rcvdptid equals e6.dptid
                      where e.wmsno == wmsno
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && (e1.bzflg == GetN() || e1.bzflg == null)
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
                        e2.sqty,
                        e4.cnvrto,
                        pkgdes = e4.pkgdes.Trim(),
                        pkg03 = GetPkgStr(e2.sqty, e4.cnvrto, e4.pkgdes),
                        pkg03pre = GetPkgStr(e2.sqty, e4.cnvrto, e4.pkgdes)
                    };
            var wmsno1 = q.Take(20).ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0182");
            }

            return RSucc("成功！", wmsno1, "S0166");
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

            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }
                      join e6 in WmsDc.dpt on e.rcvdptid equals e6.dptid
                      where e.wmsno == wmsno
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      //&& (e1.bzflg == GetN() || e1.bzflg == null)                      
                      group e1 by new { e.wmsno, e.wmsbllid, e1.gdsid, e1.bzflg, e2.gdsdes, e2.spc, e2.bsepkg } into g
                      orderby g.Key.bzflg, g.Key.gdsid
                      select new
                      {
                          g.Key.wmsno,
                          g.Key.wmsbllid,
                          g.Key.gdsid,
                          g.Key.gdsdes,
                          g.Key.spc,
                          g.Key.bsepkg,
                          g.Key.bzflg,
                          sqty = g.Sum(eqty => Math.Round(eqty.qty, 2, MidpointRounding.AwayFromZero)),
                          sqtypre = g.Sum(eqty => Math.Round(eqty.preqty==null?eqty.qty:eqty.preqty.Value, 2, MidpointRounding.AwayFromZero))
                      };
            qry = qry.Where(e => e.sqty > 0);
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
                        e2.sqty,
                        e2.sqtypre,
                        bzedall = (from e in WmsDc.stkot
                                   join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                                   where e.wmsno == e2.wmsno && e.wmsbllid == e2.wmsbllid && e1.gdsid == e2.gdsid
                                   && e.bzflg == GetN()
                                   group e1 by new { e.wmsno, e.wmsbllid } into g1
                                   select g1).Count() == 0 ? GetY() : GetN(),
                        e2.bzflg,
                        e4.cnvrto,
                        pkgdes = e4.pkgdes.Trim(),
                        pkg03 = GetPkgStr(e2.sqty, e4.cnvrto, e4.pkgdes),
                        pkg03pre = GetPkgStr(e2.sqty, e4.cnvrto, e4.pkgdes)
                    };
            var wmsno1 = q.Take(20).ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0183");
                //return RInfo( "I0387" );
            }

            return RSucc("成功！", wmsno1, "S0167");
        }

        /// <summary>
        /// 根据商品条码和拣货单号查找播种单
        /// </summary>
        /// <param name="wmsno">播种单单号</param>
        /// <param name="gdsid">条码/货号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_播种查询, pwrdes = "播种查询")]
        public ActionResult GetBoZBllByGdsid(String wmsno, String gdsid)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            if (gdsid == null)
            {
                return RInfo( "I0388" );
            }

            String Dat = GetCurrentDay();

            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e3.wmsno, wmsbllid = e3.bllid }                      
                      join e6 in WmsDc.dpt on e.rcvdptid equals e6.dptid
                      where e.wmsno == wmsno
                      && e1.gdsid == gdsid
                      && e.savdptid == LoginInfo.DefSavdptid
                      && dpts.Contains(e.dptid.Trim())
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      //&& (e.bzflg == GetN() || e.bzflg == null)                      
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
                          busid = "",
                          e6.dptdes,
                          e1.bzflg,
                          e1.bzdat,
                          e1.bzr,
                          preqty = e1.preqty == null ? e1.qty : e1.preqty
                      };
            qry = qry.Where(e => e.qty > 0);
            var qry1 = qry.GroupBy(e => new { e.bsepkg, e.busid, e.bzdat, e.bzflg, e.bzr, e.dptdes, e.dptid, e.gdsdes, e.gdsid, e.rcvdptid, e.savdptid, e.spc })
                       .Select(g => new
                       {
                           qty = g.Sum(e => e.qty),
                           preqty = g.Sum(e => e.preqty),
                           pkgqty = g.Sum(e => e.pkgqty),
                           g.Key.bsepkg,
                           g.Key.busid,
                           g.Key.bzdat,
                           g.Key.bzflg,
                           g.Key.bzr,
                           g.Key.dptdes,
                           g.Key.dptid,
                           g.Key.gdsdes,
                           g.Key.gdsid,
                           g.Key.rcvdptid,
                           g.Key.savdptid,
                           g.Key.spc
                       });
            var q = from e2 in qry1
                    join e3 in
                        (from m in WmsDc.wms_pkg group m by new { m.gdsid, m.cnvrto, m.pkgdes,  } into g select g.Key)
                    on new { e2.gdsid } equals new { e3.gdsid}
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
                        e2.pkgqty,
                        e2.preqty,
                        e2.qty,                        
                        e2.rcvdptid,
                        e2.savdptid,
                        e2.spc,                        
                        e4.cnvrto,
                        e4.pkgdes,
                        pkg03 = GetPkgStr(e2.qty, e4.cnvrto, e4.pkgdes),
                        pkg03pre = GetPkgStr(e2.preqty, e4.cnvrto, e4.pkgdes)
                    };
            var wmsno1 = q.ToArray();
            if (wmsno1.Length <= 0)
            {
                return RNoData("N0184");
            }

            var extObj = wmsno1.GroupBy(e => new { e.gdsid, e.gdsdes, e.cnvrto, e.pkgdes })
                    .Select(ek => new
                    {
                        sqty = ek.Sum(e1 => e1.qty),
                        ek.Key.gdsid,
                        ek.Key.gdsdes,
                        ek.Key.cnvrto,
                        pkgdes = ek.Key.pkgdes.Trim(),
                        pkg03 = GetPkgStr(ek.Sum(e1 => e1.qty), ek.Key.cnvrto, ek.Key.pkgdes),
                        pkg03pre = GetPkgStr(ek.Sum(e1 => e1.preqty), ek.Key.cnvrto, ek.Key.pkgdes),
                    });

            return RSucc("成功", wmsno1, extObj, "S0168");
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
                         // && e.mkedat.Substring(0, 8) == GetCurrentDay()
                      && e.bzflg == GetN() && e.zdflg == GetY() && e1.chkflg == GetY()
                      && e.wmsno != null && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && e.wmsbllid == WMSConst.BLL_TYPE_RETRIEVE
                      && e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && (from ebz in WmsDc.wms_bzcnv where ebz.sndtmd.Substring(2,4)==Fscprdid && ebz.savdptid==LoginInfo.DefSavdptid 
                              && ebz.wmsno==e.wmsno&& ebz.wmsbllid==e.wmsbllid select ebz).Any()
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
                        .GroupBy(e => new { e.wmsno, e.wmsbllid, e.zddat, e.qu })
                        .Select(e => e.Key)
                        .ToArray();
            if (arrqrymst1.Length <= 0)
            {
                return RNoData("N0185");
            }

            return RSucc("成功", arrqrymst1, "S0169");
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
                return RInfo( "I0389" );
            }
            return RSucc("成功", arrqry, "S0170");
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
                return RInfo( "I0390" );
            }
            var arrqrymst = FindBllFromCangMst107(bllid, dat, boci, gdsid, rcvdptid, busid);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0186");
            }
            return RSucc("成功", arrqrymst, "S0171");
        }

    }
}
