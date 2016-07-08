using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;

namespace WMS.Controllers
{
    public class GetBhRet
    {
        public string savdptid { get; set; }
        public string gdsdes { get; set; }
        public string spc { get; set; }
        public string bsepkg { get; set; }
        public string gdsid { get; set; }
        public double? bhqty { get; set; }
        public double? bhjg { get; set; }   //补货件规
        public string gdsid_1 { get; set; }
        public string barcode { get; set; }
        public double? highqty { get; set; }
        public double? cwqty { get; set; }
        public double? safeqty { get; set; }
    }
    /// <summary>
    /// 补货模块
    /// </summary>
    public class AddGdsController : SsnController
    {
        protected override void SetModuleInfo()
        {
            this.Mdlid = "AddGds";
            this.Mdldes = "补货模块";
        }

        /// <summary>
        /// 得到需要补货的数据
        /// </summary>
        /// <param name="mkedat"></param>
        /// <param name="qu"></param>
        /// <param name="savdptid"></param>
        /// <returns></returns>
        public ActionResult GetBh(string mkedat, string qu, string savdptid)
        {
            #region 得到需要补货的数据的sql语句
            string sql = @"
    /*declare @ls_savdptid varchar(6),@ls_peisong varchar(8)
	
	select @ls_savdptid='" + savdptid + @"'
	select @ls_peisong='" + LoginInfo.DefStoreid + @"'*/
	
	insert into wms_savegds
	select @ls_savdptid,dptid,clsid,gdsid,isnull((select max(cnvrto)  from pkg where pkg.gdsid=gds.gdsid ),0)*5,'system',convert(char(8),getdate(),112)+replace(convert(char(8),getdate(),108),':',''),'y','y' 
	from gds 
	where isstpsal='n'
	and gdsid not in (select gdsid from wms_savegds);
			
	update wms_savegds
	set updat=convert(char(8),getdate(),112)+replace(convert(char(8),getdate(),108),':',''),
		qty=ceiling((isnull(qty103,0)+isnull(qty115,0)) / (select count(distinct mkedat) from wms_cang where bllid='103' and mkedat>=convert(char(8),dateadd(dd,(select 0 - convert(integer,(select val1 from wms_set where setid='011' and isvld='y' and val3='S161'))),getdate()),112) and mkedat<convert(char(8),dateadd(dd,1,getdate()),112)) * convert(integer,(select val1 from wms_set where setid='012' and isvld='y' and val3='S161')) / (select max(cnvrto)  from pkg where pkg.gdsid=t1.gdsid )) * t4.cvnrto
	from wms_savegds t1
	left join (select gdsid,sum(qty) qty103 from wms_cangdtl where bllid='103' and wmsno in (select wmsno from wms_cang where bllid='103' and mkedat>=convert(char(8),dateadd(dd,(select 0 - convert(integer,(select val1 from wms_set where setid='011' and isvld='y' and val3='S161'))),getdate()),112) and mkedat<convert(char(8),dateadd(dd,1,getdate()),112)) group by gdsid) t2 on t1.gdsid=t2.gdsid
	left join (select gdsid,sum(qty) qty115 from wms_cangdtl_115 where bllid='115' and wmsno in (select wmsno from wms_cang_115 where bllid='115' and mkedat>=convert(char(8),dateadd(dd,(select 0 - convert(integer,(select val1 from wms_set where setid='011' and isvld='y' and val3='S161'))),getdate()),112) and mkedat<convert(char(8),dateadd(dd,1,getdate()),112)) group by gdsid) t3 on t1.gdsid=t3.gdsid
	join (select gdsid,max(cnvrto) cvnrto from pkg group by gdsid) t4 on t1.gdsid=t4.gdsid
	where t1.savdptid=@ls_savdptid 
	and t1.calflg='y' and t1.isvld='y'
	and isnull(qty103,0)+isnull(qty115,0)>0;
";
            sql = sql.Replace("@ls_savdptid", "'" + savdptid + "'");
            sql = sql.Replace("@ls_peisong", "'" + LoginInfo.DefStoreid + "'");

            sql += @"/*declare @mkedat varchar(8),@savdptid varchar(6),@qu varchar(6)

                    select @mkedat='" + mkedat + @"'
                    select @qu='" + qu + @"'
                    select @savdptid='" + savdptid + @"'*/

                         
                    select * from 
                    (
                    select t1.savdptid,(select gdsdes from gds where gdsid=t1.gdsid) gdsdes,
                    (select spc from gds where gdsid=t1.gdsid) spc,
                    (select bsepkg from gds where gdsid=t1.gdsid) bsepkg,
                    t1.gdsid,
                    isnull(t4.cwqty,0) cwqty,isnull(t1.qty,0) safeqty,
                    isnull(t4.cwqty,0) - isnull(t1.qty,0) + isnull(t8.qty_108_add,0) bhqty,
                    floor(round((isnull(t4.cwqty,0) - t1.qty + isnull(t8.qty_108_add,0))/isnull((select max(cnvrto) from pkg where iscseorspt='3' and gdsid=t1.gdsid),1),0)) bhjg

                    from 

                    --以安全库存为蓝本
                    (select * from wms_savegds where dptid in (select val1 from wms_set where setid='999' and val3=@savdptid) and calflg='y' and isvld='y') t1

                    --计算正常拣货的拣货量
                    left join (select wms_cang.savdptid,(select dptid from gds where gdsid=wms_cangdtl.gdsid) dptid,gdsid,sum(qty) qty_103 from wms_cang,wms_cangdtl
                    inner join (select * from wms_cangwei where savdptid=@savdptid and qu=@qu and tjflg='n') p1 on wms_cangdtl.barcode=p1.barcode
                    where wms_cang.wmsno=wms_cangdtl.wmsno and wms_cang.bllid=wms_cangdtl.bllid and
                    wms_cang.savdptid=@savdptid and wms_cang.qu=@qu and wms_cang.bllid='103'
                    and mkedat=@mkedat
                    group by wms_cang.savdptid,gdsid) t2
                    on t1.savdptid=t2.savdptid and t1.dptid=t2.dptid and t1.gdsid=t2.gdsid

                    --计算摘果拣货的拣货量
                    left join (select wms_cang_115.savdptid,(select dptid from gds where gdsid=wms_cangdtl_115.gdsid) dptid,gdsid,sum(qty) qty_115 from wms_cang_115,wms_cangdtl_115
                    inner join (select * from wms_cangwei where savdptid=@savdptid and qu=@qu and tjflg='n') p1 on wms_cangdtl_115.barcode=p1.barcode
                    where wms_cang_115.wmsno=wms_cangdtl_115.wmsno and wms_cang_115.bllid=wms_cangdtl_115.bllid and
                    wms_cang_115.savdptid=@savdptid and wms_cang_115.qu=@qu and wms_cang_115.bllid='115'
                    and mkedat=@mkedat
                    group by wms_cang_115.savdptid,gdsid) t3
                    on t1.savdptid=t3.savdptid and t1.dptid=t3.dptid and t1.gdsid=t3.gdsid

                    --计算非推荐仓位库存
                    left join (select wms_cwgdsbs.savdptid,(select dptid from gds where gdsid=wms_cwgdsbs.gdsid) dptid,wms_cwgdsbs.gdsid,sum(qty) - 
                    isnull((select sum(qty) sndqty
                    from wms_sendbill 
                    inner join (select * from wms_cangwei where savdptid=@savdptid and qu=@qu and tjflg='n') p1 on wms_sendbill.barcode=p1.barcode
                    where wms_sendbill.savdptid=@savdptid and wms_sendbill.qu=@qu
                    and wms_cwgdsbs.savdptid=wms_sendbill.savdptid 
                    and wms_cwgdsbs.gdsid=wms_sendbill.gdsid  group by wms_sendbill.savdptid,gdsid),0) cwqty
                    from wms_cwgdsbs
                    inner join (select * from wms_cangwei where savdptid=@savdptid and qu=@qu and tjflg='n') p1 on wms_cwgdsbs.barcode=p1.barcode
                    where wms_cwgdsbs.savdptid=@savdptid and wms_cwgdsbs.qu=@qu
                    group by wms_cwgdsbs.savdptid,wms_cwgdsbs.gdsid) t4
                    on t1.savdptid=t4.savdptid and t1.dptid=t4.dptid and t1.gdsid=t4.gdsid

                    --发生了正常拣货高仓位拣货的
                    left join (select wms_cang.savdptid,(select dptid from gds where gdsid=wms_cangdtl.gdsid) dptid,gdsid,sum(qty) qty_103_high from wms_cang,wms_cangdtl
                    inner join (select * from wms_cangwei where savdptid=@savdptid and qu=@qu and tjflg='y') p1 on wms_cangdtl.barcode=p1.barcode
                    where wms_cang.wmsno=wms_cangdtl.wmsno and wms_cang.bllid=wms_cangdtl.bllid and
                    wms_cang.savdptid=@savdptid and wms_cang.qu=@qu and wms_cang.bllid='103'
                    and mkedat=@mkedat
                    group by wms_cang.savdptid,gdsid) t6
                    on t1.savdptid=t6.savdptid and t1.dptid=t6.dptid and t1.gdsid=t6.gdsid

                    --发生了摘果拣货高仓位拣货的
                    left join (select wms_cang_115.savdptid,(select dptid from gds where gdsid=wms_cangdtl_115.gdsid) dptid,gdsid,sum(qty) qty_115_high from wms_cang_115,wms_cangdtl_115
                    inner join (select * from wms_cangwei where savdptid=@savdptid and qu=@qu and tjflg='y') p1 on wms_cangdtl_115.barcode=p1.barcode
                    where wms_cang_115.wmsno=wms_cangdtl_115.wmsno and wms_cang_115.bllid=wms_cangdtl_115.bllid and
                    wms_cang_115.savdptid=@savdptid and wms_cang_115.qu=@qu and wms_cang_115.bllid='115'
                    and mkedat=@mkedat
                    group by wms_cang_115.savdptid,gdsid) t7
                    on t1.savdptid=t7.savdptid and t1.dptid=t7.dptid and t1.gdsid=t7.gdsid

                    --当天做的仓位调整单新增的库存
                    left join (select wms_bllmst.savdptid,(select dptid from gds where gdsid=wms_blltp.gdsid) dptid,gdsid,sum(qty) qty_108_add 
                    from wms_bllmst,wms_blltp
                    where wms_bllmst.wmsno=wms_blltp.wmsno and wms_bllmst.bllid=wms_blltp.bllid
                    and wms_bllmst.savdptid=@savdptid and wms_bllmst.bllid='108' and wms_bllmst.mkedat>=@mkedat
                    and wms_bllmst.chkflg='n'
                    group by wms_bllmst.savdptid,gdsid) t8
                    on t1.savdptid=t8.savdptid and t1.dptid=t8.dptid and t1.gdsid=t8.gdsid

                    where 
                    --条件1，拣货量超过安全库存30%
                    (((isnull(t2.qty_103,0) + isnull(t3.qty_115,0)) / (case t1.qty when 0 then 1 else t1.qty end) > 0.3)
                    --条件2，低位库存为0并且高位库存有货
                    or (t4.cwqty=0)-- and t5.allqty>0) 
                    --条件3，发生了正常拣货模式高仓位拣货的
                    or t6.qty_103_high>0 
                    --条件4，发生了摘果拣货模式高仓位拣货的
                    or t7.qty_115_high>0) 
                    ) table1

