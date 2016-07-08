/**
 * Created by Administrator on 2016/5/26.
 */

var fso = new ActiveXObject("scripting.filesystemobject");
var fld = fso.GetFolder("D:\\快盘\\mycode\\hongqiapk\\services\\WMS\\WMS\\WMS\\Controllers");
enu = new Enumerator(fld.files);
var code = 471;
var flg = "I";
var RMethod = "RInfo";
var nscode = "";

while(!enu.atEnd()){
	/*	替换未ResultInfo的返回，并生成icode*/
	var fname = enu.item()+"";
	s = getFileStr(fname);
	//var reg = /Rm.ResultCode\s*=\s*ResultMessage.RESULTMESSAGE_INFO;\s*.*?return\s*(?:Rm|ReturnResult\(\))\s*;/ig;
	//var reg = /Rm.ResultCode\s*=\s*ResultMessage.RESULTMESSAGE_NODATA;\s*.*?((?:return\s*(?:Rm|ReturnResult\(\))\s*;)|(?:\}))/ig;
	//var reg = /Rm.ResultCode\s*=\s*ResultMessage.RESULTMESSAGE_ERRORS;\s*.*?((?:return\s*(?:Rm|ReturnResult\(\))\s*;)|(?:\}))/ig;
	//var reg = /(return RInfo\("成功",\s*)(.*?)\s*, "{{succ}}"\);/ig;
	var reg = /return ((?:R|RR)Info)\("([^I]{1}.*?)"\);/ig;
	s = s.replace(/\r\n/ig, "@#");
	
	s = s.replace(reg, function($0,$1,$2){	
				var smatch = $0;				
				//sparms = getParams(smatch);
				
				var scode = ("0000" + code);
				scode = flg + scode.substring( scode.length-4 );	
				code++;
				nscode += scode + " = " + $2 + "\r\n";

				//return "return RRNoData(\"" + scode + "\"" + sparms + ");\r\n";
				//return "return RSucc(ex.Message, \"" + scode + "\");\r\n";
				//return $1 + $2 + ", \"" + scode + "\");\r\n";
				return $1+"(\"" +scode+ "\"); //" + $2
			});
	s = s.replace(/@#/ig, "\r\n");
	writeFileStr(fname, s);	

	enu.moveNext();
}

writeFileStr(flg + "code.txt", nscode );
WScript.echo("完成");

function getDesc(smatch){
	var params = "";
	var reg = /Rm.ResultDesc\s*=\s*([^\r\n;]+)\s*;/ig;
	var m = reg.exec(smatch);
	if(m!=null){
		var desc = m[1];
		return desc;
	}	
	return null;
}

function getParams(smatch){
	var desc = getDesc(smatch);
	var params = "";	
	if(desc!=null){		
		var reg1 = /\+{0,1}\s*([a-zA-Z0-9\[\]\",\s\.\(\)]+)\s*\+{0,1}/ig;
		while( (m1=reg1.exec(desc)) != null){		
			if(m1[1]
				.replace(/\s*/ig,"")
				.replace(/\"\+/ig,"")
				.replace(/\"\,/ig,"")
				.replace(/^\"/ig,"")
				!=""){
				params += " ," + m1[1];
			}
		}
	}	
	return params;
}

function getRetCode(s){
	var reg = /RInfo\(([^\r\n]+)\)/ig;
	while((m=reg.exec(s))!=null){
		WScript.echo(m[1]);
	}
}

function getFileStr(fname){
	var stm = new ActiveXObject("adodb.stream");
	stm.type = 2;
	stm.mode = 3;
	stm.charset = "utf-8";
	stm.open();
	stm.loadfromfile(fname);
	var s = stm.readText();
	stm.close();
	stm = null;
	return s;
}

function writeFileStr(fname, s){
	var stm = new ActiveXObject("adodb.stream");
	stm.type = 2;
	stm.mode = 3;
	stm.charset = "utf-8";
	stm.open();
	stm.writeText(s);
	stm.flush();
	stm.saveToFile(fname+"",2);
	stm.close();
	stm = null;
	return s;
}