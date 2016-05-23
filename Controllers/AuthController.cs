using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using WMS.Common;
using WMS.Models;

namespace WMS.Controllers
{
    /// <summary>
    /// 身份验证服务
    /// </summary>
    public class AuthController : BaseController
    {
        #region  HQ3密码登录
         private static int Asc(string character)//输为ASCII码
        {
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
            int intAsciiCode = (int)asciiEncoding.GetBytes(character)[0];
            return (intAsciiCode);
            
        }
         private static char Chr(int CharCode)  //ASCII转字符
        {
            char ch1;
            if ((CharCode < -32768) || (CharCode > 0xffff))
            {
                //  throw new ArgumentException(Utils.GetResourceString("Argument_RangeTwoBytes1", new string[] { "CharCode" }));
            }
            if ((CharCode >= 0) && (CharCode <= 0x7f))
            {
                return Convert.ToChar(CharCode);
            }
            try
            {
                int num1;
                Encoding encoding1 = Encoding.GetEncoding("gb2312");
                if (encoding1.IsSingleByte && ((CharCode < 0) || (CharCode > 0xff)))
                {
                    //    throw ExceptionUtils.VbMakeException(5);
                }
                char[] chArray1 = new char[2];
                byte[] buffer1 = new byte[2];
                Decoder decoder1 = encoding1.GetDecoder();
                if ((CharCode >= 0) && (CharCode <= 0xff))
                {
                    buffer1[0] = (byte)(CharCode & 0xff);
                    num1 = decoder1.GetChars(buffer1, 0, 1, chArray1, 0);
                }
                else
                {
                    buffer1[0] = (byte)((CharCode & 0xff00) >> 8);
                    buffer1[1] = (byte)(CharCode & 0xff);
                    num1 = decoder1.GetChars(buffer1, 0, 2, chArray1, 0);
                }
                ch1 = chArray1[0];
            }
            catch (Exception exception1)
            {
                throw exception1;
            }
            return ch1;
        }


        protected String DisEncode(String str)
        {
            string response = "";
            string as_pass = str.Trim();
            int li_len = as_pass.Length;
            int ls_passtmp = 0;
            string ls_pass = "";
            if (li_len % 2 == 0)
            {
                for (int i = 0; i < li_len / 2; i++)
                {
                    //MessageBox.Show("i=" + i + ",ASC(" + as_pass.Substring(2 * i, 1).ToString() + ")=" + Asc(as_pass.Substring(2 * i, 1)).ToString());
                    ls_passtmp = (256 - Convert.ToInt16(Asc(as_pass.Substring(2 * i, 1)))) * 256 + (255 - Convert.ToInt16(Asc(as_pass.Substring((2 * i) + 1, 1)))) - 65536;
                    //MessageBox.Show("ls_passtmp=" + ls_passtmp);
                    ls_pass += Chr(ls_passtmp);
                    //MessageBox.Show("i=" + i + ",(char)ls_passtmp=" + (char)ls_passtmp);
                }
                //txtUser.Text = ls_pass;
                return response = ls_pass;
            }
            else
            {
                for (int i = 0; i < (li_len - 1) / 2; i++)
                {
                    ls_passtmp = (256 - Convert.ToInt16(Asc(as_pass.Substring(2 * i, 1)))) * 256 + (255 - Convert.ToInt16(Asc(as_pass.Substring((2 * i + 1), 1)))) - 65536;
                    //MessageBox.Show("ls_passtmp=" + ls_passtmp);
                    ls_pass += Chr(ls_passtmp);
                    //MessageBox.Show("i=" + i + ",(char)ls_passtmp=" + (char)ls_passtmp);
                }
                ls_pass += Chr((256 - Convert.ToInt16(Asc(as_pass.Substring(li_len - 1, 1)))) * 256 + 128 - 65536);
                //txtUser.Text = ls_pass;
                return response = ls_pass;
            }
        }
       
        #endregion

        /// <summary>
        /// 检查是否登录
        /// </summary>
        /// <returns></returns>
        protected Boolean CheckLogin()
        {
            return (UsrId != null && LoginInfo != null);
        }

