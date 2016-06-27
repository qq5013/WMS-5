using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Transactions;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Script.Serialization;
using System.IO;
using WMS.Common;
using WMS.Models;

namespace WMS.Controllers
{
    public class BzBll
    {
        public String rcvdptid { get; set; }
        public String rcvdptdes { get; set; }
        public String bsepkg { get; set; }
        public String busid { get; set; }
        public String bzdat { get; set; }
        public char? bzflg { get; set; }
        public double cnvrto { get; set; }
        public String gdsdes { get; set; }
        public String gdsid { get; set; }
        public String lnkbocino { get; set; }
        public String pkgdes { get; set; }
        public String spc { get; set; }
        public String bzr { get; set; }
        public String bzrdes { get; set; }
        public double qty { get; set; }
        public double? preqty { get; set; }
        public String pkg03 { get; set; }
        public String pkg03pre { get; set; }
    }

    /// <summary>
    /// Session状态相关的数据, 提供登录后需要的基础数据的访问接口
    /// </summary>
    public class SsnController : AuthController
    {
        public class GdsInBarcode
        {
            public string savdptid { get; set; }
            public string qu { get; set; }
            public string barcode { get; set; }
            public string gdsid { get; set; }
            public string gdsdes { get; set; }
            public string spc { get; set; }
            public string bsepkg { get; set; }
            public string gdstype { get; set; }
            public string bcd { get; set; }
            public double sqty{get;set;}
            public string dptid { get; set; }
            public string bnd { get; set; }
            public string bthno { get; set; }
            public string vlddat { get; set; }
        }

        /// <summary>
        /// 登录后区域数据权限
        /// </summary>
        protected String[] qus
        {
            get
            {
                return GetPwrQus();
            }
        }

        /// <summary>
        /// 商品区
        /// </summary>
        protected String[] spqus
        {
            get
            {
                String[] spqu = GetSpQu(LoginInfo.DefSavdptid);
                return spqu.Where(e => qus.Contains(e)).ToArray();
            }
        }
        /// <summary>
        /// 堆头区
        /// </summary>
        protected String[] dtqus
        {
            get
            {
                String[] spqu = GetDtQu(LoginInfo.DefSavdptid);
                return spqu.Where(e => qus.Contains(e)).ToArray();
            }
        }
        /// <summary>
        /// 退货损区
        /// </summary>
        protected String[] thqus
        {
            get
            {                
                String[] spqu = GetThQu(LoginInfo.DefCsSavdptid);
                return spqu.Where(e => qus.Contains(e)).ToArray();
            }
        }

        /// <summary>
        /// 登录后部门商品区的数据权限
        /// </summary>
        protected String[] dpts
        {
            get
            {
                return GetPwrDpts();
            }
        }

        protected string[] dtDpts
        {
            get
            {
                return GetPwrDptsDt();
            }
        }

        protected string[] thDpts
        {
            get
            {
                return GetPwrDptsTh();
            }
        }

        /// <summary>
        /// 得到商品库和残损库的数据权限
        /// </summary>
        protected String[] savdpts
        {
            get
            {
                return GetPwrSavdpts();
            }
        }
        /// <summary>
        /// 得到配送中心的权限
        /// </summary>
        protected String[] stores
        {
            get
            {
                return GetPwrStores();
            }
        }

        protected GdsInBarcode[] GetAGdsQtyInBarcode(String barcode, String gdsid, String gdstype)
        {
            JsonResult jr = (JsonResult)GetAGdsInBarcode(barcode, gdsid, gdstype);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return null;
            }
            GdsInBarcode[] gig = (GdsInBarcode[])rm.ResultObject;
            var qry1 = from e in gig
                       where e.gdsid == gdsid && e.gdstype == gdstype                       
                       select e;
            var arrqry = qry1.ToArray();

            return arrqry;
        }

        /// <summary>
        /// 是否是分货播种
        /// </summary>
        /// <param name="qu"></param>
        /// <returns></returns>
        protected bool IsCutgds(String qu)
        {
            return WmsDc.wms_set.Where(e => e.setid == "017" && e.isvld == 'y' && e.val1 == "1" && e.val2==qu.Trim() && e.val3 == LoginInfo.DefStoreid).Any();
        }

        /// <summary>
        /// 是否是边拣边播
        /// </summary>
        /// <param name="qu"></param>
        /// <returns></returns>
        protected bool IsCutgdsOneByOne(String qu)
        {
            return WmsDc.wms_set.Where(e => e.setid == "017" && e.isvld == 'y' && e.val1 == "2" && e.val2 == qu.Trim() && e.val3 == LoginInfo.DefStoreid).Any();
        }

        protected void CheckStkotAllBz(string wmsno)
        {
            #region 判断是否是该拣货单下的配送单有没有已经播种完了的单据（包括为0的商品），有就修改改配送单下的明细为0的播种标记，和主单播种标记
            //将配送单下修改为0的配送明细，直接修改其播种标记
            var qryStkotdtlZero = from e in WmsDc.stkotdtl
                                  where e.qty <= 0 && e.bzflg == GetN()
                                  && e.stkot.wmsno == wmsno && e.stkot.wmsbllid == "103"
                                  select e;
            var arrQryStkotdtlZero = qryStkotdtlZero.ToArray();
            foreach (var dz in arrQryStkotdtlZero)
            {
                dz.bzflg = GetY();
                dz.bzdat = GetCurrentDate();
                dz.bzr = LoginInfo.Usrid;
            }
            WmsDc.SubmitChanges();
            //判断配送单据下的所有商品是否都已经播种，播种了就直接修改主单播种标记
            var qryStkotdtlZero1 = from e in WmsDc.stkotdtl
                                   where e.stkot.wmsno == wmsno && e.stkot.wmsbllid == "103" && e.stkot.bllid == "206"
                                   group e by new { e.stkouno } into g
                                   select new
                                   {
                                       g.Key.stkouno,
                                       allCnt = g.Count(),
                                       hasBzCnt = g.Count(e => e.bzflg == GetY())
                                   };
            var arrQryStkotdtlZero1 = qryStkotdtlZero1.Where(e => e.allCnt == e.hasBzCnt).Select(e => e.stkouno.Trim()).ToArray();
            var qryHasAllBz = from e in WmsDc.stkot
                              where e.chkflg == GetN()
                              && e.bzflg == GetN()
                              && arrQryStkotdtlZero1.Contains(e.stkouno)
                              select e;
            foreach (var hbz in qryHasAllBz)
            {
                //修改播种标记
                hbz.bzflg = GetY();
                //审核配送单
                hbz.chkflg = GetY();
                hbz.chkdat = GetCurrentDay();
                hbz.ckr = LoginInfo.Usrid;

                //写入dtrlog
                //查看是否dtrlog已经有单据,没有就插入
                var qry = WmsDc.dtrlog
                            .Where(e => e.rcvdptid == hbz.rcvdptid && e.bllno == hbz.stkouno && e.bllid == hbz.bllid)
                            .Select(e => e.bllno);
                var arrqry = qry.ToArray();
                if (arrqry.Length <= 0)
                {
                    dtrlog dl = new dtrlog();
                    dl.bllid = hbz.bllid;
                    dl.bllno = hbz.stkouno;
                    dl.rcvdptid = hbz.rcvdptid;
                    WmsDc.dtrlog.InsertOnSubmit(dl);
                }

                if (!WmsDc.stklst.Where(e => e.stkouno == hbz.stkouno).Any())
                {
                    stklst astklst = new stklst();
                    astklst.stkouno = hbz.stkouno;
                    WmsDc.stklst.InsertOnSubmit(astklst);
                }
            }

            WmsDc.SubmitChanges();
            #endregion 判断是否是该拣货单下的配送单有没有已经播种完了的单据（包括为0的商品），有就修改改配送单下的明细为0的播种标记，和主单播种标记
        }


        /// <summary>
        /// 得到服务器时间
        /// </summary>
        /// <returns></returns>
        public ActionResult GetSvrDateTime()
        {
            DateTime dt = DateTime.Now;
            String s = dt.ToString("yyyyMMdd.HHmmss");
            return RSucc("成功", s, "S0175");
        }

        /// <summary>
        /// 得到当前商品的账存数量
        /// </summary>                
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="gdsid"></param>
        /// <param name="gdstype"></param>
        /// <returns></returns>
        public ActionResult GetGdsQtyInBarcode(String barcode, String gdsid, String gdstype)
        {
            var arrqry = GetAGdsQtyInBarcode(barcode, gdsid, gdstype);
            if (arrqry == null || arrqry.Length < 0)
            {
                return RNoData("N0191");
            }
            return RSucc("成功", arrqry, "S0176");
        }

        protected IEnumerable<GetRealteQuResult> IsExistsPwrByDptidAndQu(String adptid, String aqu)
        {
            //查询商品库            
            return LoginInfo.DatPwrs
                .Where(e => e.dptid == adptid.Trim() && e.qu == aqu.Trim() && e.savstoreid == LoginInfo.DefStoreid.Trim())
                .Select(e => e);
        }