                    ----取得补货规则的补货仓位
                    left join (select gdsid gdsid_1,barcode,vlddat,bthno,allqty highqty from wms_cwgdsbs
                    join (select  a.gdsid+min(a.vlddat+a.bthno+a.barcode) vldbarcode,sum(isnull(a.qty,0) - isnull(b.qty,0)) allqty from wms_cwgdsbs a
                    left join wms_sendbill b on a.barcode=b.barcode and a.gdsid=b.gdsid and a.gdstype=b.gdstype and a.vlddat=b.vlddat and a.bthno=b.bthno
                    inner join (select * from wms_cangwei where savdptid=@savdptid and qu=@qu and tjflg='y') p1 on a.savdptid=p1.savdptid and a.qu=p1.qu and a.barcode=p1.barcode
                    where a.savdptid=@savdptid and a.qu=@qu and isnull(a.qty,0) - isnull(b.qty,0)>0
                    group by a.gdsid) table_temp1 on gdsid+vlddat+bthno+barcode = table_temp1.vldbarcode
                    where qty>0) table2
                    on table1.gdsid=table2.gdsid_1

                    order by table2.barcode";

            sql = sql.Replace("@mkedat", "'" + mkedat + "'");
            sql = sql.Replace("@qu", "'" + qu + "'");
            sql = sql.Replace("@savdptid", "'" + savdptid + "'");
                #endregion 得到需要补货的数据

