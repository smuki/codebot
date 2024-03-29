using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Linq;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Threading;
using System.Security.Cryptography;

using Volte.Bot.Tpl;
using Volte.Data.Dapper;
using Volte.Data.Json;
using Volte.Utils;

namespace Volte.Bot.Term
{
    public class AutoGenerator
    {

        private ShellRunner _ShellRunner   = new ShellRunner();
        private List<string> _FAILURE      = new List<string>();

        public List<CommandEntity> sCommandEntity = new List<CommandEntity>();

        public StringBuilder Message = new StringBuilder();
        public AppConfigs AppConfigs;

        public  string DebugMode     = "N";
        private string sTablePrefix  = "";
        private string sCamelPrefix  = "";

        private TableUtil _TableUtil = new TableUtil();
        private Substitute _Substitute=new Substitute();

        private List<string> _L_UID_CODE = new List<string>();

        public string gTableName   { get ; set ; }
        public AutoGenerator()
        {

        }
        public void Write(object message)
        {
            Console.Write(message);
            Message.Append(message);
        }

        public void WriteLine(object message)
        {
            this.Write(message);
            this.Write("\n");
        }

        public void RefreshSysFields()
        {
            TableUtil _TableUtil = new TableUtil();

            DbContext _Trans = new DbContext(AppConfigs.GetValue("sDbName") , AppConfigs.GetValue("Provider") , AppConfigs.GetValue("dbAdapter"));

            _TableUtil.dbAdapter = _Trans.dbAdapter;
            _TableUtil.Target = "mssql";
            _TableUtil.AppPath = "";

            _TableUtil.SqlWithDelete = "N";
            _TableUtil.AppendOnly = "N";

            List<JSONObject> _TableNames = _TableUtil.DatabaseTable(_Trans, "");
            CRC32 _CRC32Cls = new CRC32();

            string sSQLString = "update SysFields set sChgFlag='x' ";
            _Trans.Execute(sSQLString);
            foreach (JSONObject kvp in _TableNames)
            {

                string sTableName = kvp.GetValue("sTableName");
                this.Write("\n" + sTableName);

                List<JSONObject> _NameValues = _TableUtil.DatabaseTableColumns(_Trans, sTableName);
                foreach (JSONObject colname in _NameValues)
                {
                    string sColumnName = colname.GetValue("sColumnName");
                    string sDataType = colname.GetValue("sDataType");
                    string sComment = colname.GetValue("sComment");
                    int nColumnLength = colname.GetInteger("nColumnLength");
                    bool bIsPKColumn = colname.GetBoolean("bPrimaryKey");
                    int IsPKColumn = 0;
                    if (bIsPKColumn)
                    {
                        IsPKColumn = 1;
                    }
                    sSQLString = "delete from SysFields where sTableName='" + sTableName + "' AND sColumnName='"+ sColumnName + "'";
                    _Trans.Execute(sSQLString);

                    sSQLString = "INSERT INTO SysFields (sTableName, sColumnName, sDataType, nColumnLength, bPrimaryKey,sComment) ";
                    sSQLString = sSQLString + " VALUES('" + sTableName + "', '" + sColumnName + "', '" + sDataType + "', " + nColumnLength + "," + IsPKColumn + ", '" + sComment + "')";
                    _Trans.Execute(sSQLString);

                    this.Write(".");
                }
            }
        }

        public void Generator(string sUID)
        {
            Message = new StringBuilder();
            if (string.IsNullOrEmpty(sUID))
            {
                sUID = "";
            }
            sUID = sUID.Replace("$", "%");
            if (sUID.ToLower() == "a")
            {
                sUID = "%";
            }
            DbContext _DbContext = new DbContext(AppConfigs.GetValue("sDbName"), AppConfigs.GetValue("Provider"), AppConfigs.GetValue("dbAdapter"));

            QueryRows RsSysFunction = new QueryRows(_DbContext);
            RsSysFunction.CommandText="SELECT sUID FROM sysfunction WHERE sUID LIKE @sUID ORDER BY sUID DESC";
            RsSysFunction.SetParameter("sUID","%" + sUID + "%");
            RsSysFunction.Open();
            _L_UID_CODE = new List<string>();
            if (sUID.ToLower() == "en")
            {
                GeneratorCaptionDefine(sUID.ToLower());
            }
            while (!RsSysFunction.EOF)
            {
                string _UID_CODE = RsSysFunction.GetValue("sUID");

                _L_UID_CODE.Add(_UID_CODE);
                GeneratorActivityDefinition(_UID_CODE);
                RsSysFunction.MoveNext();
            }
            RsSysFunction.Close();

        }

