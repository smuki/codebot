using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Threading;

using Volte.Data.Dapper;
using Volte.Data.Json;
using Volte.Utils;

namespace Volte.Bot.Term
{
    public class AppConfigs
    {
        const string ZFILE_NAME       = "AppConfig";
        private string _AppPath       = "";
        private string _AppSecret     = "";
        private string _sDbName       = "";
        private string _sToken        = "";
        private string _FileName      = "";
        private string _ProjectName   = "";
        private string _Provider      = "MsSqlServer";
        private string _Target        = "MsSqlServer";
        private bool   _lower_name    = false;
        private bool   _lower_column  = false;
        private string _Compiler      = "";
        private string _Packer        = "";
        private string _DebugMode     = "N";
        private string _DirectoryName  = "";
        private string _CurrentDirectory  = "";
        private string _Separator     = "/";
        private UTF8Encoding _UTF8Encoding = new UTF8Encoding(false, true);

        public static DateTime GetFileDateTime(string fileName)
        {

            FileInfo fi = new FileInfo(fileName);
            return fi.LastWriteTime;
        }

        private AppConfigs()
        {

        }

        public AppConfigs(string fileName)
        {

            _AppPath = AppDomain.CurrentDomain.BaseDirectory;
            _Separator = Path.DirectorySeparatorChar.ToString();
            _AppPath = _AppPath.Replace("/", _Separator);
            _AppPath = _AppPath.Replace("\\", _Separator);
            _AppPath = _AppPath.TrimEnd(_Separator.ToCharArray()) + _Separator;

            if (fileName == "")
            {

                _FileName = "term.config";
            }
            else
            {
                _FileName = fileName;
            }

            if (!File.Exists(_FileName))
            {
                if (File.Exists(_AppPath + _FileName))
                {
                    _FileName = _AppPath + _FileName;
                    ZZLogger.Debug(ZFILE_NAME, _FileName);
                }
            }

            if (File.Exists(_FileName)) {

                ZZLogger.Debug(ZFILE_NAME , "Load config file : [" + _FileName + "]");

                string sValue  = "";

                using(StreamReader sr = new StreamReader(_FileName, _UTF8Encoding)) {
                    sValue = sr.ReadToEnd();
                }
                
                _DirectoryName = System.IO.Path.GetFileNameWithoutExtension(System.IO.Directory.GetCurrentDirectory());
                _CurrentDirectory = System.IO.Directory.GetCurrentDirectory();
                _CurrentDirectory=_CurrentDirectory.Replace("\\","/");
                _DirectoryName=_DirectoryName.Replace("\\","/");
                _AppPath=_AppPath.Replace("\\","/");

                sValue = Utils.Util.ReplaceWith(sValue , "%AppPath%" , _AppPath);
                sValue = Utils.Util.ReplaceWith(sValue , "%Sep%"     , _Separator);
                sValue = Utils.Util.ReplaceWith(sValue , "%DirectoryName%" , _DirectoryName);
                sValue = Utils.Util.ReplaceWith(sValue , "%CurrentDirectory%" , _CurrentDirectory);

                _JSONObject = new JSONObject(sValue);

            }

            if (string.IsNullOrEmpty(_AppPath)) {
                _AppPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            }
            this.Process();
        }

        public void Process()
        {
            string sValue = "";
            string sLanguage = _JSONObject.GetValue("Language");

            if (File.Exists(sLanguage))
            {
                Console.WriteLine("Language   : " +sLanguage);
                using(StreamReader sr = new StreamReader(sLanguage, _UTF8Encoding)) {
                    sValue = sr.ReadToEnd();
                }
                sValue = Utils.Util.ReplaceWith(sValue , "%AppPath%" , _AppPath);
                sValue = Utils.Util.ReplaceWith(sValue , "%Sep%"     , _Separator);
                sValue = Utils.Util.ReplaceWith(sValue , "%DirectoryName%" , _DirectoryName);
                sValue = Utils.Util.ReplaceWith(sValue , "%CurrentDirectory%" , _CurrentDirectory);
                    
                JSONObject _Language = new JSONObject(sValue);

                foreach (string sKey in _Language.Names)
                {
                    if (_Language.IsJSONObject(sKey))
                    {
                        _JSONObject.SetValue(sKey, _Language.GetJSONObject(sKey));
                    }else if (_Language.IsJSONArray(sKey))
                    {
                        _JSONObject.SetValue(sKey, _Language.GetJSONArray(sKey));
                    }
                    else
                    {
                        _JSONObject.SetValue(sKey, _Language.GetValue(sKey));
                    }
                }
            }
            foreach (JSONObject its in _JSONObject.GetJSONArray("DataTypeMapping").JSONObjects) {
                string DataType = its.GetValue("value");
                foreach(string DbType in its.GetValue("name").Split(',')){
                    if (!string.IsNullOrEmpty(DbType)){

                        _DataTypeMapping["DataType_"+DbType] = DataType;
                    }
                }
            }

            foreach (JSONObject its in _JSONObject.GetJSONArray("ToDataTypeMapping").JSONObjects) {
                string value = its.GetValue("value");
                foreach(string item in its.GetValue("name").Split(',')){
                    if (!string.IsNullOrEmpty(item)){
                        _DataTypeMapping["ToDataTypeMapping_"+item] = value;
                    }
                }
            }
            foreach (JSONObject its in _JSONObject.GetJSONArray("DataTypeDefault").JSONObjects) {
                string sDefault = its.GetValue("value");
                foreach(string DbType in its.GetValue("name").Split(',')){
                    if (!string.IsNullOrEmpty(DbType)){
                        _DataTypeMapping["DataTypeDefault_"+DbType] = sDefault;
                    }
                }
            }

            foreach (JSONObject its in _JSONObject.GetJSONArray("DbTypeMapping").JSONObjects) {
                string sDefault = its.GetValue("value");
                foreach(string DbType in its.GetValue("name").Split(',')){
                    if (!string.IsNullOrEmpty(DbType)){
                        _DataTypeMapping["DbTypeMapping_"+DbType] = sDefault;
                    }
                }
            }

            
        }

