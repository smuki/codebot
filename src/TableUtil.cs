using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Principal;

using Volte.Data.Dapper;
using Volte.Data.Json;
using Volte.Utils;

namespace Volte.Bot.Term
{
    public class TableUtil {

        const string ZFILE_NAME = "TableUtil";

        private string MarksStart       = "[";
        private string MarksEnd         = "]";
        private string TargetMarksStart = "[";
        private string TargetMarksEnd   = "]";

        private string _AppPath       = "";
        private string _TableName     = "";
        private string _UID_CODE      = "";
        private string _SqlWithDelete = "Y";
        private string _DeleteFile    = "Y";
        private string _AppendOnly    = "Y";
        private string _Unicode       = "N";
        private string _dbAdapter     = "";
        private string _Target        = "MsSqlServer";
        private long   _FileIndex     = 0;
        private AppConfigs AppConfigs = new AppConfigs("");

        private Dictionary<string , bool>   _WriteFile  = new Dictionary<string , bool>();
        public  List<JSONObject> AlterTable             = new List<JSONObject>();
        private Dictionary<string , bool>   _HasTable = new Dictionary<string , bool>();

        private Dictionary<string, List<JSONObject>> _TableColumns = new Dictionary<string, List<JSONObject>>();

        public string Target
        {
            get {
                return _Target;
            }
            set
            {
                _Target = value;

                if (_Target.ToLower()=="mysql"){

                    _Unicode         = "";
                    TargetMarksStart = "`";
                    TargetMarksEnd   = "`";

                }else if (_Target.ToLower()=="mssql"){
                    _Unicode         = "N";
                    TargetMarksStart = "[";
                    TargetMarksEnd   = "]";

                }
            }
        }

        public string AppPath       { get { return _AppPath;       } set { _AppPath       = value; }  }
        public string sUID          { get { return _UID_CODE;      } set { _UID_CODE      = value; }  }
        public string SqlWithDelete { get { return _SqlWithDelete; } set { _SqlWithDelete = value; }  }
        public string DeleteFile    { get { return _DeleteFile;    } set { _DeleteFile    = value; }  }
        public string TableName     { get { return _TableName;     } set { _TableName     = value; }  }
        public string dbAdapter     { get { return _dbAdapter;     } set { _dbAdapter     = value; }  }
        public string AppendOnly    { get { return _AppendOnly;    } set { _AppendOnly    = value; }  }

        private string SysColumnsSQLString(DbContext _Trans, string tableName = "")
        {

            string SQLString = "";
            if (_Trans.Vendor=="MySql") {

                SQLString = @"SELECT
                    COLUMNS.TABLE_NAME AS sTableName,
                    ORDINAL_POSITION AS nIndex,
                    COLUMN_NAME AS sColumnName,
                    COLUMNS.EXTRA AS sAutoIncrement,
                    COLUMN_KEY AS COLUMN_PK_BIT,
                    COLUMN_TYPE AS sDataType,
                    CHARACTER_MAXIMUM_LENGTH AS sColumnLength,
                    NUMERIC_PRECISION AS nNumericPrecision,
                    NUMERIC_SCALE AS sColumnScale,
                    IS_NULLABLE AS sColumnNullable,
                    COLUMN_deFAULT AS sDefault,
                    TABLES.UPDATE_TIME as dChgDate,
                    COLUMNS .*
                        FROM
                        information_schema.COLUMNS join information_schema.TABLES
                        on information_schema.COLUMNS.TABLE_NAME=information_schema.TABLES.TABLE_NAME
                        WHERE
                        COLUMNS.TABLE_SCHEMA = database()
                        AND TABLES.TABLE_SCHEMA = database() $";

                if (tableName != "") {
                    if (tableName.IndexOf("%") >= 0) {
                        SQLString = SQLString.Replace("$", " AND TABLES.table_name like '" + tableName + "'");
                    } else {
                        SQLString = SQLString.Replace("$", " AND TABLES.table_name = '" + tableName + "'");
                    }
                } else if (this.TableName != "") {
                    SQLString = SQLString.Replace("$", " AND TABLES.table_name like '%" + _TableName + "%'");
                } else {
                    SQLString = SQLString.Replace("$", "");
                }

            }else{

                SQLString = @"SELECT d.name AS sTableName,
                          a.colorder AS nIndex, a.name AS sColumnName,d.crdate as dChgDate, CASE WHEN COLUMNPROPERTY(a.id,
                                  a.name, 'IsIdentity') = 1 THEN 'Y' ELSE 'N' END AS sAutoIncrement,
                          CASE WHEN EXISTS
                              (SELECT 1
                               FROM dbo.sysindexes si INNER JOIN
                               dbo.sysindexkeys sik ON si.id = sik.id AND si.indid = sik.indid INNER JOIN
                               dbo.syscolumns sc ON sc.id = sik.id AND sc.colid = sik.colid INNER JOIN
                               dbo.sysobjects so ON so.name = si.name AND so.xtype = 'PK'
                               WHERE sc.id = a.id AND sc.colid = a.colid) THEN 'Y' ELSE 'N' END AS COLUMN_PK_BIT,
                          b.name AS sDataType, COLUMNPROPERTY(a.id,a.name,'PRECISION') AS sColumnLength, a.prec AS nNumericPrecision, ISNULL(COLUMNPROPERTY(a.id, a.name, 'Scale'), 0) AS sColumnScale,
                          CASE WHEN a.isnullable = 1 THEN 'Y' ELSE 'N' END AS sColumnNullable, ISNULL(e.text, '')
                              AS sDefault
                              FROM dbo.syscolumns a LEFT OUTER JOIN
                              dbo.systypes b ON a.xtype = b.xusertype INNER JOIN
                              dbo.sysobjects d ON a.id = d.id AND d.xtype = 'U' AND
                              d.status >= 0 LEFT OUTER JOIN
                              dbo.syscomments e ON a.cdefault = e.id LEFT OUTER JOIN
                              sys.extended_properties g ON a.id = g.major_id AND a.colid = g.minor_id AND
                              g.name = 'MS_Description' LEFT OUTER JOIN
                              sys.extended_properties f ON d.id = f.major_id AND f.minor_id = 0 AND
                              f.name = 'MS_Description'
                              $
                              ORDER BY d.name, nIndex";


                if (tableName != "") {
                    if (tableName.IndexOf("%") >= 0) {
                        SQLString = SQLString.Replace("$", " Where d.name like N'" + tableName + "'");
                    } else {
                        SQLString = SQLString.Replace("$", " Where d.name = N'" + tableName + "'");
                    }
                } else if (this.TableName != "") {
                    SQLString = SQLString.Replace("$", " Where d.name like N'%" + _TableName + "%'");
                } else {
                    SQLString = SQLString.Replace("$", "");
                }
            }


            return SQLString;
        }

        public bool HasTable(DbContext _Trans, string tableName)
        {
            return HasTable(_Trans, tableName, "");
        }

        public bool HasTable(DbContext _Trans, string tableName, string sColumnName)
        {
            bool _value = true;

            string sKey = tableName + "_" + sColumnName;
            sKey = sKey.ToLower();

            if (_HasTable.ContainsKey(sKey.ToLower())) {

                _value = _HasTable[sKey.ToLower()];

            } else {
                QueryRows _sysobjects = new QueryRows(_Trans);

                string SQLString="";

                if (sColumnName == "") {


                    if (_Trans.Vendor=="MySql") {

                        SQLString = "SELECT * from INFORMATION_SCHEMA.TABLES Where TABLE_SCHEMA = database() AND TABLE_NAME = '${sTableName}';";

                    }else{
                        SQLString="SELECT name from sysobjects where name = '${sTableName}'";
                    }
                    SQLString = SQLString.Replace("${sTableName}" , tableName);
                    _sysobjects.CommandText = SQLString;

                } else {

                    if (_Trans.Vendor=="MySql") {
                        SQLString = "show columns from `${sTableName}` like '${sColumnName}'";
                    }else{
                        SQLString = "SELECT * FROM SysColumns WHERE name = N'${sTableName}' and [id] = object_id(N'${sColumnName}')";
                    }

                    SQLString = SQLString.Replace("${sTableName}"  , tableName);
                    SQLString = SQLString.Replace("${sColumnName}" , sColumnName);
                    _sysobjects.CommandText = SQLString;
                }
                _sysobjects.Open();

                if (_sysobjects.EOF) {
                    _value = false;
                }

                _HasTable[tableName.ToLower()] = _value;
            }

            return _value;
        }

