using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WMS
{
    // 注意: 有关启用 IIS6 或 IIS7 经典模式的说明，
    // 请访问 http://go.microsoft.com/?LinkId=9394801

    /// <summary>
    /// 应用程序类
    /// </summary>
    public class MvcApplication : System.Web.HttpApplication
    {        
        /// <summary>
        /// 注册路由
        /// </summary>
        /// <param name="routes"></param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            
            /*routes.Add("", new UrlEncodeRoute());*/

            routes.MapRoute(
                "Default", // 路由名称
                "{controller}/{action}/{id}", // 带有参数的 URL
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // 参数默认值
            );
                       
        }

        /// <summary>
        /// 应用起始接口
        /// </summary>
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();     
            
            RegisterRoutes(RouteTable.Routes);
        }
    }
}