        public string Mapping(string sType, string name)
        {
            string sKey = sType+"_"+name;
            if (_DataTypeMapping.ContainsKey(sKey))
            {
                return _DataTypeMapping[sKey];
            }
            return name;
        }
        public JSONObject JSONObject(string name)
        {
            string vValue = _JSONObject.GetJSONObject(name).ToString();
            List<string> list = Utils.Util.Parameters(vValue);

            foreach (string v in list)
            {
                if (_JSONObject.ContainsKey(v))
                {
                    vValue = Utils.Util.ReplaceWith(vValue, "${" + v + "}", this.GetValue(v));
                }
            }
            return new JSONObject(vValue);
        }

        public JSONArray JSONArray(string name)
        {
            string vValue = _JSONObject.GetJSONArray(name).ToString();
            List<string> list = Utils.Util.Parameters(vValue);

            foreach (string v in list)
            {
                if (_JSONObject.ContainsKey(v))
                {
                    vValue = Utils.Util.ReplaceWith(vValue, "${" + v + "}", this.GetValue(v));
                }
            }
            return new JSONArray(vValue);
        }

        public bool GetBoolean(string name)
        {
            return Utils.Util.ToBoolean(this.GetValue(name));
        }

        public string GetValue(string name)
        {
            string vValue = _JSONObject.GetValue(name);
            List<string> list = Utils.Util.Parameters(vValue);

            foreach (string v in list)
            {
                if (_JSONObject.ContainsKey(v))
                {
                    if (v == name)
                    {
                        vValue = Utils.Util.ReplaceWith(vValue, "${" + v + "}", _JSONObject.GetValue(v));
                    }
                    else
                    {
                        vValue = Utils.Util.ReplaceWith(vValue, "${" + v + "}", this.GetValue(v));
                    }
                }
            }
            return vValue;
        }

        public JSONObject JSONObjects
         {
            get { 
                return _JSONObject; 
            } 
         }

        public string AppSetting
        {
            get
            {
                return this.DevelopPath+@"\appsettings\";
            }
        }
        public string AddonLocation
        {
            get
            {
                return this.DevelopPath + @"\definition\functions\";
            }
        }

        public string DevelopPath
        {
            get
            {
                return this.GetValue("DevelopPath");
            }
        }

        public string ProjectPath
        {
            get
            {
                return this.GetValue("ProjectPath");
            }
        }

        public string Replications
        {
            get
            {
                return this.GetValue("Replications");
            }
        }

        public string AppPath
        {
            get
            {
                return this.GetValue("AppPath");
            }
        }
        
        public List<string> Names
        {
            get
            {
                return _JSONObject.Names;
            }
        }
        
        public JSONObject LoadSetting(string fileName){
            return this.LoadJSONObject(this.AppSetting+fileName);
        }
        public JSONObject LoadJSONObject(string fileName){
            string s = UtilSeparator.Separator(fileName);
            if (_JSONObjects.ContainsKey(s)) {
                return _JSONObjects[s];
            }

            JSONObject _Json = new JSONObject();

            if (File.Exists(s))
            {

                UTF8Encoding _UTF8Encoding = new UTF8Encoding(false, true);
                string _s = "";
                using (StreamReader sr = new StreamReader(s, _UTF8Encoding))
                {
                    _s = sr.ReadToEnd();
                }

                _Json = new JSONObject(_s);
            }
            else
            {
                Console.WriteLine(s + " Not Found!");
            }
            _JSONObjects[s] = _Json;

            return _Json;
        }
        private JSONObject _JSONObject = new JSONObject();

        private Dictionary<string, string> _DataTypeMapping  = new Dictionary<string, string>();

        private Dictionary<string, JSONObject> _JSONObjects  = new Dictionary<string, JSONObject>();

        public string sToken   { get { return _sToken;   } set { _sToken   = value; }  }
        public string sDbName  { get { return _sDbName;  } set { _sDbName  = value; }  }

        public string AppSecret      { get { return _AppSecret;      }  }
        public string DebugMode      { get { return _DebugMode;      }  }
        public string Compiler       { get { return _Compiler;       }  }
        public string Packer         { get { return _Packer;         }  }
        public bool   LowerName      { get { return _lower_name;     }  }
        public bool   LowerColumn    { get { return _lower_column;   }  }
        public string ProjectName    { get { return _ProjectName;    }  }
        public string Provider       { get { return _Provider;       }  }
        public string Target         { get { return _Target;         }  }

    }
}
