using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;

namespace WMS.Controllers
{
    /// <summary>
    /// 仓位调整
    /// </summary>
    public class AdjustBarcodeController : SsnController
    {
        class _AdjustBarcodeParam
        {
            public String oldbarcode { get; set; }
            public String gdsid { get; set; }
            public double qty { get; set; }
            public String newbarcode { get; set; }
        }
        /// <summary>
        /// 仓位调整
        /// </summary>
        public AdjustBarcodeController()
        {
            Mdlid = "AdjustBarcode";
            Mdldes = "仓位调整";
        }

        private ActionResult _MakeParam(String wmsno, String oldbarcodes, String gdsids, String gdstypes, String qtys)
        {            
            String[] oldbarcode = oldbarcodes.Split(',');
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] gdstype = gdstypes.Split(',');
            //String[] newsbarcode = newbarcodes.Split(',');
            List<wms_blldtl> lstDtl = new List<wms_blldtl>();
            if ((oldbarcode.Length != gdsid.Length)
                && (oldbarcode.Length != qty.Length))
            {                
                return RInfo("参数数量不一致");
            }
            int i=0;
            int i1 = 1;

            string qu = null;
            foreach (String s in oldbarcode)
            {
                //判断是否是一个分区
                if (!string.IsNullOrEmpty(qu) && qu!=s.Substring(0,2))
                {
                    return RInfo("商品不在同一个分区");
                }
                if (string.IsNullOrEmpty(qu))
                {
                    qu = s.Substring(0, 2);
                }                
                //判断仓位是否存在
                if (!IsExistBarcode(s))
                {
                    return RInfo("仓位" + s + "不存在");
                }
                if (!String.IsNullOrEmpty(s))
                {
                    wms_blldtl dtl = new wms_blldtl();
                    dtl.wmsno = wmsno;
                    dtl.bllid = WMSConst.BLL_TYPE_ADJCANG;
                    dtl.rcdidx = i+1;                    
                    dtl.barcode = s;
                    dtl.gdsid = gdsid[i];
                    dtl.pkgid = "01";
                    double fQty = 0;
                    if (!double.TryParse(qty[i], out fQty))
                    {
                        return RInfo(gdsid[i] + "数量：" + qty[i] + "格式化出错");
                    }
                    fQty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
                    dtl.qty = fQty;
                    dtl.preqty = fQty;                                        
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
                    dtl.prvid = "";
                    dtl.bkr = LoginInfo.Usrid;
                    dtl.bokflg = GetN();
                    dtl.bokdat = GetCurrentDate();
                    dtl.brief = "";

                    lstDtl.Add(dtl);
                    i++;
                }
            }

            return RSucc("成功", lstDtl.ToArray());
        }


        /// <summary>
        /// 查询可以合并的仓位
        /// </summary>
        /// <param name="savdptid"></param>
        /// <param name="qu"></param>
        /// <param name="mkedat"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public ActionResult GetCanUnionBarcode(string savdptid, string qu, string mkedat, int qty)
        {
 
            String sql = @"declare @savdptid varchar(6),@qu varchar(6),@mkedat varchar(14),@qty int

                            select @savdptid='" + savdptid + @"'
                            select @qu='" + qu + @"'
                            select @mkedat='" + mkedat + @"'
                            select @qty=" + qty + @"

                            select b.barcode,b.zheng,b.ling,b.zong from wms_cangwei a

                            join (select barcode,sum(zheng) zheng,sum(ling) ling,sum(zong) zong
                            from
                            (select barcode,gdsid,
                            floor(sum(qty)/(select max(cnvrto)  from pkg where pkg.gdsid=wms_cwgdsbs.gdsid)) zheng,
                            convert(integer,sum(qty))% convert(integer,(select max(cnvrto)  from pkg where pkg.gdsid=wms_cwgdsbs.gdsid )) ling,
                            sum(qty) zong
                            from wms_cwgdsbs where savdptid=@savdptid and qu=@qu and qty>0 group by barcode,gdsid) a
                            group by barcode) b on a.barcode=b.barcode

                            where a.savdptid=@savdptid and a.qu=@qu and a.tjflg='y' and a.barcode not in
                            (
                            select barcode from wms_cangdtl,wms_cang where wms_cang.wmsno=wms_cangdtl.wmsno and wms_cang.bllid=wms_cangdtl.bllid
				                            and wms_cang.savdptid=@savdptid and wms_cang.qu=@qu and wms_cang.bllid='103' and wms_cang.mkedat>='" + mkedat + @"'
                            union all
                            select barcode from wms_cangdtl_115,wms_cang_115 where wms_cang_115.wmsno=wms_cangdtl_115.wmsno and wms_cang_115.bllid=wms_cangdtl_115.bllid
				                            and wms_cang_115.savdptid=@savdptid and wms_cang_115.qu=@qu and wms_cang_115.bllid='115' and wms_cang_115.mkedat>='" + mkedat + @"'
                            union all
                            select barcode from wms_bllmst,wms_blldtl where wms_bllmst.wmsno=wms_blldtl.wmsno and wms_bllmst.bllid=wms_blldtl.bllid
				                            and wms_bllmst.savdptid=@savdptid and wms_bllmst.qu=@qu and wms_bllmst.bllid='108' and wms_bllmst.mkedat>='" + mkedat + @"'
                            )

                            and b.zheng<=@qty
                            order by b.barcode";
            IEnumerable<GetCanUnionBarcodeRet> obj = WmsDc.ExecuteQuery<GetCanUnionBarcodeRet>(sql);
            
            return RSucc("成功", obj);
        }