        public List<JSONObject> DatabaseTable(DbContext _Trans , string tableName)
        {
            List<JSONObject> _NameValues = new List<JSONObject>();

            try {

                tableName         = tableName.ToLower();
                tableName         = tableName.Replace("$", "%");
                tableName         = tableName.Replace("*", "%");
                QueryRows _Fields = new QueryRows(_Trans);

                string SQLString= "";

                if (_Trans.Vendor=="MySql") {

                    SQLString = "SELECT TABLE_NAME as Name from INFORMATION_SCHEMA.TABLES Where TABLE_SCHEMA = database();";
                    if (tableName == "%" || tableName != "") {

                        SQLString = "SELECT TABLE_NAME as Name from INFORMATION_SCHEMA.TABLES Where TABLE_SCHEMA = database() AND TABLE_NAME LIKE '%"+tableName+"%';";
                    }

                }else{
                    SQLString= "SELECT Name FROM SysObjects WHERe xtype='U' ORDER BY Name";

                    if (tableName == "%" || tableName != "") {

                        SQLString = "SELECT Name FROM SysObjects WHERe xtype = 'U' AND Name Like '%" + tableName + "%' ORDER BY Name";
                    }

                }
                _Fields.CommandText = SQLString;

                _Fields.Open();
                string _ignore = "dtproperties,";

                while (!_Fields.EOF) {
                    if (_ignore.IndexOf(_Fields.GetValue("Name")) < 0) {
                        JSONObject _Column = new JSONObject();
                        _Column.SetValue("sTableName" , _Fields.GetValue("Name"));

                        _NameValues.Add(_Column);

                    }

                    _Fields.MoveNext();
                }


            } catch (Exception ex) {
                string cMessage = "\nMessage=[" + ex.Message + "]" + "\nSource=[" + ex.Source + "]\nStackTrace=[" + ex.StackTrace + "]\nTargetSite=[" + ex.TargetSite + "]";
                Console.WriteLine(cMessage);
                ZZLogger.Trace(ZFILE_NAME , ex);

                throw new Exception(cMessage);
            }

            return _NameValues;
        }

        public List<JSONObject> TableIndex(DbContext _Trans , string tableName)
        {
            List<JSONObject> _NameValues = new List<JSONObject>();

            try {

                tableName         = tableName.ToLower();
                tableName         = tableName.Replace("$", "%");
                tableName         = tableName.Replace("*", "%");
                QueryRows _Fields = new QueryRows(_Trans);

                string SQLString= "";

                if (_Trans.Vendor=="MySql") {

                    string sColumns = "TABLE_NAME,INDEX_NAME,INDEX_TYPE,GROUP_CONCAT( DISTINCT CONCAT('`', COLUMN_NAME, '`') ORDER BY SEQ_IN_INDEX ASC SEPARATOR ', ') as COLUMN_NAME";

                    SQLString = "SELECT "+sColumns+" from INFORMATION_SCHEMA.STATISTICS Where TABLE_SCHEMA = database() GROUP BY TABLE_NAME,INDEX_NAME,INDEX_TYPE;";
                    if (tableName == "%") {

                        SQLString = "SELECT "+sColumns+" from INFORMATION_SCHEMA.STATISTICS Where TABLE_SCHEMA = database() AND TABLE_NAME LIKE '%"+tableName+"%' GROUP BY TABLE_NAME,INDEX_NAME,INDEX_TYPE;";
                    }else if (tableName != ""){

                        SQLString = "SELECT "+sColumns+" from INFORMATION_SCHEMA.STATISTICS Where TABLE_SCHEMA = database() AND TABLE_NAME='"+tableName+"' GROUP BY TABLE_NAME,INDEX_NAME,INDEX_TYPE;";
                    }

                }else{
                    SQLString= "SELECT Name FROM SysObjects WHERe xtype='U' ORDER BY Name";

                    if (tableName == "%" || tableName != "") {

                        SQLString = "SELECT Name FROM SysObjects WHERe xtype = 'U' AND Name Like '%" + tableName + "%' ORDER BY Name";
                    }

                }
                _Fields.CommandText = SQLString;
                _Fields.Open();

                while (!_Fields.EOF) {
                    JSONObject _Column = new JSONObject();
                    _Column.SetValue("sTableName"  , _Fields.GetValue("TABLE_NAME"));
                    _Column.SetValue("sIndexName"  , _Fields.GetValue("INDEX_NAME"));
                    _Column.SetValue("sColumnName" , _Fields.GetValue("COLUMN_NAME"));
                    _Column.SetValue("sIndexType"  , _Fields.GetValue("INDEX_TYPE"));

                    _NameValues.Add(_Column);

                    _Fields.MoveNext();
                }


            } catch (Exception ex) {
                string cMessage = "\nMessage=[" + ex.Message + "]" + "\nSource=[" + ex.Source + "]\nStackTrace=[" + ex.StackTrace + "]\nTargetSite=[" + ex.TargetSite + "]";
                Console.WriteLine(cMessage);
                ZZLogger.Trace(ZFILE_NAME , ex);

                throw new Exception(cMessage);
            }

            return _NameValues;
        }

        public JSONObject DumpToJSONObject(DbContext _Trans , string sTableName)
        {

            List<JSONObject> _NameValues = this.DatabaseTableColumns(_Trans , sTableName);

            JSONArray tColumns = new JSONArray();
            string sBefName = "FIRST";
            foreach (JSONObject colname in _NameValues) {
                string sDefault = colname.GetValue("sDefault");
                sDefault        = sDefault.Replace("(" , "");
                sDefault        = sDefault.Replace(")" , "");

                JSONObject col = new JSONObject();
                col.SetValue("sColumnName"    , colname.GetValue("sColumnName"));
                col.SetValue("sDataType"      , colname.GetValue("sDataType"));
                col.SetValue("nScale"         , colname.GetInteger("nColumnScale"));
                col.SetValue("nLength"        , colname.GetInteger("nColumnLength"));
                col.SetValue("bNullable"      , colname.GetBoolean("bColumnNullable"));
                col.SetValue("bAutoIdentity" , colname.GetBoolean("bAutoIdentity"));
                col.SetValue("bPrimaryKey"    , colname.GetBoolean("bPrimaryKey"));
                col.SetValue("sDefault"       , sDefault);
                col.SetValue("sBefName"       , sBefName);
                col.SetValue("nIndex"         , colname.GetInteger("nIndex"));

                sBefName = colname.GetValue("sColumnName");
                tColumns.Add(col);
            }

            JSONArray tIndexName = new JSONArray();

            List<JSONObject> vV = TableIndex(_Trans , sTableName);
            foreach (JSONObject colname in vV) {

                JSONObject col = new JSONObject();
                col.SetValue("sTableName"  , colname.GetValue("sTableName"));
                col.SetValue("sColumnName" , colname.GetValue("sColumnName"));
                col.SetValue("sIndexName"  , colname.GetValue("sIndexName"));
                col.SetValue("sIndexType"  , colname.GetValue("sIndexType"));

                tIndexName.Add(col);
            }

            JSONObject Columns = new JSONObject();
            Columns.SetValue("sTableName" , sTableName);
            Columns.SetValue("Columns"    , tColumns);
            Columns.SetValue("Indexs"     , tIndexName);

            return Columns;
        }

        public JSONObject DumpToJSONObject(DbContext _Trans , List<string> vTableName)
        {
            JSONObject vDatabase = new JSONObject();
            foreach (string sTableName in vTableName) {
                vDatabase.SetValue(sTableName , this.DumpToJSONObject(_Trans , sTableName));
            }
            return vDatabase;
        }

        public JSONObject DumpToJSONObject(DbContext _Trans)
        {
            List<string> vTableName=new List<string>();
            JSONObject vDatabase = new JSONObject();
            foreach (JSONObject kvp in this.DatabaseTable(_Trans, "")) {
                string sTableName = kvp.GetValue("sTableName");
                vDatabase.SetValue(sTableName , this.DumpToJSONObject(_Trans , sTableName));
            }
            return vDatabase;
        }


        private int ToInt32(object oValue)
        {
            if (oValue==null){
                return 0;
            }
            int d;
            return int.TryParse(oValue.ToString(), out d) ? d : 0;
        }

        public List<JSONObject> DatabaseTableColumns(DbContext _Trans, string tableName)
        {
            List<JSONObject> _NameValues = new List<JSONObject>();

            try {

                tableName  = tableName.ToLower();
                string md5 = DapperUtil.ComputeHash(tableName);

                //ZZLogger.Trace(ZFILE_NAME , md5);
                //ZZLogger.Trace(ZFILE_NAME , tableName);

                if (_TableColumns.ContainsKey(md5)) {
                    return _TableColumns[md5];
                }

                string SQLString = SysColumnsSQLString(_Trans, tableName);

                //ZZLogger.Trace(ZFILE_NAME , SQLString);

                QueryRows _Fields = new QueryRows(_Trans);
                _Fields.CommandText = SQLString;
                _Fields.Open();

                while (!_Fields.EOF) {
                    string _sColumnName    = _Fields.GetValue("sColumnName");
                    string _TABLE_NAME     = _Fields.GetValue("sTableName");
                    string sDataType       = _Fields.GetValue("sDataType");
                    long sColumnLength     = _Fields.GetLong("sColumnLength");
                    long nNumericPrecision = _Fields.GetLong("nNumericPrecision");
                    sDataType              = sDataType+"(";
                    sDataType              = sDataType.Replace(")" , "(");
                    string[] aDataType     = sDataType.Split('(');
                    sDataType              = aDataType[0];

                    if (sColumnLength==0 && aDataType.Length>1){

                        sColumnLength =  this.ToInt32(aDataType[1]);
                    }
                    if (sColumnLength==0){

                        sColumnLength = nNumericPrecision;
                    }

                    if (this.Target == "MySql"){

                        _TABLE_NAME = _TABLE_NAME.ToLower();
                    }

                    JSONObject _Column = new JSONObject();
                    _Column.SetValue("sColumnName"       , _sColumnName);
                    _Column.SetValue("sTableName"        , _TABLE_NAME);
                    _Column.SetValue("nColumnScale"      , _Fields.GetInteger("sColumnScale"));
                    _Column.SetValue("nIndex"            , _Fields.GetInteger("nIndex"));
                    _Column.SetValue("sDataType"         , sDataType);
                    _Column.SetValue("nColumnLength"     , sColumnLength);
                    _Column.SetValue("bPrimaryKey"        , Utils.Util.StringToBoolean(_Fields.GetValue("COLUMN_PK_BIT")) || _Fields.GetValue("COLUMN_PK_BIT") == "PRI" );
                    _Column.SetValue("bColumnNullable"   , Utils.Util.StringToBoolean(_Fields.GetValue("sColumnNullable")));
                    string sAutoIncrement = _Fields.GetValue("sAutoIncrement");

                    if (sAutoIncrement.IndexOf("auto_increment")>=0){
                        _Column.SetValue("bAutoIdentity" , true);
                    }else if (sAutoIncrement=="Y"){
                        _Column.SetValue("bAutoIdentity" , true);
                    }else if (sAutoIncrement=="N"){
                        _Column.SetValue("bAutoIdentity" , false);
                    }else{
                        _Column.SetValue("bAutoIdentity" , false);
                    }
                    _Column.SetValue("sDefault"          , _Fields.GetValue("sDefault"));
                    _Column.SetValue("nNumericPrecision" , _Fields.GetValue("nNumericPrecision"));

                    _NameValues.Add(_Column);

                    _Fields.MoveNext();
                }

                //ZZLogger.Trace(ZFILE_NAME , _NameValues.Count);
                _TableColumns[md5] = _NameValues;


            } catch (Exception ex) {
                string cMessage = "\nMessage=[" + ex.Message + "]" + "\nSource=[" + ex.Source + "]\nStackTrace=[" + ex.StackTrace + "]\nTargetSite=[" + ex.TargetSite + "]";
                Console.WriteLine(cMessage);
                ZZLogger.Trace(ZFILE_NAME , ex);

            }

            return _NameValues;
        }

