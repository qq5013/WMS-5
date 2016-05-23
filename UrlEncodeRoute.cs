using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Mvc;

namespace WMS
{
    /// <summary>
    /// 权限检查路由
    /// </summary>
    public class UrlEncodeRoute:RouteBase
    {
        /// <summary>
        /// 得到当前路由
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            RouteData ret = null;
            ret = new RouteData(this, new MvcRouteHandler());
            ret.Values.Add("controller", "Auth");
            ret.Values.Add("action", "Login");
            ret.Values.Add("usrid", "usrid=1");
            return ret;
        }

        /// <summary>
        /// 得到虚拟路径
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            return null;
        }
    }
}