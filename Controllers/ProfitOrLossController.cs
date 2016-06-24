using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;

namespace WMS.Controllers
{
    /// <summary>
    /// 损溢单
    /// </summary>
    public class ProfitOrLossController : SsnController
    {
        /// <summary>
        /// 损溢单构造函数
        /// </summary>
        public ProfitOrLossController()
        {
            Mdlid = "ProfitOrLoss";
            Mdldes = "损溢单";
        }

        private ActionResult _MakeParam(String wmsno, String oldbarcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys, String rsns)
        {
            String[] oldbarcode = oldbarcodes.Split(',');
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] gdstype = gdstypes.Split(',');
            String[] vlddat = vlddats.Split(',');
            String[] bthno = bthnos.Split(',');
            String[] rsn = rsns.Split(',');
            //String[] newsbarcode = newbarcodes.Split(',');
            List<wms_cangdtl_111> lstDtl = new List<wms_cangdtl_111>();
            if ((oldbarcode.Length != gdsid.Length)
                && (oldbarcode.Length != qty.Length)
                && (oldbarcode.Length != gdstype.Length)
                && (oldbarcode.Length != rsn.Length) )
            {
                return RInfo( "I0173" );
            }
            int i = 0;
            foreach (String s in oldbarcode)
            {
                if (!String.IsNullOrEmpty(s))
                {
                    //判断gdsid和barcode是不是在一个区
                    String[] qu = GetQuByGdsid(gdsid[i], LoginInfo.DefStoreid);
                    if (qu == null)
                    {
                        return RInfo("I0174");                        
                    }
                    if ( !qu.Contains(s.Substring(0, 2)) )
                    {
                        return RInfo("I0176", gdsid[i], String.Join(",", qu));
                    }
                    //判断分区是否有效
                    if (!IsExistBarcode(s))
                    {
                        return RInfo( "I0177",s.Trim()  );
                    }

                    wms_cangdtl_111 dtl = new wms_cangdtl_111();
                    dtl.wmsno = wmsno;
                    dtl.bllid = WMSConst.BLL_TYPE_PROFITORLOSS;
                    dtl.rcdidx = i+1;
                    dtl.barcode = s;
                    //判断分区是否有效
                    if (!IsExistBarcode(dtl.barcode))
                    {
                        return RInfo( "I0178",s.Trim()  );
                    }
                    dtl.gdsid = gdsid[i];
                    dtl.gdstype = gdstype[i];
                    dtl.pkgid = "01";                    
                    double fQty = 0;
                    if (!double.TryParse(qty[i], out fQty))
                    {
                        return RInfo( "I0179",gdsid[i],qty[i]  );
                    }
                    dtl.qty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.preqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.pkgqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.gdstype = gdstype[i];
                    dtl.bthno = String.IsNullOrEmpty(bthno[i]) ? "1" : bthno[i];
                    dtl.vlddat = String.IsNullOrEmpty(vlddat[i]) ? GetCurrentDay() : vlddat[i];
                    JsonResult jr = (JsonResult)GetBcdByGdsid(gdsid[i]);
                    ResultMessage rm = (ResultMessage)jr.Data;
                    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return RInfo( "I0180",gdsid[i] );
                    }
                    bcd[] b = (bcd[])rm.ResultObject;
                    dtl.bcd = b[0].bcd1;                    
                    dtl.bkr = "";
                    dtl.bokflg = GetN();
                    dtl.bokdat = "";
                    dtl.brfdtl = rsn[i];


                    lstDtl.Add(dtl);
                    i++;
                }
            }

