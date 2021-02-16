using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using Microsoft.CSharp;

using Volte.Data.Json;
using Volte.Data.Dapper;
using Volte.Bot.Tpl;
using Volte.Utils;

namespace Volte.Bot.Term
{
    public class AutoTemplate {

        const string ZFILE_NAME = "AutoTemplate";

        private static Dictionary<string, object> _Data            = new Dictionary<string, object>();
        private static Dictionary<string, string> _Fields          = new Dictionary<string, string>();
        private static Dictionary<string, string> _Context         = new Dictionary<string, string>();
        private static Dictionary<string, bool>   _TableHasColumn  = new Dictionary<string, bool>();
        private static Dictionary<string, string> _TableColumnType = new Dictionary<string, string>();
        private static Dictionary<string, string> _FunctionColumn  = new Dictionary<string, string>();
        private static Dictionary<string, bool>   _HasBoolean      = new Dictionary<string, bool>();
        private static Dictionary<string, bool>   _HasProcess      = new Dictionary<string, bool>();
        private static Dictionary<string, int>    _UsageRegion     = new Dictionary<string, int>();

        private string  _Template   = "";
        private string  _OutputFile = "";
        private string  _sUID       = "";
        private string  _debugMode  = "";
        private DbContext _Trans;
        private Templates _Templates = new Templates();
        private VoltEngine _Tmpl     = VoltEngine.Parser("");
        private Substitute _Substitute=new Substitute();

        public AppSettings AppSetting;
        public AppConfigs AppConfigs;

        public string DebugMode  { get { return _debugMode;  } set { _debugMode  = value; }  }
        public string Template   { get { return _Template;   } set { _Template   = value; }  }
        public string OutputFile { get { return _OutputFile; } set { _OutputFile = value; }  }
        public string sUID       { get { return _sUID;       } set { _sUID       = value; }  }

        public AutoTemplate()
        {
        }

        public void Initialize()
        {
            _Fields        = new Dictionary<string , string>();
            _Data          = new Dictionary<string , object>();
        }

        public Dictionary<string, int> UsageRegion()
        {
            return _UsageRegion;
        }

        public DbContext Trans
        {
            get {
                if (_Trans==null){
                    _Trans = new DbContext(AppConfigs.GetValue("sDbName") , AppConfigs.GetValue("Provider") , AppConfigs.GetValue("dbAdapter"));
                }
                return _Trans;
            }
        }

        public void AddColumn(string name)
        {
            _Fields[name.ToLower()] = name;
        }

        public void SetValue(string name, object value)
        {
            _Tmpl.SetValue(name , value);
            _Data[name.ToLower()] = value;
        }

        object GetValue(string sName)
        {
            if (_Tmpl.IsDefined(sName)){
                return _Tmpl.GetValue(sName);
            }else{
                return "";
            }
        }

        internal object GetSetting(string _name)
        {
            if (_Data.ContainsKey(_name.ToLower())) {
                return _Data[_name.ToLower()];
            } else {
                return "";
            }
        }

        string HasViewColumn(object[] args)
        {
            string sColumnName = args[0].ToString();
            string sUID = args[1].ToString();

            JSONObject _JSONObject = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\define\functions\"+sUID+".js");

            string sKey = sUID+"."+sColumnName;
            string rtv  = "";

            if (args.Length > 2) {
                string sTableName = args[2].ToString();
                sKey=sUID+"."+sTableName+"."+sColumnName;
            }
            sKey = sKey.ToLower();

            if (_FunctionColumn.ContainsKey(sKey)) {
                rtv = _FunctionColumn[sKey];
            } else {

                JSONArray _JSONArray = _JSONObject.GetJSONArray("entitys");

                foreach (JSONObject _j in _JSONArray.JSONObjects) {
                    string s = _j.GetValue("sTableName") + "." + _j.GetValue("sColumnName");
                    s = s.ToLower();
                    _FunctionColumn[s]="false";
                }
                if (_FunctionColumn.ContainsKey(sKey)) {
                    rtv = _FunctionColumn[sKey];
                }else{
                    rtv="false";
                }
            }

            return rtv;
        }

        string TableColumnName(object[] args)
        {
            string sTableName  = args[0].ToString();
            string nSequency   = args[1].ToString();
            string sColumnName = "";

            QueryRows RsSysTables = new QueryRows(this.Trans);

            RsSysTables.CommandText = "SELECT * FROM systables WHERE bActive<>0 AND sTableName='" + sTableName + "'";

            RsSysTables.Open();

            if (!RsSysTables.EOF) {
                if (nSequency == "1") {
                    sColumnName = RsSysTables.GetValue("sColumnName01");
                } else if (nSequency == "2") {
                    sColumnName = RsSysTables.GetValue("sColumnName02");
                } else if (nSequency == "3") {
                    sColumnName = RsSysTables.GetValue("sColumnName03");
                } else if (nSequency == "4") {
                    sColumnName = RsSysTables.GetValue("sColumnName04");
                } else if (nSequency == "5") {
                    sColumnName = RsSysTables.GetValue("sColumnName05");
                } else if (nSequency == "6") {
                    sColumnName = RsSysTables.GetValue("sColumnName06");
                } else if (nSequency == "7") {
                    sColumnName = RsSysTables.GetValue("sColumnName07");
                } else if (nSequency == "8") {
                    sColumnName = RsSysTables.GetValue("sColumnName08");
                } else if (nSequency == "9") {
                    sColumnName = RsSysTables.GetValue("sColumnName09");
                } else if (nSequency == "10") {
                    sColumnName = RsSysTables.GetValue("sColumnName10");
                } else if (nSequency == "11") {
                    sColumnName = RsSysTables.GetValue("sColumnName11");
                } else if (nSequency == "12") {
                    sColumnName = RsSysTables.GetValue("sColumnName12");
                }

                QueryRows RsSysFields = new QueryRows(this.Trans);
                RsSysFields.CommandText = "SELECT * FROM sysfields WHERE sTableName='" + sTableName + "' AND sColumnName='" + sColumnName + "'";
                RsSysFields.Open();

                if (RsSysFields.EOF) {
                    sColumnName = "";
                }

                RsSysFields.Close();

            }

            return sColumnName;
        }