        private string FormatName(string name)
        {
            return TargetMarksStart + name + TargetMarksEnd;
        }

        private string FieldClause(string name)
        {
            return TargetMarksStart + name + TargetMarksEnd;
        }

        public string FieldClause(JSONObject field, bool onlyDefine)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("{0} ", FormatName(field.GetValue("sColumnName")));

            string typeName = field.GetValue("sDataType");

            typeName = GetFieldType(field);

            sb.Append(typeName);

            sb.Append(GetFieldConstraints(field, onlyDefine));

            if (field.GetBoolean("bAutoIdentity")){
                sb.Append(" AUTO_INCREMENT ");
            }
            return sb.ToString();
        }

        private string GetFieldConstraints(JSONObject field, bool onlyDefine)
        {
            string sql = "";
            if (field.GetBoolean("bNullable")){
                sql = " NULL ";
                if (string.IsNullOrEmpty(field.GetValue("sDefault"))){
                    sql = sql+" DEFAULT NULL";
                }else{
                    string type = field.GetValue("sDataType");

                    if (type== "nvarchar"
                            || type== "varchar"
                            || type== "ntext"
                            || type== "text"
                            || type== "nchar"
                            || type== "char"){
                        sql = sql+" DEFAULT '"+field.GetValue("sDefault")+"'";
                    }else{
                        sql = sql+" DEFAULT "+field.GetValue("sDefault");
                    }
                }
            }else{
                sql = sql+" NOT NULL ";
            }
            return sql;
        }

        private string GetFieldType(JSONObject field)
        {
            string type     = field.GetValue("sDataType");
            string typeName = type;

            if (type== "nvarchar"
                    || type== "varchar"
                    || type== "ntext"
                    || type== "nchar"
                    || type== "char"){

                typeName = type+"("+field.GetInteger("nLength")+")";
            }else if (type=="int"){
                typeName = type+"(11)";
            }else if (type=="bitint"){
                typeName = type+"(20)";
            }else if (type=="tinyint"){
                typeName = type+"(4)";
            }else if (type=="number" ||  type=="decimal"){
                typeName = type+"("+field.GetInteger("nLength")+","+field.GetInteger("nScale")+")";
            }

            return typeName;
        }

        public string AddNewColumnSQL(string sTableName , JSONObject field , string sAfter) {

            string sql = "Alter Table "+FormatName(sTableName)+" Add Column "+FieldClause(field , true);
            if (field.GetBoolean("bAutoIdentity")){
                sql = sql +" AUTO_INCREMENT ";
            }
            if (string.IsNullOrEmpty(sAfter)){
                sql = sql +" FIRST";
            }else{
                sql = sql +" AFTER "+FormatName(sAfter);
            }
            return sql+";";
        }

        public string ChangeColumnSQL(string sTableName , JSONObject field , string sAfter){

            string sql = "Alter Table "+FormatName(sTableName)+" Modify Column "+FieldClause(field , false);
            if (field.GetBoolean("bAutoIdentity")){
                sql = sql +" AUTO_INCREMENT ";
            }
            if (string.IsNullOrEmpty(sAfter)){
                sql = sql +" FIRST";
            }else{
                sql = sql +" AFTER "+FormatName(sAfter);
            }
            return sql+";";
        }

