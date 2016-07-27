using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;

namespace WMS.Controllers
{
    /// <summary>
    /// 盘点单模块
    /// </summary>
    public class InvCkController : SsnController
    {

        public InvCkController()
        {
            Mdlid = "InvCk";
            Mdldes = "盘点单模块";
        }


        /// <summary>
        /// 得到要盘点,并且有权限的barcode
        /// </summary>
        /// <param name="boci">盘点波次</param>
        /// <returns></returns>
        private String[] GetBarcodesByBoci(String boci)
        {
            var qryset = from e in WmsDc.wms_pdset
                         join e1 in WmsDc.wms_pdsetdtl on e.pdno equals e1.pdno
                         where e.pdno == boci && savdpts.Contains(e1.savdptid.Trim())
                         && e.peisong == LoginInfo.DefStoreid
                         select e1;
            var qry0 = from e in WmsDc.wms_cangwei
                       join e1 in qryset on new { e.savdptid, e.barcode } equals new { e1.savdptid, e1.barcode }
                       select e;
            var qry1 = from e in WmsDc.wms_cangwei
                       join e1 in qryset on new { e.savdptid, e.cang } equals new { e1.savdptid, e1.cang }
                       select e;
            var qry2 = from e in WmsDc.wms_cangwei
                       join e1 in qryset on new { e.savdptid, e.ceng } equals new { e1.savdptid, e1.ceng }
                       select e;
            var qry3 = from e in WmsDc.wms_cangwei
                       join e1 in qryset on new { e.savdptid, e.huojia } equals new { e1.savdptid, e1.huojia }
                       select e;
            var qry4 = from e in WmsDc.wms_cangwei
                       join e1 in qryset on new { e.savdptid, e.tongdao } equals new { e1.savdptid, e1.tongdao }
                       select e;
            var qry5 = from e in WmsDc.wms_cangwei
                       join e1 in qryset on new { e.savdptid, e.qu } equals new { e1.savdptid, e1.qu }
                       select e;
            //var qry = qry1.Union(qry2).Union(qry3).Union(qry4).Union(qry5).Union(qry0);
            var qry = qry0;

            String[] ret = qry.Select(e => e.barcode).ToArray();

            return ret;
        }

        /// <summary>
        /// 是否已经盘过点了
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private bool HasChecked(wms_cangdtl_105 d)
        {
            /*var q1 = from e in WmsDc.wms_cang_105
                     join e1 in WmsDc.wms_cang_105 on new { e.lnkbocino, e.times } equals new { e1.lnkbocino, e1.times }
                     where e.wmsno == d.wmsno && e.times == "2"
                     select e1.wmsno;*/
            var qry = from e in WmsDc.wms_cangdtl_105
                      join e1 in WmsDc.wms_cang_105 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      where
                          //q1.Contains(e.wmsno.Trim()) 
                      e1.lnkbocino == d.oldbarcode.Trim()
                      && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                      && e1.times == "2"
                      && e.gdsid == d.gdsid.Trim() && e.gdstype == d.gdstype.Trim() //&& e.bthno == d.bthno && e.vlddat == d.vlddat
                      && (e1.savdptid == LoginInfo.DefSavdptid || e1.savdptid == LoginInfo.DefCsSavdptid)
                      && e.barcode == d.barcode.Trim()
                      select e;
            var arrqry = qry.ToArray();
            return arrqry.Length > 0;
        }

        private ActionResult _MakeParam(String wmsno, String oldbarcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys)
        {
            String[] oldbarcode = oldbarcodes.Split(',');
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] gdstype = gdstypes.Split(',');
            String[] bthno = bthnos.Split(',');
            String[] vlddat = vlddats.Split(',');
            //String[] newsbarcode = newbarcodes.Split(',');
            List<wms_cangdtl_105> lstDtl = new List<wms_cangdtl_105>();
            if ((oldbarcode.Length != gdsid.Length)
                && (oldbarcode.Length != qty.Length)
                && (oldbarcode.Length != gdstype.Length))
            {
                return RInfo( "I0131" );
            }