        string HasColumn(object[] args)
        {
            string name = args[0].ToString();
            string sUID = args[1].ToString();

            QueryRows RsZUCOLUTM = new QueryRows(this.Trans);

            if (args.Length > 2) {
                string sTableName = args[2].ToString();
                RsZUCOLUTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE bActive<>0 AND sUID='" + sUID + "' AND sTableName='" + sTableName + "' AND sColumnName='" + name + "'";
            } else {
                RsZUCOLUTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE bActive<>0 AND sUID='" + sUID + "' AND sColumnName='" + name + "'";
            }

            RsZUCOLUTM.Open();

            if (!RsZUCOLUTM.EOF) {
                return "True";
            } else {
                return "False";
            }
        }

        object IgnoreCopyColumn(object[] args)
        {
            string sColumnName  = args[0].ToString();

            JSONObject _JSONObject= AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\appsettings\IgnoreCopyColumn.js");
            return  _JSONObject.GetBoolean(sColumnName).ToString();
        }

        object FiltersWith(object[] args)
        {
            string s1 = args[0].ToString();
            string a1 = args[1].ToString();
            string[] sep = new string[1];

            if (args.Length == 3) {
                sep = new string[1];
                sep[0] = args[2].ToString();

            } else if (args.Length == 4) {
                sep = new string[2];
                sep[0] = args[2].ToString();
                sep[1] = args[3].ToString();
            } else if (args.Length == 5) {
                sep = new string[3];
                sep[0] = args[2].ToString();
                sep[1] = args[3].ToString();
                sep[2] = args[4].ToString();
            }

            string[]  array = a1.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            int i = 1;

            foreach (string a in array) {

                string ss = a.Replace("[[" , "");
                ss = ss.Replace("]]" , "");
                ss = ss.Replace("[" , "");
                ss = ss.Replace("]" , "");

                s1 = s1.Replace("<r" + i + ">", ss);
                s1 = s1.Replace("\\n" , "\n");
                i++;
            }

            return s1;
        }
        object TableHasColumn(object[] args)
        {

            string sTableName = args[0].ToString();
            string name       = args[1].ToString();
            string key        = sTableName + "." + name;
            bool   rtv        = false;

            key = key.ToLower();

            if (_TableHasColumn.ContainsKey(key)) {
                rtv = _TableHasColumn[key];
            } else {


                TableUtil _TableUtil = new TableUtil();

                List<JSONObject> aryColumns =  _TableUtil.DatabaseTableColumns(this.Trans , sTableName);

                foreach (JSONObject colname in aryColumns) {

                    string s = colname.GetValue("sTableName") + "." + colname.GetValue("sColumnName");
                    s = s.ToLower();

                    _TableHasColumn[s] = true;

                }

                if (_TableHasColumn.ContainsKey(key)) {
                    rtv = true;
                }else{
                    _TableHasColumn[key] = false;
                }
            }

            return rtv;
        }

        public string TableColumnType(string sTableName, string sColumnName)
        {

            string key = sTableName + "." + sColumnName;
            string rtv = "nvarchar";

            key = key.ToLower();

            if (_TableColumnType.ContainsKey(key)) {
                rtv = _TableColumnType[key];
            } else {

                TableUtil _TableUtil = new TableUtil();
                List<JSONObject> aryColumns =  _TableUtil.DatabaseTableColumns(this.Trans , sTableName);

                JSONObject _obj=AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\AppSettings\DataType.js");
                foreach (JSONObject colname in aryColumns) {

                    string s = colname.GetValue("sTableName") + "." + colname.GetValue("sColumnName");
                    s = s.ToLower();

                    string sDataType=_obj.GetValue(colname.GetValue("sDataType"));
                    if (string.IsNullOrEmpty(sDataType)){
                        sDataType=colname.GetValue("sDataType");
                    }
                    _TableColumnType[s] = sDataType;

                }

                if (_TableColumnType.ContainsKey(key)) {
                    rtv = _TableColumnType[key];
                }

                _TableColumnType[key] = rtv;
            }

            return rtv;
        }

        private string ColumnDataType(string sUID , string sTableName , string sColumnName)
        {

            string key       = "ColumnDataType_"+sUID + "_" + sTableName+"_"+sColumnName;
            key              = key.ToLower();
            string sTypeCode = "";

            if (_TableColumnType.ContainsKey(key)) {
                sTypeCode = _TableColumnType[key];
            } else {

                QueryRows RsSysFunctionDtl = new QueryRows(this.Trans);

                RsSysFunctionDtl.CommandText = "SELECT * FROM sysfunctiondtl WHERE sUID='" + sUID + "' AND sColumnName='" + sColumnName + "'";
                RsSysFunctionDtl.Open();
                if (!RsSysFunctionDtl.EOF){
                    sTypeCode = RsSysFunctionDtl.GetValue("sDataType");

                    if (string.IsNullOrEmpty(sTypeCode)){
                        sTypeCode = TableColumnType(RsSysFunctionDtl.GetValue("sTableName") , sColumnName);
                    }
                }
                RsSysFunctionDtl.Close();
                _TableColumnType[key] = sTypeCode;

            }
            return sTypeCode;
        }

