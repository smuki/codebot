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

    public class AutoCoder {
        const string ZFILE_NAME = "AutoCoder";

        private string _fileName = "";

        private ShellRunner _ShellRunner   = new ShellRunner();
        private List<string> _FAILURE             = new List<string>();
        private List<FileNameValue> _Hashs        = new List<FileNameValue>();

        public List<CommandEntity> sCommandEntity = new List<CommandEntity>();

        public  StringBuilder Message  = new StringBuilder();
        public  AppConfigs   AppConfigs;

        public DirectoryInfo BaseDirectory;

        public  string  Mode         = "";
        public  string  UID_TP_CODE  = "N";
        public  string  DebugMode    = "N";
        private TableUtil _TableUtil = new TableUtil();

        private  List<string> _L_UID_CODE = new List<string>();

        public string FileName { get { return _fileName; } set { _fileName = value; }  }

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

        public void Process(string sUID)
        {
            Message = new StringBuilder();
            if (string.IsNullOrEmpty(sUID))
            {
                sUID = "";
            }

            this.WriteLine("");
            this.Write(sUID);

            _L_UID_CODE = new List<string>();

            DirectoryInfo dir = new DirectoryInfo(AppConfigs.AddonLocation);

            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo i in fileinfo)
            {
                if (i is DirectoryInfo)//判断是否文件夹
                {
                }
                else
                {
                    string fileName = Path.GetFileNameWithoutExtension(i.FullName);

                    if (fileName.Contains(sUID))
                    {
                        this.WriteLine("");
                        this.Write(fileName);

                        _L_UID_CODE.Add(fileName);

                        this.GeneratorActivity(fileName);
                    }
                }
            }

            foreach (string _UID_CODE in _L_UID_CODE) {
                Prettify(_UID_CODE);
                Build(_UID_CODE);
            }

            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode    = this.DebugMode;
            _AutoTemplate.AppConfigs   = AppConfigs;
            _AutoTemplate.Initialize();
            _AutoTemplate.SetValue("ProjectName"   , AppConfigs.GetValue("ProjectName"));
            _AutoTemplate.SetValue("AppPath"       , AppConfigs.GetValue("AppPath"));
            _AutoTemplate.SetValue("ProjectPath"   , AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("DevelopPath"   , AppConfigs.DevelopPath);
            _AutoTemplate.SetValue("CommandEntity" , sCommandEntity);

            _AutoTemplate.Template   = "Build_Result.shtml";
            _AutoTemplate.OutputFile = Util.Separator(AppConfigs.GetValue("AppPath") + "\\temp\\Build_Result.html");
            _AutoTemplate.Process();
            _AutoTemplate.Close();
            if (_FAILURE.Count>0)
            {
                this.WriteLine("\n*** _FAILURE List *** ");
                foreach (var c in _FAILURE)
                {
                    this.WriteLine(c);
                }
            }
        }
        private void Prettify(string sUID)
        {
            CommandEntity sCommand = new CommandEntity();

            JSONObject Prettify = AppConfigs.JSONObject("Prettify");
            if (!string.IsNullOrEmpty(Prettify.GetValue("sCommand")))
            {
                string Argument = Utils.Util.ReplaceWith(Prettify.GetValue("Argument") , "{sUID}" , sUID);

                sCommand.sDirectory = Util.Separator(AppConfigs.ProjectPath + @"\src\"+ sUID);
                sCommand.sCommand   = Prettify.GetValue("sCommand");
                sCommand.sArguments = Util.Separator(Argument);

                sCommand = _ShellRunner.Execute(sCommand);

                WriteLine(sCommand.Message);
            }
        }

        private string Build(string sUID)
        {
            CommandEntity sCommand = new CommandEntity();

            string sArguments = AppConfigs.GetValue("Arguments");

            sArguments = Utils.Util.ReplaceWith(sArguments , "{sUID}" , sUID);

            sCommand.sDirectory = Util.Separator(AppConfigs.ProjectPath + @"\src\"+ sUID);
            sCommand.sCommand   = AppConfigs.GetValue("Compiler");
            sCommand.sArguments = sArguments;

            sCommand = _ShellRunner.Execute(sCommand);
            sCommandEntity.Add(sCommand);

            WriteLine(sCommand.Message);

            if (sCommand.SUCCESS)
            {
                if (Directory.Exists(sCommand.sDirectory))
                {
                    Console.WriteLine("Search In "+sCommand.sDirectory+@"\bin");
                    string fileNameDll = SearchFile(sCommand.sDirectory +@"\bin" , sUID+".dll");
                    if (string.IsNullOrEmpty(fileNameDll))
                    {
                        Console.WriteLine("Search In "+sCommand.sDirectory+@"\obj");
                        fileNameDll = SearchFile(sCommand.sDirectory +@"\obj" , sUID+".dll");
                    }
                    if (string.IsNullOrEmpty(fileNameDll))
                    {
                        Console.WriteLine("fileName "+sUID+".dll Not Found!! ");
                    }else{
                        Console.WriteLine("fileName "+fileNameDll);
                        Console.WriteLine("Copy file to");
                        string sPath = AppConfigs.ProjectPath+@"\apps\addons\";

                        Console.WriteLine("   "+fileNameDll+" ==> "+sPath+sUID+".dll");
                        File.Copy(fileNameDll ,sPath +sUID+".dll",true);

                        string fileName = fileNameDll.Replace(".dll" , ".pdb");
                        if (File.Exists(fileName)) {
                            Console.WriteLine("   "+fileName+" ==> "+sPath+sUID+".pdb");
                            File.Copy(fileName , sPath+sUID+".pdb" , true);
                        }else{
                            Console.WriteLine("   "+fileName+" Not Found!!");
                        }
                        fileName = fileNameDll.Replace(".dll" , ".deps.json");
                        if (File.Exists(fileName)) {
                            Console.WriteLine("   "+fileName+" ==> "+sPath+sUID+".dept.json");
                            File.Copy(fileName , sPath+sUID+".dept.json" , true);
                        }else{
                            Console.WriteLine("   "+fileName+" Not Found!!");
                        }
                    }
                }
            } else {
                _FAILURE.Add(sUID);
            }

            return "";
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
                Console.Write("\n" + sTableName);

                List<JSONObject> _NameValues = _TableUtil.DatabaseTableColumns(_Trans, sTableName);
                foreach (JSONObject colname in _NameValues)
                {
                    string sColumnName = colname.GetValue("sColumnName");
                    string sDataType = colname.GetValue("sDataType");
                    int nColumnLength = colname.GetInteger("nColumnLength");
                    bool bIsPKColumn = colname.GetBoolean("bPrimaryKey");
                    int IsPKColumn = 0;
                    if (bIsPKColumn)
                    {
                        IsPKColumn = 1;
                    }
                    sSQLString = "delete from SysFields where sTableName='" + sTableName + "' AND sColumnName='"+ sColumnName + "'";
                    _Trans.Execute(sSQLString);

                    sSQLString = "INSERT INTO SysFields (sTableName, sColumnName, sDataType, nColumnLength, bPrimaryKey) ";
                    sSQLString = sSQLString + " VALUES('" + sTableName + "', '" + sColumnName + "', '" + sDataType + "', " + nColumnLength + "," + IsPKColumn + ")";
                    _Trans.Execute(sSQLString);

                    Console.Write(".");
                }
            }
        }

        public string SearchFile(string sPath,string fileName)
        {
            try
            {
                if (!Directory.Exists(sPath)){
                    return "";
                }

                DirectoryInfo dir = new DirectoryInfo(sPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //获取目录下（不包含子目录）的文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)     //判断是否文件夹
                    {
                        string t=SearchFile(i.FullName,fileName);    //递归调用复制子文件夹
                        if (t!=""){
                            return t;
                        }
                    }
                    else
                    {
                        //Console.WriteLine(Path.GetFileName(i.FullName)+" == "+fileName);
                        if (Path.GetFileName(i.FullName).ToLower() == fileName.ToLower())
                        {
                            return i.FullName;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
            return "";
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
            RsSysFunction.CommandText="SELECT sUID FROM sysfunction WHERE sUID LIKE '%" + sUID + "%' ORDER BY sUID DESC";
            RsSysFunction.Open();
            _L_UID_CODE = new List<string>();
            if (sUID.ToLower() == "en")
            {
                GeneratorCaptionDefine(_DbContext, sUID.ToLower());
            }
            WriteLine("EOF-->" + RsSysFunction.EOF);
            WriteLine("CommandText-->" + RsSysFunction.CommandText);
            while (!RsSysFunction.EOF)
            {
                string _UID_CODE = RsSysFunction.GetValue("sUID");
                WriteLine(_UID_CODE);

                WriteLine("");
                Write(_UID_CODE);
                _L_UID_CODE.Add(_UID_CODE);
                GeneratorActivityDefinition(_DbContext, _UID_CODE);
                GeneratorActivity(_UID_CODE);
                RsSysFunction.MoveNext();
            }
            RsSysFunction.Close();
            foreach (string _UID_CODE in _L_UID_CODE)
            {
                Prettify(_UID_CODE);
                Build(_UID_CODE);
            }

            if (_FAILURE.Count <= 0)
            {
                return;
            }
            WriteLine("\n*** _FAILURE List *** ");
            foreach (string c in _FAILURE)
            {
                WriteLine(c);
            }
        }

        private void GeneratorActivity(string sUID)
        {

            CoreUtil.CreateDir(Util.Separator(AppConfigs.ProjectPath + @"\apps\addons"));
            CoreUtil.CreateDir(Util.Separator(AppConfigs.ProjectPath + @"\src"));

            string _TableName    = "";
            string _ColumnName   = "";
            string _DataTypeCode = "";

            JSONObject _JSONFunction = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation+sUID+".json");

            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode    = this.DebugMode;
            _AutoTemplate.AppConfigs   = AppConfigs;
            _AutoTemplate.sUID         = sUID;
            _AutoTemplate.Initialize();
            _AutoTemplate.SetValue("sUID"            , sUID);
            _AutoTemplate.SetValue("DistPath"        , AppConfigs.GetValue("DistPath"));
            _AutoTemplate.SetValue("AppPath"         , AppConfigs.GetValue("AppPath"));
            _AutoTemplate.SetValue("ProjectName"     , AppConfigs.GetValue("ProjectName"));
            _AutoTemplate.SetValue("ProjectPath"     , AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("DevelopPath"     , AppConfigs.DevelopPath);
            _AutoTemplate.SetValue("RefCode"         , "QD" + sUID.ToUpper());
            _AutoTemplate.SetValue("sSqlCode"        , "Q" + sUID.ToUpper());
            _AutoTemplate.SetValue("sTableName"      , _JSONFunction.GetValue("sTableName"));
            _AutoTemplate.SetValue("PK_ColumnName"   , _JSONFunction.GetValue("PK_ColumnName"));
            _AutoTemplate.SetValue("LNK_TableName"   , _JSONFunction.GetValue("LNK_TableName"));
            _AutoTemplate.SetValue("sLNKUID"         , _JSONFunction.GetValue("sLNKUID"));
            _AutoTemplate.SetValue("sType"           , _JSONFunction.GetValue("sType"));
            _AutoTemplate.SetValue("ROOT_LNKUID"     , _JSONFunction.GetValue("ROOT_LNKUID"));
            _AutoTemplate.SetValue("ROOT_TableName"  , _JSONFunction.GetValue("ROOT_TableName"));
            _AutoTemplate.SetValue("ROOT_ColumnName" , _JSONFunction.GetValue("ROOT_ColumnName"));

            List<COLUMNEntity> Entitys = new List<COLUMNEntity>();

            string _COLUMNS_NAME         = "";
            string _COLUMNS_NAMEDateTime = "";

            foreach (JSONObject _NameValue in _JSONFunction.GetJSONArray("entitys").JSONObjects)
            {

                _TableName    = _NameValue.GetValue("sTableName");
                _ColumnName   = _NameValue.GetValue("sColumnName");
                _DataTypeCode = _NameValue.GetValue("DataType");

                COLUMNEntity _COLUMNEntity   = new COLUMNEntity();
                _COLUMNEntity.TableName      = _TableName;
                _COLUMNEntity.ColumnName     = _ColumnName;
                _COLUMNEntity.CaptionCode    = _NameValue.GetValue("CaptionCode");
                _COLUMNEntity.ColumnNullable = _NameValue.GetBoolean("ColumnNullable");
                _COLUMNEntity.EnableMode     = _NameValue.GetValue("EnableMode");
                _COLUMNEntity.bHasCaption    = _NameValue.GetBoolean("bHasCaption");
                _COLUMNEntity.Index          = _NameValue.GetInteger("Index");
                _COLUMNEntity.Length         = _NameValue.GetInteger("Length");
                _COLUMNEntity.MaxLength      = _NameValue.GetInteger("MaxLength");
                _COLUMNEntity.Options        = _NameValue.GetValue("Options");
                _COLUMNEntity.Sequency       = _NameValue.GetInteger("Sequency");
                _COLUMNEntity.bWriteable     = _NameValue.GetBoolean("Writeable");
                _COLUMNEntity.sRefBrowse     = _NameValue.GetValue("sRefBrowse");
                _COLUMNEntity.sRefCheck      = _NameValue.GetValue("sRefCheck");
                _COLUMNEntity.sRefViewer     = _NameValue.GetValue("sRefViewer");
                _COLUMNEntity.DataTypeCode   = _NameValue.GetValue("DataType");
                _COLUMNEntity.DataTypeChar   = _NameValue.GetValue("DataTypeChar");
                _COLUMNEntity.ColumnScale    = _NameValue.GetInteger("ColumnScale");
                _COLUMNEntity.EnableMode     = _NameValue.GetValue("EnableMode");
                _COLUMNEntity.Release        = _NameValue.GetBoolean("Release");
                _COLUMNEntity.sRefCheck      = _NameValue.GetValue("sCheckId");
                _COLUMNEntity.sRefBrowse     = _NameValue.GetValue("sRefBrowse");
                _COLUMNEntity.sRefBrowseType = _NameValue.GetValue("sType");
                _COLUMNEntity.sRefViewer     = _NameValue.GetValue("sRefViewer");
                _COLUMNEntity.bPrimaryKey    = _NameValue.GetBoolean("bPrimaryKey");
                _COLUMNEntity.bAutoIdentity  = _NameValue.GetBoolean("bAutoIdentity");

                if (_TableName.ToLower() != "variable" && (_DataTypeCode == "nvarchar" || _DataTypeCode == "ntext")) {
                    if (_ColumnName != "sOriginal") {
                        _COLUMNS_NAME = _COLUMNS_NAME + ";" + _TableName.ToLower() + "." + _ColumnName;
                    }
                }

                if (_TableName.ToLower() != "variable" && _DataTypeCode == "datetime") {
                    _COLUMNS_NAMEDateTime = _COLUMNS_NAMEDateTime + ";" + _TableName.ToLower() +"."  + _ColumnName;
                }

                _AutoTemplate.AddColumn(_ColumnName);

                Entitys.Add(_COLUMNEntity);

            }

            JSONObject _JSONObject = AppConfigs.LoadSetting("ColumnScale.json");

            foreach (string sName in _JSONObject.Names) {
                if (_JSONObject.ContainsKey(sName)){
                    _AutoTemplate.SetValue(sName + "_Scale" , _JSONObject.GetInteger(sName));
                }
            }

            _AutoTemplate.SetValue("Entitys"       , Entitys);
            _AutoTemplate.SetValue("COLUMNS_NAME"  , _COLUMNS_NAME);
            _AutoTemplate.SetValue("COLUMNS_NAMED" , _COLUMNS_NAMEDateTime);

            string newhash   = "";
            string _App_Path = Util.Separator(AppConfigs.DevelopPath + "\\");
            string _Path     = Util.Separator(AppConfigs.ProjectPath + "\\src\\");

            if (File.Exists(_App_Path + UID_TP_CODE + ".tpl"))
            {

                using(StreamReader sr = new StreamReader(_App_Path + UID_TP_CODE + ".tpl"))
                {
                    string s;
                    StringBuilder XCodeObject = new StringBuilder();

                    while ((s = sr.ReadLine()) != null)
                    {
                        int _p = s.IndexOf("=");

                        if (_p > 0) {
                            string cName = s.Substring(0, _p);
                            string cValue = s.Substring(_p + 1, s.Length - _p - 1);

                            cName  = cName.Trim();
                            cValue = cValue.Trim();

                            cName  = cName.Replace("{sUID}"         , sUID);
                            cName  = cName.Replace("{UID_TP_CODE}"  , UID_TP_CODE);
                            cValue = cValue.Replace("{sUID}"        , sUID);
                            cValue = cValue.Replace("{AppPath}"     , AppConfigs.GetValue("AppPath"));
                            cValue = cValue.Replace("{ProjectPath}" , AppConfigs.ProjectPath);
                            cValue = cValue.Replace("{DevelopPath}" , AppConfigs.DevelopPath);
                            cValue = cValue.Replace("{UID_TP_CODE}" , UID_TP_CODE);

                            if (cName == "Path") {
                                _Path = Util.Separator(cValue);
                            } else {

                                CoreUtil.CreateDir(Util.Separator(_Path + sUID));

                                _AutoTemplate.Template   =  Util.Separator(cName);
                                _AutoTemplate.OutputFile =  Util.Separator(_Path + sUID+@"\" + cValue);
                                _AutoTemplate.Process();

                            }

                        }
                    }
                }
            }
            _AutoTemplate.Close();

        }

        private void GeneratorCaptionDefine(DbContext _DbContext , string sUID)
        {

            CoreUtil.CreateDir(Util.Separator(AppConfigs.DevelopPath + @"\definition\functions"));

            JSONObject _JSONObject = new JSONObject();
            QueryRows RsSysCaption   = new QueryRows(_DbContext);
            RsSysCaption.CommandText = "SELECT * FROM syscaption WHERE sCaptionLang = '" + sUID + "' ORDER BY sCaptionCode";
            RsSysCaption.Open();

            while (!RsSysCaption.EOF)
            {

                string sCaptionCode = RsSysCaption.GetValue("sCaptionCode").ToLower();
                string sCaption     = RsSysCaption.GetValue("sCaption");

                _JSONObject.SetValue(sCaptionCode , sCaption);

                RsSysCaption.MoveNext();
            }
            RsSysCaption.Close();

            if (File.Exists(Util.Separator(AppConfigs.DevelopPath + @"\definition\"+sUID+".json"))) {
                File.Delete(Util.Separator(AppConfigs.DevelopPath + @"\definition\"+sUID+".json"));
            }
            Utils.Util.WriteContents(Util.Separator(AppConfigs.DevelopPath + @"\definition\"+sUID+".json") , _JSONObject.ToString());
        }

        private void GeneratorActivityDefinition(DbContext _DbContext , string sUID)
        {

            CoreUtil.CreateDir(Util.Separator(AppConfigs.DevelopPath + @"\definition\functions"));


            if (File.Exists(Util.Separator(AppConfigs.DevelopPath + "\\definition\\functions\\"+sUID+".json"))) {
                File.Delete(Util.Separator(AppConfigs.DevelopPath + "\\definition\\functions\\"+sUID+".json"));
            }

            QueryRows RsZUPRGDTM = new QueryRows(_DbContext);
            RsZUPRGDTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE bActive<>0 AND sUID='" + sUID + "' ORDER BY nIndex";
            RsZUPRGDTM.Open();

            if (RsZUPRGDTM.EOF) {
                return;
            }

            QueryRows RsSysFunction   = new QueryRows(_DbContext);
            QueryRows _SysColumnClass = new QueryRows(_DbContext);

            RsSysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID='" + sUID + "'";
            RsSysFunction.Open();

            string _TableName     = "";
            string _ColumnName    = "";
            string _DataType      = "";
            int Keys              = 0;
            string sLNKUID        = "";
            string sTableName     = "";
            string sLNK_TableName = "";
            string sColumnName    = "";
            string sType          = "";
            string sHash          = "";
            bool   bActive        = false;

            if (RsSysFunction.EOF) {
                return;
            } else {
                sTableName  = RsSysFunction.GetValue("sTableName");
                sColumnName = RsSysFunction.GetValue("sColumnName");
                bActive     = RsSysFunction.GetBoolean("bActive");
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
            string _sColumnClass  = "";

            List<COLUMNEntity> Entitys = new List<COLUMNEntity>();
            List<string> sRef          = new List<string>();
            JSONArray _entitys         = new JSONArray();
            QueryRows _SysRef          = new QueryRows(_DbContext);

            while (!RsZUPRGDTM.EOF) {

                _TableName = RsZUPRGDTM.GetValue("sTableName");

                if (AppConfigs.GetBoolean("LowerName")){
                    _TableName = _TableName.ToLower();
                }

                _ColumnName   = RsZUPRGDTM.GetValue("sColumnName");
                _DataType     = RsZUPRGDTM.GetValue("sDataType");
                _sColumnClass = RsZUPRGDTM.GetValue("sColumnClass");

                COLUMNEntity _COLUMNEntity   = new COLUMNEntity();
                _COLUMNEntity.CaptionCode    = RsZUPRGDTM.GetValue("sCaptionCode");
                //_COLUMNEntity.ColSpan        = RsZUPRGDTM.GetInteger("nColSpan");
                _COLUMNEntity.ColumnName     = _ColumnName;
                _COLUMNEntity.ColumnNullable = RsZUPRGDTM.GetBoolean("bColumnNullable");
                _COLUMNEntity.DataTypeCode   = _DataType;
                _COLUMNEntity.EnableMode     = RsZUPRGDTM.GetValue("sEnableMode");
                _COLUMNEntity.bHasCaption    = RsZUPRGDTM.GetBoolean("bHasCaption");
                //_COLUMNEntity.Height         = RsZUPRGDTM.GetInteger("nHeight");
                _COLUMNEntity.Index          = RsZUPRGDTM.GetInteger("nIndex");
                _COLUMNEntity.Length         = RsZUPRGDTM.GetInteger("nWidth");
                _COLUMNEntity.MaxLength      = RsZUPRGDTM.GetInteger("nWidth");
                _COLUMNEntity.Options        = RsZUPRGDTM.GetValue("sOption");
                //_COLUMNEntity.ColumnAlign    = RsZUPRGDTM.GetValue("sColumnAlign");
                //_COLUMNEntity.CaptionAlign   = RsZUPRGDTM.GetValue("sCaptionAlign");
                _COLUMNEntity.Sequency       = RsZUPRGDTM.GetInteger("nSequency");
                _COLUMNEntity.TableName      = _TableName;
                _COLUMNEntity.bWriteable     = RsZUPRGDTM.GetBoolean("bWriteable");

                if (_TableName.ToLower() == "variable") {
                    _COLUMNEntity.bWriteable = false;
                }

                if (_COLUMNEntity.Sequency == 0) {
                    _COLUMNEntity.Sequency = _COLUMNEntity.Index;
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
                _SysRef.CommandText = "SELECT * FROM sysref WHERE sRefCode = '" + _COLUMNEntity.sRefBrowse + "'";
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
                    _COLUMNEntity.CaptionCode = RsSysFields.GetValue("sCaptionCode");

                    if (!RsSysFields.GetBoolean("bColumnUsage")) {
                        _DbContext.Execute("UPDATE sysfields Set bColumnUsage=1 WHERE sTableName = '" + _TableName + "' AND sColumnName='" + _ColumnName + "'");
                    }

                    if (string.IsNullOrEmpty(_DataType)) {
                        _DataType = RsSysFields.GetValue("sDataType");
                    }

                    _COLUMNEntity.Length      = RsSysFields.GetInteger("nColumnLength");
                    _COLUMNEntity.ColumnScale = RsSysFields.GetInteger("nColumnScale");
                    _COLUMNEntity.bPrimaryKey = RsSysFields.GetBoolean("bPrimaryKey");

                    string sColumnClass = RsSysFields.GetValue("sColumnClass");

                    if (_COLUMNEntity.ColumnScale < 0) {

                        JSONObject _JSONObject2= AppConfigs.LoadSetting("ColumnScale.json");

                        if (_JSONObject2.ContainsKey(sColumnClass)){
                            _COLUMNEntity.ColumnScale =_JSONObject2.GetInteger(sColumnClass);
                        }
                    }

                }

                RsSysFields.Close();

                if (_COLUMNEntity.Length > 60 && _DataType != "ntext") {
                    _COLUMNEntity.Length = 60;
                }

                JSONObject _JSONObject3= AppConfigs.LoadSetting("AdjustmentLength.json");
                if (_DataType == "datetime") {
                    _COLUMNEntity.MaxLength = 10;
                }

                if (_DataType == "decimal") {
                    _COLUMNEntity.MaxLength = 12;
                }

                if (_DataType == "int") {
                    _COLUMNEntity.MaxLength = 9;
                }

                int nMax=_COLUMNEntity.MaxLength;
                if (nMax <=0) {
                    nMax = 8;
                }
                if (nMax > 80) {
                    nMax = 80;
                }
                if (_JSONObject3.ContainsKey(nMax.ToString())){

                    _COLUMNEntity.Length =_JSONObject3.GetInteger(nMax.ToString());
                }

                if (_COLUMNEntity.bPrimaryKey) {
                    Keys++;
                }

                //if (_DataType == "ntext" && _COLUMNEntity.Height <= 2) {
                //_COLUMNEntity.Height = 3;
                //}

                _COLUMNEntity.DataTypeCode   = _DataType;

                _COLUMNEntity.DataTypeChar   = this.DataTypeChar(_DataType);

                if (_TableName.ToLower() == "variable" && (_COLUMNEntity.ColumnName == "sHash" || _COLUMNEntity.ColumnName == "sHash2" || _COLUMNEntity.ColumnName == "sHash3")) {
                    _COLUMNEntity.DataTypeChar = "p";
                }

                if (_TableName.ToLower() != "variable" && (_DataType == "nvarchar" || _DataType == "ntext")) {
                    if (_ColumnName != "sOriginal") {
                        _COLUMNS_NAME = _COLUMNS_NAME + ";" + _TableName + "." + _ColumnName;
                    }
                }

                if (_TableName.ToLower() != "variable" && _DataType == "datetime") {
                    _COLUMNS_NAMED = _COLUMNS_NAMED + ";" + _TableName  + _ColumnName;
                }

                if (_TableName.ToLower() == "variable" && (_DataType == "decimal" || _DataType == "picture")) {
                    if (_sColumnClass == "N00") {
                        _COLUMNEntity.ColumnScale = 0;
                    } else if (_sColumnClass == "N01") {
                        _COLUMNEntity.ColumnScale = 1;
                    } else if (_sColumnClass == "N02") {
                        _COLUMNEntity.ColumnScale = 2;
                    } else if (_sColumnClass == "N03") {
                        _COLUMNEntity.ColumnScale = 3;
                    } else if (_sColumnClass == "N04") {
                        _COLUMNEntity.ColumnScale = 4;
                    } else if (_sColumnClass == "N05") {
                        _COLUMNEntity.ColumnScale = 5;
                    } else if (_sColumnClass == "N06") {
                        _COLUMNEntity.ColumnScale = 6;
                    } else if (_sColumnClass == "N07") {
                        _COLUMNEntity.ColumnScale = 7;
                    } else if (_sColumnClass == "N08") {
                        _COLUMNEntity.ColumnScale = 8;
                    } else if (_sColumnClass == "ND00") {
                        _COLUMNEntity.ColumnScale = 0;
                    } else if (_sColumnClass == "ND01") {
                        _COLUMNEntity.ColumnScale = 1;
                    } else if (_sColumnClass == "ND02") {
                        _COLUMNEntity.ColumnScale = 2;
                    } else if (_sColumnClass == "ND03") {
                        _COLUMNEntity.ColumnScale = 3;
                    } else if (_sColumnClass == "ND04") {
                        _COLUMNEntity.ColumnScale = 4;
                    } else if (_sColumnClass == "ND05") {
                        _COLUMNEntity.ColumnScale = 5;
                    } else if (_sColumnClass == "ND06") {
                        _COLUMNEntity.ColumnScale = 6;
                    } else if (_sColumnClass == "ND07") {
                        _COLUMNEntity.ColumnScale = 7;
                    } else if (_sColumnClass == "ND08") {
                        _COLUMNEntity.ColumnScale = 8;
                    }else if (Utils.Util.IsNumeric(_sColumnClass)){
                        _COLUMNEntity.ColumnScale = Utils.Util.ToInt(_sColumnClass);
                    }
                } else if (_TableName.ToLower() == "variable" && _DataType == "") {
                    _COLUMNEntity.DataTypeChar = "c";
                    _COLUMNEntity.DataTypeCode   = "nvarchar";
                }

                _COLUMNEntity.EnableMode = _COLUMNEntity.EnableMode.Replace("ADDNEW" , "NEW");
                _COLUMNEntity.EnableMode = _COLUMNEntity.EnableMode.Replace("UPDATE" , "EDIT");

                JSONObject _entity = new JSONObject();

                _entity.SetValue("sTableName"       , _COLUMNEntity.TableName);
                _entity.SetValue("sColumnName"      , _COLUMNEntity.ColumnName);
                _entity.SetValue("CaptionCode"      , _COLUMNEntity.CaptionCode);
                _entity.SetBoolean("ColumnNullable" , _COLUMNEntity.ColumnNullable);
                _entity.SetBoolean("bHasCaption"    , _COLUMNEntity.bHasCaption);
                _entity.SetBoolean("Writeable"      , _COLUMNEntity.bWriteable);
                _entity.SetBoolean("bPrimaryKey"    , _COLUMNEntity.bPrimaryKey);
                _entity.SetBoolean("Release"        , _COLUMNEntity.Release);
                //_entity.SetInteger("ColSpan"        , _COLUMNEntity.ColSpan);
                //_entity.SetInteger("Height"         , _COLUMNEntity.Height);
                _entity.SetInteger("Index"          , _COLUMNEntity.Index);
                _entity.SetInteger("Length"         , _COLUMNEntity.Length);
                _entity.SetInteger("MaxLength"      , _COLUMNEntity.MaxLength);
                _entity.SetInteger("Sequency"       , _COLUMNEntity.Sequency);
                _entity.SetInteger("ColumnScale"    , _COLUMNEntity.ColumnScale);
                //_entity.SetValue("CaptionAlign"     , _COLUMNEntity.CaptionAlign);
                //_entity.SetValue("ColumnAlign"      , _COLUMNEntity.ColumnAlign);
                _entity.SetValue("EnableMode"       , _COLUMNEntity.EnableMode);
                _entity.SetValue("Options"          , _COLUMNEntity.Options);
                if (_TableName.ToLower() == "variable" && (_COLUMNEntity.ColumnName == "sHash" || _COLUMNEntity.ColumnName == "sHash2" || _COLUMNEntity.ColumnName == "sHash3")) {

                    _entity.SetValue("DataTypeChar" , "p");

                }else{

                    _entity.SetValue("DataTypeChar" , _COLUMNEntity.DataTypeChar);
                }
                _entity.SetValue("DataType"   , _COLUMNEntity.DataTypeCode);
                _entity.SetValue("EnableMode" , _COLUMNEntity.EnableMode);
                _entity.SetValue("sCheckId"   , _COLUMNEntity.sRefCheck);
                _entity.SetValue("sRefBrowse" , _COLUMNEntity.sRefBrowse);
                _entity.SetValue("sRefViewer" , _COLUMNEntity.sRefViewer);

                _entitys.Add(_entity);

                Entitys.Add(_COLUMNEntity);

                RsZUPRGDTM.MoveNext();
            }

            RsZUPRGDTM.Close();

            JSONObject _JSONRefs = new JSONObject();

            foreach (string sRefCode in sRef){
                JSONObject _JSONRef = new JSONObject();

                _SysRef = new QueryRows(_DbContext);
                _SysRef.CommandText = "SELECT * FROM sysref WHERE sRefCode = '" + sRefCode+ "'";
                _SysRef.Open();

                if (!_SysRef.EOF) {

                    _JSONRef.SetValue("sRefCode"     , _SysRef.GetValue("sRefCode"));
                    _JSONRef.SetValue("sType"        , _SysRef.GetValue("sType"));
                    _JSONRef.SetValue("sTableName"   , _SysRef.GetValue("sTableName"));
                    _JSONRef.SetValue("sSqlCode"     , _SysRef.GetValue("sSqlCode"));
                    _JSONRef.SetValue("sWhereClause" , _SysRef.GetValue("sWhereClause"));

                    QueryRows _SysData = new QueryRows(_DbContext);
                    _SysData.CommandText = "SELECT * FROM sysdata WHERE sSqlCode = '" + _SysRef.GetValue("sSqlCode")+ "'";
                    _SysData.Open();

                    if (!_SysData.EOF) {
                        _JSONRef.SetValue("sFromClause"  , _SysData.GetValue("sFromClause"));
                    }
                    _SysData.Close();
                }
                _SysRef.Close();

                JSONArray _JSONSysRefDtl = new JSONArray();

                QueryRows _SysRefDtl = new QueryRows(_DbContext);
                _SysRefDtl.CommandText = "SELECT * FROM sysrefdtl WHERE sRefCode = '" + sRefCode+ "'";
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

            JSONObject _JSONFunction = new JSONObject();
            _JSONFunction.SetValue("sUID"            , sUID);
            _JSONFunction.SetValue("sTableName"      , sTableName);
            _JSONFunction.SetValue("PK_ColumnName"   , sColumnName);
            _JSONFunction.SetValue("LNK_TableName"   , sLNK_TableName);
            _JSONFunction.SetValue("sLNKUID"         , sLNKUID);
            _JSONFunction.SetValue("ROOT_LNKUID"     , sROOT_LNKUID);
            _JSONFunction.SetValue("ROOT_TableName"  , sRoot_TableName);
            _JSONFunction.SetValue("ROOT_ColumnName" , sRoot_ColumnName);
            _JSONFunction.SetBoolean("bActive"       , bActive);
            _JSONFunction.SetValue("sHash"           , sHash);
            _JSONFunction.SetValue("entitys"         , _entitys);
            _JSONFunction.SetValue("sysref"          , _JSONRefs);

            Utils.Util.WriteContents(Util.Separator(AppConfigs.DevelopPath + "\\definition\\functions\\"+sUID+".json") , JsonFormatter.PrettyPrint(_JSONFunction.ToString()));

        }

        public void GeneratorEntityDefinition()
        {
            DbContext  _Trans = new DbContext(AppConfigs.GetValue("sDbName") , AppConfigs.GetValue("Provider") , AppConfigs.GetValue("dbAdapter"));
            try {


                List<JSONObject> _JSONObject = _TableUtil.DatabaseTable(_Trans , "");

                int i = 0;
                int n = 0;

                JSONObject Ignore = AppConfigs.JSONObject("Ignore");
                string IgnoreTables=Ignore.GetValue("Tables");
                string IgnoreTableColumns=Ignore.GetValue("TableColumns");
                List<string> hIgnoreTables = (IgnoreTables + ",").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                List<string> hIgnoreTableColumns = (IgnoreTableColumns + ",").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                hIgnoreTables.Add("dtproperties");
                hIgnoreTables.Add("sysfunctiondtl");

                foreach (JSONObject _Table in _JSONObject) {
                    string _sTableName = _Table.GetValue("sTableName");

                    if (hIgnoreTables.Contains(_sTableName)) {

                    } else {

                        List<JSONObject> _JSONColumn = _TableUtil.DatabaseTableColumns(_Trans , _sTableName);

                        if (File.Exists(Util.Separator(AppConfigs.DevelopPath + "\\definition\\entity\\"+_sTableName+".json"))) {
                            File.Delete(Util.Separator(AppConfigs.DevelopPath + "\\definition\\entity\\"+_sTableName+".json"));
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

                            if (bActive) {

                                string sDataType   = RsSysFields.GetValue("sDataType");
                                string sColumnName = RsSysFields.GetValue("sColumnName");
                                string sTableName  = RsSysFields.GetValue("sTableName");
                                int nColumnLength  = RsSysFields.GetInteger("nColumnLength");

                                JSONObject _ColumnEntity = new JSONObject();
                                _ColumnEntity.SetValue("sTableName"  , sTableName.Trim());
                                _ColumnEntity.SetValue("sColumnName" , sColumnName);
                                _ColumnEntity.SetValue("sDataType"   , sDataType);
                                _ColumnEntity.SetValue("bPrimaryKey" , RsSysFields.GetBoolean("bPrimaryKey"));
                                _ColumnEntity.SetValue("bAutoIdentity", RsSysFields.GetBoolean("bAutoIdentity"));
                                _ColumnEntity.SetInteger("nLength"   , nColumnLength);

                                if (sDataType == "nvarchar") {
                                    if (nColumnLength > 1000 || nColumnLength < 0) {
                                        _ColumnEntity.SetInteger("nLength" , 60);
                                        sDataType = "ntext";
                                    }
                                    _ColumnEntity.SetInteger("nLength" , nColumnLength/2 );
                                }

                                _ColumnEntity.SetValue("sDataType" , DataType(sDataType));

                                _Fields.Add(_ColumnEntity);
                            }
                        }

                        JSONObject _JSONTableName = new JSONObject();

                        _JSONTableName.SetValue(_sTableName , _Fields);

                        Utils.Util.WriteContents(Util.Separator(AppConfigs.DevelopPath + "\\definition\\entity\\"+_sTableName+".json") , JsonFormatter.PrettyPrint(_JSONTableName.ToString()));

                    }
                }

            } catch {

            }finally{
                _Trans.Close();
            }
        }

        public void GeneratorEntity()
        {

            CoreUtil.CreateDir(Util.Separator(AppConfigs.DevelopPath + "\\definition"));
            CoreUtil.CreateDir(Util.Separator(AppConfigs.DevelopPath + "\\definition\\entity"));
            CoreUtil.CreateDir(Util.Separator(AppConfigs.ProjectPath + "\\src"));
            CoreUtil.CreateDir(Util.Separator(AppConfigs.ProjectPath + "\\src\\entity"));

            List<string> sTableNames = new List<string>();

            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode    = this.DebugMode;
            _AutoTemplate.AppConfigs   = this.AppConfigs;

            _AutoTemplate.SetValue("AppPath"     , AppConfigs.GetValue("AppPath"));
            _AutoTemplate.SetValue("DistPath"    , AppConfigs.GetValue("DistPath"));
            _AutoTemplate.SetValue("ProjectPath" , AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("ProjectName" , AppConfigs.GetValue("ProjectName"));

            string localDirectory = Util.Separator(AppConfigs.DevelopPath + @"\definition\entity\");

            WriteLine("localDirectory = "+ localDirectory);

            List<COLUMNEntity> ColumnEntity = new List<COLUMNEntity>();

            if (Directory.Exists(localDirectory)) {

                DirectoryInfo _DirInfo = new DirectoryInfo(localDirectory);

                FileSystemInfo[] objFiles = _DirInfo.GetFileSystemInfos("*.json");

                for (int i = 0; i < objFiles.Length; i++) {
                    FileInfo _FileInfo = objFiles[i] as FileInfo;

                    if (_FileInfo != null) {

                        JSONObject _JSONTableNames =  AppConfigs.LoadJSONObject(_FileInfo.FullName);

                        foreach (string _s in _JSONTableNames.Names) {

                            string sTableName =_s;

                            ColumnEntity = new List<COLUMNEntity>();

                            if (_JSONTableNames.GetType(_s)=="l"){

                                JSONArray _JSONObjects = _JSONTableNames.GetJSONArray(_s);

                                foreach (JSONObject _JSONObject in _JSONObjects.JSONObjects) {

                                    COLUMNEntity _COLUMNEntity = new COLUMNEntity();
                                    _COLUMNEntity.TableName    = sTableName;
                                    _COLUMNEntity.ColumnName   = _JSONObject.GetValue("sColumnName");
                                    _COLUMNEntity.DataTypeCode = _JSONObject.GetValue("sDataType");
                                    _COLUMNEntity.bPrimaryKey  = _JSONObject.GetBoolean("bPrimaryKey");
                                    _COLUMNEntity.bAutoIdentity = _JSONObject.GetBoolean("bAutoIdentity");
                                    _COLUMNEntity.Length       = _JSONObject.GetInteger("nLength");
                                    ColumnEntity.Add(_COLUMNEntity);
                                }
                            }

                            _AutoTemplate.SetValue("Entitys"    , ColumnEntity);
                            _AutoTemplate.SetValue("sTableName" , sTableName);

                            _AutoTemplate.Template   = "N_Entity_Template.cs";
                            _AutoTemplate.OutputFile = Util.Separator(AppConfigs.ProjectPath + "\\src\\entity\\" +Volte.Utils.Util.ToCamelCase(sTableName) + "Entity.cs");
                            _AutoTemplate.Process();

                            sTableNames.Add(sTableName);

                            this.WriteLine("[" + sTableName + "]");
                        }
                    }
                }

                _AutoTemplate.SetValue("sTableNames" , sTableNames);

                _AutoTemplate.Template   = "N_Entity_Build_Template.cs";
                _AutoTemplate.OutputFile = Util.Separator(AppConfigs.ProjectPath  + @"\src\entity\\entity.Build");
                _AutoTemplate.Process();

                _AutoTemplate.Template = "N_Entity_Build_Template.csproj";
                _AutoTemplate.OutputFile = Util.Separator(AppConfigs.ProjectPath + @"\src\entity\\entity.csproj");
                _AutoTemplate.Process();

                Prettify("entity");

                string sArguments = AppConfigs.GetValue("Arguments");

                sArguments = Utils.Util.ReplaceWith(sArguments , "{sUID}" , Util.Separator(@"entity"));

                CommandEntity sCommand = new CommandEntity();
                sCommand.sDirectory    = Util.Separator(AppConfigs.ProjectPath + @"\src\entity");
                sCommand.sCommand      = AppConfigs.GetValue("Compiler");
                sCommand.sArguments    = sArguments;

                sCommand=_ShellRunner.Execute(sCommand);
                //Console.WriteLine(sCommand.Message);
                sCommandEntity.Add(sCommand);

            }else{
                Console.WriteLine(localDirectory+" Not Found!");
            }
            _AutoTemplate.Close();
        }

        private static bool IsAllChar(string text)
        {
            foreach (char tempchar in text.ToCharArray()) {
                if (!char.IsLetter(tempchar)) {
                    return false;
                }
            }

            return true;
        }

        public void AddFunction(string sUID , string sTableName , string sColumnName)
        {
            try {
                TableUtil _TableUtil = new TableUtil();
                sUID = sUID.ToUpper();

                if (sUID.Length != 9) {
                    this.WriteLine("invalid sUID Format [AAA00000A] " + sUID);
                    this.WriteLine("-u [AAA00000A] " + sUID);
                    return;
                }

                if (!DapperUtil.IsNumeric(sUID.Substring(3, 5))) {
                    this.WriteLine("Invalid sUID Format [AAA#####A] " + sUID);
                    this.WriteLine("-u [AAA00000A] " + sUID);
                    return;
                }

                string sModuleCode = sUID.Substring(0, 3);
                string sModuleList="ADM,MKT,ENG,PUR,STR,ACC,APP,LEV,TAX,PAY,UID,";

                if (sModuleList.IndexOf(sModuleCode)<0) {
                    this.WriteLine("Invalid sUID Format [AAA#####A] " + sUID);
                    this.WriteLine("Invalid Module AAA" + sModuleCode);
                    this.WriteLine("-u [ADA00000A] " + sUID);
                    return;
                }

                if (!IsAllChar(sModuleCode)) {
                    this.WriteLine("Invalid sUID Format [AAA#####A] " + sUID);
                    this.WriteLine("-u [AAA00000A] " + sUID);
                    return;
                }

                if (!IsAllChar(sUID.Substring(8, 1))) {
                    this.WriteLine("Invalid sUID Format [AAA#####A] " + sUID);
                    this.WriteLine("-u [AAA00000A] " + sUID);
                    return;
                }

                if (sColumnName == "") {
                    sColumnName = "%";
                }else{
                    sColumnName = sColumnName.Replace("*" , "%");
                }

                List<JSONObject> aryColumns = new List<JSONObject>();

                string _Column      = "";
                string _PKColumn    = "";
                bool   _NewFunction = false;

                DbContext _Trans = new DbContext(AppConfigs.GetValue("sDbName") , AppConfigs.GetValue("Provider") , AppConfigs.GetValue("dbAdapter"));

                QueryRows sysfunction   = new QueryRows(_Trans);
                sysfunction.CommandText = "SELECT * From sysfunction Where sUID='" + sUID + "'";
                sysfunction.Open();

                if (sysfunction.EOF) {

                    if (sTableName == "") {
                        this.WriteLine("TableName Miss , -table xxx*");
                        return;
                    }else{
                        sTableName = sTableName.Replace("*"  , "%");
                    }

                    aryColumns = _TableUtil.DatabaseTableColumns(_Trans, sTableName);

                    if (aryColumns.Count == 0) {
                        return;
                    }

                    foreach (JSONObject colname in aryColumns) {
                        if (colname.GetBoolean("bPrimaryKey")) {
                            sTableName = colname.GetValue("sTableName");
                            _Column    = colname.GetValue("sColumnName");
                            _PKColumn  = colname.GetValue("sColumnName");
                        }
                    }

                    if (_Column == "") {
                        this.WriteLine("ColumnName Miss");
                        return;
                    }

                    this.WriteLine("Add Function... Table =" + sTableName + " , ColumnName=" + _Column);

                    string sAppend = "IF NOT EXISTS (SELECT * FROM sysfunction WHERE sUID='" + sUID + "')\n";
                    sAppend = sAppend + "INSERT INTO sysfunction(sUID,sModuleCode)";
                    sAppend = sAppend + " VALUES('" + sUID + "','"+sModuleCode+"')\n";

                    _Trans.Execute(sAppend);

                    sAppend = "Update sysfunction Set sCaptionCode='lbl_'+sUID Where sUID='" + sUID + "'";
                    _Trans.Execute(sAppend);

                    sAppend = "Update sysfunction Set sUID_TP_CODE='L',sSqlCode='Q'+sUID Where sUID='" + sUID + "'";
                    _Trans.Execute(sAppend);

                    sAppend = "Update sysfunction Set bActive=1 Where sUID='" + sUID + "'";
                    _Trans.Execute(sAppend);

                    sAppend = "Update sysfunction Set sTableName='" + sTableName + "' Where sUID='" + sUID + "'";
                    _Trans.Execute(sAppend);

                    sAppend = "Update sysfunction Set sColumnName='" + _Column + "' Where sUID='" + sUID + "'";
                    _Trans.Execute(sAppend);

                    sAppend = "Update sysfunction Set sUIDGP='" + sUID + "' Where sUID='" + sUID + "'";
                    _Trans.Execute(sAppend);

                    sAppend = "IF NOT EXISTS (SELECT * FROM sysdata WHERE sSqlCode='Q" + sUID + "')\n";
                    sAppend = sAppend + "INSERT INTO sysdata(sSqlCode,sUID)";
                    sAppend = sAppend + " VALUES('Q" + sUID + "','" + sUID + "')\n";

                    _Trans.Execute(sAppend);

                    _Trans.Execute("UPDATE sysdata SET sFromClause='" + sTableName + "' WHERE sSqlCode='Q" + sUID + "' AND sFromClause Is Null");

                    _NewFunction = true;

                    string fileName1 = Util.Separator(AppConfigs.GetValue("CodePath") + "\\L.cs");
                    string fileName2 = Util.Separator(AppConfigs.GetValue("CodePath") + "\\" + sUID + ".cs");

                    if (sUID.Substring(3, 2) == "02") {
                        if (File.Exists(fileName1) && !File.Exists(fileName2)) {
                            File.Copy(fileName1 , fileName2);

                        }
                    }

                    if (sUID.Substring(3, 2) == "03") {
                        fileName1 = Util.Separator(AppConfigs.GetValue("CodePath") + "\\T.cs");
                        fileName2 = Util.Separator(AppConfigs.GetValue("CodePath") + "\\" + sUID + ".cs");

                        if (File.Exists(fileName1) && !File.Exists(fileName2)) {
                            File.Copy(fileName1 , fileName2);
                        }
                    }

                } else {

                    if (sTableName.ToLower()!="variable" && sTableName!="" && sTableName != sysfunction.GetValue("sTableName")){
                        this.WriteLine("TableName MisMath "+sysfunction.GetValue("sTableName")+"<>"+sTableName);
                        return;
                    }

                    if (sTableName.ToLower()!="variable"){
                        sTableName = sysfunction.GetValue("sTableName");
                    }

                    if (sTableName == "") {
                        this.WriteLine("TableName Miss , -table xxx*");
                        return;
                    }

                    aryColumns = _TableUtil.DatabaseTableColumns(_Trans , sTableName);

                    if (sTableName.ToLower()=="variable"){
                        JSONObject _VariableColumn = new JSONObject();
                        _VariableColumn.SetValue("sColumnName"       , sColumnName);
                        _VariableColumn.SetValue("sTableName"        , sTableName);
                        _VariableColumn.SetValue("nColumnScale"      , 0);
                        _VariableColumn.SetValue("sDataType"       , "nvarchar");
                        _VariableColumn.SetValue("nColumnLength"     , 10);
                        _VariableColumn.SetValue("bPrimaryKey"       , false);
                        _VariableColumn.SetValue("bColumnNullable"   , true);
                        _VariableColumn.SetValue("bAutoIdentity"     , false);
                        _VariableColumn.SetValue("sDefault"          , "");
                        _VariableColumn.SetValue("nNumericPrecision" , 0);

                        aryColumns.Add(_VariableColumn);

                    }

                    if (aryColumns.Count == 0) {
                        return;
                    }

                    foreach (JSONObject colname in aryColumns) {
                        if (colname.GetBoolean("bPrimaryKey")) {
                            sTableName = colname.GetValue("sTableName");
                            _Column    = colname.GetValue("sColumnName");
                            _PKColumn  = colname.GetValue("sColumnName");

                            string sAppend = "Update sysfunction Set sTableName = '" + sTableName + "' Where sUID = '" + sUID + "'";
                            _Trans.Execute(sAppend);

                            sAppend = "Update sysfunction Set sColumnName = '" + _Column + "' Where sUID = '" + sUID + "'";
                            _Trans.Execute(sAppend);
                        }
                    }

                    _Trans.Execute("UPDATE sysdata SET sFromClause='" + sTableName + "' WHERE sSqlCode='Q" + sUID + "' AND sFromClause Is Null");

                }

                sysfunction.Close();

                string sCaptionCode = "";
                int    NewColumns   = 0;
                string sSysFields   = "dAddDate,dChgDate,sAddUser,sChgUser";
                QueryRows RsZUPRGDTM = new QueryRows(_Trans);

                foreach (JSONObject colname in aryColumns) {

                    _Column    = colname.GetValue("sColumnName");
                    sTableName = colname.GetValue("sTableName");


                    if (sTableName.ToLower()!="variable"){

                        sCaptionCode = "lbl_"+ sTableName+"_"+_Column ;
                    }

                    RsZUPRGDTM = new QueryRows(_Trans);
                    RsZUPRGDTM.CommandText = "SELECT * FROM sysfunctiondtl WHERE sUID='" + sUID + "' AND sTableName='" + sTableName + "' AND sColumnName='" + _Column + "'";
                    RsZUPRGDTM.Open();

                    if (RsZUPRGDTM.EOF) {

                        NewColumns++;

                        string sRefBrowse = "";
                        string sRefCheck   = "";
                        string sRefViewer    = "";

                        QueryRows RsZUPRGDTM2 = new QueryRows(_Trans);
                        RsZUPRGDTM2.CommandText = "SELECT * FROM sysfunctiondtl Where sColumnName='" + _Column + "'";
                        RsZUPRGDTM2.Open();

                        if (!RsZUPRGDTM2.EOF) {
                            sRefBrowse = RsZUPRGDTM2.GetValue("sRefBrowse");
                            sRefCheck   = RsZUPRGDTM2.GetValue("sRefCheck");
                            sRefViewer    = RsZUPRGDTM2.GetValue("sRefViewer");
                        }

                        RsZUPRGDTM2.Close();

                        string sUID_DT    = IdGenerator.NewBase36("_");
                        string sSQLString = "INSERT INTO sysfunctiondtl (bActive , sKey , sUID , sTableName , sColumnName) ";

                        sSQLString = sSQLString + " VALUES(1 , '" + sUID_DT + "' , '" + sUID + "' , '" + sTableName + "' , '" + _Column + "')";

                        _Trans.Execute(sSQLString);

                        if (sSysFields.IndexOf(_Column) < 0) {
                            _Trans.Execute("UPDATE sysfunctiondtl SET bActive = 0 WHERE sKey = '" + sUID_DT + "'");
                        }
                        _Trans.Execute("UPDATE sysfunctiondtl SET bWriteable = 1 WHERE sKey = '" + sUID_DT + "'");
                        _Trans.Execute("UPDATE sysfunctiondtl SET bHasCaption = 1 WHERE sKey = '" + sUID_DT + "'");
                        _Trans.Execute("UPDATE sysfunctiondtl SET bNewCell   = 1 WHERE sKey = '" + sUID_DT + "'");
                        //_Trans.Execute("UPDATE sysfunctiondtl SET nColSpan  = 1 WHERE sKey = '" + sUID_DT + "'");
                        //_Trans.Execute("UPDATE sysfunctiondtl SET nRowSpan  = 1 WHERE sKey = '" + sUID_DT + "'");

                        _Trans.Execute("UPDATE sysfunctiondtl SET nIndex=999999 WHERE sKey='" + sUID_DT + "'");
                        _Trans.Execute("UPDATE sysfunctiondtl SET bColumnNullable=1 WHERE sKey='" + sUID_DT + "'");
                        _Trans.Execute("UPDATE sysfunctiondtl SET sCaptionCode='" + sCaptionCode + "' WHERE sKey='" + sUID_DT + "'");

                        _Trans.Execute("UPDATE sysfunctiondtl SET sRefViewer='" + sRefViewer + "' WHERE sKey='" + sUID_DT + "'");
                        _Trans.Execute("UPDATE sysfunctiondtl SET sRefBrowse='" + sRefBrowse + "' WHERE sKey='" + sUID_DT + "'");
                        _Trans.Execute("UPDATE sysfunctiondtl SET sRefCheck='" + sRefCheck + "' WHERE sKey='" + sUID_DT + "'");

                        if (_PKColumn.ToUpper() == _Column.ToUpper()) {
                            _Trans.Execute("UPDATE sysfunctiondtl SET sEnableMode='ADDNEW' WHERE sKey='" + sUID_DT + "'");
                            _Trans.Execute("UPDATE sysfunctiondtl SET bColumnNullable=0 WHERE sKey='" + sUID_DT + "'");
                        } else {
                            if (_Column=="sChgFlag") {
                                _Trans.Execute("UPDATE sysfunctiondtl SET sEnableMode='HIDDEN' WHERE sKey='" + sUID_DT + "'");
                            }
                            _Trans.Execute("UPDATE sysfunctiondtl SET sEnableMode='ADDNEW,UPDATE' WHERE sKey='" + sUID_DT + "'");
                        }
                    }
                }

                this.WriteLine("Function "+sUID);
                this.WriteLine("Add Columns "+NewColumns);

                _Trans.Execute("UPDATE sysfunctiondtl SET sysfunctiondtl.nWidth=sysfields.nColumnLength From sysfunctiondtl INNER JOIN sysfields ON sysfunctiondtl.sTableName = sysfields.sTableName AND sysfunctiondtl.sColumnName = sysfields.sColumnName Where sysfunctiondtl.sTableName<>'VARIABLE'");


                //_Trans.Execute("UPDATE sysfunctiondtl SET nColSpan=1 WHERE nColSpan<=0");
                //_Trans.Execute("UPDATE sysfunctiondtl SET nRowSpan=1 WHERE nRowSpan<=0");
                _Trans.Execute("UPDATE sysdata SET bDistinct=0 WHERE bDistinct IS NULL");
                _Trans.Execute("UPDATE sysdata SET bGroupBy=0 WHERE bGroupBy IS NULL");

            } catch (Exception e) {
                ZZLogger.Debug(ZFILE_NAME, e);
                this.WriteLine("Exception=" + e.ToString());
                string cMessage = "<HR>Message=[" + e.Message + "]" + "<HR>Source=[" + e.Source + "]<HR>StackTrace=[" + e.StackTrace + "]<HR>TargetSite=[" + e.TargetSite + "]";
                this.WriteLine("Exception=" + cMessage);
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
                                _SysFieldsEntity.bColumnNullable = _Fields.GetBoolean("bColumnNullable");
                                _SysFieldsEntity.bPrimaryKey     = _Fields.GetBoolean("bPrimaryKey");
                                _SysFieldsEntity.nColumnLength   = _Fields.GetInteger("nColumnLength");
                                _SysFieldsEntity.nColumnScale    = _Fields.GetInteger("nColumnScale");
                                _SysFieldsEntity.sCaptionCode    = _CaptionCode;
                                _SysFieldsEntity.sColumnName     = _COLUMN_NAME;
                                _SysFieldsEntity.sDataType       = _Fields.GetValue("sDataType");
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
                                _SysFieldsEntity.bColumnNullable = _Fields.GetBoolean("bColumnNullable");
                                _SysFieldsEntity.bAutoIdentity  = _Fields.GetBoolean("bAutoIdentity");

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
                ZZLogger.Debug(ZFILE_NAME, e);
                this.WriteLine("Exception=" + e.ToString());
                string cMessage = "<HR>Message=[" + e.Message + "]" + "<HR>Source=[" + e.Source + "]<HR>StackTrace=[" + e.StackTrace + "]<HR>TargetSite=[" + e.TargetSite + "]";
                this.WriteLine("Exception=" + cMessage);
            }

        }

        public void Templates()
        {

            CoreUtil.CreateDir(Util.Separator(AppConfigs.ProjectPath + "\\apps\\ddons"));
            CoreUtil.CreateDir(Util.Separator(AppConfigs.ProjectPath + "\\src"));


            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode    = this.DebugMode;
            _AutoTemplate.AppConfigs   = AppConfigs;
            _AutoTemplate.sUID         = "";
            _AutoTemplate.SetValue("sUID"        , "");
            _AutoTemplate.SetValue("ProjectName" , AppConfigs.GetValue("ProjectName"));
            _AutoTemplate.SetValue("AppPath"     , AppConfigs.GetValue("AppPath"));
            _AutoTemplate.SetValue("ProjectPath" , AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("DevelopPath" , AppConfigs.DevelopPath);

            string _App_Path = Util.Separator(AppConfigs.DevelopPath + "\\");
            string _Path     = Util.Separator(AppConfigs.ProjectPath + "\\src\\");

            string sFile=this.FileName;
            if (!File.Exists(sFile)) {
                sFile=_App_Path+this.FileName;
            }
            if (!File.Exists(sFile)) {
                sFile=_App_Path + "A.tpl";
            }
            Console.WriteLine(sFile);
            if (File.Exists(sFile)) {
                using(StreamReader sr = new StreamReader(sFile)) {
                    string s;

                    while ((s = sr.ReadLine()) != null) {
                        int _p = s.IndexOf("=");

                        if (_p > 0) {
                            string cName = s.Substring(0, _p);
                            string cValue = s.Substring(_p + 1, s.Length - _p - 1);

                            cName  = cName.Trim();
                            cValue = cValue.Trim();
                            cName  = cName.Replace("{UID_TP_CODE}"  , UID_TP_CODE);

                            cValue = cValue.Replace("{AppPath}"     , AppConfigs.GetValue("AppPath"));
                            cValue = cValue.Replace("{ProjectPath}" , AppConfigs.ProjectPath);
                            cValue = cValue.Replace("{UID_TP_CODE}" , UID_TP_CODE);


                            if (cName == "Path") {
                                _Path = cValue;
                            } else {
                                _AutoTemplate.Template   = cName;
                                _AutoTemplate.OutputFile = _Path + cValue;
                                Console.WriteLine("Template "+cName);
                                Console.WriteLine("---->    "+_Path + cValue);
                                _AutoTemplate.Process();

                            }

                        }
                    }
                }
            }
            _AutoTemplate.Close();
        }

        private string DataTypeChar(string dataType)
        {
            string sValue = AppConfigs.LoadSetting("DataTypeChar.json").GetValue(dataType);
            if (sValue==""){
                return "undefine";
            }else{
                return sValue;
            }
        }

        public string DataType(string sType)
        {

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

            RsSysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID='" + sUID + "'";
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

            RsSysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID='" + sUID + "'";
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

            QueryRows _SysFunction = new QueryRows(_DbContext);

            _SysFunction.CommandText = "SELECT * FROM sysfunction WHERE sUID='" + sUID + "'";
            _SysFunction.Open();

            if (!_SysFunction.EOF) {
                return sLNKUID;
            }else{
                return sLNKUID;
            }
        }
    }
}
