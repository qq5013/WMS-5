var reg = /Rm.ResultCode\s*=\s*ResultMessage.RESULTMESSAGE_INFO;\s*.*?return\s*[^;]*;/ig;
var s = "3Rm.ResultCode = ResultMessage.RESULTMESSAGE_INFO;fsadfsdfasdf;return 233;";
var m = reg.exec(s);
if(m!=null){
	WScript.echo(m);
}