        object HasLNKColumn(object[] args)
        {

            string name      = args[0].ToString();
            string sUID = args[1].ToString();
            string key       = sUID + "_" + name;
            key              = key.ToLower();
            bool rtv         = false;

            if (_HasBoolean.ContainsKey(key)) {
                rtv = _HasBoolean[key];
            } else {

                object[] _args = new object[1];

                _args[0] = sUID;

                string _LNKUID = TOP_UID_CODE(_args);

                if (_LNKUID == sUID) {
                    return false;
                }

                QueryRows RsZUCOLUTM = new QueryRows(this.Trans);

                RsZUCOLUTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE bActive<>0 AND sUID='" + _LNKUID + "' AND sColumnName='" + name + "'";
                RsZUCOLUTM.Open();

                rtv = (!RsZUCOLUTM.EOF);

                RsZUCOLUTM.Close();
                _HasBoolean[key] = rtv;

            }

            return rtv;
        }

        string FunctionActive(object[] args)
        {

            string sUID = args[0].ToString();

            JSONObject _JSONObject= AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\define\functions\"+sUID+".js");
            return  _JSONObject.GetValue("bActive");
        }

        string FunctionColumnName(object[] args)
        {
            string sUID       = args[0].ToString();
            JSONObject _JSONObject = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\define\functions\"+sUID+".js");
            return  _JSONObject.GetValue("sColumnName");
        }

        string getHash(object[] args)
        {

            string sUID       = args[0].ToString();
            JSONObject _JSONObject = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\define\functions\"+sUID+".js");
            return  _JSONObject.GetValue("sHash");
        }

        string FunctionTableName(object[] args)
        {
            string sUID       = args[0].ToString();
            JSONObject _JSONObject = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\define\functions\"+sUID+".js");
            return  _JSONObject.GetValue("sTableName");
        }

        public string TOP_UID_CODE(object[] args)
        {
            string sUID       = args[0].ToString();
            JSONObject _JSONObject = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\define\functions\"+sUID+".js");
            if (_JSONObject.GetValue("sTopUID_CODE")=="")
            {
                return sUID;
            }else{
                return _JSONObject.GetValue("sTopUID_CODE");
            }
        }

        string DbType(object[] args)
        {
            string dataType = args[0].ToString();

            string sValue = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\appsettings\DbType.js").GetValue(dataType);
            if (sValue==""){
                return "undefine"+dataType;
            }else{
                return sValue;
            }
        }

        string SqlDataTypeToDataType(object[] args)
        {

            string dataType = args[0].ToString();

            string sValue = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\appsettings\SqlDataTypeToDataType.js").GetValue(dataType);
            if (sValue==""){
                return "undefine-"+dataType;
            }else{
                return sValue;
            }
        }

        string StringToDataType(object[] args)
        {

            string dataType = args[0].ToString();

            string sValue = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\appsettings\StringToDataType.js").GetValue(dataType);
            if (sValue==""){
                return "undefine"+dataType;
            }else{
                return sValue;
            }
        }

        string DataTypeDefault(object[] args)
        {

            string dataType = args[0].ToString();

            string sValue = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\appsettings\DataTypeDefault.js").GetValue(dataType);
            if (sValue==""){
                return "=\"" + dataType + "\"";
            }else{
                return sValue;
            }
        }

        string SetInitializeValue(string varName , string sTypeCode)
        {
            if (sTypeCode == "decimal") {
                return varName + " = 0;";
            } else if (sTypeCode == "datetime") {
                return varName + " = DateTime.MinValue;";
            } else {
                return varName + " = \"\";";
            }
        }

        string SetAccessValue(string varName , string sTypeCode , string sColumnName)
        {
            if (sTypeCode == "decimal") {
                return varName + ".GetDecimal(\"" + sColumnName + "\");";
            } else if (sTypeCode == "datetime") {
                return varName + ".GetDateTime(\"" + sColumnName + "\");";
            } else {
                return varName + ".GetValue(\"" + sColumnName + "\");";
            }
        }

