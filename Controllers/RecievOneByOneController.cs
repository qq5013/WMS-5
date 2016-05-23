using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web.Mvc;
using WMS.Common;
using WMS.Models;

namespace WMS.Controllers
{
    /// <summary>
    /// 收货模块(边收边上架)
    /// </summary>
    public class RecievOneByOneController : SsnController
    {

        protected override void SetModuleInfo()
        {
            this.Mdlid = "RecievOneByOne";
            this.Mdldes = "收货模块(边收边上架)";
        }

        private String GetRcvBll(String wmsno, String savdptid)
        {
            var qry = from e in WmsDc.wms_cang
                      where e.lnkno == wmsno && e.lnkbllid == WMSConst.BLL_TYPE_REVIECEBLL
                      && e.savdptid == savdptid.Trim()
                      select e.wmsno;
            String wno = qry.FirstOrDefault();

            return wno;
        }

        /// <summary>
        /// 确认商品收货 
        /// </summary>
        /// <param name="wmsno">收货单单号</param>
        /// <param name="gdsid">商品编码</param>
        /// <param name="gdstypes">单据商品序号</param>
        /// <param name="qtys">实收数量</param>
        /// <param name="tpcodes">托盘码</param>
        /// <param name="pkgids">包装ID</param>
        /// <returns>wms_blldtl, wms_blltp</returns>
        /// 如果 qtys,tpcodes,pkgids 不传参，就修改实收数量为0
        [PWR(Pwrid = WMSConst.WMS_BACK_收货确认, pwrdes = "收货确认")]
        public ActionResult BokRecievGds(String wmsno, String gdsid, String gdstypes, String qtys, String tpcodes, String pkgids)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            if (gdsid == null)
            {
                return RInfo("货号无效！");
            }

            double[] qty = null;
            if (!String.IsNullOrEmpty(qtys))
            {
                String[] sqtys = qtys.Split(',');
                List<double> lstqtys = new List<double>();
                foreach (String s in sqtys)
                {
                    lstqtys.Add(double.Parse(s));
                }
                qty = lstqtys.ToArray();
            }
            String[] tpcode = !String.IsNullOrEmpty(tpcodes) ? tpcodes.Split(',') : null;
            String[] pkgid = !String.IsNullOrEmpty(pkgids) ? pkgids.Split(',') : null;
            String[] gdstype = gdstypes.Split(',');
            using (TransactionScope scop = new TransactionScope())
            {

                var qrymst = from e in WmsDc.wms_bllmst
                             where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                             select e;
                //1.判断收货单是否已经审核，如果审核则退出
                var qry11 = from e in WmsDc.wms_blldtl
                            join e1 in WmsDc.wms_bllmst on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                            where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL && (e.gdsid == gdsid) /*&& e.rcdidx == rcdidx*/
                            select e;
                var qry1 = from e in WmsDc.wms_blldtl
                           join e1 in WmsDc.wms_bllmst on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                           join e2 in WmsDc.bcd on e.gdsid equals e2.gdsid
                           where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL && (e.gdsid == gdsid)/*&& e.rcdidx == rcdidx*/
                           select new { e, e1 };
                var qry = from e in qry1
                          select new
                          {
                              wmsno = e.e.wmsno.Trim(),
                              lnknewno = e.e1.lnknewno.Trim(),
                              e.e1.chkflg,
                              bllid = e.e1.bllid,
                              e.e1.qu,
                              gdsid = e.e.gdsid,
                              e.e.qty,
                              gdstype = e.e.gdstype,
                              rcdidx = e.e.rcdidx,
                              preqty = e.e.preqty,
                              mkr = e.e1.mkr
                          };
                var arrqry = qry.ToArray();


                //var arrqry11 = qry11.ToArray();
                foreach (wms_blldtl bdtl11 in qry11)
                {
                    bdtl11.qty = Math.Round(bdtl11.preqty, 4, MidpointRounding.AwayFromZero);
                }


                //0.删除商品审核
                var qrysh = from e in WmsDc.wms_blltp
                            where e.savdptid == LoginInfo.DefSavdptid
                            && e.qu == arrqry[0].qu
                            && e.gdsid == gdsid
                            && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                            && e.wmsno == wmsno
                            /*&& e.rcdidx == rcdidx*/
                            select e;
                var arrqrysh = qrysh.ToArray();
                if (arrqrysh != null && arrqrysh.Length > 0)
                {
                    WmsDc.wms_blltp.DeleteAllOnSubmit(arrqrysh);
                    iDelTpDtl(arrqrysh, qrymst.FirstOrDefault());
                }
                //WmsDc.SubmitChanges();

                //1.1.没有找到订单
                if (arrqry.Length <= 0)
                {
                    return RNoData("未找到单号,或者商品编码有误");
                }
                //1.3.盘点是否是同一个操作人
                if (!IsSameLogin(arrqry[0].mkr))
                {
                    return RInfo("收货审核人" + LoginInfo.Usrid + ",和制单人" + arrqry[0].mkr + ",不是同一个人");
                }

                //1.2.订单已经审核
                /*if (arrqry[0].chkflg == GetY())
                {
                    Rm.ResultCode = ResultMessage.RESULTMESSAGE_INFO;
                    Rm.ResultDesc = "订单已经审核,不能修改订单";
                    return ReturnResult();
                }*/
                //2.判断托盘参数和数量参数的数目是否一致
                List<wms_blltp> blltps = new List<wms_blltp>();
                if (!String.IsNullOrEmpty(tpcodes) && !String.IsNullOrEmpty(qtys) && qty != null)    //如果托盘参数不为空
                {
                    if (qty.Length != tpcode.Length || pkgid.Length != qty.Length)
                    {
                        return RInfo("托盘参数和数量参数数目不一致！");
                    }
                    //3.将商品导入托盘表                    
                    for (int i = 0; i < tpcode.Length; i++)
                    {
                        wms_blltp blltp = new wms_blltp();
                        blltp.wmsno = wmsno;
                        blltp.bllid = arrqry[0].bllid;
                        blltp.qu = arrqry[0].qu;
                        blltp.rcdidx = arrqry[0].rcdidx;
                        blltp.rcdidxtp = i;
                        blltp.gdsid = arrqry[0].gdsid;
                        blltp.pkgid = pkgid[i];
                        blltp.qty = Math.Round(qty[i], 4, MidpointRounding.AwayFromZero);
                        blltp.tpcode = tpcode[i];
                        blltp.savdptid = LoginInfo.DefSavdptid;
                        blltp.gdstype = gdstype[i];
                        blltp.bokflg = GetN();
                        blltp.bkr = "";
                        blltp.bokdat = "";
                        blltps.Add(blltp);
                    }

                    WmsDc.wms_blltp.InsertAllOnSubmit(blltps);
                }
                var qry2 = from e in qry1
                           select e.e;
                var arrqry2 = qry2.ToArray();
                //5.修改实收数量，并登帐
                arrqry2[0].qty = tpcodes != null ? Math.Round(blltps.Sum(s => s.qty), 4, MidpointRounding.AwayFromZero) : 0;
                arrqry2[0].bokflg = GetY();
                arrqry2[0].bokdat = DateTime.Now.ToString("yyyyMMddhhmmss");
                arrqry2[0].bkr = LoginInfo.Usrid;
                WmsDc.SubmitChanges();
                //如果明细实收数据为0，就直接审核, 不在生成上架单
                wms_blldtl[] dtls = GetDtl(wmsno);
                if (dtls.Sum(e => e.qty) == 0 && dtls.Count(e => e.bokflg == GetY()) == dtls.Count())
                {
                    wms_bllmst mst = qrymst.FirstOrDefault();
                    if (mst != null)
                    {
                        ActionResult ar = BokReciev(wmsno);
                        JsonResult jr = (JsonResult)ar;
                        ResultMessage rm = (ResultMessage)jr.Data;
                        if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                        {
                            return ar;
                        }
                    }
                }

                //6.判断实收是否大于应收
                if (arrqry2[0].qty > arrqry[0].preqty)
                {
                    return RInfo("实收数量大于应收数量！");
                }
                else
                {
                    String savdptid = LoginInfo.DefSavdptid;
                    //按照实收数量收货， 记日志
                    if (arrqry2[0].qty != arrqry[0].preqty)
                    {
                        Log.i(LoginInfo.Usrid, this.Mdlid, wmsno, WMSConst.BLL_TYPE_REVIECEBLL,
                            "收货审核", arrqry[0].gdsid.Trim() + ":应收:" + arrqry[0].preqty + ";实收:" + arrqry2[0].qty,
                            arrqry[0].qu, savdptid);
                    }

                }

                try
                {
                    WmsDc.SubmitChanges();
                    scop.Complete();
                    Rm.ResultCode = ResultMessage.RESULTMESSAGE_SUCCESS;
                    Rm.ResultDesc = "登帐成功！";
                    Rm.ResultObject = new
                    {
                        wms_blldtl = arrqry2[0],
                        wms_blltp = blltps
                    };
                    return ReturnResult();
                }
                catch (Exception ex)
                {
                    return RErr(ex.Message);
                }
            }
        }