        public List<string> DetectColumn(DbContext _Trans , JSONObject table , string sMode)
        {
            string _tTarget   = _Target;
            this.Target       = "MySql";
            string sTableName = table.GetValue("sTableName");
            string sSQLString = "";

            List<string> vSQLs=new List<string>();
            if (!HasTable(_Trans , sTableName)){

                sSQLString = MysqlCreateTableSQL(table);
                vSQLs.Add(sSQLString);
            }else{

                List<JSONObject> _TargetColumns = this.DatabaseTableColumns(_Trans , sTableName);

                Dictionary<string , JSONObject> _TargetColumnsHash = new Dictionary<string , JSONObject>();
                Dictionary<string , JSONObject> _SourceColumnsHash = new Dictionary<string , JSONObject>(StringComparer.OrdinalIgnoreCase);

                string sBefName = "FIRST";
                foreach (JSONObject colname in _TargetColumns) {
                    string sDefault    = colname.GetValue("sDefault");
                    string sColumnName = colname.GetValue("sColumnName");

                    sDefault = sDefault.Replace("(" , "");
                    sDefault = sDefault.Replace(")" , "");

                    JSONObject col = new JSONObject();
                    col.SetValue("sColumnName"    , sColumnName);
                    col.SetValue("sDataType"      , colname.GetValue("sDataType"));
                    col.SetValue("nScale"         , colname.GetInteger("nColumnScale"));
                    col.SetValue("nLength"        , colname.GetInteger("nColumnLength"));
                    col.SetValue("bNullable"      , colname.GetBoolean("bColumnNullable"));
                    col.SetValue("bAutoIdentity" , colname.GetBoolean("bAutoIdentity"));
                    col.SetValue("bPrimaryKey"    , colname.GetBoolean("bPrimaryKey"));
                    col.SetValue("sDefault"       , sDefault);
                    col.SetValue("sBefName"       , sBefName);
                    col.SetValue("nIndex"         , colname.GetInteger("nIndex"));

                    sBefName = sColumnName;

                    _TargetColumnsHash[sColumnName.ToLower()] = col;
                }

                List<JSONObject> zTargetIndexs = this.TableIndex(_Trans , sTableName);
                Dictionary<string , JSONObject> _HashTargetIndex   = new Dictionary<string , JSONObject>(StringComparer.OrdinalIgnoreCase);
                foreach(JSONObject field in zTargetIndexs){
                    string sIndexName = field.GetValue("sIndexName");
                    _HashTargetIndex[sIndexName] = field;
                }

                JSONArray oSourceColumn = table.GetJSONArray("Columns");

                StringBuilder sb = new StringBuilder();

                string sAfter       = "";
                bool bModifyPrimary = false;
                List<string> pks    = new List<String>();

                foreach(JSONObject field in oSourceColumn.JSONObjects){

                    string sColumnName = field.GetValue("sColumnName").ToLower();

                    _SourceColumnsHash[sColumnName] = field;

                    if (!_TargetColumnsHash.ContainsKey(sColumnName)){
                        sSQLString = AddNewColumnSQL(sTableName , field , sAfter);
                        vSQLs.Add(sSQLString);
                    }else{
                        JSONObject TargetCol = _TargetColumnsHash[sColumnName];
                        bool change  = false;

                        StringBuilder s = new StringBuilder();
                        s.AppendLine(sTableName+"."+sColumnName);
                        if (TargetCol.GetValue("sDataType")!=field.GetValue("sDataType")){
                            s.AppendLine(" Type          "+TargetCol.GetValue("sDataType") +"->"+ field.GetValue("sDataType"));
                            change=true;
                        }
                        if (TargetCol.GetBoolean("bAutoIdentity")!=field.GetBoolean("bAutoIdentity")){
                            s.AppendLine(" bAutoIdentity "+TargetCol.GetBoolean("bAutoIdentity") +"->"+ field.GetBoolean("bAutoIdentity"));
                            change=true;
                        }
                        if ("order"==sMode.ToLower()){
                            if (TargetCol.GetValue("sBefName")!=field.GetValue("sBefName")){
                                s.AppendLine(" Prev          "+TargetCol.GetValue("sBefName") +"->"+ field.GetValue("sBefName"));
                                change=true;
                            }
                            if (TargetCol.GetInteger("nIndex")!=field.GetInteger("nIndex")){
                                s.AppendLine(" nIndex        "+TargetCol.GetInteger("nIndex") +"->"+ field.GetInteger("nIndex"));
                                change=true;
                            }
                        }
                        if (TargetCol.GetInteger("nScale")!=field.GetInteger("nScale")){
                            s.AppendLine(" Scale         "+TargetCol.GetInteger("nScale") +"->"+ field.GetInteger("nScale"));
                            change=true;
                        }
                        if (TargetCol.GetInteger("nLength")!=field.GetInteger("nLength")){
                            s.AppendLine(" Length        "+TargetCol.GetInteger("nLength") +"->"+ field.GetInteger("nLength"));
                            change=true;
                        }
                        if (TargetCol.GetBoolean("bNullable")!=field.GetBoolean("bNullable")){
                            s.AppendLine("  Nullable     "+TargetCol.GetBoolean("bNullable") +"->"+ field.GetBoolean("bNullable"));
                            change=true;
                        }
                        if (TargetCol.GetValue("sDefault")!=field.GetValue("sDefault")){
                            s.AppendLine("  Default      "+TargetCol.GetValue("sDefault") +"->"+ field.GetValue("sDefault"));
                            change=true;
                        }
                        if (change){

                            Console.WriteLine(s.ToString());
                            Console.WriteLine("");

                            sSQLString = ChangeColumnSQL(sTableName , field , sAfter);
                            vSQLs.Add(sSQLString);
                            if (TargetCol.GetBoolean("bPrimaryKey")){
                                bModifyPrimary=true;
                            }

                        }
                    }
                    if (field.GetBoolean("bPrimaryKey")){
                        pks.Add(FormatName(field.GetValue("sColumnName")));
                    }
                    sAfter = field.GetValue("sColumnName");
                }


                if (bModifyPrimary){

                    foreach(JSONObject item in oSourceColumn.JSONObjects){
                        if (item.GetBoolean("bIdentity") && !item.GetBoolean("bPrimaryKey")) {
                            pks.Clear();
                            pks.Add(FormatName(item.GetValue("sColumnName")));
                            break;
                        }
                    }
                    if (pks.Count > 0) {

                        sSQLString = "ALTER TABLE "+FormatName(sTableName)+" DROP PRIMARY KEY;";
                        vSQLs.Add(sSQLString);

                        sSQLString = "ALTER TABLE "+FormatName(sTableName)+" ADD PRIMARY KEY ("+String.Join("," , pks.ToArray())+");";
                        vSQLs.Add(sSQLString);
                    }
                }


                JSONArray oSourceIndex = table.GetJSONArray("Indexs");
                Dictionary<string , JSONObject> _HashSourceIndex   = new Dictionary<string , JSONObject>(StringComparer.OrdinalIgnoreCase);
                foreach(JSONObject field in oSourceIndex.JSONObjects){

                    string sIndexName  = field.GetValue("sIndexName");
                    string sColumnName = field.GetValue("sColumnName");
                    string sIndexType  = field.GetValue("sIndexType");
                    sIndexType         = "";

                    _HashSourceIndex[sIndexName] = field;
                    if (!_HashTargetIndex.ContainsKey(sIndexName)){

                        if (sIndexName=="PRIMARY"){
                            sSQLString = "ALTER TABLE "+FormatName(sTableName)+" ADD PRIMARY KEY ("+sColumnName+");";
                            vSQLs.Add(sSQLString);
                        }else{
                            sSQLString = "ALTER TABLE "+FormatName(sTableName)+" ADD INDEX "+FormatName(sIndexName)+"("+sColumnName+") "+sIndexType+";";
                            vSQLs.Add(sSQLString);

                            Console.WriteLine("Add Index:");
                            Console.WriteLine("  "+sTableName+"."+sIndexName);
                        }
                    }else{
                        JSONObject c = _HashTargetIndex[sIndexName];
                        if (c.GetValue("sIndexName")!=sIndexName
                                || c.GetValue("sColumnName")!=sColumnName
                           )
                        {

                            if (sIndexName=="PRIMARY"){

                                sSQLString = "ALTER TABLE "+FormatName(sTableName)+" DROP PRIMARY KEY;";
                                vSQLs.Add(sSQLString);

                                sSQLString = "ALTER TABLE "+FormatName(sTableName)+" ADD PRIMARY KEY ("+sColumnName+");";
                                vSQLs.Add(sSQLString);
                            }else{
                                Console.WriteLine("change Index:");
                                Console.WriteLine("  "+sTableName+"."+sIndexName);
                                sSQLString = "ALTER TABLE "+FormatName(sTableName)+" DROP INDEX "+FormatName(sIndexName)+";";
                                vSQLs.Add(sSQLString);

                                sSQLString = "ALTER TABLE "+FormatName(sTableName)+" ADD INDEX "+FormatName(sIndexName)+" ("+sColumnName+") "+sIndexType+";";
                                vSQLs.Add(sSQLString);
                            }
                        }
                    }
                }

                if ("delete"==sMode.ToLower()){
                    foreach (JSONObject colname in _TargetColumns) {
                        string sColumnName = colname.GetValue("sColumnName");

                        if (!_SourceColumnsHash.ContainsKey(sColumnName)){

                            Console.WriteLine("ALTER TABLE "+FormatName(sTableName)+" DROP "+FormatName(sColumnName)+";");

                        }
                    }
                    foreach(JSONObject field in zTargetIndexs){
                        string sIndexName = field.GetValue("sIndexName");
                        if (!_HashSourceIndex.ContainsKey(sIndexName)){
                            Console.WriteLine("ALTER TABLE "+FormatName(sTableName)+" DROP INDEX "+FormatName(sIndexName)+";");
                        }
                    }
                }
            }

            this.Target = _tTarget;
            return vSQLs;

        }

        public string MysqlAlterAddColumnSQL(JSONObject table)
        {

            JSONArray fs = table.GetJSONArray("Columns");

            StringBuilder sb = new StringBuilder();

            string sAfter = "";
            foreach(JSONObject field in fs.JSONObjects){

                string alt = AddNewColumnSQL(table.GetValue("sTableName") , field , sAfter);
                Console.WriteLine(alt);
                sAfter     = field.GetValue("sColumnName");
            }
            return sb.ToString();
        }

        public string MysqlAlterTableSQL(JSONObject table)
        {

            JSONArray fs = table.GetJSONArray("Columns");

            StringBuilder sb = new StringBuilder();

            string sAfter = "";
            foreach(JSONObject field in fs.JSONObjects){

                string alt = ChangeColumnSQL(table.GetValue("sTableName") , field , sAfter);
                sAfter     = field.GetValue("sColumnName");
            }
            return sb.ToString();
        }

        public string MysqlCreateTableSQL(JSONObject table)
        {

            string _tTarget = _Target;
            this.Target     = "MySql";

            JSONArray fs = table.GetJSONArray("Columns");

            StringBuilder sb = new StringBuilder();
            List<string> pks = new List<String>();

            sb.AppendFormat("Create Table If Not Exists {0}(", FormatName(table.GetValue("sTableName")));
            int i=0;
            foreach(JSONObject field in fs.JSONObjects){
                sb.AppendLine();
                sb.Append("\t");
                sb.Append(FieldClause(field , true));
                if (i < fs.JSONObjects.Count - 1){
                    sb.Append(",");
                }

                if (field.GetBoolean("bPrimaryKey")){
                    pks.Add(FormatName(field.GetValue("sColumnName")));
                }
                i++;
            }

            foreach(JSONObject item in fs.JSONObjects){
                if (item.GetBoolean("bIdentity") && !item.GetBoolean("bPrimaryKey"))
                {
                    pks.Clear();
                    pks.Add(FormatName(item.GetValue("sColumnName")));
                    break;
                }
            }
            if (pks.Count > 0)
            {
                sb.AppendLine(",");
                sb.AppendFormat("\tPRIMARY KEy ({0})", String.Join(",", pks.ToArray()));
            }
            sb.AppendLine();
            sb.Append(")");

            // ?????ͱ???
            sb.Append(" DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci ROW_FORMAT=DYNAMIC");
            sb.Append(";");

            Console.WriteLine(sb.ToString());

            this.Target = _tTarget;

            return sb.ToString();
        }

        private void Console_Write(string message, int CursorPosition = -1)
        {
            try {

                if (CursorPosition >= 0) {
                    Console.SetCursorPosition(CursorPosition, Console.CursorTop);
                }

                Console.Write(message);
            } catch (Exception sErrMsg) {

            }
        }

        public void GenerateSqlInserts(DbContext _Trans, string tableName)
        {
            GenerateSqlInserts(_Trans, tableName + ".sql", tableName);
        }

        public void GenerateSqlInserts(DbContext _Trans, string fileName, string tableName) {

            GenerateSqlInserts(_Trans , fileName , tableName,"");
        }

        public JSONArray GenerateJSON(DbContext _Trans , string tableName , string sQuery , string sRptColumn)
        {
            List<JSONObject> aryColumns = DatabaseTableColumns(_Trans , tableName);
            StringBuilder sbColumns = new StringBuilder(string.Empty);

            if (aryColumns.Count == 0) {
                return new JSONArray();
            }

            foreach (JSONObject colname in aryColumns) {

                if (colname.GetBoolean("bPrimaryKey")) {

                    if (sbColumns.ToString() != string.Empty) {
                        sbColumns.Append(", ");
                    }

                    sbColumns.Append(MarksStart + colname.GetValue("sColumnName") + MarksEnd);
                }
            }

            string sql=sQuery;
            if (sql==""){
                sql="SELECT * FROM " + MarksStart + tableName + MarksEnd;
            }
            return GenerateJSON(_Trans , sql , aryColumns , sRptColumn);
        }