        string DefineColumn(object[] args)
        {

            string tableName   = args[0].ToString();
            string columnName  = args[1].ToString();
            string captionCode = "" ;
            string type        = "nvarchar" ;
            int    scale       = -1 ;

            if (args.Length > 2) {
                captionCode = args[2].ToString();
            }

            if (args.Length > 3) {
                type = args[3].ToString();
            }

            if (args.Length > 4) {
                int.TryParse(args[4].ToString() , out scale);
            }

            if (string.IsNullOrEmpty(captionCode)) {
                if ( tableName =="VARIABLE"){
                    captionCode = "lbl_" + columnName;
                } else{
                    captionCode = "lbl_" + tableName + "_" + columnName;
                }
            }

            int    nColumnLength      = 10;
            int    nColumnScale       = 2;
            string _NonPrintable      = "false";
            string sAlignColumnName = "";
            string sEnableMode      = "";
            string sDataBand          = "";
            string rtv                = "";

            string key = tableName + "_" + columnName + "_" + type + "_" + captionCode + "_" + scale;
            JSONObject _DataTypeChar = AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\appsettings\DataTypeChar.js");

            if (captionCode.IndexOf("=") > 0) {
                captionCode = captionCode + ",";
            }

            if (captionCode.IndexOf(";") > 0 || captionCode.IndexOf(",") > 0) {
                string[] sName  = captionCode.Split(new char[]{';',','});
                captionCode = "";

                foreach (string s in sName) {

                    string ss = s;

                    if (s.IndexOf("=") > 0) {

                        int _Position = s.IndexOf("=");
                        string cName  = s.Substring(0 , _Position);
                        string cValue = s.Substring(_Position + 1, s.Length - _Position - 1);

                        cValue = cValue.Trim();
                        cName  = cName.Trim();

                        if (cName.ToLower() == "captioncode") {

                            captionCode = cValue;
                        } else if (cName.ToLower() == "type") {

                            type = cValue;
                        } else if (cName.ToLower() == "length") {

                            int _P = cValue.IndexOf(".");
                            int l  = 0;
                            int _s = 0;

                            if (_P > 0) {
                                l  = DapperUtil.ToInt(cValue.Substring(0 , _P));
                                scale = DapperUtil.ToInt(cValue.Substring(_P + 1, cValue.Length - _P - 1));

                                if (l > 1) {
                                    nColumnLength = l;

                                }
                            } else {
                                l = DapperUtil.ToInt(cValue);

                                if (l > 1) {
                                    nColumnLength = l;
                                }
                            }
                        } else if (cName.ToLower() == "databand" || cName.ToLower() == "sdataband") {

                            sDataBand = cValue;
                        } else if (cName.ToLower() == "saligntocolumnname") {

                            sAlignColumnName = cValue;
                        } else if (cName.ToLower() == "enablemode") {
                            sEnableMode = cValue;
                        } else if (cName.ToLower() == "nonprintable") {

                            if (cValue.ToLower() == "true") {
                                _NonPrintable = "true";
                            } else {
                                _NonPrintable = "false";
                            }

                        }

                    }
                }
            }

            if (string.IsNullOrEmpty(captionCode)) {
                if ( tableName =="VARIABLE"){
                    captionCode = "lbl_" + columnName;
                } else{
                    captionCode = "lbl_" + tableName + "_" + columnName;
                }
            }

            if (_Context.ContainsKey(key)) {

                rtv = _Context[key];

            } else {
                StringBuilder XObject = new StringBuilder();

                QueryRows RsZUCOLUTM = new QueryRows(this.Trans);

                RsZUCOLUTM.CommandText = "SELECT * FROM sysfields WHERE sTableName='" + tableName + "' AND sColumnName='" + columnName + "'";
                RsZUCOLUTM.Open();

                if (!RsZUCOLUTM.EOF) {
                    type                = RsZUCOLUTM.GetValue("sDataType");
                    nColumnLength       = RsZUCOLUTM.GetInteger("nColumnLength");
                    nColumnScale        = RsZUCOLUTM.GetInteger("nColumnScale");
                    string sColumnClass = RsZUCOLUTM.GetValue("sColumnClass");

                    if (nColumnScale < 0) {
                        JSONObject _JSONObject2= AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\appsettings\ColumnScale.js");

                        if (_JSONObject2.ContainsKey(sColumnClass)){
                            nColumnScale =_JSONObject2.GetInteger(sColumnClass);
                        }
                    }
                } else {
                    if (tableName != "ZZFields" && tableName != "VARIABLE") {
                        JSONObject _JSONObject2= AppSetting.LoadJSONObject(AppConfigs.GetValue("DevelopPath")+@"\appsettings\AppSettings.js");
                        Console.WriteLine();
                        Console.WriteLine("*************************");
                        Console.WriteLine(tableName + "." + columnName + " Is Invalid , Please Use Table Name [Variable].");
                        Console.WriteLine("*************************");

                        if (_JSONObject2.GetBoolean("VARIABLE")){
                            return "";
                        }
                    }
                }

                RsZUCOLUTM.Close();

                if (tableName == "ZZFields" && columnName == "CONT_DATA") {

                    _NonPrintable = "true";
                }

                if (string.IsNullOrEmpty(type)) {
                    type = "nvarchar";
                }

                if (nColumnLength==0) {
                    nColumnLength=8;
                }
                if (scale >= 0 && type == "decimal") {
                    nColumnScale = scale;
                }

                int _fontPixe = 12;
                string _name  = "_" + tableName + "_" + columnName;
                string sSqlCode = "Q" + GetSetting("sUID");

                XObject.AppendLine("AttributeMapping " + _name + " = new AttributeMapping();");

                XObject.AppendLine(_name + ".TableName      = \"" + tableName + "\";");
                XObject.AppendLine(_name + ".ColumnName     = \"" + columnName + "\";");
                XObject.AppendLine(_name + ".Name           = \"" + tableName + "_" + columnName + "\";");
                XObject.AppendLine(_name + ".Caption        = _Captions.Caption(\"" + captionCode + "\" , true);");
                XObject.AppendLine(_name + ".Width          = " + nColumnLength + ";");
                XObject.AppendLine(_name + ".DataType   = \"" + type + "\";");
                if (sEnableMode!=""){
                    XObject.AppendLine(_name + ".EnableMode   = \"" + sEnableMode + "\";");
                }
                XObject.AppendLine(_name + "[\"sDataBand\"] = \"" + sDataBand + "\";");
                XObject.AppendLine(_name + ".Scale          = " + nColumnScale + ";");
                XObject.AppendLine(_name + ".NonPrintable   = " + _NonPrintable + ";");
                XObject.AppendLine(_name + ".TypeChar       = \"" + _DataTypeChar.GetValue(type) + "\";");

                XObject.AppendLine("_JSONTable.Declare(_SQLStatement.Process(" + _name + " , \"" + sSqlCode + "\" , entity.DataOption));");


                if (!(tableName.ToLower() == "zzfields" && columnName.ToLower() == "cont_data")) {

                    if (!_HasProcess.ContainsKey("SQL_" + sSqlCode)) {
                        _HasProcess["SQL_" + sSqlCode] = true;
                        this.Trans.Execute("Delete From sysdatadtl Where sSqlCode='" + sSqlCode + "' AND sChgFlag='A'");
                    }

                    QueryRows _SysDataDtl  = new QueryRows(this.Trans);
                    _SysDataDtl.CommandText = "SELECT * From sysdatadtl Where sSqlCode='" + sSqlCode + "' AND sTableName='" + tableName + "' AND sColumnName='" + columnName + "'";
                    _SysDataDtl.Open();
                    string sSQLString = "";

                    if (_SysDataDtl.EOF) {
                        sSQLString = "INSERT INTO sysdatadtl ([sSqlCode] , [sKey] , [sTableName] , [sColumnName] , [bActive] , [nSequency] , [sDataType] , [nColumnLength] , [nColumnScale] , [sCaptionCode],sChgFlag)";

                        sSQLString = sSQLString + "VALUES (N'" + sSqlCode + "' , N'" + IdGenerator.NewBase36("_") + "' , N'" + tableName + "' , N'" + columnName + "' , '1' , '99999' , N'' , NULL , '0' , N'','A');";

                        this.Trans.Execute(sSQLString);

                    }

                    sSQLString = "UPDATE sysdatadtl Set sDataBand='" + sDataBand + "' Where sSqlCode='" + sSqlCode + "' AND sTableName='" + tableName + "' AND sColumnName='" + columnName + "'";
                    this.Trans.Execute(sSQLString);

                    sSQLString = "UPDATE sysdatadtl Set sAlignColumnName='" + sAlignColumnName + "' Where sSqlCode='" + sSqlCode + "' AND sTableName='" + tableName + "' AND sColumnName='" + columnName + "'";
                    this.Trans.Execute(sSQLString);
                }


                _Context[key] = XObject.ToString();
                rtv = XObject.ToString();
            }

            return rtv;
        }

