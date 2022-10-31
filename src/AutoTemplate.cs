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

            JSONObject _JSONObject = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation+sUID+".json");

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

        string HasColumn(object[] args)
        {
            string name = args[0].ToString();
            string sUID = args[1].ToString();
            string rtv = "False";
            JSONObject _JSONFunction = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation+ sUID + ".json");
            JSONArray _entity = _JSONFunction.GetJSONArray("entitys");
            foreach (JSONObject it in _entity.JSONObjects)
            {
                if (args.Length > 2)
                {
                    string sTableName = args[2].ToString();
                    if (it.GetValue("sTableName")== sTableName && it.GetValue("sColumnName") == name)
                    {
                        return "True";
                    }
                }
                else
                {
                    if (it.GetValue("sColumnName") == name)
                    {
                        return "True";
                    }
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

                JSONObject _obj=AppConfigs.LoadSetting("DataType.json");
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

            string name = args[0].ToString();
            string sUID = args[1].ToString();
            string key = sUID + "_" + name;
            key = key.ToLower();
            bool rtv = false;

            if (_HasBoolean.ContainsKey(key))
            {
                rtv = _HasBoolean[key];
            }
            else
            {

                object[] _args = new object[1];

                _args[0] = sUID;

                string _LNKUID = TOP_UID_CODE(_args);

                if (_LNKUID == sUID)
                {
                    return false;
                }

                JSONObject _JSONFunction = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation + _LNKUID + ".json");
                JSONArray _entity = _JSONFunction.GetJSONArray("entitys");
                foreach (JSONObject it in _entity.JSONObjects)
                {
                    if (it.GetValue("sColumnName") == name)
                    {
                        return "True";
                    }
                }
            }

            return rtv;
        }
        object TableHasColumn(object[] args)
        {

            string sTableName = args[0].ToString();
            string name = args[1].ToString();
            string key = sTableName + "." + name;
            bool rtv = false;

            key = key.ToLower();

            if (_TableHasColumn.ContainsKey(key))
            {
                
                rtv = _TableHasColumn[key];
            }
            else
            {
                JSONObject _JSONFunction = AppConfigs.LoadJSONObject(UtilSeparator.Separator(AppConfigs.DevelopPath + @"\definition\entity\" + sTableName + ".json"));
                JSONArray _entity = _JSONFunction.GetJSONArray(sTableName);

                foreach (JSONObject it in _entity.JSONObjects)
                {
                    if (it.GetValue("sColumnName").ToLower() == name.ToLower())
                    {
                        string s = it.GetValue("sTableName") + "." + it.GetValue("sColumnName");
                        s = s.ToLower();
                        _TableHasColumn[s] = true;
                        return "True";
                    }
                }
            }

            return rtv;
        }        
        string FunctionActive(object[] args)
        {

            string sUID = args[0].ToString();

            JSONObject _JSONObject= AppConfigs.LoadJSONObject(AppConfigs.AddonLocation+sUID+".json");
            return  _JSONObject.GetValue("bActive");
        }

        string FunctionColumnName(object[] args)
        {
            string sUID       = args[0].ToString();
            JSONObject _JSONObject = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation+sUID+".json");
            return  _JSONObject.GetValue("sColumnName");
        }

        string getHash(object[] args)
        {

            string sUID       = args[0].ToString();
            JSONObject _JSONObject = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation+sUID+".json");
            return  _JSONObject.GetValue("sHash");
        }

        string FunctionTableName(object[] args)
        {
            string sUID       = args[0].ToString();
            JSONObject _JSONObject = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation+sUID+".json");
            return  _JSONObject.GetValue("sTableName");
        }

        public string TOP_UID_CODE(object[] args)
        {
            string sUID       = args[0].ToString();
            JSONObject _JSONObject = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation+sUID+".json");
            if (_JSONObject.GetValue("sTopUID_CODE")=="")
            {
                return sUID;
            }else{
                return _JSONObject.GetValue("sTopUID_CODE");
            }
        }

        string LowerCase(object[] args)
        {
            string str = args[0].ToString();
            if (str == null)
                return null;
         
            return str.ToLower();
        }

        string TitleCase(object[] args)
        {
            string str = args[0].ToString();
            if (str == null)
                return null;
         
            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);
         
            return str.ToUpper();
        }

        string TrimLowerStart(object[] args)
        {
            string str = args[0].ToString();
            if (str == null){
                return null;
            }
            if (str.Length>2){

                if (char.IsLower(str[0]))
                {
                    str = str.Remove(0, 1);
                }
            }
            return str;
        }

        string TrimStart(object[] args)
        {
            string str = args[0].ToString();
            string sStartValue = "";

            if (args.Length > 1) {
                if (args[1]!=null) {
                    sStartValue=args[1].ToString();
                }
            }   

            if (string.IsNullOrEmpty(sStartValue)) {
                return str;
            }
            if (str.StartsWith(sStartValue))
            {
                str = str.Remove(0, sStartValue.Length);
            }
            return str;
        }

        string SqlDataTypeToDataType(object[] args)
        {

            string dataType = args[0].ToString();
            string sLang="";
            if (args.Length > 1) {
                sLang = args[1].ToString();
            }

            return AppConfigs.Mapping("DataType", dataType);
        }

        string StringToDataType(object[] args)
        {

            string dataType = args[0].ToString();

            string sLang="";
            if (args.Length > 1) {
                sLang = args[1].ToString();
            }

            return AppConfigs.Mapping("ToDataTypeMapping",dataType);
        }

        string DbType(object[] args)
        {
            string dataType = args[0].ToString();
            string sLang="";
            if (args.Length > 1) {
                sLang = args[1].ToString();
            }

            string sValue = AppConfigs.Mapping("DbTypeMapping",dataType);
            if (sValue==""){
                return "undefine-"+dataType;
            }else{
                return sValue;
            }
        }

        string DataTypeDefault(object[] args)
        {

            string dataType = args[0].ToString();

            string sValue =  AppConfigs.Mapping("DataTypeDefault",dataType);

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
            int    nScale       = 2;
            string _NonPrintable      = "false";
            string sAlignColumnName = "";
            string sEnableMode      = "";
            string sDataBand          = "";
            string rtv                = "";

            string key = tableName + "_" + columnName + "_" + type + "_" + captionCode + "_" + scale;

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
                                
                                l  = Utils.Util.ToInt(cValue.Substring(0 , _P));
                                scale = Utils.Util.ToInt(cValue.Substring(_P + 1, cValue.Length - _P - 1));

                                if (l > 1) {
                                    nColumnLength = l;

                                }
                            } else {
                                l = Utils.Util.ToInt(cValue);

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
                    nScale        = RsZUCOLUTM.GetInteger("nScale");
                    string sGroup = RsZUCOLUTM.GetValue("sGroup");

                } else {
                    if (tableName != "ZZFields" && tableName != "VARIABLE") {
                        JSONObject _JSONObject2= AppConfigs.LoadSetting("AppSettings.json");
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
                    nScale = scale;
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
                XObject.AppendLine(_name + ".Scale          = " + nScale + ";");
                XObject.AppendLine(_name + ".NonPrintable   = " + _NonPrintable + ";");

                XObject.AppendLine("_JSONTable.Declare(_SQLStatement.Process(" + _name + " , \"" + sSqlCode + "\" , entity.DataOption));");

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
                return Utils.Util.ToCamelCase(args[0].ToString(),0);
            }else
            {
                return Utils.Util.ToCamelCase(args[0].ToString(), Convert.ToInt32(args[1].ToString()));
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
            foreach (KeyValuePair<string , object> kvp in _Data) {

                _Tmpl.SetValue(kvp.Key , _Data[kvp.Key]);
            }

            _Tmpl.SetValue("sUID"   , _UID_CODE);
            _Tmpl.SetValue("ProjectPath" , AppConfigs.ProjectPath);
            _Tmpl.SetValue("DevelopPath" , AppConfigs.GetValue("DevelopPath"));
            _Tmpl.SetValue("ProjectName" , AppConfigs.GetValue("ProjectName"));

            _Tmpl.RegisterFunction("Process"               , this.Process);
            _Tmpl.RegisterFunction("SqlDataTypeToDataType" , SqlDataTypeToDataType);
            _Tmpl.RegisterFunction("TrimStart"             , TrimStart);
            _Tmpl.RegisterFunction("TrimLowerStart"        , TrimLowerStart);
            _Tmpl.RegisterFunction("TitleCase"             , TitleCase);
            _Tmpl.RegisterFunction("LowerCase"             , LowerCase);
            _Tmpl.RegisterFunction("DefineColumn"          , DefineColumn);
            _Tmpl.RegisterFunction("DbType"                , DbType);
            _Tmpl.RegisterFunction("DataTypeDefault"       , DataTypeDefault);
            _Tmpl.RegisterFunction("StringToDataType"      , StringToDataType );
            _Tmpl.RegisterFunction("HasColumn"             , HasColumn);
            _Tmpl.RegisterFunction("HasLNKColumn"          , HasLNKColumn);
            _Tmpl.RegisterFunction("TableHasColumn"        , TableHasColumn);
            _Tmpl.RegisterFunction("TOP_UID_CODE"          , TOP_UID_CODE);
            _Tmpl.RegisterFunction("FunctionActive"        , FunctionActive);
            _Tmpl.RegisterFunction("FunctionTableName"     , FunctionTableName);
            _Tmpl.RegisterFunction("getHash"               , getHash);
            _Tmpl.RegisterFunction("FunctionColumnName"    , FunctionColumnName);
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

            if (Path.GetExtension(OutputFile) == ".cs")
            {
                _Tmpl.Debug = this.DebugMode == "Y";
            }

            foreach (KeyValuePair<string, object> kvp in _Data) {
                _Tmpl.SetValue(kvp.Key       , _Data[kvp.Key]);
            }

            _Tmpl.SetValue("ProjectPath" , AppConfigs.ProjectPath);
            _Tmpl.SetValue("DevelopPath" , AppConfigs.GetValue("DevelopPath"));
            _Tmpl.SetValue("ProjectName" , AppConfigs.GetValue("ProjectName"));

            _Tmpl.RegisterFunction("Process"               , this.Process);
            _Tmpl.RegisterFunction("SqlDataTypeToDataType" , SqlDataTypeToDataType);
            _Tmpl.RegisterFunction("TrimStart"             , TrimStart);
            _Tmpl.RegisterFunction("TrimLowerStart"        , TrimLowerStart);
            _Tmpl.RegisterFunction("TitleCase"             , TitleCase);
            _Tmpl.RegisterFunction("LowerCase"             , LowerCase);
            _Tmpl.RegisterFunction("DefineColumn"          , DefineColumn);
            _Tmpl.RegisterFunction("DbType"                , DbType);
            _Tmpl.RegisterFunction("DataTypeDefault"       , DataTypeDefault);
            _Tmpl.RegisterFunction("StringToDataType"      , StringToDataType);
            _Tmpl.RegisterFunction("HasColumn"             , HasColumn);
            _Tmpl.RegisterFunction("HasLNKColumn"          , HasLNKColumn);
            _Tmpl.RegisterFunction("TableHasColumn"        , TableHasColumn);
            _Tmpl.RegisterFunction("TOP_UID_CODE"          , TOP_UID_CODE);
            _Tmpl.RegisterFunction("FunctionActive"        , FunctionActive);
            _Tmpl.RegisterFunction("FunctionTableName"     , FunctionTableName);
            _Tmpl.RegisterFunction("FunctionColumnName"    , FunctionColumnName);
            _Tmpl.RegisterFunction("getHash"               , getHash);
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

            this.SetValue("DebugMode", this.DebugMode);
            string sExtension=Path.GetExtension(OutputFile);
            if (sExtension == ".cs")
            {
                _Templates.DebugMode = this.DebugMode;
            }else{
                _Templates.DebugMode = "N";
            }

            _Templates.AppPath = AppConfigs.GetValue("DevelopPath");

            string fileName  = UtilSeparator.Separator(AppConfigs.GetValue("DevelopPath") + @"\template\" + _Template);
            string fileName2 = UtilSeparator.Separator(AppConfigs.GetValue("DevelopPath") + @"\template\" + _Template);
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
                    Console.WriteLine("DevelopPath = " + AppConfigs.DevelopPath);
                }

                return;
            }

            string temp=Term.UtilSeparator.Separator(AppConfigs.GetValue("DevelopPath") + @"\temp\");

            Utils.Util.CreateDir(temp);

            string _t_template = UtilSeparator.Separator(temp + this.sUID + "_" + Path.GetFileName(_Template) + ".tpl");

            UTF8Encoding _UTF8Encoding = new UTF8Encoding(false, true);

            try {

                JSONObject Substitute = AppConfigs.JSONObject("Substitute");
                string    sExtensions = Substitute.GetValue("sExtension");

                if (this.DebugMode == "Y") {
                    Console.WriteLine("bSubstitute="+Substitute.GetBoolean("bActive"));
                    Console.WriteLine("sExtensions="+sExtensions);
                }

                if (Substitute.GetBoolean("bActive") && 
                      (sExtensions.Contains(sExtension) || sExtension==".cs" || sExtension == ".csproj")
                   )
                {

                    string tOutputFile = OutputFile+".bak";
                    StreamWriter _File = new StreamWriter(tOutputFile, false, _UTF8Encoding);
                    _File.Write(Process(Path.GetFileName(_Template) , code));
                    _File.Close();

                    _Substitute.Initialize(Substitute.GetValue("sCode"));
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