        public JSONArray GenerateJSON(DbContext _Trans , string sQuery , List<JSONObject> aryColumns , string sRptColumn)
        {
            Console.WriteLine(sQuery);

            string _WithDeleteMode = SqlWithDelete;

            sQuery = sQuery.Replace("%sUID%"   , this.sUID);

            string sSqlInserts            = string.Empty;
            StringBuilder sbSqlStatements = new StringBuilder(string.Empty);
            StringBuilder sbColumns       = new StringBuilder(string.Empty);
            StringBuilder sbColumns2      = new StringBuilder(string.Empty);
            string _TableName             = "";

            foreach (JSONObject colname in aryColumns) {
                _TableName = colname.GetValue("sTableName");

                if (sbColumns.ToString() != string.Empty) {
                    sbColumns.Append(", ");
                }

                sbColumns.Append(TargetMarksStart + colname.GetValue("sColumnName") + TargetMarksEnd);
            }

            if (this.Target=="MySql"){

                _TableName = _TableName.ToLower();
            }

            sbColumns2 = sbColumns;

            int _row       = 0;
            int _rows      = 0;
            int _file_rows = 0;

            JSONArray rows = new JSONArray();

            try {

                IDataReader dr = _Trans.DataReader(sQuery);

                // loop thru each record of the datatable
                while (dr.Read()) {
                    // loop thru each column, and include the value if the column is in the array
                    StringBuilder sbKeyValues = new StringBuilder(string.Empty);

                    JSONObject row = new JSONObject();
                    foreach (JSONObject colname in aryColumns) {

                        // need to do a case to check the column-value types(quote strings(check for dups first), convert bools)
                        string col   = colname.GetValue("sColumnName");
                        string sType = colname.GetValue("sDataType");

                        try {

                            //Console.WriteLine(col+"="+sType.Trim().ToLower());
                            switch (sType.Trim().ToLower()) {
                                case "bit":

                                    if (dr[col] == System.DBNull.Value) {
                                        row.SetValue(col , false);
                                    } else {
                                        row.SetValue(col , System.Convert.ToBoolean(dr[col]));
                                    }

                                    break;

                                case "nvarchar":
                                case "varchar":
                                case "ntext":
                                case "text":
                                case "longtext":
                                case "mediumtext":
                                case "nchar":
                                case "char":

                                    row.SetValue(col , string.Format("{0}" , dr[col]));

                                    if (colname.GetBoolean("bPrimaryKey")) {
                                        if (sbKeyValues.ToString() != string.Empty) {
                                            sbKeyValues.Append(" AND ");
                                        }

                                        sbKeyValues.Append(string.Format("{0}="+_Unicode+"'{1}'", col, QuoteString(dr[col])));
                                    }

                                    break;

                                case "smalldatetime":
                                case "datetime":
                                case "date":
                                    string sDateTime = QuoteString(dr[col]);

                                    if (IsDateTime(sDateTime) == true) {

                                        row.SetDateTime(col , (DateTime)dr[col]);
                                    } else {
                                        row.SetDateTime(col , null);
                                    }

                                    if (colname.GetBoolean("bPrimaryKey")) {
                                        if (sbKeyValues.ToString() != string.Empty) {
                                            sbKeyValues.Append(" AND ");
                                        }

                                        sbKeyValues.Append(string.Format("{0}='{1}'", col, sDateTime));
                                    }

                                    break;

                                case "system.byte[]":
                                    row.SetValue(col , System.Convert.ToBase64String((byte[]) dr[col]));
                                    break;

                                case "varbinary":


                                    byte[] _bytes   = (byte[])dr[col];
                                    StringBuilder s = new StringBuilder(string.Empty);

                                    for (int i = 0; i < _bytes.Length; i++) {
                                        s.Append(_bytes[i].ToString("X2"));
                                    }

                                    //sbValues.Append("0x" + s);
                                    row.SetValue(col , s);

                                    break;

                                case "decimal":
                                case "int":
                                case "float":
                                case "tinyint":
                                case "smallint":
                                case "bigint":
                                    if (dr[col] == System.DBNull.Value) {
                                        row.SetValue(col , 0);
                                    } else {
                                        row.SetValue(col , dr[col]);

                                        if (colname.GetBoolean("bPrimaryKey")) {
                                            if (sbKeyValues.ToString() != string.Empty) {
                                                sbKeyValues.Append(" AND ");
                                            }

                                            sbKeyValues.Append(string.Format("{0}={1}", col, System.Convert.ToString(dr[col])));
                                        }
                                    }

                                    break;

                                default:
                                    //Console.WriteLine(sType.Trim().ToLower());

                                    if (dr[col] == System.DBNull.Value) {
                                        //sbValues.Append("NULL");
                                        //row.SetValue(col , null);
                                    } else {
                                        //sbValues.Append(System.Convert.ToString(dr[col]));
                                        row.SetValue(col , dr[col]);

                                        if (colname.GetBoolean("bPrimaryKey")) {
                                            if (sbKeyValues.ToString() != string.Empty) {
                                                sbKeyValues.Append(" AND ");
                                            }

                                            sbKeyValues.Append(string.Format("{0}={1}", col, System.Convert.ToString(dr[col])));
                                        }
                                    }

                                    break;
                            }
                        } catch {

                        }
                    }
                    string[] aColumns = sRptColumn.Split('|');
                    foreach(string s in aColumns){
                        if (!string.IsNullOrEmpty(s) && row.ContainsKey(s)){

                            row.SetValue(s , "${{"+s+"}}");
                        }
                    }

                    rows.Add(row);

                    _row++;
                    _rows++;

                    _file_rows++;

                }

                dr.Close();


            } catch (Exception ex) {
                string cMessage = "\nMessage=[" + ex.Message + "]" + "\nSource=[" + ex.Source + "]\nStackTrace=[" + ex.StackTrace + "]\nTargetSite=[" + ex.TargetSite + "]";
                Console.WriteLine(cMessage);
                Console.WriteLine(sQuery);
                ZZLogger.Trace(ZFILE_NAME , ex);

                throw new Exception("GetDatabases error: " + ex.Message);
            } finally {
            }

            Console_Write(_TableName + " : " + _rows.ToString("###,###,###,##0") + " records exported", 0);

            return rows;
        }

        public void GenerateSqlInserts(DbContext _Trans, string fileName, string tableName,string sQuery)
        {
            List<JSONObject> aryColumns = DatabaseTableColumns(_Trans , tableName);
            StringBuilder sbColumns = new StringBuilder(string.Empty);

            if (aryColumns.Count == 0) {
                return;
            }

            foreach (JSONObject colname in aryColumns) {

                if (colname.GetBoolean("bPrimaryKey")) {

                    if (sbColumns.ToString() != string.Empty) {
                        sbColumns.Append(", ");
                    }

                    sbColumns.Append(MarksStart + colname.GetValue("sColumnName") + MarksEnd);
                }
            }

            string OrderBy = "";

            if (sbColumns.ToString() != string.Empty) {

                OrderBy = " ORDER BY " + sbColumns.ToString();
            }

            if (this.DeleteFile == "Y" && File.Exists(fileName)) {
                File.Delete(fileName);
            }

            if (File.Exists(tableName + ".def")) {

                using(StreamReader sr = new StreamReader(tableName + ".def", System.Text.Encoding.Default)) {
                    string _ss;

                    if (this.DeleteFile == "Y" && File.Exists(fileName)) {
                        File.Delete(fileName);
                    }

                    while ((_ss = sr.ReadLine()) != null) {
                        string _s = _ss;
                        _s = _s.Replace("{sUID}", this.sUID);
                        _s = _s.Replace("{TableName}", tableName);
                        GenerateSqlInserts(_Trans , fileName , _s , aryColumns);
                    }

                }

            } else {
                string sql=sQuery;
                if (sql==""){
                    sql="SELECT * FROM " + MarksStart + tableName + MarksEnd + OrderBy;
                }
                GenerateSqlInserts(_Trans , fileName , sql , aryColumns);
            }
        }