        object HasRegion(object[] args)
        {
            string sUID = args[0].ToString();
            string sName     = args[1].ToString();
            return _Templates.HasRegion(sUID , sName);
        }

        public string Snapshot(object[] args)
        {
            string sUID   = args[0].ToString();
            string sParentUID  = "";
            string sParentName = sUID;

            if (args.Length > 1) {
                sParentUID  = args[1].ToString();
                sParentName = args[1].ToString();
            } else {
                sParentName = "";
                sParentUID  = "";
                _HasProcess = new Dictionary<string, bool>();
            }

            QueryRows _SysFunction = new QueryRows(this.Trans);
            List<string> aUID_CODE = new List<string>();

            ZZLogger.Debug(ZFILE_NAME , sUID);

            object[] _args = new object[2];

            _args[0] = sUID;
            _args[1] = sParentUID;

            string _LNK_UID_CODE = TOP_UID_CODE(_args);

            StringBuilder XObject = new StringBuilder();

            _SysFunction.CommandText = "SELECT * FROM sysfunction WHERE bActive<>0 AND sUID='" + sUID + "' ORDER By sUID , sTableName";
            _SysFunction.Open();

            if (!_SysFunction.EOF) {

                string sTableName  = _SysFunction.GetValue("sTableName");
                string sColumnName = _SysFunction.GetValue("sColumnName");
                string _UID_CODE   = _SysFunction.GetValue("sUID");
                string Name        = _UID_CODE;

                if (_HasProcess.ContainsKey(_UID_CODE)) {

                } else {

                    _HasProcess[_UID_CODE] = true;

                    _args[0] = sUID;
                    _args[1] = sParentName;

                    string SQLString = SnapshotSQLString(_UID_CODE , sParentName);

                    if (SQLString != "") {

                        XObject.AppendLine("");
                        XObject.AppendLine("JSONArray _" + Name + "Entitys = new JSONArray();");
                        XObject.AppendLine("QueryRows Rs" + Name + "= new QueryRows(Trans);");
                        XObject.AppendLine("Rs" + Name + ".CommandText = \"" + SQLString + "\";");

                        XObject.AppendLine("Rs" + Name + ".Open();");
                        XObject.AppendLine("while (!Rs" + Name + ".EOF) {");
                        XObject.AppendLine("    JSONObject _" + Name + "Entity = new JSONObject();");

                        Dictionary<string, bool> _HasColumn = new Dictionary<string, bool>();

                        _HasColumn[sColumnName.ToLower()] = true;

                        XObject.AppendLine(SnapshotColumnName(Name , sTableName , sColumnName));

                        QueryRows RsZUPRGDTM = new QueryRows(this.Trans);

                        if (_LNK_UID_CODE==sUID){
                            RsZUPRGDTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE bActive<>0 AND sUID='" + sUID + "'";
                        }else{
                            RsZUPRGDTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE bActive<>0 AND sUID='" + sUID + "' AND sTableName='" + sTableName + "'";
                        }
                        RsZUPRGDTM.Open();

                        while (!RsZUPRGDTM.EOF) {
                            sColumnName        = RsZUPRGDTM.GetValue("sColumnName");
                            string sTableName2 = RsZUPRGDTM.GetValue("sTableName");

                            if (!_HasColumn.ContainsKey(sColumnName.ToLower())){
                                XObject.AppendLine(SnapshotColumnName(Name , sTableName2 , sColumnName));
                            }

                            _HasColumn[sColumnName.ToLower()] = true;

                            RsZUPRGDTM.MoveNext();
                        }

                        RsZUPRGDTM.Close();

                        QueryRows _SysFunction2 = new QueryRows(this.Trans);

                        _SysFunction2.CommandText = "SELECT * FROM sysfunction WHERE bActive<>0 AND sLNKUID='" + sUID + "' ORDER By sUID , sTableName";
                        _SysFunction2.Open();

                        while (!_SysFunction2.EOF) {

                            object[] _args2 = new object[2];

                            _args2[0] = _SysFunction2.GetValue("sUID");
                            _args2[1] = _SysFunction2.GetValue("sLNKUID");

                            //XObject.AppendLine(SnapshotUID_CODE(_args2).ToString());
                            _SysFunction2.MoveNext();
                        }

                        _SysFunction2.Close();

                        XObject.AppendLine("   _" + Name + "Entitys.Add(_" + Name + "Entity);");

                        XObject.AppendLine("    Rs" + Name + ".MoveNext();");
                        XObject.AppendLine("}");
                        XObject.AppendLine("Rs" + Name + ".Close();");

                        if (sParentName != "") {
                            XObject.AppendLine("_" + sParentName + "Entity.SetValue(\"" + sUID + "\",_" + Name + "Entitys);");
                        } else {
                            XObject.AppendLine("_Journal.SetValue(\"" + sUID + "\",_" + Name + "Entitys);");
                        }

                        XObject.AppendLine("");
                    }
                }

            }
            _SysFunction.Close();

            return XObject.ToString();

        }

