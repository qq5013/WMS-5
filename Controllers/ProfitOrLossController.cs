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

        private ActionResult _MakeParam(String wmsno, String oldbarcodes, String gdsids, String gdstypes, String qtys, String rsns)
        {
            String[] oldbarcode = oldbarcodes.Split(',');
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] gdstype = gdstypes.Split(',');
            String[] rsn = rsns.Split(',');
            //String[] newsbarcode = newbarcodes.Split(',');
            List<wms_cangdtl_111> lstDtl = new List<wms_cangdtl_111>();
            if ((oldbarcode.Length != gdsid.Length)
                && (oldbarcode.Length != qty.Length)
                && (oldbarcode.Length != gdstype.Length)
                && (oldbarcode.Length != rsn.Length) )
            {
                return RInfo("参数数量不一致");
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
                        return RInfo("未找到该商品分区信息,或你无分区权限");
                        //return RInfo("商品" + gdsid[i] + "不在区[" + String.Join(",", s.Substring(0, 2)) + "]内");
                    }
                    if ( !qu.Contains(s.Substring(0, 2)) )
                    {
                        return RInfo("商品" + gdsid[i] + "不在区[" + String.Join(",", qu) + "]内");
                    }
                    //判断分区是否有效
                    if (!IsExistBarcode(s))
                    {
                        return RInfo("仓位码" + s.Trim() + "无效");
                    }

                    wms_cangdtl_111 dtl = new wms_cangdtl_111();
                    dtl.wmsno = wmsno;
                    dtl.bllid = WMSConst.BLL_TYPE_PROFITORLOSS;
                    dtl.rcdidx = i+1;
                    dtl.barcode = s;
                    //判断分区是否有效
                    if (!IsExistBarcode(dtl.barcode))
                    {
                        return RInfo("仓位码" + s.Trim() + "无效");
                    }
                    dtl.gdsid = gdsid[i];
                    dtl.gdstype = gdstype[i];
                    dtl.pkgid = "01";                    
                    double fQty = 0;
                    if (!double.TryParse(qty[i], out fQty))
                    {
                        return RInfo(gdsid[i] + "数量：" + qty[i] + "格式化出错");
                    }
                    dtl.qty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.preqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.pkgqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.gdstype = gdstype[i];
                    dtl.bthno = "";
                    dtl.vlddat = "";
                    JsonResult jr = (JsonResult)GetBcdByGdsid(gdsid[i]);
                    ResultMessage rm = (ResultMessage)jr.Data;
                    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return RInfo(gdsid[i] + "商品条码不正确");
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

            return RSucc("成功", lstDtl.ToArray());
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
                return RNoData("该会计期间未找到有损溢单");
            }

            return RSucc("成功", arrqry);
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
                return RInfo("已有损溢单，请不要重复制单");
            }

            return RSucc("尚无损溢单", null);
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
        public ActionResult MkPrftOLssBll(String barcodes, String gdsids, String gdstypes, String qtys, String rsns)
        {
            String[] barcode = barcodes.Split(',');
            String qu = barcode[0].Substring(0,2);
            String savdptid = GetSavdptidByQu(qu);
            

            return MakeNewBllNo(savdptid, WMSConst.BLL_TYPE_PROFITORLOSS, (bllno) =>
            {
                //检查并创建明细
                JsonResult jr = (JsonResult)_MakeParam(bllno, barcodes, gdsids, gdstypes, qtys, rsns);
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
                    rm.ResultCode = ResultMessage.RESULTMESSAGE_INFO;
                    rm.ResultDesc = "已有损溢制单，请不要重复制单";
                    rm.ResultObject = null;
                    return rm;                    
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
                            rm.ResultCode = ResultMessage.RESULTMESSAGE_INFO;
                            rm.ResultDesc = "商品" + d.gdsid + "，原因不能为空";
                            rm.ResultObject = null;
                            return rm;
                        }

                        //得到一个商品的库存数量
                        GdsInBarcode[] gb = GetAGdsQtyInBarcode(d.barcode, d.gdsid, d.gdstype);
                        double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                        double ktqty = bqty;  //可调数量 = 库存数量
                        //如果 需调整数量 > 可调数量
                        if (d.qty > ktqty)
                        {
                            rm.ResultCode = ResultMessage.RESULTMESSAGE_NODATA;
                            rm.ResultDesc = "商品" + d.gdsid + "，在" + d.barcode + "仓位中，无库存信息";
                            rm.ResultObject = null;
                            return rm;
                        }             
                    }
                }                

                WmsDc.wms_cang_111.InsertOnSubmit(mst);
                WmsDc.wms_cangdtl_111.InsertAllOnSubmit(dtls);

                try
                {
                    WmsDc.SubmitChanges();
                    rm.ResultCode = ResultMessage.RESULTMESSAGE_SUCCESS;
                    rm.ResultDesc = "成功";
                    rm.ResultObject = mst;
                    return rm;
                }
                catch (Exception ex)
                {
                    rm.ResultCode = ResultMessage.RESULTMESSAGE_ERRORS;
                    rm.ResultDesc = ex.Message;
                    if (ex.Message.IndexOf("牺牲品") > 0)
                    {
                        rm.ResultCode = ResultMessage.RESULTMESSAGE_DEALTHREAD;
                        rm.ResultDesc = "数据提交异常，请重新提交";
                    }
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
                return RNoData("未找到该单据");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo("你没有该区域的权限");
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核，不能修改");
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo("非本人制单，不能修改");
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
                return RSucc("成功", null);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
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
                return RNoData("未找到该单据");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo("你没有该区域的权限");
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo("非本人制单，不能修改");
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核，不能修改");
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
                return RSucc("成功", null);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
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
                return RInfo("仓位码" + newbarcode + "无效");
            }

            //单据是否找到
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到该单据");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo("你没有该区域的权限");
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核，不能修改");
            }
            //未找到该商品
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("该单据未查找到该商品");
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo("非本人制单，不能修改");
            }

            //判断QTY应该为正为负数
            if (mst.times.Trim() == "+" && qty < 0)
            {
                return RInfo("该单据为溢单，请填写正数");
            }
            if (mst.times.Trim() == "-" && qty > 0)
            {
                return RInfo("该单据为损单，请填写负数");
            }
            if (mst.times.Trim() == "-" && rsn.Trim() == "")
            {
                return RInfo("商品" + gdsid + "，原因不能为空");
            }
                        
            //判断新仓位码是否和主单是一个区
            if (mst.qu.Trim() != newbarcode.Substring(0, 2))
            {
                return RInfo("新仓位码和单据不是一个分区");
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
                GdsInBarcode[] gb = GetAGdsQtyInBarcode(dtl.barcode, gdsid, dtl.gdstype);
                double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                double ktqty = Math.Abs( bqty) + Math.Abs( dtl.qty);  //可调数量 = 库存数量+本单该商品的数量
                //如果 需调整数量 > 可调数量
                if (Math.Abs(qty) > Math.Abs(ktqty))
                {
                    return RInfo("需要调整的数量" + qty + "大于可调整数量" + ktqty);
                }

                dtl.brfdtl = rsn.ToString();
                //return RInfo("商品" + gdsid + "，在" + arrqrydtl[0].barcode + "仓位中，无库存信息");
            }

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", arrqrydtl[0]);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
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
        public ActionResult AdPrftOLsses(String wmsno, String barcodes, String gdsids, String gdstypes, String qtys, String rsns)
        {
            //判断barcodes、gdsids、gdstypes、qtys是否数量一致
            String[] barcode = barcodes.Split(',');
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] gdstype = gdstypes.Split(',');
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
                return RInfo("barcodes、gdsids、gdstypes、qtys是否数量不一致");
            }

            for (int i = 0; i < gdsid.Length; i++)
            {
                double d = 0;
                if (!double.TryParse(qty[i], out d))
                {
                    return RInfo(gdsid[i] + "数量“" + qty[i] + "”解析出错");
                }

                JsonResult jr = (JsonResult)AdPrftOLss(wmsno, barcode[i], gdsid[i], gdstype[i], d, rsn[i]);
                ResultMessage rm = (ResultMessage)jr.Data;
                if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return jr;
                }
                retObjs.Add(rm.ResultObject);
            }

            return RSucc("成功", retObjs);
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
        public ActionResult AdPrftOLss(String wmsno, String barcode, String gdsid, String gdstype, double qty, String rsn)
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
                return RNoData("未找到该单据");
            }
            //判断分区是否有效
            if (!IsExistBarcode(barcode))
            {
                return RInfo("仓位码" + barcode.Trim() + "无效");
            }

            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu))
            {
                return RInfo("你没有该区域的权限");
            }

            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核，不能修改");
            }
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo("非本人制单，不能修改");
            }
            //判断商品是否已经再单据里面
            int iHasIn = arrqrydtl.Where(e => e.gdsid == gdsid && e.gdstype == gdstype && e.barcode == barcode).Count();
            if (iHasIn > 0)
            {
                return RInfo("商品" + gdsid + "，已在该单据中，请不要重复增加");
            }
            //如果是报损，判断是否有库存
            if (mst.times.Trim() == "-")
            {
                if (rsn.Trim() == "")
                {
                    return RInfo("商品" + gdsid + "，原因不能为空");
                }

                //如果是报损，判断是否有库存                
                //得到一个商品的库存数量
                GdsInBarcode[] gb = GetAGdsQtyInBarcode(barcode, gdsid, gdstype);
                double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                double ktqty = bqty;  //可调数量 = 库存数量
                //如果 需调整数量 > 可调数量
                if (Math.Abs(qty) > Math.Abs(ktqty))
                {
                    return RInfo("需要调整的数量" + qty + "大于可调整数量" + ktqty);
                }
                //return RInfo("商品" + gdsid + "，在" + barcode + "仓位中，无库存信息");
            }
                      
            

            //判断gdsid和barcode是不是在一个区
            String[] qu = GetQuByGdsid(gdsid, LoginInfo.DefStoreid);
            if (!qu.Contains(barcode.Substring(0, 2)))
            {
                return RInfo("商品" + gdsid + "不在区[" + String.Join(",", qu) + "]内");
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
            dtl.bthno = "";
            dtl.vlddat = "";
            JsonResult jr = (JsonResult)GetBcdByGdsid(gdsid);
            ResultMessage rm = (ResultMessage)jr.Data;
            if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return RInfo(gdsid + "商品条码不正确");
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
                return RSucc("成功", dtl);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
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
        public ActionResult MdPrftOLssBll(String wmsno, String barcodes, String gdsids, String gdstypes, String qtys, String rsns)
        {        
            //拆分参数
            //检查并创建明细
            JsonResult jr = (JsonResult)_MakeParam(wmsno, barcodes, gdsids, gdstypes, qtys, rsns);
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
                return RNoData("未找到该单据");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo("你没有该区域的权限");
            }
            //检查单号是否已经审核
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核，不能修改");
            }
   
            //是否是本人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo("非本人制单，不能修改");
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
                        return RInfo("商品" + d.gdsid + "，原因不能为空");
                    }

                    //得到一个商品的库存数量
                    GdsInBarcode[] gb = GetAGdsQtyInBarcode(arrqrydtl[i].barcode, arrqrydtl[i].gdsid, arrqrydtl[i].gdstype);
                    double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                    double ktqty = Math.Abs(bqty) + Math.Abs(arrqrydtl[i].qty);  //可调数量 = 库存数量+本单该商品的数量
                    //如果 需调整数量 > 可调数量
                    if (Math.Abs(d.qty) > Math.Abs(ktqty))
                    {
                        return RInfo("需要调整的数量" + d.qty + "大于可调整数量" + ktqty);
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
                return RSucc("成功", newdtl);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
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
                return RNoData("未找到该单据");
            }
            //单据是否找到
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("未找到该单据明细");
            }
            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo("你没有该区域的权限");
            }

            return RSucc("成功", arrqrydtl);
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
                return RNoData("未找到该单据");
            }

            //检查是否有数据权限
            wms_cang_111 mst = arrqrymst[0];
            if (!qus.Contains(mst.qu.Trim()))
            {
                return RInfo("你没有该区域的权限");
            }

            return RSucc("成功", arrqrymst); 
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
                return RInfo("仓位码" + barcode.Trim() + "无效");
            }

            var arrqrymst = FindBllFromCangMst111(WMSConst.BLL_TYPE_PROFITORLOSS, begindat, enddat, wmsno, gdsid, barcode);
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到符合条件的单据");
            }
            return RSucc("成功", arrqrymst);
        }
    }
}