            //检查盘点抄账单是否有单据
            var qrycz = from e in WmsDc.wms_cang_105
                        where e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                        && e.wmsno == wmsno && e.times == "2"
                        select e;
            var arrqrycz = qrycz.ToArray();
            if (arrqrycz.Length <= 0)
            {
                return RNoData("N0087");                
            }
            //判断盘点是否结束，结束不允许制单
            wms_cang_105 czmst = arrqrycz[0];
            if (czmst.chkflg == GetY())
            {
                return RInfo( "I0132" );
            }
            //盘点传来的参数是否有barcode的权限
            String[] pwdBarcodes = GetBarcodesByBoci(czmst.lnkbocino);
            if (pwdBarcodes == null)
            {
                return RInfo( "I0133" );
            }
            var qryPdcang = from e in oldbarcode
                            where pwdBarcodes.Contains(e.Trim())
                            select e;
            if (qryPdcang.Count() <= 0)
            {
                return RNoData("N0088");
            }

            //查询idx最大值
            var qrymx = from e in WmsDc.wms_cangdtl_105
                        where e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                        && e.wmsno == wmsno
                        orderby e.rcdidx descending
                        select e;
            var arrqrymx = qrymx.ToArray();
            int i = 0, ii=0;
            if (arrqrymx.Length > 0)
            {
                i = arrqrymx[0].rcdidx;
            }
            foreach (String s in oldbarcode)
            {
                if (!String.IsNullOrEmpty(s))
                {
                    //判断分区是否有效
                    if (!IsExistBarcode(s))
                    {
                        return RInfo( "I0134",s.Trim()  );
                    }
                    wms_cangdtl_105 dtl = new wms_cangdtl_105();
                    dtl.wmsno = wmsno;
                    dtl.bllid = WMSConst.BLL_TYPE_INVENTORY_CHECK;
                    dtl.rcdidx = i + 1;
                    dtl.barcode = s;
                    dtl.gdsid = gdsid[ii];
                    dtl.gdstype = gdstype[ii];
                    dtl.pkgid = "01";
                    double fQty = 0;
                    if (!double.TryParse(qty[ii], out fQty))
                    {
                        return RInfo( "I0135",gdsid[i],qty[ii]  );
                    }
                    dtl.qty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero); 
                    dtl.preqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.pkgqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.gdstype = gdstype[ii];
                    dtl.bthno = string.IsNullOrEmpty(bthno[ii]) ? "1" : bthno[ii];
                    dtl.vlddat = string.IsNullOrEmpty(vlddat[ii]) ? GetCurrentDay() : vlddat[ii];
                    if (gdsid[ii] != "1")
                    {
                        JsonResult jr = (JsonResult)GetBcdByGdsid(gdsid[ii]);
                        ResultMessage rm = (ResultMessage)jr.Data;
                        if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                        {
                            return RInfo( "I0136",gdsid[ii] );
                        }
                        bcd[] b = (bcd[])rm.ResultObject;
                        dtl.bcd = b[0].bcd1;
                    }
                    else
                    {
                        dtl.bcd = "";
                    }
                    dtl.bkr = "";
                    dtl.bokflg = GetN();
                    dtl.bokdat = "";

                    lstDtl.Add(dtl);
                    i++;
                    ii++;
                }
            }