        string SnapshotColumnName(string Name , string sTableName , string sColumnName)
        {

            StringBuilder _Code = new StringBuilder();

            if (sColumnName.ToLower()=="schgflag") {
                return _Code.ToString();
            }

            QueryRows RsSysFields = new QueryRows(this.Trans);
            RsSysFields.CommandText = "SELECT * FROM sysfields WHERE bColumnUsage<>0 AND sTableName='" + sTableName + "' AND sColumnName='" + sColumnName + "'";
            RsSysFields.Open();

            if (RsSysFields.EOF) {

                _Code.AppendLine("          _" + Name + "Entity[\"" + sColumnName + "\"] = entity." + sColumnName + ";");

            }else{

                string _ColumnType = RsSysFields.GetValue("sDataType");
                string sName       = RsSysFields.GetValue("sTableName")+"_"+RsSysFields.GetValue("sColumnName");

                if (_ColumnType == "datetime") {

                    _Code.AppendLine(" _" + Name + "Entity.SetValue(\"" + sName + "\" , Rs" + Name + ".GetValue(\"" + sColumnName + "\"));");

                } else if (_ColumnType == "decimal") {

                    _Code.AppendLine(" _" + Name + "Entity.SetDecimal(\"" + sName + "\" , Rs" + Name + ".GetDecimal(\"" + sColumnName + "\"));");

                } else if (_ColumnType == "int") {

                    _Code.AppendLine(" _" + Name + "Entity.SetInteger(\"" + sName +  "\" , Rs" + Name + ".GetInteger(\"" + sColumnName + "\"));");

                } else if (_ColumnType == "ntext" || _ColumnType == "nvarchar") {

                    _Code.AppendLine(" _" + Name + "Entity.SetValue(\"" + sName + "\" , Rs" + Name + ".GetValue(\"" + sColumnName + "\"));");

                } else {

                    _Code.AppendLine(" _" + Name + "Entity[\"" + sName + "\"] = Rs" + Name + "[\"" + sColumnName + "\"];");

                }

            }

            return _Code.ToString();
        }

        string SnapshotSQLString(string sUID , string sParentName)
        {

            string sTableName      = "";
            string sColumnName     = "";
            string LNK_COLUMN_NAME = "";
            string SQLString       = "";

            QueryRows _SysFunction = new QueryRows(this.Trans);

            _SysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID='" + sUID + "'";
            _SysFunction.Open();

            if (!_SysFunction.EOF) {
                sTableName  = _SysFunction.GetValue("sTableName");
                sColumnName = _SysFunction.GetValue("sColumnName");
            }
            _SysFunction.Close();

            QueryRows RsZUPRGDTM = new QueryRows(this.Trans);
            RsZUPRGDTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE bActive<>0 AND sUID='" + sUID + "' AND sTableName='" + sTableName + "' AND LNK_COLUMN_NAME<>''";
            RsZUPRGDTM.Open();

            if (!RsZUPRGDTM.EOF) {
                LNK_COLUMN_NAME = RsZUPRGDTM.GetValue("LNK_COLUMN_NAME");
                //sColumnName     = RsZUPRGDTM.GetValue("sColumnName");
            }

            RsZUPRGDTM.Close();

            if (sTableName != "" && sColumnName != "") {
                // if (LNK_COLUMN_NAME != "") {
                //     if (sParentName != "") {
                //         SQLString = "SELECT * FROM " + sTableName.ToLower() + " Where " + sColumnName + "='\"+Rs" + sParentName + ".GetValue(\"" + LNK_COLUMN_NAME + "\")+\"'";
                //     } else {
                //         SQLString = "SELECT * FROM " + sTableName.ToLower() + " Where " + sColumnName + "='\"+entity." + LNK_COLUMN_NAME + "+\"'";
                //     }
                // } else {
                if (sParentName != "") {
                    SQLString = "";
                } else {
                    SQLString = "SELECT * FROM " + sTableName.ToLower() + " Where " + sColumnName + "='\"+entity." + sColumnName + "+\"'";
                }
                // }
            }

            return SQLString;
        }