        public string GenerateJSONObject(DbContext _Trans, string fileName, string sQuery, List<JSONObject> aryColumns)
        {
            Console.WriteLine(sQuery);

            fileName = fileName.Replace("%AppPath%" , this.AppPath);

            string _fileName = fileName;

            if (fileName.IndexOf("#") >= 0) {
                if (_FileIndex == 0) {
                    fileName = fileName.Replace("#" , "");
                } else {
                    fileName = fileName.Replace("#" , _FileIndex.ToString("00"));
                }

                if (File.Exists(fileName)) {
                    FileInfo fi = new FileInfo(fileName);
                    long s1 = fi.Length;

                    if (s1 > 512 * 1024 * 1024) {
                        _FileIndex++;
                        fileName = _fileName.Replace("#", _FileIndex.ToString("00"));
                    }
                }
            }

            fileName = Path.GetFullPath(fileName);

            if (!System.IO.Directory.Exists(Path.GetDirectoryName(fileName))) {
                if (System.IO.Directory.Exists(Path.GetDirectoryName(Path.GetDirectoryName(fileName)))) {
                    System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }
            }

            sQuery = sQuery.Replace("%sUID%"   , this.sUID);

            JSONArray vRows = new JSONArray();

            try {

                IDataReader dr  = _Trans.DataReader(sQuery);

                while (dr.Read()) {

                    JSONObject row = new JSONObject();
                    foreach (JSONObject colname in aryColumns) {
                        // need to do a case to check the column-value types(quote strings(check for dups first), convert bools)
                        string sColumnName = colname.GetValue("sColumnName");

                        if (dr[sColumnName] == System.DBNull.Value) {
                            row.SetValue(sColumnName , "");
                        } else {
                            row.SetValue(sColumnName , dr[sColumnName]);
                        }
                    }

                    vRows.Add(row);
                }

                dr.Close();

                Utils.Util.WriteContents(fileName, vRows.ToString());

            } catch (Exception ex) {
                string cMessage = "\nMessage=[" + ex.Message + "]" + "\nSource=[" + ex.Source + "]\nStackTrace=[" + ex.StackTrace + "]\nTargetSite=[" + ex.TargetSite + "]";
                Console.WriteLine(cMessage);
                Console.WriteLine(sQuery);
                ZZLogger.Trace(ZFILE_NAME , ex);

                throw new Exception("GetDatabases error: " + ex.Message);
            } finally {
            }

            return vRows.ToString();
        }

        public string GenerateSqlInserts(DbContext _Trans, string fileName, string sQuery, List<JSONObject> aryColumns)
        {
            Console.WriteLine(sQuery);

            fileName = fileName.Replace("%AppPath%" , this.AppPath);

            string _fileName       = fileName;
            string _WithDeleteMode = SqlWithDelete;

            if (fileName.IndexOf("#") >= 0) {
                if (_FileIndex == 0) {
                    fileName = fileName.Replace("#" , "");
                } else {
                    fileName = fileName.Replace("#" , _FileIndex.ToString("00"));
                }

                if (File.Exists(fileName)) {
                    FileInfo fi = new FileInfo(fileName);
                    long s1 = fi.Length;

                    if (s1 > 512 * 1024 * 1024) {
                        _FileIndex++;
                        fileName = _fileName.Replace("#", _FileIndex.ToString("00"));
                    }
                }
            }

            fileName = Path.GetFullPath(fileName);

            if (!System.IO.Directory.Exists(Path.GetDirectoryName(fileName))) {
                if (System.IO.Directory.Exists(Path.GetDirectoryName(Path.GetDirectoryName(fileName)))) {
                    System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }
            }

            sQuery = sQuery.Replace("%sUID%"   , this.sUID);

            string sSqlInserts            = string.Empty;
            StringBuilder sbSqlStatements = new StringBuilder(string.Empty);
            StringBuilder sbColumns       = new StringBuilder(string.Empty);
            StringBuilder sbColumns2      = new StringBuilder(string.Empty);
            string _TableName             = "";

            foreach (JSONObject colname in aryColumns) {
                _TableName = colname.GetValue("sTableName");

                if (sbColumns.ToString() != string.Empty) {
                    sbColumns.Append(", ");
                }

                sbColumns.Append(TargetMarksStart + colname.GetValue("sColumnName") + TargetMarksEnd);
            }

            if (this.Target=="MySql"){

                _TableName = _TableName.ToLower();
            }

            sbColumns2 = sbColumns;

            int _row       = 0;
            int _rows      = 0;
            int _file_rows = 0;

            string _insertsql = string.Format("INSERT INTO " + TargetMarksStart + "{0}" + TargetMarksEnd + "({1}) ", _TableName, sbColumns.ToString());

            if ((_WithDeleteMode == "A" || _WithDeleteMode == "W") && this.Target=="MySql") {
                _insertsql = string.Format("INSERT IGNORE INTO " + TargetMarksStart + "{0}" + TargetMarksEnd + "({1}) ", _TableName, sbColumns.ToString());
            }

            try {

                IDataReader dr = _Trans.DataReader(sQuery);

                // loop thru each record of the datatable
                while (dr.Read()) {
                    // loop thru each column, and include the value if the column is in the array
                    StringBuilder sbValues    = new StringBuilder(string.Empty);
                    StringBuilder sbKeyValues = new StringBuilder(string.Empty);

                    foreach (JSONObject colname in aryColumns) {
                        if (sbValues.ToString() != string.Empty) {
                            sbValues.Append(", ");
                        }

                        // need to do a case to check the column-value types(quote strings(check for dups first), convert bools)
                        string sType = string.Empty;
                        string col   = colname.GetValue("sColumnName");
                        sType        = colname.GetValue("sDataType");

                        try {

                            //Console.WriteLine(col+"="+sType.Trim().ToLower());
                            switch (sType.Trim().ToLower()) {
                                case "bit":

                                    if (dr[col] == System.DBNull.Value) {
                                        sbValues.Append("0");
                                    } else {
                                        sbValues.Append((System.Convert.ToBoolean(dr[col]) == true ? "1" : "0"));
                                    }

                                    if (colname.GetBoolean("bPrimaryKey")) {
                                        if (sbKeyValues.ToString() != string.Empty) {
                                            sbKeyValues.Append(" AND ");
                                        }

                                        sbKeyValues.Append((System.Convert.ToBoolean(dr[col]) == true ? col + "<>0" : col + "=0"));
                                    }

                                    break;

                                case "nvarchar":
                                case "varchar":
                                case "ntext":
                                case "text":
                                case "longtext":
                                case "mediumtext":
                                case "nchar":
                                case "char":
                                    sbValues.Append(_Unicode+string.Format("'{0}'", QuoteString(dr[col])));

                                    if (colname.GetBoolean("bPrimaryKey")) {
                                        if (sbKeyValues.ToString() != string.Empty) {
                                            sbKeyValues.Append(" AND ");
                                        }

                                        sbKeyValues.Append(string.Format("{0}="+_Unicode+"'{1}'", col, QuoteString(dr[col])));
                                    }

                                    break;

                                case "smalldatetime":
                                case "datetime":
                                case "date":
                                    string sDateTime = QuoteString(dr[col]);

                                    if (IsDateTime(sDateTime) == true) {
                                        sDateTime = System.DateTime.Parse(sDateTime).ToString("yyyy-MM-dd HH:mm:ss");
                                        sbValues.Append(string.Format("'{0}'", sDateTime));
                                    } else {
                                        sbValues.Append("NULL");
                                    }


                                    if (colname.GetBoolean("bPrimaryKey")) {
                                        if (sbKeyValues.ToString() != string.Empty) {
                                            sbKeyValues.Append(" AND ");
                                        }

                                        sbKeyValues.Append(string.Format("{0}='{1}'", col, sDateTime));
                                    }

                                    break;

                                case "system.byte[]":
                                    sbValues.Append(string.Format("'{0}'", System.Convert.ToBase64String((byte[]) dr[col])));
                                    break;

                                case "varbinary":


                                    byte[] _bytes   = (byte[])dr[col];
                                    StringBuilder s = new StringBuilder(string.Empty);

                                    for (int i = 0; i < _bytes.Length; i++) {
                                        s.Append(_bytes[i].ToString("X2"));
                                    }

                                    sbValues.Append("0x" + s);

                                    break;

                                case "decimal":
                                case "int":
                                case "float":
                                case "tinyint":
                                case "smallint":
                                case "bigint":
                                    if (dr[col] == System.DBNull.Value) {
                                        sbValues.Append("0");
                                    } else {
                                        sbValues.Append(System.Convert.ToDouble(dr[col]).ToString("0.########"));

                                        if (colname.GetBoolean("bPrimaryKey")) {
                                            if (sbKeyValues.ToString() != string.Empty) {
                                                sbKeyValues.Append(" AND ");
                                            }

                                            sbKeyValues.Append(string.Format("{0}={1}", col, System.Convert.ToString(dr[col])));
                                        }
                                    }

                                    break;

                                default:
                                    //Console.WriteLine(sType.Trim().ToLower());

                                    if (dr[col] == System.DBNull.Value) {
                                        sbValues.Append("NULL");
                                    } else {
                                        sbValues.Append(System.Convert.ToString(dr[col]));

                                        if (colname.GetBoolean("bPrimaryKey")) {
                                            if (sbKeyValues.ToString() != string.Empty) {
                                                sbKeyValues.Append(" AND ");
                                            }

                                            sbKeyValues.Append(string.Format("{0}={1}", col, System.Convert.ToString(dr[col])));
                                        }
                                    }

                                    break;
                            }
                        } catch {
                            sbValues.Append(string.Format("'{0}'", QuoteString(dr[col])));
                        }
                    }

                    if (_WithDeleteMode == "Y" && sbKeyValues.ToString() == "") {
                        _WithDeleteMode = "";
                    }

                    if (_WithDeleteMode == "Y" && sbKeyValues.ToString() != "") {
                        string sDelete = string.Format("DELETE FROM {0} WHERE {1}" , TargetMarksStart + _TableName + TargetMarksEnd , sbKeyValues.ToString());
                        sbSqlStatements.Append(sDelete);
                        WriteSqlClose(ref sbSqlStatements);
                    } else if (_WithDeleteMode == "A" && sbKeyValues.ToString() != "") {
                        if (this.Target=="MySql") {
                        }else{
                            string sDelete = string.Format("IF NOT EXISTS (SELECT * FROM {0} WHERE {1})\n" , TargetMarksStart + _TableName + TargetMarksEnd , sbKeyValues.ToString());
                            sbSqlStatements.Append(sDelete);
                        }
                    } else if (_WithDeleteMode == "T") {
                        _WithDeleteMode = "";
                        string sDelete = string.Format("TRUNCATE TABLE {0}" , TargetMarksStart + _TableName + TargetMarksEnd);
                        sbSqlStatements.Append(sDelete);
                        WriteSqlClose(ref sbSqlStatements);
                    }

                    sbSqlStatements.Append(_insertsql);
                    string snewsql = string.Format(" VALUES({0})", sbValues.ToString());
                    sbSqlStatements.Append(snewsql);

                    WriteSqlClose(ref sbSqlStatements);

                    _row++;
                    _rows++;

                    _file_rows++;

                    if (_row >= 1000) {

                        Console_Write(_TableName + " : " + _rows.ToString("###,###,###,##0") + " records exported", 0);
                        Utils.Util.WriteContents(fileName, sbSqlStatements.ToString());
                        sbSqlStatements = new StringBuilder(string.Empty);
                        _row = 0;

                    }
                }

                dr.Close();

                Utils.Util.WriteContents(fileName, sbSqlStatements.ToString());

                sSqlInserts = sbSqlStatements.ToString();

            } catch (Exception ex) {
                string cMessage = "\nMessage=[" + ex.Message + "]" + "\nSource=[" + ex.Source + "]\nStackTrace=[" + ex.StackTrace + "]\nTargetSite=[" + ex.TargetSite + "]";
                Console.WriteLine(cMessage);
                Console.WriteLine(sQuery);
                ZZLogger.Trace(ZFILE_NAME , ex);

                throw new Exception("GetDatabases error: " + ex.Message);
            } finally {
            }

            Console_Write(_TableName + " : " + _rows.ToString("###,###,###,##0") + " records exported", 0);

            return sSqlInserts;
        }

