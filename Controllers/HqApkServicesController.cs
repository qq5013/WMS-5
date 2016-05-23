using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WMS.Models;
using System.Text;

namespace WMS.Controllers
{
    public class HqApkServicesController : Controller
    {
        /// <summary>
        /// 程序升级地址
        /// </summary>
        /// <param name="apk">程序的名称</param>
        /// <param name="version">程序版本</param>
        public void HqlsAppDn(String apk, String version, String ext)
        {
            ext = ext == null ? "apk" : ext;
            if (Request.ServerVariables["HTTP_USER_AGENT"].IndexOf("MicroMessenger") >= 0)
            {
                Response.ContentType = "text/html";
                String strPath = Server.MapPath("../syapksx.htm");
                Response.WriteFile(strPath);
                Response.End();
            }
            else
            {
                ApkInfo ai = GetNewApk(apk, version, "." + ext);

                if (ai != null)
                {
                    Response.ContentType = "application/octet-stream";
                    Response.AddHeader("Content-Disposition", "attachment; filename=\"" + ai.appname + "\"");
                    Response.BinaryWrite(ai.apk.ToArray());
                }

            }
        }

        private ApkInfo GetNewApk(String apk, String version)
        {
            return GetNewApk(apk, version, ".apk");
        }

        private ApkInfo GetNewApk(String apk, String version, String ext)
        {
            ApkInfo ai = null;
            if (apk == null) { apk = "HQLSApp" + ext; } else { apk += ext; }
            PhoneAppsDataClassesDataContext padc = new PhoneAppsDataClassesDataContext();

            var qry = from e in padc.ApkInfo
                      where e.appname == apk
                      select e;
            if (version != null)
            {
                qry = qry.Where(e1 => e1.versionname == version);
            }
            qry = qry.OrderByDescending(eodrd => eodrd.appname).OrderByDescending(eodrid1 => eodrid1.versioncode).Take(1);
            if (qry != null && qry.Count() > 0)
            {
                ai = qry.ToArray()[0];
            }

            return ai;
        }

        /// <summary>
        /// 得到APK的版本信息
        /// </summary>
        /// <param name="apk">程序的名称</param>
        /// <returns></returns>
        public ActionResult Version(String apk)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version='1.0' encoding='utf-8'?>");
            sb.Append("<info>");

            ApkInfo ai = GetNewApk(apk, null);
            if (ai != null)
            {
                String sBasePath = System.Web.Configuration.WebConfigurationManager.AppSettings["DownUrl"];
                sb.Append("<versioncode>" + ai.versioncode + "</versioncode>");
                sb.Append("<version>" + ai.versionname + "</version>");
                sb.Append("<url><![CDATA[" + sBasePath + RouteData.Values["Controller"] + "/HqlsAppDn?apk=" + ai.appname.Replace(".apk", "") + "&version=" + ai.versionname + "]]></url>");
                sb.Append("<description><![CDATA[检查到新版本，请及时升级]]></description>");
                sb.Append("<debug>");
                foreach (ApkDebugInfo ad in ai.ApkDebugInfo)
                {
                    sb.Append("<item><![CDATA[" + ad.DebugItem.Trim() + "]]></item>");
                }
                sb.Append("</debug>");
                sb.Append("<permissions>");
                foreach (ApkPermission ap in ai.ApkPermission)
                {
                    sb.Append("<permission>" + ap.permission + "</permission>");
                }
                sb.Append("</permissions>");
            }
            sb.Append("</info>");
            return Content(sb.ToString(), "text/xml");
        }

    }
}