        public void Close()
        {
            this.Trans.Close();
            _Trans=null;
        }

        object JSONObject(object[] args)
        {
            return new JSONObject();
        }

        object JSONArray(object[] args)
        {
            return new JSONArray();
        }

        object Print(object[] args)
        {
            if (args.Length>0){
                Console.WriteLine(args[0].ToString());
            }
            if (args.Length>1){
                Console.WriteLine(args[1].ToString());
            }
            return "";
        }

        object ToUnderlineName(object[] args)
        {
            return Volte.Utils.Util.ToUnderlineName(args[0].ToString());
        }

        object ToCamelCase(object[] args)
        {
            if (args.Length == 1)
            {
                return Volte.Utils.Util.ToCamelCase(args[0].ToString(),0);
            }else
            {
                return Volte.Utils.Util.ToCamelCase(args[0].ToString(), Convert.ToInt32(args[1].ToString()));
            }
        }

        object Util(object[] args) {

            return new Util();

        }

        public string Process(object[] args)
        {
            this.SetValue("DebugMode" , this.DebugMode);
            string _UID_CODE = args[0].ToString();
            string code = args[1].ToString();
            _Tmpl = VoltEngine.Parser(code);

            _Tmpl.Debug      = this.DebugMode == "Y";

            foreach (KeyValuePair<string , object> kvp in _Data) {

                _Tmpl.SetValue(kvp.Key , _Data[kvp.Key]);
            }

            _Tmpl.SetValue("sUID"   , _UID_CODE);
            _Tmpl.SetValue("ProjectPath" , AppConfigs.GetValue("ProjectPath"));
            _Tmpl.SetValue("AppPath"     , AppConfigs.GetValue("AppPath"));
            _Tmpl.SetValue("ProjectName" , AppConfigs.GetValue("ProjectName"));

            _Tmpl.RegisterFunction("Process"               , this.Process);
            _Tmpl.RegisterFunction("SqlDataTypeToDataType" , SqlDataTypeToDataType);
            _Tmpl.RegisterFunction("DefineColumn"          , DefineColumn);
            _Tmpl.RegisterFunction("DbType"                , DbType);
            _Tmpl.RegisterFunction("DataTypeDefault"       , DataTypeDefault);
            _Tmpl.RegisterFunction("StringToDataType"      , StringToDataType );
            _Tmpl.RegisterFunction("HasColumn"             , HasColumn);
            _Tmpl.RegisterFunction("TableColumnName"       , TableColumnName);
            _Tmpl.RegisterFunction("HasLNKColumn"          , HasLNKColumn);
            _Tmpl.RegisterFunction("TableHasColumn"        , TableHasColumn);
            _Tmpl.RegisterFunction("TOP_UID_CODE"          , TOP_UID_CODE);
            _Tmpl.RegisterFunction("FunctionActive"        , FunctionActive);
            _Tmpl.RegisterFunction("FunctionTableName"     , FunctionTableName);
            _Tmpl.RegisterFunction("Snapshot"              , Snapshot);
            _Tmpl.RegisterFunction("getHash"               , getHash);
            _Tmpl.RegisterFunction("FunctionColumnName"    , FunctionColumnName);
            _Tmpl.RegisterFunction("IgnoreCopyColumn"      , IgnoreCopyColumn);
            _Tmpl.RegisterFunction("FiltersWith"           , FiltersWith);
            _Tmpl.RegisterFunction("HasRegion"             , HasRegion);
            _Tmpl.RegisterFunction("JSONObject"            , this.JSONObject);
            _Tmpl.RegisterFunction("Print"                 , this.Print);
            _Tmpl.RegisterFunction("Util"                  , this.Util);
            _Tmpl.RegisterFunction("ToUnderlineName"       , this.ToUnderlineName);
            _Tmpl.RegisterFunction("ToCamelCase"           , this.ToCamelCase);

            return _Tmpl.Process();
        }