        /// <summary>
        /// 根据USRID,得到登录信息
        /// </summary>
        /// <param name="UsrId"></param>
        /// <returns></returns>
        protected LoginInfo GetLoginInfoByUsrId(string UsrId)
        {            
            //查询商品库            
            var qrydtapwrs = from a in WmsDc.dtapwr
                             join b in WmsDc.bizdep on a.dptid equals b.savdptid
                             join qu in WmsDc.wms_set on
                                new { dptid = b.dptid, savdptid = b.savdptid } equals new { dptid = qu.val2, savdptid = qu.val3 }
                             join st in WmsDc.wms_set on
                                new { savdptid = qu.val3, storetypeid = WMSConst.SET_TYPE_STORETYPEDEFIND }
                                    equals new { savdptid = st.val1, storetypeid = st.setid }
                             join d in WmsDc.dpt on b.savdptid equals d.dptid
                             join f in WmsDc.dpt on st.val3 equals f.dptid
                             where b.isrs == GetY() && a.empid == UsrId
                             && qu.setid == WMSConst.SET_TYPE_RELATEDPT
                             && qu.val1 != null
                             && st.val2 == "1"      // setid="008" , val2="1" --商品库（商品区、堆头区）,val2="2" --残损库                             
                             && (from ee in WmsDc.dtapwr where ee.empid == UsrId && ee.dptid == b.dptid select 1).Any()
                             select new GetRealteQuResult
                             {
                                 qu = qu.val1,
                                 dptid = b.dptid.Trim(),
                                 savdptid = b.savdptid.Trim(),
                                 savdptdes = d.dptdes.Trim(),
                                 storetypeid = st.val2.Trim(),
                                 savstoreid = st.val3.Trim(),
                                 savstoredes = f.dptdes.Trim()
                             }; 
            //查询残损库
            var qrydta_cs = from a in WmsDc.dtapwr
                            join b in WmsDc.bizdep on a.dptid equals b.savdptid
                            join qu in WmsDc.wms_set on
                               new { dptid = b.dptid, savdptid = b.savdptid } equals new { dptid = qu.val2, savdptid = qu.val3 }
                            join st in WmsDc.wms_set on
                               new { savdptid = qu.val3, storetypeid = WMSConst.SET_TYPE_STORETYPEDEFIND }
                                   equals new { savdptid = st.val1, storetypeid = st.setid }
                            join d in WmsDc.dpt on b.savdptid equals d.dptid
                            join f in WmsDc.dpt on st.val3 equals f.dptid
                            where /*b.isrs == GetY() &&*/ a.empid == UsrId
                            && qu.setid == WMSConst.SET_TYPE_RELATEDPT
                            && qu.val1 != null
                            && st.val2 == "2"      // setid="008" , val2="1" --商品库（商品区、堆头区）,val2="2" --残损库                             
                            && (from ee in WmsDc.dtapwr where ee.empid==UsrId && ee.dptid==b.dptid select 1).Any()
                            select new GetRealteQuResult
                            {
                                qu = qu.val1,
                                dptid = b.dptid.Trim(),
                                savdptid = b.savdptid.Trim(),
                                savdptdes = d.dptdes.Trim(),
                                storetypeid = st.val2.Trim(),
                                savstoreid = st.val3.Trim(),
                                savstoredes = f.dptdes.Trim()
                            };
            /*qrydta_cs = from e in qrydta_cs
                        from e1 in WmsDc.dpt
                        where e1.prtdptid == "998"
                        select new GetRealteQuResult
                        {
                            qu = e.qu,
                            dptid = e1.dptid.Trim(),
                            savdptdes = e.savdptdes,
                            savdptid = e.savdptid,
                            storetypeid = e.storetypeid,
                            savstoredes = e.savstoredes,
                            savstoreid = e.savstoreid
                        };*/
            //堆头区
            var qrydta_dt = from e in WmsDc.dtapwr
                            join d in WmsDc.dpt on e.dptid equals d.dptid
                            join st in WmsDc.wms_set on e.dptid equals st.val1
                            join qu in WmsDc.wms_set on new { savdptid = st.val1, dptid = "all", setid = WMSConst.SET_TYPE_RELATEDPT } equals new { savdptid = qu.val3, dptid = qu.val2, setid = qu.setid }
                            join f in WmsDc.dpt on st.val3 equals f.dptid
                            where st.val2 == "1"            // setid="008" , val2="1" --商品库（商品区、堆头区）,val2="2" --残损库
                            && st.setid == WMSConst.SET_TYPE_STORETYPEDEFIND
                            group new { e, d, st, qu, f } by new { qu = qu.val1, dptid = qu.val2, savdptid=st.val1, 
                                savdptdes = d.dptdes, storetypeid= st.val2, savstoreid= st.val3, savstoredes= f.dptdes } into g
                            select new GetRealteQuResult
                            {
                                qu = g.Key.qu,
                                dptid = g.Key.dptid,
                                savdptid = g.Key.savdptid,
                                savdptdes = g.Key.savdptdes,
                                storetypeid = g.Key.storetypeid,
                                savstoreid = g.Key.savstoreid,
                                savstoredes = g.Key.savstoredes
                            };
            qrydta_dt = from e in qrydta_dt
                        from e1 in WmsDc.dpt
                        where e1.prtdptid == "998"
                        select new GetRealteQuResult
                        {
                            qu = e.qu,
                            dptid = e1.dptid.Trim(),
                            savdptdes = e.savdptdes,
                            savdptid = e.savdptid,
                            savstoredes = e.savstoredes,
                            savstoreid = e.savstoreid
                        };             

            var arrqrydtapwrs = qrydtapwrs.ToArray();
            var arrqrydta_cs = qrydta_cs.ToArray();
            var arrqrydta_dt = qrydta_dt.ToArray();
            //合并商品库和残损库
            var arrqrydtapwrsUnion = arrqrydtapwrs.Union(arrqrydta_cs).Union(arrqrydta_dt);

            GetRealteQuResult[] arrdtapwrs = arrqrydtapwrsUnion.ToArray();
            var qrydtapwrsgroup = from e in arrdtapwrs.AsEnumerable()
                                  group e by new
                                  {
                                      savdptid = e.savdptid != null ? e.savdptid.Trim() : "",
                                      savdptdes = e.savdptdes != null ? e.savdptdes.Trim() : "",
                                      storetypeid = e.storetypeid != null ? e.storetypeid.Trim() : "",
                                      savstoreid = e.savstoreid != null ? e.savstoreid.Trim() : "",
                                      savstoredes = e.savstoredes != null ? e.savstoredes.Trim() : ""
                                  } into g
                                  select new GetRealteQuResult
                                  {
                                      savdptdes = g.Key.savdptdes.Trim(),
                                      savdptid = g.Key.savdptid.Trim(),
                                      storetypeid = g.Key.storetypeid.Trim(),
                                      savstoreid = g.Key.savstoreid.Trim(),
                                      savstoredes = g.Key.savstoredes.Trim()
                                  };

            var arrqrySavStoreids = (from ge in qrydtapwrsgroup
                                         group ge by new { ge.savstoreid, ge.savstoredes } into g
                                         select new Store
                                         {
                                             Storeid = g.Key.savstoreid,
                                              Storedes = g.Key.savstoredes
                                         }).ToArray();
            var qry = from e in this.WmsDc.emp
                      join e1 in WmsDc.dpt on e.dptid equals e1.dptid
                      where e.empid == UsrId
                      && e.isstp == GetN()
                      select new LoginInfo
                      {
                          Usrid = e.empid.Trim(),
                          LoginDtm = DateTime.Now,
                          UsrName = e.empdes.Trim(),
                          EmpPwrs = (from r in WmsDc.emppwr
                                     where r.empid == UsrId
                                     && r.mdlid == "wms_back"
                                     select r
                                          ).ToArray(),
                          DatPwrs = arrdtapwrs,
                          SavDptids = qrydtapwrsgroup.ToArray(),
                          SavStoreids = arrqrySavStoreids
                      };
            LoginInfo li = null;
            var arrqry = qry.ToArray();
            if (arrqry.Length >= 0)
            {
                li = arrqry[0];                
            }
            return li;
        }        