        private void GeneratorCaptionDefine(string sUID)
        {

            this.PrepareDir();

            DbContext  _DbContext = new DbContext(AppConfigs.GetValue("sDbName") , AppConfigs.GetValue("Provider") , AppConfigs.GetValue("dbAdapter"));

            JSONObject _JSONObject = new JSONObject();
            QueryRows RsSysCaption   = new QueryRows(_DbContext);
            RsSysCaption.CommandText = "SELECT * FROM syscaption WHERE sCaptionLang=@sUID ORDER BY sCaptionCode";
            RsSysCaption.SetParameter("sUID",sUID);
            RsSysCaption.Open();

            while (!RsSysCaption.EOF)
            {

                string sCaptionCode = RsSysCaption.GetValue("sCaptionCode").ToLower();
                string sCaption     = RsSysCaption.GetValue("sCaption");

                _JSONObject.SetValue(sCaptionCode , sCaption);

                RsSysCaption.MoveNext();
            }
            RsSysCaption.Close();

            string sDefinition = UtilSeparator.Separator(AppConfigs.DevelopPath + @"\definition\functions\"+sUID+".json");

            if (File.Exists(sDefinition)) {
                File.Delete(sDefinition);
            }
            Utils.Util.WriteContents(sDefinition , _JSONObject.ToString());
        }

        private void PrepareDir()
        {
            UtilSeparator.CreateDir(UtilSeparator.Separator(AppConfigs.DevelopPath + @"\definition\functions"));
        }

        public void GeneratorActivityDefinition(string sUID)
        {
            DbContext  _DbContext = new DbContext(AppConfigs.GetValue("sDbName") , AppConfigs.GetValue("Provider") , AppConfigs.GetValue("dbAdapter"));

            this.PrepareDir();

            string sDefinition = UtilSeparator.Separator(AppConfigs.DevelopPath + @"\definition\functions\"+sUID+".json");

            if (File.Exists(sDefinition)) {
                File.Delete(sDefinition);
            }

            QueryRows RsZUPRGDTM = new QueryRows(_DbContext);
            RsZUPRGDTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE bActive<>0 AND sUID=@sUID ORDER BY nIndex";
            RsZUPRGDTM.SetParameter("sUID",sUID);
            RsZUPRGDTM.Open();
            if (RsZUPRGDTM.EOF) {
                Console.WriteLine("EOF By "+RsZUPRGDTM.CommandText);
                return;
            }

            QueryRows RsSysFunction   = new QueryRows(_DbContext);
            QueryRows _SysColumnClass = new QueryRows(_DbContext);

            RsSysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID=@sUID";
            RsSysFunction.SetParameter("sUID",sUID);
            RsSysFunction.Open();

            string _TableName     = "";
            string _ColumnName    = "";
            string _DataType      = "";
            int Keys              = 0;
            string sLNKUID        = "";
            string sTableName     = "";
            string sLNK_TableName = "";
            string sColumnName    = "";
            string sCode          = "";
            string sType          = "";
            string sHash          = "";
            string sDescription   = "";
            string sContext       = "";

            bool   bActive        = false;

            if (RsSysFunction.EOF) {
                Console.WriteLine("fucntion not define");
                return;
            } else {
                sTableName   = RsSysFunction.GetValue("sTableName");
                sColumnName  = RsSysFunction.GetValue("sColumnName");
                sDescription = RsSysFunction.GetValue("sDescription01");
                sCode        = RsSysFunction.GetValue("sCode");
                sContext     = RsSysFunction.GetValue("sContext");
                bActive      = RsSysFunction.GetBoolean("bActive");
                if (AppConfigs.GetBoolean("LowerName")){
                    sTableName = sTableName.ToLower();
                }
            }
            RsSysFunction.Close();

            string sROOT_LNKUID     = Root_LNKUID(_DbContext        , sUID);
            string sRoot_TableName  = FunctionTableName(_DbContext  , sROOT_LNKUID);
            string sRoot_ColumnName = FunctionColumnName(_DbContext , sROOT_LNKUID);

            if (AppConfigs.GetBoolean("LowerName")){
                sRoot_TableName  = sRoot_TableName.ToLower();
                sRoot_ColumnName = sRoot_ColumnName.ToLower();
            }

            QueryRows RsZUCOLUTM  = new QueryRows(_DbContext);
            QueryRows RsSysFields = new QueryRows(_DbContext);

            string _COLUMNS_NAME  = "";
            string _COLUMNS_NAMED = "";
            string sGroup  = "";

            List<COLUMNEntity> Entitys = new List<COLUMNEntity>();
            List<string> sRef          = new List<string>();
            JSONArray _entitys         = new JSONArray();
            JSONObject oContext=new JSONObject(sContext);

            QueryRows _SysRef          = new QueryRows(_DbContext);

            while (!RsZUPRGDTM.EOF) {

                _TableName = RsZUPRGDTM.GetValue("sTableName");

                if (AppConfigs.GetBoolean("LowerName")){
                    _TableName = _TableName.ToLower();
                }

                _ColumnName   = RsZUPRGDTM.GetValue("sColumnName");
                _DataType     = RsZUPRGDTM.GetValue("sDataType");
                sGroup = RsZUPRGDTM.GetValue("sGroup");

                COLUMNEntity _COLUMNEntity   = new COLUMNEntity();
                _COLUMNEntity.sDescriptionId = RsZUPRGDTM.GetValue("sCaptionCode");
                _COLUMNEntity.sColumnName    = _ColumnName;
                _COLUMNEntity.bNullable      = RsZUPRGDTM.GetBoolean("bColumnNullable");
                _COLUMNEntity.sDataType      = _DataType;
                _COLUMNEntity.sEnableMode    = RsZUPRGDTM.GetValue("sEnableMode");
                _COLUMNEntity.Options        = RsZUPRGDTM.GetValue("sOption");
                _COLUMNEntity.sTableName     = _TableName;
                _COLUMNEntity.bWriteable     = RsZUPRGDTM.GetBoolean("bWriteable");
                _COLUMNEntity.nMaxLength     = RsZUPRGDTM.GetInteger("nWidth");

                if (_TableName.ToLower() == "variable") {
                    _COLUMNEntity.bWriteable = false;
                }

                _COLUMNEntity.sRefBrowse = RsZUPRGDTM.GetValue("sRefBrowse");
                _COLUMNEntity.sRefCheck  = RsZUPRGDTM.GetValue("sRefCheck");
                _COLUMNEntity.sRefViewer = RsZUPRGDTM.GetValue("sRefViewer");
                if (_COLUMNEntity.sRefBrowse!=""){
                    sRef.Add(_COLUMNEntity.sRefBrowse);
                }
                if (_COLUMNEntity.sRefCheck!=""){
                    sRef.Add(_COLUMNEntity.sRefCheck);
                }
                if (_COLUMNEntity.sRefViewer!=""){
                    sRef.Add(_COLUMNEntity.sRefViewer);
                }

                _SysRef = new QueryRows(_DbContext);
                _SysRef.CommandText = "SELECT * FROM sysref WHERE sRefCode=@sRefCode";
                _SysRef.SetParameter("sRefCode", _COLUMNEntity.sRefBrowse);
                _SysRef.Open();

                if (!_SysRef.EOF) {
                    _COLUMNEntity.sRefBrowseType = _SysRef.GetValue("sType");
                }
                _SysRef.Close();

                RsSysFields.CommandText = "SELECT * FROM sysfields WHERE sTableName = '" + _TableName + "' AND sColumnName='" + _ColumnName + "'";
                RsSysFields.Open();

                if (RsSysFields.EOF) {

                    if (_TableName.ToLower()!="variable"){
                        GeneratorColumns(_DbContext , _TableName);
                    }

                    RsSysFields.CommandText = "SELECT * FROM sysfields WHERE sTableName = '" + _TableName + "' AND sColumnName='" + _ColumnName + "'";
                    RsSysFields.Open();

                }

                if (!RsSysFields.EOF) {
                    _COLUMNEntity.sDescriptionId = RsSysFields.GetValue("sCaptionCode");
                    _COLUMNEntity.sComment       = RsSysFields.GetValue("sComment");

                    if (!RsSysFields.GetBoolean("bColumnUsage")) {
                        _DbContext.Execute("UPDATE sysfields Set bColumnUsage=1 WHERE sTableName = '" + _TableName + "' AND sColumnName='" + _ColumnName + "'");
                    }

                    if (string.IsNullOrEmpty(_DataType)) {
                        _DataType = RsSysFields.GetValue("sDataType");
                    }

                    _COLUMNEntity.bPrimaryKey = RsSysFields.GetBoolean("bPrimaryKey");

                }
                RsSysFields.Close();

                if (_COLUMNEntity.bPrimaryKey) 
                {
                    Keys++;
                }

                _COLUMNEntity.sDataType = _DataType;

                if (_TableName.ToLower() != "variable" && (_DataType == "nvarchar" || _DataType == "ntext")) 
                {
                    if (_ColumnName != "sOriginal") 
                    {
                        _COLUMNS_NAME = _COLUMNS_NAME + ";" + _TableName + "." + _ColumnName;
                    }
                }

                if (_TableName.ToLower() != "variable" && _DataType == "datetime") {
                    _COLUMNS_NAMED = _COLUMNS_NAMED + ";" + _TableName  + _ColumnName;
                }

                if (_TableName.ToLower() == "variable" && (_DataType == "decimal" || _DataType == "picture"))
                {
                } else if (_TableName.ToLower() == "variable" && _DataType == "") {
                    _COLUMNEntity.sDataType   = "nvarchar";
                }

                _COLUMNEntity.sEnableMode = _COLUMNEntity.sEnableMode.Replace("ADDNEW" , "NEW");
                _COLUMNEntity.sEnableMode = _COLUMNEntity.sEnableMode.Replace("UPDATE" , "EDIT");

                JSONObject _entity = new JSONObject();

                _entity.SetValue("sTableName"    , _COLUMNEntity.sTableName);
                _entity.SetValue("sColumnName"   , _COLUMNEntity.sColumnName);
                _entity.SetValue("sDataType"     , _COLUMNEntity.sDataType);
                _entity.SetBoolean("bPrimaryKey" , _COLUMNEntity.bPrimaryKey);
                _entity.SetBoolean("bIndexes" , _COLUMNEntity.bIndexes);
                _entity.SetBoolean("bNullable"   , _COLUMNEntity.bNullable);
                _entity.SetBoolean("Writeable"   , _COLUMNEntity.bWriteable);

                if (_COLUMNEntity.nMaxLength>0){
                    _entity.SetInteger("nMaxLength"  , _COLUMNEntity.nMaxLength);
                }

                if (!string.IsNullOrEmpty(_COLUMNEntity.sDescriptionId)){
                    _entity.SetValue("sDescriptionId"   , _COLUMNEntity.sDescriptionId);
                }
                if (!string.IsNullOrEmpty(_COLUMNEntity.Options)){
                    _entity.SetValue("Options"       , _COLUMNEntity.Options);
                }
                if (!string.IsNullOrEmpty(_COLUMNEntity.sComment)){
                    _entity.SetValue("sComment"      , _COLUMNEntity.sComment);
                }
                if (!string.IsNullOrEmpty(_COLUMNEntity.sEnableMode)){
                    _entity.SetValue("sEnableMode" , _COLUMNEntity.sEnableMode);
                }
                if (!string.IsNullOrEmpty(_COLUMNEntity.sRefCheck)){
                    _entity.SetValue("sRefCheck"   , _COLUMNEntity.sRefCheck);
                }
                if (!string.IsNullOrEmpty(_COLUMNEntity.sRefBrowse)){
                    _entity.SetValue("sRefBrowse" , _COLUMNEntity.sRefBrowse);
                }
                if (!string.IsNullOrEmpty(_COLUMNEntity.sRefViewer)){
                    _entity.SetValue("sRefViewer" , _COLUMNEntity.sRefViewer);
                }

                _entitys.Add(_entity);

                Entitys.Add(_COLUMNEntity);

                RsZUPRGDTM.MoveNext();
            }
            RsZUPRGDTM.Close();

            JSONObject _JSONRefs = new JSONObject();

            foreach (string sRefCode in sRef){
                JSONObject _JSONRef = new JSONObject();

                _SysRef = new QueryRows(_DbContext);
                _SysRef.CommandText = "SELECT * FROM sysref WHERE sRefCode=@sRefCode";
                _SysRef.SetParameter("sRefCode", sRefCode);
                _SysRef.Open();

                if (!_SysRef.EOF) {

                    _JSONRef.SetValue("sRefCode"     , _SysRef.GetValue("sRefCode"));
                    _JSONRef.SetValue("sType"        , _SysRef.GetValue("sType"));
                    _JSONRef.SetValue("sTableName"   , _SysRef.GetValue("sTableName"));
                    _JSONRef.SetValue("sSqlCode"     , _SysRef.GetValue("sSqlCode"));
                    _JSONRef.SetValue("sWhereClause" , _SysRef.GetValue("sWhereClause"));

                    QueryRows _SysData = new QueryRows(_DbContext);
                    _SysData.CommandText = "SELECT * FROM sysdata WHERE sSqlCode=@sSqlCode";
                    _SysData.SetParameter("sSqlCode", _SysRef.GetValue("sSqlCode"));
                    _SysData.Open();

                    if (!_SysData.EOF) {
                        _JSONRef.SetValue("sFromClause"  , _SysData.GetValue("sFromClause"));
                    }
                    _SysData.Close();
                }
                _SysRef.Close();

                JSONArray _JSONSysRefDtl = new JSONArray();

                QueryRows _SysRefDtl = new QueryRows(_DbContext);
                _SysRefDtl.CommandText = "SELECT * FROM sysrefdtl WHERE sRefCode=@sRefCode";
                _SysRefDtl.SetParameter("sRefCode", sRefCode);
                _SysRefDtl.Open();

                while (!_SysRefDtl.EOF) {
                    JSONObject _JSONRefDtl = new JSONObject();
                    _JSONRefDtl.SetValue("sRefCode"          , _SysRefDtl.GetValue("sRefCode"));
                    _JSONRefDtl.SetValue("sTableName"        , _SysRefDtl.GetValue("sTableName"));
                    _JSONRefDtl.SetValue("sColumnName"       , _SysRefDtl.GetValue("sColumnName"));
                    if (string.IsNullOrEmpty(_SysRefDtl.GetValue("sAccessTableName"))){
                        _JSONRefDtl.SetValue("sAccessTableName"  , sTableName);
                    }else{
                        _JSONRefDtl.SetValue("sAccessTableName"  , _SysRefDtl.GetValue("sAccessTableName"));
                    }
                    _JSONRefDtl.SetValue("sAccessColumnName" , _SysRefDtl.GetValue("sAccessColumnName"));
                    _JSONRefDtl.SetValue("sAccessType"       , _SysRefDtl.GetValue("sAccessType"));
                    _JSONSysRefDtl.Add(_JSONRefDtl);
                    _SysRefDtl.MoveNext();
                }
                _SysRefDtl.Close();

                _JSONRef.SetValue("RefDtl" , _JSONSysRefDtl);

                _JSONRefs.SetValue(sRefCode,_JSONRef);

            }
            JSONObject Ignore   = AppConfigs.JSONObject("Table");
            string IgnoreTables = Ignore.GetValue("Tables");
            sTablePrefix = Ignore.GetValue("Prefix");
            sCamelPrefix = Ignore.GetValue("CamelPrefix");

            string sCamelTableName=sCamelPrefix+Utils.Util.ToCamelCase(UtilSeparator.TrimStart(sTableName,sTablePrefix));

            JSONObject _JSONFunction = new JSONObject();
            _JSONFunction.SetValue("sUID"            , sUID);
            _JSONFunction.SetValue("sTableName"      , sTableName);
            _JSONFunction.SetValue("sCamelTableName" , sCamelTableName);
            _JSONFunction.SetValue("sCamelPrimaryKey", Utils.Util.ToCamelCase(sColumnName));
            _JSONFunction.SetValue("sPrimaryKey"     , sColumnName);
            _JSONFunction.SetValue("PK_ColumnName"   , sColumnName);
            _JSONFunction.SetValue("sDescription"    , sDescription);
            _JSONFunction.SetValue("sCode"           , sCode);
            _JSONFunction.SetValue("LNK_TableName"   , sLNK_TableName);
            _JSONFunction.SetValue("sLNKUID"         , sLNKUID);
            _JSONFunction.SetValue("ROOT_LNKUID"     , sROOT_LNKUID);
            _JSONFunction.SetValue("ROOT_TableName"  , sRoot_TableName);
            _JSONFunction.SetValue("ROOT_ColumnName" , sRoot_ColumnName);
            _JSONFunction.SetBoolean("bActive"       , bActive);
            _JSONFunction.SetValue("sHash"           , sHash);
            _JSONFunction.SetValue("entitys"         , _entitys);
            _JSONFunction.SetValue("mapping"         , oContext.GetJSONArray("mapping"));
            _JSONFunction.SetValue("Property"        , oContext.GetJSONObject("Property"));
            _JSONFunction.SetValue("sysref"          , _JSONRefs);

            Utils.Util.WriteContents(sDefinition , JsonFormatter.PrettyPrint(_JSONFunction.ToString()));

        }

        public void GeneratorEntityDefinition()
        {

            DbContext  _Trans = new DbContext(AppConfigs.GetValue("sDbName") , AppConfigs.GetValue("Provider") , AppConfigs.GetValue("dbAdapter"));
            try {

                List<JSONObject> _JSONObject = _TableUtil.DatabaseTable(_Trans , gTableName);

                int i = 0;
                int n = 0;

                JSONObject Ignore   = AppConfigs.JSONObject("Table");
                string IgnoreTables = Ignore.GetValue("Tables");
                string sTablePrefix = Ignore.GetValue("Prefix");
                string sCamelPrefix = Ignore.GetValue("CamelPrefix");

                string IgnoreTableColumns  = Ignore.GetValue("TableColumns");
                List<string> hIgnoreTables = (IgnoreTables + ",").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                List<string> hIgnoreTableColumns = (IgnoreTableColumns + ",").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                hIgnoreTables.Add("dtproperties");
                hIgnoreTables.Add("sysfunctiondtl");

                Dictionary<string, string> IgnoreHash = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

                foreach (JSONObject _Table in _JSONObject)
                {
                    string _sTableName = _Table.GetValue("sTableName");
                    IgnoreHash[_sTableName]=_sTableName;

                    if (sTablePrefix=="" || _sTableName.StartsWith(sTablePrefix)){
                        if (hIgnoreTables.Contains(_sTableName)) {

                        } else {

                            List<JSONObject> _JSONColumn = _TableUtil.DatabaseTableColumns(_Trans , _sTableName);

                            string sEntityFileName = UtilSeparator.Separator(AppConfigs.DevelopPath + "\\definition\\entity\\"+_sTableName+".json");
                            if (File.Exists(sEntityFileName)) {
                                File.Delete(sEntityFileName);
                            }

                            string sContext="";
                            JSONObject oContext=new JSONObject(sContext);

                            if (string.IsNullOrEmpty(sContext)){
                                string sEntityName = UtilSeparator.Separator(AppConfigs.DevelopPath + "\\definition\\components\\"+_sTableName+".json");
                                if (File.Exists(sEntityName)) {
                                    oContext=UtilSeparator.FileToJSONObject(sEntityName);
                                }
                            }

                            Dictionary<string, string> DataTypeMapping = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                            Dictionary<string, string> Hash = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

                            foreach (JSONObject RsSysFields in _JSONColumn) {
                                string sColumnName = RsSysFields.GetValue("sColumnName");
                                Hash[sColumnName]=sColumnName;
                            }                            

                            foreach (JSONObject obj in oContext.GetJSONArray("Columns").JSONObjects)
                            {
                                if (!Hash.ContainsKey(obj.GetValue("sColumnName"))){
                                    _JSONColumn.Add(obj);
                                }
                                DataTypeMapping[obj.GetValue("sColumnName")]=obj.GetValue("sDataType");
                            }

                            JSONArray _Fields = new JSONArray();

                            foreach (JSONObject RsSysFields in _JSONColumn) {

                                bool bActive=true;
                                if (hIgnoreTableColumns.Contains(RsSysFields.GetValue("sColumnName"))) {
                                    bActive=false;
                                }
                                if (hIgnoreTableColumns.Contains(_sTableName +"."+ RsSysFields.GetValue("sColumnName"))) {
                                    bActive=false;
                                }

                                if (bActive)
                                {

                                    string sDataType   = RsSysFields.GetValue("sDataType");
                                    string sColumnName = RsSysFields.GetValue("sColumnName");
                                    string sTableName  = RsSysFields.GetValue("sTableName");
                                    int nColumnLength  = RsSysFields.GetInteger("nColumnLength");

                                    JSONObject _ColumnEntity = new JSONObject();
                                    _ColumnEntity.SetValue("sTableName"   , sTableName.Trim());
                                    _ColumnEntity.SetValue("sColumnName"  , sColumnName);
                                    _ColumnEntity.SetValue("sDataType"    , sDataType);
                                    _ColumnEntity.SetValue("sComment"     , RsSysFields.GetValue("sComment"));
                                    _ColumnEntity.SetValue("bPrimaryKey"  , RsSysFields.GetBoolean("bPrimaryKey"));
                                    _ColumnEntity.SetValue("bAutoIdentity", RsSysFields.GetBoolean("bAutoIdentity"));

                                    if (RsSysFields.GetValue("sComment").IndexOf("#")>=0)
                                    {
                                        _ColumnEntity.SetValue("bIndexes", true);
                                    }


                                    if (DataTypeMapping.ContainsKey(sColumnName)){
                                        sDataType = DataTypeMapping[sColumnName];
                                    }

                                    if (sDataType == "nvarchar")
                                    {
                                        if (nColumnLength > 1000 || nColumnLength < 0) {
                                            sDataType = "ntext";
                                        }
                                    }

                                    _ColumnEntity.SetValue("sDataType" , DataType(sDataType));

                                    _Fields.Add(_ColumnEntity);
                                }
                            }

                            JSONObject _JSONTableName = new JSONObject();

                            _JSONTableName.SetValue(_sTableName , _Fields);


                            _JSONTableName.SetValue("Property" , oContext.GetJSONObject("Property"));

                            Utils.Util.WriteContents(sEntityFileName , JsonFormatter.PrettyPrint(_JSONTableName.ToString()));

                        }
                    }
                }

                JSONObject Components = AppConfigs.JSONObject("Components");

                foreach (string _Name in Components.GetJSONArray("Tables").Names)
                {
                    if (!IgnoreHash.ContainsKey(_Name)){
                        string sEntityName = UtilSeparator.Separator(AppConfigs.DevelopPath + "\\definition\\components\\"+_Name+".json");
                        if (File.Exists(sEntityName)) {
                            string sEntityFileName = UtilSeparator.Separator(AppConfigs.DevelopPath + "\\definition\\entity\\"+_Name+".json");

                            JSONObject oContext=UtilSeparator.FileToJSONObject(sEntityName);

                            if (File.Exists(sEntityFileName)) {
                                File.Delete(sEntityFileName);
                            }
                            Utils.Util.WriteContents(sEntityFileName , JsonFormatter.PrettyPrint(oContext.ToString()));
                        }
                    }
                }

            } catch {

            }finally{
                _Trans.Close();
            }
        }

        public void GeneratorColumns(DbContext _Trans, string sTableName)
        {
            this.WriteLine("GeneratorColumns sTableName " + sTableName);
            try
            {

                List<JSONObject> _JSONObject = _TableUtil.DatabaseTableColumns(_Trans, sTableName);
                this.WriteLine("Count=" + _JSONObject.Count);

                int i = 0;
                int n = 0;
                string cExclude = "dtproperties,sysfunctiondtl";

                foreach (JSONObject _Fields in _JSONObject)
                {

                    if (cExclude.IndexOf(_Fields.GetValue("sTableName")) >= 0)
                    {
                    }
                    else
                    {
                        string _COLUMN_NAME = _Fields.GetValue("sColumnName");
                        string _sTableName = _Fields.GetValue("sTableName");

                        i++;

                        bool _Write = true;

                        if (sTableName != "" && sTableName.ToUpper() != _sTableName.ToUpper())
                        {
                            _Write = false;
                        }

                        if (_Write)
                        {
                            string _CaptionCode = "lbl_" + _sTableName + "_" + _COLUMN_NAME;

                            var _criteria = QueryBuilder<SysFields>.Builder(_Trans);
                            _criteria.WhereClause("sTableName", Operation.Equal, _sTableName);
                            _criteria.WhereClause("sColumnName", Operation.Equal, _COLUMN_NAME);

                            SysFields _SysFieldsEntity = _Trans.SingleOrDefault<SysFields>(_criteria);

                            if (_SysFieldsEntity == null)
                            {
                                _SysFieldsEntity = new SysFields();

                                _SysFieldsEntity.bAutoIdentity  = _Fields.GetBoolean("bAutoIdentity");
                                _SysFieldsEntity.bColumnNullable = _Fields.GetBoolean("bNullable");
                                _SysFieldsEntity.bPrimaryKey     = _Fields.GetBoolean("bPrimaryKey");
                                _SysFieldsEntity.nColumnLength   = _Fields.GetInteger("nColumnLength");
                                _SysFieldsEntity.nScale    = _Fields.GetInteger("nScale");
                                _SysFieldsEntity.sCaptionCode    = _CaptionCode;
                                _SysFieldsEntity.sColumnName     = _COLUMN_NAME;
                                _SysFieldsEntity.sDataType       = _Fields.GetValue("sDataType");
                                _SysFieldsEntity.sComment        = _Fields.GetValue("sComment");
                                _SysFieldsEntity.sTableName      = _sTableName;

                                if (_SysFieldsEntity.sDataType == "nvarchar")
                                {
                                    if (_Fields.GetInteger("nColumnLength") > 1000 || _Fields.GetInteger("nColumnLength") < 0)
                                    {
                                        _SysFieldsEntity.nColumnLength = 60;
                                        _SysFieldsEntity.sDataType = "ntext";
                                    }
                                    _SysFieldsEntity.nColumnLength = _SysFieldsEntity.nColumnLength / 2;
                                }

                                _SysFieldsEntity.sDataType = DataType(_SysFieldsEntity.sDataType);
                                _SysFieldsEntity.bVerified = true;

                                n++;
                                _Trans.AddNew<SysFields>(_SysFieldsEntity);

                            }
                            else
                            {

                                _SysFieldsEntity.sCaptionCode = _CaptionCode;

                                if (_SysFieldsEntity.sDataType == "picture" && _Fields.GetValue("sDataType") == "nvarchar")
                                {

                                }
                                else
                                {
                                    _SysFieldsEntity.sDataType = _Fields.GetValue("sDataType");
                                }

                                _SysFieldsEntity.bPrimaryKey     = _Fields.GetBoolean("bPrimaryKey");
                                _SysFieldsEntity.bColumnNullable = _Fields.GetBoolean("bNullable");
                                _SysFieldsEntity.bAutoIdentity   = _Fields.GetBoolean("bAutoIdentity");
                                _SysFieldsEntity.sComment        = _Fields.GetValue("sComment");

                                if (_SysFieldsEntity.sDataType == "nvarchar")
                                {
                                    if (_Fields.GetInteger("nColumnLength") > 1000 || _Fields.GetInteger("nColumnLength") < 0)
                                    {
                                        _SysFieldsEntity.nColumnLength = 60;
                                        _SysFieldsEntity.sDataType = "ntext";
                                    }
                                }

                                _SysFieldsEntity.sDataType = DataType(_SysFieldsEntity.sDataType);
                                _SysFieldsEntity.bVerified = true;

                                _Trans.Update<SysFields>(_SysFieldsEntity);
                            }

                        }
                    }

                }

                this.WriteLine("Process New [" + n + "] / Total [" + i + " ] Fields ");

            }
            catch (Exception e)
            {
                this.WriteLine("Exception=" + e.ToString());
                string cMessage = "<HR>Message=[" + e.Message + "]" + "<HR>Source=[" + e.Source + "]<HR>StackTrace=[" + e.StackTrace + "]<HR>TargetSite=[" + e.TargetSite + "]";
                this.WriteLine("Exception=" + cMessage);
            }
        }

        public string DataType(string sType)
        {

            if (sType == "float") {
                sType= "decimal";
            }
            if (sType == "smalldatetime") {
                sType= "datetime";
            }

            if (sType== "bit") {
                sType= "boolean";
            }
            return sType;

        }

        private string FunctionColumnName(DbContext _DbContext,string sUID)
        {
            QueryRows RsSysFunction = new QueryRows(_DbContext);
            string sValue="";

            RsSysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID=@sUID";
            RsSysFunction.SetParameter("sUID", sUID);
            RsSysFunction.Open();

            if (!RsSysFunction.EOF) {
                sValue=RsSysFunction.GetValue("sColumnName");
            }
            RsSysFunction.Close();
            return sValue;
        }

        private string FunctionTableName(DbContext _DbContext , string sUID)
        {

            QueryRows RsSysFunction = new QueryRows(_DbContext);

            string sValue="";

            RsSysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID=@sUID";
            RsSysFunction.SetParameter("sUID", sUID);
            RsSysFunction.Open();

            if (!RsSysFunction.EOF) {
                sValue = RsSysFunction.GetValue("sTableName");
            }
            RsSysFunction.Close();
            return sValue;
        }

        private string Root_LNKUID(DbContext _DbContext , string sUID)
        {

            string sLNKUID = sUID;

            QueryRows RsSysFunction = new QueryRows(_DbContext);

            RsSysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID=@sUID";
            RsSysFunction.SetParameter("sUID", sUID);
            RsSysFunction.Open();

            if (!RsSysFunction.EOF) {
                return sLNKUID;
            }else{
                return sLNKUID;
            }
        }

    }
}