        public GdsInBarcode[] GetAGdsQtyInBarcodeComm(String barcode, String gdsid, String gdstype)
        {
            var qry = from e in WmsDc.wms_cwgdsbs
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      where e.barcode == barcode
                      && e.gdsid == gdsid && e.gdstype == gdstype                      
                      //&& dpts.Contains(e1.dptid.Trim())                                           
                      group new { e, e1, e3 } by new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype, e.bthno, e.vlddat, e1.gdsdes, e1.spc, e1.bsepkg, e3.bcd1, e1.dptid } into g
                      select new
                      {
                          g.Key.savdptid,
                          g.Key.qu,
                          g.Key.barcode,
                          g.Key.gdsid,
                          g.Key.gdsdes,
                          g.Key.spc,
                          g.Key.bsepkg,
                          g.Key.gdstype,
                          g.Key.bthno,
                          g.Key.vlddat,
                          g.Key.dptid,
                          bcd = g.Key.bcd1,
                          sqty = g.Sum(ge => ge.e.qty)
                      };
            //减去开单量
            var qry1 = from e in qry
                       join e1 in WmsDc.wms_sendbill on new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype, e.bthno, e.vlddat } equals new { e1.savdptid, e1.qu, e1.barcode, e1.gdsid, e1.gdstype, e1.bthno, e1.vlddat }
                        into JoinedEmpQry
                       from e2 in JoinedEmpQry.DefaultIfEmpty()
                       //where e.sqty - (e2.qty == null ? 0 : e2.qty) > 0
                       select new GdsInBarcode
                       {
                           savdptid = e.savdptid.Trim(),
                           qu = e.qu.Trim(),
                           barcode = e.barcode.Trim(),
                           gdsid = e.gdsid.Trim(),
                           gdsdes = e.gdsdes.Trim(),
                           spc = e.spc.Trim(),
                           bsepkg = e.bsepkg.Trim(),
                           bcd = e2.bcd.Trim(),
                           gdstype = e.gdstype.Trim(),
                           bthno = e.bthno.Trim(),
                           vlddat = e.vlddat.Trim(),
                           dptid  = e.dptid.Trim(),
                           sqty = Math.Round( (e.sqty - (e2.qty == null ? 0 : e2.qty)), 4, MidpointRounding.AwayFromZero)
                       };
            var arrqry = qry1.ToArray().Where(e => e.sqty >= 0).ToArray();
            arrqry = arrqry.Where(e => IsExistsPwrByDptidAndQu(e.dptid, e.qu).Any()).ToArray();
            return arrqry;
        }

        protected string GetBranchid(String savdptid)
        {
            var qry = from e in WmsDc.branchsavdpt
                      where e.savdptid == savdptid
                      select e;
            foreach (var q in qry)
            {
                return q.branchid;
            }
            return null;
        }

        /// <summary>
        /// 得到仓位商品数量
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        public GdsInBarcode[] GetGdsQtyInBarcodeComm(String barcode)
        {
            var qry = from e in WmsDc.wms_cwgdsbs
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { gdsid = e2.gdsid, bcd = e2.bcd1 }
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      where e.barcode == barcode
                      && e.qu == e.barcode.Substring(0,2)
                      //&& dpts.Contains(e1.dptid.Trim())
                      group new { e, e1, e3 } by new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype, e.bthno, e.vlddat, e1.gdsdes, e1.spc, e1.bsepkg, e3.bcd1 } into g
                      select new
                      {
                          savdptid = g.Key.savdptid.Trim(),
                          qu = g.Key.qu.Trim(),
                          barcode = g.Key.barcode.Trim(),
                          gdsid = g.Key.gdsid.Trim(),
                          gdsdes = g.Key.gdsdes.Trim(),
                          spc = g.Key.spc.Trim(),
                          bsepkg = g.Key.bsepkg.Trim(),
                          gdstype = g.Key.gdstype.Trim(),
                          bcd = g.Key.bcd1.Trim(),
                          vlddat = g.Key.vlddat.Trim(),
                          bthno = g.Key.bthno.Trim(),
                          sqty = g.Sum(ge => ge.e.qty)
                      };
            //减去开单量
            var qry1 = from e in qry
                       join e1 in WmsDc.wms_sendbill on new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype, e.bthno, e.vlddat } equals new { e1.savdptid, e1.qu, e1.barcode, e1.gdsid, e1.gdstype, e1.bthno, e1.vlddat }
                        into JoinedEmpQry
                       from e2 in JoinedEmpQry.DefaultIfEmpty()
                       //where e.sqty - (e2.qty == null ? 0 : e2.qty) > 0
                       select new GdsInBarcode
                       {
                           savdptid = e.savdptid.Trim(),
                           qu = e.qu.Trim(),
                           barcode = e.barcode.Trim(),
                           gdsid = e.gdsid.Trim(),
                           gdsdes = e.gdsdes.Trim(),
                           spc = e.spc.Trim(),
                           bsepkg = e.bsepkg.Trim(),
                           bcd = e2.bcd.Trim(),
                           gdstype = e.gdstype.Trim(),
                           bthno = e.bthno.Trim(),
                           vlddat = e.vlddat.Trim(),
                           sqty = Math.Round((e.sqty - (e2.qty == null ? 0 : e2.qty)), 4, MidpointRounding.AwayFromZero)
                       };
            var arrqry = qry1.ToArray().Where(e => e.sqty >= 0).OrderByDescending(e => e.sqty).ToArray();
            var iNz = arrqry.Where(e => e.sqty > 0).Count();   //不为0的记录
            //如果不为0的记录数==0
            if (iNz < 5)
            {
                arrqry = arrqry.Take(5).ToArray();
            }

            return arrqry;
        }

        /// <summary>
        /// 得到仓位商品数量(托盘还原)
        /// </summary>
        /// <param name="barcode"></param>       
        /// <returns></returns>
        public GdsInBarcode[] GetGdsQtyInBarcodeCommTphy(String barcode)
        {            
            /// 周胖胖 2016/5/12 14:43:00
            /// 帐存表的库存减去开单量不变
            /// 1、然后加上wms_cangdtl  里面 bllid 103 的当天的未确认的库存
            /// 2、再加上wms_cangdtl_115  里面 bllid 115 的当天的未确认的库存
            /// 修改： 2016/5/23日， 如果是托盘还原才去执行1、2
            var qry = from e in WmsDc.wms_cwgdsbs
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { gdsid = e2.gdsid, bcd = e2.bcd1 }
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      where e.barcode == barcode
                      && e.qu == e.barcode.Substring(0, 2)
                      //&& dpts.Contains(e1.dptid.Trim())
                      group new { e, e1, e3 } by new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype, e1.gdsdes, e1.spc, e1.bsepkg, e3.bcd1 } into g
                      select new
                      {
                          savdptid= g.Key.savdptid.Trim(),
                          qu = g.Key.qu.Trim(),
                          barcode = g.Key.barcode.Trim(),
                          gdsid = g.Key.gdsid.Trim(),
                          gdsdes = g.Key.gdsdes.Trim(),
                          spc = g.Key.spc.Trim(),
                          bsepkg = g.Key.bsepkg.Trim(),
                          gdstype = g.Key.gdstype.Trim(),
                          bcd = g.Key.bcd1.Trim(),
                          sqty = g.Sum(ge => ge.e.qty)
                      };
            //减去开单量
            var qry1 = from e in qry
                       join e1 in WmsDc.wms_sendbill on new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype } equals new { e1.savdptid, e1.qu, e1.barcode, e1.gdsid, e1.gdstype }
                        into JoinedEmpQry
                       from e2 in JoinedEmpQry.DefaultIfEmpty()
                       //where e.sqty - (e2.qty == null ? 0 : e2.qty) > 0
                       select new GdsInBarcode
                       {
                           savdptid = e.savdptid.Trim(),
                           qu = e.qu.Trim(),
                           barcode = e.barcode.Trim(),
                           gdsid = e.gdsid.Trim(),
                           gdsdes = e.gdsdes.Trim(),
                           spc = e.spc.Trim(),
                           bsepkg = e.bsepkg.Trim(),
                           bcd = e2.bcd.Trim(),
                           gdstype = e.gdstype.Trim(),
                           sqty = Math.Round((e.sqty - (e2.qty == null ? 0 : e2.qty)), 4, MidpointRounding.AwayFromZero)
                       };
            var arrqry = qry1.ToArray()/*.Where(e => e.sqty >= 0)*/.OrderByDescending(e=>e.sqty).ToArray();
            

            //然后加上wms_cangdtl  里面 bllid 103(捡货单) 的当天的未确认的库存
            var nowCangdtl103 = from e in WmsDc.wms_cangdtl
                             join e1 in WmsDc.wms_cang on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                             where e.bllid == WMSConst.BLL_TYPE_RETRIEVE
                                 /*&& e1.mkedat == GetCurrentDay() */&& e1.chkflg == GetN() && e.bokflg==GetN()
                                 && e.barcode == barcode.Trim()
                             group e by new { e.barcode, e.gdsid, e.gdstype }
                                 into g
                                 select new
                                 {
                                     barcode = g.Key.barcode.Trim(),
                                     gdsid = g.Key.gdsid.Trim(),
                                     gdstype = g.Key.gdstype.Trim(),
                                     qty = g.Sum(e => e.qty)
                                 };
            var cangdtl103 = nowCangdtl103.ToArray();
            //再加上wms_cangdtl_115()  里面 bllid 115(摘果播种捡货单) 的当天的未确认的库存
            var nowCangdtl115 = from e in WmsDc.wms_cangdtl_115
                             join e1 in WmsDc.wms_cang_115 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                             where e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                                 /*&& e1.mkedat == GetCurrentDay() */&& e1.chkflg == GetN() && e.bokflg == GetN()
                                 && e.barcode == barcode.Trim()
                             group e by new { e.barcode, e.gdsid, e.gdstype }
                                 into g
                                 select new
                                 {
                                     barcode = g.Key.barcode.Trim(),
                                     gdsid = g.Key.gdsid.Trim(),
                                     gdstype = g.Key.gdstype.Trim(),
                                     qty = g.Sum(e => e.qty)
                                 };
            var cangdtl115 = nowCangdtl115.ToArray();

            var allArrAry = from e in arrqry
                            join ee1 in cangdtl103 on new { e.barcode, e.gdsid, e.gdstype } equals new { ee1.barcode, ee1.gdsid, ee1.gdstype }
                            into join103
                            from e1 in join103.DefaultIfEmpty()
                            join ee2 in cangdtl115 on new { e.barcode, e.gdsid, e.gdstype } equals new { ee2.barcode, ee2.gdsid, ee2.gdstype }
                            into join115
                            from e2 in join115.DefaultIfEmpty()
                            select new GdsInBarcode
                            {
                                barcode = e.barcode,
                                bcd = e.bcd,
                                bsepkg = e.bsepkg,
                                dptid = e.dptid,
                                gdsdes = e.gdsdes,
                                gdsid = e.gdsid,
                                gdstype = e.gdstype,
                                qu = e.qu,
                                savdptid = e.savdptid,
                                spc = e.spc,
                                sqty = Math.Round((e.sqty + ( e1==null ? 0 : e1.qty) + (e2 == null ? 0 : e2.qty)), 4, MidpointRounding.AwayFromZero)
                            };
            arrqry = allArrAry.Where(e => e.sqty > 0).ToArray();
            /*
            var iNz = arrqry.Where(e => e.sqty > 0).Count();   //不为0的记录
            //如果不为0的记录数==0
            if (iNz < 5)
            {
                arrqry = arrqry.Take(5).ToArray();
            }*/

            return arrqry;
        }

        /// <summary>
        /// 得到仓位的商品
        /// </summary>
        /// <param name="barcode">仓位编码</param>
        /// <returns></returns>        
        public ActionResult GetGdsInBarcode(String barcode)
        {
            var arrqry = GetGdsQtyInBarcodeComm(barcode);
            if (arrqry.Length < 0)
            {
                return RNoData("N0192");
            }
            return RSucc("成功", arrqry, "S0177");
        }
        
        /// <summary>
        /// 得到仓位的商品(托盘还原)
        /// </summary>
        /// <param name="barcode">仓位编码</param>
        /// <returns></returns>        
        public ActionResult GetGdsQtyInBarcodeTphy(String barcode)
        {
            var arrqry = GetGdsQtyInBarcodeCommTphy(barcode);
            if (arrqry.Length < 0)
            {
                return RNoData("N0193");
            }
            return RSucc("成功", arrqry, "S0178");
        }

        /// <summary>
        /// 得到仓位的商品
        /// </summary>
        /// <param name="barcode">仓位编码</param>
        /// <returns></returns>        
        public ActionResult GetAGdsInBarcode(String barcode, String gdsid, String gdstype)
        {
            var arrqry = GetAGdsQtyInBarcodeComm(barcode, gdsid, gdstype);
            if (arrqry.Length < 0)
            {
                return RNoData("N0194");
            }
            return RSucc("成功", arrqry, "S0179");
        }

        private void Init(RequestContext requestContext)
        {
            //3.初始化登录信息
            if (requestContext.HttpContext.Session["usrid"] != null)
            {
                UsrId = (String)Session["usrid"];
                LoginInfo = GetLoginInfoByUsrId(UsrId);
                if (requestContext.HttpContext.Session["defstoreid"] != null)
                {
                    SetDefSavdptid((String)requestContext.HttpContext.Session["defstoreid"]);
                }                

                //初始化日志对象
                Log = new Log();
                Log.man = UsrId;
                Log.mdlid = this.Mdlid;
                Log.WmsDc = WmsDc;

                
                                   
            }
        }

        protected virtual void SetModuleInfo()
        {
        }

        // GET: /Retrieve/
        /// <summary>
        /// 是否是同一个登录员工
        /// </summary>
        /// <param name="empid">员工工号</param>
        /// <returns></returns>
        protected bool IsSameLogin(String empid)
        {
            return empid.Trim() == LoginInfo.Usrid.Trim();
        }

        protected bool HasPwr(RequestContext requestContext)
        {            
            RouteData rd = requestContext.RouteData;
            String controller = (String)rd.Values["controller"];
            String action = (String)rd.Values["action"];            
            Type t = Type.GetType("WMS.Controllers." + controller + "Controller",false, true);
            if (t == null)
            {
                return true;
            }
            //得到自定义属性
            MethodInfo mi = t.GetMethod(action, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (mi == null)
            {
                return true;
            }
            PWRAttribute[] pwra = (PWRAttribute[])mi.GetCustomAttributes(typeof(PWRAttribute), true);
            if (pwra.Length==0)
            {
                return true;
            }
            var qry = from e in LoginInfo.EmpPwrs
                        where e.mdlid.Trim() == "wms_back" && e.pwrid.Trim() == pwra[0].Pwrid
                        select e;
            if (qry.Count() > 0)
            {
                return true;
            }            

            return false;
        }

        /// <summary>
        /// 得到有权限的分区
        /// </summary>
        /// <returns></returns>
        protected String[] GetPwrQus()
        {
            String[] qus = LoginInfo.DatPwrs.GroupBy(e => e.qu).Select(g => g.Key).ToArray();
            return qus;
        }

        /// <summary>
        /// 得到配送中心的权限
        /// </summary>
        protected String[] GetPwrStores()
        {
            String[] stores = LoginInfo.SavStoreids.Select(e => e.Storeid).ToArray();
            return stores;
        }

        //得到堆头区
        protected string[] GetDtQu( String savdptid )
        {
            var qry = from e in WmsDc.wms_set
                      where e.setid == "006"
                      && e.isvld == GetY()
                      && e.val2 == "5"
                      && e.val3 == savdptid
                      select e.val1;
            var arrqry = qry.ToArray();
            return arrqry;
        }

        //得到退货区
        protected string[] GetThQu(String savdptid)
        {
            var qry = from e in WmsDc.wms_set
                      where e.setid == "006"
                      && e.isvld == GetY()
                      && e.val2 == "3"
                      && e.val3 == savdptid
                      select e.val1;
            var arrqry = qry.ToArray();
            return arrqry;
        }

        //得到商品区
        protected string[] GetSpQu(String savdptid)
        {
            var qry = from e in WmsDc.wms_set
                      where e.setid == "006"
                      && e.isvld == GetY()
                      && e.val2 == "7"
                      && e.val3 == savdptid
                      select e.val1;
            var arrqry = qry.ToArray();
            return arrqry;
        }

        /// <summary>
        /// 得到有商品区的权限部门
        /// </summary>
        /// <returns></returns>
        protected String[] GetPwrDpts()
        {
            //得到商品区
            String[] spqu = GetSpQu(LoginInfo.DefSavdptid);
            String[] dpts = LoginInfo.DatPwrs
                .Where(e => spqu.Contains(e.qu))
                .GroupBy(e => e.dptid).Select(g => g.Key.Trim()).ToArray();
            //查看启用的部门
            var qry = from e in WmsDc.wms_set
                      where e.setid == WMSConst.SET_TYPE_ENABLEDPT
                      && (e.val3 == LoginInfo.DefCsSavdptid || e.val3 == LoginInfo.DefSavdptid)
                      && e.isvld == GetY()
                      select e.val1;
            var arrqry = qry.ToArray();
            if (arrqry.Length > 0)
            {
                dpts = dpts.Where(e => arrqry.Contains(e.Trim())).ToArray();
            }            
            return dpts;
        }

        /// <summary>
        /// 分区是否在生成拣货单
        /// </summary>
        /// <param name="storeid"></param>
        /// <param name="qu"></param>
        /// <returns></returns>
        protected Boolean DoingRetrieve(String storeid, String qu)
        {
            var qry = from e in WmsDc.wms_set
                      where e.isvld == 'y' && e.val3 == storeid.Trim() && e.val2 == qu.Trim()
                      && e.setid == "016"
                      select e;
            wms_set st = qry.FirstOrDefault();
            if (st != null)
            {
                return st.val1 == "y";
            }
            return false;
        }

        /// <summary>
        /// 得到有商品区的权限部门(堆头)
        /// </summary>
        /// <returns></returns>
        protected String[] GetPwrDptsDt()
        {
            //得到商品区
            String[] spqu = GetDtQu(LoginInfo.DefSavdptid);
            String[] dpts = LoginInfo.DatPwrs
                .Where(e => spqu.Contains(e.qu))
                .GroupBy(e => e.dptid).Select(g => g.Key).ToArray();
            //查看启用的部门
            var qry = from e in WmsDc.wms_set
                      where e.setid == WMSConst.SET_TYPE_ENABLEDPT
                      && (e.val3 == LoginInfo.DefCsSavdptid || e.val3 == LoginInfo.DefSavdptid)
                      && e.isvld == GetY()
                      select e.val1;
            var arrqry = qry.ToArray();
            if (arrqry.Length > 0)
            {
                dpts = dpts.Where(e => arrqry.Contains(e.Trim())).ToArray();
            }
            return dpts;
        }

        /// <summary>
        /// 得到有商品区的权限部门(退货)
        /// </summary>
        /// <returns></returns>
        protected String[] GetPwrDptsTh()
        {
            //得到商品区
            String[] spqu = GetThQu(LoginInfo.DefCsSavdptid);
            String[] dpts = LoginInfo.DatPwrs
                .Where(e => spqu.Contains(e.qu))
                .GroupBy(e => e.dptid).Select(g => g.Key).ToArray();
            //查看启用的部门
            var qry = from e in WmsDc.wms_set
                      where e.setid == WMSConst.SET_TYPE_ENABLEDPT
                      && (e.val3 == LoginInfo.DefCsSavdptid || e.val3 == LoginInfo.DefSavdptid)
                      && e.isvld == GetY()
                      select e.val1;
            var arrqry = qry.ToArray();
            if (arrqry.Length > 0)
            {
                dpts = dpts.Where(e => arrqry.Contains(e.Trim())).ToArray();
            }
            return dpts;
        }

        /// <summary>
        /// 得到商品库和残损库的权限
        /// </summary>
        /// <returns></returns>
        protected String[] GetPwrSavdpts()
        {
            String[] arrSavdptid = LoginInfo.SavDptids
                .GroupBy(e => new { e.savdptid, e.savdptdes })
                .Select(e => e.Key.savdptid)
                .ToArray();
            return arrSavdptid;
        }

        /// <summary>
        /// 得到分区数据权限信息
        /// </summary>
        /// <param name="pwrFlg">权限控制标志（GetY()表示控制权限,null/GetN()表示不控制权限）</param>
        /// <returns></returns>
        public ActionResult GetQusInfo(String pwrFlg)
        {
            pwrFlg = string.IsNullOrEmpty(pwrFlg) ? "y" : "n";
            var qry = from e in WmsDc.wms_set
                      where e.setid == WMSConst.SET_TYPE_QUSET
                      && (e.val3 == LoginInfo.DefSavdptid || e.val3 == LoginInfo.DefCsSavdptid)
                      && e.isvld == GetY()
                      orderby e.val2 descending
                      select new
                      {
                          qu = e.val1,
                          qudes = e.typedes,
                      };
            if (pwrFlg == "y")
            {
                qry = qry.Where(e => qus.Contains(e.qu.Trim()));
            }
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0195");
            }
            return RSucc("成功", arrqry, "S0180");
        }

        /// <summary>
        /// 根据供应商和货号查询(残损库的商品)
        /// </summary>
        /// <param name="gdsid">货号/条码/商品名称 查询参数</param>
        /// <param name="prvid">供应商编号</param>
        /// <returns></returns>
        public ActionResult GetGdsByGdsidPrvid(String gdsid, String prvid)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            var qry = from e in WmsDc.gds
                      join e1 in WmsDc.bcd on e.gdsid equals e1.gdsid
                      join e2 in WmsDc.mctdtl on e.gdsid equals e2.gdsid
                      join e3 in WmsDc.mctct on e2.ctno equals e3.ctno
                      where (e.gdsid == gdsid
                      || e1.bcd1 == gdsid) && e3.prvid == prvid
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0196");
            }


            //查询残损库是否有该商品信息，有的话就返回
            var qrysp = from e in WmsDc.wms_cwgdsbs
                        join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                        join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }
                        into joinBcd
                        from e3 in joinBcd.DefaultIfEmpty()
                        where e.gdsid == gdsid && (e.savdptid == LoginInfo.DefCsSavdptid)
                        group new { e, e1, e3 } by new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype, e1.gdsdes, e1.spc, e1.bsepkg, e3.bcd1, e.vlddat, e.bthno } into g
                        select new
                        {
                            g.Key.savdptid,
                            g.Key.qu,
                            g.Key.barcode,
                            g.Key.gdsid,
                            g.Key.gdsdes,
                            g.Key.spc,
                            g.Key.bsepkg,
                            g.Key.gdstype,
                            bcd = g.Key.bcd1,
                            g.Key.vlddat,
                            g.Key.bthno,
                            sqty = Math.Round(g.Sum(ge => ge.e.qty), 4, MidpointRounding.AwayFromZero)
                        };
            var qrysp1 = from e in qrysp
                         join e1 in WmsDc.wms_sendbill on new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype, e.bthno, e.vlddat } equals new { e1.savdptid, e1.qu, e1.barcode, e1.gdsid, e1.gdstype, e1.bthno, e1.vlddat }
                          into JoinedEmpQry
                         from e2 in JoinedEmpQry.DefaultIfEmpty()
                         where e.sqty - (e2.qty == null ? 0 : e2.qty) > 0
                         select new GdsInBarcode
                         {
                             savdptid = e.savdptid.Trim(),
                             qu = e.qu.Trim(),
                             barcode = e.barcode.Trim(),
                             gdsid = e.gdsid.Trim(),
                             gdsdes = e.gdsdes.Trim(),
                             spc = e.spc.Trim(),
                             bsepkg = e.bsepkg.Trim(),
                             bcd = e2.bcd.Trim(),
                             gdstype = e.gdstype.Trim(),
                             bthno = e.bthno.Trim(),
                             vlddat = e.vlddat.Trim(),
                             sqty = Math.Round((e.sqty - (e2.qty == null ? 0 : e2.qty)), 4, MidpointRounding.AwayFromZero)
                         };
            var arrqrysp1 = qrysp1.ToArray();
            if (arrqrysp1.Length <= 0)
            {
                return RNoData("N0197");
            }
            
            return RSucc("成功", arrqrysp1.ToArray(), "S0181");
        }

        /// <summary>
        /// 查询商品对应每个仓位的信息
        /// </summary>
        /// <param name="gdsid">商品货号</param>
        /// <param name="gdstype">商品类型</param>
        /// <returns></returns>
        public ActionResult GetGdsStoreQtyByGdsid(String gdsid, String gdstype)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            var qry = from e in WmsDc.gds
                      join e2 in WmsDc.wms_cwgdsbs on e.gdsid equals e2.gdsid
                      where
                      (e2.savdptid == LoginInfo.DefCsSavdptid || e2.savdptid == LoginInfo.DefSavdptid)
                      && qus.Contains(e2.qu)
                      select new
                      {
                          gdsid = e.gdsid.Trim(),
                          gdsdes = e.gdsdes.Trim(),
                          spc = e.spc.Trim(),
                          bsepkg = e.bsepkg.Trim(),
                          dptid = e.dptid.Trim(),
                          bnd = e.bnd.Trim(),
                          e2.bcd,
                          qu = e2.qu,
                          savdptid = e2.savdptid,
                          barcode = e2.barcode,
                          gdstype = e2.gdstype.Trim(),
                          e2.qty
                      };
            var q = from e in qry                    
                      where e.gdsid == gdsid //&& e.gdstype==gdstype
                    group e by new { e.gdsid, e.gdsdes, e.gdstype, e.barcode, e.spc, e.bsepkg, e.dptid, e.bnd, e.bcd, e.qu, e.savdptid} into g
                    select new
                    {
                        g.Key.gdsid,
                        g.Key.gdsdes,
                        g.Key.gdstype,
                        g.Key.spc,
                        g.Key.bsepkg,
                        g.Key.dptid,
                        g.Key.bnd,
                        g.Key.qu,
                        g.Key.savdptid,
                        g.Key.barcode,
                        qty = g.Sum(e => e.qty)
                    };
            //减去开单量
            var qry1 = from e in  q
                       join e1 in WmsDc.wms_sendbill on new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype } equals new { e1.savdptid, e1.qu, e1.barcode, e1.gdsid, e1.gdstype }
                        into JoinedEmpQry
                       from e2 in JoinedEmpQry.DefaultIfEmpty()
                       //where e.sqty - (e2.qty == null ? 0 : e2.qty) > 0
                       select new GdsInBarcode
                       {
                           savdptid = e.savdptid.Trim(),
                           qu = e.qu.Trim(),
                           barcode = e.barcode.Trim(),
                           gdsid = e.gdsid.Trim(),
                           gdsdes = e.gdsdes.Trim(),
                           spc = e.spc.Trim(),
                           bnd = e.bnd.Trim(),
                           bsepkg = e.bsepkg.Trim(),
                           bcd = e2.bcd.Trim(),
                           gdstype = e.gdstype.Trim(),
                           dptid = e.dptid.Trim(),
                           sqty = Math.Round((e.qty - (e2.qty == null ? 0 : e2.qty)), 4, MidpointRounding.AwayFromZero)
                       };

            var q1 = from e in qry1
                     join e3 in
                         WmsDc.v_wms_pkg on new { e.gdsid } equals new { e3.gdsid }
                                          into joinPkg
                     from e4 in joinPkg.DefaultIfEmpty()
                     select new
                     {
                         e.bnd,
                         e.bsepkg,
                         e.dptid,
                         e.gdsdes,
                         e.gdsid,
                         e.gdstype,
                         qty = e.sqty,
                         e.qu,
                         e.savdptid,
                         e.spc,
                         e.barcode,
                         pkg03 = GetPkgStr(e.sqty, e4.cnvrto, e4.pkgdes),
                         pkg03pre = GetPkgStr(e.sqty, e4.cnvrto, e4.pkgdes)
                     };
            q1 = q1.Where(e => e.qty != 0);

            var arrqry = q1.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0198");
            }
            return RSucc("成功！", arrqry, "S0182");
        }

        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();

            SetModuleInfo();

            base.Initialize(requestContext);            
            Init(requestContext);

            //判断是否是重复提交
            String _rnd = requestContext.HttpContext.Request["rndre"];
            if (requestContext.HttpContext.Session["rndre"] != null
                && _rnd == (String)requestContext.HttpContext.Session["rndre"])
            {
                Rm.ResultCode = "-3";
                Rm.ResultDesc = "请不要重复提交";
                Rm.ExtObject = null;
                Rm.ResultObject = null;

                Response.ContentType = "application/json";
                Response.Write(jss.Serialize(Rm));
                Response.End();
                ReRequestException nlex = new ReRequestException();
                throw new ReRequestException();
            }
            else
            {
                requestContext.HttpContext.Session["rndre"] = _rnd;
            }

            //1.检查是否登录
            if (!CheckLogin())
            {
                Rm.ResultCode = "-1";
                Rm.ResultDesc = "尚未登录";
                Rm.ExtObject = null;
                Rm.ResultObject = null;                

                Response.ContentType = "application/json";
                Response.Write(jss.Serialize(Rm));
                Response.End();
                NotLoginException nlex = new NotLoginException();                
                throw new NotLoginException();                
            }

            //2.检查权限            
            if (!HasPwr(requestContext))
            {
                Rm.ResultCode = "-2";
                Rm.ResultDesc = "没有权限";
                Rm.ExtObject = null;
                Rm.ResultObject = null;                

                Response.ContentType = "application/json";
                Response.Write(jss.Serialize(Rm));
                Response.End();

                throw new HasNonPwrException();
            }
            
        }

        protected bool IsInSavdptid(string dptid)
        {
            var qry = from e in WmsDc.shopstore
                      where (e.savdptid == LoginInfo.DefCsSavdptid || e.savdptid == LoginInfo.DefSavdptid)
                      && e.rcvdptid == dptid
                      select e;
            foreach (shopstore s in qry)
            {
                return true;
            }
            return false;
        }

        protected Object[] FindBllFromBllMst(String bllid, String begindat, String enddat, String wmsno, String gdsid, String barcode)
        {
            String fscprdid = GetCurrentFscprdid();
            var qry = from e in WmsDc.wms_blldtl
                      join e1 in WmsDc.wms_bllmst on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      where e.bllid == bllid
                      && qus.Contains(e1.qu.Trim())
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e3.gdsid,
                          e3.bcd1,
                          e.barcode
                      };
            //如果没有时间查询条件就查询当前会计期间的单据
            if (begindat == null && enddat == null)
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(wmsno))
            {
                qry = qry.Where(e => e.wmsno == wmsno);
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => (e.bcd1 == gdsid || e.gdsid == gdsid));
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_bllmst
                         join e1 in WmsDc.emp on e.mkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()
                         where arrqry.Contains(e.wmsno)
                         && e.bllid == bllid
                         select new
                         {                             
                             e.arvdat,
                             e.bllid,
                             e.brief,
                             e.chkdat,
                             e.chkflg,
                             e.ckr,
                             e.hndbllno,
                             e.huojia,
                             e.lnknewbllid,
                             e.lnknewbrief,
                             e.lnknewno,                             
                             e.mkedat,
                             e.mkr,
                             e.odrdat,
                             e.opr,
                             e.prvid,
                             e.qu,
                             e.savdptid,
                             e.tongdao,
                             e.wmsno,
                             e3.prvdes,
                             bokall = (from et in WmsDc.wms_blldtl
                                       where et.bllid == e.bllid
                                       && et.wmsno == e.wmsno
                                       && (et.bokflg == null || et.bokflg == GetN())
                                       select et).Count(),
                             mkrdes = e1.empdes
                         };
            var arrqrymst = qrymst.ToArray();

            return arrqrymst;
        }

        

        /// <summary>
        /// 配送播种
        /// </summary>
        /// <param name="dat">配送日期</param>
        /// <returns></returns>
        protected IQueryable<BzBll> Psbz(String dat)
        {
            var qry = from e in WmsDc.stkot
                      join e1 in WmsDc.stkotdtl on e.stkouno equals e1.stkouno
                      join e2 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e2.wmsno, wmsbllid = e2.bllid }
                      join e3 in
                          (from m in
                               WmsDc.v_wms_pkg
                           group m by new { m.gdsid, m.cnvrto,  m.pkgdes } into g
                           select g.Key) on new { e1.gdsid } equals new { e3.gdsid }
                      into joinPkg
                      from e4 in joinPkg.DefaultIfEmpty()
                      join e5 in WmsDc.gds on e1.gdsid equals e5.gdsid
                      join e6 in WmsDc.wms_boci on new { dh = e2.lnkbocino, sndtmd = e2.lnkbocidat, e2.qu } equals new { e6.dh, e6.sndtmd, e6.qu }
                      join e7 in WmsDc.view_pssndgds on new { e6.dh, e6.clsid, e6.sndtmd, e.rcvdptid, e6.qu } equals new { e7.dh, e7.clsid, e7.sndtmd, e7.rcvdptid, e7.qu }
                      join e8 in WmsDc.emp on e1.bzr equals e8.empid
                      into joinEmp
                      from e9 in joinEmp.DefaultIfEmpty()
                      join e10 in WmsDc.dpt on e.rcvdptid equals e10.dptid                      
                      where e.bllid == WMSConst.BLL_TYPE_DISPATCH
                      && savdpts.Contains(e.savdptid)
                      && e6.sndtmd == dat
                      group new
                      {
                          e.rcvdptid,
                          e10.dptdes,
                          e7.busid,
                          e4.cnvrto,
                          e4.pkgdes,
                          e1.gdsid,
                          e5.gdsdes,
                          e5.spc,
                          e5.bsepkg,
                          e6.clsid,
                          e1.bzflg,
                          e1.bzr,
                          e1.bzdat,
                          e9.empdes,
                          e2.lnkbocino,
                          e1.qty,
                          e1.preqty
                      } by new
                      {
                          e.rcvdptid,
                          e10.dptdes,
                          e7.busid,
                          e4.cnvrto,
                          e4.pkgdes,
                          e1.gdsid,
                          e5.gdsdes,
                          e5.spc,
                          e5.bsepkg,
                          e1.bzflg,
                          e1.bzr,
                          e1.bzdat,
                          e9.empdes,
                          e2.lnkbocino,
                          e6.clsid
                      } into g
                      orderby g.Key.busid.Trim().Substring(g.Key.busid.Trim().Length - 1, 1), g.Key.busid.Trim().Substring(0, 3),
                      g.Key.rcvdptid
                      select new BzBll
                      {
                          rcvdptid = g.Key.rcvdptid,
                          rcvdptdes = g.Key.dptdes,
                          bsepkg = g.Key.bsepkg,
                          busid = g.Key.busid,
                          bzflg = g.Key.bzflg,
                          cnvrto = g.Key.cnvrto,
                          gdsdes = g.Key.gdsdes,
                          gdsid = g.Key.gdsid,
                          lnkbocino = g.Key.clsid,
                          pkgdes = g.Key.pkgdes,
                          spc = g.Key.spc,
                          qty = g.Sum(e => e.qty),
                          bzr = g.Key.bzr,
                          bzrdes = g.Key.empdes,
                          bzdat = g.Key.bzdat.Trim(),
                          preqty = g.Sum(e => e.preqty == null ? 0 : e.preqty),
                          pkg03 = GetPkgStr(g.Sum(e=>e.qty), g.Key.cnvrto, g.Key.pkgdes),
                          pkg03pre = GetPkgStr(g.Sum(e => e.preqty == null ? 0 : e.preqty), g.Key.cnvrto, g.Key.pkgdes)
                      };
            return qry;
        }

        /// <summary>
        /// 内调播种
        /// </summary>
        /// <param name="dat">配送日期</param>
        /// <returns></returns>
        protected IQueryable<BzBll> Ndbz(String dat)
        {
            var qry = from e in WmsDc.stkin
                      join e1 in WmsDc.stkindtl on e.stkinno equals e1.stkinno
                      join e2 in WmsDc.wms_cang on new { wmsno = e.outwmsno, bllid = e.outwmsbllid } equals new { e2.wmsno, e2.bllid }
                      join e3 in
                          (from m in
                               WmsDc.v_wms_pkg
                           group m by new { m.gdsid, m.cnvrto, m.pkgdes } into g
                           select g.Key) on new { e1.gdsid } equals new { e3.gdsid }
                      into joinPkg
                      from e4 in joinPkg.DefaultIfEmpty()
                      join e5 in WmsDc.gds on e1.gdsid equals e5.gdsid
                      join e6 in WmsDc.dpt on e.savdptid equals e6.dptid
                      join e8 in WmsDc.emp on e1.bzr equals e8.empid
                      into joinEmp
                      from e9 in joinEmp.DefaultIfEmpty()
                      where e.bllid == WMSConst.BLL_TYPE_INNERADJ
                      && e.mkedat == dat
                      group new
                      {
                          e.savdptid,
                          e6.dptdes,
                          e4.cnvrto,
                          e4.pkgdes,
                          e1.gdsid,
                          e5.gdsdes,
                          e5.spc,
                          e5.bsepkg,
                          e1.bzflg,
                          e1.bzr,
                          e9.empdes,
                          e2.lnkbocino,
                          e1.qty                          
                      } by new
                      {
                          e.savdptid,
                          e6.dptdes,
                          e4.cnvrto,
                          e4.pkgdes,
                          e1.gdsid,
                          e5.gdsdes,
                          e5.spc,
                          e5.bsepkg,
                          e1.bzflg,
                          e1.bzr,
                          e1.bzdat,
                          e9.empdes,
                          e2.lnkbocino
                      } into g
                      select new BzBll
                      {
                          rcvdptid = g.Key.savdptid,
                           rcvdptdes = g.Key.dptdes,
                          bsepkg = g.Key.bsepkg,
                          busid = "",
                          bzflg = g.Key.bzflg,
                          cnvrto = g.Key.cnvrto,
                          gdsdes = g.Key.gdsdes,
                          gdsid = g.Key.gdsid,
                          lnkbocino = g.Key.lnkbocino,
                          pkgdes = g.Key.pkgdes,
                          spc = g.Key.spc,
                          qty = g.Sum(e => e.qty),
                          bzr = g.Key.bzr,
                          bzrdes = g.Key.empdes,
                          bzdat = g.Key.bzdat.Trim(),
                          preqty = g.Sum(e => e.qty == null ? 0 : e.qty),
                          pkg03 = GetPkgStr(g.Sum(e => e.qty), g.Key.cnvrto, g.Key.pkgdes),
                          pkg03pre = GetPkgStr((double)g.Sum(e => e.qty == null ? 0 : e.qty), g.Key.cnvrto, g.Key.pkgdes)
                      };
            return qry;
        }

        /// <summary>
        /// 外销播种
        /// </summary>
        /// <param name="dat">配送日期</param>
        /// <returns></returns>
        protected IQueryable<BzBll> Wxbz(String dat)
        {
            var qry = from e in WmsDc.sivc
                      join e1 in WmsDc.sivcdtl on e.sivcno equals e1.sivcno
                      join e2 in WmsDc.wms_cang on new { e.wmsno, e.wmsbllid } equals new { e2.wmsno, wmsbllid = e2.bllid }
                      join e3 in
                          (from m in
                               WmsDc.v_wms_pkg
                           group m by new { m.gdsid, m.cnvrto,  m.pkgdes } into g
                           select g.Key) on new { e1.gdsid } equals new { e3.gdsid }
                      into joinPkg
                      from e4 in joinPkg.DefaultIfEmpty()
                      join e5 in WmsDc.gds on e1.gdsid equals e5.gdsid
                      join e6 in WmsDc.cus on e.cusid equals e6.cusid
                      join e8 in WmsDc.emp on e1.bzr equals e8.empid
                      into joinEmp
                      from e9 in joinEmp.DefaultIfEmpty()
                      where e.bllid == WMSConst.BLL_TYPE_WXDISPATCH
                      && e.mkedat == dat
                      group new
                      {
                          e.cusid,
                          e6.cusdes,
                          e4.cnvrto,
                          e4.pkgdes,
                          e1.gdsid,
                          e5.gdsdes,
                          e5.spc,
                          e5.bsepkg,
                          e1.bzflg,
                          e1.bzr,
                          e9.empdes,
                          e2.lnkbocino,
                          e1.qty,
                          e1.preqty
                      } by new
                      {
                          e.cusid,
                          e6.cusdes,
                          e4.cnvrto,
                          e4.pkgdes,
                          e1.gdsid,
                          e5.gdsdes,
                          e5.spc,
                          e5.bsepkg,
                          e1.bzflg,
                          e1.bzr,
                          e1.bzdat,
                          e9.empdes,
                          e2.lnkbocino
                      } into g
                      select new BzBll
                      {
                          rcvdptid = g.Key.cusid,
                          rcvdptdes = g.Key.cusdes,
                          bsepkg = g.Key.bsepkg,
                          busid = "",
                          bzflg = g.Key.bzflg,
                          bzr = g.Key.bzr,
                          bzrdes = g.Key.empdes,
                          bzdat = g.Key.bzdat,
                          cnvrto = g.Key.cnvrto,
                          gdsdes = g.Key.gdsdes,
                          gdsid = g.Key.gdsid,
                          lnkbocino = g.Key.lnkbocino,
                          pkgdes = g.Key.pkgdes,
                          spc = g.Key.spc,
                          qty = g.Sum(e => e.qty),
                          preqty = g.Sum(e => e.preqty == null ? 0 : e.preqty),
                          pkg03 = GetPkgStr(g.Sum(e => e.qty), g.Key.cnvrto, g.Key.pkgdes),
                          pkg03pre = GetPkgStr(g.Sum(e => e.preqty == null ? 0 : e.preqty), g.Key.cnvrto, g.Key.pkgdes)
                      };
            return qry;
        }

        //播种查询
        protected Object[] FindBllFromCangMst107(String bllid, String dat, String boci, String gdsid, String rcvdptid, String busid)
        {
            String fscprdid = GetCurrentFscprdid();
            if (string.IsNullOrEmpty(dat))
            {
                return null;
            }
            var qry = (bllid.Trim() == WMSConst.BLL_TYPE_INNERADJ) ? Ndbz(dat) :
                    (bllid.Trim() == WMSConst.BLL_TYPE_DISPATCH) ? Psbz(dat) :
                    (bllid.Trim() == WMSConst.BLL_TYPE_WXDISPATCH) ? Wxbz(dat) : null;

            if (!string.IsNullOrEmpty(boci))
            {
                qry = qry.Where(e => e.lnkbocino == boci);
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => e.gdsid == gdsid);
            }
            if (!string.IsNullOrEmpty(rcvdptid))
            {
                qry = qry.Where(e => e.rcvdptid == rcvdptid);
            }
            if (!string.IsNullOrEmpty(busid))
            {
                qry = qry.Where(e => e.busid.ToUpper().Contains(busid.Trim().ToUpper()));
            }
            var arrqrymst = qry.ToArray();

            return arrqrymst;
        }

        protected Object[] FindBllFromCangMst(String bllid, String begindat, String enddat, String wmsno, String gdsid, String barcode)
        {
            String fscprdid = GetCurrentFscprdid();

            var qry = from e in WmsDc.wms_cangdtl
                      join e1 in WmsDc.wms_cang on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd=e2.bcd1 }
                      into joinBcd from e3 in joinBcd.DefaultIfEmpty()
                      where e.bllid == bllid
                      && qus.Contains(e1.qu.Trim())
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e3.gdsid,
                          e3.bcd1,
                          e.barcode
                      };
            //如果没有时间查询条件就查询当前会计期间的单据
            if (begindat == null && enddat == null)
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(wmsno))
            {
                qry = qry.Where(e => e.wmsno == wmsno);
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => (e.bcd1 == gdsid || e.gdsid == gdsid));
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_cang
                         join e1 in WmsDc.emp on e.mkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()                         
                         where qus.Contains(e.qu.Trim()) 
                         && arrqry.Contains(e.wmsno)                 
                         && e.bllid == bllid                         
                         select new
                         {                                                          
                             e.bllid,
                             e.brief,
                             e.chkdat,
                             e.chkflg,
                             e.ckr,                          
                             e.mkedat,
                             e.mkr,                             
                             e.opr,
                             e.prvid,
                             e.qu,
                             e.savdptid, 
                             e3.prvdes,
                             e.wmsno,
                             e.lnkno,
                             e.lnkbllid,
                             e.lnkbocino,
                             bokall = (from et in WmsDc.wms_cangdtl
                                      where et.bllid==e.bllid
                                      && et.wmsno==e.wmsno
                                      && (et.bokflg==null||et.bokflg==GetN())
                                      select et).Count(),
                             mkrdes = e1.empdes
                         };
            var arrqrymst = qrymst.ToArray();

            return arrqrymst;
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="oldPwd">原密码</param>
        /// <param name="pwd">新密码</param>
        /// <returns></returns>
        public ActionResult MdPwd(String oldPwd, String pwd)
        {
            oldPwd = DisEncode(oldPwd);
            emp ep = (from e in WmsDc.emp
                      where //e.pwd == oldPwd && 
                      e.empid == LoginInfo.Usrid
                      select e).FirstOrDefault();
            if (ep == null)
            {
                return RNoData("N0199");
            }
            if (ep.pwd != oldPwd)
            {
                return RInfo( "I0394" );
            }
            ep.pwd = DisEncode(pwd);
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("密码修改成功", null, "S0183");
            }
            catch (Exception ex)
            {
                return RErr(ex.Message, "E0052");
            }
        }

        protected Object[] FindBllFromCangMst115(String begindat, String enddat, String barcode, String bllid, String bkr, String gdsid)
        {
            String fscprdid = GetCurrentFscprdid();

            var qry = from e in WmsDc.wms_cangdtl_115
                      join e1 in WmsDc.wms_cang_115 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      join e4 in WmsDc.emp on e.bkr equals e4.empid
                      where e1.lnkbllid == bllid
                      && e1.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                      && qus.Contains(e1.qu.Trim())                      
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e3.gdsid,
                          e.gdstype,
                          e3.bcd1,
                          e.barcode,
                          e.bkr,
                          e.bokflg,
                          e.bokdat,
                          bkrdes = e4.empdes
                      };
            //如果没有时间查询条件就查询当前会计期间的单据
            if (string.IsNullOrEmpty(begindat) && string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(bkr))
            {
                qry = qry.Where(e => (e.bkr == bkr) || e.bkrdes.Contains(bkr));
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => e.gdsid == gdsid.Trim());
            }

            var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_cang_115
                         join e4 in WmsDc.wms_cangdtl_115 on new { e.wmsno, e.bllid } equals new { e4.wmsno, e4.bllid }
                         join e1 in WmsDc.emp on e4.bkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()
                         join e5 in qry on new { e.wmsno, e.bllid, e4.barcode, e4.bkr, e4.gdsid, e4.gdstype } equals new { e5.wmsno, e5.bllid, e5.barcode, e5.bkr, e5.gdsid, e5.gdstype }
                         join e6 in WmsDc.gds on e5.gdsid equals e6.gdsid
                         join e7 in
                             (from m in
                                  WmsDc.v_wms_pkg
                              group m by new { m.gdsid, m.cnvrto, m.pkgdes, } into g
                              select g.Key) on new { e6.gdsid } equals new { e7.gdsid }
                         into joinPkg
                         from e8 in joinPkg.DefaultIfEmpty()
                         where qus.Contains(e.qu.Trim())
                         && arrqry.Contains(e.wmsno)
                         && e.lnkbllid == bllid
                         && e.bllid == WMSConst.BLL_TYPE_FRUITRETRIEVE
                         select new
                         {
                             e.wmsno,
                             e.bllid,
                             e4.gdsid,
                             e6.gdsdes,
                             e6.spc,
                             e6.bsepkg,
                             e8.pkgdes,
                             e8.cnvrto,
                             e4.qty,
                             e4.preqty,
                             cwqty = (from cw in WmsDc.wms_cwgdsbs
                                      where cw.barcode == e4.barcode
                                      group cw by cw.barcode into g
                                      select g.Sum(c => c.qty)).FirstOrDefault(),
                             bkremp = e1.empdes,
                             e4.barcode,
                             e4.bokflg,
                             e4.bokdat,
                             pkg03 = GetPkgStr(e4.qty, e8.cnvrto, e8.pkgdes),
                             pkg03pre = GetPkgStr(e4.preqty, e8.cnvrto, e8.pkgdes)
                         };
            qrymst = qrymst.Distinct();
            var arrqrymst = qrymst.ToArray();
            return arrqrymst;
        }


        protected Object[] FindBllFromCangMst103(String begindat, String enddat, String barcode, String bllid, String bkr, String gdsid)
        {
            String fscprdid = GetCurrentFscprdid();

            var qry = from e in WmsDc.wms_cangdtl
                      join e1 in WmsDc.wms_cang on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }                      
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      join e4 in WmsDc.emp on e.bkr equals e4.empid
                      where e1.lnkbllid==bllid
                      && qus.Contains(e1.qu.Trim())
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e3.gdsid,
                          e.gdstype,
                          e3.bcd1,
                          e.barcode,
                          e.bkr,
                          e.bokflg,
                          e.bokdat,
                          bkrdes = e4.empdes
                      };
            //如果没有时间查询条件就查询当前会计期间的单据
            if ( string.IsNullOrEmpty(begindat) && string.IsNullOrEmpty(enddat) )
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(bkr))
            {
                qry = qry.Where(e => (e.bkr == bkr) || e.bkrdes.Contains(bkr));
            }            
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => e.gdsid == gdsid.Trim());
            }

            var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_cang
                         join e4 in WmsDc.wms_cangdtl on new { e.wmsno, e.bllid } equals new { e4.wmsno, e4.bllid }
                         join e1 in WmsDc.emp on e4.bkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()
                         join e5 in qry on new { e.wmsno, e.bllid, e4.barcode, e4.bkr, e4.gdsid, e4.gdstype } equals new { e5.wmsno, e5.bllid, e5.barcode, e5.bkr, e5.gdsid, e5.gdstype }
                         join e6 in WmsDc.gds on e5.gdsid equals e6.gdsid
                         join e7 in
                             (from m in
                                  WmsDc.v_wms_pkg
                              group m by new { m.gdsid, m.cnvrto, m.pkgdes, } into g
                              select g.Key) on new { e6.gdsid } equals new { e7.gdsid }
                         into joinPkg
                         from e8 in joinPkg.DefaultIfEmpty()
                         where qus.Contains(e.qu.Trim())
                         && arrqry.Contains(e.wmsno)
                         && e.lnkbllid == bllid
                         select new
                         {
                             e.wmsno,
                             e.bllid,
                             e4.gdsid,
                             e6.gdsdes,
                             e6.spc,
                             e6.bsepkg,
                             e8.pkgdes,
                             e8.cnvrto,
                             e4.qty,
                             e4.preqty,
                             cwqty = (from cw in WmsDc.wms_cwgdsbs
                                      where cw.barcode == e4.barcode
                                      group cw by cw.barcode into g
                                      select g.Sum(c => c.qty)).FirstOrDefault(),
                             bkremp = e1.empdes,
                             e4.barcode,
                             e4.bokflg, 
                             e4.bokdat,
                             pkg03 = GetPkgStr(e4.qty, e8.cnvrto, e8.pkgdes),
                             pkg03pre = GetPkgStr(e4.preqty, e8.cnvrto, e8.pkgdes)
                         };
            var arrqrymst = qrymst.ToArray();            
            return arrqrymst;
        }

        protected void d(String wmsno, String bllid, String actid, String brief, String qu, String savdptid)
        {
            if (WMSConst.DEBUG)
            {
                Log.i(LoginInfo.Usrid, Mdlid, wmsno, bllid, actid, brief, qu, savdptid);
            }
        }

        protected void iFile(string desc)
        {
            if (WMSConst.DEBUG)
            {
                try
                {
                    string filename = Mdlid + "_" + RouteData.Values["action"] + "_" + GetCurrentDay() + "_" + LoginInfo.Usrid.Trim() + ".txt";
                    filename = Server.MapPath("/WMS") + "\\" + filename;
                    StreamWriter sw = null;
                    if (System.IO.File.Exists(filename))
                    {
                        sw = System.IO.File.CreateText(filename);
                    }
                    else
                    {
                        FileStream fs = System.IO.File.OpenWrite(filename);
                        fs.Seek(0, SeekOrigin.End);
                        sw = new StreamWriter(fs);
                    }                    
                    sw.WriteLine("[" + GetCurrentDate() + "]    " + desc);
                    sw.Close();
                }
                catch (Exception ex)
                {
                }
            }
        }

        protected void i(String wmsno, String bllid, String actid, String brief, String qu, String savdptid)
        {
            Log.i(LoginInfo.Usrid, Mdlid, wmsno, bllid, actid, brief, qu, savdptid);
        }
        protected void iDelCwgdsbs(IEnumerable<wms_cwgdsbs> qrydtl)
        {
            foreach (var q in qrydtl)
            {
                i(q.barcode, "", "[PDA]" + Mdldes + "删除帐表",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    q.qu,
                    q.savdptid);
            }
        }
        /*protected void iDelTpDtl(IEnumerable<wms_blltp> qrydtl, wms_bllmst mst)
        {
            foreach (var q in qrydtl)
            {
                i(q.wmsno, q.bllid, "[PDA]" + Mdldes + "删除托盘明细",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    mst.qu,
                    mst.savdptid);
            }
        }*/
        protected void iDelTpDtl(IEnumerable<wms_blltp> qrydtl, wms_bllmst mst)
        {
            foreach (var q in qrydtl)
            {
                i(q.wmsno, q.bllid, "[PDA]" + Mdldes + "删除托盘明细",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    mst.qu,
                    mst.savdptid);
            }
        }
        protected void iDelCangDtl(IEnumerable<wms_cangdtl> qrydtl, wms_cang mst)
        {            
            foreach (var q in qrydtl)
            {                                
                i(q.wmsno, q.bllid, "[PDA]" + Mdldes + "删除明细",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    mst.qu,
                    mst.savdptid);
            }
        }
        protected void iDelCangMst(wms_cang mst)
        {
            i(mst.wmsno, mst.bllid, "[PDA]" + Mdldes + "删除主表",
                    "wmsno:" + mst.wmsno + ",bllid:" + mst.bllid,
                    mst.qu,
                    mst.savdptid);
        }

        protected void iDelCangDtl105(IEnumerable<wms_cangdtl_105> qrydtl, wms_cang_105 mst)
        {
            foreach (var q in qrydtl)
            {
                i(q.wmsno, q.bllid, "[PDA]" + Mdldes + "删除明细",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    mst.qu,
                    mst.savdptid);
            }
        }
        protected void iDelCangMst105(wms_cang_105 mst)
        {
            i(mst.wmsno, mst.bllid, "[PDA]" + Mdldes + "删除主表",
                    "wmsno:" + mst.wmsno + ",bllid:" + mst.bllid,
                    mst.qu,
                    mst.savdptid);
        }

        protected void iDelCangDtl109(IEnumerable<wms_cangdtl_109> qrydtl, wms_cang_109 mst)
        {
            foreach (var q in qrydtl)
            {
                i(q.wmsno, q.bllid, "[PDA]" + Mdldes + "删除明细",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    mst.qu,
                    mst.savdptid);
            }
        }
        protected void iDelCangMst109(wms_cang_109 mst)
        {
            i(mst.wmsno, mst.bllid, "[PDA]" + Mdldes + "删除主表",
                    "wmsno:" + mst.wmsno + ",bllid:" + mst.bllid,
                    mst.qu,
                    mst.savdptid);
        }

        protected void iDelCangDtl110(IEnumerable<wms_cangdtl_110> qrydtl, wms_cang_110 mst)
        {
            foreach (var q in qrydtl)
            {
                i(q.wmsno, q.bllid, "[PDA]" + Mdldes + "删除明细",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    mst.qu,
                    mst.savdptid);
            }
        }
        protected void iDelCangMst110(wms_cang_110 mst)
        {
            i(mst.wmsno, mst.bllid, "[PDA]" + Mdldes + "删除主表",
                    "wmsno:" + mst.wmsno + ",bllid:" + mst.bllid,
                    mst.qu,
                    mst.savdptid);
        }

        protected void iDelCangDtl111(IEnumerable<wms_cangdtl_111> qrydtl, wms_cang_111 mst)
        {
            foreach (var q in qrydtl)
            {
                i(q.wmsno, q.bllid, "[PDA]" + Mdldes + "删除明细",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    mst.qu,
                    mst.savdptid);
            }
        }
        protected void iDelCangMst111(wms_cang_111 mst)
        {
            i(mst.wmsno, mst.bllid, "[PDA]" + Mdldes + "删除主表",
                    "wmsno:" + mst.wmsno + ",bllid:" + mst.bllid,
                    mst.qu,
                    mst.savdptid);
        }

        protected void iDelBllDtl(IEnumerable<wms_blldtl> qrydtl, wms_bllmst mst)
        {
            foreach (var q in qrydtl)
            {
                i(q.wmsno, q.bllid, "[PDA]" + Mdldes + "删除明细",
                    "barcode:" + q.barcode + ",gdsid:" + q.gdsid + ",gdstype:" + q.gdstype + ",qty:" + q.qty,
                    mst.qu,
                    mst.savdptid);
            }
        }
        protected void iDelBllMst(wms_bllmst mst)
        {
            i(mst.wmsno, mst.bllid, "[PDA]" + Mdldes + "删除主表",
                    "wmsno:" + mst.wmsno + ",bllid:" + mst.bllid,
                    mst.qu,
                    mst.savdptid);         
        }

        protected Object[] FindBllFromCangMst105(String bllid, String begindat, String enddat, String wmsno, String gdsid, String barcode, String mkr, string isAdt)
        {
            String fscprdid = GetCurrentFscprdid();

            var qry = from e in WmsDc.wms_cangdtl_105
                      join e1 in WmsDc.wms_cang_105 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }                      
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      join e4 in WmsDc.v_wms_pkg on new { e.gdsid } equals new { e4.gdsid }
                      join e5 in WmsDc.gds on e.gdsid equals e5.gdsid
                      where e.bllid == bllid
                      && qus.Contains(e1.qu.Trim())
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)                      
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e.gdsid,
                          e.gdstype,
                          e3.bcd1,
                          e.barcode,
                          vlddat = e.vlddat.Trim(),
                          bthno = e.bthno.Trim(),
                          e1.chkflg,
                          gdsdes = e5.gdsdes.Trim(),
                          pkg03 = GetPkgStr(e.qty, e4.cnvrto, e4.pkgdes),
                          pkg03pre = GetPkgStr(e.preqty, e4.cnvrto, e4.pkgdes)
                      };            
            if (!String.IsNullOrEmpty(isAdt))
            {
                char[] flg = isAdt.ToCharArray();
                qry = qry.Where(e => e.chkflg == flg[0]);
            }

            if (!String.IsNullOrEmpty(mkr))
            {
                qry = qry.Where(e => e.mkr == mkr);
            }
            //如果没有时间查询条件就查询当前会计期间的单据
            if (begindat == null && enddat == null)
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(wmsno))
            {
                qry = qry.Where(e => e.wmsno == wmsno);
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => (e.bcd1 == gdsid || e.gdsid == gdsid));
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_cang_105
                         join e1 in WmsDc.emp on e.mkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()
                         where
                         qry.Where(ee=>ee.wmsno==e.wmsno).Any()                         
                         && e.bllid == bllid
                         select new
                         {                             
                             e.bllid,
                             e.brief,
                             e.chkdat,
                             e.chkflg,
                             e.ckr,
                             e.mkedat,
                             e.mkr,
                             e.opr,
                             e.prvid,
                             e.qu,
                             e.savdptid,
                             e.wmsno,
                             e3.prvdes,
                             bokall = (from et in WmsDc.wms_cangdtl_105
                                       where et.bllid == e.bllid
                                       && et.wmsno == e.wmsno
                                       && (et.bokflg == null || et.bokflg == GetN())
                                       select et).Count(),
                             mkrdes = e1.empdes,
                             dtls = qry.Where(ee=>ee.wmsno==e.wmsno.Trim()).Select(ee=>ee),
                             e.lnkbocino
                         };
            var arrqrymst = qrymst.ToArray();

            return arrqrymst;
        }
        protected Object[] FindBllFromCangMst109(String bllid, String begindat, String enddat, String wmsno, String gdsid, String barcode, String mkr, String rcvdptid)
        {
            String fscprdid = GetCurrentFscprdid();

            /*var qry = from e in WmsDc.wms_cangdtl_109
                      join e1 in WmsDc.wms_cang_109 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      where e.bllid == bllid                      
                      && qus.Contains(e1.qu.Trim())
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e3.gdsid,
                          e3.bcd1,
                          e1.rcvdptid,
                          e.barcode,
                          e1.lnkno
                      };
             */

            var qry = from e in WmsDc.wms_cang_109
                      join e1 in WmsDc.emp on e.mkr equals e1.empid
                      join e2 in WmsDc.prv on e.prvid equals e2.prvid
                      into JoinedEmpPrv
                      from e3 in JoinedEmpPrv.DefaultIfEmpty()
                      join e4 in WmsDc.wms_cangdtl_109 on new { e.wmsno, e.bllid } equals new { e4.wmsno, e4.bllid }
                      join e9 in WmsDc.gds on e4.gdsid equals e9.gdsid
                      join e5 in WmsDc.bcd on new { e4.gdsid, e4.bcd } equals new { e5.gdsid, bcd = e5.bcd1 }
                   into joinBcd
                      from e6 in joinBcd.DefaultIfEmpty()
                      join e7 in
                          WmsDc.v_wms_pkg on new { e4.gdsid } equals new { e7.gdsid }
                      into joinPkg
                      from e8 in joinPkg.DefaultIfEmpty()
                      where qus.Contains(e.qu.Trim())
                      && e.bllid == bllid
                      && (
                      (spqus.Contains(e.qu) && dpts.Contains(e9.dptid))  //商品区权限验证
                      || (thqus.Contains(e.qu) && thDpts.Contains(e9.dptid)) //退货区权限验证
                      || (dtqus.Contains(e.qu) && dtDpts.Contains(e9.dptid)) //堆头区权限验证
                      )
                      select new
                      {
                          e.rcvdptid,
                          e.bllid,
                          e.brief,
                          e.chkdat,
                          e.chkflg,
                          e.ckr,
                          e.mkedat,
                          e.mkr,
                          e.opr,
                          e.prvid,
                          e.qu,
                          e.savdptid,
                          e.wmsno,
                          e3.prvdes,
                          e.lnkno,
                          e.lnkbllid,
                          e.lnkbocino,
                          bokall = (from et in WmsDc.wms_cangdtl_109
                                    where et.bllid == e.bllid
                                    && et.wmsno == e.wmsno
                                    && (et.bokflg == null || et.bokflg == GetN())
                                    select et).Count(),
                          mkrdes = e1.empdes,
                          e4.bcd,
                          e4.gdsid,
                          e4.gdstype,
                          e4.barcode,
                          e4.qty,
                          e4.preqty,
                          e9.gdsdes,
                          e9.spc,
                          e9.bsepkg,
                          pkg03 = GetPkgStr(e4.qty, e8.cnvrto, e8.pkgdes),
                          pkg03pre = GetPkgStr(e4.preqty, e8.cnvrto, e8.pkgdes)
                      };
            //如果没有时间查询条件就查询当前会计期间的单据
            if (begindat == null && enddat == null)
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(wmsno))
            {
                qry = qry.Where(e => e.wmsno == wmsno || e.lnkno == wmsno);
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => (e.bcd == gdsid || e.gdsid == gdsid));
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            if (!string.IsNullOrEmpty(rcvdptid))
            {
                qry = qry.Where(e => e.rcvdptid == rcvdptid.Trim());
            }
            if (!string.IsNullOrEmpty(mkr))
            {
                qry = qry.Where(e => e.mkr == mkr.Trim());
            }
            /*var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_cang_109
                         join e1 in WmsDc.emp on e.mkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()
                         join e4 in WmsDc.wms_cangdtl_109 on new { e.wmsno, e.bllid } equals new { e4.wmsno, e4.bllid }
                         join e9 in WmsDc.gds on e4.gdsid equals e9.gdsid
                         join e5 in WmsDc.bcd on new { e4.gdsid, e4.bcd } equals new { e5.gdsid, bcd = e5.bcd1 }
                      into joinBcd
                         from e6 in joinBcd.DefaultIfEmpty()
                         join e7 in WmsDc.pkg on new { e4.gdsid, pkgid='3' } equals new { e7.gdsid, pkgid=e7.iscseorspt }
                         into joinPkg
                         from e8 in joinPkg.DefaultIfEmpty()
                         where qus.Contains(e.qu.Trim())
                         && arrqry.Contains(e.wmsno)
                         && e.bllid == bllid
                         select new
                         {
                             e.rcvdptid,
                             e.bllid,
                             e.brief,
                             e.chkdat,
                             e.chkflg,
                             e.ckr,
                             e.mkedat,
                             e.mkr,
                             e.opr,
                             e.prvid,
                             e.qu,
                             e.savdptid,
                             e.wmsno,
                             e3.prvdes,
                             e.lnkno,
                             e.lnkbllid,
                             e.lnkbocino,
                             bokall = (from et in WmsDc.wms_cangdtl_109
                                       where et.bllid == e.bllid
                                       && et.wmsno == e.wmsno
                                       && (et.bokflg == null || et.bokflg == GetN())
                                       select et).Count(),
                             mkrdes = e1.empdes
                         };
            var arrqrymst = qrymst.ToArray();*/
            var arrqrymst = qry.ToArray();

            return arrqrymst;
        }
        protected Object[] FindBllFromCangMst110(String bllid, String begindat, String enddat, String wmsno, String gdsid, String barcode)
        {
            String fscprdid = GetCurrentFscprdid();

            var qry = from e in WmsDc.wms_cangdtl_110
                      join e1 in WmsDc.wms_cang_110 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      where e.bllid == bllid                      
                      && qus.Contains(e1.qu.Trim())
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e3.gdsid,
                          e3.bcd1,
                          e.barcode
                      };
            //如果没有时间查询条件就查询当前会计期间的单据
            if (begindat == null && enddat == null)
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(wmsno))
            {
                qry = qry.Where(e => e.wmsno == wmsno);
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => (e.bcd1 == gdsid || e.gdsid == gdsid));
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_cang_110
                         join e1 in WmsDc.emp on e.mkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()
                         where qus.Contains(e.qu.Trim()) 
                         && arrqry.Contains(e.wmsno)
                         && e.bllid == bllid
                         select new
                         {
                             e.bllid,
                             e.brief,
                             e.chkdat,
                             e.chkflg,
                             e.ckr,
                             e.mkedat,
                             e.mkr,
                             e.opr,
                             e.prvid,
                             e.qu,
                             e.savdptid,
                             e.wmsno,
                             e3.prvdes,
                             bokall = (from et in WmsDc.wms_cangdtl_110
                                       where et.bllid == e.bllid
                                       && et.wmsno == e.wmsno
                                       && (et.bokflg == null || et.bokflg == GetN())
                                       select et).Count(),
                             mkrdes = e1.empdes
                         };
            var arrqrymst = qrymst.ToArray();

            return arrqrymst;
        }
        protected Object[] FindBllFromCangMst111(String bllid, String begindat, String enddat, String wmsno, String gdsid, String barcode)
        {
            String fscprdid = GetCurrentFscprdid();

            var qry = from e in WmsDc.wms_cangdtl_111
                      join e1 in WmsDc.wms_cang_111 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      where e.bllid == bllid                      
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      && qus.Contains(e1.qu.Trim())
                      select new
                      {
                          e.wmsno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e3.gdsid,
                          e3.bcd1,
                          e.barcode
                      };
            //如果没有时间查询条件就查询当前会计期间的单据
            if (begindat == null && enddat == null)
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(wmsno))
            {
                qry = qry.Where(e => e.wmsno == wmsno);
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => (e.bcd1 == gdsid || e.gdsid == gdsid));
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_cang_111
                         join e1 in WmsDc.emp on e.mkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()
                         where qus.Contains(e.qu.Trim())
                         &&arrqry.Contains(e.wmsno)
                         && e.bllid == bllid
                         select new
                         {
                             e.bllid,
                             e.brief,
                             e.chkdat,
                             e.chkflg,
                             e.ckr,
                             e.mkedat,
                             e.mkr,
                             e.opr,
                             e.prvid,
                             e.qu,
                             e.savdptid,
                             e.wmsno,
                             e3.prvdes,
                             bokall = (from et in WmsDc.wms_cangdtl_111
                                       where et.bllid == e.bllid
                                       && et.wmsno == e.wmsno
                                       && (et.bokflg == null || et.bokflg == GetN())
                                       select et).Count(),
                             mkrdes = e1.empdes
                         };
            var arrqrymst = qrymst.ToArray();

            return arrqrymst;
        }
        protected Object[] FindBllFromBllMst101(String bllid, String begindat, String enddat, String wmsno, String gdsid, String barcode,String prvid, String dptid)
        {
            String fscprdid = GetCurrentFscprdid();
            var qry = from e in WmsDc.wms_blldtl
                      join e1 in WmsDc.wms_bllmst on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e4 in WmsDc.odr on e1.lnknewno  equals e4.odrno
                      join e2 in WmsDc.bcd on new { e.gdsid, e.bcd } equals new { e2.gdsid, bcd = e2.bcd1 }                      
                      into joinBcd
                      from e3 in joinBcd.DefaultIfEmpty()
                      where e.bllid == bllid
                      && qus.Contains(e1.qu.Trim())
                      && (e1.savdptid == LoginInfo.DefCsSavdptid || e1.savdptid == LoginInfo.DefSavdptid)
                      select new
                      {
                          e.wmsno,
                          e1.lnknewno,
                          e.bllid,
                          e1.mkedat,
                          e1.mkr,
                          e3.gdsid,
                          e3.bcd1,
                          e.barcode,
                          e1.prvid,
                          e4.dptid
                      };
            if (!String.IsNullOrEmpty(prvid))
            {
                qry = qry.Where(e => e.prvid == prvid);
            }
            if (!String.IsNullOrEmpty(dptid))
            {
                qry = qry.Where(e => e.dptid == dptid);
            }

            //如果没有时间查询条件就查询当前会计期间的单据
            if (begindat == null && enddat == null)
            {
                qry = qry.Where(e => e.mkedat.Substring(2, 4) == fscprdid);
            }
            if (!string.IsNullOrEmpty(begindat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(begindat) >= 0);
            }
            if (!string.IsNullOrEmpty(enddat))
            {
                qry = qry.Where(e => e.mkedat.CompareTo(enddat) < 0);
            }
            if (!string.IsNullOrEmpty(wmsno))
            {
                qry = qry.Where(e => e.lnknewno.Contains(wmsno.Trim()) || e.wmsno.Contains(wmsno.Trim()));
            }
            if (!string.IsNullOrEmpty(gdsid))
            {
                qry = qry.Where(e => (e.bcd1 == gdsid || e.gdsid == gdsid));
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                qry = qry.Where(e => e.barcode.Contains(barcode.Trim()));
            }
            var arrqry = qry.Select(e => e.wmsno).ToArray();

            var qrymst = from e in WmsDc.wms_bllmst
                         join e1 in WmsDc.emp on e.mkr equals e1.empid
                         join e2 in WmsDc.prv on e.prvid equals e2.prvid
                         into JoinedEmpPrv
                         from e3 in JoinedEmpPrv.DefaultIfEmpty()
                         where arrqry.Contains(e.wmsno)
                         && e.bllid == bllid
                         select new
                         {
                             e.arvdat,
                             e.bllid,
                             e.brief,
                             e.chkdat,
                             e.chkflg,
                             e.ckr,
                             e.hndbllno,
                             e.huojia,
                             e.lnknewbllid,
                             e.lnknewbrief,
                             e.lnknewno,
                             e.mkedat,
                             e.mkr,
                             e.odrdat,
                             e.opr,
                             e.prvid,
                             e.qu,
                             e.savdptid,
                             e.tongdao,
                             e.wmsno,
                             e3.prvdes,
                             bokall = (from et in WmsDc.wms_blldtl
                                       where et.bllid == e.bllid
                                       && et.wmsno == e.wmsno
                                       && (et.bokflg == null || et.bokflg == GetN())
                                       select et).Count(),
                             mkrdes = e1.empdes
                         };
            var arrqrymst = qrymst.ToArray();

            return arrqrymst;
        }

        #region 基础信息查询

        protected bool IsExistBarcode(String barcode)
        {
            var qry = from e in WmsDc.wms_cangwei
                      where e.isvld == GetY()
                      && e.barcode == barcode
                      select e;
            return qry.Count() > 0;
        }

        /// <summary>
        /// 根据货号或者商品编码得到商品的货号
        /// </summary>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        protected String GetGdsidByGdsidOrBcd(String gdsid)
        {
            var qry = from e in WmsDc.gds
                      join e1 in WmsDc.bcd on e.gdsid equals e1.gdsid
                      into joinBcdDefault
                      from e2 in joinBcdDefault.DefaultIfEmpty()
                      where (e.gdsid == gdsid || e2.bcd1 == gdsid)
                      //&& dpts.Contains(e.dptid.Trim())
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
            }
            return arrqry[0].gdsid.Trim();
        }

        /// <summary>
        /// 根据分店编号返回分店信息
        /// </summary>
        /// <param name="dptid"></param>
        /// <returns></returns>
        public ActionResult GetDptByDptid(String dptid)
        {
            var qry = from e in WmsDc.dpt
                      where e.dptid == dptid.Trim() && e.isstp == GetN()
                      && e.regflg == GetY()
                      select new
                      {
                          dptid = e.dptid.Trim(),
                          dptdes = e.dptdes.Trim(),
                          adr = e.adr.Trim(),
                          tel = e.tel.Trim()
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RInfo( "I0395" );
            }

            return RSucc("成功", arrqry, "S0184");
        }

        //判断仓位是否存在
        protected wms_cangwei GetCangweiByBarcode(String Barcode)
        {
            var qry = from e in WmsDc.wms_cangwei
                      where e.barcode == Barcode
                      && qus.Contains(e.qu.Trim())
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
            }
            return arrqry[0];
        }


        /// <summary>
        /// 返回商品基础信息
        /// </summary>
        /// <param name="schInfo">货号/条码/商品名称 查询参数</param>
        /// <returns></returns>
        public ActionResult GetGds(String schInfo)
        {
            var qry = from e in WmsDc.gds
                      join e1 in WmsDc.bcd on e.gdsid equals e1.gdsid
                      where (e.gdsid == schInfo || e.gdsdes.Contains(schInfo.Trim())
                      || e1.bcd1 == schInfo)
                      //&& dpts.Contains(e.dptid.Trim())
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0200");
            }

            return RSucc("成功", arrqry, "S0185");
        }

        protected String GetABcdByGdsid(String gdsid)
        {
            /*
             * 
             *   周胖胖 2016/4/26 14:28:09
             *   要把所有有这个长度判断的都取消哦
             *   只校验有效性哈
             *   只要验证条码是有效的就能过才对
             */
            var qry = from e in WmsDc.bcd
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      where (e.gdsid == gdsid || e.bcd1 == gdsid)
                          //&& dpts.Contains(e1.dptid.Trim())
                      //&& e.bcd1.Trim().Length == 13
                      orderby e.bcd1 descending
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
            }
            return arrqry[0].bcd1;
        }

        /// <summary>
        /// 不判断13位
        /// </summary>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        protected String GetABcdByGdsid1(String gdsid)
        {
            var qry = from e in WmsDc.bcd
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      where (e.gdsid == gdsid || e.bcd1 == gdsid)
                          //&& dpts.Contains(e1.dptid.Trim())
                      //&& e.bcd1.Trim().Length == 13
                      orderby e.bcd1 descending
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
            }
            return arrqry[0].bcd1;
        }

        /// <summary>
        /// 得到Bcd信息
        /// </summary>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        public ActionResult GetBcdByGdsid(String gdsid)
        {
            var qry = from e in WmsDc.bcd
                      join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                      where (e.gdsid == gdsid || e.bcd1 == gdsid)
                      //&& dpts.Contains(e1.dptid.Trim())
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0201");
            }

            return RSucc("成功", arrqry, "S0186");
        }

        /// <summary>
        /// 返回商品基础信息
        /// </summary>
        /// <param name="gdsid">货号/条码</param>
        /// <returns></returns>
        public ActionResult GetGdsByGdsid(String gdsid)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            var qry = from e in WmsDc.gds
                      join e1 in WmsDc.bcd on e.gdsid equals e1.gdsid
                      join e2 in WmsDc.wms_set on new { e.dptid, setid = WMSConst.SET_TYPE_RELATEDPT } equals new { dptid = e2.val2, e2.setid }
                      join e3 in WmsDc.v_wms_pkg on new { e.gdsid } equals new { e3.gdsid }
                      where
                      e.gdsid == gdsid
                      && e2.isvld == GetY()
                      /*&& (e2.val3==LoginInfo.DefSavdptid || e2.val3==LoginInfo.DefCsSavdptid)
                      && (
                      (spqus.Contains(e2.val1) && dpts.Contains(e.dptid))  //商品区权限验证
                      || (thqus.Contains(e2.val1) && thDpts.Contains(e.dptid)) //退货区权限验证
                      || (dtqus.Contains(e2.val1) && dtDpts.Contains(e.dptid)) //堆头区权限验证
                      )*/
                      select new
                      {
                          savdptid = e2.val3.Trim(),
                          qu = e2.val1.Trim(),
                          gdsid = e.gdsid.Trim(),
                          gdsdes = e.gdsdes.Trim(),
                          spc = e.spc.Trim(),
                          bsepkg = e.bsepkg.Trim(),
                          dptid = e.dptid.Trim(),
                          bnd = e.bnd.Trim(),
                          bcd = e1.bcd1.Trim(),
                          isstp = e.isstp,
                          isstpsal = e.isstpsal,
                          cnvrto = e3.cnvrto,
                          pkgdes = e3.pkgdes
                      };
            var arrqry = qry.ToArray();

            if (arrqry.Length <= 0)
            {
                return RNoData("N0202");
            }

            //查看有无权限
            if (!arrqry.Where(e =>
                    (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                      && (
                      (spqus.Contains(e.qu) && dpts.Contains(e.dptid))  //商品区权限验证
                      || (thqus.Contains(e.qu) && thDpts.Contains(e.dptid)) //退货区权限验证
                      || (dtqus.Contains(e.qu) && dtDpts.Contains(e.dptid)) //堆头区权限验证
                )).Any())
            {
                return RNoData("I0476");
            }

            
            return RSucc("成功！", arrqry, "S0187");
        }



        /// <summary>
        /// 得到商品的包装
        /// </summary>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        public ActionResult GetGdsPkg(String gdsid)
        {
            var qry = from e in WmsDc.v_wms_pkg
                      where e.gdsid == gdsid
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0253");
            }
            
            return RSucc("成功", arrqry, "S0226");

        }

        /// <summary>
        /// 获取供应商信息
        /// </summary>
        /// <param name="prvid">供应商编号、供应商名称</param>
        /// <returns>ResultMessage对象</returns>
        public ActionResult GetPrv(String prvid)
        {
            prvid = prvid.ToLower();
            var qry = from e in WmsDc.prv
                      where
                      (e.prvdes.Contains(prvid.Trim()) || e.prvid == prvid || e.srtcde.Contains(prvid.Trim()))
                      && e.isstp == GetN()
                      select new
                      {
                          prvid = e.prvid.Trim(),
                          prvdes = e.prvdes.Trim(),
                          srtcde = e.srtcde.Trim()
                      };
            var arrqry = qry.ToArray();

            //1.未找到供应商信息
            if (arrqry.Count() <= 0)
            {
                return RInfo("I0465");

            }

            //2.返回查询到的供应商信息
            Rm.PaginationObj.PageCount = arrqry.Count();            
            return RSucc("成功", arrqry, "S0227");

        }

        /// <summary>
        /// 根据货号得到仓位(残损)
        /// </summary>
        /// <param name="savdptid"></param>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        public wms_cangwei GetBarcodeByGdsid(String savdptid, String gdsid)
        {
            var qry = from e in WmsDc.gds
                      join e1 in WmsDc.wms_set on new { e.dptid, savdptid = savdptid } equals new { dptid = e1.val2, savdptid = e1.val3 }
                      join e2 in WmsDc.wms_cangwei on new { savdptid = e1.val3, qu = e1.val1 } equals new { e2.savdptid, e2.qu }
                      where e1.setid == WMSConst.SET_TYPE_RELATEDPT
                      && e.gdsid == gdsid
                      && e2.isvld == GetY()
                      //&& dpts.Contains(e.dptid.Trim())
                      && qus.Contains(e2.qu.Trim())
                      select e2;
            var qry1 = from e in WmsDc.gds
                       join e1 in WmsDc.wms_set on new { dptid = e.dptid, savdptid = savdptid } equals new { dptid = e1.val2.Trim().ToLower(), savdptid = e1.val3 }
                       join e2 in WmsDc.wms_cangwei on new { savdptid = e1.val3, qu = e1.val1 } equals new { e2.savdptid, e2.qu }
                       where e1.setid == WMSConst.SET_TYPE_RELATEDPT
                       && e.gdsid == gdsid
                       && e2.isvld == GetY()
                       //&& dpts.Contains(e.dptid.Trim())
                       && qus.Contains(e2.qu.Trim())
                       select e2;
            qry = qry.Union(qry1);

            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
            }

            return arrqry[0];
        }

        /// <summary>
        /// 根据区位码得到关联部门编码
        /// </summary>
        /// <param name="qu"></param>
        /// <returns></returns>
        public GetRealteQuResult[] GetRelateDpt(String qu)
        {
            //1.得到登录的配送权限
            String[] savdptid = (from e in this.LoginInfo.DatPwrs
                                 select e.savdptid).ToArray();

            var qry = from e in WmsDc.wms_set
                      where e.setid == WMSConst.SET_TYPE_RELATEDPT
                      && savdptid.Contains(e.val3.Trim())
                      && e.isvld == GetY()
                      && e.val1 == qu
                      select new GetRealteQuResult
                      {
                          qu = e.val1,
                          dptid = e.val2,
                          savdptid = e.val3
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
            }

            //Rm.ResultObject = arrqry;
            return arrqry;
        }


        /// <summary>
        /// 根据dptid查找区位
        /// </summary>
        /// <param name="qu">区位码</param>
        /// <returns></returns>
        protected GetRealteQuResult GetRealteQu(String dptid, String asavdptid)
        {
            //1.得到登录的配送权限
            String[] savdptid = (from e in this.LoginInfo.DatPwrs
                                 where e.savdptid == asavdptid.Trim()
                                 select e.savdptid).ToArray();

            var qry = from e in WmsDc.wms_set
                      where e.setid == WMSConst.SET_TYPE_RELATEDPT
                      && e.isvld == GetY()
                      && e.val2 == dptid && savdptid.Contains(e.val3.Trim())
                      select new GetRealteQuResult
                      {
                          qu = e.val1.Trim(),
                          dptid = e.val2.Trim(),
                          savdptid = e.val3.Trim()
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
                /*return RRInfo("I0466");
*/
            }

            return arrqry[0];
        }

        /// <summary>
        /// 得到商品类型
        /// </summary>
        /// <returns></returns>
        public ActionResult GetGdsType()
        {
            var qry = from e in WmsDc.wms_set
                      where e.setid == WMSConst.SET_TYPE_GOODSTYPE
                      && e.isvld==GetY()
                      select new
                      {
                          gdstype = e.type,
                          gdstypedes = e.typedes
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0254");

            }

            return RSucc("成功", arrqry.ToArray(), "S0228");
                        
        }

        //通过区得到Savdptid
        protected String GetSavdptidByQu(String qu)
        {
            var qry = from e in WmsDc.wms_set
                      where e.setid == "006" && e.val1 == qu
                      && e.isvld == GetY()
                      select e.val3;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return null;
            }

            return arrqry[0];
        }

        /// <summary>
        /// 通过配送中心得到库信息
        /// </summary>
        /// <param name="storeid"></param>
        /// <returns></returns>
        protected dpt[] GetSavdptsByStore(String storeid)
        {
            var qry = from e in WmsDc.dpt
                      where e.prtdptid == storeid
                      select e;
            return qry.ToArray();
        }

        protected String[] GetSavdptidsByStore(String storeid)
        {
            dpt[] dpts = GetSavdptsByStore(storeid);
            if (dpts != null && dpts.Length >= 0)
            {
                return dpts.Select(e => e.dptid.Trim()).ToArray();
            }
            return null;
        }

        protected String[] GetQuByGdsidAndBarcode(String gdsid, String oldbarcode, String storeid)
        {
            String[] qu = GetQuByGdsid(gdsid, storeid);
            var qry = from e in WmsDc.wms_cangwei                      
                      where
                      e.barcode==oldbarcode &&
                      (e.savdptid == LoginInfo.DefCsSavdptid||e.savdptid==LoginInfo.DefSavdptid)                      
                      select e.qu;
            return qry.ToArray();
        }

        protected wms_set[] GetQuSetByGdsid(String gdsid, String storeid)
        {            
            var qrysavdpt = from e in WmsDc.wms_set
                            where e.setid == WMSConst.SET_TYPE_STORETYPEDEFIND
                            && e.val3 == storeid
                            && e.isvld == GetY()
                            select e;
            var arrqrysavdpt = qrysavdpt.ToArray();
            if (arrqrysavdpt.Length <= 0)
            {
                return null;
            }
            String savdptid = null;
            String cssavdptid = null;
            if (arrqrysavdpt.Length == 1)
            {
                savdptid = arrqrysavdpt[0].val1;
            }
            if (arrqrysavdpt.Length > 1)
            {
                savdptid = (from e in arrqrysavdpt
                            where e.val2 == "1"
                            select e).Single().val1;
                cssavdptid = (from e in arrqrysavdpt
                              where e.val2 == "2"
                              select e).Single().val1;
            }

            var qry1 = from e in WmsDc.gds
                       join e1 in WmsDc.wms_set on new { dptid = e.dptid, savdptid = savdptid, setid = WMSConst.SET_TYPE_RELATEDPT } equals new { dptid = e1.val2, savdptid = e1.val3, setid = e1.setid }
                       where e.gdsid == gdsid
                       && e1.isvld == GetY()
                       && dpts.Contains(e.dptid.Trim())
                       select e1;
            var qry2 = from e in WmsDc.gds
                       join e1 in WmsDc.wms_set on new { dptid = e.dptid, savdptid = cssavdptid, setid = WMSConst.SET_TYPE_RELATEDPT } equals new { dptid = e1.val2, savdptid = e1.val3, setid = e1.setid }
                       where e.gdsid == gdsid                       
                       && thDpts.Contains(e.dptid.Trim())
                       && e1.isvld == GetY()
                       select e1;
            var qry3 = from e in WmsDc.gds
                       join e1 in WmsDc.wms_set on new { dptid = "ALL", savdptid = savdptid, setid = WMSConst.SET_TYPE_RELATEDPT } equals new { dptid = e1.val2, savdptid = e1.val3, setid = e1.setid }
                       where e.gdsid == gdsid                           
                       && dtDpts.Contains(e.dptid.Trim())
                       && e1.isvld == GetY()
                       select e1;
            var qry = qry1.Union(qry2).Union(qry3);
            //qry = qry.Where(e => e.val3 == savdptid1);
            var arrqry = qry.ToArray();

            return arrqry;
        }
        

        protected String[] GetQuByGdsid(String gdsid, String storeid)
        {
            String[] ret = null;
            var arrqry = GetQuSetByGdsid(gdsid, storeid);            
            if (arrqry.Length > 0)
            {
                ret = arrqry.Select(e => e.val1.Trim()).ToArray();
            }
            return ret;
        }

        /// <summary>
        /// 得到新的收货号
        /// </summary>
        /// <returns></returns>
        protected String GetNewBllNo(String savdptid, String bllid)
        {
            var qry = from e in WmsDc.wms_bll
                      where e.bllid == bllid
                      && e.savdptid == savdptid
                      && e.fscprdid == GetCurrentFscprdid()
                      select e;
            var arrqry = qry.ToArray();
            String sNewBllno = null;
            if (arrqry.Length > 0)
            {
                int vlu = arrqry[0].nowvlu + 1;
                sNewBllno = savdptid.Trim() + GetCurrentFscprdid().Trim() + vlu.ToString().PadLeft(8, '0');
                arrqry[0].nowvlu += 1;
            }

            return sNewBllno;
        }

        protected string GetQuByBarcode(string newbarcode)
        {
            var qry = from e in WmsDc.wms_cangwei
                      where e.barcode.Trim() == newbarcode.Trim()
                      && savdpts.Contains(e.savdptid)
                      && qus.Contains(e.qu)
                      select e;
            wms_cangwei cw = qry.FirstOrDefault();
            if (cw == null)
            {
                return null;
            }
            return cw.qu;
        }

        /// <summary>
        /// 设置新单号
        /// </summary>
        /// <param name="bllid"></param>
        protected void SetNewBllNo(String savdptid, String bllid)
        {
            /*var qry = from e in WmsDc.wms_bll
                      where e.savdptid == savdptid
                      && e.bllid == bllid
                      select e;
            WmsDc.ExecuteCommand("update wms_bll set nowvlu=nowvlu+1 where savdptid={0} and bllid={1}", new[] { savdptid, bllid });
             */
        }

        /// <summary>
        /// 制新单
        /// </summary>
        /// <param name="savdptid">仓库</param>
        /// <param name="bllid">需要制单的单号</param>
        /// <param name="func">新建后</param>
        protected ActionResult MakeNewBllNo(String savdptid, String qu, String bllid, Func<String, ResultMessage> func)
        {
            using (TransactionScope scop = new TransactionScope())
            {
                ////正在生成拣货单，请稍候重试                 
                //string storeid = GetStoreidBySavdptid(savdptid);
                //if (DoingRetrieve(storeid, qu))
                //{
                //    return RInfo("I0051");
                //}

                ResultMessage ret = new ResultMessage();
                String bllno = GetNewBllNo(savdptid, bllid);
                if (bllno != null)
                {
                    SetNewBllNo(savdptid, bllid);
                    ret = func(bllno);
                    //如果不成功就不提交
                    if (ret.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return Json(ret, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    ret.ResultCode = ResultMessage.RESULTMESSAGE_INFO;
                    ret.ResultDesc = "新订单号生成失败";
                    ret.ResultObject = null;
                    return Json(ret, JsonRequestBehavior.AllowGet);
                }

                try
                {
                    WmsDc.SubmitChanges();
                    scop.Complete();

                    return Json(ret, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    ret.ResultCode = ResultMessage.RESULTMESSAGE_ERRORS;
                    ret.ResultDesc = ex.Message;

                    return Json(ret, JsonRequestBehavior.AllowGet);
                }
            }
        }

        

        /// <summary>
        /// 得到当期会计期间
        /// </summary>
        /// <returns></returns>
        protected String GetCurrentFscprdid()
        {
            return DateTime.Now.ToString("yyMM");
        }

        /// <summary>
        /// 得到当前日期
        /// </summary>
        /// <returns></returns>
        protected String GetCurrentDay()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        /// <summary>
        /// 得到昨天
        /// </summary>
        /// <returns></returns>
        protected String GetPrevDay()
        {
            return DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
        }

        /// <summary>
        /// 得到明天
        /// </summary>
        /// <returns></returns>
        protected String GetNextDay()
        {
            return DateTime.Now.AddDays(1).ToString("yyyyMMdd");
        }

        /// <summary>
        /// 得到当前日期和时间
        /// </summary>
        /// <returns></returns>
        protected String GetCurrentDate()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }
        
        /// <summary>
        /// 得到最小层
        /// </summary>
        /// <param name="savdptid"></param>
        /// <param name="qu"></param>
        /// <param name="tongdao"></param>
        /// <returns></returns>
        public ActionResult GetMinCengByTongdao(String savdptid, string qu, string tongdao)
        {
            var qry = from e in WmsDc.wms_cangwei
                      where e.savdptid == savdptid.Trim() && e.qu == qu && e.tongdao == tongdao
                      && e.isvld == GetY() && e.tjflg == GetY()
                      group e by new { e.savdptid, e.qu, e.tongdao } into g
                      select new
                      {
                          g.Key.savdptid,g.Key.qu,g.Key.tongdao,
                          ceng = g.Min(e=>e.ceng).Trim()
                      };
            var cw = qry.FirstOrDefault();
            if (cw == null)
            {
                return RNoData("N0203");
            }
            return RSucc("成功", cw, "S0188");
        }

        /// <summary>
        /// 得到配送信息
        /// </summary>
        /// <returns></returns>
        public ActionResult GetSavdpts()
        {
            object[] Savdpts = new object[] { 
                new { savdptid = "S161", savdptdes="温江配送" },                
                new { savdptid = "899", savdptdes="西河配送" },
                new { savdptid = "992", savdptdes="簇桥配送" },
                new { savdptid = "993", savdptdes="高新配送" }                
            };

            return RSucc("成功", Savdpts, "S0189");
        }

        /// <summary>
        /// 报损原因
        /// </summary>
        /// <returns></returns>
        public ActionResult GetLosRsn()
        {
            var qry = from e in WmsDc.losrsn
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0204");
            }

            return RSucc("成功", arrqry, "S0190");
        }

        /// <summary>
        /// 返仓原因
        /// </summary>
        /// <returns></returns>
        public ActionResult GetRcbakrsn()
        {
            var qry = from e in WmsDc.rcbakrsn
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("N0205");
            }

            return RSucc("成功", arrqry, "S0191");
        }

        /// <summary>
        /// 判断仓位里面是否有库存
        /// </summary>
        /// <param name="barcode">仓位</param>
        /// <param name="gdsid">商品货号</param>
        /// <param name="gdstype">商品类型</param>
        /// <returns></returns>
        protected bool HasQtyInBarcode(String barcode, string gdsid, string gdstype)
        {
            GdsInBarcode[] gdss = GetGdsQtyInBarcodeComm(barcode).Where(e => e.gdsid == gdsid && e.gdstype == e.gdstype).ToArray();
            return (gdss != null && gdss.Length > 0);
        }
        #endregion
    }



}