        /// <summary>
        /// 设置默认配送
        /// </summary>
        /// <param name="savdptid"></param>
        /// <returns></returns>
        public ActionResult SetDefSavdptid(String savdptid)
        {
            /*if (!CheckLogin())
            {
                Rm.ResultCode = "-1";
                Rm.ResultDesc = "尚未登录";
                return ReturnResult();
            }*/
            if (LoginInfo == null && Session != null && !string.IsNullOrEmpty((string)Session["usrid"]))
            {
                LoginInfo = GetLoginInfoByUsrId((string)Session["usrid"]);
            }
            Common.LoginInfo li1 = LoginInfo;

            var qry = from e in LoginInfo.SavDptids
                      where e.savstoreid == savdptid
                      select e;
            var qrysp = qry.Where(e => e.storetypeid == "1");
            var qrycs = qry.Where(e => e.storetypeid == "2");
            var arrqry = qry.ToArray();
            var arrqrysp = qrysp.ToArray();
            var arrqrycs = qrycs.ToArray();
            /*if (arrqrysp.Length <= 0)
            {
                return RNoData("未找到默认的商品库");
            }
            if (arrqrycs.Length <= 0)
            {
                return RNoData("未找到默认的残损库");
            }*/
            if (arrqrysp.Length > 0)
            {
                li1.DefSavdptid = arrqrysp[0].savdptid;
                li1.DefSavdptdes = arrqrysp[0].savdptdes;
            }
            if (arrqrycs.Length > 0)
            {
                li1.DefCsSavdptid = arrqrycs[0].savdptid;
                li1.DefCsSavdptdes = arrqrycs[0].savdptdes;
            }
            if (arrqrysp.Length > 0)
            {
                li1.DefStoreid = arrqrysp[0].savstoreid;
                li1.DefStoredes = arrqrysp[0].savstoredes;
            }
            if (arrqrycs.Length > 0)
            {
                li1.DefStoreid = arrqrycs[0].savstoreid;
                li1.DefStoredes = arrqrycs[0].savstoredes;
            }
            Session["defsavdptid"] = li1.DefSavdptid;
            Session["defsavdptdes"] = li1.DefSavdptdes;
            Session["defcssavdptid"] = li1.DefCsSavdptid;
            Session["defcssavdptdes"] = li1.DefCsSavdptdes;
            Session["defstoreid"] = li1.DefStoreid;
            Session["defstoredes"] = li1.DefStoredes;
            Rm.ResultObject = li1;
            //li1.DatPwrs = null;
            //li1.EmpPwrs = null;
            //Rm.ResultObject = GetLoginInfoByUsrId(UsrId);
            return ReturnResult();
        }

        
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="usrid">帐号</param>
        /// <param name="pwd">密码</param>
        /// <returns></returns>
        public ActionResult Login(String usrid, String pwd)
        {
            Logout();

            pwd = DisEncode(pwd);
            var qry = from e in WmsDc.emp
                      where e.empid == usrid
                      && e.pwd == pwd
                      select e;
            var arrqry = qry.ToArray();
            if (arrqry.Length <= 0)
            {
                Rm.ResultCode = ResultMessage.RESULTMESSAGE_INFO;
                Rm.ResultDesc = "登录失败，用户名或密码错误！";
                return this.ReturnResult();
            }

            Session["usrid"] = arrqry[0].empid.Trim();
            UsrId = (String)Session["usrid"];            
            //得到用户信息
            LoginInfo = GetLoginInfoByUsrId(usrid);
            //设置默认配送信息 
            SetDefSavdptid(LoginInfo.SavStoreids[0].Storeid);

            Rm.ResultObject = LoginInfo;
            //LoginInfo.DatPwrs = null;                      
            return this.ReturnResult();
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        /// <returns></returns>
        public ActionResult Logout()
        {
            Session["usrid"] = null;
            UsrId = null;
            LoginInfo = null;

            return this.ReturnResult();
        }


    }
}
