using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;
using WMS.Common;
using System.Data.SqlClient;

namespace WMS.Controllers
{
    /// <summary>
    /// 返仓模块
    /// </summary>
    public class RetCangController : SsnController
    {
        class ParamRetBll
        {
            public string Bcd { get; set; }
            public String Gdsid { get; set; }
            public double Qty { get; set; }
            public String Rsn { get; set; }
        }
        /// <summary>
        /// 返仓模块构造函数
        /// </summary>
        public RetCangController()
        {
            Mdlid = "RetCang";
            Mdldes = "返仓";
        }

        private bcd GetGdsid(String gdsid)
        {
            var qry = from e in WmsDc.gds
                      join e1 in WmsDc.bcd on e.gdsid equals e1.gdsid
                      where e.gdsid == gdsid
                      //&& (dpts.Length > 0 && dpts.Contains(e.dptid.Trim()))
                      select e1;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
            }
            return arrqry[0];
        }

        private ActionResult _MkParam(String gdsids, String qtys, String rsns)
        {
            if (gdsids == null)
            {
                return RInfo( "I0285" );
            }
            if (qtys == null)
            {
                return RInfo( "I0286" );
            }
            if (rsns == null)
            {
                return RInfo( "I0287" );
            }
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] rsn = rsns.Split(',');
            List<ParamRetBll> lstParam = new List<ParamRetBll>();            
            if (gdsid.Length != qty.Length)
            {
                return RInfo( "I0288" );
            }
            if (gdsid.Length != rsn.Length)
            {
                return RInfo( "I0289" );
            }
            for (int i = 0; i < qty.Length; i++)
            {
                if (!String.IsNullOrEmpty(gdsid[i]) && !string.IsNullOrEmpty(qty[i]))
                {
                    double f = 0;
                    if (!double.TryParse(qty[i], out f))
                    {
                        return RInfo( "I0290",gdsid[i],qty[i]  );
                    }
                    bcd agdsid = GetGdsid(gdsid[i]);
                    if (agdsid == null)
                    {
                        return RInfo( "I0291",gdsid[i] );
                    }
                    lstParam.Add(new ParamRetBll() { Gdsid = agdsid.gdsid, Bcd = agdsid.bcd1, Qty = f, Rsn = rsn[i] });
                }
            }