        public string Process(string name , string code)
        {

            this.SetValue("DebugMode", this.DebugMode);
            _Tmpl = VoltEngine.Parser(name , code);

            foreach (KeyValuePair<string, object> kvp in _Data) {
                _Tmpl.SetValue(kvp.Key       , _Data[kvp.Key]);
            }

            _Tmpl.SetValue("ProjectPath" , AppConfigs.GetValue("ProjectPath"));
            _Tmpl.SetValue("AppPath"     , AppConfigs.GetValue("AppPath"));
            _Tmpl.SetValue("ProjectName" , AppConfigs.GetValue("ProjectName"));

            _Tmpl.RegisterFunction("Process"               , this.Process);
            _Tmpl.RegisterFunction("SqlDataTypeToDataType" , SqlDataTypeToDataType);
            _Tmpl.RegisterFunction("DefineColumn"          , DefineColumn);
            _Tmpl.RegisterFunction("DbType"                , DbType);
            _Tmpl.RegisterFunction("DataTypeDefault"       , DataTypeDefault);
            _Tmpl.RegisterFunction("StringToDataType"      , StringToDataType);
            _Tmpl.RegisterFunction("HasColumn"             , HasColumn);
            _Tmpl.RegisterFunction("TableColumnName"       , TableColumnName);
            _Tmpl.RegisterFunction("HasLNKColumn"          , HasLNKColumn);
            _Tmpl.RegisterFunction("TableHasColumn"        , TableHasColumn);
            _Tmpl.RegisterFunction("TOP_UID_CODE"          , TOP_UID_CODE);
            _Tmpl.RegisterFunction("FunctionActive"        , FunctionActive);
            _Tmpl.RegisterFunction("FunctionTableName"     , FunctionTableName);
            _Tmpl.RegisterFunction("FunctionColumnName"    , FunctionColumnName);
            _Tmpl.RegisterFunction("Snapshot"              , Snapshot);
            _Tmpl.RegisterFunction("getHash"               , getHash);
            _Tmpl.RegisterFunction("IgnoreCopyColumn"      , IgnoreCopyColumn);
            _Tmpl.RegisterFunction("FiltersWith"           , FiltersWith);
            _Tmpl.RegisterFunction("HasRegion"             , this.HasRegion);
            _Tmpl.RegisterFunction("JSONObject"            , this.JSONObject);
            _Tmpl.RegisterFunction("Print"                 , this.Print);
            _Tmpl.RegisterFunction("Util"                  , this.Util);
            _Tmpl.RegisterFunction("ToUnderlineName"       , this.ToUnderlineName);
            _Tmpl.RegisterFunction("ToCamelCase"           , this.ToCamelCase);

            return _Templates.KeepSingleEmptyLine(_Tmpl.Process());
        }

        public void Process()
        {
            string separator = Path.DirectorySeparatorChar.ToString();

            this.SetValue("DebugMode", this.DebugMode);

            _Templates.DebugMode = this.DebugMode;
            _Templates.AppPath   = AppConfigs.GetValue("AppPath");

            string fileName  = Volte.Bot.Term.Util.Separator(AppConfigs.GetValue("AppPath") + @"\template\" + _Template);
            string fileName2 = Volte.Bot.Term.Util.Separator(AppConfigs.GetValue("TemplatePath") + @"\" + _Template);
            string code      = "";

            if (File.Exists(fileName)) {
                code = _Templates.Template(this.sUID, fileName);

                if (this.DebugMode == "Y") {

                    Console.WriteLine("file = " + fileName);
                }
            } else if (File.Exists(fileName2)) {
                code = _Templates.Template(this.sUID, fileName2);

                if (this.DebugMode == "Y") {

                    Console.WriteLine("file = " + fileName2);
                }
            } else {
                if (this.DebugMode == "Y") {

                    Console.WriteLine(" template not found");
                    Console.WriteLine("file        = " + fileName);
                    Console.WriteLine("file2       = " + fileName2);
                    Console.WriteLine("DevelopPath = " + AppConfigs.GetValue("DevelopPath"));
                }

                return;
            }

            CoreUtil.CreateDir(Volte.Bot.Term.Util.Separator(AppConfigs.GetValue("AppPath") + @"\temp"));

            string _t_template = Volte.Bot.Term.Util.Separator(AppConfigs.GetValue("AppPath") + @"\temp\" + this.sUID + "_" + Path.GetFileName(_Template) + ".tpl");

            UTF8Encoding _UTF8Encoding = new UTF8Encoding(false, true);

            try {
                if (Path.GetExtension(OutputFile)==".cs" || Path.GetExtension(OutputFile) == ".csproj"){

                    string tOutputFile=OutputFile+".cs";
                    StreamWriter _File = new StreamWriter(tOutputFile, false, _UTF8Encoding);

                    _File.Write(Process(Path.GetFileName(_Template) , code));
                    _File.Close();
                    _Substitute.Initialize();
                    if (this.DebugMode == "Y") {

                        Console.WriteLine("tOutputFile = " + tOutputFile);
                        Console.WriteLine("OutputFile  = " + OutputFile);
                    }
                    _Substitute.CopyFile(tOutputFile,OutputFile);
                    if (File.Exists(tOutputFile))
                    {
                        File.Delete(tOutputFile);
                    }
                }else{
                    StreamWriter _File = new StreamWriter(OutputFile, false, _UTF8Encoding);

                    _File.Write(Process(Path.GetFileName(_Template) , code));
                    _File.Close();
                }

                Dictionary<string, int> _UnUsingRegion = _Templates.UnUsing();
                foreach (string kvp in _UnUsingRegion.Keys) {
                    if (_UsageRegion.ContainsKey(kvp)){
                        _UsageRegion[kvp] = _UsageRegion[kvp] +_UnUsingRegion[kvp];
                    }else{
                        _UsageRegion[kvp] = _UnUsingRegion[kvp];
                    }
                }
                if (this.DebugMode == "Y") {
                    try {
                        StreamWriter _debug = new StreamWriter(_t_template, false, _UTF8Encoding);
                        _debug.Write(code);
                        _debug.Close();
                    } catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }

                    Console.WriteLine(">=" + fileName);
                    Console.WriteLine("<=" + this.OutputFile);
                }

            } catch (Exception e) {

                Console.WriteLine("");
                Console.WriteLine(_Template);
                Console.WriteLine(_t_template);

                try {
                    StreamWriter debugFile = new StreamWriter(_t_template, false, _UTF8Encoding);
                    debugFile.Write(code);
                    debugFile.Close();
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }

                Console.WriteLine(OutputFile);
                Console.WriteLine("===");
                Console.WriteLine(e.ToString());
                Console.WriteLine("===");
            }
        }
    }
}