        /// <summary>
        /// 通过上架单执行函数
        /// </summary>
        /// <param name="wmsno">收货单单号</param>
        /// <param name="savdptid">配送编码</param>
        /// <param name="func">需要执行的函数</param>
        /// <returns></returns>
        private ResultMessage RDoActByRcvbll(String wmsno, String qu, String savdptid, Func<string, ResultMessage> func)
        {
            JsonResult ar = (JsonResult)DoActByRcvbll(wmsno, qu, savdptid, func);
            ResultMessage rm = (ResultMessage)ar.Data;
            return rm;
        }

        /// <summary>
        /// 通过上架单执行函数
        /// </summary>
        /// <param name="wmsno">收货单单号</param>
        /// <param name="savdptid">配送编码</param>
        /// <param name="func">需要执行的函数</param>
        /// <returns></returns>
        private ActionResult DoActByRcvbll(String wmsno, String qu, String savdptid, Func<string, ResultMessage> func)
        {
            // 根据收货单得到上架单单号
            String bllno = GetRcvBll(wmsno, savdptid);            
            // 如果找到该单据，就返回单号
            if (!string.IsNullOrEmpty(bllno))
            {
                return Json(func(bllno), JsonRequestBehavior.AllowGet);
            }
            else        //如果没有找到，就返回新单据
            {
                return MakeNewBllNo(LoginInfo.DefSavdptid, WMSConst.BLL_TYPE_UPBLL, func);
            }
        }

        /// <summary>
        /// 通过上架单执行函数
        /// </summary>
        /// <param name="wmsno"></param>
        //private ActionResult DoActByRcvbll(String wmsno, String savdptid, Func<string, ActionResult> func)
        //{
        //    String bllno = GetRcvBll(wmsno, savdptid);
        //    // 如果找到该单据，就返回单号
        //    if (!string.IsNullOrEmpty(bllno))
        //    {
        //        return Json(func(bllno));
        //    }
        //    else        //如果没有找到，就返回新单据
        //    {
        //        //func
        //        return MakeNewBllNo(LoginInfo.DefSavdptid, WMSConst.BLL_TYPE_UPBLL, func);
        //    }
        //}