            IEnumerable<GetBhRet> retobj =  WmsDc.ExecuteQuery<GetBhRet>(sql);
            GetBhRet[] arrRetObj = retobj.ToArray();
            if (arrRetObj.Length == 0)
            {
                return RNoData("I0467");
            }
            return RSucc("成功", arrRetObj, "S0229");
        }

        /// <summary>
        /// 获取当天的补货模块
        /// </summary>
        /// <returns></returns>
        public ActionResult GetCurDayBll()
        {
            var qry = from e in WmsDc.wms_addgds
                      where e.mkedat.Substring(0,8) == GetCurrentDay()
                      && e.bllid == WMSConst.BLL_TYPE_ADDGDSBLL
                      && qus.Contains(e.qu)
                      && savdpts.Contains(e.savdptid)
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length == 0)
            {
                return RNoData("N0001");
            }

            return RSucc("成功", arrqry, "S0001");
        }

        /// <summary>
        /// 得到单号对应的明细信息
        /// </summary>
        /// <param name="wmsno"></param>
        /// <returns></returns>
        public ActionResult GetBlldtl(String wmsno)
        {
            var qry = from e in WmsDc.wms_addgds
                      join e1 in WmsDc.wms_addgdsdtl on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.wms_pkg on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.gds on e1.gdsid equals e3.gdsid
                      where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADDGDSBLL
                      && qus.Contains(e.qu)
                      && savdpts.Contains(e.savdptid)
                      select new
                      {
                          e1.wmsno,
                          e1.bllid,
                          e1.rcdidx,
                          e1.gdsid,
                          e1.lowqty,
                          e1.safeqty,
                          e1.qty,
                          e1.qtyper,
                          e1.safeflg,
                          gdsdes = e3.gdsdes.Trim(),
                          e3.spc,
                          e3.bsepkg,
                          e2.cnvrto,
                          pkgdes = e2.pkgdes.Trim(),
                          pkg03 = GetPkgStr(e1.qty, e2.cnvrto, e2.pkgdes),
                          pkg03pre = GetPkgStr(e1.qty, e2.cnvrto, e2.pkgdes)
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length == 0)
            {
                return RNoData("N0002");
            }
            return RSucc("成功", arrqry, "S0002");
        }

        /// <summary>
        /// 增加补货明细
        /// </summary>
        /// <param name="wmsno"></param>
        /// <param name="rcdidx"></param>
        /// <param name="gdsid"></param>
        /// <param name="gdstype"></param>
        /// <param name="outbarcode"></param>
        /// <param name="outqty"></param>
        /// <param name="inbarcode"></param>
        /// <param name="inqty"></param>
        /// <returns></returns>
        public ActionResult AddAdj(String wmsno, int rcdidx, String gdsid, String gdstype, String outbarcode, double outqty, String inbarcode, double inqty)
        {
            var qry = from e in WmsDc.wms_addgds
                      join e1 in WmsDc.wms_addgdsdtl on new { e.wmsno, e.bllid } equals new { e1.wmsno, e1.bllid }
                      join e2 in WmsDc.wms_pkg on e1.gdsid equals e2.gdsid
                      join e3 in WmsDc.gds on e1.gdsid equals e3.gdsid
                      where e.wmsno == wmsno && e.bllid == WMSConst.BLL_TYPE_ADDGDSBLL
                      && qus.Contains(e.qu)
                      && savdpts.Contains(e.savdptid)
                      select new
                      {
                          e1.wmsno,
                          e1.bllid,
                          e1.rcdidx,
                          e1.gdsid,
                          e1.lowqty,
                          e1.safeqty,
                          e1.qty,
                          e1.qtyper,
                          e1.safeflg,
                          gdsdes = e3.gdsdes.Trim(),
                          e3.spc,
                          e3.bsepkg,
                          e2.cnvrto,
                          pkgdes = e2.pkgdes.Trim(),
                          pkg03 = GetPkgStr(e1.qty, e2.cnvrto, e2.pkgdes),
                          pkg03pre = GetPkgStr(e1.qty, e2.cnvrto, e2.pkgdes)
                      };
            var arrqry = qry.ToArray();
            if (arrqry.Length == 0)
            {
                return RNoData("N0003");
            }
            // 未找到商品信息
            if(!arrqry.Where(e=>e.rcdidx==rcdidx&&e.wmsno==wmsno.Trim()&&e.gdsid==gdsid.Trim()).Any()){
                return RNoData("N0004");
            }
            // 插入新的信息
            wms_addgdsadj aga = new wms_addgdsadj();

            return null;
        }

    }
}
