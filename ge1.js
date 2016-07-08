/**
 * Created by Administrator on 2016/5/26.
 */

var fso = new ActiveXObject("scripting.filesystemobject");
var fld = fso.GetFolder("D:\\øÏ≈Ã\\mycode\\hongqiapk\\services\\WMS\\WMS\\WMS\\Controllers");
enu = new Enumerator(fld.files);
var code = 1;
var flg = "I";
var RMethod = "RInfo";
var nscode = "";

var fcodename = flg+"code.txt";
var fcodestr = getFileStr(fcodename);
var arrLn = fcodestr.split("\n");

	
while(!enu.atEnd()){
	var fname = enu.item()+"";
	s = getFileStr(fname);		
			
	for(var i=0; i<arrLn.length; i++){
		var sLn = arrLn[i];
		var kv = sLn.split("=");
		if(kv.length>1){
			k = kv[0].replace(/\s*$/,"");
			v = kv[1].replace(/\s*$/,"");
					
			var reg1 = /(?:^\s*([^\"\+]+)\s\+\s*\")|(?:\"\s*\+\s*([^\+\"]*)\s*\+\s*\")|(?:\"\s*\+\s*(.+)$)/ig;		
			var params = "";
			while( ( m=reg1.exec(v) ) != null )
			{
				params += "," + ( m[1]=="" ? (m[2]=="" ? (m[3]=="" ? "" : m[3]) : m[2]) : m[1] );
			}
			
			var reg = new RegExp( RMethod + "\\(([^\\r\\n,]+),\\s*(\\\"" + k + "\\\")\\s*\\);", "ig");
			s = s.replace(reg, RMethod + "( \"" + k + "\"" + params + " );");			
		}
	}
	//WScript.echo(s);
	writeFileStr(fname, s);		

	enu.moveNext();
}

WScript.echo("ÕÍ≥…");

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