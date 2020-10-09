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
    public class AppSettings {
        const string ZFILE_NAME       = "AppSettings";
        private string _dbAdapter     = "";
        private string _AppPath       = "";
        private string _AppSecret     = "";
        private string _sAppPath      = "";
        private string _sDbName       = "";
        private string _sToken        = "";
        private string _DistPath      = "";
        private string _FileName      = "";
        private string _ProjectName   = "";
        private string _ProjectPath   = "";
        private string _PackageUrl    = "";
        private string _Provider      = "MsSqlServer";
        private string _Target        = "MsSqlServer";
        private string _PathRoot      = "";
        private bool   _lower_name    = false;
        private bool   _lower_column  = false;
        private string _DevelopPath   = "";
        private string _CodePath      = "";
        private string _TemplatePath  = "";
        private string _WorkSpacePath = "";
        private string _Compiler      = "";
        private string _Packer        = "";
        private string _Arguments     = "";
        private string _BuildArguments = "";
        private string _BuildTools    = "";
        private string _DebugMode     = "N";

        public static DateTime GetFileDateTime(string fileName)
        {

            FileInfo fi = new FileInfo(fileName);
            return fi.LastWriteTime;
        }

        private AppSettings()
        {

        }

        public AppSettings(string fileName)
        {

            _AppPath  = AppDomain.CurrentDomain.BaseDirectory;
            string separator = Path.DirectorySeparatorChar.ToString();
            _AppPath = _AppPath.Replace("/", separator);
            _AppPath = _AppPath.TrimEnd(separator.ToCharArray())+separator;
          
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

                StreamReader sr = new StreamReader(_FileName);
                string s        = "";
                string cName    = "";
                string cValue   = "";

                while ((s = sr.ReadLine()) != null) {
                    if (s.IndexOf("=") > 0) {
                        int cPositionB = s.IndexOf("=");

                        cName  = s.Substring(0, cPositionB);
                        cValue = s.Substring(cPositionB + 1, s.Length - cPositionB - 1);
                        cValue = cValue.Trim();
                        cName  = cName.Trim();
                        cName  = cName.ToUpper();

                        cValue = Utils.Util.ReplaceWith(cValue , "%AppPath%"       , _AppPath);
                        cValue = Utils.Util.ReplaceWith(cValue , "%WorkSpacePath%" , _WorkSpacePath);
                        cValue = Utils.Util.ReplaceWith(cValue , "%PathRoot%"      , _PathRoot);
                        cValue = Utils.Util.ReplaceWith(cValue , "%Sep%"           , separator);

                        if (cName == "PROJECTPATH") {

                            _ProjectPath = cValue;
                            _ProjectPath = Utils.Util.ReplaceWith(_ProjectPath , "%ProjectPath%" , _ProjectPath);

                        }else if (cName == "WORKSPACEPATH") {

                            _WorkSpacePath = cValue;

                        }else if (cName == "DEVELOPPATH") {

                            _DevelopPath = cValue;
                            _DevelopPath = Utils.Util.ReplaceWith(_DevelopPath , "%ProjectPath%" , _ProjectPath);

                        }else if (cName == "CODEPATH") {

                            _CodePath = cValue;
                            _CodePath = Utils.Util.ReplaceWith(_CodePath , "%ProjectPath%" , _ProjectPath);
                            _CodePath = Utils.Util.ReplaceWith(_CodePath , "%DevelopPath%" , _DevelopPath);

                        }else if (cName == "TEMPLATEPATH") {

                            _TemplatePath = cValue;
                            _TemplatePath = Utils.Util.ReplaceWith(_TemplatePath , "%ProjectPath%" , _ProjectPath);
                            _TemplatePath = Utils.Util.ReplaceWith(_TemplatePath , "%DevelopPath%" , _DevelopPath);

                        } else if (cName == "APPPATH") {

                            _AppPath = cValue;
                            _AppPath = Utils.Util.ReplaceWith(_AppPath , "%ProjectPath%" , _ProjectPath);
                            _AppPath = Utils.Util.ReplaceWith(_AppPath , "%DevelopPath%" , _DevelopPath);

                            _PathRoot = System.IO.Path.GetPathRoot(_AppPath);

                        } else if (cName == "APPSECRET") {
                            _AppSecret = cValue.Trim();
                        } else if (cName == "DISTPATH") {

                            _DistPath = cValue;
                            _DistPath = Utils.Util.ReplaceWith(_DistPath , "%ProjectPath%" , _ProjectPath);
                            _DistPath = Utils.Util.ReplaceWith(_DistPath , "%DevelopPath%" , _DevelopPath);

                        } else if (cName == "DBADAPTER") {

                            _dbAdapter = cValue;

                        } else if (cName == "PROVIDER") {

                            _Provider = cValue;

                        } else if (cName == "PACKAGEURL") {

                            _PackageUrl = cValue;

                        } else if (cName == "TARGET") {

                            _Target = cValue;

                        } else if (cName == "COMPILER") {

                            _Compiler = cValue;

                        } else if (cName == "BUILDTOOLS") {

                            _BuildTools = cValue;
                        } else if (cName == "PACKER") {

                            _Packer = cValue;

                        } else if (cName == "LOWERNAME") {

                            if (cValue=="YES"){
                                _lower_name = true;
                            }else{
                                _lower_name = false;
                            }
                        } else if (cName == "LOWERCOLUMN") {
                            if (cValue=="YES"){
                                _lower_column = true;
                            }else{
                                _lower_column = false;
                            }

                        } else if (cName == "ARGUMENTS") {

                            _Arguments = cValue;
                            _Arguments = Utils.Util.ReplaceWith(_Arguments , "%ProjectPath%" , _ProjectPath);
                            _Arguments = Utils.Util.ReplaceWith(_Arguments , "%DevelopPath%" , _DevelopPath);
                        } else if (cName == "BUILDARGUMENTS") {

                            _BuildArguments = cValue;
                            _BuildArguments = Utils.Util.ReplaceWith(_BuildArguments, "%ProjectPath%", _ProjectPath);
                            _BuildArguments = Utils.Util.ReplaceWith(_BuildArguments, "%DevelopPath%", _DevelopPath);

                        } else if (cName == "PROJECTNAME") {

                            _ProjectName = cValue;

                        } else if (cName == "DEBUG") {

                            _DebugMode = cValue;
                        }
                    }

                }

                sr.Close();
            }

            if (string.IsNullOrEmpty(_AppPath)) {
                _AppPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
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

        private Dictionary<string, JSONObject> _JSONObjects  = new Dictionary<string, JSONObject>();

        public string sToken   { get { return _sToken;   } set { _sToken   = value; }  }
        public string sDbName  { get { return _sDbName;  } set { _sDbName  = value; }  }
        public string sAppPath { get { return _sAppPath; } set { _sAppPath = value; }  }

        public string AppPath        { get { return _AppPath;        }  }
        public string AppSecret      { get { return _AppSecret;      }  }
        public string DistPath       { get { return _DistPath;       }  }
        public string DebugMode      { get { return _DebugMode;      }  }
        public string ProjectPath    { get { return _ProjectPath;    }  }
        public string DevelopPath    { get { return _DevelopPath;    }  }
        public string CodePath       { get { return _CodePath;    }  }
        public string TemplatePath   { get { return _TemplatePath;    }  }
        public string Compiler       { get { return _Compiler;       }  }
        public string Packer         { get { return _Packer;         }  }
        public bool   LowerName      { get { return _lower_name;     }  }
        public bool   LowerColumn    { get { return _lower_column;   }  }
        public string Arguments      { get { return _Arguments;      }  }
        public string BuildArguments { get { return _BuildArguments; }  }
        public string BuildTools     { get { return _BuildTools;     }  }
        public string ProjectName    { get { return _ProjectName;    }  }
        public string dbAdapter      { get { return _dbAdapter;      }  }
        public string PackageUrl     { get { return _PackageUrl;     }  }
        public string WorkSpacePath  { get { return _WorkSpacePath;  }  }
        public string Provider       { get { return _Provider;       }  }
        public string Target         { get { return _Target;         }  }

    }
}
