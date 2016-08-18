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
    /// 返厂单
    /// </summary>
    public class RetPrvController : SsnController
    {
        /// <summary>
        /// 返厂单构造函数
        /// </summary>
        public RetPrvController()
        {
            Mdlid = "RetPrv";
            Mdldes = "返厂单";
        }

        private ActionResult _MakeParam(String wmsno, String oldbarcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys)
        {
            if (gdsids == null)
            {
                return RInfo( "I0324" );
            }
            if (qtys == null)
            {
                return RInfo( "I0325" );
            }
            if (gdstypes == null)
            {
                return RInfo( "I0326" );
            }
            if (oldbarcodes == null)
            {
                return RInfo( "I0327" );
            }
            String[] oldbarcode = oldbarcodes.Split(',');
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] gdstype = gdstypes.Split(',');
            String[] bthno = bthnos.Split(',');
            String[] vlddat = vlddats.Split(',');
            //String[] newsbarcode = newbarcodes.Split(',');
            List<wms_cangdtl_110> lstDtl = new List<wms_cangdtl_110>();
            if ((oldbarcode.Length != gdsid.Length)
                && (oldbarcode.Length != qty.Length)
                && (oldbarcode.Length != gdstype.Length)
                && (bthno.Length != gdstype.Length)
                && (vlddat.Length != gdstype.Length)
                )
            {
                return RInfo( "I0328" );
            }
            int i = 0;
            foreach (String s in oldbarcode)
            {
                if (!String.IsNullOrEmpty(s))
                {
                    //判断分区是否有效
                    if (!IsExistBarcode(s))
                    {
                        return RInfo( "I0329",s.Trim()  );
                    }

                    //判断gdsid和barcode是不是在一个区
                    String[] qu = GetQuByGdsid(gdsid[i], LoginInfo.DefStoreid);
                    if (qu == null)
                    {
                        return RInfo("I0330");
                    }
                    if (!qu.Contains(s.Substring(0, 2)))
                    {
                        return RInfo("I0332", gdsid[i], String.Join(",", qu));
                    }

                    wms_cangdtl_110 dtl = new wms_cangdtl_110();
                    dtl.wmsno = wmsno;
                    dtl.bllid = WMSConst.BLL_TYPE_RETPRV;
                    dtl.rcdidx = i+1;
                    dtl.barcode = s;
                    dtl.gdsid = gdsid[i];
                    dtl.gdstype = gdstype[i];
                    dtl.pkgid = "01";
                    double fQty = 0;
                    if (!double.TryParse(qty[i], out fQty))
                    {
                        return RInfo( "I0333",gdsid[i],qty[i]  );
                    }                    
                    dtl.qty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.preqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.pkgqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.bthno = string.IsNullOrEmpty(bthno[i]) ? "1" : bthno[i];
                    dtl.vlddat = string.IsNullOrEmpty(vlddat[i]) ? "1" : vlddat[i];
                    JsonResult jr = (JsonResult)GetBcdByGdsid(gdsid[i]);
                    ResultMessage rm = (ResultMessage)jr.Data;
                    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return RInfo( "I0334",gdsid[i] );
                    }
                    bcd[] b = (bcd[])rm.ResultObject;
                    dtl.bcd = b[0].bcd1;                    
                    dtl.bkr = "";
                    dtl.bokflg = GetN();
                    dtl.bokdat = GetCurrentDate();
                    // 判断在列表中是否已经有这条记录了
                    if (IsExistsInBarcode(lstDtl.ToArray(), dtl.barcode, dtl.gdsid, dtl.gdstype, dtl.vlddat, dtl.bthno))
                    {
                        return RInfo("I0478");
                    }

                    lstDtl.Add(dtl);
                    i++;
                }
            }

            return RSucc("成功", lstDtl.ToArray(), "S0148");
        }

        private bool IsExistsInBarcode(wms_cangdtl_110[] wms_cangdtl_110, string barcode, string gdsid, string gdstype, string vlddat, string bthno)
        {
            return wms_cangdtl_110.Where(e => e.barcode.Trim() == barcode.Trim()
                && e.gdsid.Trim() == gdsid.Trim() && e.gdstype.Trim() == gdstype.Trim()
                && e.vlddat.Trim() == vlddat.Trim() && e.bthno.Trim() == bthno.Trim()).Any();
        }

        /// <summary>
        /// 得到会计期间的返厂单
        /// </summary>
        /// <param name="fscprdid"></param>
        /// <returns></returns>
        [PWR(Pwrid=WMSConst.WMS_BACK_退厂制单, pwrdes="退厂制单")]
        public ActionResult GetBllsByFscprdid(String fscprdid)
        {
            var qry = from e in WmsDc.wms_cang_110
                      join e1 in WmsDc.prv on e.prvid equals e1.prvid
                      join e2 in WmsDc.emp on e.mkr equals e2.empid
                      where e.bllid == WMSConst.BLL_TYPE_RETPRV
                      && qus.Contains(e.qu.Trim())
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
                          mkrdes = e2.empdes,
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
                          e1.prvdes,
                          dtls = (from edtl in WmsDc.wms_cangdtl_111
                                  join gd in WmsDc.gds on edtl.gdsid equals gd.gdsid
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
                                      edtl.pkgqty,
                                      edtl.qty,
                                      edtl.gdstype,
                                      edtl.bthno,
                                      edtl.vlddat,
                                      edtl.bcd,
                                      edtl.tpcode,
                                      edtl.bkr,
                                      edtl.bokflg,
                                      edtl.bokdat,
                                      edtl.preqty,
                                      gd.gdsdes,
                                      gd.spc,
                                      gd.bsepkg
                                  }).ToArray()
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0158");
            }

            return RSucc("成功", arrqry, "S0149");
        }

        /// <summary>
        /// 新制返厂单
        /// </summary>
        /// <param name="prvid">供应商编号</param>
        /// <param name="barcodes">仓位编码</param>
        /// <param name="gdsids">货号</param>
        /// <param name="gdstypes">商品类型</param>
        /// <param name="qtys">损溢数量（正为溢，负为损）</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_退厂制单, pwrdes = "退厂制单")]
        public ActionResult MkRetPrvBll(String prvid, String barcodes, String gdsids, String gdstypes, String bthnos, string vlddats, String qtys)
        {
            using (TransactionScope scop = new TransactionScope(TransactionScopeOption.Required, options))
            {
                //检查并创建明细
                JsonResult jr = (JsonResult)_MakeParam("", barcodes, gdsids, gdstypes, bthnos, vlddats, qtys);
                ResultMessage rm = (ResultMessage)jr.Data;
                if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return jr;
                }
                wms_cangdtl_110[] dtls1 = (wms_cangdtl_110[])rm.ResultObject;
                string qu = dtls1[0].barcode.Substring(0, 2);
                return MakeNewBllNo(LoginInfo.DefCsSavdptid, qu, WMSConst.BLL_TYPE_RETPRV, (bllno) =>
                {
                    //检查并创建明细
                    jr = (JsonResult)_MakeParam(bllno, barcodes, gdsids, gdstypes, bthnos, vlddats, qtys);
                    rm = (ResultMessage)jr.Data;
                    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return rm;
                    }

                    //判断供应商编号是否正确
                    var qryprv = from e in WmsDc.prv
                                 where e.prvid == prvid
                                 select e;
                    var arrqryprv = qryprv.ToArray();
                    if (arrqryprv.Length <= 0)
                    {
                        rm.ResultObject = null;
                        return RRNoData("N0241");

                    }

                    //创建主表
                    wms_cangdtl_110[] dtls = (wms_cangdtl_110[])rm.ResultObject;
                    wms_cang_110 mst = new wms_cang_110();
                    mst.wmsno = bllno;
                    mst.bllid = WMSConst.BLL_TYPE_RETPRV;
                    mst.savdptid = LoginInfo.DefCsSavdptid;
                    mst.prvid = prvid;
                    mst.qu = dtls[0].barcode.Substring(0, 2);
                    ////正在生成拣货单，请稍候重试
                    //string quRetrv = mst.qu;
                    //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                    //{
                    //    return RRInfo( "I0335" );
                    //}
                    mst.rcvdptid = "";
                    mst.times = "1";
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
                    foreach (wms_cangdtl_110 d in dtls)
                    {
                        //得到一个商品的库存数量
                        GdsInBarcode[] gb = GetAGdsQtyInBarcode(d.barcode, d.gdsid, d.gdstype)
                                            .Where(e => e.vlddat == d.vlddat && e.bthno == d.bthno.Trim())
                                            .ToArray();
                        double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                        double ktqty = bqty;  //可调数量 = 库存数量
                        //如果 需调整数量 > 可调数量
                        if (d.qty > ktqty)
                        {
                            return RRNoData("N0242", d.qty.ToString(), ktqty.ToString());

                        }
                    }

                    try
                    {
                        WmsDc.wms_cang_110.InsertOnSubmit(mst);
                        WmsDc.SubmitChanges();
                        WmsDc.wms_cangdtl_110.InsertAllOnSubmit(dtls);
                        WmsDc.SubmitChanges();

                        WmsDc.SubmitChanges();
                        scop.Complete();
                        return RRSucc("成功", mst, "S0221");

                    }
                    catch (Exception ex)
                    {
                        return RRErr(ex.Message, "E0069");

                        rm.ResultObject = null;
                        return rm;
                    }
                });
            }
        }

        /// <summary>
        /// 返厂单明细删除
        /// </summary>
        /// <param name="wmsno">返厂单单号</param>
        /// <param name="gdsid">货号</param>
        /// <param name="rcdidx">序号</param>
        /// <param name="isMd">是否修改(为y，标志正在修改，为n,表示只有一条记录就删除主单)</param>
        /// <returns></returns> 
        [PWR(Pwrid=WMSConst.WMS_BACK_退厂制单, pwrdes="退厂制单")]
        public ActionResult DlRetPrvBllDtl(String wmsno, String gdsid, int rcdidx, String isMd)
        {
            using (TransactionScope scop = new TransactionScope(TransactionScopeOption.Required, options))
            {
                ////正在生成拣货单，请稍候重试
                //string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0336" );
                //}

                isMd = String.IsNullOrEmpty(isMd) ? "n" : "y";
                //检查单号是否存在
                var qrymst = from e in WmsDc.wms_cang_110
                             where e.wmsno == wmsno
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             select e;
                var arrqrymst = qrymst.ToArray();
                var qrydtl = from e in WmsDc.wms_cangdtl_110
                             where e.wmsno == wmsno
                                 //&& e.gdsid == gdsid && e.rcdidx == rcdidx
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             select e;
                var arrqrydtl = qrydtl.ToArray();
                var arrqrydtl1 = qrydtl.Where(e => e.gdsid == gdsid && e.rcdidx == rcdidx).ToArray();
                //单据是否找到
                if (arrqrymst.Length <= 0)
                {
                    return RNoData("N0159");
                }
                //检查是否有数据权限
                wms_cang_110 mst = arrqrymst[0];
                if (!qus.Contains(mst.qu.Trim()))
                {
                    return RInfo("I0337");
                }
                //检查单号是否已经审核
                if (mst!=null && mst.chkflg == GetY())
                {
                    return RInfo("I0338");
                }
                //是否是同一个人制单
                if (!IsSameLogin(mst.mkr))
                {
                    return RInfo("I0339", mst.mkr, LoginInfo.Usrid);
                }
                //删除单据明细
                int iDtlCnt = arrqrydtl.Length;
                WmsDc.wms_cangdtl_110.DeleteAllOnSubmit(arrqrydtl1);
                iDelCangDtl110(arrqrydtl1, mst);
                if (iDtlCnt == 1 && isMd == "n")
                {
                    WmsDc.wms_cang_110.DeleteAllOnSubmit(arrqrymst);
                    iDelCangMst110(arrqrymst[0]);
                }
                //删除主单据
                try
                {
                    //WmsDc.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, mst);
                    //检查单号是否已经审核
                    if (mst!=null && mst.chkflg == GetY())
                    {
                        return RInfo("I0207");
                    }

                    if (iDtlCnt > 1)
                    {
                        //修改主单时间戳
                        string sql = @"update wms_cang_110 set bllid='110' where wmsno='" + mst.wmsno + "' and bllid='110' and udtdtm={0}";
                        int iEff = WmsDc.ExecuteCommand(sql, mst.udtdtm);
                        if (iEff == 0)
                        {
                            return RInfo("I0207");
                        }
                    }

                    WmsDc.SubmitChanges();
                    scop.Complete();
                    return RSucc("成功", null, "S0150");
                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0044");
                }
            }
        }

        /// <summary>
        /// 删除返厂单
        /// </summary>
        /// <param name="wmsno">返厂单单号</param>
        /// <returns></returns>
        [PWR(Pwrid=WMSConst.WMS_BACK_退厂制单, pwrdes="退厂制单")]
        public ActionResult DlRetPrvBll(String wmsno)
        {
            using (TransactionScope scop = new TransactionScope(TransactionScopeOption.Required, options))
            {
                //检查单号是否存在
                var qrymst = from e in WmsDc.wms_cang_110
                             where e.wmsno == wmsno
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             select e;
                var arrqrymst = qrymst.ToArray();
                var qrydtl = from e in WmsDc.wms_cangdtl_110
                             where e.wmsno == wmsno
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             select e;
                var arrqrydtl = qrydtl.ToArray();
                int iDtlCount = arrqrydtl.Length;
                //单据是否找到
                if (arrqrymst.Length <= 0)
                {
                    return RNoData("N0160");
                }
                //检查是否有数据权限
                wms_cang_110 mst = arrqrymst[0];
                ////正在生成拣货单，请稍候重试
                //string quRetrv = mst.qu;
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0340" );
                //}
                if (!qus.Contains(mst.qu.Trim()))
                {
                    return RInfo("I0341");
                }
                //检查单号是否已经审核
                if (mst!=null && mst.chkflg == GetY())
                {
                    return RInfo("I0342");
                }
                //是否是同一个人制单
                if (!IsSameLogin(mst.mkr))
                {
                    return RInfo("I0343", mst.mkr, LoginInfo.Usrid);
                }
                //删除单据明细
                WmsDc.wms_cangdtl_110.DeleteAllOnSubmit(arrqrydtl);
                WmsDc.wms_cang_110.DeleteAllOnSubmit(arrqrymst);
                iDelCangDtl110(arrqrydtl, mst);
                iDelCangMst110(arrqrymst[0]);
                //删除主单据
                try
                {
                    //WmsDc.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, mst);
                    //检查单号是否已经审核
                    if (mst!=null && mst.chkflg == GetY())
                    {
                        return RInfo("I0207");
                    }

                    if (iDtlCount > 1)
                    {
                        //修改主单时间戳
                        string sql = @"update wms_cang_110 set bllid='110' where wmsno='" + mst.wmsno + "' and bllid='110' and udtdtm={0}";
                        int iEff = WmsDc.ExecuteCommand(sql, mst.udtdtm);
                        if (iEff == 0)
                        {
                            return RInfo("I0207");
                        }
                    }

                    WmsDc.SubmitChanges();
                    scop.Complete();
                    return RSucc("成功", null, "S0151");
                }
                catch (Exception ex)
                {                    
                    return RErr(ex.Message, "E0045");
                }
            }
        }

        /// <summary>
        /// 修改返厂单
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="rcdidx"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public ActionResult MdARetPrv(String wmsno, String gdsid, int rcdidx, double qty)
        {
            using (TransactionScope scop = new TransactionScope(TransactionScopeOption.Required, options))
            {
                //检查单号是否存在
                var qrymst = from e in WmsDc.wms_cang_110
                             where e.wmsno == wmsno
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             select e;
                var arrqrymst = qrymst.ToArray();
                var qrydtl = from e in WmsDc.wms_cangdtl_110
                             where e.wmsno == wmsno
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             && e.gdsid == gdsid && e.rcdidx == rcdidx
                             orderby e.rcdidx descending
                             select e;
                var arrqrydtl = qrydtl.ToArray();
                //单据是否找到
                if (arrqrymst.Length <= 0)
                {
                    return RNoData("N0161");
                }
                //检查是否有数据权限
                wms_cang_110 mst = arrqrymst[0];
                ////正在生成拣货单，请稍候重试
                //string quRetrv = mst.qu;
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0344" );
                //}

                if (!qus.Contains(mst.qu.Trim()))
                {
                    return RInfo("I0345");
                }
                //检查单号是否已经审核
                if (mst!=null && mst.chkflg == GetY())
                {
                    return RInfo("I0346");
                }
                //是否是同一个人制单
                if (!IsSameLogin(mst.mkr))
                {
                    return RInfo("I0347", mst.mkr, LoginInfo.Usrid);
                }

                //未找到该商品
                if (arrqrydtl.Length <= 0)
                {
                    return RNoData("N0162");
                }

                //修改数量
                wms_cangdtl_110 dtl = arrqrydtl[0];

                //得到一个商品的库存数量
                GdsInBarcode[] gb = GetAGdsQtyInBarcode(dtl.barcode, gdsid, dtl.gdstype)
                                    .Where(e => e.vlddat.Trim() == dtl.vlddat.Trim() && e.bthno.Trim() == dtl.bthno.Trim())
                                    .ToArray();
                double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                double ktqty = bqty + dtl.qty;  //可调数量 = 库存数量+本单该商品的数量
                //如果 需调整数量 > 可调数量
                if (qty > ktqty)
                {
                    return RInfo("I0348", qty, ktqty);
                }
                //修改数量
                dtl.qty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
                dtl.pkgqty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
                dtl.preqty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);

                try
                {
                    //WmsDc.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, mst);
                    //检查单号是否已经审核
                    if (mst!=null && mst.chkflg == GetY())
                    {
                        return RInfo("I0207");
                    }

                    //修改主单时间戳
                    string sql = @"update wms_cang_110 set bllid='110' where wmsno='" + mst.wmsno + "' and bllid='110' and udtdtm={0}";
                    int iEff = WmsDc.ExecuteCommand(sql, mst.udtdtm);
                    if (iEff == 0)
                    {
                        return RInfo("I0207");
                    }

                    WmsDc.SubmitChanges();
                    scop.Complete();
                    return RSucc("成功", arrqrydtl[0], "S0152");
                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0046");
                }
            }
        }

        /// <summary>
        /// 新增返厂单商品
        /// </summary>
        /// <param name="wmsno">返厂单单号</param>
        /// <param name="barcodes">仓位信息</param>
        /// <param name="gdsids">要损溢的货号</param>        
        /// <param name="gdstypes">商品类型</param>
        /// <param name="qtys">商品数量</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_退厂制单, pwrdes = "退厂制单")]
        public ActionResult AdRetPrvs(String wmsno, String barcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys)
        {
            using (TransactionScope scop = new TransactionScope(TransactionScopeOption.Required, options))
            {
                //判断barcodes、gdsids、gdstypes、qtys是否数量一致
                String[] barcode = barcodes.Split(',');
                String[] gdsid = gdsids.Split(',');
                String[] qty = qtys.Split(',');
                String[] gdstype = gdstypes.Split(',');
                String[] bthno = bthnos.Split(',');
                String[] vlddat = vlddats.Split(',');
                List<object> retObjs = new List<object>();
                if (
                    (barcode.Length != gdsid.Length)
                || (barcode.Length != qty.Length)
                    || (gdsid.Length != qty.Length)
                || (gdstype.Length != gdsid.Length)
                    )
                {
                    return RInfo("I0349");
                }

                for (int i = 0; i < gdsid.Length; i++)
                {
                    double d = 0;
                    if (!double.TryParse(qty[i], out d))
                    {
                        return RInfo("I0350", gdsid[i], qty[i]);
                    }

                    JsonResult jr = (JsonResult)AdRetPrv(wmsno, barcode[i], gdsid[i], gdstype[i], bthno[i], vlddat[i], d);
                    ResultMessage rm = (ResultMessage)jr.Data;
                    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return jr;
                    }
                    retObjs.Add(rm.ResultObject);
                }

                //修改主单时间戳
                //检查单号是否存在
                var qrymst = from e in WmsDc.wms_cang_110
                             where e.wmsno == wmsno
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             select e;
                wms_cang_110 mst = qrymst.FirstOrDefault();
                if (mst == null)
                {
                    return RNoData("N0258");
                }
                string sql = @"update wms_cang_110 set bllid='110' where wmsno='" + mst.wmsno + "' and bllid='110' and udtdtm={0}";
                int iEff = WmsDc.ExecuteCommand(sql, mst.udtdtm);
                if (iEff == 0)
                {
                    return RInfo("I0207");
                }

                WmsDc.SubmitChanges();
                scop.Complete();
                return RSucc("成功", retObjs, "S0153");
            }
        }

        /// <summary>
        /// 新增返厂单商品
        /// </summary>
        /// <param name="wmsno">返厂单单号</param>
        /// <param name="barcode">仓位信息</param>
        /// <param name="gdsid">要损溢的货号</param>        
        /// <param name="gdstype">商品类型</param>
        /// <param name="qty">商品数量</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_退厂制单, pwrdes = "退厂制单")]
        public ActionResult AdRetPrv(String wmsno, String barcode, String gdsid, String gdstype, String bthno, String vlddat, double qty)
        {
            //判断分区是否有效
            if (!IsExistBarcode(barcode))
            {
                return RInfo( "I0351",barcode.Trim()  );
            }

            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_110
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_RETPRV
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_110
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_RETPRV
                         orderby e.rcdidx descending
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0163");
            }
            //检查是否有数据权限
            wms_cang_110 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0352" );
            //}

            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0353" );
            }
            //检查单号是否已经审核
            if (mst!=null && mst.chkflg == GetY())
            {
                return RInfo( "I0354" );
            }

            //判断gdsid和barcode是不是在一个区
            String[] qu = GetQuByGdsid(gdsid, LoginInfo.DefStoreid);
            if (!qu.Contains(barcode.Substring(0, 2)))
            {
                return RInfo( "I0355", gdsid, String.Join(",", qu));
            }

            //如果是报损，判断是否有库存            
            if (!HasQtyInBarcode(barcode, gdsid, gdstype))
            {
                return RInfo( "I0356",gdsid ,barcode  );
            }
            if (mst.times.Trim() == "-")
            {                
                //如果是报损，判断是否有库存                
                //得到一个商品的库存数量
                GdsInBarcode[] gb = GetAGdsQtyInBarcode(barcode, gdsid, gdstype)
                                    .Where(e => e.vlddat == vlddat.Trim() && e.bthno == bthno.Trim())
                                            .ToArray();
                double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                double ktqty = bqty;  //可调数量 = 库存数量+本单该商品的数量
                //如果 需调整数量 > 可调数量
                if (qty > ktqty)
                {
                    return RInfo( "I0357",qty ,ktqty );
                }                
            }   


            //判断商品是否已经再单据里面
            int iHasIn = arrqrydtl.Where(e => e.gdsid.Trim() == gdsid.Trim() && e.gdstype.Trim() == gdstype.Trim() && e.barcode.Trim() == barcode.Trim() && e.bthno.Trim() == bthno.Trim() && e.vlddat.Trim() == vlddat.Trim()).Count();
            if (iHasIn > 0)
            {
                return RInfo( "I0358",gdsid );
            }

            wms_cangdtl_110 dtl = new wms_cangdtl_110();
            dtl.wmsno = wmsno;
            dtl.bllid = WMSConst.BLL_TYPE_RETPRV;
            dtl.rcdidx = arrqrydtl[0].rcdidx + 1;
            dtl.barcode = barcode;
            dtl.gdsid = gdsid;
            dtl.gdstype = gdstype;
            dtl.pkgid = "01";
            double fQty = qty;
            dtl.qty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
            dtl.preqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
            dtl.pkgqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
            dtl.gdstype = gdstype;
            dtl.bthno = string.IsNullOrEmpty(bthno.Trim()) ? "1" : bthno.Trim();
            dtl.vlddat = string.IsNullOrEmpty(vlddat.Trim()) ? GetCurrentDay() : vlddat.Trim();
            JsonResult jr = (JsonResult)GetBcdByGdsid(gdsid);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return RInfo( "I0359",gdsid );
            }
            bcd[] b = (bcd[])rm.ResultObject;
            dtl.bcd = b[0].bcd1;
            dtl.bkr = "";
            dtl.bokflg = GetN();
            dtl.bokdat = GetCurrentDate();
            
            WmsDc.wms_cangdtl_110.InsertOnSubmit(dtl);
            try
            {
                //WmsDc.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, mst);
                //检查单号是否已经审核
                if (mst!=null && mst.chkflg == GetY())
                {
                    return RInfo("I0207");
                }

                
                WmsDc.SubmitChanges();
                return RSucc("成功", dtl, "S0154");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0047");
            }
        }

        /// <summary>
        /// 修改返厂单
        /// </summary>
        /// <param name="wmsno">返厂单单号</param>
        /// <param name="barcodes">仓位信息</param>
        /// <param name="gdsids">要损溢的货号</param>        
        /// <param name="gdstypes">商品类型</param>
        /// <param name="qtys">商品数量</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_退厂查询, pwrdes = "退厂查询")]
        public ActionResult MdRetPrvBll(String wmsno, String barcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys)
        {
            using (TransactionScope scop = new TransactionScope(TransactionScopeOption.Required, options))
            {
                //拆分参数
                //检查并创建明细
                JsonResult jr = (JsonResult)_MakeParam(wmsno, barcodes, gdsids, gdstypes, bthnos, vlddats, qtys);
                ResultMessage rm = (ResultMessage)jr.Data;
                if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return jr;
                }

                //检查单号是否存在
                var qrymst = from e in WmsDc.wms_cang_110
                             where e.wmsno == wmsno
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             select e;
                var arrqrymst = qrymst.ToArray();
                var qrydtl = from e in WmsDc.wms_cangdtl_110
                             where e.wmsno == wmsno
                             && e.bllid == WMSConst.BLL_TYPE_RETPRV
                             select e;
                var arrqrydtl = qrydtl.ToArray();
                //单据是否找到
                if (arrqrymst.Length <= 0)
                {
                    return RNoData("N0164");
                }
                //检查是否有数据权限
                wms_cang_110 mst = arrqrymst[0];
                ////正在生成拣货单，请稍候重试
                //string quRetrv = mst.qu;
                //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                //{
                //    return RInfo( "I0360" );
                //}

                if (!qus.Contains(mst.qu.Trim()))
                {
                    return RInfo("I0361");
                }
                //检查单号是否已经审核
                if (mst!=null && mst.chkflg == GetY())
                {
                    return RInfo("I0362");
                }
                //是否是同一个人制单
                if (!IsSameLogin(mst.mkr))
                {
                    return RInfo("I0363", mst.mkr, LoginInfo.Usrid);
                }

                wms_cangdtl_110[] newdtl = (wms_cangdtl_110[])rm.ResultObject;
                if (mst.times.Trim() == "-")
                {
                    int i = 0;
                    //如果是报损，判断是否有库存
                    foreach (wms_cangdtl_110 d in newdtl)
                    {
                        //得到一个商品的库存数量
                        GdsInBarcode[] gb = GetAGdsQtyInBarcode(arrqrydtl[i].barcode, arrqrydtl[i].gdsid, arrqrydtl[i].gdstype)
                                            .Where(e => e.bthno.Trim() == arrqrydtl[i].bthno.Trim() && e.vlddat.Trim() == arrqrydtl[i].vlddat.Trim())
                                            .ToArray();
                        double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                        double ktqty = bqty + arrqrydtl[i].qty;  //可调数量 = 库存数量+本单该商品的数量
                        //如果 需调整数量 > 可调数量
                        if (d.qty > ktqty)
                        {
                            return RInfo("I0364", d.qty, ktqty);
                        }
                        i++;
                    }
                }


                //删除单据明细            
                WmsDc.wms_cangdtl_110.DeleteAllOnSubmit(arrqrydtl);
                iDelCangDtl110(arrqrydtl, mst);
                //增加单据明细            
                WmsDc.wms_cangdtl_110.InsertAllOnSubmit(newdtl);

                try
                {
                    //WmsDc.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, mst);
                    //检查单号是否已经审核
                    if (mst!=null && mst.chkflg == GetY())
                    {
                        return RInfo("I0207");
                    }

                    //修改主单时间戳
                    string sql = @"update wms_cang_110 set bllid='110' where wmsno='" + mst.wmsno + "' and bllid='110' and udtdtm={0}";
                    int iEff = WmsDc.ExecuteCommand(sql, mst.udtdtm);
                    if (iEff == 0)
                    {
                        return RInfo("I0207");
                    }

                    WmsDc.SubmitChanges();
                    scop.Complete();
                    return RSucc("成功", newdtl, "S0155");
                }
                catch (Exception ex)
                {
                    return RErr(ex.Message, "E0048");
                }
            }
        }

        /// <summary>
        /// 得到返厂单明细
        /// </summary>
        /// <param name="wmsno">单号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_退厂查询, pwrdes = "退厂查询")]
        public ActionResult GetRetPrvBllDtl(String wmsno)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_110
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_RETPRV
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_110
                         join e4 in WmsDc.wms_cang_110 on new { e.wmsno, e.bllid } equals new { e4.wmsno, e4.bllid }
                         join e5 in WmsDc.emp on e4.ckr equals e5.empid
                         into empJoin from e6 in empJoin.DefaultIfEmpty()
                         join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                         join e2 in WmsDc.wms_pkg on new { e1.gdsid } equals new { e2.gdsid }
                         into joinPkg
                         from e3 in joinPkg.DefaultIfEmpty()
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_RETPRV
                         select new
                         {
                             e4.chkflg,
                             e4.ckr,
                             e4.chkdat,
                             ckrdes = e6.empdes.Trim(),
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
                             e.qty,
                             e.rcdidx,
                             e.tpcode,
                             e.vlddat,
                             e.wmsno,
                             e1.gdsdes,
                             e1.spc,
                             e1.bsepkg,
                             pkg03 = GetPkgStr(e.qty, e3.cnvrto, e3.pkgdes),
                             pkg03pre = GetPkgStr(e.preqty, e3.cnvrto, e3.pkgdes),
                         };
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0165");
            }
            //单据是否找到
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0166");
            }
            //检查是否有数据权限
            wms_cang_110 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0365" );
            }

            return RSucc("成功", arrqrydtl, "S0156");
        }

        /// <summary>
        /// 得到返厂单主单
        /// </summary>
        /// <param name="wmsno">单号</param>
        /// <returns></returns>
        [PWR(Pwrid=WMSConst.WMS_BACK_退厂查询, pwrdes="退厂查询")]
        public ActionResult GetRetPrvBll(String wmsno)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_110
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_RETPRV
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_110
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_RETPRV
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0167");
            }

            //检查是否有数据权限
            wms_cang_110 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0366" );
            }

            return RSucc("成功", arrqrymst, "S0157"); 
        }

        /// <summary>
        /// 退厂查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="wmsno">调整单单号</param>
        /// <param name="gdsid">商品货号、条码</param>
        /// <param name="barcode">仓位</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_退厂查询, pwrdes = "退厂查询")]
        public ActionResult FindBll(String begindat, String enddat, String wmsno, String gdsid, String barcode)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo( "I0367",barcode.Trim()  );
            }
            var arrqrymst = FindBllFromCangMst110(WMSConst.BLL_TYPE_RETPRV, begindat, enddat, wmsno, gdsid, barcode);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0168");
            }
            return RSucc("成功", arrqrymst, "S0158");
        }
    }
}
