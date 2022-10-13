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
    public class AutoCoder
    {

        private ShellRunner _ShellRunner   = new ShellRunner();
        private List<string> _FAILURE      = new List<string>();
        private List<FileNameValue> _Hashs = new List<FileNameValue>();

        public List<CommandEntity> sCommandEntity = new List<CommandEntity>();

        public StringBuilder Message = new StringBuilder();
        public AppConfigs AppConfigs;

        public  string  Mode         = "";
        public  string  DebugMode    = "N";
        private string sTablePrefix  = "";
        private TableUtil _TableUtil = new TableUtil();
        private Substitute _Substitute=new Substitute();

        private List<string> _L_UID_CODE = new List<string>();

        public string FileName     { get ; set ; }
        public string gTableName   { get ; set ; }
        public string sTemplate    { get ; set ; }

        public AutoCoder()
        {
            sTemplate = "N";
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

        public void Process(string sUID)
        {
            Message = new StringBuilder();
            if (string.IsNullOrEmpty(sUID))
            {
                sUID = "";
            }

            _L_UID_CODE = new List<string>();

            DirectoryInfo dir = new DirectoryInfo(AppConfigs.AddonLocation);

            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo i in fileinfo)
            {
                if (!(i is DirectoryInfo))//判断是否文件夹
                {
                    string fileName=Path.GetFileNameWithoutExtension(i.FullName);

                    if (fileName.Contains(sUID))
                    {
                        if (this.DebugMode == "Y") {

                            this.WriteLine(fileName);
                        }

                        _L_UID_CODE.Add(fileName);

                        this.GeneratorActivity(fileName);
                    }
                }
            }

            foreach (string _UID_CODE in _L_UID_CODE)
            {
                Prettify(AppConfigs.ProjectPath + @"\src\",_UID_CODE);
                Replication(AppConfigs.ProjectPath + @"\src\",_UID_CODE);
                Build(_UID_CODE);
            }

            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode = this.DebugMode;
            _AutoTemplate.AppConfigs = AppConfigs;
            _AutoTemplate.Initialize();
            _AutoTemplate.SetValue("ProjectName"   , AppConfigs.GetValue("ProjectName"));
            _AutoTemplate.SetValue("ProjectPath"   , AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("DevelopPath"   , AppConfigs.DevelopPath);
            _AutoTemplate.SetValue("CommandEntity" , sCommandEntity);

            _AutoTemplate.Close();
            if (_FAILURE.Count > 0)
            {
                this.WriteLine("\n*** _FAILURE List *** ");
                foreach (var c in _FAILURE)
                {
                    this.WriteLine(c);
                }
            }
        }

        private void Prettify(string sTarget, string sUID)
        {
            CommandEntity sCommand = new CommandEntity();

            JSONArray aPrettify = AppConfigs.JSONArray("Prettify");
            foreach (JSONObject Prettify in aPrettify.JSONObjects)
            {
                if (!string.IsNullOrEmpty(Prettify.GetValue("sCommand")))
                {
                    string Argument = Utils.Util.ReplaceWith(Prettify.GetValue("Argument"), "{sUID}", sUID);
                    Argument = Utils.Util.ReplaceWith(Argument, "${sTarget}", UtilSeparator.Separator(sTarget));

                    sCommand.sDirectory = UtilSeparator.Separator(UtilSeparator.Separator(sTarget) + sUID);
                    sCommand.sCommand   = Prettify.GetValue("sCommand");
                    sCommand.sArguments = UtilSeparator.Separator(Argument);
                    
                    if (this.DebugMode == "Y") {
                        this.WriteLine(UtilSeparator.Separator(Argument));
                    }
                    sCommand = _ShellRunner.Execute(sCommand);

                    WriteLine(sCommand.Message);
                }
            }
        }

        private void Replication(string source,string sUID)
        {

            JSONArray aReplication = AppConfigs.JSONArray("Replications");

            string sDir=UtilSeparator.Separator(source + sUID);

            if (!Directory.Exists(sDir))
            {
                sDir=UtilSeparator.Separator(source);
            }

            if (aReplication.Count>0 && Directory.Exists(sDir))
            {
                DirectoryInfo _DirInfo = new DirectoryInfo(sDir);

                FileSystemInfo[] objFiles = _DirInfo.GetFileSystemInfos("*.*");

                foreach (string _Replication in aReplication.Names)
                {
                    string tReplication=_Replication;
                    string tRep=UtilSeparator.Separator(_Replication + @"\" + sUID );;
                    if (!Directory.Exists(tRep))
                    {
                        tRep=UtilSeparator.Separator(_Replication);;
                    }

                    if (Directory.Exists(tRep))
                    {

                        for (int i = 0; i < objFiles.Length; i++) {
                            FileInfo _FileInfo = objFiles[i] as FileInfo;

                            if (_FileInfo != null) {

                                if (Path.GetExtension(_FileInfo.FullName)==".java" || Path.GetExtension(_FileInfo.FullName)==".cs" || Path.GetExtension(_FileInfo.FullName)==".csproj" ){

                                    tReplication = UtilSeparator.Separator(tRep+ @"\" +Path.GetFileName(_FileInfo.FullName));

                                    if (File.Exists(tReplication)){
                                        if (this.DebugMode == "Y") {
                                            this.Write(UtilSeparator.Separator(_FileInfo.FullName)+"--->");
                                            this.WriteLine(UtilSeparator.Separator(tRep));
                                        }
                                        File.Copy(_FileInfo.FullName, tReplication, true);
                                    }
                                }
                            }
                        }
                    }else{
                        if (this.DebugMode == "Y") {
                            this.WriteLine("Dir "+tRep+" Ignore");
                        }
                    }
                }
                
            }else{
                if (this.DebugMode == "Y") {
                    this.WriteLine("Replication=none");
                    this.WriteLine(sDir+"Exists="+Directory.Exists(sDir));
                }
            }
        }

        private void Build(string sUID)
        {
            CommandEntity sCommand = new CommandEntity();

            JSONObject _Compiler = AppConfigs.JSONObject("Compiler");
            string sCompiler     = _Compiler.GetValue("Compiler");
            string sArguments    = _Compiler.GetValue("Arguments");

            if (sCompiler.ToLower()=="ignore" || string.IsNullOrEmpty(sCompiler))
            {
                return;
            }

            sArguments = Utils.Util.ReplaceWith(sArguments, "{sUID}", sUID);

            sCommand.sDirectory = UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src\" + sUID);
            sCommand.sCommand   = sCompiler;
            sCommand.sArguments = sArguments;

            if (this.DebugMode == "Y") {
                Console.WriteLine("sDirectory : "+sCommand.sDirectory);
                Console.WriteLine("sCommand   : "+sCommand.sCommand);
                Console.WriteLine("sArguments : "+sCommand.sArguments);
            }

            sCommand = _ShellRunner.Execute(sCommand);
            sCommandEntity.Add(sCommand);

            WriteLine(sCommand.Message);

            if (sCommand.SUCCESS)
            {
                if (Directory.Exists(sCommand.sDirectory))
                {
                    this.WriteLine("Search File "+sUID + ".dll In ");
                    this.WriteLine("   " + sCommand.sDirectory + @"\bin");
                    string fileNameDll = UtilSeparator.SearchFile(sCommand.sDirectory + @"\bin", sUID + ".dll");
                    if (string.IsNullOrEmpty(fileNameDll))
                    {
                        this.WriteLine("   " + sCommand.sDirectory + @"\obj");
                        fileNameDll = UtilSeparator.SearchFile(sCommand.sDirectory + @"\obj", sUID + ".dll");
                    }
                    if (string.IsNullOrEmpty(fileNameDll))
                    {
                        this.WriteLine("fileName " + sUID + ".dll Not Found!! ");
                    }
                    else
                    {
                        this.WriteLine("");
                        this.WriteLine("Copy file to");

                        string sPath = UtilSeparator.Separator(AppConfigs.ProjectPath + @"\apps\addons\");

                        this.WriteLine("   " + sPath + "*");
                        this.WriteLine("       1. " + sUID + ".dll");
                        File.Copy(fileNameDll, sPath + sUID + ".dll", true);

                        string fileName = fileNameDll.Replace(".dll", ".pdb");
                        if (File.Exists(fileName))
                        {
                            this.WriteLine("       2. "+ sUID + ".pdb");
                            File.Copy(fileName, sPath + sUID + ".pdb", true);
                        }
                        else
                        {
                            this.WriteLine("   " + fileName + " Not Found!!");
                        }
                        fileName = fileNameDll.Replace(".dll", ".deps.json");
                        if (File.Exists(fileName))
                        {
                            this.WriteLine("       3. "+ sUID + ".dept.json");
                            File.Copy(fileName, sPath + sUID + ".dept.json", true);
                        }
                        else
                        {
                            this.WriteLine("   " + fileName + " Not Found!!");
                        }
                    }
                }
            }
            else
            {
                _FAILURE.Add(sUID);
            }
        }

        private void PrepareDir()
        {
            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.ProjectPath + @"\apps\addons"));
            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src"));
            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src\entity"));

            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.DevelopPath + @"\definition"));
            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.DevelopPath + @"\definition\entity"));

        }

        private void GeneratorActivity(string sUID)
        {

            this.PrepareDir();

            string _TableName = "";
            string sPrimaryKey="";
            string _ColumnName;
            string _DataType;

            JSONObject _JSONFunction = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation + sUID + ".json");

            JSONObject Ignore   = AppConfigs.JSONObject("Table");
            string IgnoreTables = Ignore.GetValue("Tables");
            sTablePrefix = Ignore.GetValue("Prefix");

            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode    = this.DebugMode;
            _AutoTemplate.AppConfigs   = AppConfigs;
            _AutoTemplate.sUID         = sUID;
            _AutoTemplate.Initialize();
            _AutoTemplate.SetValue("sUID"            , sUID);
            _AutoTemplate.SetValue("ProjectName"     , AppConfigs.GetValue("ProjectName"));
            _AutoTemplate.SetValue("ProjectPath"     , AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("DevelopPath"     , AppConfigs.DevelopPath);
            _AutoTemplate.SetValue("RefCode"         , "QD" + sUID.ToUpper());
            _AutoTemplate.SetValue("sSqlCode"        , "Q" + sUID.ToUpper());

            foreach (string sName in _JSONFunction.Names)
            {
                if (!_JSONFunction.IsJSONArray(sName) && !_JSONFunction.IsJSONObject(sName) && _JSONFunction.ContainsKey(sName))
                {
                    _AutoTemplate.SetValue(sName , _JSONFunction.GetValue(sName));
                }
            }

            List<COLUMNEntity> Entitys = new List<COLUMNEntity>();

            string _COLUMNS_NAME = "";
            string _COLUMNS_NAMEDateTime = "";

            foreach (JSONObject _NameValue in _JSONFunction.GetJSONArray("entitys").JSONObjects)
            {

                _TableName  = _NameValue.GetValue("sTableName");
                _ColumnName = _NameValue.GetValue("sColumnName");
                _DataType   = _NameValue.GetValue("sDataType");

                COLUMNEntity _COLUMNEntity   = new COLUMNEntity();
                _COLUMNEntity.sTableName     = _TableName;
                _COLUMNEntity.sColumnName    = _ColumnName;
                _COLUMNEntity.sDescriptionId = _NameValue.GetValue("sDescriptionId");
                _COLUMNEntity.bNullable      = _NameValue.GetBoolean("bNullable");
                _COLUMNEntity.sEnableMode    = _NameValue.GetValue("sEnableMode");
                _COLUMNEntity.Options        = _NameValue.GetValue("Options");
                _COLUMNEntity.bWriteable     = _NameValue.GetBoolean("Writeable");
                _COLUMNEntity.sComment       = _NameValue.GetValue("sComment");
                _COLUMNEntity.sRefBrowse     = _NameValue.GetValue("sRefBrowse");
                _COLUMNEntity.sRefCheck      = _NameValue.GetValue("sRefCheck");
                _COLUMNEntity.sRefViewer     = _NameValue.GetValue("sRefViewer");
                _COLUMNEntity.sDataType      = _NameValue.GetValue("sDataType");
                _COLUMNEntity.sRefCheck      = _NameValue.GetValue("sRefCheck");
                _COLUMNEntity.sRefBrowse     = _NameValue.GetValue("sRefBrowse");
                _COLUMNEntity.sRefBrowseType = _NameValue.GetValue("sType");
                _COLUMNEntity.sRefViewer     = _NameValue.GetValue("sRefViewer");
                _COLUMNEntity.bPrimaryKey    = _NameValue.GetBoolean("bPrimaryKey");
                _COLUMNEntity.bAutoIdentity  = _NameValue.GetBoolean("bAutoIdentity");
                _COLUMNEntity.nMaxLength     = _NameValue.GetInteger("nMaxLength");

                if (string.IsNullOrEmpty(_COLUMNEntity.sComment)){
                    _COLUMNEntity.sComment = _ColumnName;
                }
                if (_COLUMNEntity.bPrimaryKey){
                    sPrimaryKey=_COLUMNEntity.sColumnName;
                }
                if (_TableName.ToLower() != "variable" && (_DataType == "nvarchar" || _DataType == "ntext"))
                {
                    if (_ColumnName != "sOriginal")
                    {
                        _COLUMNS_NAME = _COLUMNS_NAME + ";" + _TableName.ToLower() + "." + _ColumnName;
                    }
                }

                if (_TableName.ToLower() != "variable" && _DataType == "datetime")
                {
                    _COLUMNS_NAMEDateTime = _COLUMNS_NAMEDateTime + ";" + _TableName.ToLower() + "." + _ColumnName;
                }

                _AutoTemplate.AddColumn(_ColumnName);

                Entitys.Add(_COLUMNEntity);

            }

            _AutoTemplate.SetValue("Entitys"       , Entitys);
            _AutoTemplate.SetValue("sPrimaryKey"   , sPrimaryKey);

            _AutoTemplate.SetValue("sTablePrefix"  , sTablePrefix);
            _AutoTemplate.SetValue("COLUMNS_NAME"  , _COLUMNS_NAME);
            _AutoTemplate.SetValue("COLUMNS_NAMED" , _COLUMNS_NAMEDateTime);

            string newhash        = "";
            string _App_Path      = UtilSeparator.Separator(AppConfigs.DevelopPath + "\\");
            string _Path          = UtilSeparator.Separator(AppConfigs.ProjectPath + "\\src\\");
            string _Replications  = UtilSeparator.Separator(AppConfigs.Replications);
            string[] aReplication = _Replications.Split(';');
            if (string.IsNullOrEmpty(sTemplate)){
                sTemplate="N";
            }

            foreach (JSONObject Template in AppConfigs.JSONArray("activity").JSONObjects)
            {

                string cName  = Template.GetValue("template");
                string cValue = Template.GetValue("target");

                cName = cName.Trim();
                cValue = cValue.Trim();

                cName  = cName.Replace("{sUID}"         , sUID);
                cName  = cName.Replace("{UID_TP_CODE}"  , sTemplate);
                cValue = cValue.Replace("{sUID}"        , sUID);
                cValue = cValue.Replace("{ProjectPath}" , AppConfigs.ProjectPath);
                cValue = cValue.Replace("{DevelopPath}" , AppConfigs.DevelopPath);
                cValue = cValue.Replace("{sTableName}"  , Utils.Util.ToCamelCase(UtilSeparator.TrimStart(_JSONFunction.GetValue("sTableName") , sTablePrefix)));
                cValue = cValue.Replace("{UID_TP_CODE}" , sTemplate);

                if (cName == "Path")
                {
                    _Path = UtilSeparator.Separator(cValue);
                }
                else
                {

                    Utils.Util.CreateDir(UtilSeparator.Separator(_Path + sUID));
                    
                    _AutoTemplate.DebugMode  = this.DebugMode;
                    _AutoTemplate.Template   = UtilSeparator.Separator(cName);
                    _AutoTemplate.OutputFile = UtilSeparator.Separator(_Path + sUID + @"\" + cValue);

                    if (Path.GetExtension(_AutoTemplate.OutputFile) == ".csproj")
                    {
                        _AutoTemplate.DebugMode = "N";
                    }else{
                        _AutoTemplate.DebugMode = this.DebugMode;
                    }

                    _AutoTemplate.Process();

                }


            }

            _AutoTemplate.Close();

        }

        public void GeneratorEntity()
        {

            this.PrepareDir();

            List<string> sTableNames = new List<string>();

            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode    = this.DebugMode;
            _AutoTemplate.AppConfigs   = this.AppConfigs;

            _AutoTemplate.SetValue("ProjectPath" , AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("ProjectName" , AppConfigs.GetValue("ProjectName"));

            JSONObject Ignore   = AppConfigs.JSONObject("Table");
            string IgnoreTables = Ignore.GetValue("Tables");
            sTablePrefix = Ignore.GetValue("Prefix");

            string localDirectory = UtilSeparator.Separator(AppConfigs.DevelopPath + @"\definition\entity\");

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

                            if (sTablePrefix=="" || sTableName.StartsWith(sTablePrefix))
                            {

                                ColumnEntity = new List<COLUMNEntity>();
                                string sPrimaryKey="";
                                if (_JSONTableNames.GetType(_s) == "l")
                                {

                                    JSONArray _JSONObjects = _JSONTableNames.GetJSONArray(_s);

                                    foreach (JSONObject _JSONObject in _JSONObjects.JSONObjects)
                                    {

                                        COLUMNEntity _COLUMNEntity  = new COLUMNEntity();
                                        _COLUMNEntity.sTableName    = sTableName;
                                        _COLUMNEntity.sColumnName   = _JSONObject.GetValue("sColumnName");
                                        _COLUMNEntity.sDataType     = _JSONObject.GetValue("sDataType");
                                        _COLUMNEntity.sComment      = _JSONObject.GetValue("sComment");
                                        _COLUMNEntity.bPrimaryKey   = _JSONObject.GetBoolean("bPrimaryKey");
                                        _COLUMNEntity.bAutoIdentity = _JSONObject.GetBoolean("bAutoIdentity");

                                        ColumnEntity.Add(_COLUMNEntity);
                                        if (_COLUMNEntity.bPrimaryKey){
                                            sPrimaryKey=_COLUMNEntity.sColumnName;
                                        }
                                    }
                                }

                                _AutoTemplate.SetValue("Entitys"      , ColumnEntity);
                                _AutoTemplate.SetValue("sTableName"   , sTableName);
                                _AutoTemplate.SetValue("sPrimaryKey"  , sPrimaryKey);
                                _AutoTemplate.SetValue("sTablePrefix" , sTablePrefix);

                                foreach (JSONObject Template in AppConfigs.JSONArray("entity").JSONObjects)
                                {

                                    string sUID      = "entity";
                                    string _App_Path = UtilSeparator.Separator(AppConfigs.DevelopPath + "\\");
                                    string _Path     = UtilSeparator.Separator(AppConfigs.ProjectPath + "\\src\\");

                                    string cName  = Template.GetValue("template");
                                    string cValue = Template.GetValue("target");

                                    cName = cName.Trim();
                                    cValue = cValue.Trim();
                                    cName  = cName.Replace("{sUID}", "entity");
                                    cValue = cValue.Replace("{sUID}", "entity");
                                    cValue = cValue.Replace("{ProjectPath}", AppConfigs.ProjectPath);
                                    cValue = cValue.Replace("{DevelopPath}", AppConfigs.DevelopPath);
                                    cValue = cValue.Replace("{sTableName}", Utils.Util.ToCamelCase(UtilSeparator.TrimStart(sTableName,sTablePrefix)));
                                
                                    _AutoTemplate.Template = UtilSeparator.Separator(cName);
                                    if (cValue.IndexOf("/")>=0){
                                        _AutoTemplate.OutputFile = UtilSeparator.Separator(cValue);
                                    }else{
                                        _AutoTemplate.OutputFile = UtilSeparator.Separator(_Path + sUID + @"\" + cValue);
                                    }
                                    _AutoTemplate.Process();
                                            
                                }

                                sTableNames.Add(sTableName);

                                this.WriteLine("[" + sTableName + "]");
                            }
                        }
                    }
                }

                _AutoTemplate.SetValue("sTableNames" , sTableNames);

                _AutoTemplate.Template   = "N_Entity_Build_Template.csproj";
                _AutoTemplate.OutputFile = UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src\entity\entity.csproj");
                _AutoTemplate.Process();

                Prettify(AppConfigs.ProjectPath + @"\src\","entity");
                Prettify(@"\java\","entity");

                JSONObject _Compiler = AppConfigs.JSONObject("Compiler");
                string sCompiler     = _Compiler.GetValue("Compiler");
                string sArguments    = _Compiler.GetValue("Arguments");

                if (sCompiler.ToLower()=="ignore" || string.IsNullOrEmpty(sCompiler)){
                    return;
                }else{

                    sArguments = Utils.Util.ReplaceWith(sArguments , "{sUID}" , UtilSeparator.Separator(@"entity"));
                    if (this.DebugMode == "Y") {
                        Console.WriteLine("Arguments="+sArguments);
                    }
                    CommandEntity sCommand = new CommandEntity();
                    sCommand.sDirectory    = UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src\entity");
                    sCommand.sCommand      = sCompiler;
                    sCommand.sArguments    = sArguments;

                    sCommand = _ShellRunner.Execute(sCommand);
                    sCommandEntity.Add(sCommand);

                    WriteLine(sCommand.Message);

                    if (sCommand.SUCCESS)
                    {
                        //WriteLine("SUCCESS.");
                    }
                }
                Replication(AppConfigs.ProjectPath + @"\src\","entity");
                Replication(@"\java\","entity");

            }else{
                this.WriteLine(localDirectory+" Not Found!");
            }
            _AutoTemplate.Close();
        }
    }
}
