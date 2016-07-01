using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS
{
    

    #region 单据枚举
    /// <summary>
    /// 采购订单状态
    /// </summary>
    public enum ORD_STATUS
    {
        /// <summary>
        /// 未审核
        /// </summary>
        UNAUDIT = '0',    
        /// <summary>
        /// 已审核
        /// </summary>
        AUDIT = '1',
        /// <summary>
        /// 转单成功
        /// </summary>
        RETURN_BLL_SUCCESS = '2',
        /// <summary>
        /// 正常订单作废
        /// </summary>
        NORMAL_DISABLE  = '3'
    }
    #endregion 单据枚举

    /// <summary>
    /// 常数类型
    /// </summary>
    public static class WMSConst
    {        
        public const bool DEBUG = true;

        #region BLL_TYPE（订单类型）
        /// <summary>
        /// 采购订单
        /// </summary>        
        public static String BLL_TYPE_PURCHASE = "958";

        /// <summary>
        /// 内调单
        /// </summary>
        public static String BLL_TYPE_INNERADJ = "112";

        /// <summary>
        /// 收货单
        /// </summary>
        public static String BLL_TYPE_REVIECEBLL = "101";

        /// <summary>
        /// 上架单
        /// </summary>
        public static String BLL_TYPE_UPBLL = "102";

        /// <summary>
        /// 补货单
        /// </summary>
        public static string BLL_TYPE_ADDGDSBLL = "113";
        
        /// <summary>
        /// 配送单
        /// </summary>
        public static String BLL_TYPE_DISPATCH = "206";

        /// <summary>
        /// 外销单
        /// </summary>
        public static String BLL_TYPE_WXDISPATCH = "501";

        /// <summary>
        /// 捡货单
        /// </summary>
        public static String BLL_TYPE_RETRIEVE = "103";

        /// <summary>
        /// 摘果播种捡货单
        /// </summary>
        public static String BLL_TYPE_FRUITRETRIEVE = "115";

        /// <summary>
        /// 捡货单损溢单
        /// </summary>
        public static String BLL_TYPE_RETRIEVE_PROFIT_LOSS = "104";

        /// <summary>
        /// 盘点单
        /// </summary>
        public static String BLL_TYPE_INVENTORY_CHECK = "105";

        /// <summary>
        /// 库存调整单
        /// </summary>
        public static String BLL_TYPE_ADJUSTQTY = "111";

        /// <summary>
        /// 播种单
        /// </summary>
        public static String BLL_TYPE_BZ = "107";

        /// <summary>
        /// 返仓单
        /// </summary>
        public static String BLL_TYPE_RETCANG = "109";

        /// <summary>
        /// 仓位调整单
        /// </summary>
        public static String BLL_TYPE_ADJCANG = "108";

        /// <summary>
        /// 损溢单
        /// </summary>
        public static String BLL_TYPE_PROFITORLOSS = "111";

        /// <summary>
        /// 返厂单
        /// </summary>
        public static String BLL_TYPE_RETPRV = "110";
        #endregion

        #region SET_TYPE（设置类型）
        /// <summary>
        /// 分区部门关联
        /// </summary>
        public const String SET_TYPE_RELATEDPT = "001";

        /// <summary>
        /// 单据类型
        /// </summary>
        public const String SET_TYPE_BLLTYPE = "002";

        /// <summary>
        /// 拣货方式
        /// </summary>
        public const String SET_TYPE_RETRIEVETYPE = "003";

        /// <summary>
        /// 商品类型
        /// </summary>
        public const string SET_TYPE_GOODSTYPE = "005";

        /// <summary>
        /// 区位定义
        /// </summary>
        //public const string SET_TYPE_QUDEF = "005";

        /// <summary>
        /// 特殊业务库
        /// </summary>
        public const String SET_TYPE_SPECIALSTORE = "007";

        /// <summary>
        /// 区位定义
        /// </summary>
        public const String SET_TYPE_QUSET = "006";

        /// <summary>
        /// 仓库类型定义
        /// </summary>
        public const String SET_TYPE_STORETYPEDEFIND = "008";

        /// <summary>
        /// 允许启用的部门
        /// </summary>
        public const String SET_TYPE_ENABLEDPT = "999";
        #endregion

        #region GDS_TYPE(商品类型)
        /// <summary>
        /// 正常商品
        /// </summary>
        public const String GDS_TYPE_NORMAL = "50"; //正常商品
        /// <summary>
        /// 赠品
        /// </summary>
        public const String GDS_TYPE_GIFT = "70";   //赠品
        /// <summary>
        /// 促销包装
        /// </summary>
        public const String GDS_TYPE_SPECIAL_GIFT = "90";   //促销包装
        #endregion

        #region KC_FLG(有无库存,0无库存，1有库存)
        /// <summary>
        /// 有库存
        /// </summary>
        public static char KC_FLG_HASQTY = '1';
        /// <summary>
        /// 无库存
        /// </summary>
        public static char KC_FLG_NONQTY = '0';
        #endregion

        #region MDL_NAME(模块名称)
        /// <summary>
        /// 收货模块
        /// </summary>
        public static String MDL_NAME_RECIEV = "reciev";
        /* ============  特殊的设置 ==========================
         * --- 程序里面做的特殊处理，数据库中没有这种设置   --
         * ---------------------------------------------------
        */
        public const String WMS_BACK_仓位调整正常确认 = "0906";
        public const String WMS_BACK_仓位调整退货确认 = "0903";

        #region 老的设置
        /*
        public const String WMS_BACK_仓位调整正常确认 = "0906";
        public const String WMS_BACK_仓位调整退货确认 = "0903";
        public const String WMS_BACK_收货查询 = "0101";
        public const String WMS_BACK_收货制单 = "0102";
        public const String WMS_BACK_收货确认 = "0103";
        public const String WMS_BACK_收货审核 = "0104";
        public const String WMS_BACK_收货打印 = "0105";
        public const String WMS_BACK_上架查询 = "0201";
        public const String WMS_BACK_上架制单 = "0202";
        public const String WMS_BACK_上架确认 = "0203";
        public const String WMS_BACK_上架审核 = "0204";
        public const String WMS_BACK_上架打印 = "0205";
        public const String WMS_BACK_拣货查询 = "0301";
        public const String WMS_BACK_拣货制单 = "0302";
        public const String WMS_BACK_拣货确认 = "0303";
        public const String WMS_BACK_拣货审核 = "0304";
        public const String WMS_BACK_拣货打印 = "0305";
        public const String WMS_BACK_正常拣货 = "0307";
        public const String WMS_BACK_残损拣货 = "0308";
        public const String WMS_BACK_外销拣货 = "0309";
        public const String WMS_BACK_内调拣货 = "0310";
        public const String WMS_BACK_代发拣货 = "0311";
        public const String WMS_BACK_播种查询 = "0401";
        public const String WMS_BACK_播种制单 = "0402";
        public const String WMS_BACK_播种确认 = "0403";
        public const String WMS_BACK_播种审核 = "0404";
        public const String WMS_BACK_播种打印 = "0405";
        public const String WMS_BACK_返仓查询 = "0501";
        public const String WMS_BACK_返仓制单 = "0502";
        public const String WMS_BACK_返仓确认 = "0503";
        public const String WMS_BACK_返仓审核 = "0504";
        public const String WMS_BACK_返仓打印 = "0505";
        public const String WMS_BACK_退厂查询 = "0601";
        public const String WMS_BACK_退厂制单 = "0602";
        public const String WMS_BACK_退厂确认 = "0603";
        public const String WMS_BACK_退厂审核 = "0604";
        public const String WMS_BACK_退厂打印 = "0605";
        public const String WMS_BACK_损溢查询 = "0701";
        public const String WMS_BACK_损溢制单 = "0702";
        public const String WMS_BACK_损溢确认 = "0703";
        public const String WMS_BACK_损溢审核 = "0704";
        public const String WMS_BACK_损溢打印 = "0705";
        public const String WMS_BACK_盘点查询 = "0801";
        public const String WMS_BACK_盘点制单 = "0802";
        public const String WMS_BACK_盘点确认 = "0803";
        public const String WMS_BACK_盘点审核 = "0804";
        public const String WMS_BACK_盘点打印 = "0805";
        public const String WMS_BACK_盘点设置 = "0806";
        public const String WMS_BACK_盘点抄帐 = "0807";
        public const String WMS_BACK_盘点损溢 = "0808";
        public const String WMS_BACK_盘点检查 = "0809";
        public const String WMS_BACK_盘点取消确认 = "0810";        
        public const String WMS_BACK_仓位调整查询 = "0901";
        public const String WMS_BACK_仓位调整制单 = "0902";        
        public const String WMS_BACK_仓位调整审核 = "0904";
        public const String WMS_BACK_仓位调整打印 = "0905";        
        public const String WMS_BACK_内调查询 = "1001";
        public const String WMS_BACK_内调制单 = "1002";
        public const String WMS_BACK_内调确认 = "1003";
        public const String WMS_BACK_内调审核 = "1004";
        public const String WMS_BACK_内调打印 = "1005";
        public const String WMS_BACK_分货查询 = "1101";
        public const String WMS_BACK_分货制单 = "1102";
        public const String WMS_BACK_分货确认 = "1103";
        public const String WMS_BACK_分货审核 = "1104";
        public const String WMS_BACK_分货打印 = "1105";
        public const String WMS_BACK_账务比对 = "9001";
        public const String WMS_BACK_账务生成差异 = "9002";
        public const String WMS_BACK_账务差异导入 = "9003";
        public const String WMS_BACK_报表打印 = "9901";
        public const String WMS_BACK_差异报表查询 = "9902";
        public const String WMS_BACK_库存报表查询 = "9903";
        public const String WMS_BACK_监控报表查询 = "9904";
        public const String WMS_BACK_报表转存 = "9905";
        */
        #endregion 老的设置

        public const String WMS_BACK_收货查询 = "0101";
        public const String WMS_BACK_收货制单 = "0102";
        public const String WMS_BACK_收货确认 = "0103";
        public const String WMS_BACK_收货审核 = "0104";
        public const String WMS_BACK_收货打印 = "0105";
        public const String WMS_BACK_上架查询 = "0201";
        public const String WMS_BACK_上架制单 = "0202";
        public const String WMS_BACK_上架确认 = "0203";
        public const String WMS_BACK_上架审核 = "0204";
        public const String WMS_BACK_上架打印 = "0205";
        public const String WMS_BACK_拣货查询 = "0301";
        public const String WMS_BACK_拣货制单 = "0302";
        public const String WMS_BACK_拣货确认 = "0303";
        public const String WMS_BACK_拣货审核 = "0304";
        public const String WMS_BACK_拣货打印 = "0305";
        public const String WMS_BACK_正常拣货 = "0307";
        public const String WMS_BACK_残损拣货 = "0308";
        public const String WMS_BACK_外销拣货 = "0309";
        public const String WMS_BACK_内调拣货 = "0310";
        public const String WMS_BACK_代发拣货 = "0311";
        public const String WMS_BACK_摘果拣货 = "0312";
        public const String WMS_BACK_播种查询 = "0401";
        public const String WMS_BACK_播种制单 = "0402";
        public const String WMS_BACK_播种确认 = "0403";
        public const String WMS_BACK_播种审核 = "0404";
        public const String WMS_BACK_播种打印 = "0405";
        public const String WMS_BACK_返仓查询 = "0501";
        public const String WMS_BACK_返仓制单 = "0502";
        public const String WMS_BACK_返仓确认 = "0503";
        public const String WMS_BACK_返仓审核 = "0504";
        public const String WMS_BACK_返仓打印 = "0505";
        public const String WMS_BACK_退厂查询 = "0601";
        public const String WMS_BACK_退厂制单 = "0602";
        public const String WMS_BACK_退厂确认 = "0603";
        public const String WMS_BACK_退厂审核 = "0604";
        public const String WMS_BACK_退厂打印 = "0605";
        public const String WMS_BACK_损溢查询 = "0701";
        public const String WMS_BACK_损溢制单 = "0702";
        public const String WMS_BACK_损溢确认 = "0703";
        public const String WMS_BACK_损溢审核 = "0704";
        public const String WMS_BACK_损溢打印 = "0705";
        public const String WMS_BACK_盘点查询 = "0801";
        public const String WMS_BACK_盘点制单 = "0802";
        public const String WMS_BACK_盘点确认 = "0803";
        public const String WMS_BACK_盘点审核 = "0804";
        public const String WMS_BACK_盘点打印 = "0805";
        public const String WMS_BACK_盘点设置 = "0806";
        public const String WMS_BACK_盘点抄帐 = "0807";
        public const String WMS_BACK_盘点损溢 = "0808";
        public const String WMS_BACK_盘点检查 = "0809";
        public const String WMS_BACK_盘点取消确认 = "0810";
        public const String WMS_BACK_仓位调整查询 = "0901";
        public const String WMS_BACK_仓位调整制单 = "0902";
        public const String WMS_BACK_仓位调整确认 = "0903";
        public const String WMS_BACK_仓位调整审核 = "0904";
        public const String WMS_BACK_仓位调整打印 = "0905";
        public const String WMS_BACK_内调查询 = "1001";
        public const String WMS_BACK_内调制单 = "1002";
        public const String WMS_BACK_内调确认 = "1003";
        public const String WMS_BACK_内调审核 = "1004";
        public const String WMS_BACK_内调打印 = "1005";
        public const String WMS_BACK_分货查询 = "1101";
        public const String WMS_BACK_分货制单 = "1102";
        public const String WMS_BACK_分货确认 = "1103";
        public const String WMS_BACK_分货审核 = "1104";
        public const String WMS_BACK_分货打印 = "1105";
        public const String WMS_BACK_跨区调整查询 = "1201";
        public const String WMS_BACK_跨区调整制单 = "1202";
        public const String WMS_BACK_跨区调整确认 = "1203";
        public const String WMS_BACK_跨区调整审核 = "1204";
        public const String WMS_BACK_跨区调整打印 = "1205";
        public const String WMS_BACK_账务比对 = "9001";
        public const String WMS_BACK_账务生成差异 = "9002";
        public const String WMS_BACK_账务差异导入 = "9003";
        public const String WMS_BACK_设定安全库存 = "9801";
        public const String WMS_BACK_报表打印 = "9901";
        public const String WMS_BACK_差异报表查询 = "9902";
        public const String WMS_BACK_库存报表查询 = "9903";
        public const String WMS_BACK_监控报表查询 = "9904";
        public const String WMS_BACK_报表转存 = "9905";
        #endregion
    }
    
}