        private string QuoteString(string str)
        {
            if (str == null) {
                return "";
            }

            return str.Replace("'", "''");
        }

        private string QuoteString(object ostr)
        {
            if (ostr == null) {
                return "";
            }

            return ostr.ToString().Replace("'", "''");
        }

        private bool IsDateTime(string sDateTime)
        {
            bool bIsDateTime = false;

            try {
                System.DateTime.Parse(sDateTime);
                bIsDateTime = true;
            } catch {
                bIsDateTime = false;
            }

            return bIsDateTime;
        }

        public void WriteSql(string fileName , string sQuery)
        {

            Utils.Util.WriteContents(fileName , sQuery);
            StringBuilder _SQLString = new StringBuilder(string.Empty);
            WriteSqlClose(ref _SQLString);
            Utils.Util.WriteContents(fileName , _SQLString.ToString());

        }

        internal StringBuilder WriteSqlClose(ref StringBuilder s)
        {
            s.AppendLine("");
            s.AppendLine();
            if (this.Target=="MySql"){
                s.AppendLine(";");
            }else{
                s.AppendLine("GO");
            }
            s.AppendLine();
            s.AppendLine();
            return s;
        }

        public string ColumnDefaultStatement(List<JSONObject> nameValues)
        {

            if (this.Target=="MySql"){
                return "";
            }
            StringBuilder _SQLString = new StringBuilder(string.Empty);

            foreach (JSONObject colname in nameValues) {

                bool ColumnNullable  = colname.GetBoolean("bColumnNullable");
                int ColumnLength     = colname.GetInteger("nColumnLength");
                int NumericPrecision = colname.GetInteger("nNumericPrecision");
                int ColumnScale      = colname.GetInteger("nColumnScale");
                string ColumnType    = colname.GetValue("sDataType");
                string _Default      = colname.GetValue("sDefault");
                string columnName    = colname.GetValue("sColumnName");
                string tableName     = colname.GetValue("sTableName");

                if (this.Target=="MySql"){

                    tableName = tableName.ToLower();
                }

                _Default = _Default.Replace("(", "");
                _Default = _Default.Replace(")", "");

                if (this.Target=="MySql"){
                }else{
                    if (_Default != "") {
                        _Default = "(" + _Default + ")";
                    }
                }

                if (ColumnType == "nvarchar" ) {
                    if (_Default == "") {
                        _SQLString.AppendLine("IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = '" + "DF_" + tableName + "_" + columnName  + "' AND type='d')");
                        _SQLString.Append("ALTER TABLE " + TargetMarksStart + tableName + TargetMarksEnd + " ADD ");
                        _SQLString.Append("CONSTRAINT " + TargetMarksStart + "DF_" + tableName + "_" + columnName + TargetMarksEnd + " DEFAULT '' FOR ") ;
                        _SQLString.Append(TargetMarksStart + columnName + TargetMarksEnd);

                        WriteSqlClose(ref _SQLString);

                    }
                } else if (ColumnType == "decimal") {
                    if (_Default == "") {
                        _SQLString.AppendLine("IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = '" + "DF_" + tableName + "_" + columnName  + "' AND type='d')");

                        _SQLString.Append("ALTER TABLE " + TargetMarksStart + tableName + TargetMarksEnd + " ADD ");
                        _SQLString.Append("CONSTRAINT " + TargetMarksStart + "DF_" + tableName + "_" + columnName + TargetMarksEnd + " DEFAULT 0 FOR ") ;
                        _SQLString.Append(TargetMarksStart + columnName + TargetMarksEnd);

                        WriteSqlClose(ref _SQLString);
                    }
                } else if (ColumnType == "bit") {

                }
            }

            return _SQLString.ToString();

        }

        public string CreateTableStatementMySql(string tableName, List<JSONObject> nameValues)
        {

            StringBuilder _SQLString    = new StringBuilder(string.Empty);
            StringBuilder AltertColumns = new StringBuilder(string.Empty);
            StringBuilder sbColumns     = new StringBuilder(string.Empty);
            StringBuilder PKColumns     = new StringBuilder(string.Empty);

            JSONObject _AlterColumns = new JSONObject();

            JSONObject _JSONObject = AppConfigs.LoadSetting("MsSqlToMySqlDataType.json");

            string sPrev = "";
            foreach (JSONObject colname in nameValues) {
                if (sbColumns.ToString() != string.Empty) {
                    sbColumns.Append(",");
                    sbColumns.AppendLine("");
                }

                tableName = colname.GetValue("sTableName");
                tableName = tableName.ToLower();

                string sColumnName   = colname.GetValue("sColumnName");
                string ColumnType    = colname.GetValue("sDataType");
                long ColumnLength    = colname.GetLong("nColumnLength");
                int ColumnScale      = colname.GetInteger("nColumnScale");
                string _Default      = colname.GetValue("sDefault");
                bool ColumnNullable  = colname.GetBoolean("bColumnNullable");
                int NumericPrecision = colname.GetInteger("nNumericPrecision");
                string _ColumnLength = "";
                if (ColumnLength > 10000) {
                    ColumnType = "ntext";
                }
                if (ColumnLength > 0 && NumericPrecision==0) {

                    NumericPrecision = (int)ColumnLength;
                }
                if (ColumnLength > 10000) {
                    ColumnType = "LONGTEXT";
                }
                if (ColumnType == "varbinary" || ColumnType == "nchar" || ColumnType == "char" || ColumnType == "nvarchar" || ColumnType == "varchar") {
                    if (NumericPrecision == -1) {
                        ColumnType = "longtext";
                    }
                }

                if (_JSONObject.ContainsKey(ColumnType)){
                    ColumnType = _JSONObject.GetValue(ColumnType);
                }

                if (colname.GetBoolean("bPrimaryKey")) {
                    if (PKColumns.ToString() != string.Empty) {
                        PKColumns.Append(",");
                    }

                    PKColumns.Append(TargetMarksStart + sColumnName + TargetMarksEnd);
                }

                if (ColumnType == "varbinary" || ColumnType == "nchar" || ColumnType == "char" || ColumnType == "nvarchar" || ColumnType == "varchar") {
                    if (ColumnLength == -1) {
                        _ColumnLength = "(MAX)";
                    } else {
                        _ColumnLength = "(" + ColumnLength + ")";
                    }
                } else if (ColumnType == "numeric" || ColumnType == "decimal") {
                    _ColumnLength = "(" + NumericPrecision + "," + ColumnScale + ")";
                } else if (ColumnType == "float") {
                    if (ColumnScale == 0) {
                        _ColumnLength = "(" + NumericPrecision + ")";
                    } else {
                        _ColumnLength = "(" + NumericPrecision + "," + ColumnScale + ")";
                    }
                }

                if (_Default != "") {

                    _Default = " DEFAULT " + _Default + "";
                    _Default = _Default.Replace("(","");
                    _Default = _Default.Replace(")","");
                }

                StringBuilder s = new StringBuilder(string.Empty);

                s.Append(" " + TargetMarksStart + sColumnName + TargetMarksEnd + " " + ColumnType);
                s.Append(_ColumnLength);

                if (ColumnType=="longtext") {
                    _Default="";
                }
                if (!ColumnNullable) {
                    s.Append(" NOT NULL ");
                }

                s.Append(_Default);

                StringBuilder _alter = new StringBuilder();

                if (colname.GetBoolean("bPrimaryKey")) {
                    _alter.Append("UPDATE " + TargetMarksStart + tableName + TargetMarksEnd + " set " + TargetMarksStart + sColumnName + TargetMarksEnd + "='' WHERE " + TargetMarksStart + sColumnName + TargetMarksEnd + " IS NULL");

                    WriteSqlClose(ref _alter);
                }

                _alter.AppendLine("DELIMITER^");
                _alter.AppendLine("DROP PROCEDURE IF EXISTS schema_change^");
                _alter.AppendLine("CREATE PROCEDURE schema_change()");
                _alter.AppendLine("BEGIN");
                _alter.AppendLine("IF NOT EXISTS (SELECT * FROM information_schema.columns WHERE table_schema = database() AND table_name = '" + tableName + "' AND column_name = '" + sColumnName + "') THEN");
                if (sPrev==""){
                    _alter.AppendLine("  ALTER TABLE " + TargetMarksStart + tableName + TargetMarksEnd + " ADD COLUMN "+s+" FIRST;");
                }else{
                    _alter.AppendLine("  ALTER TABLE " + TargetMarksStart + tableName + TargetMarksEnd + " ADD COLUMN "+s+" AFTER `"+sPrev+"`;");
                }
                if (_AppendOnly=="N"){
                    _alter.AppendLine("ELSE");
                    if (sPrev==""){
                        _alter.AppendLine("  ALTER TABLE " +TargetMarksStart + tableName + TargetMarksEnd + " MODIFY COLUMN " + s+" FIRST;");
                    }else{
                        _alter.AppendLine("  ALTER TABLE " +TargetMarksStart + tableName + TargetMarksEnd + " MODIFY COLUMN " + s+" AFTER `"+sPrev+"`;");
                    }
                }
                _alter.AppendLine("END IF;");
                _alter.AppendLine("END^");
                _alter.AppendLine("DELIMITER;");
                _alter.AppendLine("CALL schema_change()");

                WriteSqlClose(ref _alter);

                _AlterColumns.SetValue(tableName+"_"+sColumnName , _alter.ToString());

                AltertColumns.AppendLine(_alter.ToString());

                sPrev = sColumnName;
                Console.Write(".");
                sbColumns.Append(s.ToString());
            }


            if (!_WriteFile.ContainsKey(tableName.ToLower())) {
                _WriteFile[tableName.ToLower()]=true;

                _AlterColumns.SetValue("sTableName" , tableName.ToLower());
                AlterTable.Add(_AlterColumns);
            }

            _SQLString.AppendLine("-- Generated by Generic Tools");
            _SQLString.AppendLine("CREATE TABLE IF NOT EXISTS " + TargetMarksStart + tableName + TargetMarksEnd + "(");
            _SQLString.Append(sbColumns.ToString());

            if (PKColumns.ToString() != "") {
                _SQLString.AppendLine(",");
                _SQLString.AppendLine(" PRIMARY KEY (" + PKColumns.ToString() + "),");
                _SQLString.AppendLine(" KEY "+TargetMarksStart+"IX_" + tableName +TargetMarksEnd+ " (" + PKColumns.ToString() + ")");
            }

            _SQLString.Append(")");

            WriteSqlClose(ref _SQLString);

            _SQLString.AppendLine(AltertColumns.ToString());

            return _SQLString.ToString();
        }