            return RSucc("成功", lstParam, "S0136");
        }

        /// <summary>
        /// 返回会计期间的返仓单
        /// </summary>
        /// <param name="fscprdid">会计期间</param>
        /// <param name="dptid">分店编码</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult GetRetBlls(String fscprdid/*, String dptid*/, String bdat, String edat)
        {
            if (string.IsNullOrEmpty(fscprdid))
            {
                fscprdid = GetCurrentFscprdid();
            }            
            String currDay = GetCurrentDay();
            var qry = from e in WmsDc.wms_cang_109
                      join e1 in WmsDc.emp on e.mkr equals e1.empid
                      join e2 in WmsDc.dpt on e.rcvdptid equals e2.dptid
                      where e.mkedat.Substring(2, 4) == fscprdid
                      //&& e.mkedat == currDay
                      //&& e.rcvdptid == dptid
                      && e.mkr == LoginInfo.Usrid
                      && e.bllid == WMSConst.BLL_TYPE_RETCANG
                      && e.savdptid == LoginInfo.DefCsSavdptid
                      && qus.Contains(e.qu.Trim())
                      select new
                      {
                          e.wmsno,
                          e.savdptid,
                          e.rcvdptid,
                          e.mkr,
                          e.mkedat,
                          e.lnkno,
                          e.chkflg,
                          e.chkdat,
                          e.ckr,
                          mkrdes = e1.empdes,
                          e2.dptdes,
                          e2.adr,
                          e2.tel
                      };
            if (string.IsNullOrEmpty(bdat))
            {
                bdat = GetCurrentDay();
            }
            if (string.IsNullOrEmpty(edat))
            {
                edat = GetNextDay();
            }
            if (!string.IsNullOrEmpty(bdat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(bdat) >= 0);
            }
            if (!string.IsNullOrEmpty(edat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(edat) < 0);
            }
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0148");
            }
            return RSucc("成功", arrqry, "S0137");
        }

        /// <summary>
        /// 返仓单明细
        /// </summary>
        /// <param name="wmsno">单号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult GetRetBllDetails(String wmsno)
        {
            var qus = GetPwrQus();
            var qry = from e in WmsDc.wms_cang_109
                      join e1 in WmsDc.wms_cangdtl_109 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.rcbakrsn on e1.brfdtl equals e3.rcbakrsnid
                      into joinRcbak
                      from e4 in joinRcbak.DefaultIfEmpty()
                      join e5 in WmsDc.wms_pkg on new { e2.gdsid } equals new { e5.gdsid}
                      into joinPkg from e6 in joinPkg.DefaultIfEmpty()
                      where e.bllid == WMSConst.BLL_TYPE_RETCANG
                      && e.wmsno == wmsno
                      && e.savdptid == LoginInfo.DefCsSavdptid
                      && qus.Contains(e.qu.Trim())
                      select new
                      {
                          e1.wmsno,
                          e.chkdat,
                          e.chkflg,
                          e.ckr,
                          e1.bllid,
                          e1.gdsid,
                          qty = Math.Round(e1.qty, 4, MidpointRounding.AwayFromZero),
                          e2.gdsdes,
                          e2.spc,
                          e2.bsepkg,
                          e2.isstpsal,
                          bcd = e1.bcd,
                          e1.brfdtl,
                          e4.rcbakrsndes,
                          e6.cnvrto,
                          pkgdes = e6.pkgdes.Trim(),
                          pkg03 = GetPkgStr(Math.Round(e1.qty, 4, MidpointRounding.AwayFromZero), e6.cnvrto,e6.pkgdes),
                          pkg03pre = GetPkgStr(Math.Round(e1.preqty.Value, 4, MidpointRounding.AwayFromZero), e6.cnvrto, e6.pkgdes)
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0149");
            }
            return RSucc("成功", arrqry, "S0138");
        }

        /// <summary>
        /// 删除单据明细
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsids"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult DlRetBllDetail(String wmsno, String gdsids)
        {
            String[] gdsid = gdsids.Split(',');
            var qrydtl = from e in WmsDc.wms_cangdtl_109
                         where e.wmsno == wmsno                         
                         && e.bllid == WMSConst.BLL_TYPE_RETCANG                         
                         select e;            
            var dlarrdtl = qrydtl.Where(w => gdsid.Contains(w.gdsid.Trim())).ToArray();
            var qrymst = from e in WmsDc.wms_cang_109
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_RETCANG
                         && e.mkr == LoginInfo.Usrid
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0150");
            }
            wms_cang_109 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0292" );
            //}
            if (mst.chkflg==GetY())
            {
                return RInfo( "I0293" );
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo( "I0294" );
            }

            if (dlarrdtl.Length <= 0)
            {
                return RNoData("N0151");
            }
            try
            {
                WmsDc.wms_cangdtl_109.DeleteAllOnSubmit(dlarrdtl);
                iDelCangDtl109(dlarrdtl, mst);
                //查询明细删除后，是否已经没有明细，没有明细就删除主单据
                var arrqrydtl = qrydtl.ToArray();               
                if (arrqrydtl.Length <= 1)
                {
                    WmsDc.wms_cang_109.DeleteOnSubmit(mst);
                    iDelCangMst109(mst);
                }
                WmsDc.SubmitChanges();
                return RSucc("删除成功",null, "S0139");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0039");
            }
        }

        /// <summary>
        /// 整单删除
        /// </summary>
        /// <param name="wmsnos"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult DlRetBlls(String wmsnos)
        {
            String[] wmsno = wmsnos.Split(',');
            String sInfo = "";
            if (wmsno.Length > 0)
            {
                foreach (String s in wmsno)
                {
                    var qrydtl = from e in WmsDc.wms_cangdtl_109
                                 where e.wmsno == s
                                 && e.bllid == WMSConst.BLL_TYPE_RETCANG                                 
                                 select e;
                    var arrqrydtl = qrydtl.ToArray();
                    var qrymst = from e in WmsDc.wms_cang_109
                                 where e.wmsno == s && e.bllid == WMSConst.BLL_TYPE_RETCANG
                                 && qus.Contains(e.qu.Trim())
                                 && e.mkr == LoginInfo.Usrid
                                 select e;
                    var arrqrymst = qrymst.ToArray();
                    if (arrqrymst.Length > 0)
                    {
                        wms_cang_109 mst = arrqrymst[0];
                        ////正在生成拣货单，请稍候重试
                        //string quRetrv = mst.qu;
                        //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                        //{
                        //    return RInfo( "I0295" );
                        //}

                        if (mst.chkflg == GetY())
                        {
                            sInfo += mst.wmsno + "单据已经审核，不能删除\r\n";
                        }
                        else if (!IsSameLogin(mst.mkr)) //是否是本人制单
                        {
                            return RInfo( "I0296" );
                        }
                        else
                        {
                            WmsDc.wms_cangdtl_109.DeleteAllOnSubmit(arrqrydtl);                            
                            WmsDc.wms_cang_109.DeleteOnSubmit(mst);
                            iDelCangDtl109(arrqrydtl, mst);
                            iDelCangMst109(mst);
                        }
                    }

                }
            }

            if (!String.IsNullOrEmpty(sInfo))
            {
                return RSucc(sInfo, null, "S0140");
            }

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0141");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0040");
            }
            
        }

        /// <summary>
        /// 返仓单明细增加        
        /// </summary>
        /// <param name="bllno">单号</param>
        /// <param name="dptid">分店编码</param>
        /// <param name="gdsids">货号/条码（数组）</param>
        /// <param name="hndno">手工单号</param>
        /// <param name="qtys">数量（数组）</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult AdRetBll(String bllno, String dptid, String hndno, String gdsids, String qtys, String rsns)
        {
            ////正在生成拣货单，请稍候重试
            //string quRetrv = GetQuByDptid(dptid, LoginInfo.DefStoreid);
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0297" );
            //}

            //检查是否有残损库权限
            if (String.IsNullOrEmpty(LoginInfo.DefCsSavdptid))
            {
                return RInfo( "I0298" );
            }            

            var qrymst = from e in WmsDc.wms_cang_109
                         where e.wmsno == bllno && e.bllid == WMSConst.BLL_TYPE_RETCANG
                         && e.mkr == LoginInfo.Usrid
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_cangdtl_109
                         where e.wmsno == bllno && e.bllid == WMSConst.BLL_TYPE_RETCANG
                         select e;
            var arrqrydtl = qrydtl.ToArray();

            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0152");
            }
            wms_cang_109 mst = arrqrymst[0];
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0299" );
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo( "I0300" );
            }
            

            //拆分货号，数量
            JsonResult jr = (JsonResult)_MkParam(gdsids, qtys, rsns);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return jr;
            }
            List<ParamRetBll> param = null;
            param = (List<ParamRetBll>)rm.ResultObject;
            
            //检查录入的商品有没有相同记录
            var qrySameGds = from e in param
                             group e by e.Gdsid into g
                             where g.Count()>1
                             select g.Key;
            var arrQrySameGds = qrySameGds.ToArray();
            if (arrQrySameGds.Length>0)
            {
                return RInfo( "I0301",arrQrySameGds[0]  );
            }
            

            //得到分区            
            //GetRealteQuResult rq = GetRealteQu("all", LoginInfo.DefCsSavdptid);
            var gdsqus = (from e in WmsDc.gds
                          join e1 in WmsDc.wms_set on new { setid = "001", e.dptid } equals new { e1.setid, dptid = e1.val2.Trim() }
                          join e2 in WmsDc.wms_set on new { setid = "008", storeid = "2", savdptid = e1.val3 }     //残损库对应的区
                                       equals new { e2.setid, storeid = e2.val2.Trim(), savdptid = e2.val1.Trim() }
                          where param.Select(ee => ee.Gdsid.Trim()).Contains(e.gdsid)
                          && e2.val3 == LoginInfo.DefStoreid
                          && e1.isvld == GetY() && e2.isvld == GetY()
                          && thqus.Contains(e1.val1.Trim())
                          select new { qu = e1.val1.Trim() }
                          ).Distinct().ToArray();
            if (gdsqus.Length == 0 || gdsqus.Length > 1)
            {
                return RInfo( "I0302" );
            }
            if (gdsqus[0].qu != mst.qu.Trim())
            {
                return RInfo( "I0303",gdsqus[0].qu ,mst.qu.Trim()  );
            }
            
            

            //开始制单

            //生成明细单  
            int i = 0;
            var qrymx = from e in arrqrydtl
                        where e.wmsno == bllno && e.bllid == WMSConst.BLL_TYPE_RETCANG                        
                        orderby e.rcdidx descending
                        select e;
            var arrqrymx = qrymx.ToArray();
            if (arrqrymx.Length > 0)
            {
                i = arrqrymx[0].rcdidx;
            }
            List<wms_cangdtl_109> lstDtl = new List<wms_cangdtl_109>();
            foreach (ParamRetBll r in param)
            {
                wms_cangdtl_109 dtl = new wms_cangdtl_109();
                dtl.wmsno = bllno;
                dtl.bllid = WMSConst.BLL_TYPE_RETCANG;
                dtl.rcdidx = i + 1;
                dtl.oldbarcode = "";
                wms_cangwei cw = GetBarcodeByGdsid(LoginInfo.DefCsSavdptid, r.Gdsid);
                if (cw == null)
                {
                    return RInfo(r.Gdsid + ",推荐仓位为空", "I0304");
                }
                dtl.barcode = cw.barcode;
                dtl.gdsid = r.Gdsid;
                dtl.pkgid = "01";
                dtl.pkgqty = Math.Round(r.Qty, 4, MidpointRounding.AwayFromZero);
                dtl.qty = Math.Round(r.Qty, 4, MidpointRounding.AwayFromZero);
                dtl.gdstype = "95";
                dtl.bthno = "1";
                dtl.vlddat = GetCurrentDay();
                dtl.bcd = r.Bcd;
                dtl.tpcode = "";
                dtl.bkr = "";
                dtl.bokflg = GetN();
                dtl.bokdat = "";
                dtl.preqty = null;
                dtl.brfdtl = r.Rsn.ToString();

                //看看数据库有没有已加的商品
                if (arrqrydtl.Length > 0)
                {
                    var qryhasgds = from e in arrqrydtl
                                    where e.wmsno == bllno && e.bllid == WMSConst.BLL_TYPE_RETCANG
                                    && e.gdsid == dtl.gdsid
                                    select e;
                    foreach(var q in qryhasgds){
                        return RInfo( "I0305",q.gdsid  );
                    }
                }

                lstDtl.Add(dtl);
                //修改推荐仓位标志                        
                i++;
            }

            WmsDc.wms_cangdtl_109.InsertAllOnSubmit(lstDtl);

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null, "S0142");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0041");
            }
        }

        protected bool HasHndno(String hndno){
            var qry = from e in WmsDc.wms_cang_109
                      where e.bllid == WMSConst.BLL_TYPE_RETCANG
                      && e.lnkno == hndno
                      select e;
            foreach(wms_cang_109 q in qry){
                return true;
            }
            return false;
        }

        /// <summary>
        /// 手工单号是否还没有使用
        /// </summary>
        /// <param name="hndno">手工单号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult HasNotAlrdyHndno(String hndno)
        {
            if (!HasHndno(hndno))
            {
                return RSucc("手工单号尚未使用", null, "S0143"); 
            }
            return RInfo( "I0306" );
        }

        /// <summary>
        /// 得到退货区
        /// </summary>
        /// <returns></returns>
        public ActionResult GetCsQus()
        {
            return RSucc("成功", thqus, "S0144");
        }

        /// <summary>
        /// 返仓单制单
        /// </summary>
        /// <param name="dptid">分店编码</param>
        /// <param name="gdsids">货号/条码（数组）</param>
        /// <param name="gdsids">手工单号</param>
        /// <param name="qtys">数量（数组）</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult MkRetBll(String dptid, String hndno,  String gdsids, String qtys, String rsns, String qu)
        {
            /*if(GetCsQuByDptid(dptid, LoginInfo.DefStoreid)!=qu.Trim()){
                return RInfo("I0468");
            }*/
            if(string.IsNullOrEmpty(qu)){
                return RInfo( "I0307" );
            }

            //检查是否有残损库权限
            if(String.IsNullOrEmpty(LoginInfo.DefCsSavdptid)){
                return RInfo( "I0308" );
            }

            //判断分店是不是操作的仓库（残损）
            if (!IsInSavdptid(dptid))
            {
                return RInfo( "I0309" );
            }

            //拆分货号，数量
            JsonResult jr = (JsonResult)_MkParam(gdsids, qtys, rsns);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return jr;
            }
            List<ParamRetBll> param = null;
            param = (List<ParamRetBll>)rm.ResultObject;

            //得到分区
            //GetRealteQuResult rq= GetRealteQu("all", LoginInfo.DefCsSavdptid);

            //手工单号不能重复
            if (HasHndno(hndno))
            {
                return RInfo( "I0310",hndno );
            }

            //开始制单
            return MakeNewBllNo(LoginInfo.DefCsSavdptid, qu, WMSConst.BLL_TYPE_RETCANG, (bllno) =>
                {
                    
                    //生成主单
                    wms_cang_109 mst = new wms_cang_109();
                    mst.wmsno = bllno;                   
                    mst.bllid = WMSConst.BLL_TYPE_RETCANG;
                    mst.savdptid = LoginInfo.DefCsSavdptid;
                    mst.prvid = "";
                    mst.qu = qu;
                    ////正在生成拣货单，请稍候重试
                    //string quRetrv = mst.qu;
                    //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
                    //{
                    //    return RRInfo( "I0311" );
                    //}
                    mst.rcvdptid = dptid;
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
                    mst.lnkno = hndno;
                    mst.lnkbrief = "";

                    //生成明细单  
                    int i = 0;
                    List<wms_cangdtl_109> lstDtl = new List<wms_cangdtl_109>();
                    foreach (ParamRetBll r in param)
                    {
                        string dptid1 = (from e in WmsDc.gds where e.gdsid == r.Gdsid select e.dptid.Trim()).FirstOrDefault();
                        if (GetCsQuByDptid(dptid1, LoginInfo.DefStoreid) != qu.Trim())
                        {
                            return RRInfo("I0468");
                        }


                        wms_cangdtl_109 dtl = new wms_cangdtl_109();
                        dtl.wmsno = bllno;
                        dtl.bllid = WMSConst.BLL_TYPE_RETCANG;
                        dtl.rcdidx = i + 1;
                        dtl.oldbarcode = "";
                        wms_cangwei cw = GetBarcodeByGdsid(LoginInfo.DefCsSavdptid, r.Gdsid);
                        if(cw==null){
                            rm.ResultObject=null;
                            return RRInfo("I0454" ,r.Gdsid);

                        }
                        dtl.barcode = cw.barcode;
                        dtl.gdsid = r.Gdsid;
                        dtl.pkgid = "01";
                        dtl.pkgqty = Math.Round(r.Qty,4, MidpointRounding.AwayFromZero);
                        dtl.qty = Math.Round(r.Qty,4, MidpointRounding.AwayFromZero);
                        dtl.gdstype = "95";
                        dtl.bthno = "1";
                        dtl.vlddat = GetCurrentDay();
                        dtl.bcd = r.Bcd;
                        dtl.tpcode = "";
                        dtl.bkr = "";
                        dtl.bokflg = GetN();
                        dtl.bokdat = "";
                        dtl.preqty = null;
                        dtl.brfdtl = r.Rsn.ToString();
                        
                        lstDtl.Add(dtl);
                        //修改推荐仓位标志                        
                        i++;
                    }

                    //done 判断商品是否在同一个部门

                    WmsDc.wms_cang_109.InsertOnSubmit(mst);
                    WmsDc.wms_cangdtl_109.InsertAllOnSubmit(lstDtl);

                    rm.ResultObject = bllno;

                    return rm;
                });
        }       

        /// <summary>
        /// 修改返仓单（单品修改）
        /// </summary>        
        /// <param name="bllno">要修改的单号</param>
        /// <param name="gdsids">货号/条码（数组）</param>
        /// <param name="qtys">数量（数组）</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult MdARetBll(String bllno, String gdsid, String qty, String rsn)
        {
            ////正在生成拣货单，请稍候重试
            //string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0312" );
            //}

            //拆分货号，数量
            JsonResult jr = (JsonResult)_MkParam(gdsid, qty, rsn);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return jr;
            }
            List<ParamRetBll> param = null;
            param = (List<ParamRetBll>)rm.ResultObject;
            
            //查询返仓单
            //查询返仓单主表
            var qrymst = from e in WmsDc.wms_cang_109
                         where e.bllid == WMSConst.BLL_TYPE_RETCANG
                         && e.wmsno == bllno
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0153");
            }
            wms_cang_109 mst = arrqrymst[0];

            //查询返仓单明细
            var qrydtl = from e in WmsDc.wms_cangdtl_109
                         join e1 in qrymst on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                         where e.gdsid==param[0].Gdsid
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0154");
            }
            if (rsn.Trim() == "")
            {
                return RInfo( "I0313" );
            }

            //查询是不是本人操作
            if (mst.mkr.Trim() != LoginInfo.Usrid)
            {
                return RInfo( "I0314" );
            }

            //查询是否有残损库权限
            if (mst.savdptid.Trim() != LoginInfo.DefCsSavdptid.Trim())
            {
                return RInfo( "I0315" );
            }

            //返仓单是否已经审核，审核不允许修改            
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0316" );
            }

            //插入单据明细
            //生成明细单  
            int _i = 0;
            List<wms_cangdtl_109> lstDtl = new List<wms_cangdtl_109>();
            foreach (ParamRetBll r in param)
            {
                wms_cangdtl_109 dtl = new wms_cangdtl_109();
                dtl.wmsno = bllno;
                dtl.bllid = WMSConst.BLL_TYPE_RETCANG;
                dtl.rcdidx = _i + 1;
                dtl.oldbarcode = "";
                wms_cangwei cw = GetBarcodeByGdsid(LoginInfo.DefCsSavdptid, r.Gdsid);
                if (cw == null)
                {
                    return RInfo(r.Gdsid + ",推荐仓位为空", "I0317");
                }
                dtl.barcode = cw.barcode;
                dtl.gdsid = r.Gdsid;
                dtl.pkgid = "01";
                dtl.pkgqty = Math.Round(r.Qty, 4, MidpointRounding.AwayFromZero);
                dtl.qty = Math.Round(r.Qty, 4, MidpointRounding.AwayFromZero);
                dtl.gdstype = "95";
                dtl.bthno = "1";
                dtl.vlddat = GetCurrentDay();
                dtl.bcd = r.Bcd;
                dtl.tpcode = "";
                dtl.bkr = "";
                dtl.bokflg = GetN();
                dtl.bokdat = "";
                dtl.preqty = null;
                dtl.brfdtl = r.Rsn.ToString();

                lstDtl.Add(dtl);                
                _i++;
            }

            wms_cangdtl_109 mddtl = arrqrydtl[0];
            mddtl.pkgqty = lstDtl[0].pkgqty;
            mddtl.qty = lstDtl[0].qty;
            mddtl.brfdtl = rsn;

            /*WmsDc.wms_cangdtl_109.DeleteAllOnSubmit(arrqrydtl);
            if (lstDtl.Count > 0)
            {
                WmsDc.wms_cangdtl_109.InsertAllOnSubmit(lstDtl);
            }
            else
            {
                WmsDc.wms_cang_109.DeleteOnSubmit(mst);
            }*/

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("修改成功", null, "S0145");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0042");
            }

        }
              
        /// <summary>
        /// 修改返仓单（整单修改）
        /// </summary>        
        /// <param name="bllno">要修改的单号</param>
        /// <param name="gdsids">货号/条码（数组）</param>
        /// <param name="qtys">数量（数组）</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓制单, pwrdes = "返仓制单")]
        public ActionResult MdRetBll(String bllno, String gdsids, String qtys, String rsns)
        {            
            //拆分货号，数量
            JsonResult jr = (JsonResult)_MkParam(gdsids, qtys, rsns);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return jr;
            }            
            List<ParamRetBll> param = null;
            param = (List<ParamRetBll>)rm.ResultObject;
            
            //查询返仓单
            //查询返仓单主表
            var qrymst = from e in WmsDc.wms_cang_109
                         where e.bllid == WMSConst.BLL_TYPE_RETCANG
                         && e.wmsno == bllno
                         && qus.Contains(e.qu.Trim())
                         select e;
            var arrqrymst = qrymst.ToArray();
            if (arrqrymst.Length<=0)
            {
                return RNoData("N0155");
            }
            wms_cang_109 mst = arrqrymst[0];
            ////正在生成拣货单，请稍候重试
            //string quRetrv = mst.qu;
            //if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            //{
            //    return RInfo( "I0318" );
            //}

            //查询返仓单明细
            var qrydtl = from e in WmsDc.wms_cangdtl_109
                         join e1 in qrymst on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("N0156");
            }

            //查询是不是本人操作
            if (mst.mkr.Trim() != LoginInfo.Usrid)
            {
                return RInfo( "I0319" );
            }

            //查询是否有残损库权限
            if (mst.savdptid.Trim() != LoginInfo.DefCsSavdptid.Trim())
            {
                return RInfo( "I0320" );
            }

            //返仓单是否已经审核，审核不允许修改            
            if (mst.chkflg == GetY())
            {
                return RInfo( "I0321" );
            }

            //插入单据明细
            //生成明细单  
            int _i = 0;
            List<wms_cangdtl_109> lstDtl = new List<wms_cangdtl_109>();
            foreach (ParamRetBll r in param)
            {
                wms_cangdtl_109 dtl = new wms_cangdtl_109();
                dtl.wmsno = bllno;
                dtl.bllid = WMSConst.BLL_TYPE_RETCANG;
                dtl.rcdidx = _i + 1;
                dtl.oldbarcode = "";
                wms_cangwei cw = GetBarcodeByGdsid(LoginInfo.DefCsSavdptid, r.Gdsid);
                if (cw == null)
                {
                    return RInfo(r.Gdsid + ",推荐仓位为空", "I0322");
                }
                dtl.barcode = cw.barcode;
                dtl.gdsid = r.Gdsid;
                dtl.pkgid = "01";
                dtl.pkgqty = Math.Round(r.Qty,4, MidpointRounding.AwayFromZero);
                dtl.qty = Math.Round(r.Qty, 4, MidpointRounding.AwayFromZero);
                dtl.gdstype = "95";
                dtl.bthno = "1";
                dtl.vlddat = GetCurrentDay();
                dtl.bcd = r.Bcd;
                dtl.tpcode = "";
                dtl.bkr = "";
                dtl.bokflg = GetN();
                dtl.bokdat = "";
                dtl.preqty = null;
                dtl.brfdtl = r.Rsn.ToString();

                lstDtl.Add(dtl);                
                _i++;
            }
            WmsDc.wms_cangdtl_109.DeleteAllOnSubmit(arrqrydtl);
            iDelCangDtl109(arrqrydtl, mst);

            if (lstDtl.Count > 0)
            {
                WmsDc.wms_cangdtl_109.InsertAllOnSubmit(lstDtl);
            }
            else
            {
                WmsDc.wms_cang_109.DeleteOnSubmit(mst);
                iDelCangMst109(mst);
            }

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("修改成功", null, "S0146");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0043");
            }
            
        }

        /// <summary>
        /// 返仓查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="wmsno">返仓单单号/手工单号</param>
        /// <param name="gdsid">商品货号、条码</param>
        /// <param name="barcode">仓位</param>
        /// <param name="mkr">制单人</param>
        /// <param name="rcvdptid">分店编码</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_返仓查询, pwrdes = "返仓查询")]
        public ActionResult FindBll(String begindat, String enddat, String wmsno, String gdsid, String barcode, String mkr, String rcvdptid)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo( "I0323",barcode.Trim()  );
            }

            var arrqrymst = FindBllFromCangMst109(WMSConst.BLL_TYPE_RETCANG, begindat, enddat, wmsno, gdsid, barcode, mkr, rcvdptid);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("N0157");
            }
            return RSucc("成功", arrqrymst, "S0147");
        }
        
    }
}
