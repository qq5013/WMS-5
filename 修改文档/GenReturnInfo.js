var folderbase = "D:/øÏ≈Ã/mycode/hongqiapk/services/WMS/WMS/WMS/";
var connstr1 = "driver={sql server};server=192.10.200.1;database=test_newshop;uid=app;pwd=666666";
var connstr3 = "driver={sql server};server=192.10.200.3;database=newshop;uid=app;pwd=666666";
var files = [];

function GetAllFiles(sfld) {    
    var fso = new ActiveXObject("Scripting.FileSystemObject");
    var fld = fso.GetFolder(sfld);      

    var e1 = new Enumerator(fld.Files);
    for (; !e1.atEnd(); e1.moveNext()) {
        var fName = e1.item() + "";
        var m = /\.cs$/.exec(fName);
        if (m != null) {
            files.push(fName);
        }
    }
                                    
    var e2 = new Enumerator(fld.SubFolders);
    for (; !e2.atEnd(); e2.moveNext()) {
        GetAllFiles(e2.item() + "");
    }

}

function ReadFileText(fName) {
    var stm = new ActiveXObject("Adodb.Stream");
    stm.Type = 2;
    stm.Mode = 3;
    stm.charset = "UTF-8";
    stm.open();    
    stm.loadFromFile(fName);
    var s = stm.readText();
    stm.close();
    return s;
}

function WriteFileText(fName, txt) {
    var stm = new ActiveXObject("Adodb.Stream");
    stm.Type = 2;
    stm.Mode = 3;
    stm.charset = "UTF-8";
    stm.open();
    stm.writeText(txt);
    stm.saveToFile(fName, 2);
    stm.flush();
    stm.close();
}

function GetMaxItem(type) {
    var rs = new ActiveXObject("adodb.recordset");
    var sql = "SELECT   TOP (1) setid, setdes, type, typedes, val1, val2, val3,brief, isvld FROM wms_set WHERE   (setid = '996') and val1<>'I486' AND (setdes LIKE '" + type + "%') ORDER BY type DESC";
    rs.open(sql, connstr1);
    var s = rs("val1");
    var m = /(\d+)/.exec(s);

    if (m != null) {
        return type + ("0000" + (parseInt(m[1], 10) + 1)).slice(-4);        
    }
}

function IstItem(type, itemNo,  brief){
    var cmd = new ActiveXObject("adodb.command");    
    var cmdsql = "insert into wms_set(setid, setdes, type, typedes, val1, val2, val3,brief, isvld) "
                + "SELECT   TOP (1) setid, setdes, '" + itemNo + "' type, typedes, '" + itemNo + "' val1, val2, val3, '" + brief + "' brief, isvld FROM wms_set WHERE   (setid = '996') AND (setdes LIKE '" + type + "%') ORDER BY type DESC";
    cmd.ActiveConnection = connstr1;
    cmd.CommandText = cmdsql;
    cmd.execute;

    cmd.ActiveConnection = connstr3;
    cmd.CommandText = cmdsql;
    cmd.execute;
    
    //cmd.execute(cmdsql, connstr3);
}

function FindInfoAndReplace(txt) {
    var rg = /\{\{([ISNE]{1})\:(.*?)\}\}/ig;
    txt = txt.replace(rg, function($0,$1,$2){ 
        var itemNo = GetMaxItem($1);
        IstItem($1, itemNo, $2);
        return itemNo;
    });

    return txt;
}

GetAllFiles(folderbase);
for(var i=0; i<files.length; i++){
    var txt1 = ReadFileText(files[i]);
    var txt = FindInfoAndReplace(txt1);
    if (txt != txt1) {
        WriteFileText(files[i], txt);
    }
}

/***************  ≤‚ ‘«¯”Ú *************************************************************************
var s = files.join(",");                                                                            *
var txt = ReadFileText(files[0]);                                                                   *
//WScript.echo(GetMaxItem("I"));                                                                    *
//WScript.echo( FindInfoAndReplace("fsdfds\r\n{{I:«Î≤‚ ‘“ªœ¬}}\r\n{{s:«Î≤‚ ‘“ªœ¬}}\r\n") );         *
****************************************************************************************************/
