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
    public class AppConfigs {
        const string ZFILE_NAME       = "AppConfig";
        private string _AppPath       = "";
        private string _AppSecret     = "";
        private string _sAppPath      = "";
        private string _sDbName       = "";
        private string _sToken        = "";
        private string _FileName      = "";
        private string _ProjectName   = "";
        private string _Provider      = "MsSqlServer";
        private string _Target        = "MsSqlServer";
        private bool   _lower_name    = false;
        private bool   _lower_column  = false;
        private string _TemplatePath  = "";
        private string _Compiler      = "";
        private string _Packer        = "";
        private string _DebugMode     = "N";
        private string _Separator     = "/";

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

            _AppPath   = AppDomain.CurrentDomain.BaseDirectory;
            _Separator = Path.DirectorySeparatorChar.ToString();
            _AppPath   = _AppPath.Replace("/", _Separator);
            _AppPath   = _AppPath.Replace("\\", _Separator);
            _AppPath   = _AppPath.TrimEnd(_Separator.ToCharArray())+_Separator;
          
            if (fileName==""){

                _FileName = "term.config";
            }else{
                _FileName = fileName;
            }

            if (!File.Exists(_FileName)) {
                if (File.Exists(_AppPath + _FileName)) {
                    _FileName = _AppPath + _FileName;
                    ZZLogger.Debug(ZFILE_NAME,_FileName);
                }
            }

            if (File.Exists(_FileName)) {

                ZZLogger.Debug(ZFILE_NAME , "Load config file : [" + _FileName + "]");

                UTF8Encoding _UTF8Encoding = new UTF8Encoding(false, true);
                string sValue  = "";

                using(StreamReader sr = new StreamReader(_FileName, _UTF8Encoding)) {
                    sValue = sr.ReadToEnd();
                }
                
                string DirectoryName = System.IO.Path.GetFileNameWithoutExtension(System.IO.Directory.GetCurrentDirectory());
                string CurrentDirectory = System.IO.Directory.GetCurrentDirectory();
                CurrentDirectory=CurrentDirectory.Replace("\\","/");
                DirectoryName=DirectoryName.Replace("\\","/");

                sValue = Utils.Util.ReplaceWith(sValue , "%AppPath%" , _AppPath);
                sValue = Utils.Util.ReplaceWith(sValue , "%Sep%"     , _Separator);
                sValue = Utils.Util.ReplaceWith(sValue , "%DirectoryName%" , DirectoryName);
                sValue = Utils.Util.ReplaceWith(sValue , "%CurrentDirectory%" , CurrentDirectory);

                _JSONObject = new JSONObject(sValue);

            }

            if (string.IsNullOrEmpty(_AppPath)) {
                _AppPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            }
        }
        
        public JSONObject JSONObject(string name)
        {
            string vValue = _JSONObject.GetJSONObject(name).ToString();
            List<string> list = Utils.Util.Parameters(vValue);
            //Console.WriteLine("***********="+vValue);

            foreach(string v in list)
            {
                //Console.WriteLine("##########="+v);
                if (_JSONObject.ContainsKey(v)){
                    vValue = Utils.Util.ReplaceWith(vValue, "${"+v+"}", this.GetValue(v));
                }
            }
            //Console.WriteLine("***********="+vValue);

            return new JSONObject(vValue);
        }

        public bool GetBoolean(string name)
        {
            return Utils.Util.ToBoolean(this.GetValue(name));
        }

        public string GetValue(string name)
        {
            string vValue = _JSONObject.GetValue(name);
            List<string> list = Utils.Util.Parameters(vValue);
            
         //   Console.WriteLine("");
         //   Console.WriteLine("");
         //   Console.WriteLine("");
         //   Console.WriteLine("");
         //   Console.WriteLine("name   = "+name);
         //   Console.WriteLine("vValue = "+vValue);
            foreach(string v in list)
            {
                //Console.WriteLine("     "+v+"=>"+_JSONObject.GetValue(v));
                if (_JSONObject.ContainsKey(v)){
                    if (v==name){
                        vValue = Utils.Util.ReplaceWith(vValue, "${"+v+"}", _JSONObject.GetValue(v));
                    }else{
                        vValue = Utils.Util.ReplaceWith(vValue, "${"+v+"}", this.GetValue(v));
                    }
                }
            }
                //Console.WriteLine("vValue ="+vValue);

            return vValue;
        }

        public JSONObject JSONObjects
         {
            get { 
                return _JSONObject; 
            } 
         }

        public List<string> Names  
         {
            get { 
                return _JSONObject.Names; 
            } 
         }

        public JSONObject LoadJSONObject(string fileName){
            string s = Util.Separator(fileName);
            if (_JSONObjects.ContainsKey(s)) {
                return _JSONObjects[s];
            }

            JSONObject _Json = new JSONObject();

            if (File.Exists(s)) {

                UTF8Encoding _UTF8Encoding = new UTF8Encoding(false, true);
                string _s="";
                using(StreamReader sr = new StreamReader(s, _UTF8Encoding)) {
                    _s = sr.ReadToEnd();
                }

                _Json = new JSONObject(_s);
            }else{
                Console.WriteLine(s+" Not Found!");
            }
            _JSONObjects[s] = _Json;

            return _Json;
        }
        private JSONObject _JSONObject = new JSONObject();

        private Dictionary<string, JSONObject> _JSONObjects  = new Dictionary<string, JSONObject>();

        public string sToken   { get { return _sToken;   } set { _sToken   = value; }  }
        public string sDbName  { get { return _sDbName;  } set { _sDbName  = value; }  }
        public string sAppPath { get { return _sAppPath; } set { _sAppPath = value; }  }

        public string AppPath        { get { return _AppPath;        }  }
        public string AppSecret      { get { return _AppSecret;      }  }
        public string DebugMode      { get { return _DebugMode;      }  }
        public string TemplatePath   { get { return _TemplatePath;    }  }
        public string Compiler       { get { return _Compiler;       }  }
        public string Packer         { get { return _Packer;         }  }
        public bool   LowerName      { get { return _lower_name;     }  }
        public bool   LowerColumn    { get { return _lower_column;   }  }
        public string ProjectName    { get { return _ProjectName;    }  }
        public string Provider       { get { return _Provider;       }  }
        public string Target         { get { return _Target;         }  }

    }
}