            return RSucc("成功", lstDtl.ToArray(), "S0096");
        }

        /// <summary>
        /// 得到会计期间的损溢单
        /// </summary>
        /// <param name="fscprdid"></param>
        /// <returns></returns>
        [PWR(Pwrid=WMSConst.WMS_BACK_损溢制单, pwrdes="损溢制单")]
        public ActionResult GetBllsByFscprdid(String fscprdid)
        {
            var qry = from e in WmsDc.wms_cang_111
                      join e1 in WmsDc.emp on e.mkr equals e1.empid
                      where e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                      && qus.Contains(e.qu.Trim())
                      && e.mkr == LoginInfo.Usrid
                      && e.mkedat.Substring(2, 4) == fscprdid
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e.times,
                          e.savdptid,
                          e.rcvdptid,
                          e.qu,
                          e.prvid,
                          e.opr,
                          e.mkr,
                          mkrdes = e1.empdes,
                          e.mkedat,
                          e.lnkno,
                          e.lnkbrief,
                          e.lnkbocino,
                          e.lnkbocidat,
                          e.lnkbllid,
                          e.ckr,
                          e.chkflg,
                          e.chkdat,
                          e.brief,
                          dtls = (from edtl in WmsDc.wms_cangdtl_111
                                  join gd in WmsDc.gds on edtl.gdsid equals gd.gdsid
                                  join e3 in WmsDc.v_wms_pkg on new { gd.gdsid } equals new { e3.gdsid }
                                  into joinPkg
                                  from e4 in joinPkg.DefaultIfEmpty()
                                  where edtl.wmsno == e.wmsno && edtl.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                                  select new
                                  {
                                      edtl.wmsno,
                                      edtl.bllid,
                                      edtl.rcdidx,
                                      edtl.oldbarcode,
                                      edtl.barcode,
                                      edtl.gdsid,
                                      edtl.pkgid,
                                      pkgqty = Math.Round(edtl.pkgqty.Value, 4, MidpointRounding.AwayFromZero),
                                      qty = Math.Round(edtl.qty, 4, MidpointRounding.AwayFromZero),
                                      edtl.gdstype,
                                      edtl.bthno,
                                      edtl.vlddat,
                                      edtl.bcd,
                                      edtl.tpcode,
                                      edtl.bkr,
                                      edtl.bokflg,
                                      edtl.bokdat,
                                      preqty = Math.Round(edtl.preqty.Value, 4, MidpointRounding.AwayFromZero),
                                      gd.gdsdes,
                                      gd.spc,
                                      gd.bsepkg,
                                      pkg03 = GetPkgStr(Math.Round(edtl.qty, 4, MidpointRounding.AwayFromZero), e4.cnvrto, e4.pkgdes),
                                      pkg03pre = GetPkgStr(Math.Round(edtl.preqty.Value, 4, MidpointRounding.AwayFromZero), e4.cnvrto, e4.pkgdes)
                                  }).ToArray()
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0113");
            }

            return RSucc("成功", arrqry, "S0097");
        }

        /// <summary>
        /// 损溢当天是否有损溢单据
        /// </summary>
        /// <param name="boci"></param>
        /// <param name="qu"></param>
        /// <returns></returns>
        public ActionResult HasCkBll(string qu, String savdptid, 
            String sybol)
        {
            if (HasCkBll(qu, LoginInfo.Usrid, savdptid, sybol))
            {
                return RInfo( "I0181" );
            }

            return RSucc("尚无损溢单", null, "S0098");
        }

        private bool HasCkBll(string qu, string usrid, String savdptid, String sybol)
        {
            var qry = from e in WmsDc.wms_cangdtl_111
                      join e1 in WmsDc.wms_cang_111 on new { e.wmsno,e.bllid } equals new { e1.wmsno,e1.bllid }
                      where e1.qu == qu
                      && (e1.savdptid == savdptid)
                      && e1.mkr == usrid
                      && e1.mkedat == GetCurrentDay()
                      && (e1.chkflg != GetY())                      
                      && e1.times == sybol
                      select e;
            foreach (wms_cangdtl_111 m in qry)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 新制损溢单
        /// </summary>
        /// <param name="barcodes">仓位编码</param>
        /// <param name="gdsids">货号</param>
        /// <param name="gdstypes">商品类型</param>
        /// <param name="qtys">损溢数量（正为溢，负为损）</param>
        /// <returns></returns>
        /// 
        [PWR(Pwrid=WMSConst.WMS_BACK_损溢制单, pwrdes="损溢制单")]
        public ActionResult MkPrftOLssBll(String barcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys, String rsns)
        {
            String[] barcode = barcodes.Split(',');
            String qu = barcode[0].Substring(0,2);
            String savdptid = GetSavdptidByQu(qu);
            //正在生成拣货单，请稍候重试
            string quRetrv = qu;
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo( "I0182" );
            }

            return MakeNewBllNo(savdptid, qu, WMSConst.BLL_TYPE_PROFITORLOSS, (bllno) =>
            {
                //检查并创建明细
                JsonResult jr = (JsonResult)_MakeParam(bllno, barcodes, gdsids, gdstypes, bthnos, vlddats, qtys, rsns);
                ResultMessage rm = (ResultMessage)jr.Data;
                if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return rm;
                }

                //创建主表
                wms_cangdtl_111[] dtls = (wms_cangdtl_111[])rm.ResultObject;                
                wms_cang_111 mst = new wms_cang_111();
                mst.wmsno = bllno;
                mst.bllid = WMSConst.BLL_TYPE_PROFITORLOSS;
                mst.savdptid = savdptid;
                mst.prvid = "";
                //String qu = dtls[0].barcode.Substring(0,2) ; //GetQuByGdsid(dtls[0].gdsid, LoginInfo.DefStoreid);
                mst.qu = qu;
                mst.rcvdptid = "";
                String symbol = "+";  //symbol="+"报溢，symbol="-"报损
                if (dtls.Length > 0)
                {
                    double? dqty = dtls[0].qty;
                    symbol = dqty != null && dqty >= 0 ? "+" : "-";
                }
                /*if (HasCkBll(qu, LoginInfo.Usrid, savdptid, symbol))
                {
                    return RRInfo("I0441");
                    
                }*/
                mst.times = symbol;
                mst.lnkbocino = "";
                mst.lnkbocidat = "";
                mst.mkr = LoginInfo.Usrid;
                mst.mkedat = GetCurrentDay();
                mst.mkedat2 = GetCurrentDate();
                mst.ckr = "";
                mst.chkflg = GetN();
                mst.chkdat = "";
                mst.opr = LoginInfo.Usrid;
                mst.brief = "";
                mst.lnkbllid = "";
                mst.lnkno = "";
                mst.lnkbrief = "";

                //如果是报损，判断是否有库存
                foreach(wms_cangdtl_111 d in dtls){                    
                    if (mst.times.Trim() == "-")
                    {
                        if (d.brfdtl.Trim() == "")
                        {
                            return RRInfo("I0442" ,d.gdsid );

                        }

                        //得到一个商品的库存数量
                        GdsInBarcode[] gb = GetAGdsQtyInBarcode(d.barcode, d.gdsid, d.gdstype)
                                            .Where(e => e.vlddat == d.vlddat.Trim() && e.bthno == d.bthno.Trim())
                                            .ToArray();
                        double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                        double ktqty = bqty;  //可调数量 = 库存数量
                        //如果 需调整数量 > 可调数量
                        if (d.qty > ktqty)
                        {
                            return RRNoData("N0238" ,d.gdsid  ,d.barcode );

                        }             
                    }
                }                

                WmsDc.wms_cang_111.InsertOnSubmit(mst);
                WmsDc.wms_cangdtl_111.InsertAllOnSubmit(dtls);

                try
                {
                    WmsDc.SubmitChanges();
                    return RRSucc("成功", mst, "S0217");

                }
                catch (Exception ex)
                {
                    return RRErr(ex.Message, "E0064");

                    rm.ResultObject = null;
                    return rm;
                }
            });
        }

        /// <summary>
        /// 损溢单明细删除
        /// </summary>
        /// <param name="wmsno">损溢单单号</param>
        /// <param name="gdsid">货号</param>
        /// <param name="rcdidx">序号</param>
        /// <returns></returns>        
        /// 
        [PWR(Pwrid=WMSConst.WMS_BACK_损溢制单, pwrdes="损溢制单")]
        public ActionResult DlPrftOLssBllDtl(String wmsno, String gdsid, int rcdidx)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_111
                         where e.wmsno == wmsno
                         //&& e.gdsid == gdsid && e.rcdidx == rcdidx
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            var arrqrydtl1 = qrydtl.Where(e => e.gdsid == gdsid && e.rcdidx == rcdidx).ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0114");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            //正在生成拣货单，请稍候重试
            string quRetrv = mst.qu;
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo( "I0183" );
            }

            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0184" );
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0185" );
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo( "I0186" );
            }
            //删除单据明细
            int iDtlCnt = arrqrydtl.Length;
            WmsDc.wms_cangdtl_111.DeleteAllOnSubmit(arrqrydtl1);
            iDelCangDtl111(arrqrydtl1, mst);
            if (iDtlCnt == 1)
            {
                WmsDc.wms_cang_111.DeleteAllOnSubmit(arrqrymst);
                iDelCangMst111(mst);
            }
            //删除主单据
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0099");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0023");
            }       
        }

        /// <summary>
        /// 删除损溢单
        /// </summary>
        /// <param name="wmsno">损溢单单号</param>
        /// <returns></returns>
        /// 
        [PWR(Pwrid=WMSConst.WMS_BACK_损溢制单, pwrdes="损溢制单")]
        public ActionResult DlPrftOLssBll(String wmsno)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0115");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            //正在生成拣货单，请稍候重试
            string quRetrv = mst.qu;
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo( "I0187" );
            }

            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0188" );
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo( "I0189" );
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0190" );
            }


            //删除单据明细
            WmsDc.wms_cangdtl_111.DeleteAllOnSubmit(arrqrydtl);
            WmsDc.wms_cang_111.DeleteAllOnSubmit(arrqrymst);
            iDelCangDtl111(arrqrydtl, mst);
            iDelCangMst111(mst);
            //删除主单据
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0100");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0024");
            }       
        }

        /// <summary>
        /// 修改损溢单
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="gdstype"></param>
        /// <param name="newbarcode">新仓位码</param>
        /// <param name="rcdidx"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public ActionResult MdAPrftOLss(String wmsno, String gdsid, String newbarcode, String gdstype, int rcdidx, double qty, String rsn)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         && e.gdsid == gdsid && e.rcdidx == rcdidx
                         orderby e.rcdidx descending
                         select e;
            var arrqrydtl = qrydtl.ToArray();

            //检查barcode是否有效
            if (!IsExistBarcode(newbarcode))
            {
                return RInfo( "I0191",newbarcode  );
            }

            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0116");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            //正在生成拣货单，请稍候重试
            string quRetrv = mst.qu;
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo( "I0192" );
            }

            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0193" );
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0194" );
            }
            //未找到该商品
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0117");
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo( "I0195" );
            }

            //判断QTY应该为正为负数
            if (mst.times.Trim() == "+" && qty < 0)
            {
                return RInfo( "I0196" );
            }
            if (mst.times.Trim() == "-" && qty > 0)
            {
                return RInfo( "I0197" );
            }
            if (mst.times.Trim() == "-" && rsn.Trim() == "")
            {
                return RInfo( "I0198",gdsid  );
            }
                        
            //判断新仓位码是否和主单是一个区
            if (mst.qu.Trim() != newbarcode.Substring(0, 2))
            {
                return RInfo( "I0199" );
            }

            //修改数量
            arrqrydtl[0].barcode = newbarcode;
            arrqrydtl[0].qty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            arrqrydtl[0].pkgqty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            arrqrydtl[0].preqty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            if (!String.IsNullOrEmpty(gdstype))
            {
                arrqrydtl[0].gdstype = gdstype;
            }

            

            //如果是报损，判断是否有库存
            if (mst.times.Trim() == "-")
            {
                //修改数量
                wms_cangdtl_111 dtl = arrqrydtl[0];

                //得到一个商品的库存数量
                GdsInBarcode[] gb = GetAGdsQtyInBarcode(dtl.barcode, gdsid, dtl.gdstype)
                                    .Where(e => e.vlddat == dtl.vlddat.Trim() && e.bthno == dtl.bthno.Trim())
                                            .ToArray();
                double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                double ktqty = Math.Abs( bqty) + Math.Abs( dtl.qty);  //可调数量 = 库存数量+本单该商品的数量
                //如果 需调整数量 > 可调数量
                if (Math.Abs(qty) > Math.Abs(ktqty))
                {
                    return RInfo( "I0200",qty ,ktqty );
                }

                dtl.brfdtl = rsn.ToString();
                //return RInfo( "I0201",gdsid ,arrqrydtl[0].barcode  );
            }

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", arrqrydtl[0], "S0101");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0025");
            } 
        }

        /// <summary>
        /// 增加损溢单商品(批量)
        /// </summary>
        /// <param name="wmsno">损溢单单号</param>
        /// <param name="barcodes">仓位信息</param>
        /// <param name="gdsids">要损溢的货号</param>
        /// <param name="gdstypes">商品类型</param>
        /// <param name="qtys">商品数量</param>
        /// <param name="rsn"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_损溢制单, pwrdes = "损溢制单")]
        public ActionResult AdPrftOLsses(String wmsno, String barcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys, String rsns)
        {
            //判断barcodes、gdsids、gdstypes、qtys是否数量一致
            String[] barcode = barcodes.Split(',');
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] gdstype = gdstypes.Split(',');
            String[] bthno = bthnos.Split(',');
            String[] vlddat = vlddats.Split(',');
            String[] rsn = rsns.Split(',');
            List<object> retObjs = new List<object>();
            if (
                (barcode.Length != gdsid.Length)
            || (barcode.Length != qty.Length)
                || (gdsid.Length != qty.Length)
            || (gdstype.Length != gdsid.Length)
               || (rsn.Length != gdsid.Length) 
                )
            {
                return RInfo( "I0202" );
            }

            for (int i = 0; i < gdsid.Length; i++)
            {
                double d = 0;
                if (!double.TryParse(qty[i], out d))
                {
                    return RInfo( "I0203",gdsid[i],qty[i]  );
                }

                JsonResult jr = (JsonResult)AdPrftOLss(wmsno, barcode[i], gdsid[i], gdstype[i], bthno[i], vlddat[i], d, rsn[i]);
                ResultMessage rm = (ResultMessage)jr.Data;
                if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return jr;
                }
                retObjs.Add(rm.ResultObject);
            }

            return RSucc("成功", retObjs, "S0102");
        }

        /// <summary>
        /// 增加损溢单商品
        /// </summary>
        /// <param name="wmsno">损溢单单号</param>
        /// <param name="barcode">仓位信息</param>
        /// <param name="gdsid">要损溢的货号</param>        
        /// <param name="gdstype">商品类型</param>
        /// <param name="qty">商品数量</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_损溢制单, pwrdes = "损溢制单")]
        public ActionResult AdPrftOLss(String wmsno, String barcode, String gdsid, String gdstype, String bthno, String vlddat, double qty, String rsn)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_111
                         where e.wmsno == wmsno                         
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         orderby e.rcdidx descending
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0118");
            }
            //判断分区是否有效
            if (!IsExistBarcode(barcode))
            {
                return RInfo( "I0204",barcode.Trim()  );
            }

            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            //正在生成拣货单，请稍候重试
            string quRetrv = mst.qu;
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo( "I0205" );
            }

            if (!qus.Contains(mst.qu))
            {
                return RInfo( "I0206" );
            }

            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0207" );
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo( "I0208" );
            }
            //判断商品是否已经再单据里面
            int iHasIn = arrqrydtl.Where(e => e.gdsid == gdsid && e.gdstype == gdstype && e.barcode == barcode).Count();
            if (iHasIn > 0)
            {
                return RInfo( "I0209",gdsid  );
            }
            //如果是报损，判断是否有库存
            if (mst.times.Trim() == "-")
            {
                if (rsn.Trim() == "")
                {
                    return RInfo( "I0210",gdsid  );
                }

                //如果是报损，判断是否有库存                
                //得到一个商品的库存数量
                GdsInBarcode[] gb = GetAGdsQtyInBarcode(barcode, gdsid, gdstype)
                                    .Where(e => e.vlddat == vlddat.Trim() && e.bthno == bthno.Trim())
                                            .ToArray();
                double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                double ktqty = bqty;  //可调数量 = 库存数量
                //如果 需调整数量 > 可调数量
                if (Math.Abs(qty) > Math.Abs(ktqty))
                {
                    return RInfo( "I0211",qty ,ktqty );
                }
                //return RInfo( "I0212",gdsid ,barcode  );
            }
                      
            

            //判断gdsid和barcode是不是在一个区
            String[] qu = GetQuByGdsid(gdsid, LoginInfo.DefStoreid);
            if (!qu.Contains(barcode.Substring(0, 2)))
            {
                return RInfo("I0213", gdsid, String.Join(",", qu));
            }            

            wms_cangdtl_111 dtl = new wms_cangdtl_111();
            dtl.wmsno = wmsno;
            dtl.bllid = WMSConst.BLL_TYPE_PROFITORLOSS;
            dtl.rcdidx = arrqrydtl[0].rcdidx + 1;
            dtl.barcode = barcode;
            dtl.gdsid = gdsid;
            dtl.gdstype = gdstype;
            dtl.pkgid = "01";
            double fQty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            dtl.qty = fQty;
            dtl.preqty = fQty;
            dtl.pkgqty = fQty;
            dtl.gdstype = gdstype;
            dtl.bthno = string.IsNullOrEmpty(bthno) ? "1" : bthno;
            dtl.vlddat = string.IsNullOrEmpty(vlddat) ? GetCurrentDay() : vlddat;
            JsonResult jr = (JsonResult)GetBcdByGdsid(gdsid);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return RInfo( "I0214",gdsid );
            }
            bcd[] b = (bcd[])rm.ResultObject;
            dtl.bcd = b[0].bcd1;
            dtl.bkr = "";
            dtl.bokflg = GetN();
            dtl.bokdat = "";
            dtl.brfdtl = rsn.ToString();

            WmsDc.wms_cangdtl_111.InsertOnSubmit(dtl);

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", dtl, "S0103");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0026");
            }            
        }        

        /// <summary>
        /// 修改损溢单
        /// </summary>
        /// <param name="wmsno">损溢单单号</param>
        /// <param name="barcodes">仓位信息</param>
        /// <param name="gdsids">要损溢的货号</param>        
        /// <param name="gdstypes">商品类型</param>
        /// <param name="qtys">商品数量</param>
        /// <returns></returns>
        /// 
        [PWR(Pwrid=WMSConst.WMS_BACK_损溢制单, pwrdes="损溢制单")]
        public ActionResult MdPrftOLssBll(String wmsno, String barcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys, String rsns)
        {        
            //拆分参数
            //检查并创建明细
            JsonResult jr = (JsonResult)_MakeParam(wmsno, barcodes, gdsids, gdstypes, bthnos, vlddats, qtys, rsns);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return jr;
            }                        

            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0119");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            //正在生成拣货单，请稍候重试
            string quRetrv = mst.qu;
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo( "I0215" );
            }

            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0216" );
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0217" );
            }
   
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo( "I0218" );
            }

            wms_cangdtl_111[] newdtl = (wms_cangdtl_111[])rm.ResultObject;
            int i = 0;

            if (mst.times.Trim() == "-")
            {
                //如果是报损，判断是否有库存
                foreach (wms_cangdtl_111 d in newdtl)
                {
                    if (d.brfdtl.Trim() == "")
                    {
                        return RInfo( "I0219",d.gdsid  );
                    }

                    //得到一个商品的库存数量
                    GdsInBarcode[] gb = GetAGdsQtyInBarcode(arrqrydtl[i].barcode, arrqrydtl[i].gdsid, arrqrydtl[i].gdstype)
                                        .Where(e => e.vlddat == arrqrydtl[i].vlddat.Trim() && e.bthno == arrqrydtl[i].bthno.Trim())
                                            .ToArray(); 
                    double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                    double ktqty = Math.Abs(bqty) + Math.Abs(arrqrydtl[i].qty);  //可调数量 = 库存数量+本单该商品的数量
                    //如果 需调整数量 > 可调数量
                    if (Math.Abs(d.qty) > Math.Abs(ktqty))
                    {
                        return RInfo( "I0220",d.qty ,ktqty );
                    }
                    i++;
                }
            }


            //删除单据明细            
            WmsDc.wms_cangdtl_111.DeleteAllOnSubmit(arrqrydtl);
            iDelCangDtl111(arrqrydtl,mst);
            //增加单据明细            
            WmsDc.wms_cangdtl_111.InsertAllOnSubmit(newdtl);
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", newdtl, "S0104");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0027");
            }            
        }

        /// <summary>
        /// 得到损溢单明细
        /// </summary>
        /// <param name="wmsno">单号</param>
        /// <returns></returns>
        /// 
        [PWR(Pwrid=WMSConst.WMS_BACK_损溢查询, pwrdes="损溢查询")]
        public ActionResult GetPrftOLssBllDtl(String wmsno)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_111
                         join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                         join e2 in WmsDc.losrsn on e.brfdtl.Trim() equals e2.losrsnid.Trim()
                         into joinLosrsnDef
                         from e3 in joinLosrsnDef.DefaultIfEmpty()
                         join e4 in WmsDc.v_wms_pkg on new { e1.gdsid } equals new { e4.gdsid }
                         into joinPkg from e5 in joinPkg.DefaultIfEmpty()
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
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
                             pkgqty = Math.Round(e.pkgqty.Value, 4, MidpointRounding.AwayFromZero),
                             preqty = Math.Round(e.preqty.Value, 4, MidpointRounding.AwayFromZero),
                             qty = Math.Round(e.qty, 4, MidpointRounding.AwayFromZero),
                             e.rcdidx,
                             e.tpcode,
                             e.vlddat,
                             e.wmsno,
                             e1.gdsdes,
                             e1.spc,
                             e1.bsepkg,
                             e.brfdtl,
                             e3.losrsndes,
                             pkg03 = GetPkgStr(Math.Round(e.qty, 4, MidpointRounding.AwayFromZero), e5.cnvrto,e5.pkgdes),
                             pkg03pre = GetPkgStr(Math.Round(e.preqty.Value, 4, MidpointRounding.AwayFromZero), e5.cnvrto, e5.pkgdes),
                         };
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0120");
            }
            //单据是否找到
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0121");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0221" );
            }

            return RSucc("成功", arrqrydtl, "S0105");
        }

        /// <summary>
        /// 得到损溢单主单
        /// </summary>
        /// <param name="wmsno">单号</param>
        /// <returns></returns>
        /// 
        [PWR(Pwrid=WMSConst.WMS_BACK_损溢查询, pwrdes="损溢查询")]
        public ActionResult GetPrftOLssBll(String wmsno)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_111
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_PROFITORLOSS
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0122");
            }

            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0222" );
            }

            return RSucc("成功", arrqrymst, "S0106"); 
        }

        /// <summary>
        /// 损溢查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="wmsno">调整单单号</param>
        /// <param name="gdsid">商品货号、条码</param>
        /// <param name="barcode">仓位</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_损溢查询, pwrdes = "损溢查询")]
        public ActionResult FindBll(String begindat, String enddat, String wmsno, String gdsid, String barcode)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo( "I0223",barcode.Trim()  );
            }

            var arrqrymst = FindBllFromCangMst111(WMSConst.BLL_TYPE_PROFITORLOSS, begindat, enddat, wmsno, gdsid, barcode);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0123");
            }
            return RSucc("成功", arrqrymst, "S0107");
        }
    }
}
