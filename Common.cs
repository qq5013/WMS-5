using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WMS.Models;

namespace WMS.Common
{
    #region 返回类型
    /// <summary>
    /// GetRealteQu 返回类型
    /// </summary>
    public class GetRealteQuResult
    {
        /// <summary>
        /// 区位码
        /// </summary>
        public string qu { get; set; }
        /// <summary>
        /// 部门编码
        /// </summary>
        public string dptid { get; set; }
        /// <summary>
        /// 配送商品库编码
        /// </summary>
        public string savdptid { get; set; }
        /// <summary>
        /// 配送商品库名称
        /// </summary>
        public string savdptdes { get; set; }
        /// <summary>
        /// 配送类型定义
        /// </summary>
        public String storetypeid { get; set; }
        /// <summary>
        /// 配送仓库编码
        /// </summary>
        public String savstoreid { get; set; }
        /// <summary>
        /// 配送仓库名称
        /// </summary>
        public String savstoredes { get; set; }
    }

    /// <summary>
    /// 单据商品类型
    /// </summary>
    public class WmsBllGds
    {
        public String gdsid { get; set; }
        public String gdsdes { get; set; }
        public String spc { get; set; }
        public String bsepkg { get; set; }
        public String pkg03 { get; set; }
        public String pkg03pre { get; set; }
    }

    public class WmsBllPrv{
        public String prvid{get;set;}
        public String prvdes{get;set;}
    }

    public class Wmsbll
    {
        /// <summary>
        /// 单据主表
        /// </summary>
        public wms_bllmst mst { get; set; }
        /// <summary>
        /// 主表对应的供应商
        /// </summary>
        public WmsBllPrv prv { get; set; }
        /// <summary>
        /// 业务部门
        /// </summary>
        public String dptdes { get; set; }
        /// <summary>
        /// 单据明细表
        /// </summary>
        public wms_blldtl[] dtl { get; set; }
        /// <summary>
        /// 明细对应的商品基础表
        /// </summary>
        public WmsBllGds[] gds { get; set; }
        /// <summary>
        /// 整合明细
        /// </summary>
        public Object[] dtls { get; set; }
    }

    #endregion   

    /// <summary>
    /// 登录信息
    /// </summary>
    public class LoginInfo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public String Usrid { get; set; }
        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime LoginDtm { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        public String UsrName { get; set; }
        /// <summary>
        /// 默认配送中心编码
        /// </summary>
        public String DefStoreid { get; set; }
        /// <summary>
        /// 默认配送中心名称
        /// </summary>
        public String DefStoredes { get; set; }
        /// <summary>
        /// 默认所属商品库配送编码
        /// </summary>
        public String DefSavdptid { get; set; }
        /// <summary>
        /// 默认所属商品库配送名称
        /// </summary>
        public String DefSavdptdes { get; set; }
        /// <summary>
        /// 默认所属残损库配送编码
        /// </summary>
        public String DefCsSavdptid { get; set; }
        /// <summary>
        /// 默认所属残损库配送名称
        /// </summary>
        public String DefCsSavdptdes { get; set; }
        /// <summary>
        /// 所属分区
        /// </summary>
        public GetRealteQuResult[] SavDptids
        { get; set; }
        /// <summary>
        /// 所属配送
        /// </summary>
        public Store[] SavStoreids { get; set; }
        /// <summary>
        /// 权限分区关联类
        /// </summary>
        public GetRealteQuResult[] DatPwrs { get; set; }
        /// <summary>
        /// 模块权限表
        /// </summary>
        public emppwr[] EmpPwrs {get;set;}
    }

    /// <summary>
    /// 配送中心
    /// </summary>
    public class Store
    {
        /// <summary>
        /// 配送中心编码
        /// </summary>
        public String Storeid { get; set; }
        /// <summary>
        /// 配送中心名称
        /// </summary>
        public String Storedes { get; set; }
    }
}