        /// <summary>
        /// 收货查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="wmsno">调整单单号/采购订单号</param>
        /// <param name="gdsid">商品货号、条码</param>
        /// <param name="barcode">仓位</param>
        /// <param name="barcode">供应商编号</param>
        /// <param name="barcode">业务库编码</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货制单, pwrdes = "收货制单")]
        public ActionResult FindBll(String begindat, String enddat, String wmsno, String gdsid, String barcode, String prvid, String dptid)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo("仓位码" + barcode.Trim() + "无效");
            }
            var arrqrymst = FindBllFromBllMst101(WMSConst.BLL_TYPE_REVIECEBLL, begindat, enddat, wmsno, gdsid, barcode, prvid, dptid);

            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到符合条件的单据");
            }
            return RSucc("成功", arrqrymst);
        }

        /// <summary>
        /// 根据供应商提取采购订单
        /// </summary>
        /// <param name="prvid">供应商编码</param>
        /// <returns>ResultMessage对象</returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货查询, pwrdes = "收货查询")]
        public ActionResult GetPurchaseByPrv(String prvid)
        {
            var qry = from e in WmsDc.odr
                      join e1 in WmsDc.prv on e.prvid equals e1.prvid
                      join e2 in WmsDc.dpt on e.dptid equals e2.dptid
                      where e.bllid == WMSConst.BLL_TYPE_PURCHASE
                      && e.prvid == prvid
                      && e.savdptid == LoginInfo.DefSavdptid
                      && e.ordstu == (char)ORD_STATUS.AUDIT && e.zdflg == GetN()
                      && dpts.Contains(e.dptid.Trim())
                      //&& e.istel == GetN()
                      select new
                      {
                          e.arvdat,
                          e.bllid,
                          e.odrno,
                          e.prvid,
                          e.savdptid,
                          e.ordstu,
                          e.zdflg,
                          e.istel,
                          e1.prvdes,
                          e2.dptdes
                      };
            var arrqry = qry.ToArray();

            //1.未找到供应商采购订单
            if (arrqry.Count() <= 0)
            {
                return RInfo("未找到供应商采购订单!");
            }


            //2.返回供应商采购订单            
            return RSucc("成功", arrqry);
        }

        /// <summary>
        /// 得到仓储单查询
        /// </summary>
        /// <param name="wmsno">单据号</param>
        /// <returns></returns>
        protected IQueryable<object> GetWmsbll(String wmsno)
        {
            var qry = from e in WmsDc.wms_bllmst
                      where e.wmsno == wmsno
                      && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                      select new
                      {
                          dtl = (from e1 in WmsDc.wms_blldtl
                                 join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                                 join e3 in WmsDc.v_wms_pkg on new { e2.gdsid } equals new { e3.gdsid }
                                 into joinPkg
                                 from e4 in joinPkg.DefaultIfEmpty()
                                 where e1.wmsno == wmsno
                                 && e1.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                                 select new
                                 {
                                     e1.barcode,
                                     e1.bcd,
                                     e1.bkr,
                                     e1.bllid,
                                     e1.bokdat,
                                     e1.bokflg,
                                     e1.brief,
                                     e1.bthno,
                                     e1.gdsid,
                                     e1.gdstype,
                                     e1.pkgid,
                                     e1.preqty,
                                     e1.prvid,
                                     e1.qty,
                                     e1.rcdidx,
                                     e1.vlddat,
                                     e1.wmsno,
                                     e2.spc,
                                     e2.gdsdes,
                                     e2.bsepkg,
                                     pkg03 = GetPkgStr(e1.qty, e4.cnvrto, e4.pkgdes),
                                     pkg03pre = GetPkgStr(e1.preqty, e4.cnvrto, e4.pkgdes)
                                 }).ToArray()
                      };
            return qry;
        }

        //public ActionResult 

        /// <summary>
        /// 查找当天的收货单
        /// </summary>
        /// <param name="day">查询日期</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货查询, pwrdes = "收货查询")]
        public ActionResult FindCurrDayRcvBlls(String day)
        {

            var qry = from e in WmsDc.wms_bllmst
                      join e5 in WmsDc.odr on e.lnknewno equals e5.odrno
                      join e6 in WmsDc.dpt on e5.dptid equals e6.dptid
                      join e7 in WmsDc.prv on e.prvid equals e7.prvid
                      join e8 in WmsDc.emp on e.mkr equals e8.empid
                      where e.mkedat.Substring(0, 8) == day
                      && e.savdptid == LoginInfo.DefSavdptid
                      && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                      //&& e.mkr == LoginInfo.Usrid
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
                          mkrdes = e8.empdes,
                          e.odrdat,
                          e.opr,
                          e.prvid,
                          e.qu,
                          e.savdptid,
                          e.tongdao,
                          e.wmsno,
                          e7.prvdes
                      };
            /*select new Wmsbll
            {
                mst = e,
                dptdes = e6.dptdes,
                prv = (from e4 in WmsDc.prv
                       where e4.prvid == e.prvid
                       select new WmsBllPrv
                       {
                           prvid = e4.prvid.Trim(),
                           prvdes = e4.prvdes.Trim(),
                       }).Single(),
                dtl = (from e1 in WmsDc.wms_blldtl
                       where e1.wmsno == e.wmsno
                       select e1).ToArray(),
                gds = (from e2 in WmsDc.wms_blldtl
                       join e3 in WmsDc.gds on e2.gdsid equals e3.gdsid
                       where e2.wmsno == e.wmsno
                       select new WmsBllGds
                       {
                           gdsid = e3.gdsid.Trim(),
                           gdsdes = e3.gdsdes.Trim(),
                           bsepkg = e3.bsepkg.Trim(),
                           spc = e3.spc.Trim()
                       }).ToArray(),
                dtls = (from e1 in WmsDc.wms_blldtl
                        join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                        where e1.wmsno == e.wmsno
                        select new
                        {
                            e1.wmsno,
                            e1.vlddat,
                            e1.rcdidx,
                            e1.qty,
                            e1.prvid,
                            e1.preqty,
                            e1.pkgid,
                            e1.gdstype,
                            e1.gdsid,
                            e1.brief,
                            e1.bokflg,
                            e1.bokdat,
                            e1.bllid,
                            e1.bkr,
                            e1.bcd,
                            e1.barcode,
                            e2.gdsdes,
                            e2.spc,
                            e2.bsepkg
                        }
                             ).ToArray()*/


            return RSucc("成功", qry.ToArray());
        }

        /// <summary>
        /// 得到收货单
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货查询, pwrdes = "收货查询")]
        public ActionResult GetRecievBll(String wmsno)
        {
            var qry = GetWmsbll(wmsno);

            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                return RNoData("未找到收货单，请检查单号是否正确。");
            }

            return RSucc("成功", arrqry[0]);
        }

        /// <summary>
        /// 生成收货单
        /// </summary>
        /// <param name="odrnos">订单编号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货制单, pwrdes = "收货制单")]
        public ActionResult GenerateRecievBlls(String odrnos)
        {
            String[] odrno = odrnos.Split(',');
            JsonResult jr = null;

            List<object> lstobj = new List<object>();
            foreach (String odr in odrno)
            {
                jr = (JsonResult)GenerateRecievBll(odr.Trim());
                //WmsDc.SubmitChanges();

                Rm = (ResultMessage)jr.Data;

                if (Rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return jr;
                }

                lstobj.Add(Rm.ResultObject);
            }
            Rm.ResultObject = lstobj;

            return Json(Rm, "application/json", JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 查到未审核单据
        /// </summary>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货查询, pwrdes = "收货查询")]
        public ActionResult FindUnAdtRecievBll(char chkflg, String prvid, String bdtm, String edtm)
        {

            var qry = from e in WmsDc.wms_bllmst
                      where qus.Contains(e.qu.Trim())
                      && e.savdptid == LoginInfo.DefSavdptid
                      && e.chkflg == GetN()
                      && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                      select e;
            if (chkflg != null)
            {
                qry = qry.Where(w => w.chkflg == chkflg).Select(e => e);
            }

            var qry1 = from e in qry
                       join e1 in WmsDc.prv on e.prvid equals e1.prvid
                       select new Wmsbll
                       {
                           mst = e,
                           prv = new WmsBllPrv
                           {
                               prvid = e1.prvid,
                               prvdes = e1.prvdes
                           }
                       };

            var bllmsts = qry.ToArray();
            if (bllmsts.Length <= 0)
            {
                return RNoData("未找到未审核收货单");
            }

            return RSucc("成功", bllmsts);
        }

        /// <summary>
        /// 生成收货单
        /// </summary>
        /// <param name="odrno">订单编号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货制单, pwrdes = "收货制单")]
        public ActionResult GenerateRecievBll(String odrno)
        {
            return MakeNewBllNo(
                LoginInfo.DefSavdptid,
                WMSConst.BLL_TYPE_REVIECEBLL,
                ((bllno) =>
                {
                    //WmsDc.Transaction = WmsDc.Connection.BeginTransaction();                    
                    var qry = from e in WmsDc.odr
                              where e.odrno == odrno
                              && e.savdptid == LoginInfo.DefSavdptid
                              && e.bllid == WMSConst.BLL_TYPE_PURCHASE
                              && e.ordstu == (char)ORD_STATUS.AUDIT && e.zdflg == GetN()
                              select new
                              {
                                  mst = e,
                                  dtl = from e1 in WmsDc.odrdtl
                                        where e1.odrno == odrno
                                        select e1
                              };
                    var arrqry = qry.ToArray();
                    //1.未找到供应商采购订单
                    if (arrqry.Count() <= 0)
                    {
                        Rm.ResultCode = ResultMessage.RESULTMESSAGE_INFO;
                        Rm.ResultDesc = "未找到供应商采购订单!";
                        Rm.ResultObject = null;
                        Rm.ExtObject = null;
                        return Rm;
                    }

                    //2.查看采购单据是否已经转过为收货单
                    var qryshd = from e in WmsDc.wms_bllmst
                                 where e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                                 && e.lnknewno == odrno
                                 select e;
                    if (qryshd.Count() > 0)
                    {
                        Rm.ResultCode = ResultMessage.RESULTMESSAGE_INFO;
                        Rm.ResultDesc = "该采购单已转换为收货单，请不要重复转单!";
                        Rm.ResultObject = null;
                        Rm.ExtObject = null;
                        return Rm;
                    }

                    var odr = arrqry[0];
                    //修改转单标记
                    String sNow = DateTime.Now.ToString("yyyyMMdd");
                    String cmd = "update odr set zdflg='"+GetY()+"',zddat={0} where odrno={1}";
                    WmsDc.ExecuteCommand(cmd, new[] { sNow, odrno });



                    //===================== 生成一张新的收货单 =====================
                    //--------------------- 1.生成主单 -----------------------------
                    wms_bllmst bllmst = new wms_bllmst();
                    bllmst.wmsno = bllno;
                    bllmst.hndbllno = odr.mst.hndbllno;
                    bllmst.bllid = WMSConst.BLL_TYPE_REVIECEBLL;
                    bllmst.prvid = odr.mst.prvid;
                    bllmst.savdptid = odr.mst.savdptid;
                    //得到登录仓库所在部门的区位码
                    GetRealteQuResult realte = GetRealteQu(odr.mst.dptid, LoginInfo.DefSavdptid);
                    bllmst.qu = realte.qu;
                    /*bllmst.tongdao = odr.mst.tongdao;
                    bllmst.huojia = odr.mst.huojia;*/
                    bllmst.odrdat = odr.mst.odrdat;
                    bllmst.arvdat = odr.mst.arvdat;
                    bllmst.mkr = LoginInfo.Usrid;
                    bllmst.mkedat = DateTime.Now.ToString("yyyyMMddhhmmss");
                    bllmst.ckr = "";
                    bllmst.chkflg = GetN();
                    bllmst.chkdat = "";
                    bllmst.opr = LoginInfo.Usrid;
                    bllmst.brief = odr.mst.brief;
                    bllmst.lnknewbllid = odr.mst.bllid;
                    bllmst.lnknewno = odr.mst.odrno;
                    bllmst.lnknewbrief = odr.mst.brief;
                    this.WmsDc.wms_bllmst.InsertOnSubmit(bllmst);
                    //--------------------- 2.生成明细 -----------------------------
                    List<wms_blldtl> blldtls = new List<wms_blldtl>();
                    List<WmsBllGds> gdss = new List<WmsBllGds>();
                    foreach (odrdtl dtl in odr.dtl)
                    {
                        if (dtl.qty != 0)
                        {
                            wms_blldtl blldtl = new wms_blldtl();
                            blldtl.wmsno = bllmst.wmsno;
                            blldtl.bllid = bllmst.bllid;
                            blldtl.rcdidx = dtl.rcdidx;
                            blldtl.barcode = "";
                            blldtl.gdsid = dtl.gdsid;
                            blldtl.pkgid = "01";   //dtl.pkgid;
                            blldtl.qty = 0;
                            blldtl.preqty = Math.Round(dtl.qty, 4, MidpointRounding.AwayFromZero);
                            blldtl.gdstype = WMSConst.GDS_TYPE_NORMAL;
                            blldtl.bthno = dtl.bthno == null ? "" : dtl.bthno;
                            blldtl.vlddat = dtl.vlddat == null ? "" : dtl.vlddat;
                            blldtl.bcd = dtl.bcd == null ? "" : dtl.bcd;
                            blldtl.prvid = bllmst.prvid == null ? "" : dtl.prvid;
                            blldtl.bkr = "";
                            blldtl.bokflg = GetN();
                            blldtl.bokdat = "";
                            blldtl.brief = "";

                            WmsBllGds gds = WmsDc.gds
                                      .Where(e => e.gdsid == dtl.gdsid)
                                      .Select(e => new WmsBllGds
                                      {
                                          gdsid = e.gdsid.Trim(),
                                          gdsdes = e.gdsdes.Trim(),
                                          spc = e.spc.Trim(),
                                          bsepkg = e.bsepkg.Trim()
                                      }).Single();
                            gdss.Add(gds);

                            blldtls.Add(blldtl);
                        }
                    }
                    this.WmsDc.wms_blldtl.InsertAllOnSubmit(blldtls);
                    //===================== 生成一张新的收货单 =====================

                    try
                    {
                        var dtlstmp = (from e in blldtls
                                       join e1 in gdss on e.gdsid.Trim() equals e1.gdsid.Trim()
                                       select new
                                       {
                                           e.wmsno,
                                           e.vlddat,
                                           e.rcdidx,
                                           e.qty,
                                           e.prvid,
                                           e.preqty,
                                           e.pkgid,
                                           e.gdstype,
                                           e.gdsid,
                                           e.brief,
                                           e.bokflg,
                                           e.bokdat,
                                           e.bllid,
                                           e.bkr,
                                           e.bcd,
                                           e.barcode,
                                           e1.gdsdes,
                                           e1.spc,
                                           e1.bsepkg
                                       }
                                           ).ToArray();
                        Wmsbll obj = new Wmsbll
                        {
                            mst = bllmst,
                            dptdes = (from e in WmsDc.dpt
                                      where e.dptid == odr.mst.dptid
                                      select e.dptdes).Single(),
                            prv = (from e4 in WmsDc.prv
                                   where e4.prvid == bllmst.prvid
                                   select new WmsBllPrv
                                   {
                                       prvid = e4.prvid.Trim(),
                                       prvdes = e4.prvdes.Trim(),
                                   }).Single(),
                            dtl = blldtls.ToArray(),
                            gds = gdss.ToArray(),
                            dtls = dtlstmp
                        };

                        //obj = ((Wmsbll)Rm.ResultObject);
                        var objrm = new
                        {
                            obj.mst.arvdat,
                            obj.mst.bllid,
                            obj.mst.brief,
                            obj.mst.chkdat,
                            obj.mst.chkflg,
                            obj.mst.ckr,
                            obj.mst.hndbllno,
                            obj.mst.huojia,
                            obj.mst.lnknewbllid,
                            obj.mst.lnknewbrief,
                            obj.mst.lnknewno,
                            obj.mst.mkedat,
                            obj.mst.mkr,
                            obj.mst.odrdat,
                            obj.mst.opr,
                            obj.mst.prvid,
                            obj.mst.qu,
                            obj.mst.savdptid,
                            obj.mst.tongdao,
                            obj.mst.wmsno,
                            obj.prv.prvdes,
                            obj.dptdes
                        };
                        Rm.ResultObject = objrm;

                        return Rm;
                    }
                    catch (Exception ex)
                    {
                        Rm.ResultCode = ResultMessage.RESULTMESSAGE_ERRORS;
                        Rm.ResultDesc = ex.Message;
                        Rm.ResultObject = null;
                        Rm.ExtObject = null;
                        return Rm;
                    }

                }));

        }

        /// <summary>
        /// 推荐仓位
        /// </summary>
        /// <returns></returns>        
        protected String SuggestBarcode(String qu, Action<wms_cangwei> action)
        {
            String barcode = null;
            wms_cangwei cw = null;
            /*String[] barcodes = GetEmptyBarcodeByQu(qu);
            var qrybar = from e in WmsDc.wms_cangwei
                         where e.tjflg == GetY() && e.isvld == GetY() && e.tpflg == GetN()
                         && qu == e.qu && barcodes.Contains(e.barcode)                         
                         orderby e.barcode
                         select e;*/
            var qrybar = from e in WmsDc.wms_cangwei
                         where e.savdptid == LoginInfo.DefSavdptid
                         && e.qu == qu
                         && e.isvld == GetY() && e.tjflg == GetY()
                         && e.tpflg == GetN() && e.kcflg == WMSConst.KC_FLG_NONQTY
                         orderby e.barcode
                         select e;

            var arrqrybar = qrybar.Take(1).ToArray();
            if (arrqrybar.Length > 0)
            {
                cw = arrqrybar[0];
                barcode = cw.barcode;
                action(cw);
            }
            else
            {
                barcode = null;
            }

            return barcode;
        }

        /// <summary>
        /// 登帐收货单
        /// </summary>
        /// <param name="wmsno">收货单号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货审核, pwrdes = "收货审核")]
        public ActionResult BokReciev(String wmsno)
        {
            // 得到主单
            #region 得到收货单主单
            wms_bllmst mst = GetMst(wmsno);
            if(mst==null){
                return RNoData("未找到收货单");
            }
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核，不能重复审核");
            }
            #region 判断操作员是否有审核该单据的权限
            //0.判断操作员是否有审核该单据的权限
            var dtapwrs = from e in LoginInfo.DatPwrs
                          select e.qu;
            if (!dtapwrs.Contains(mst.qu.Trim()))
            {
                return RInfo("没有操作该数据的权限！");
            }
            #endregion
            #endregion 得到收货单主单
            // 得到明细单
            #region 得到收货单明细单
            wms_blldtl[] dtls = GetDtl(wmsno);
            if (dtls == null && dtls.Length == 0)
            {
                return RNoData("未找到收货单");
            }
            /*if (dtls.Length == GetY())
            {
                return RInfo("单据已经审核，不能重复审核");
            }*/
            #endregion 得到收货单明细单

            #region 得到托盘明细
            // 得到托盘明细
            wms_blltp[] tps = GetTp(wmsno);
            #region 是否该单据下的所有商品都已经确认

            // 判断是否所有商品都已经完成收货
            var qryHsQty = (from e in WmsDc.wms_blldtl
                 join e1 in WmsDc.wms_bllmst on new { e.wmsno, e.bllid }                
                 equals new { e1.wmsno, e1.bllid }
                 join e4 in WmsDc.gds on e.gdsid equals e4.gdsid
                 join e2 in WmsDc.wms_blltp on new { e.wmsno, e.bllid, e.gdsid }
                 equals new { e2.wmsno, e2.bllid, e2.gdsid } into joinBlltp
                 from e3 in joinBlltp.DefaultIfEmpty()
                 where !(e.qty == 0 && e.bokflg == GetY()) && (e3.wmsno == null)
                 && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                 && e.wmsno==wmsno.Trim()
                 select e4.gdsdes);
            var hsQty = qryHsQty.ToArray();
            if (hsQty.Length>0
              )
            {
                return RInfo("商品"+hsQty[0]+"未收货");
            }

            //1.是否该单据下的所有商品都已经审核                        
            if (tps.Length <= 0 && hsQty.Length > 0)
            {
                return RNoData("无该单据下的托盘信息");
            }
            else
            {
                foreach (wms_blltp tp in tps)
                {
                    if (tp.bokflg == GetN())
                    {
                        return RInfo(tp.tpcode + ",该托盘还未完成收货确认操作！");
                    }
                }
            }
            #endregion
            #endregion 得到托盘明细

            


            // 根据得到的上架单单号处理逻辑
            return DoActByRcvbll(wmsno, mst.qu, mst.savdptid, (bllno) =>
            {
                // 将托盘表（wms_blltp）中审核后的数量回填到明细表（wms_blldtl）中
                #region 将托盘表（wms_blltp）中审核后的数量回填到明细表（wms_blldtl）中
                var arrTpsSumQty = tps.GroupBy(e => new { e.gdsid, e.rcdidx })
                                    .Select(g => new { g.Key.gdsid, g.Key.rcdidx, qty = g.Sum(e => e.qty) })
                                    .ToArray();
                foreach (var tsq in arrTpsSumQty)
                {
                    wms_blldtl dtl = dtls
                                    .Where(e => e.gdsid == tsq.gdsid && e.rcdidx == tsq.rcdidx)
                                    .Select(e => e)
                                    .FirstOrDefault();
                    dtl.qty = tsq.qty;
                }
                WmsDc.SubmitChanges();
                #endregion 将托盘表（wms_blltp）中审核后的数量回填到明细表（wms_blldtl）中
                
                #region 修改采购单实收数量
                //判断是否有比应收数量更大的商品
                //var qrytpdtl = tps
                //               .GroupBy(e=>new{e.wmsno, e.gdsid})
                //               .Select(g=>new{
                //                   wmsno = g.Key.wmsno,
                //                   gdsid = g.Key.gdsid,
                //                   qty = g.Sum(e1 => e1.qty)
                //               });
                               
                //var qryodrdtl = from e in WmsDc.odrdtl
                //                join e1 in WmsDc.wms_bllmst on e.odrno equals e1.lnknewno
                //                join e2 in qrytpdtl on new { e1.wmsno, e.gdsid } equals new { e2.wmsno, e2.gdsid }
                //                where e1.wmsno == wmsno && e1.bllid == WMSConst.BLL_TYPE_REVIECEBLL && e.qty < e2.qty
                //                select e;
                //var arrqryodrdtl = qryodrdtl.ToArray();
                //if (arrqryodrdtl.Length > 0)
                //{
                //    return RRInfo(arrqryodrdtl[0].gdsid + "，实收数量大于应收数量");
                //}
                StringBuilder sb = new StringBuilder();
                String cmdsql = null;
                //修改采购单实收数量                
                cmdsql = "update odrdtl set preqty=qty, qty=isnull(b.sumqty,0), pkgqty=isnull(b.sumqty,0), amt=convert(decimal(18,2),round(a.prc*isnull(b.sumqty,0), 4)), patamt=convert(decimal(18,2),round(a.taxprc*isnull(b.sumqty,0), 4)), taxamt=convert(decimal(18,2),round((a.taxprc*isnull(b.sumqty,0))-a.prc*isnull(b.sumqty,0), 4))   from "
                            + " odrdtl a "
                            + " left join "
                            + " ( "
                            + " select a1.wmsno, a1.gdsid, sum(a1.qty) sumqty ,b1.lnknewno from wms_blltp a1 "
                            + " 	inner join wms_bllmst b1 on a1.wmsno=b1.wmsno and a1.bllid=b1.bllid"
                            + " where b1.wmsno={0} and b1.bllid={1}"
                            + " group by a1.wmsno, a1.gdsid, b1.lnknewno	"
                            + " ) b on a.odrno=b.lnknewno and a.gdsid=b.gdsid "
                            + " where a.odrno in (select lnknewno from wms_bllmst where wmsno={2})";
                sb.Append(cmdsql);
                
                //设置收货单审核标志
                cmdsql = "update wms_bllmst set chkflg='"+GetY()+"', ckr={3}, chkdat={4} where wmsno={5} and bllid={6} ";
                sb.Append(cmdsql);
                //设置采购单收货标志
                cmdsql = "update odr set shflg='" + GetY() + "' from odr a inner join wms_bllmst b on b.lnknewno=a.odrno and b.lnknewbllid=a.bllid where b.wmsno={7} and b.bllid={8} ";
                sb.Append(cmdsql);

                //设置收货单明细确认标志
                cmdsql = "update wms_blldtl set bokflg='" + GetY() + "', bkr={9}, bokdat={10} where wmsno={11} and bllid={12} and bokflg='"+GetN()+"' ";
                sb.Append(cmdsql);

                //执行处理
                string sNow = GetCurrentDate();
                WmsDc.ExecuteCommand(sb.ToString(), new[] { wmsno, WMSConst.BLL_TYPE_REVIECEBLL, wmsno, LoginInfo.Usrid, sNow, wmsno, WMSConst.BLL_TYPE_REVIECEBLL, wmsno, WMSConst.BLL_TYPE_REVIECEBLL, LoginInfo.Usrid, sNow, wmsno, WMSConst.BLL_TYPE_REVIECEBLL });
                try
                {
                    WmsDc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    return RRErr(ex.Message);
                }
                #endregion

                try
                {
                    WmsDc.SubmitChanges();

                    return RRSucc("成功", null);
                }
                catch (Exception ex)
                {
                    return RRErr(ex.Source + ex.Message);
                }

            });


        }

        private wms_blltp[] GetTp(String wmsno)
        {
            var qry1 = from e in WmsDc.wms_blltp
                       where e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                       && e.wmsno == wmsno
                       select e;
            var arrqry1 = qry1.ToArray();
            return arrqry1;
        }

        private wms_blldtl[] GetDtl(String wmsno)
        {
            var qryDtl = from e in WmsDc.wms_blldtl
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                         select e;
            wms_blldtl[] dtl = qryDtl.ToArray();
            return dtl;
        }

        private wms_bllmst GetMst(String wmsno)
        {
            var qryMst = from e in WmsDc.wms_bllmst
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                         && savdpts.Contains(e.savdptid) && qus.Contains(e.qu)
                         select e;
            wms_bllmst mst = qryMst.FirstOrDefault();
            return mst;
        }

        /// <summary>
        /// 得到空仓号
        /// </summary>
        /// <param name="q">区位编码</param>
        /// <returns></returns>
        private string[] GetEmptyBarcodeByQu(string q)
        {
            var sfscprdid = GetCurrentFscprdid();
            var qry1 = from e in WmsDc.wms_cangwei
                       where e.qu == q
                       select new { e.barcode };
            var qry11 = from e in qry1
                        select e.barcode;

            var qry = from e in WmsDc.wms_gdsbs
                      where qry11.Contains(e.barcode.Trim())
                      && e.fscprdid == sfscprdid
                      group e by new { e.barcode, e.fscprdid } into g
                      select new
                      {
                          barcode = g.Key.barcode,
                          fscprdid = g.Key.fscprdid,
                          sumqty = g.Sum(g1 => Math.Pow(-1, g1.dbtcrt) * g1.qty)
                      };
            var qry2 = from e in qry1
                       join e1 in qry on e.barcode equals e1.barcode into allbarcode
                       from e1 in allbarcode.DefaultIfEmpty()
                       where e1.sumqty == 0 || e1.sumqty == null
                       select e.barcode.Trim();

            var arrqry = qry2.ToArray();

            return arrqry;
        }

        /// <summary>
        /// 得到托盘商品
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货查询, pwrdes = "收货查询")]
        public ActionResult ListTpByGdsid(String wmsno, String gdsid, String gdstype)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            if (gdsid == null)
            {
                return RInfo("货号无效！");
            }

            var qrysh = from e in WmsDc.wms_blltp
                        where e.savdptid == LoginInfo.DefSavdptid
                        && e.gdsid == gdsid
                            /*&& e.gdstype == gdstype*/
                        && e.wmsno == wmsno
                        && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                        select e;
            return RSucc("成功", qrysh.ToArray());
        }

        /// <summary>
        /// 删除托盘中的商品
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="tpcode"></param>
        /// <param name="gdsid"></param>
        /// <param name="gdstype"></param>
        /// <param name="rcdidx"></param>
        /// <param name="rcdidxtp"></param>
        /// <returns></returns>
        public ActionResult DelAgdsFrmTpcode(string wmsno, String tpcode, String gdsid, string gdstype, int rcdidx, int rcdidxtp)
        {
            //得到托盘
            wms_blltp[] blltps = GetTp(wmsno);
            if (blltps.Length == 0)
            {
                return RNoData("未找到托盘");
            }
            wms_blltp tp = blltps.Where(e => e.tpcode.Trim() == tpcode.Trim()
                && e.gdsid.Trim() == gdsid.Trim() && e.gdstype.Trim() == gdstype.Trim() && e.rcdidx == rcdidx && e.rcdidxtp == rcdidxtp
                ).FirstOrDefault();
            if (tp == null)
            {
                return RNoData("未找到需要修改的商品信息");
            }
            if (tp.bokflg == GetY())
            {
                return RInfo("该商品已确认，不能修修改");
            }
            
            WmsDc.wms_blltp.DeleteOnSubmit(tp);
            WmsDc.SubmitChanges();
            return RSucc("成功", null);
        }

        /// <summary>
        /// 修改托盘中的商品
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="tpcode"></param>
        /// <param name="gdsid"></param>
        /// <param name="gdstype">需要修改的类型</param>
        /// <param name="rcdidx"></param>
        /// <param name="rcdidxtp"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public ActionResult MdAgdsFrmTpcode(string wmsno, String tpcode, String gdsid, string gdstype, int rcdidx, int rcdidxtp, double qty)
        {
            //得到托盘
            wms_blltp[] blltps = GetTp(wmsno);
            if (blltps.Length == 0)
            {
                return RNoData("未找到托盘");
            }
            wms_blltp tp = blltps.Where(e => e.tpcode.Trim() == tpcode.Trim()
                && e.gdsid.Trim() == gdsid.Trim() && e.gdstype.Trim() == gdstype.Trim() && e.rcdidx == rcdidx && e.rcdidxtp == rcdidxtp
                && e.bokflg== GetN()
                ).FirstOrDefault();
            if (tp == null)
            {
                return RNoData("未找到需要修改的商品信息");
            }
            if (tp.bokflg == GetY())
            {
                return RInfo("该商品已确认，不能修修改");
            }
            // done: 判断增加的数量是否已经超过了应收的数量                        
            wms_blldtl[] blldtls = GetDtl(wmsno);
            double sumPreqty = blldtls.Where(e => e.gdsid.Trim() == gdsid.Trim()).Sum(e => e.preqty);
            double sumHasqty = blltps.Where(e => e.gdsid.Trim() == gdsid.Trim()).Sum(e => e.qty);
            if ((sumHasqty - tp.qty + qty) > sumPreqty)
            {
                return RInfo("增加的数量已经超过应收的数量");
            }
            tp.qty = qty;
            tp.gdstype = gdstype;
            WmsDc.SubmitChanges();
            return RSucc("成功", null);
        }

        /// <summary>
        /// 得到收货单的托盘信息和推荐仓位信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货查询, pwrdes = "收货查询")]
        public ActionResult ListTpByWsmno(String wmsno)
        {
            //按wmsno, tpcode, barcode得到托盘信息
            var qry = from e in WmsDc.wms_bllmst
                      join e1 in WmsDc.wms_blltp on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                      && savdpts.Contains(e.savdptid) && qus.Contains(e.qu)
                      group e1 by new { e1.wmsno, e1.tpcode, e1.barcode, e1.bokflg, e1.bokdat } into g
                      select new
                      {
                          g.Key.wmsno,
                          g.Key.tpcode,
                          g.Key.barcode,
                          g.Key.bokflg,
                          g.Key.bokdat,
                          gdscnt = g.Count(),
                          qtycnt = g.Sum(e1 => e1.qty)
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length == 0)
            {
                return RInfo("得到托盘信息失败");
            }
            return RSucc("成功", arrqry);
        }

        /// <summary>
        /// 确认托盘
        /// </summary>
        /// <param name="wmsno">收货单</param>
        /// <param name="tpcode">托盘码</param>
        /// <returns></returns>
        public ActionResult BokRecievByTp(String wmsno, String tpcode)
        {
            using (TransactionScope scop = new TransactionScope())
            {
                // done: 查询收货单和明细
                var qryMst = from e in WmsDc.wms_bllmst
                             where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                             && qus.Contains(e.qu) && savdpts.Contains(e.savdptid)
                             select e;
                var qryDtlTp = from e in WmsDc.wms_blltp
                               where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                                && qus.Contains(e.qu) && savdpts.Contains(e.savdptid)
                               //&& e.tpcode == tpcode]
                               select e;
                var qryDtlTpCode = qryDtlTp.Where(e => e.tpcode == tpcode && e.bokflg==GetN());
                wms_bllmst mst = qryMst.FirstOrDefault();
                wms_blltp[] dtltpcode = qryDtlTpCode.ToArray();
                wms_blltp[] dtltp = qryDtlTp.ToArray();
                if (mst == null) { return RNoData("未找到单据信息"); }
                if (dtltpcode == null || dtltpcode.Length == 0) { return RNoData("未找到托盘明细信息"); }
                if (mst.chkflg == GetY()) { return RInfo("该单据已经审核,不能重复审核"); }
                if (qryDtlTpCode.Where(e => e.bokflg == GetY()).Any()) { return RInfo("托盘“" + qryDtlTp.First().tpcode + "”已经确定,不能重复确定"); }

                //上架单单号
                    String upwmsno = null;

                // done: 增加上架单
                    String bokdat = GetCurrentDate();
                List<object> lstObj = new List<object>();
                foreach (wms_blltp t in dtltpcode)
                {
                    // done: 修改确认标记
                    t.bokflg = GetY();
                    t.bokdat = bokdat;                    
                    t.bkr = LoginInfo.Usrid;
                    // done: 修改序号;
                    WmsDc.SubmitChanges();
                    int rcdidxtp = GetMaxIdxTp(wmsno, t.gdsid, t.rcdidx);
                    //t.rcdidxtp = rcdidxtp + 1;
                    //WmsDc.SubmitChanges();

                    //判断上架单是否存在，不存在就增加新单据
                    
                    JsonResult jr = (JsonResult)DoActByRcvbll(wmsno, mst.qu, LoginInfo.DefSavdptid, (bllno) =>
                    {
                        upwmsno = bllno;
                        // 增加上货单明细
                        Rm = AddUpShelfDtlAndSuggestBarcode(bllno, mst.qu, wmsno, t.gdsid, t.pkgid, t.qty, t.gdstype, t.tpcode);
                        // 把推荐仓位推荐给wms_blltp
                        wms_cangdtl cdtl = (wms_cangdtl)Rm.ResultObject;
                        if (Rm.ResultCode!=ResultMessage.RESULTMESSAGE_SUCCESS || cdtl == null) { return Rm; }
                        //将推进仓位回填到托盘表中
                        t.barcode = cdtl.barcode;
                        WmsDc.SubmitChanges();

                        //如果商品收货已经完成

                        // done: 设置返回
                        if (Rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                        {
                            return Rm;
                        }
                        return Rm;
                    });
                    ResultMessage rm = (ResultMessage)jr.Data;
                    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return jr;
                    }
                    lstObj.Add(rm.ResultObject);

                }

                //计算wms_cangdtl中对应商品的收货数量
                if (wmsno != null)
                {
                    wms_blltp[] blltps = GetTp(wmsno);
                    wms_blldtl[] blldtls = GetDtl(wmsno);
                    if (blldtls.Length == 0)
                    {
                        return RInfo("上架单明细为空");
                    }
                    foreach (wms_blldtl bd in blldtls)
                    {
                        var tps = blltps.Where(e => e.gdsid.Trim() == bd.gdsid.Trim()
                                && e.tpcode.Trim() == tpcode.Trim())
                                    .Select(e => e);
                        if (tps.Any())
                        {
                            bd.qty += tps.Sum(e=>e.qty);
                        }
                        WmsDc.SubmitChanges();
                    }
                }
                try
                {
                    scop.Complete();
                    return RSucc("成功", lstObj);
                }
                catch (Exception ex)
                {
                    return RErr(ex.Message);
                }
                
            }   //事务结束
            
        }

        /// <summary>
        /// 得到审核的托盘该商品的最大序号
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        private int GetMaxIdxTp(string wmsno, string gdsid, int rcdidx)
        {
            var qryDtlTp = from e in WmsDc.wms_blltp
                               where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                                && qus.Contains(e.qu) && savdpts.Contains(e.savdptid)
                               //&& e.tpcode == tpcode
                               select e;
            int i = 0;
            wms_blltp maxIdxTp = qryDtlTp
                                    .Where(e => e.bokflg == GetY() && e.gdsid == gdsid && e.rcdidx == rcdidx)
                                    .OrderByDescending(e => e.rcdidxtp)
                                    .FirstOrDefault();
            if (maxIdxTp != null) { i = maxIdxTp.rcdidxtp; }

            return i;
        }

        /// <summary>
        /// 确认商品收货
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsids"></param>
        /// <param name="gdstypes"></param>
        /// <param name="qtys"></param>
        /// <param name="tpcode"></param>
        /// <param name="pkgids"></param>
        /// <returns></returns>
        public ActionResult AdGdsByTps(String wmsno, String gdsids, String gdstypes, String qtys, String tpcode, String pkgids)
        {
            using (TransactionScope scop = new TransactionScope())
            {
                #region 验证参数
                if (string.IsNullOrEmpty(tpcode))
                {
                    return RInfo("货号无效！");
                }                
                String[] gdsid = !String.IsNullOrEmpty(gdsids) ? gdsids.Split(',') : null;
                String[] pkgid = !String.IsNullOrEmpty(pkgids) ? pkgids.Split(',') : null;
                String[] gdstype = gdstypes.Split(',');
                String[] qty = !String.IsNullOrEmpty(qtys) ? qtys.Split(',') : null;
                if (gdsid.Length != pkgid.Length
                    || gdsid.Length != gdstype.Length
                    || pkgid.Length != gdstype.Length
                    || gdsid.Length != qty.Length)
                {
                    return RInfo("货号、数量、商品类型或者包装码参数数量不一致");
                }
                #endregion 验证参数

                List<Object> lstObj = new List<object>();
                for (int i = 0; i < gdsid.Length; i++)
                {
                    double iqty = 0;
                    if(!double.TryParse(qty[i], out iqty)){
                        return RInfo("商品"+gdsid[i]+",数量参数解析出错");
                    }
                    JsonResult jr = (JsonResult)AdGdsByTp(wmsno, gdsid[i], gdstype[i], qty[i], tpcode, pkgid[i]);
                    ResultMessage rm = (ResultMessage)jr.Data;
                    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return jr;
                    }                    
                    // 增加上架单
                    /*
                     String[] qu = GetQuByGdsid(gdsid[i], LoginInfo.DefSavdptid);
                    if (qu == null || qu.Length == 0)
                    {
                        return RInfo("得到分区信息出错");
                    }
                    jr = (JsonResult)DoActByRcvbll(wmsno, qu[0], LoginInfo.DefSavdptid, (bllno) =>
                    {
                        AddUpShelfDtlAndSuggestBarcode(bllno, gdsid[i], pkgid[i], iqty, gdstype[i], tpcode );
                        return null;
                    });
                     */
                    if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                    {
                        return jr;
                    }
                    lstObj.Add(rm.ResultObject);
                }

                try
                {
                    scop.Complete();
                    return RSucc("成功", lstObj);
                    
                }
                catch (Exception ex)
                {
                    return RErr(ex.Message);
                }
            }
        }
        
        /// <summary>
        /// 增加上货单明细，并推荐仓位
        /// </summary>
        /// <param name="bllno"></param>
        /// <param name="gdsid"></param>
        /// <param name="pkgid"></param>
        /// <param name="iqty"></param>
        /// <param name="gdstype"></param>
        /// <param name="tpcode"></param>
        private ResultMessage AddUpShelfDtlAndSuggestBarcode(string bllno, string qu, string wmsno, string gdsid, string pkgid, double iqty, string gdstype, string tpcode)
        {
            wms_cang mst = GetCangMst(bllno);
            wms_cangdtl[] dtls = GetCangDtl(bllno)
                                .OrderByDescending(e=>e.rcdidx)
                                .ToArray();
            int iRcdidx = 1;
            if (dtls != null && dtls.Length > 0)
            {
                wms_cangdtl tp1 = dtls.FirstOrDefault();
                iRcdidx = tp1.rcdidx + 1;
            }

            //如果没有主单就加一个主单
            if (mst == null)
            {
                var arrBllmst = GetMst(wmsno);
                mst = new wms_cang();
                mst.bllid = WMSConst.BLL_TYPE_UPBLL;
                mst.brief = "";
                mst.chkdat = "";
                mst.chkflg = GetN();
                mst.ckr = "";
                mst.lnkbllid = WMSConst.BLL_TYPE_REVIECEBLL;
                mst.lnkbocidat = "";
                mst.lnkbocino= "";
                mst.lnkbrief = "";
                mst.lnkno = wmsno;
                mst.mkedat = GetCurrentDay();
                mst.mkedat2 = GetCurrentDate();
                mst.mkr = LoginInfo.Usrid;
                mst.opr = LoginInfo.Usrid;
                mst.prvid = arrBllmst.prvid;
                mst.qu = qu;
                mst.rcvdptid = "";
                mst.savdptid = LoginInfo.DefSavdptid;
                mst.times = "";
                mst.wmsno = bllno;
                WmsDc.wms_cang.InsertOnSubmit(mst);
                WmsDc.SubmitChanges();
            }

            //判断相同托盘是否有barcode，如果有就不用推荐，copy到新的上架单里面
            String sBarcode = null;
            wms_cangdtl sameTpcode = dtls.Where(e => e.tpcode == tpcode && (e.barcode != "" && e.barcode != null)).FirstOrDefault();
            if (sameTpcode != null)
            {
                sBarcode = sameTpcode.barcode;
            }
            else
            {
                sBarcode = SuggestABarcodeByTpcode(bllno, qu, LoginInfo.DefSavdptid);
            }
            
            wms_cangdtl newdtl = new wms_cangdtl();
            newdtl.wmsno = bllno;
            newdtl.bllid = WMSConst.BLL_TYPE_UPBLL;
            newdtl.rcdidx = iRcdidx;
            newdtl.oldbarcode = "";
            newdtl.barcode = sBarcode;
            newdtl.gdsid = gdsid;
            newdtl.pkgid = pkgid;
            newdtl.pkgqty = iqty;
            newdtl.qty = iqty;
            newdtl.gdstype = gdstype;
            newdtl.bthno = "";
            newdtl.vlddat = "";
            newdtl.bcd = GetABcdByGdsid1(gdsid);
            newdtl.tpcode = tpcode;
            newdtl.bkr = "";
            newdtl.bokflg = GetN();
            newdtl.bokdat = "";
            newdtl.preqty = null;
            newdtl.losreason = null;
            WmsDc.wms_cangdtl.InsertOnSubmit(newdtl);
            try{
                WmsDc.SubmitChanges();
                return RRSucc("成功", newdtl);
            }catch(Exception ex){
                return RRErr(ex.Message);
            }            
        }

        private wms_cangdtl[] GetCangDtl(string bllno)
        {
            var qryDtl = from e in WmsDc.wms_cangdtl
                         where e.bllid == WMSConst.BLL_TYPE_UPBLL
                         && e.wmsno == bllno
                         //orderby e.rcdidx descending
                         select e;
            wms_cangdtl[] dtls = qryDtl.ToArray();
            return dtls;
        }

        private wms_cang GetCangMst(string bllno)
        {
            var qryMst = from e in WmsDc.wms_cang
                         where e.bllid == WMSConst.BLL_TYPE_UPBLL
                         && e.wmsno == bllno
                         select e;
            wms_cang mst = qryMst.FirstOrDefault();
            return mst;
        }

        /// <summary>
        /// 推荐仓位
        /// </summary>
        /// <param name="wmsno">上架单单号</param>
        /// <param name="gdsid"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private string SuggestABarcodeByTpcode(String wmsno, string qu, string savdptid)
        {
            // done 推荐仓位
            /*
             * 
             * select @wmsno = wmsno, @qu = qu, @tpcode=tpcode, @savdptid=savdptid from #tmp_wms_blltp where id=@i  
             * 
             * select top 1 @barcode = barcode from wms_cangwei where savdptid=@savdptid and qu=@qu and isvld=GetY() and tjflg=GetY() and tpflg=GetN() and kcflg='0' order by ceng, barcode  
             * --检查日志	
             * 
             * update wms_cangwei set tpflg=GetY() where barcode=@barcode  
             * 
             * insert into #tmp1(barcode) values(@barcode)  
             * update wms_blltp set barcode=@barcode where wmsno=@wmsno and bllid='101' and tpcode=@tpcode and savdptid=@savdptid
             * set @i=@i+1  
             */
            Random rd = new Random();
            int iRnd = rd.Next(100000);

            wms_cangwei cw1 = (from e in WmsDc.wms_cangwei
                              where e.savdptid == savdptid && e.isvld == GetY() && e.qu == qu
                              && e.tjflg == GetY() && e.tpflg == GetN() && e.kcflg == '0'
                               orderby e.ceng, e.barcode
                              select e).FirstOrDefault();

            wms_cangwei cw = (from e in WmsDc.wms_cangwei
                              where e.savdptid == savdptid && e.isvld == GetY() && e.qu == qu
                              && e.tjflg == GetY() && e.tpflg == GetN() && e.kcflg == '0'
                              && !(from e1 in WmsDc.wms_cangdtl                                                                       //*
                                   join e2 in WmsDc.wms_cang on new { e1.wmsno, e1.bllid } equals new { e2.wmsno, e2.bllid }          //*   13:00:35
                                   where e1.barcode == e.barcode                                                                      //*   周胖胖 2016/4/27 13:00:35
                                   && e2.bllid == WMSConst.BLL_TYPE_UPBLL                                                             //* 你这样处理
                                   && e2.mkedat == GetCurrentDay()                                                                    //* 推荐仓位的时候
                                   select 1).Any()                                                                                    //* 把当天所有的wms_cangdtl里面的单据类型为102的给我排除掉
                              orderby e.ceng, e.barcode                                                                               //*
                              select e).FirstOrDefault();                                                                             //*
            if (cw1.barcode != cw.barcode)
            {
                d(wmsno, WMSConst.BLL_TYPE_UPBLL, "rnd=" + iRnd + ",idx=1,"
                                        + ",barcode=" + cw1.barcode + ",isvld=" + cw1.isvld + ",kcflg=" + cw1.kcflg + ",tjflg=" + cw1.tjflg + ",tpflg=" + cw1.tpflg, "", qu, savdptid);
            }
            else
            {
                d(wmsno, WMSConst.BLL_TYPE_UPBLL, "rnd=" + iRnd + ",idx=1,"
                                        + ",barcode=" + cw1.barcode + ",isvld=" + cw1.isvld + ",kcflg=" + cw1.kcflg + ",tjflg=" + cw1.tjflg + ",tpflg=" + cw1.tpflg, "", qu, savdptid);
            }
            
            // 未找到仓位
            if (cw == null) { return null; }
            cw.tpflg = GetY();            

            try
            {
                WmsDc.SubmitChanges();

                d(wmsno, WMSConst.BLL_TYPE_UPBLL, "rnd=" + iRnd + ",idx=2,"
                                        + ",barcode=" + cw.barcode + ",isvld=" + cw.isvld + ",kcflg=" + cw.kcflg + ",tjflg=" + cw.tjflg + ",tpflg=" + cw.tpflg, "", qu, savdptid);

                return cw.barcode;
            }
            catch (Exception ex)
            {
                return null;
            }            
        }

        /// <summary>
        /// 通过wmsno和tpcode得到商品信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="tpcode"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货查询, pwrdes = "收货查询")]
        public ActionResult GetGdsByWmsnoAndTpcode(String wmsno, String tpcode)
        {
            wms_blltp[] tps = GetTp(wmsno)
                              .Where(e => e.tpcode == tpcode)
                              .ToArray();
            if (tps == null || tps.Length == 0)
            {
                return RInfo("该托盘没有商品信息");
            }
            var gdsinfo = (from e in WmsDc.gds
                          join e1 in WmsDc.v_wms_pkg on e.gdsid equals e1.gdsid
                          where tps.Select(et => et.gdsid).Contains(e.gdsid)
                          select new
                          {
                              e.gdsid,
                              e.gdsdes,
                              e1.pkgdes,
                              e1.cnvrto
                          }).ToArray();
            var retInfo = (from e in tps
                          join e1 in gdsinfo on e.gdsid equals e1.gdsid
                          select new
                          {
                              e.gdsid,
                              e.gdstype,
                              e.qty,
                              e1.gdsdes,
                              e1.pkgdes,
                              e1.cnvrto,
                              e.rcdidxtp,
                              e.rcdidx,
                              pkg03 = GetPkgStr(e.qty, e1.cnvrto, e1.pkgdes),
                              pkg03pre = GetPkgStr(e.qty, e1.cnvrto, e1.pkgdes)
                          }).ToArray();
            return RSucc("成功", retInfo);
        }

        /// <summary>
        /// 确认商品收货 
        /// </summary>
        /// <param name="wmsno">收货单单号</param>
        /// <param name="gdsid">商品编码</param>
        /// <param name="gdstype">单据商品序号</param>
        /// <param name="qtys">实收数量</param>
        /// <param name="tpcode">托盘码</param>
        /// <param name="pkgid">包装ID</param>
        /// <returns>wms_blldtl, wms_blltp</returns>
        /// 如果 qtys,tpcodes,pkgids 不传参，就修改实收数量为0
        [PWR(Pwrid = WMSConst.WMS_BACK_收货制单, pwrdes = "收货制单")]
        public ActionResult AdGdsByTp(String wmsno, String gdsid, String gdstype, String qty, String tpcode, String pkgid)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            if (gdsid == null)
            {
                return RInfo("货号无效！");
            }

            double iqty = 0;
            if (String.IsNullOrEmpty(qty) || !double.TryParse(qty, out iqty))
            {
                return RInfo("解析" + gdsid + "的数量" + qty + "出错");
            }

            //using (TransactionScope scop = new TransactionScope())
            //{

            var qrymst = from e in WmsDc.wms_bllmst
                            where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                            select e;
            wms_bllmst mst = qrymst.FirstOrDefault();
            if (mst == null)
            {
                return RInfo("未找到收货单");
            }
            //1.3.盘点是否是同一个操作人
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo("收货审核人" + LoginInfo.Usrid + ",和制单人" + mst.mkr + ",不是同一个人");
            }
            //1.判断收货单是否已经审核，如果审核则退出
            if (mst.chkflg == GetY())
            {
                return RInfo("收货单已经审核，不允许修改");
            }
                 
            // done: 判断增加的数量是否已经超过了应收的数量            
            wms_blltp[] blltps = GetTp(wmsno);
            // done: 判断是否有未上架的相同的托盘吗
            if ((from e in WmsDc.wms_cangdtl
                 join e1 in WmsDc.wms_cang
                 on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                 where e.tpcode == tpcode.Trim() && e.bokflg == GetN()
                 && e1.bllid == WMSConst.BLL_TYPE_UPBLL && e1.mkedat.CompareTo(GetCurrentDay())>=0
                 select 1).Any())
            {
                return RInfo("该托盘有商品尚未完成上架操作，不允许使用");
            }
            wms_blldtl[] blldtls = GetDtl(wmsno);
            double sumPreqty = blldtls.Where(e => e.gdsid.Trim() == gdsid.Trim()).Sum(e => e.preqty);
            double sumHasqty = blltps.Where(e => e.gdsid.Trim() == gdsid.Trim()).Sum(e => e.qty);
            if ((sumHasqty + iqty) > sumPreqty)
            {
                return RInfo("增加的数量已经超过应收的数量");
            }
            //判断该商品是否缺货
            wms_blldtl blldtl = blldtls.Where(e => e.gdsid.Trim() == gdsid.Trim() && e.bokflg == GetY() && e.qty == 0).FirstOrDefault();
            if (blldtl != null)
            {
                return RInfo("该商品缺货");
            }


            //2.增加托盘信息            
            Rm = InstAGdsTp(wmsno, gdsid, gdstype, tpcode, pkgid, iqty, mst);
            if (Rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return Json(Rm, JsonRequestBehavior.AllowGet);
            }

            try
            {
                //scop.Complete();
                return RSucc("成功", Rm.ResultObject);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
            }
            //}
        }

        /// <summary>
        /// 托盘删除
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="tpcode"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货制单, pwrdes = "收货制单")]
        public ActionResult DlGdsByTp(String wmsno, String tpcode)
        {
            //done: 查询出需要删除的托盘主表和明细
            var qrymst = from e in WmsDc.wms_bllmst
                         where e.bllid == WMSConst.BLL_TYPE_REVIECEBLL && e.wmsno == wmsno                         
                         select e;
            var qrydtltp = from e in WmsDc.wms_blltp
                           join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                           where e.bllid == WMSConst.BLL_TYPE_REVIECEBLL && e.wmsno == wmsno
                           && e.tpcode == tpcode.Trim() && dpts.Contains(e1.dptid)
                           && e.bokflg == GetN()
                           select e;
            wms_bllmst mst = qrymst.FirstOrDefault();
            wms_blltp[] dtltp = qrydtltp.ToArray();
            if (mst == null)
            {
                return RNoData("未找到收货单");
            }
            if (!qus.Contains(mst.qu) || !savdpts.Contains(mst.savdptid.Trim()))
            {
                return RInfo("你没有该单据的数据权限");
            }
            if (dtltp == null || dtltp.Length == 0)
            {
                return RNoData("未找到明细");
            }
            //done: 判断是否托盘已经确认，确认不允许删除
            //Rm = TpHasUnBoked(wmsno, tpcode);
            if (Rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return Json(Rm);
            }
            //done: 判断收货单据是否已经审核，已经审核不能删除
            if (mst.chkflg == GetY())
            {
                return RInfo("收货单据已经审核");
            }
            //done: 删除托盘
            try
            {
                WmsDc.wms_blltp.DeleteAllOnSubmit(dtltp);
                WmsDc.SubmitChanges();
                return RSucc("成功", null);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
            }

        }

        /// <summary>
        /// 得到托盘上商品的信息（未审核前）
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="tpcode"></param>
        /// <param name="bokdat"></param>
        /// <returns></returns>
        public ActionResult GetGdsByTpcode(String wmsno, String tpcode, String bokdat)
        {
            var qryDtlTp = from e in WmsDc.wms_bllmst
                           join e1 in WmsDc.wms_blltp on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                           join e2 in WmsDc.gds on e1.gdsid equals e2.gdsid
                           join e3 in WmsDc.v_wms_pkg on e2.gdsid equals e3.gdsid
                           where e1.wmsno == wmsno && e1.bllid == WMSConst.BLL_TYPE_REVIECEBLL //&& e1.gdsid == gdsid
                              // && e1.bokflg == GetN() && e1.rcdidxtp >= 1000
                           && e1.tpcode == tpcode
                           orderby e1.rcdidxtp descending
                           select new
                           {
                               e1.gdsid,e1.qty,e1.gdstype,e2.gdsdes,e2.spc,e2.bsepkg,
                               e1.tpcode,e1.rcdidx,e1.rcdidxtp, e1.bokdat, e1.bokflg,
                               pkg03 = GetPkgStr(e1.qty, e3.cnvrto, e3.pkgdes),                               
                               pkg03pre = GetPkgStr(e1.qty, e3.cnvrto, e3.pkgdes)
                           };
            if (!string.IsNullOrEmpty(bokdat.Trim()))
            {
                qryDtlTp = qryDtlTp.Where(e => e.bokdat.Trim() == bokdat.Trim() && e.bokflg==GetY());
            }
            else
            {
                qryDtlTp = qryDtlTp.Where(e => e.bokflg == GetN());
            }
            var arrqryDtlTp = qryDtlTp.ToArray();
            if (arrqryDtlTp.Length == 0)
            {
                return RNoData("未找到托盘上有数据");
            }
            return RSucc("成功", arrqryDtlTp);
        }

        /// <summary>
        /// 插入一条托盘记录
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="gdstype"></param>
        /// <param name="tpcode"></param>
        /// <param name="pkgid"></param>
        /// <param name="iqty"></param>
        /// <param name="mst"></param>
        /// <returns></returns>
        private ResultMessage InstAGdsTp(String wmsno, String gdsid, String gdstype, String tpcode, String pkgid, double iqty, wms_bllmst mst)
        {
            //判断该托盘在该单据中已经审核
            //Rm = TpHasUnBoked(wmsno, tpcode);
            if (Rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
            {
                return Rm;
            }

            var qryDtl = from e in WmsDc.wms_bllmst
                         join e1 in WmsDc.wms_blldtl on new { e.wmsno, e.bllid, gdsid = gdsid } equals new { e1.wmsno, e1.bllid, e1.gdsid }
                         where e1.wmsno == wmsno && e1.bllid == WMSConst.BLL_TYPE_REVIECEBLL && e1.gdsid == gdsid
                         orderby e1.rcdidx
                         select e1;
            var qryDtlTp = from e in WmsDc.wms_bllmst
                           join e1 in WmsDc.wms_blltp on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                           where e1.wmsno == wmsno && e1.bllid == WMSConst.BLL_TYPE_REVIECEBLL && e1.gdsid == gdsid
                           /*&& e1.bokflg == GetN()*/ && e1.rcdidxtp >= 1000
                           orderby e1.rcdidxtp descending
                           select e1;
            wms_blltp idxDtlTp = qryDtlTp.FirstOrDefault();
            wms_blldtl idxDtp = qryDtl.FirstOrDefault();
            if (idxDtp == null)
            {
                return RRInfo("收货单" + wmsno + "未找到商品" + gdsid + "信息");
            }
            int iIdx = 0;
            iIdx = idxDtp.rcdidx;
            int iTpIdx = 1000;
            if (idxDtlTp != null)
            {
                iTpIdx = idxDtlTp.rcdidxtp + 1;
            }
            //同一个商品同一个托盘 同一个类型 不应该重复
            if (qryDtlTp.Where(e => e.gdsid == gdsid && e.gdstype == gdstype && e.tpcode == tpcode).Any())
            {
                return RRInfo("同一个商品同一个托盘 同一个类型 不应该重复,请核对");
            }
            wms_blltp newdtltp = new wms_blltp();
            newdtltp.wmsno = mst.wmsno;
            newdtltp.bllid = mst.bllid;
            newdtltp.qu = mst.qu;
            newdtltp.rcdidx = iIdx;
            newdtltp.rcdidxtp = iTpIdx;
            newdtltp.barcode = "";
            newdtltp.gdsid = gdsid;
            newdtltp.pkgid = pkgid;
            newdtltp.qty = iqty;
            newdtltp.tpcode = tpcode;
            newdtltp.savdptid = mst.savdptid;
            newdtltp.gdstype = gdstype;
            newdtltp.bkr = "";
            newdtltp.bokflg = GetN();
            newdtltp.bokdat = "";
            WmsDc.wms_blltp.InsertOnSubmit(newdtltp);
            WmsDc.SubmitChanges();
            return RRSucc("成功", newdtltp);
        }

        private ResultMessage TpHasUnBoked(String wmsno, String tpcode)
        {
            var qryDtlTp1 = from e in WmsDc.wms_bllmst
                            join e1 in WmsDc.wms_blltp on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                            where e1.wmsno == wmsno && e1.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                            && e1.tpcode == tpcode
                            orderby e1.bokflg descending
                            select e1;
            wms_blltp[] tp = qryDtlTp1.ToArray();            
            if (tp.Length > 0 && tp[0].bokflg == GetY())
            {
                return RRInfo("托盘" + tpcode + "已经确定，不能重复确定");
            }
            return RRSucc("尚未确认", null);
        }


        /// <summary>
        /// 修改收货单商品类型
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="gdstype"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_收货审核, pwrdes = "收货审核")]
        public ActionResult MdfyRecievGdsType(String wmsno, String gdsid, String gdstype, String newgdstype)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            if (gdsid == null)
            {
                return RInfo("货号无效！");
            }

            /*var qry = from e in WmsDc.wms_blldtl
                      where e.wmsno == wmsno
                      && e.gdsid == gdsid
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {                
                return RNoData("未找到商品");                
            }
            wms_blldtl dtl = arrqry[0];*/
            var qry = from e in WmsDc.wms_blltp
                      where e.wmsno == wmsno
                      && e.bllid == WMSConst.BLL_TYPE_REVIECEBLL
                      && e.gdsid == gdsid
                      && e.gdstype == gdstype
                      select e;
            var arrqry = qry.ToArray();
            wms_blldtl[] dtls = GetDtl(wmsno).Where(e => e.gdsid == gdsid && e.gdstype == gdstype).ToArray();
            if (arrqry.Length <= 0 || dtls.Length<=0)
            {
                return RNoData("未找到商品");
            }
            wms_blltp tp = arrqry[0];
            tp.gdstype = gdstype;
            wms_blldtl dtl = dtls.FirstOrDefault();
            dtl.gdstype = gdstype;
            try
            {
                WmsDc.SubmitChanges();

                return RSucc("成功", arrqry[0]);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
            }
        }
    }
}
