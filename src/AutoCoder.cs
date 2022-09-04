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
                    string fileName=Path.GetFileNameWithoutExtension(i.FullName);

                    if (fileName.Contains(sUID))
                    {
                        this.WriteLine("");
                        this.Write(fileName);

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
            _AutoTemplate.SetValue("ProjectName", AppConfigs.GetValue("ProjectName"));
            _AutoTemplate.SetValue("AppPath", AppConfigs.GetValue("AppPath"));
            _AutoTemplate.SetValue("ProjectPath", AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("DevelopPath", AppConfigs.DevelopPath);
            _AutoTemplate.SetValue("CommandEntity", sCommandEntity);

            _AutoTemplate.Template = "Build_Result.shtml";
            _AutoTemplate.OutputFile = UtilSeparator.Separator(AppConfigs.GetValue("AppPath") + "\\temp\\Build_Result.html");
            _AutoTemplate.Process();
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

                    Console.WriteLine(UtilSeparator.Separator(Argument));
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
                                        Console.Write(_FileInfo.FullName+"--->");
                                        Console.WriteLine(tRep);
                                        File.Copy(_FileInfo.FullName, tReplication, true);
                                    }
                                }
                            }
                        }
                    }else{
                        Console.WriteLine("Dir "+tRep+" Ignore");
                    }
                }
                
            }else{
                Console.WriteLine("Replication=none");
                Console.WriteLine(sDir+"Exists="+Directory.Exists(sDir));
            }
        }

        private string Build(string sUID)
        {
            CommandEntity sCommand = new CommandEntity();

            if (AppConfigs.GetValue("Compiler").ToLower()=="ignore"){
                return "";
            }

            string sArguments = AppConfigs.GetValue("Arguments");

            sArguments = Utils.Util.ReplaceWith(sArguments, "{sUID}", sUID);

            sCommand.sDirectory = UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src\" + sUID);
            sCommand.sCommand   = AppConfigs.GetValue("Compiler");
            sCommand.sArguments = sArguments;

            sCommand = _ShellRunner.Execute(sCommand);
            sCommandEntity.Add(sCommand);

            WriteLine(sCommand.Message);

            if (sCommand.SUCCESS)
            {
                if (Directory.Exists(sCommand.sDirectory))
                {
                    Console.WriteLine("Search In " + sCommand.sDirectory + @"\bin");
                    string fileNameDll = SearchFile(sCommand.sDirectory + @"\bin", sUID + ".dll");
                    if (string.IsNullOrEmpty(fileNameDll))
                    {
                        Console.WriteLine("Search In " + sCommand.sDirectory + @"\obj");
                        fileNameDll = SearchFile(sCommand.sDirectory + @"\obj", sUID + ".dll");
                    }
                    if (string.IsNullOrEmpty(fileNameDll))
                    {
                        Console.WriteLine("fileName " + sUID + ".dll Not Found!! ");
                    }
                    else
                    {
                        Console.WriteLine("fileName " + fileNameDll);
                        Console.WriteLine("Copy file to");
                        string sPath = AppConfigs.ProjectPath + @"\apps\addons\";

                        Console.WriteLine("   " + fileNameDll + " ==> " + sPath + sUID + ".dll");
                        File.Copy(fileNameDll, sPath + sUID + ".dll", true);

                        string fileName = fileNameDll.Replace(".dll", ".pdb");
                        if (File.Exists(fileName))
                        {
                            Console.WriteLine("   " + fileName + " ==> " + sPath + sUID + ".pdb");
                            File.Copy(fileName, sPath + sUID + ".pdb", true);
                        }
                        else
                        {
                            Console.WriteLine("   " + fileName + " Not Found!!");
                        }
                        fileName = fileNameDll.Replace(".dll", ".deps.json");
                        if (File.Exists(fileName))
                        {
                            this.WriteLine("   " + fileName + " ==> " + sPath + sUID + ".dept.json");
                            File.Copy(fileName, sPath + sUID + ".dept.json", true);
                        }
                        else
                        {
                            Console.WriteLine("   " + fileName + " Not Found!!");
                        }
                    }
                }
            }
            else
            {
                _FAILURE.Add(sUID);
            }

            return "";
        }

        public string SearchFile(string sPath,string fileName)
        {
            try
            {
                if (!Directory.Exists(sPath))
                {
                    return "";
                }

                DirectoryInfo dir = new DirectoryInfo(sPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //获取目录下（不包含子目录）的文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)     //判断是否文件夹
                    {
                        string t = SearchFile(i.FullName, fileName);    //递归调用复制子文件夹
                        if (t != "")
                        {
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
                Console.WriteLine(sPath);
                Console.WriteLine(e.ToString());
                throw;
            }
            return "";
        }

        private void GeneratorActivity(string sUID)
        {

            this.WriteLine(UtilSeparator.Separator(AppConfigs.ProjectPath + @"\apps\addons"));

            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.ProjectPath + @"\apps\addons"));

            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src"));

            string _TableName="";
            string _ColumnName;
            string _DataType;

            JSONObject _JSONFunction = AppConfigs.LoadJSONObject(AppConfigs.AddonLocation + sUID + ".json");

            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode = this.DebugMode;
            _AutoTemplate.AppConfigs = AppConfigs;
            _AutoTemplate.sUID = sUID;
            _AutoTemplate.Initialize();
            _AutoTemplate.SetValue("sUID", sUID);
            _AutoTemplate.SetValue("DistPath", AppConfigs.GetValue("DistPath"));
            _AutoTemplate.SetValue("AppPath", AppConfigs.GetValue("AppPath"));
            _AutoTemplate.SetValue("ProjectName", AppConfigs.GetValue("ProjectName"));
            _AutoTemplate.SetValue("ProjectPath", AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("DevelopPath", AppConfigs.DevelopPath);
            _AutoTemplate.SetValue("RefCode", "QD" + sUID.ToUpper());
            _AutoTemplate.SetValue("sSqlCode", "Q" + sUID.ToUpper());
            _AutoTemplate.SetValue("sTableName", _JSONFunction.GetValue("sTableName"));
            _AutoTemplate.SetValue("sCode"     , _JSONFunction.GetValue("sCode"));
            _AutoTemplate.SetValue("PK_ColumnName", _JSONFunction.GetValue("PK_ColumnName"));
            _AutoTemplate.SetValue("LNK_TableName", _JSONFunction.GetValue("LNK_TableName"));
            _AutoTemplate.SetValue("sLNKUID", _JSONFunction.GetValue("sLNKUID"));
            _AutoTemplate.SetValue("sType", _JSONFunction.GetValue("sType"));
            _AutoTemplate.SetValue("ROOT_LNKUID", _JSONFunction.GetValue("ROOT_LNKUID"));
            _AutoTemplate.SetValue("ROOT_TableName", _JSONFunction.GetValue("ROOT_TableName"));
            _AutoTemplate.SetValue("ROOT_ColumnName", _JSONFunction.GetValue("ROOT_ColumnName"));

            List<COLUMNEntity> Entitys = new List<COLUMNEntity>();

            string _COLUMNS_NAME = "";
            string _COLUMNS_NAMEDateTime = "";

            foreach (JSONObject _NameValue in _JSONFunction.GetJSONArray("entitys").JSONObjects)
            {

                _TableName = _NameValue.GetValue("sTableName");
                _ColumnName = _NameValue.GetValue("sColumnName");
                _DataType = _NameValue.GetValue("sDataType");

                COLUMNEntity _COLUMNEntity = new COLUMNEntity();
                _COLUMNEntity.sTableName = _TableName;
                _COLUMNEntity.sColumnName = _ColumnName;
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
                if (string.IsNullOrEmpty(_COLUMNEntity.sComment)){
                    _COLUMNEntity.sComment=_ColumnName;
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

            JSONObject _JSONObject = AppConfigs.LoadSetting("nScale.json");

            foreach (string sName in _JSONObject.Names)
            {
                if (_JSONObject.ContainsKey(sName))
                {
                    _AutoTemplate.SetValue(sName + "_Scale", _JSONObject.GetInteger(sName));
                }
            }

            _AutoTemplate.SetValue("Entitys", Entitys);
            _AutoTemplate.SetValue("sTablePrefix", AppConfigs.GetValue("sTablePrefix"));
            _AutoTemplate.SetValue("COLUMNS_NAME", _COLUMNS_NAME);
            _AutoTemplate.SetValue("COLUMNS_NAMED", _COLUMNS_NAMEDateTime);

            string newhash   = "";
            string _App_Path = UtilSeparator.Separator(AppConfigs.DevelopPath + "\\");
            string _Path        = UtilSeparator.Separator(AppConfigs.ProjectPath + "\\src\\");
            string _Replications = UtilSeparator.Separator(AppConfigs.Replications);
            string[] aReplication = _Replications.Split(';');
            if (string.IsNullOrEmpty(sTemplate)){
                sTemplate="N";
            }
            string tTemplate=_App_Path + sTemplate + ".tpl";
            if (File.Exists(tTemplate))
            {
                using (StreamReader sr = new StreamReader(tTemplate))
                {
                    string s;
                    StringBuilder XCodeObject = new StringBuilder();

                    while ((s = sr.ReadLine()) != null)
                    {
                        int _p = s.IndexOf("=");
                        Console.WriteLine(s);

                        if (_p > 0)
                        {
                            string cName = s.Substring(0, _p);
                            string cValue = s.Substring(_p + 1, s.Length - _p - 1);

                            cName = cName.Trim();
                            cValue = cValue.Trim();

                            cName = cName.Replace("{sUID}", sUID);
                            cName = cName.Replace("{UID_TP_CODE}", sTemplate);
                            cValue = cValue.Replace("{sUID}", sUID);
                            cValue = cValue.Replace("{AppPath}", AppConfigs.GetValue("AppPath"));
                            cValue = cValue.Replace("{ProjectPath}", AppConfigs.ProjectPath);
                            cValue = cValue.Replace("{DevelopPath}", AppConfigs.DevelopPath);
                            cValue = cValue.Replace("{sTableName}", Utils.Util.ToCamelCase(UtilSeparator.TrimStart(_JSONFunction.GetValue("sTableName"), AppConfigs.GetValue("sTablePrefix"))));
                            cValue = cValue.Replace("{UID_TP_CODE}", sTemplate);

                            if (cName == "Path")
                            {
                                _Path = UtilSeparator.Separator(cValue);
                            }
                            else
                            {

                                Utils.Util.CreateDir(UtilSeparator.Separator(_Path + sUID));
                                
                                _AutoTemplate.Template = UtilSeparator.Separator(cName);
                                _AutoTemplate.OutputFile = UtilSeparator.Separator(_Path + sUID + @"\" + cValue);
                                _AutoTemplate.Process();

                            }

                        }
                    }
                }
            }else{
                Console.WriteLine(tTemplate+" Not Found!");
            }
            _AutoTemplate.Close();

        }

        public void GeneratorEntity()
        {

            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.DevelopPath + "\\definition"));
            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.DevelopPath + "\\definition\\entity"));
            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.ProjectPath + "\\src"));
            Utils.Util.CreateDir(UtilSeparator.Separator(AppConfigs.ProjectPath + "\\src\\entity"));

            List<string> sTableNames = new List<string>();

            AutoTemplate _AutoTemplate = new AutoTemplate();
            _AutoTemplate.DebugMode    = this.DebugMode;
            _AutoTemplate.AppConfigs   = this.AppConfigs;

            _AutoTemplate.SetValue("AppPath"     , AppConfigs.GetValue("AppPath"));
            _AutoTemplate.SetValue("DistPath"    , AppConfigs.GetValue("DistPath"));
            _AutoTemplate.SetValue("ProjectPath" , AppConfigs.ProjectPath);
            _AutoTemplate.SetValue("ProjectName" , AppConfigs.GetValue("ProjectName"));

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

                            if (AppConfigs.GetValue("sTablePrefix")=="" || sTableName.StartsWith(AppConfigs.GetValue("sTablePrefix")))
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

                                _AutoTemplate.SetValue("Entitys"    , ColumnEntity);
                                _AutoTemplate.SetValue("sTableName" , sTableName);
                                _AutoTemplate.SetValue("sPrimaryKey" , sPrimaryKey);
                                _AutoTemplate.SetValue("sTablePrefix" , AppConfigs.GetValue("sTablePrefix"));

                                string entityTpl=UtilSeparator.Separator(AppConfigs.DevelopPath + "/"+sTemplate+".tpl");
                                if (File.Exists(entityTpl))
                                {
                                    string sUID  = "entity";
                                    string _App_Path = UtilSeparator.Separator(AppConfigs.DevelopPath + "\\");
                                    string _Path = UtilSeparator.Separator(AppConfigs.ProjectPath + "\\src\\");

                                    using (StreamReader sr = new StreamReader(entityTpl))
                                    {
                                        string s;
                                        StringBuilder XCodeObject = new StringBuilder();

                                        while ((s = sr.ReadLine()) != null)
                                        {
                                            int _p = s.IndexOf("=");

                                            if (_p > 0)
                                            {
                                                string cName = s.Substring(0, _p);
                                                string cValue = s.Substring(_p + 1, s.Length - _p - 1);

                                                cName = cName.Trim();
                                                cValue = cValue.Trim();

                                                cName = cName.Replace("{sUID}", "entity");
                                                cValue = cValue.Replace("{sUID}", "entity");
                                                cValue = cValue.Replace("{AppPath}", AppConfigs.GetValue("AppPath"));
                                                cValue = cValue.Replace("{ProjectPath}", AppConfigs.ProjectPath);
                                                cValue = cValue.Replace("{DevelopPath}", AppConfigs.DevelopPath);
                                                cValue = cValue.Replace("{sTableName}", Utils.Util.ToCamelCase(UtilSeparator.TrimStart(sTableName,AppConfigs.GetValue("sTablePrefix"))));
                                            
                                                if (cName == "Path")
                                                {
                                                    _Path = UtilSeparator.Separator(cValue);
                                                }
                                                else
                                                {

                                                    _AutoTemplate.Template = UtilSeparator.Separator(cName);
                                                    if (cValue.IndexOf("/")>=0){
                                                        _AutoTemplate.OutputFile = UtilSeparator.Separator(cValue);
                                                    }else{
                                                        _AutoTemplate.OutputFile = UtilSeparator.Separator(_Path + sUID + @"\" + cValue);
                                                    }
                                                    _AutoTemplate.Process();

                                                }

                                            }
                                        }
                                    }
                                }else{
                                    Console.WriteLine(entityTpl + "/entity.tpl Not Found!");

                                }

                                sTableNames.Add(sTableName);

                                this.WriteLine("[" + sTableName + "]");
                            }else{
                                this.WriteLine(AppConfigs.GetValue("sTablePrefix")+" Ignore [" + sTableName + "]");
                            }
                        }
                    }
                }

                _AutoTemplate.SetValue("sTableNames" , sTableNames);

                _AutoTemplate.Template   = "N_Entity_Build_Template.cs";
                _AutoTemplate.OutputFile = UtilSeparator.Separator(AppConfigs.ProjectPath  + @"\src\entity\Zero.Addons.entity.Build");
                _AutoTemplate.Process();

                _AutoTemplate.Template = "N_Entity_Build_Template.csproj";
                _AutoTemplate.OutputFile = UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src\entity\Zero.Addons.entity.csproj");
                _AutoTemplate.Process();

                Prettify(AppConfigs.ProjectPath + @"\src\","entity");
                Prettify(@"\java\","entity");

                string sArguments = AppConfigs.GetValue("Arguments");

                sArguments = Utils.Util.ReplaceWith(sArguments , "{sUID}" , UtilSeparator.Separator(@"entity"));

                CommandEntity sCommand = new CommandEntity();
                sCommand.sDirectory    = UtilSeparator.Separator(AppConfigs.ProjectPath + @"\src\entity");
                sCommand.sCommand      = AppConfigs.GetValue("Compiler");
                sCommand.sArguments    = sArguments;

                sCommand = _ShellRunner.Execute(sCommand);
                sCommandEntity.Add(sCommand);

                Replication(AppConfigs.ProjectPath + @"\src\","entity");
                Replication(@"\java\","entity");

            }else{
                Console.WriteLine(localDirectory+" Not Found!");
            }
            _AutoTemplate.Close();
        }
    }
}