        /// <summary>
        /// 仓位调整单审核
        /// </summary>
        /// <param name="wmsno">仓位调整单单号</param>
        /// <returns></returns>
        /// done: 审核仓位调整单还原
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整审核, pwrdes = "仓位调整审核")]
        public ActionResult AdtAdjBll(String wmsno)
        {
            /*
             * 仓位调整审核步骤
             * 1.检查是否审核过
             * 2.检查调出调入是否匹配
             * 3.判断是否有从退货区调入商品区，或者从商品区调入退货区，如果有，要生成经销调拨单
             * 4.审核主单，修改主单标记
             * 5.增加帐表库存
             * 6.减少帐表库存
             * 7.插入经销调拨单主单和明细
             * 8.插入sftdtl表
             */
            wms_bllmst mst = GetAdjMst(wmsno);
            //正在生成拣货单，请稍候重试            
            if (DoingRetrieve(LoginInfo.DefStoreid, mst.qu))
            {
                return RInfo("正在生成拣货单，请稍候重试");
            }

            wms_blldtl[] dtls = GetAdjDtls(wmsno);
            wms_blltp[] tps = (from e in WmsDc.wms_blltp
                               where e.wmsno == wmsno.Trim()
                               && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                               select e).ToArray();            
            var dpts = (from e in WmsDc.gds
                          join e1 in WmsDc.dpt on e.dptid equals e1.dptid
                              where dtls.Select(ee=>ee.gdsid.Trim()).Contains(e.gdsid)
                              select new{
                                  gdsid = e.gdsid.Trim(),
                                  gdsdes = e.gdsdes.Trim(),
                                  salprc = e.salprc,
                                  dptid = e.dptid.Trim(),
                                  dptdes = e1.dptdes.Trim()
                              }
                              ).ToArray();
            var deps = (from e in WmsDc.bizdep
                        where savdpts.Contains(e.savdptid)
                        && dpts.Select(e1 => e1.dptid).Contains(e.dptid)
                        select e).ToArray();
            var bcds = (from e in WmsDc.bcd
                        where dtls.Select(ee=>ee.gdsid.Trim()).Contains(e.gdsid)
                        group e by e.gdsid into g
                        select new{
                            gdsid = g.Key.Trim(),
                            bcd  = g.Max(e=>e.bcd1).Trim()
                        }).ToArray();

            // 判断单据是否找到
            if (mst == null)
            {
                return RNoData("未找到该单据");
            }
            //判断是否有主单权限
            if (qus.Contains(mst.qu) && savdpts.Contains(mst.savdptid))
            {
                return RInfo("您无权审核该单据");
            }
            //判断该单据是否审核
            if (mst.chkflg == GetY())
            {
                return RInfo("该单据已经审核，请不要重复审核");
            }
            //判断是否有明细数据权限
            if (dtls.Length == 0)
            {
                return RNoData("未找到明细信息");
            }            
            // done 检查调出调入是否匹配           
            //如果明细单不是所有商品的调整数量都为0，审核的时候TP表中的数量必须与明细中的数量一致
            if (/*!(dtls.Count(e => e.qty == 0) == dtls.Count()) && */dtls.Sum(e => e.qty) != tps.Sum(e => e.qty))
            {
                return RInfo("调出调入数量不一致");
            }            

            //审核主单，修改主单标记
            mst.chkflg = GetY();
            mst.chkdat = GetCurrentDate();
            mst.ckr = LoginInfo.Usrid;            


            //增加帐表库存
            #region 增加帐表库存
            var tpsGrp = from e in tps
                             group e by new { e.barcode, e.gdsid, e.gdstype, e.qu } into g
                             select new
                             {
                                 g.Key.barcode,
                                 g.Key.gdsid,
                                 g.Key.gdstype,
                                 g.Key.qu,
                                 bcd = WmsDc.bcd.Where(e=>e.gdsid.Trim()== g.Key.gdsid.Trim()).Select(e=>e.bcd1).Max(), // WmsDc.bcd.Where(ee=>ee.gdsid==g.Key.gdsid.Trim()).Select(e=>e.bcd1).Max(),
                                 qty = g.Sum(e => e.qty)
                             };
            
            foreach (var ag in tpsGrp)
            {
                wms_cwgdsbs gdsbs = new wms_cwgdsbs();
                gdsbs.barcode = ag.barcode;
                gdsbs.bcd = ag.bcd;
                gdsbs.bthno = "";
                gdsbs.gdsid = ag.gdsid;
                gdsbs.gdstype = ag.gdstype;
                gdsbs.prvid = "";
                gdsbs.qty = ag.qty;
                gdsbs.qu = ag.qu;
                gdsbs.savdptid = GetSavdptidByQu(ag.qu);
                gdsbs.vlddat = "";
                //如果没有就增加库存
                wms_cwgdsbs egdsbs = WmsDc.wms_cwgdsbs.Where(e => e.barcode == gdsbs.barcode && e.gdsid == gdsbs.gdsid
                    && e.gdstype == gdsbs.gdstype && e.savdptid == gdsbs.savdptid && e.qu == gdsbs.qu).Select(e => e).FirstOrDefault();
                if (egdsbs==null)
                {
                    WmsDc.wms_cwgdsbs.InsertOnSubmit(gdsbs);                    
                    //return RInfo("帐表中未找到有待调整仓位商品" + gdsbs.gdsid + ",类型为" + gdsbs.gdstype + "的商品");
                }else{
                    egdsbs.qty += gdsbs.qty;
                }
                //WmsDc.SubmitChanges();
            }
            #endregion 增加帐表库存

            //减少帐表库存
            #region 减少帐表库存
            var dtlsGrp = from e in dtls
                             group e by new { e.barcode, e.gdsid, e.gdstype } into g
                             select new
                             {
                                 g.Key.barcode,
                                 g.Key.gdsid,
                                 g.Key.gdstype,
                                 bcd = WmsDc.bcd.Where(ee => ee.gdsid == ee.gdsid).Select(e => e.bcd1).Max(),
                                 qu = mst.qu,
                                 qty = g.Sum(e => e.qty)
                             };
            foreach (var ag in dtlsGrp)
            {
                wms_cwgdsbs gdsbs = new wms_cwgdsbs();
                gdsbs.barcode = ag.barcode.Trim();
                gdsbs.bcd = ag.bcd.Trim();
                gdsbs.bthno = "";
                gdsbs.gdsid = ag.gdsid.Trim();
                gdsbs.gdstype = ag.gdstype.Trim();
                gdsbs.prvid = "";
                gdsbs.qty = ag.qty;
                gdsbs.qu = mst.qu.Trim();
                gdsbs.savdptid = mst.savdptid.Trim();
                gdsbs.vlddat = "";
                //如果没有就增加库存
                wms_cwgdsbs egdsbs = WmsDc.wms_cwgdsbs.Where(e => e.barcode == gdsbs.barcode && e.gdsid == gdsbs.gdsid
                    && e.gdstype == gdsbs.gdstype && e.savdptid == gdsbs.savdptid && e.qu == gdsbs.qu).Select(e => e).FirstOrDefault();
                if (egdsbs == null)
                {
                    return RInfo("帐表中未找到有待调整仓位商品"+gdsbs.gdsid+",类型为"+gdsbs.gdstype+"的商品");
                }
                else
                {
                    egdsbs.qty -= gdsbs.qty;
                }
                //WmsDc.SubmitChanges();
            }
            #endregion 减少帐表库存

            //判断是否有从退货区调入商品区，或者从商品区调入退货区，如果有，要生成经销调拨单
            //插入经销调拨单主单和明细(bllid="112")
            #region 插入经销调拨单主单和明细(bllid="112")
            //得到tps idx分组的查询
            var tpsIdxGrp = (from e in tps
                             join e1 in bcds on new { gdsid = e.gdsid.Trim() } equals new { gdsid = e1.gdsid }
                             join e2 in dpts on new { gdsid = e.gdsid.Trim() } equals new { gdsid = e2.gdsid }
                             group e by new { e.barcode, e.gdsid, e.gdstype, e.qu, e.rcdidx, e.savdptid, e1.bcd, e2.dptdes, e2.dptid } into g
                             select new
                             {
                                 g.Key.barcode,
                                 gdsid = g.Key.gdsid.Trim(),
                                 g.Key.gdstype,
                                 g.Key.rcdidx,
                                 g.Key.qu,
                                 g.Key.bcd,
                                 g.Key.dptdes,
                                 g.Key.dptid,
                                 g.Key.savdptid,
                                 qty = g.Sum(e => e.qty)
                             }).ToArray();
            //得到dtls idx分组的查询
            var dtlsIdxGrp = (from e in dtls
                              join e1 in bcds on new { gdsid = e.gdsid.Trim() } equals new { gdsid = e1.gdsid }
                              join e2 in dpts on new { gdsid = e.gdsid.Trim() } equals new { gdsid = e2.gdsid }
                             group e by new { e.barcode, e.gdsid, e.gdstype, e.rcdidx, e1.bcd, e2.dptdes, e2.dptid } into g
                          select new
                          {
                              g.Key.barcode,
                              gdsid = g.Key.gdsid.Trim(),
                              g.Key.gdstype,
                              g.Key.rcdidx,                              
                              qu = mst.qu,
                              g.Key.bcd,
                              g.Key.dptdes,
                              g.Key.dptid,
                              savdptid=mst.savdptid,
                              qty = g.Sum(e => e.qty)
                          }).ToArray();
            // 通过idx关联dtls和tps分区不一致的商品
            var lnkDtlsTps = (from e in dtlsIdxGrp
                             join e2 in tpsIdxGrp on e.rcdidx equals e2.rcdidx
                             into joinTps from e1 in joinTps.DefaultIfEmpty()                             
                             where e.qu != e1.qu
                             orderby e1.savdptid, e1.dptid, e1.qu
                             select new
                             {
                                 oldbarcode = e.barcode,
                                 e.bcd,
                                 e.gdsid,
                                 e.gdstype,
                                 oldqty = e.qty,
                                 oldqu = e.qu,
                                 oldsavdptid=e.savdptid,
                                 olddptid = e.dptid,
                                 olddptdes = e.dptdes,
                                 e.dptid,
                                 e.dptdes,
                                 e.rcdidx,
                                 newsavdptid=e1.savdptid,
                                 newdptid = e1.dptid,
                                 newdptdes = e1.dptdes,
                                 newbarcode = e1.barcode,
                                 newqty = e1.qty,
                                 newqu = e1.qu                                 
                             }).ToArray();
            // 如果有调出调入分区不一致的单据就生产调拨单
            if (lnkDtlsTps.Length > 0)
            {
                // 调拨单单号            
                String stkinno = null;
                String adptid = null;
                String fscprdid = GetCurrentFscprdid();
                String bllid = "112";
                stkin sin = null;
                int i = 1;
                
                // 对这些分区不一致的商品，生产经销调拨单（单据类型：112）
                foreach (var dp in lnkDtlsTps)
                {                    
                    //检查调出调入数量是否一致
                    /*if (dp.oldqty != dp.newqty)
                    {
                        return RInfo("商品" + dp.gdsid.Trim() + "调出数量" + dp.oldqty + "与调入数量" + dp.newqty + "不一致，请核对");
                    }*/
                    //判断aqu分区是否有null,为null就为改去分配单号
                    if (adptid == null || adptid != dp.newdptid)
                    {
                        // 序号初始化
                        i = 1;
                        // 得到单号
                        adptid = dp.newdptid;
                        WmsDc.get_bllno(fscprdid, "01", "002", ref stkinno);
                        //stkinno = WmsDc.get_wms_sy_bllno(fscprdid, dp.newdptid, dp.newsavdptid.Trim()).FirstOrDefault().Column1;
                        // 生成主单据
                        #region 生成主单
                        sin = new stkin();
                        sin.stkinno = stkinno;
                        sin.hndbllno = mst.wmsno;
                        sin.bllid = bllid;
                        sin.dptid = dp.newdptid;
                        sin.savdptid = dp.newsavdptid;
                        sin.depid = WmsDc.bizdep.Where(e => e.savdptid.Trim() == dp.newsavdptid.Trim()
                            && e.dptid.Trim() == dp.newdptid.Trim()).Select(e => e.depid.Trim()).FirstOrDefault();
                        sin.prvid = "";
                        sin.mkr = mst.ckr;
                        sin.mkedat = GetCurrentDay();
                        sin.ckr = "";
                        sin.chkflg = GetN();
                        sin.chkdat = "";
                        sin.bkr = "";
                        sin.bokflg = GetN();
                        sin.bokdat = "";
                        sin.opr = mst.mkr;
                        sin.rcv = "";
                        sin.stkindat = "";
                        sin.brief = "WMS系统调仓导入";
                        sin.lnkodrno = "";
                        sin.lnkivcno = "";
                        sin.mctortrust = '0';
                        sin.stktpid = "";
                        sin.taxflg = null;
                        String branchid = GetBranchid(dp.newsavdptid);
                        sin.branchid = branchid;
                        sin.sft_branchid = branchid;
                        sin.drtdpt = null;
                        sin.outzdflg = GetN();
                        sin.inzdflg = GetN();
                        sin.outflg = GetN();
                        sin.inflg = GetN();
                        sin.outwmsbllid = "";
                        sin.inwmsbllid = "";
                        sin.outwmsno = "";
                        sin.inwmsno = "";
                        sin.indat = "";
                        sin.outdat = "";
                        sin.inzddat = "";
                        sin.outzddat = "";
                        WmsDc.stkin.InsertOnSubmit(sin);
                        #endregion 生成主单

                        //插入sftdtl表
                        #region 插入sftdtl表
                        sftdtl sdtl = new sftdtl();
                        sdtl.sft_depout = WmsDc.bizdep.Where(e => e.dptid.Trim() == dp.olddptid.Trim() && e.savdptid.Trim() == dp.oldsavdptid).Select(e => e.depid.Trim()).FirstOrDefault();
                        sdtl.sft_dptout = dp.olddptid;
                        sdtl.sft_sdtout = dp.oldsavdptid;
                        sdtl.stkinno = sin.stkinno;
                        WmsDc.sftdtl.InsertOnSubmit(sdtl);
                        #endregion 插入sftdtl表
                    }
                    //插入明细单据
                    #region 插入明细单据
                    // 单号不为空就插入明细
                    if (stkinno != null && sin!=null)
                    {
                        stkindtl sindtl = new stkindtl();
                        sindtl.stkinno = stkinno;
                        sindtl.rcdidx = i;
                        sindtl.depid = sin.depid;
                        sindtl.outdepid = WmsDc.bizdep.Where(e=>e.dptid.Trim()==dp.olddptid.Trim()
                                            &&e.savdptid.Trim()==dp.oldsavdptid.Trim()).Select(e=>e.depid.Trim()).FirstOrDefault();
                        f_get_bthprcResult bthprc = WmsDc.f_get_bthprc(fscprdid, sindtl.outdepid, dp.gdsid).First();

                        sindtl.gdsid = dp.gdsid;
                        sindtl.pkgid = "01";
                        sindtl.duepkgqty = dp.newqty;
                        sindtl.dueqty = dp.newqty;
                        sindtl.pkgqty = dp.newqty;                                               
                        sindtl.pkgprc = bthprc.prc;  // 计算批次价
                        sindtl.pkgtaxprc = bthprc.taxprc;
                        sindtl.qty = dp.newqty;
                        sindtl.prc = sindtl.pkgprc;
                        sindtl.amt = Math.Round(sindtl.qty * sindtl.prc.Value, 2, MidpointRounding.AwayFromZero);
                        sindtl.taxrto = bthprc.taxrto;
                        //sindtl.taxamt = Math.Round( (bthprc.taxprc.Value * sindtl.qty - bthprc.prc.Value * sindtl.qty) , 2, MidpointRounding.AwayFromZero); 
                        ///算法就是这样的
                        ///周胖胖 2016/5/23 14:54:34
                        ///反正最终出来的结果就是
                        ///taxamt=prc*taxrto*qty
                        sindtl.taxamt = Math.Round(bthprc.prc.Value * bthprc.taxrto.Value * sindtl.qty, 2, MidpointRounding.AwayFromZero);
                        sindtl.taxprc = bthprc.taxprc;
                        sindtl.patamt = Math.Round(bthprc.taxprc.Value * sindtl.qty, 2, MidpointRounding.AwayFromZero);
                        sindtl.salprc = dpts.Where(e=>e.gdsid.Trim()==dp.gdsid.Trim()).Select(e=>e.salprc).FirstOrDefault() ;
                        sindtl.salprcamt = sindtl.salprc * dp.newqty;
                        sindtl.dlvprc = null;
                        sindtl.dlvamt = null;
                        sindtl.saltaxrto = bthprc.taxrto;
                        sindtl.orisalprc = 0;
                        sindtl.prvid = bthprc.prvid;
                        sindtl.bthprc = bthprc.prc;
                        sindtl.stllnkno = null;
                        sindtl.stllnkidx = null;
                        sindtl.bthno = null;
                        sindtl.vlddat = null;
                        sindtl.bcd = dp.bcd;
                        sindtl.brfdtl = null;
                        sindtl.stincstprc = null;
                        sindtl.stincstamt = null;
                        sindtl.mctortrust = '0';
                        sindtl.taxflg = 'y';
                        sindtl.ctno = null;
                        sindtl.arcid = null;
                        sindtl.bthamt = null;
                        sindtl.bzflg = 'n';
                        sindtl.bzr = "";
                        sindtl.bzdat = "";
                        WmsDc.stkindtl.InsertOnSubmit(sindtl);
                    }
                    #endregion 插入明细单据

                    i++;
                }
                
            }
            #endregion 插入经销调拨单主单和明细(bllid="112")

            WmsDc.SubmitChanges();
            return RSucc("成功", null);
        }

        private wms_blldtl[] GetAdjDtls(String wmsno)
        {
            var qry = from e in WmsDc.wms_blldtl
                      where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                      select e;
            wms_blldtl[] dtls = qry.ToArray();
            return dtls;
        }

        private wms_bllmst GetAdjMst(String wmsno)
        {
            var qry = from e in WmsDc.wms_bllmst
                      where e.wmsno == wmsno.Trim()
                      && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                      select e;
            wms_bllmst mst = qry.FirstOrDefault();
            return mst;
        }

        /// <summary>
        /// 仓位调整单制单
        /// </summary>
        /// <param name="oldbarcodes">原仓位</param>
        /// <param name="gdsids">调整货号</param>
        /// <param name="qtys">调整数量</param>
        /// <param name="newbarcodes">调整到仓位</param>
        /// <returns></returns>
        [PWR(Pwrid=WMSConst.WMS_BACK_仓位调整制单,pwrdes="仓位调整制单")]
        public ActionResult MkAdjBll(String oldbarcodes, String gdsids, String gdstypes, String qtys)
        {
            String[] barcode = oldbarcodes.Split(',');
            String qu = barcode[0].Substring(0, 2);
            String savdptid = GetSavdptidByQu(qu);
            //正在生成拣货单，请稍候重试
            string quRetrv = qu;
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo("正在生成拣货单，请稍候重试");
            }

            return MakeNewBllNo(savdptid, WMSConst.BLL_TYPE_ADJCANG, (bllno) =>
            {
                //检查并创建明细
                JsonResult jr = (JsonResult)_MakeParam(bllno, oldbarcodes, gdsids, gdstypes, qtys);
                ResultMessage rm = (ResultMessage)jr.Data;
                if (rm.ResultCode != ResultMessage.RESULTMESSAGE_SUCCESS)
                {
                    return rm;
                }
                //创建主表
                wms_blldtl[] dtls = (wms_blldtl[])rm.ResultObject;
                wms_bllmst mst = new wms_bllmst();
                mst.wmsno = bllno;
                mst.hndbllno = "";
                mst.bllid = WMSConst.BLL_TYPE_ADJCANG;
                mst.prvid = "";
                mst.savdptid = savdptid;
                mst.qu = dtls[0].barcode.Substring(0, 2); //GetQuByGdsid(dtls[0].gdsid, LoginInfo.DefStoreid); ;
                mst.tongdao = null;
                mst.huojia = null;
                mst.odrdat = "";// GetCurrentDay();
                mst.arvdat = "";// GetCurrentDay();
                mst.mkr = LoginInfo.Usrid;
                mst.mkedat = GetCurrentDate();                
                mst.ckr = "";
                mst.chkflg = GetN();
                mst.chkdat = "";
                mst.opr = LoginInfo.Usrid;
                mst.brief = "";
                mst.lnknewbllid = "";
                mst.lnknewno = "";
                mst.lnknewbrief = "";

                //如果是报损，判断是否有库存
                foreach (wms_blldtl d in dtls)
                {
                    //得到一个商品的库存数量
                    GdsInBarcode[] gb = GetAGdsQtyInBarcode(d.barcode, d.gdsid, d.gdstype);
                    double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
                    double ktqty = bqty;  //可调数量 = 库存数量
                    //如果 需调整数量 > 可调数量
                    if (d.qty > ktqty)
                    {
                        rm.ResultCode = ResultMessage.RESULTMESSAGE_NODATA;
                        rm.ResultDesc = "需要调整的数量" + d.qty + "大于可调整数量" + ktqty;
                        rm.ResultObject = null;
                        return rm;
                    }
                }  

                WmsDc.wms_bllmst.InsertOnSubmit(mst);
                WmsDc.wms_blldtl.InsertAllOnSubmit(dtls);

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
                    rm.ResultObject = null;
                    return rm;
                }
            });
        }

        /// <summary>
        /// 确认旧商品
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="rcdidx"></param>
        /// <returns></returns>
        public ActionResult BokOldBarcodeGdsid(String wmsno, String gdsid, int rcdidx)
        {
            //正在生成拣货单，请稍候重试
            string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo("正在生成拣货单，请稍候重试");
            }

            //检查是否存在单据
            var qrymst = from e in WmsDc.wms_bllmst
                         where e.mkr == LoginInfo.Usrid && 
                         e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_blldtl
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         && e.gdsid == gdsid && e.rcdidx == rcdidx
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            var qrytpdtl = from e in WmsDc.wms_blltp
                           where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                           && e.gdsid == gdsid && e.rcdidx == rcdidx
                           orderby e.rcdidxtp descending
                           select e;
            var arrqrytpdtl = qrytpdtl.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("未找到商品明细信息");
            }
            if (arrqrytpdtl.Count()>0)
            {
                return RInfo("调整单尚未确定，但已有目的仓位");
            }

            //检查单据是否已经审核
            wms_bllmst mst = arrqrymst[0];
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核不能修改");
            }
            //是否是同一个人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo("制单人" + mst.mkr + "和登录人" + LoginInfo.Usrid + "不同，不能操作");
            }
            foreach(wms_blldtl d in arrqrydtl){
                d.bokflg = GetY();
                d.bkr = LoginInfo.Usrid;
                d.bokdat = GetCurrentDate();
            }
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
        /// 调整新仓位
        /// </summary>
        /// <param name="wmsno">调整单单号</param>
        /// <param name="gdsid">调整的货号</param>
        /// <param name="rcdidx">调整的序号</param>
        /// <param name="newbarcode">新仓位编码</param>
        /// <param name="qty">调整数量</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整制单, pwrdes = "仓位调整制单")]
        public ActionResult AdAdjBarcode(String wmsno, String gdsid, int rcdidx, String newbarcode, double qty)
        {                       
            gdsid = GetGdsidByGdsidOrBcd(gdsid);

            //正在生成拣货单，请稍候重试
            string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo("正在生成拣货单，请稍候重试");
            }

            //判断仓位是否存在
            if (!IsExistBarcode(newbarcode))
            {
                return RInfo("仓位"+newbarcode+"不存在");
            }
            //检查是否存在单据
            var qrymst = from e in WmsDc.wms_bllmst
                         where e.mkr == LoginInfo.Usrid && 
                         e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select e;
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_blldtl
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         && e.gdsid == gdsid && e.rcdidx == rcdidx
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            var qrytpdtl = from e in WmsDc.wms_blltp
                           where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                           && e.gdsid == gdsid && e.rcdidx == rcdidx
                           orderby e.rcdidxtp descending
                           select e;
            var arrqrytpdtl = qrytpdtl.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("未找到商品明细信息");
            }
            // 判断是否是同一个区能不能互调
            wms_set[] sets = (from e in WmsDc.wms_set
                      join e1 in WmsDc.wms_cangwei
                        on new { qu = e.val1, savdptid = e.val3, e.setid }
                        equals new { e1.qu, e1.savdptid, setid = "006" }
                      where e1.barcode == arrqrydtl[0].barcode || e1.barcode == newbarcode
                      select e).ToArray();
            if (sets.Length == 2)
            {
                if(sets[0].brief.Trim()!=sets[1].brief.Trim()){
                    return RInfo("分区["+sets[0].val1+"，"+sets[1].val1+"]不能互调");
                }
            }
            else
            {
                return RInfo("分区设置不正确");
            }

            //检查单据是否已经审核
            wms_bllmst mst = arrqrymst[0];
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核不能修改");
            }
            //是否是同一个人制单
            if (!IsSameLogin(mst.mkr))
            {
                return RInfo("制单人" + mst.mkr + "和登录人" + LoginInfo.Usrid + "不同，不能操作");
            }
            int iTp = 1;
            if (arrqrytpdtl.Length > 0)
            {
                iTp = arrqrytpdtl[0].rcdidxtp + 1;
            }
            // done: 跨区调整单据时，判断是否有权限
            /*if (!HasPwrAdjDiffQu(mst.qu, newbarcode))
            {
                return RInfo("你没有跨区调整的权限");
            }*/

            // done: 需要调整的商品是否已经经过确认
            /*if (arrqrydtl[0].bokflg!=GetY())
            {
                return RInfo("商品尚未确认");
            }*/

            wms_blltp tp = new wms_blltp();
            tp.wmsno = wmsno;
            tp.bllid = WMSConst.BLL_TYPE_ADJCANG;
            wms_cangwei cw = GetCangweiByBarcode(newbarcode);
            tp.qu = cw.qu;
            tp.rcdidx = rcdidx;
            tp.rcdidxtp = iTp;
            tp.barcode = cw.barcode;
            tp.gdsid = gdsid;
            tp.pkgid = "01";
            tp.qty = Math.Round(qty, 4, MidpointRounding.AwayFromZero);
            tp.tpcode = "";
            tp.savdptid = cw.savdptid;
            tp.gdstype = arrqrydtl[0].gdstype;
            tp.bkr = LoginInfo.Usrid;
            tp.bokdat = GetCurrentDate();
            tp.bokflg = GetN();

            WmsDc.wms_blltp.InsertOnSubmit(tp);
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
        /// 跨区调整单据时，判断是否有权限
        /// </summary>
        /// <param name="oldqu"></param>
        /// <param name="newbarcode"></param>
        /// <returns></returns>
        private bool HasPwrAdjDiffQu(string oldqu, string newbarcode)
        {
            /*
             * a.只在跨商品区与退货区的时候增加校验
             * b.调出区为退货区，必须有退货确认权限才能确认调出以及增加新仓位
             * c.调出区为商品区，必须有正常确认权限才能确认调出以及增加新仓位
             */
            String newqu = GetQuByBarcode(newbarcode);
            if (newqu == null) { return false; }
            if (oldqu == newqu) { return true; }
            else //a.只在跨商品区与退货区的时候增加校验
            {
                // b.调出区为退货区，必须有退货确认权限才能确认调出以及增加新仓位
                // 如果是 退货区 调入 商品区/堆头区
                if (thqus.Contains(oldqu) && (spqus.Contains(newqu)|| dtqus.Contains(newqu) ) )
                {
                    if ((from e in LoginInfo.EmpPwrs
                         where e.mdlid.Trim() == "wms_back" && e.pwrid.Trim()== WMSConst.WMS_BACK_仓位调整退货确认
                         select e).Any()
                        )
                    {
                        return true;
                    }
                }
                // c.调出区为商品区，必须有正常确认权限才能确认调出以及增加新仓位
                // 如果是 商品区/堆头区 调入 退货区
                if ((spqus.Contains(oldqu) || dtqus.Contains(oldqu)) && thqus.Contains(newqu))
                {
                    if ((from e in LoginInfo.EmpPwrs
                         where e.mdlid.Trim() == "wms_back" && e.pwrid.Trim() == WMSConst.WMS_BACK_仓位调整正常确认
                         select 1).Any())
                    {
                        return true;
                    }
                }
            }
            return false;            
             
        }

        

        /// <summary>
        /// 删除仓位调整单
        /// </summary>
        /// <param name="wmsno">仓位调整单单号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整制单, pwrdes = "仓位调整制单")]
        public ActionResult DlAdjBll(String wmsno)
        {            
            //检查是否存在单据
            var qrymst = from e in WmsDc.wms_bllmst
                         where e.mkr == LoginInfo.Usrid  &&
                         e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select e;
            
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_blldtl
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            var qrytpdtl = from e in WmsDc.wms_blltp
                           where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG                           
                           orderby e.rcdidxtp descending
                           select e;
            var arrqrytpdtl = qrytpdtl.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }
            if (arrqrymst[0].chkflg == GetY())
            {
                return RInfo("单据已经审核不能删除新仓位码");
            }
            //是否是同一个人制单
            if (!IsSameLogin(arrqrymst[0].mkr))
            {
                return RInfo("制单人" + arrqrymst[0].mkr + "和登录人" + LoginInfo.Usrid + "不同，不能操作");
            }

            //正在生成拣货单，请稍候重试
            string quRetrv = arrqrymst[0].qu;
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo("正在生成拣货单，请稍候重试");
            }

            WmsDc.wms_blltp.DeleteAllOnSubmit(qrytpdtl);            
            WmsDc.wms_blldtl.DeleteAllOnSubmit(arrqrydtl);
            WmsDc.wms_bllmst.DeleteAllOnSubmit(arrqrymst);
            //写删除日志
            iDelTpDtl(qrytpdtl, arrqrymst[0]);
            iDelBllDtl(qrydtl, arrqrymst[0]);
            iDelBllMst(arrqrymst[0]);

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
        /// 新增需要调整的仓位
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整制单, pwrdes = "仓位调整制单")]
        public ActionResult AdOldBarcodes(String wmsno, String barcodes, String gdsids, String gdstypes, String qtys)
        {
            //判断barcodes、gdsids、gdstypes、qtys是否数量一致
            String[] barcode = barcodes.Split(',');
            String[] gdsid = gdsids.Split(',');
            String[] qty = qtys.Split(',');
            String[] gdstype = gdstypes.Split(',');            
            List<object> retObjs = new List<object>();
            if (
                (barcode.Length != gdsid.Length)
            || (barcode.Length != qty.Length)
                || (gdsid.Length != qty.Length)
            || (gdstype.Length != gdsid.Length)               
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

                JsonResult jr = (JsonResult)AdOldBarcode(wmsno, barcode[i], gdsid[i], gdstype[i], qty[i]);
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
        /// 新增需要调整的仓位
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整制单, pwrdes = "仓位调整制单")]
        public ActionResult AdOldBarcode(String wmsno, String barcode, String gdsid, String gdstype, String qty)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);

            //正在生成拣货单，请稍候重试
            string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo("正在生成拣货单，请稍候重试");
            }

            //判断gdsid和barcode是否是一个区
            String[] qu = GetQuByGdsid(gdsid, LoginInfo.DefStoreid);
            if ( !qu.Contains(barcode.Substring(0, 2)) )
            {
                return RInfo("商品" + gdsid + "不在区[" + string.Join(",", qu) + "]内");
            }
            //判断分区是否有效
            if (!IsExistBarcode(barcode))
            {
                return RInfo("仓位码" + barcode.Trim() + "无效");
            }

            //检查是否存在单据
            var qrymst = from e in WmsDc.wms_bllmst
                         where e.mkr == LoginInfo.Usrid                         && 
                         e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         && barcode.Substring(0, 2) == e.qu
                         select e;
            
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_blldtl
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG //&& e.barcode == barcode
                         select e;
            var arrqrydtl = qrydtl.OrderByDescending(e => e.rcdidx).ToArray();
            var arrqrydtl1 = qrydtl.Where(e => e.gdsid == gdsid && e.gdstype == gdstype && e.barcode == barcode).ToArray();
            var qrytpdtl = from e in WmsDc.wms_blltp
                           where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                           && e.gdsid == gdsid
                           orderby e.rcdidxtp descending
                           select e;
            var arrqrytpdtl = qrytpdtl.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }
            if (arrqrydtl1.Length > 0)
            {
                return RNoData("商品已在调整单中，请不要重复增加");
            }
            /*if (arrqrytpdtl.Length <= 0)
            {
                return RInfo("未找到调整的仓位信息");
            }*/
            if (arrqrymst[0].chkflg == GetY())
            {
                return RInfo("单据已经审核不能删除新仓位码");
            }

            wms_blldtl dtl = new wms_blldtl();
            dtl.wmsno = wmsno;
            dtl.bllid = WMSConst.BLL_TYPE_ADJCANG;
            dtl.rcdidx = arrqrydtl[0].rcdidx + 1;
            dtl.barcode = barcode;
            dtl.gdsid = gdsid;
            dtl.pkgid = "01";
            double fQty = 0;
            if (double.TryParse(qty, out fQty))
            {
                dtl.qty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
            }
            dtl.preqty = Math.Round(fQty, 4, MidpointRounding.AwayFromZero);
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
            dtl.prvid = "";
            dtl.bkr = LoginInfo.Usrid;
            dtl.bokflg = GetN();
            dtl.bokdat = GetCurrentDate();
            dtl.brief = "";

            //如果是报损，判断是否有库存
            //得到一个商品的库存数量
            GdsInBarcode[] gb = GetAGdsQtyInBarcode(dtl.barcode, dtl.gdsid, dtl.gdstype);
            double bqty = (gb == null || gb.Length <= 0) ? 0 : gb[0].sqty;
            double ktqty = bqty;  //可调数量 = 库存数量
            //如果 需调整数量 > 可调数量
            if (dtl.qty > ktqty)
            {
                return RNoData("需要调整的数量" + dtl.qty + "大于可调整数量" + ktqty);
            }

            WmsDc.wms_blldtl.InsertOnSubmit(dtl);

            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", qrydtl.ToArray());
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
            }
        }

        /// <summary>
        /// 删除旧仓位
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="rcdidx"></param>        
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整制单, pwrdes = "仓位调整制单")]
        public ActionResult DlOldBarcode(String wmsno, String gdsid, int rcdidx)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            //正在生成拣货单，请稍候重试
            string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo("正在生成拣货单，请稍候重试");
            }

            //检查是否存在单据
            var qrymst = from e in WmsDc.wms_bllmst
                         where e.mkr == LoginInfo.Usrid &&
                         e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select e;
            
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_blldtl
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG                         
                         select e;
            var arrqrydtl = qrydtl.Where(e=>e.gdsid==gdsid && e.rcdidx==rcdidx).ToArray();
            var qrytpdtl = from e in WmsDc.wms_blltp
                           where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                           && e.gdsid == gdsid && e.rcdidx == rcdidx 
                           orderby e.rcdidxtp descending
                           select e;
            var arrqrytpdtl = qrytpdtl.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("未找到商品明细信息");
            }
            /*if (arrqrytpdtl.Length <= 0)
            {
                return RInfo("未找到调整的仓位信息");
            }*/
            if (arrqrymst[0].chkflg == GetY())
            {
                return RInfo("单据已经审核不能删除新仓位码");
            }
            //是否是同一个人制单
            if (!IsSameLogin(arrqrymst[0].mkr))
            {
                return RInfo("制单人" + arrqrymst[0].mkr + "和登录人" + LoginInfo.Usrid + "不同，不能操作");
            }
            int iArrDtlCnt = qrydtl.Count();
            WmsDc.wms_blltp.DeleteAllOnSubmit(arrqrytpdtl);
            WmsDc.wms_blldtl.DeleteAllOnSubmit(arrqrydtl);
            iDelTpDtl(arrqrytpdtl, arrqrymst[0]);
            iDelBllDtl(qrydtl, arrqrymst[0]);

            //如果明细单没有数据了，就删除主表
            if (iArrDtlCnt == 1)
            {
                WmsDc.wms_bllmst.DeleteOnSubmit(arrqrymst[0]);
                iDelBllMst(arrqrymst[0]);
            }
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

        public class SuggestBarcodeAdjBllRet
        {
            public String barcode { get; set; }
            public double sqty { get; set; }
            public int ty { get; set; }
        }
        /// <summary>
        /// 得到推荐仓位
        /// </summary>
        /// <param name="gdsid">商品编码</param>
        /// <param name="oldbarcode">原仓位</param>
        /// <param name="oldbarcode">推荐到那个分区</param>
        /// <returns></returns>
        public ActionResult SuggestBarcodeAdjBll(String gdsid, String oldbarcode)
        {
            //得到商品的分区
            //String[] qu = GetQuByGdsidAndBarcode(gdsid, oldbarcode, LoginInfo.DefStoreid);
            wms_set[] qusets = GetQuSetByGdsid(gdsid, LoginInfo.DefStoreid);
            if (qusets == null)
            {
                return RInfo("未找到分区信息");
            }
            var quset = qusets.Where(e => e.val3 == LoginInfo.DefSavdptid).Select(e => new
            {
                qu = e.val1,
                dpt = e.val2,
                savdptid = e.val3
            });
            String[] qu = quset.Select(e => e.qu.Trim()).ToArray();
            if (qu == null || qu.Length <= 0)
            {
                return RInfo("未找到分区信息");
            }
            
            //得到商品部门            
            //得到有商品的仓位
            var qcwgds = from e in WmsDc.wms_cwgdsbs
                         join e1 in WmsDc.gds on e.gdsid equals e1.gdsid
                         where (e.savdptid == LoginInfo.DefSavdptid || e.savdptid == LoginInfo.DefCsSavdptid)
                         && qu.Contains(e.qu.Trim()) && qus.Contains(e.qu.Trim())
                         && e.gdsid == gdsid && e.barcode != oldbarcode
                         group e by new { e.savdptid, e.qu, e.barcode, e.gdsid, e.gdstype, e1.gdsdes, e1.spc, e1.bsepkg, e1.dptid } into g
                         select new
                         {
                             g.Key.savdptid,
                             g.Key.qu,
                             g.Key.barcode,
                             g.Key.gdsid,
                             g.Key.gdstype,
                             g.Key.gdsdes,
                             g.Key.spc,
                             g.Key.bsepkg,
                             g.Key.dptid,
                             sqty = g.Sum(eg=>eg.qty)
                         };
            //减去开单量
            var qry1 = from e in qcwgds
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
                           dptid = e.dptid.Trim(),
                           sqty = Math.Round((e.sqty - (e2.qty == null ? 0 : e2.qty)), 4, MidpointRounding.AwayFromZero)
                       };
            var qrycwgds = from e in qry1
                           group e by new { e.barcode, e.savdptid, e.qu } into g                           
                           orderby g.Key.barcode
                           orderby g.Sum(e => e.sqty)
                           select new SuggestBarcodeAdjBllRet { barcode= g.Key.barcode, sqty = g.Sum(e => e.sqty), ty = 0 };
            qrycwgds = qrycwgds.Where(e => e.sqty > 0);

            var arrqrycwgds = qrycwgds.Take(5).ToArray();

            

            //得到没有商品的仓位
            var qrycwempt = from e in WmsDc.wms_cangwei
                            where e.kcflg == '0' && e.tpflg == GetN() && e.isvld == GetY() && e.tjflg == GetY()
                            && qu.Contains(e.qu.Trim()) && qus.Contains(e.qu.Trim())
                            select new SuggestBarcodeAdjBllRet { barcode = e.barcode, sqty = 0, ty = 1 };
            var arrqrycwempt = qrycwempt.Take(1).ToArray();

            var qry = arrqrycwgds.Union(arrqrycwempt).OrderBy(a=>a.ty).ToArray();
            if (qry.Length <= 0)
            {
                return RNoData("未找到有推荐仓位");
            }

            return RSucc("成功", qry);
        }

        /// <summary>
        /// 删除新仓位
        /// </summary>
        /// <param name="wmsno">调整单单号</param>
        /// <param name="gdsid">调整的货号</param>
        /// <param name="rcdidx">调整的序号</param>
        /// <param name="newbarcode">调整到的新仓位</param>
        /// <param name="tprcdidx">调整到的新仓位的序号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整制单, pwrdes = "仓位调整制单")]
        public ActionResult DlNewBarcode(String wmsno, String gdsid, int rcdidx, String newbarcode, int tprcdidx)
        {
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            //正在生成拣货单，请稍候重试
            string quRetrv = GetQuByGdsid(gdsid, LoginInfo.DefStoreid).FirstOrDefault();
            if (DoingRetrieve(LoginInfo.DefStoreid, quRetrv))
            {
                return RInfo("正在生成拣货单，请稍候重试");
            }

            //判断仓位是否存在
            if (!IsExistBarcode(newbarcode))
            {
                return RInfo("仓位" + newbarcode + "不存在");
            }

            //检查是否存在单据
            var qrymst = from e in WmsDc.wms_bllmst
                         where e.mkr == LoginInfo.Usrid  &&
                         e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select e;
            
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_blldtl
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         && e.gdsid == gdsid && e.rcdidx == rcdidx
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            var qrytpdtl = from e in WmsDc.wms_blltp
                           where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                           && e.gdsid == gdsid && e.rcdidx == rcdidx && e.rcdidxtp == tprcdidx
                           orderby e.rcdidxtp descending
                           select e;
            var arrqrytpdtl = qrytpdtl.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("未找到商品明细信息");
            }
            if (arrqrytpdtl.Length <= 0)
            {
                return RNoData("未找到调整的仓位信息");
            }
            if (arrqrymst[0].chkflg == GetY())
            {
                return RInfo("单据已经审核不能删除新仓位码");
            }
            //是否是同一个人制单
            if (!IsSameLogin(arrqrymst[0].mkr))
            {
                return RInfo("制单人" + arrqrymst[0].mkr + "和登录人" + LoginInfo.Usrid + "不同，不能操作");
            }

            WmsDc.wms_blltp.DeleteAllOnSubmit(arrqrytpdtl);
            iDelTpDtl(arrqrytpdtl, arrqrymst[0]); 
            try
            {
                WmsDc.SubmitChanges();
                return RSucc("成功", null);
            }
            catch (Exception ex)
            {
                return RErr(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// 得到会计期间的调整单单
        /// </summary>
        /// <param name="fscprdid">会计期间</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整查询, pwrdes = "仓位调整查询")]
        public ActionResult GetAdjBll(String fscprdid)
        {

            var qrymst = from e in WmsDc.wms_bllmst
                         join e1 in WmsDc.dpt on e.savdptid equals e1.dptid
                         join e2 in WmsDc.emp on e.mkr equals e2.empid
                         where qus.Contains(e.qu.Trim())
                             && e.mkedat.Substring(2, 4) == fscprdid && e.chkflg == GetN()     /// 刘启杰说的
                             //&& e.mkedat.Substring(0, 8) == GetCurrentDay()               /// 周熙说的
                         //&& e.mkr == LoginInfo.Usrid
                         && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select new
                         {
                             e.wmsno,
                             e.savdptid,
                             savdptdes = e1.dptdes,
                             e.mkedat,
                             e.mkr,
                             e.chkflg,
                             e.chkdat,
                             mkrdes = e2.empdes,
                             e.qu
                         };  

            //todo : 判断是否有审核权限，如果有就显示全部未审核的和自己做的单子
            var qry = from e in LoginInfo.EmpPwrs
                      where e.mdlid.Trim() == "wms_back" && e.pwrid.Trim() == WMSConst.WMS_BACK_仓位调整审核
                      && e.empid.Trim() == LoginInfo.Usrid.Trim()
                      select e;
            //如果没有审核权限，就只能看自己没有审核的
            if (qry.Count() == 0)
            {
                qrymst = qrymst.Where(e => e.mkr == LoginInfo.Usrid);
            }


            var arrqrymst = qrymst.ToArray();

            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }

            //检查单据是否已经审核
            /*var mst = arrqrymst[0];
            if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核不能修改");
            }*/

            return RSucc("成功", arrqrymst);
        }

        /// <summary>
        /// 得到调整单商品明细
        /// </summary>
        /// <param name="wmsno">返仓单单号</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整制单, pwrdes = "仓位调整制单")]
        public ActionResult GetAdjDtlBll(String wmsno)
        {            
            var qrymst = from e in WmsDc.wms_bllmst
                         where // e.mkr == LoginInfo.Usrid                         && 
                         qus.Contains(e.qu.Trim())
                         && e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select e;
            //todo : 判断是否有审核权限，如果有就显示全部未审核的和自己做的单子
            var qry = from e in LoginInfo.EmpPwrs
                      where e.mdlid.Trim() == "wms_back" && e.pwrid.Trim() == WMSConst.WMS_BACK_仓位调整审核
                      && e.empid.Trim() == LoginInfo.Usrid.Trim()
                      select e;
            //如果没有审核权限，就只能看自己没有审核的
            if (qry.Count() == 0)
            {
                qrymst = qrymst.Where(e => e.mkr == LoginInfo.Usrid);
            }

            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_blldtl
                         join e1 in WmsDc.gds on e.gdsid equals e1.gdsid                         
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select new
                         {
                             e.barcode,
                             e.bcd,
                             e.bkr,
                             e.bllid,
                             e.bokdat,
                             e.bokflg,
                             e.brief,
                             e.bthno,
                             e.gdsid,
                             e.gdstype,
                             e.pkgid,
                             e.preqty,
                             e.prvid,
                             qty = Math.Round(e.qty, 4, MidpointRounding.AwayFromZero),
                             e.rcdidx,
                             e.vlddat,
                             e.wmsno,
                             e1.gdsdes,
                             e1.spc,
                             e1.bsepkg,
                             tpqty = from ht in WmsDc.wms_blltp
                                     where ht.wmsno==e.wmsno && ht.bllid==e.bllid && ht.rcdidx==e.rcdidx
                                     group ht by new{ht.wmsno,ht.bllid,ht.rcdidx} into g
                                     select g.Sum(t=>t.qty)
                         };
            //join e2 in WmsDc.pkg on new { e1.gdsid, iscseorspt = '3' } equals new { e2.gdsid, e2.iscseorspt }
            var q = from e in qrydtl
                    join e1 in
                        WmsDc.v_wms_pkg on new { e.gdsid } equals new { e1.gdsid }
                    into joinPkgDtl
                    from e2 in joinPkgDtl.DefaultIfEmpty()
                    select new
                    {
                        e.barcode,
                        e.bcd,
                        e.bkr,
                        e.bllid,
                        e.bokdat,
                        e.bokflg,
                        e.brief,
                        e.bsepkg,
                        e.bthno,
                        e.gdsdes,
                        e.gdsid,
                        e.gdstype,
                        e.pkgid,
                        e.preqty,
                        e.prvid,
                        e.qty,
                        e.rcdidx,
                        e.spc,
                        e.vlddat,
                        e.wmsno,
                        tpqty = e.tpqty,
                        pkg03 = e2.cnvrto != null ? GetPkgStr(e.qty, e2.cnvrto, e2.pkgdes) : GetPkgStr(e.qty, null, null),
                        pkg03pre = e2.cnvrto != null ? GetPkgStr(e.preqty, e2.cnvrto, e2.pkgdes) : GetPkgStr(e.qty, null, null)
                    };

            var arrqrydtl = q.ToArray()
                            .Select(t => new
                            {
                                t.barcode,
                                t.bcd,
                                t.bkr,
                                t.bllid,
                                t.bokdat,
                                t.bokflg,
                                t.brief,
                                t.bsepkg,
                                t.bthno,
                                t.gdsdes,
                                t.gdsid,
                                t.gdstype,
                                t.pkg03,
                                t.pkg03pre,
                                t.pkgid,
                                t.preqty,
                                t.prvid,
                                t.qty,
                                t.rcdidx,
                                t.spc,
                                tpqty = t.tpqty.ToArray().Length==0?0:t.tpqty.ToArray()[0],
                                t.vlddat,
                                t.wmsno
                            }).ToArray();

            
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("未找到商品明细信息");
            }

            return RSucc("成功", arrqrydtl);
        }

        /// <summary>
        /// 得到商品新调整仓位的信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="gdsid"></param>
        /// <param name="rcdidx"></param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整制单, pwrdes = "仓位调整制单")]
        public ActionResult GetNewBarcode(String wmsno, String gdsid, int rcdidx)
        {            
            gdsid = GetGdsidByGdsidOrBcd(gdsid);
            //检查是否存在单据
            var qrymst = from e in WmsDc.wms_bllmst
                         where //e.mkr == LoginInfo.Usrid &&
                         qus.Contains(e.qu.Trim())
                         && e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         select e;
            //todo : 判断是否有审核权限，如果有就显示全部未审核的和自己做的单子
            var qry = from e in LoginInfo.EmpPwrs
                      where e.mdlid.Trim() == "wms_back" && e.pwrid.Trim() == WMSConst.WMS_BACK_仓位调整审核
                      && e.empid.Trim() == LoginInfo.Usrid.Trim()
                      select e;
            //如果没有审核权限，就只能看自己没有审核的
            if (qry.Count() == 0)
            {
                qrymst = qrymst.Where(e => e.mkr == LoginInfo.Usrid);
            }
            var arrqrymst = qrymst.ToArray();
            var qrydtl = from e in WmsDc.wms_blldtl
                         where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                         && e.gdsid == gdsid && e.rcdidx == rcdidx
                         select e;
            var arrqrydtl = qrydtl.ToArray();
            var qrytpdtl = from e in WmsDc.wms_blltp
                           where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADJCANG
                           && e.gdsid == gdsid && e.rcdidx == rcdidx
                           orderby e.rcdidxtp descending
                           select new
                           {
                               e.barcode,
                               e.bllid,
                               e.gdsid,
                               e.gdstype,
                               e.pkgid,
                               qty = Math.Round(e.qty, 4, MidpointRounding.AwayFromZero),
                               e.qu,
                               e.rcdidx,
                               e.rcdidxtp,
                               e.savdptid,
                               e.tpcode,
                               e.wmsno
                           };
            var arrqrytpdtl = qrytpdtl.ToArray();
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到单据信息");
            }
            if (arrqrydtl.Length <= 0)
            {
                return RNoData("未找到商品明细信息");
            }
            if (arrqrytpdtl.Length <= 0)
            {
                return RNoData("未找到调整的仓位信息");
            }
            //检查单据是否已经审核
            wms_bllmst mst = arrqrymst[0];
            /*if (mst.chkflg == GetY())
            {
                return RInfo("单据已经审核不能修改");
            }*/

            //返回新仓位信息
            return RSucc("成功", arrqrytpdtl);
        }

        /// <summary>
        /// 仓位调整查询
        /// </summary>
        /// <param name="begindat">查询开始时间</param>
        /// <param name="enddat">查询结束时间</param>
        /// <param name="wmsno">调整单单号</param>
        /// <param name="gdsid">商品货号、条码</param>
        /// <param name="barcode">仓位</param>
        /// <returns></returns>
        [PWR(Pwrid = WMSConst.WMS_BACK_仓位调整查询, pwrdes = "仓位调整查询")]
        public ActionResult FindBll(String begindat, String enddat, String wmsno, String gdsid, String barcode)
        {
            //判断分区是否有效
            if (!String.IsNullOrEmpty(barcode) && !IsExistBarcode(barcode))
            {
                return RInfo("仓位码" + barcode.Trim() + "无效");
            }

            var arrqrymst = FindBllFromBllMst(WMSConst.BLL_TYPE_ADJCANG,begindat,enddat,wmsno, gdsid, barcode);
                      
            if (arrqrymst.Length <= 0)
            {
                return RNoData("未找到符合条件的单据");
            }
            return RSucc("成功", arrqrymst);
        }
    }

    public class GetCanUnionBarcodeRet
    {
        public String barcode;
        public double? zheng;
        public int? ling;
        public double? zong;        
    }
}
