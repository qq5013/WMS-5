using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS.Controllers
{
    public struct 分区部门关联
    {
        //val1-区位,val2-业务,val3-配送
        String qu; 
        String dptid;
        String savdptid;
    }



    /// <summary>
    /// 设置表重新定义
    /// </summary>
    public class WmsSet
    {
        public static String SET_分区部门关联 = "001";
        public static String SET_单据类型 = "002";
        public static String SET_拣货方式 = "003";
        public static String SET_商品类型 = "005";
        public static String SET_区位定义 = "006";
        public static String SET_特殊业务库 = "007";
        public static String SET_仓库定义 = "008";
        public static String SET_比对期间 = "009";
        public static String SET_启用业务库 = "999";
        
    }


}