        public string CreateTableStatementMsSql(string tableName, List<JSONObject> nameValues)
        {

            StringBuilder _SQLString    = new StringBuilder(string.Empty);
            StringBuilder AltertColumns = new StringBuilder(string.Empty);
            StringBuilder sbColumns     = new StringBuilder(string.Empty);
            StringBuilder PKColumns     = new StringBuilder(string.Empty);

            JSONObject _AlterColumns = new JSONObject();

            foreach (JSONObject colname in nameValues) {
                if (sbColumns.ToString() != string.Empty) {
                    sbColumns.Append(",");
                    sbColumns.AppendLine("");
                }

                tableName = colname.GetValue("sTableName");

                string sColumnName   = colname.GetValue("sColumnName");
                string ColumnType    = colname.GetValue("sDataType");
                long ColumnLength    = colname.GetLong("nColumnLength");
                int ColumnScale      = colname.GetInteger("nColumnScale");
                string _Default      = colname.GetValue("sDefault");
                bool ColumnNullable  = colname.GetBoolean("bColumnNullable");
                int NumericPrecision = colname.GetInteger("nNumericPrecision");
                string _ColumnLength = "";
                if (ColumnLength > 10000) {
                    ColumnType = "ntext";
                }

                if (colname.GetBoolean("bPrimaryKey")) {
                    if (PKColumns.ToString() != string.Empty) {
                        PKColumns.Append(",");
                    }

                    PKColumns.Append(TargetMarksStart + sColumnName + TargetMarksEnd);
                }

                if (ColumnType == "varbinary" || ColumnType == "nchar" || ColumnType == "char" || ColumnType == "nvarchar" || ColumnType == "varchar") {
                    if (ColumnLength == -1) {
                        _ColumnLength = "(MAX)";
                    } else {
                        _ColumnLength = "(" + ColumnLength + ")";
                    }
                } else if (ColumnType == "numeric" || ColumnType == "decimal") {
                    _ColumnLength = "(" + NumericPrecision + "," + ColumnScale + ")";
                } else if (ColumnType == "float") {
                    if (ColumnScale == 0) {
                        _ColumnLength = "(" + NumericPrecision + ")";
                    } else {
                        _ColumnLength = "(" + NumericPrecision + "," + ColumnScale + ")";
                    }
                }
                if (ColumnNullable && ColumnType == "datetime") {

                }

                if (_Default != "") {
                    _Default = " DEFAULT " + _Default + "";
                }

                StringBuilder s = new StringBuilder(string.Empty);

                s.Append(" " + TargetMarksStart + sColumnName + TargetMarksEnd + " " + ColumnType);
                s.Append(_ColumnLength);

                if (ColumnNullable) {
                    s.Append(" NULL ");
                } else {
                    s.Append(" NOT NULL ");
                }

                s.Append(_Default);

                StringBuilder _alter = new StringBuilder();

                _alter.AppendLine("IF NOT EXISTS (SELECT * FROM [dbo].[SysColumns] WHERE [name] = "+_Unicode+"'" + sColumnName + "' and [id] = object_id("+_Unicode+"'" + tableName + "'))");
                _alter.Append("ALTER TABLE " + TargetMarksStart + tableName + TargetMarksEnd + " ADD ");
                _alter.Append(s.ToString());

                WriteSqlClose(ref _alter);

                _AlterColumns.SetValue(tableName+"_"+sColumnName , _alter.ToString());

                if (colname.GetBoolean("bPrimaryKey")) {
                    _alter.Append("UPDATE " + TargetMarksStart + tableName + TargetMarksEnd + " set " + TargetMarksStart + sColumnName + TargetMarksEnd + "='' WHERE " + TargetMarksStart + sColumnName + TargetMarksEnd + " IS NULL");

                    WriteSqlClose(ref _alter);
                }

                AltertColumns.AppendLine(_alter.ToString());

                Console.Write(".");
                sbColumns.Append(s.ToString());
            }

            if (!_WriteFile.ContainsKey(tableName.ToLower())) {
                _WriteFile[tableName.ToLower()]=true;

                _AlterColumns.SetValue("sTableName" , tableName.ToLower());
                AlterTable.Add(_AlterColumns);
            }

            _SQLString.AppendLine("-- Generated by Generic Tools");

            _SQLString.Append("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE");
            WriteSqlClose(ref _SQLString);
            _SQLString.Append("BEGIN TRANSACTION");
            WriteSqlClose(ref _SQLString);
            _SQLString.Append("PRINT N'Altering Table " + tableName + "'");
            WriteSqlClose(ref _SQLString);

            _SQLString.AppendLine("IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = '" + tableName + "')");
            _SQLString.AppendLine("CREATE TABLE " + TargetMarksStart + tableName + TargetMarksEnd + "(");
            _SQLString.Append(sbColumns.ToString());

            if (PKColumns.ToString() != "") {
                _SQLString.AppendLine(",");
                _SQLString.AppendLine(" CONSTRAINT PK_" + tableName + " PRIMARY KEY (" + PKColumns.ToString() + ")");
            }

            _SQLString.Append(")");

            WriteSqlClose(ref _SQLString);

            if (PKColumns.ToString() != string.Empty) {
                _SQLString.AppendLine("IF NOT EXISTS (SELECT name FROM sysindexes WHERE name = '" + "IX_" + tableName + "')");
                _SQLString.Append("CREATE INDEX " + TargetMarksStart + "IX_" + tableName + TargetMarksEnd + " ON " + TargetMarksStart + tableName + TargetMarksEnd + " (" + PKColumns.ToString() + ")");

                WriteSqlClose(ref _SQLString);
            }

            _SQLString.AppendLine(AltertColumns.ToString());
            _SQLString.Append("IF (@@ERROR <> 0) AND (@@TRANCOUNT > 0) ROLLBACK TRANSACTION");

            WriteSqlClose(ref _SQLString);
            _SQLString.AppendLine("IF @@TRANCOUNT > 0");
            _SQLString.Append("      COMMIT TRANSACTION");

            WriteSqlClose(ref _SQLString);
            _SQLString.Append("PRINT '" + tableName + " Script deployment completed'");

            WriteSqlClose(ref _SQLString);

            return _SQLString.ToString();
        }
    }
}