            return RSucc("成功", lstDtl.ToArray(), "S0077");
        }

        /// <summary>
        /// 得到会计期间的盘点单
        /// </summary>
        /// <param name="fscprdid"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点制单, pwrdes = "盘点制单")]
        public ActionResult GetBllsByFscprdid(String fscprdid)
        {
            var qry = from e in WmsDc.wms_cang_105
                      where e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                      && qus.Contains(e.qu.Trim())
                      && e.times == "2"
                      && e.mkedat.Substring(2, 4) == fscprdid
                      //&& e.mkedat.Substring(0,8) == GetCurrentDay()
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
                          dtls = (from edtl in WmsDc.wms_cangdtl_105
                                  join gd in WmsDc.gds on edtl.gdsid equals gd.gdsid
                                  join e3 in WmsDc.wms_pkg on new { gd.gdsid } equals new { e3.gdsid }
                                  into joinPkg from e4 in joinPkg.DefaultIfEmpty()
                                  where edtl.wmsno == e.wmsno && edtl.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
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
                                      gd.bsepkg,
                                      e4.cnvrto,
                                      pkgdes = e4.pkgdes.Trim(),
                                      pkg03= GetPkgStr(edtl.qty, e4.cnvrto, e4.pkgdes),
                                      pkg03pre = GetPkgStr(edtl.preqty, e4.cnvrto, e4.pkgdes)
                                  }).ToArray()
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0089");
            }

            return RSucc("成功", arrqry, "S0078");
        }
        /// <summary>
        /// 新制盘点单
        /// </summary>
        /// <param name="boci">盘点抄账单单号（wms_cang_105里面times=1的单据）</param>
        /// <param name="qu">分区</param>
        /// /*, String barcodes, String gdsids, String gdstypes, String qtys*/
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点制单, pwrdes = "盘点制单")]
        public ActionResult MkInvCkBll(String boci, String qu)
        {            
            String savdptid = GetSavdptidByQu(qu);
            ////正在生成拣货单，请稍候重试
            //string quRetrv = qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0137" );
            //}

            //盘点区在不在设置区内
            if (!IsInSetQu(boci, qu))
            {
                return RInfo( "I0138" );
            }

            return MakeNewBllNo(savdptid, qu, WMSConst.BLL_TYPE_INVENTORY_CHECK, (bllno) =>
            {
                //检查并创建明细
                /*JsonResult jr = (JsonResult)_MakeParam(bllno, barcodes, gdsids, gdstypes, qtys);
                ResultMessage rm = (ResultMessage)jr.Data;
                if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return rm;
                }
                wms_cangdtl_105[] dtls = (wms_cangdtl_105[])rm.ResultObject;*/

                ResultMessage rm = new ResultMessage();
                wms_cang_105 mst = new wms_cang_105();
                //创建主表                
                mst.wmsno = bllno;
                mst.bllid = WMSConst.BLL_TYPE_INVENTORY_CHECK;
                mst.savdptid = savdptid;
                mst.prvid = "";
                //String qu = GetQuByGdsid(dtls[0].gdsid, LoginInfo.DefSavdptid);
                mst.qu = qu;
                mst.rcvdptid = "";                
                mst.times = "2";
                mst.lnkbocino = boci;
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

                if (HasCkBll(boci, qu, LoginInfo.Usrid))
                {
                    return RRInfo("I0440" ,boci  ,qu );

                }

                WmsDc.wms_cang_105.InsertOnSubmit(mst);
                //WmsDc.wms_cangdtl_105.InsertAllOnSubmit(dtls);

                try
                {
                    WmsDc.SubmitChanges();
                    return RRSucc("成功", mst, "S0216");

                }
                catch (Exception ex)
                {
                    return RRErr(ex.Message, "E0063");

                }
            });
        }

        /// <summary>
        /// 判断分区是否在设置分区内
        /// </summary>
        /// <param name="boci"></param>
        /// <param name="qu"></param>
        /// <returns></returns>
        private bool IsInSetQu(string boci, string qu)
        {
            var qry = from e in WmsDc.wms_pdset
                      join e1 in WmsDc.wms_pdsetdtl on e.pdno equals e1.pdno
                      where e.pdno == boci
                      && e1.qu == qu && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      select e1;
            foreach (var q in qry)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 盘点当天是否有盘点单据
        /// </summary>
        /// <param name="boci"></param>
        /// <param name="qu"></param>
        /// <returns></returns>
        public ActionResult HasCkBll(string boci, string qu)
        {
            if (HasCkBll(boci, qu, LoginInfo.Usrid))
            {
                return RInfo( "I0139" );
            }

            return RSucc("尚无盘点单", null, "S0079");
        }

        private bool HasCkBll(string boci, string qu, string usrid)
        {
            var qry = from e in WmsDc.wms_cang_105
                      where e.lnkbocino == boci && e.qu == qu
                      && (e.savdptid == LoginInfo.DefCsSavdptid || e.savdptid == LoginInfo.DefSavdptid)
                      && e.mkr == usrid
                      && e.mkedat == GetCurrentDay()
                      && (e.chkflg !=GetY())
                      && e.times=="2"
                      select e;
            foreach (wms_cang_105 m in qry)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 盘点单明细删除
        /// </summary>
        /// <param name="wmsno">盘点单单号</param>
        /// <param name="gdsid">货号</param>
        /// <param name="rcdidx">序号</param>
        /// <returns></returns>        
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点制单, pwrdes = "盘点制单")]
        public ActionResult DlInvCkBllDtl(String wmsno, String gdsid, int rcdidx)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            var arrqrydtl1 = qrydtl.Where(e=>e.gdsid == gdsid && e.rcdidx == rcdidx).ToArray();            
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0090");
            }
            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0140" );
            //}
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0141" );
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0142" );
            }
            //检查是否查询到明细数据
            if (arrqrydtl1.Length <= 0)
            {
                return RNoData("N0091");
            }
            //检查明细是否有审核的
            if (arrqrydtl1[0].bokflg == GetY())
            {
                return RInfo( "I0143" );
            }
            //删除单据明细
            int iDtlCnt = arrqrydtl.Length;
            WmsDc.wms_cangdtl_105.DeleteAllOnSubmit(arrqrydtl1);
            iDelCangDtl105(arrqrydtl1, arrqrymst[0]);
            if (iDtlCnt == 1)
            {
                WmsDc.wms_cang_105.DeleteAllOnSubmit(arrqrymst);
                iDelCangMst105(arrqrymst[0]);
            }
            //删除主单据
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0080");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0016");
            }
        }
        /// <summary>
        /// 盘点单明细修改
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="rcdidx"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public ActionResult MdInvCkBllDtl(String wmsno, String gdsid, int rcdidx, double qty)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         where e.wmsno == wmsno
                         && e.gdsid == gdsid && e.rcdidx == rcdidx
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrydtl = qrydtl.ToArray();            

            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0092");
            }
            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];

            

            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0144" );
            //}

            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0145" );
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0146" );
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0093");
            }           
            wms_cangdtl_105 dtl = arrqrydtl[0];
            //明细如果已经确认就不能修改明细了
            if (dtl.bokflg == GetY())
            {
                return RInfo( "I0147" );
            }
            dtl.qty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            dtl.pkgqty = dtl.qty;
            dtl.preqty = dtl.qty;
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0081");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0017");
            }
        }
        /// <summary>
        /// 删除盘点单
        /// </summary>
        /// <param name="wmsno">盘点单单号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点制单, pwrdes = "盘点制单")]
        public ActionResult DlInvCkBll(String wmsno)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0094");
            }
            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0148" );
            //}

            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0149" );
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0150" );
            }
            //检查明细是否有审核的
            foreach (wms_cangdtl_105 d in arrqrydtl)
            {
                if (d.bokflg == GetY())
                {
                    return RInfo( "I0151" );
                }
            }
            //删除单据明细
            WmsDc.wms_cangdtl_105.DeleteAllOnSubmit(arrqrydtl);
            WmsDc.wms_cang_105.DeleteAllOnSubmit(arrqrymst);
            iDelCangDtl105(arrqrydtl, mst);
            iDelCangMst105(mst);
            //删除主单据
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0082");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0018");
            }
        }
        /// <summary>
        /// 新增盘点单
        /// </summary>
        /// <param name="wmsno">盘点单单号</param>
        /// <param name="barcodes">仓位信息</param>
        /// <param name="gdsids">要盘点的货号（gdsid=1为空仓位的货号）</param>        
        /// <param name="gdstypes">商品类型(空仓位的gdstype=50)</param>
        /// <param name="qtys">商品数量</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点制单, pwrdes = "盘点制单")]
        public ActionResult InstInvCkBll(String wmsno, String barcodes, String gdsids, String gdstypes, String bthnos, String vlddats, String qtys)
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
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         && e.times == "2"
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0095");
            }
            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0152" );
            //}

            
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0153" );
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0154" );
            }
            //删除单据明细            
            //WmsDc.wms_cangdtl_105.DeleteAllOnSubmit(arrqrydtl);

            //增加单据明细            
            wms_cangdtl_105[] newdtl = (wms_cangdtl_105[])rm.ResultObject;

            //判断newdtl里面有没有重复录入的数据
            var qrygrp = newdtl.GroupBy(e => new { e.barcode, e.gdsid, e.gdstype }).Where(e => e.Count() > 1);
            var arrqrygrp = qrygrp.Select(e=>e.Key).ToArray();
            if (arrqrygrp.Length > 0)
            {
                var g = arrqrygrp[0];
                return RInfo("I0155", g.gdsid, g.gdstype);                
            }

            //检查是否已经盘过点了
            foreach(wms_cangdtl_105 d in newdtl){
                //判断是否该商品是否在该区
                if (!dtqus.Contains(mst.qu) && d.gdsid.Trim()!="1")
                {
                    var hasPwrInQu = (from e in WmsDc.wms_set
                                      join e1 in WmsDc.gds on e.val2 equals e1.dptid
                                      where e.setid == "001" && e.val3 == mst.savdptid
                                      && e1.gdsid == d.gdsid.Trim() && e.val1==GetQuByBarcode(d.barcode.Trim())
                                      select e.val1.Trim()).FirstOrDefault();
                    if (hasPwrInQu==null || hasPwrInQu != mst.qu)
                    {
                        return RInfo("I0488");
                    }
                }

                String boci = GetBociByWmsno(d.wmsno);
                d.oldbarcode = boci;
                if(HasChecked(d)){
                    return RInfo("I0156",d.gdsid, d.gdstype);
                }
                d.oldbarcode = "";
            }

            WmsDc.wms_cangdtl_105.InsertAllOnSubmit(newdtl);
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", newdtl, "S0083");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0019");
            }
        }

        private string GetBociByWmsno(string wmsno)
        {
            var qry = from e in WmsDc.wms_cang_105
                      where e.times == "2" && e.wmsno == wmsno
                      && (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      select e.lnkbocino;
            foreach(string m in qry){
                return m;
            }
            return null;
        }
        /// <summary>
        /// 是否已经盘过点了
        /// </summary>
        /// <param name="boci">盘点批次</param>
        /// <param name="barcode">区位码</param>
        /// <param name="gdsid">货号</param>
        /// <param name="gdstype">商品类型</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点制单, pwrdes = "盘点制单")]
        public ActionResult HasChecked(String boci, String barcode, String gdsid, String gdstype, String bthno, String vlddat)
        {
            wms_cangdtl_105 d = new wms_cangdtl_105();
            d.oldbarcode = boci;    //oldbarcode 临时传入盘点批次
            d.barcode = barcode;
            d.gdsid = gdsid;
            d.gdstype = gdstype;
            d.bthno = bthno;
            d.vlddat = vlddat;
            //判断分区是否有效
            if (!IsExistBarcode(barcode))
            {
                return RInfo( "I0157",barcode.Trim()  );
            }

            if (HasChecked(d))
            {
                return RInfo( "I0158" );
            }
            return RSucc("该商品尚未盘过",null, "S0084");
        }
        /// <summary>
        /// 得到盘点单明细
        /// </summary>
        /// <param name="wmsno">单号</param>
        /// <returns></returns>        
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点查询, pwrdes = "盘点查询")]
        public ActionResult GetInvCkBllDtl(String wmsno)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                         into joinGds from e2 in joinGds.DefaultIfEmpty()
                         join e3 in WmsDc.wms_pkg on new { e2.gdsid } equals new { e3.gdsid }
                         into joinPkg from e4 in joinPkg.DefaultIfEmpty()
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
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
                             e.qty,
                             e.rcdidx,
                             e.tpcode,
                             e.vlddat,
                             e.wmsno,
                             e2.spc,
                             e2.gdsdes,
                             e2.bsepkg,
                             cnvrto = e4.cnvrto == null ? 0 : e4.cnvrto,
                             pkgdes = e4.pkgdes.Trim(),
                             pkg03 = GetPkgStr(e.qty, e4.cnvrto, e4.pkgdes),
                             pkg03pre = GetPkgStr(e.preqty, e4.cnvrto, e4.pkgdes)
                         };
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0096");
            }
            //单据是否找到
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0097");
            }
            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0159" );
            }

            return RSucc("成功", arrqrydtl, "S0085");
        }
        /// <summary>
        /// 得到盘点单主单
        /// </summary>
        /// <param name="wmsno">单号</param>
        /// <returns></returns>        
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点查询, pwrdes = "盘点查询")]
        public ActionResult GetInvCkBll(String wmsno)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.times == "2"
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         where e.wmsno == wmsno                         
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0098");
            }

            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo( "I0160" );
            }

            return RSucc("成功", arrqrymst, "S0086");
        }
        /// <summary>
        /// 得到要盘点的批次
        /// </summary>
        /// <param name="fscprdid"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点制单, pwrdes = "盘点制单")]
        public ActionResult GetInvBociByFscprdid(String fscprdid, String qu)
        {
            String curday = GetCurrentDay();
            var qry = from e in WmsDc.wms_pdset
                      join e1 in WmsDc.wms_cang_105 on new { e.pdno, times = "1" } equals new { pdno = e1.lnkbocino, times = e1.times }
                      join e2 in WmsDc.dpt on e.peisong equals e2.dptid
                      where e.fscprd == fscprdid
                      //&& e1.qu.Contains(qu)
                      && (e.peisong == LoginInfo.DefStoreid)
                      && e.pdstart == GetY() && e.pdover == GetN()
                      group new { e, e1, e2 } by new { e.actid, e.brief, e.fscprd, e.pddat, e.pdno, e.pdover, e.pdstart, e.peisong, e2.dptdes } into g
                      select new
                      {
                          g.Key.actid,
                          g.Key.brief,
                          g.Key.fscprd,
                          g.Key.pddat,
                          g.Key.pdno,
                          g.Key.pdover,
                          g.Key.pdstart,
                          g.Key.peisong,
                          peisongdes = g.Key.dptdes,
                          boci = g.Key.pdno
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0099");
            }

            return RSucc("成功",arrqry, "S0087");
        }
        /// <summary>
        /// 根据盘点波次单据查找该单据下的盘点单
        /// </summary>
        /// <param name="boci">盘点波次</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点查询, pwrdes = "盘点查询")]
        public ActionResult GetInvsByBoci(String boci, String qu)
        {
            var qry = from e in WmsDc.wms_cang_105
                      join e1 in WmsDc.emp on e.mkr equals e1.empid
                      where e.times == "2" //&& e.mkr == LoginInfo.Usrid
                      && e.lnkbocino == boci && e.qu == qu
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
                          mkrdes = e1.empdes,
                          e.opr,
                          e.prvid,
                          e.qu,
                          e.rcvdptid,
                          e.savdptid,
                          e.times,
                          e.wmsno
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0100");
            }
            return RSucc("成功", arrqry, "S0088");
        }


        public ActionResult GetDtlByBarcodeGdsid(String barcode, string gdsid)
        {
            String sql = "select * from wms_cangdtl_105 t1 "
                        + " join (select * from wms_cang_105 where mkedat='{0}' and times=2) t2 on t1.wmsno=t2.wmsno and t1.bllid=t2.bllid"
                        + " where t1.barcode={1} and t1.gdsid={2}";
            wms_cangdtl_105[] dl = WmsDc.ExecuteQuery<wms_cangdtl_105>(sql, GetCurrentDay(), barcode, gdsid).ToArray();
            if (dl == null || dl.Length == 0)
            {
                return RNoData("N0101");
            }

            return RSucc("成功", dl, "S0089");
        }

        /// <summary>
        /// 未确认的TOP20条
        /// </summary>
        /// <param name="boci"></param>        
        /// <returns></returns>
        public ActionResult GetUnAdtDtlTop20(string boci)
        {
            // 帐表的数据
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.lnkbocino == boci
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            wms_cang_105 mst = qrymst.FirstOrDefault();
            object[] dtls = (from e in WmsDc.wms_cangdtl_105
                             join e1 in WmsDc.wms_pkg on e.gdsid equals e1.gdsid
                             join e2 in WmsDc.wms_cang_105 on new { e.wmsno, e.bllid } equals new { e2.wmsno, e2.bllid }
                             join e3 in WmsDc.gds on e.gdsid equals e3.gdsid
                             join e4 in WmsDc.emp on e2.mkr equals e4.empid
                             where e2.lnkbocino == boci
                             && e2.chkflg == GetN()
                             && e.bokflg == GetN()
                             && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                             select new
                             {
                                 e2.chkflg,
                                 e.gdsid,
                                 e.rcdidx,
                                 e.tpcode,
                                 e.barcode,
                                 e.bcd,
                                 e.bkr,
                                 e.bllid,
                                 e.bokdat,
                                 e.bokflg,
                                 e.bthno,
                                 e.gdstype,
                                 e.oldbarcode,
                                 e.pkgid,
                                 e.pkgqty,
                                 e.preqty,
                                 e.qty,
                                 e.vlddat,
                                 e.wmsno,
                                 e3.spc,
                                 e3.bsepkg,
                                 e3.gdsdes,
                                 e2.mkr,
                                 mkrdes = e4.empdes.Trim(),
                                 e1.cnvrto,
                                 pkgdes = e1.pkgdes.Trim(),
                                 pkg03 = GetPkgStr(e.qty, e1.cnvrto, e1.pkgdes),
                                 pkg03pre = GetPkgStr(e.preqty, e1.cnvrto, e1.pkgdes)
                             }).ToArray();
            if (mst == null)
            {
                return RNoData("N0102");
            }
            if (dtls.Length == 0)
            {
                return RNoData("N0103");
            }

            return RSucc("成功", dtls, "S0090");
        }

        /// <summary>
        /// 通过仓位得到商品明细
        /// </summary>
        /// <param name="boci"></param>
        /// <param name="barcode"></param>
        /// <returns></returns>
        public ActionResult GetDtlByBarcode(string boci, string barcode)
        {
            // 帐表的数据
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.lnkbocino == boci
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            wms_cang_105 mst = qrymst.FirstOrDefault();
            object[] dtls = (from e in WmsDc.wms_cangdtl_105
                             join e1 in WmsDc.wms_pkg on e.gdsid equals e1.gdsid                             
                             join e2 in WmsDc.wms_cang_105 on new { e.wmsno, e.bllid } equals new { e2.wmsno,e2.bllid }
                             join e3 in WmsDc.gds on e.gdsid equals e3.gdsid
                             join e4 in WmsDc.emp on e2.mkr  equals e4.empid
                             where e2.lnkbocino == boci
                             && e2.chkflg == GetN()
                             && e.barcode == barcode.Trim()
                             && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                             select new
                             {
                                 e2.chkflg,                                 
                                 e.gdsid,
                                 e.rcdidx,
                                 e.tpcode,
                                 e.barcode,
                                 e.bcd,
                                 e.bkr,
                                 e.bllid,
                                 e.bokdat,
                                 e.bokflg,
                                 e.bthno,
                                 e.gdstype,
                                 e.oldbarcode,
                                 e.pkgid,
                                 e.pkgqty,
                                 e.preqty,
                                 e.qty,
                                 e.vlddat,
                                 e.wmsno,
                                 e3.spc, e3.bsepkg, e3.gdsdes,
                                 e2.mkr,
                                 mkrdes = e4.empdes.Trim(),
                                 e1.cnvrto,
                                 pkgdes = e1.pkgdes.Trim(),
                                 pkg03 = GetPkgStr(e.qty, e1.cnvrto, e1.pkgdes),
                                 pkg03pre = GetPkgStr(e.preqty, e1.cnvrto, e1.pkgdes)
                             }).ToArray();
            if (mst == null)
            {
                return RNoData("N0104");
            }
            if (dtls.Length == 0)
            {
                return RNoData("N0105");
            }

            return RSucc("成功", dtls, "S0091");
        }

        /// <summary>
        /// 取消复核盘点明细
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="rcdidx"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点取消确认, pwrdes = "盘点取消确认")]
        public ActionResult CancelAdtDtl(String wmsno, String gdsid, int rcdidx)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         where e.wmsno == wmsno
                         && e.gdsid == gdsid && e.rcdidx == rcdidx
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0106");
            }
            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0161" );
            //}

            /*if (!qus.Contains(mst.qu))
            {
                return RInfo( "I0162" );
            }*/
            // 判断有无重新复核的权限
            // 检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0163" );
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0107");
            }
            wms_cangdtl_105 dtl = arrqrydtl[0];

            dtl.bokflg = GetN();
            dtl.bokdat = "";
            dtl.bkr = "";
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0092");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0020");
            }
        }

        /// <summary>
        /// 复核盘点明细
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="rcdidx"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点确认, pwrdes = "盘点确认")]
        public ActionResult AdtInvChkDtl(String wmsno, String gdsid, int rcdidx, double? qty)
        {
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         where e.wmsno == wmsno
                         && e.gdsid == gdsid && e.rcdidx == rcdidx
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0108");
            }
            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0164" );
            //}

            /*if (!qus.Contains(mst.qu))
            {
                return RInfo( "I0165" );
            }*/
            // 判断有无重新复核的权限
            // 检查单号是否已经审核
            if ( mst.chkflg == GetY())
            {
                return RInfo( "I0166" );
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0109");
            }
            wms_cangdtl_105 dtl = arrqrydtl[0];
            if (dtl.bokflg == GetY())
            {
                return RInfo( "I0167" );
            }
            dtl.bokflg = GetY();
            dtl.bokdat = GetCurrentDay();
            dtl.bkr = LoginInfo.Usrid;
            if (qty != null)
            {
                dtl.qty = qty.Value;
                dtl.pkgqty = qty.Value;
                dtl.preqty = qty.Value;
            }
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0093");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0021");
            }
        }

        /// <summary>
        /// 复核盘点单
        /// </summary>
        /// <param name="wmsno">盘点单</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点审核, pwrdes = "盘点审核")]
        public ActionResult AdtInvChk(String wmsno)
        {
            
            //检查单号是否存在
            var qrymst = from e in WmsDc.wms_cang_105
                         where e.wmsno == wmsno
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_105
                         where e.wmsno == wmsno                         
                         && e.bllid == WMSConst.BLL_TYPE_INVENTORY_CHECK
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0110");
            }
            //检查是否有数据权限
            wms_cang_105 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0168" );
            //}

            /*if (!qus.Contains(mst.qu))
            {
                return RInfo( "I0169" );
            }*/
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0170" );
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0111");
            }
            
            //检查明细是否已经审核完毕
            foreach (wms_cangdtl_105 d in arrqrydtl)
            {
                if(d.bokflg!=GetY() && d.gdsid.Trim()!="1"){
                    return RInfo( "I0171" );
                }
            }

            //审核主单
            mst.chkflg = GetY();
            mst.chkdat = GetCurrentDay();
            mst.ckr = LoginInfo.Usrid;

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0094");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0022");
            }
        }

        /// <summary>
        /// 盘点查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="wmsno">调整单单号</param>
        /// <param name="gdsid">商品货号、条码</param>
        /// <param name="barcode">仓位</param>
        /// <param name="mkr">制单人</param>
        /// <param name="isAdt">是否已经审核</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_盘点查询, pwrdes = "盘点查询")]
        public ActionResult FindBll(String begindat, String enddat, String wmsno, String gdsid, String barcode, String mkr, string isAdt)
        {
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo( "I0172",barcode.Trim()  );
            }
            var arrqrymst = FindBllFromCangMst105(WMSConst.BLL_TYPE_INVENTORY_CHECK, begindat, enddat, wmsno, gdsid, barcode, mkr, isAdt);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0112");
            }
            return RSucc("成功", arrqrymst, "S0095");
        }
    }
}
