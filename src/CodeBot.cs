using System;
using System.Web;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Collections.Specialized;
using System.Xml;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

using System.Security.Cryptography;

using Volte.Data.Dapper;
using Volte.Data.Json;

namespace Volte.Bot.Term
{

    public class MainCG
    {

        static byte[] getData(string name)
        {

            Stream sm = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            byte[] bs = new byte[sm.Length];
            sm.Read(bs, 0, (int)sm.Length);
            sm.Close();

            return bs;
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {

            AssemblyName name = new AssemblyName(args.Name);

            try
            {
                return AppDomain.CurrentDomain.Load(getData(name.Name + ".dll"));

            }
            catch (Exception ex)
            {
            }

            return null;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToOADate());
            Console.WriteLine("Date =" + DateTime.Now.AddDays(400).ToString("yyyy-MM-dd"));
            Console.WriteLine("Value=" + DateTime.Now.AddDays(400).ToOADate());

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            CG _CG = new CG();
            _CG.Process(args);
        }
    }

    public class CG {

        const string ZFILE_NAME = "CG";

        private readonly object _PENDING     = new object();
        private FileSystemWatcher[] watcher  = new FileSystemWatcher[20];
        private string[] _filePath           = new string[20];
        private List<String> _FileQueue      = new List<string>();
        private List<String> _FormatQueue    = new List<string>();

        private Dictionary<string, DateTime> Check       = new Dictionary<string, DateTime>();
        private Dictionary<string, DateTime> FormatCheck = new Dictionary<string, DateTime>();

        public void PrintHelp()
        {
            string fileName = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Console.WriteLine("");
            Console.WriteLine("Usage");
            Console.WriteLine("");
            Console.WriteLine("    " + fileName + " <command> [Options]");
            Console.WriteLine("");
            Console.WriteLine("Command :");
            Console.WriteLine("the following command are available");
            Console.WriteLine(" /build   = Build Program");
            Console.WriteLine("     Parameter:");
            Console.WriteLine("         /u  [Function Id]");
            Console.WriteLine("");
            Console.WriteLine(" /entity  = build entity");
            Console.WriteLine("");
            Console.WriteLine("Options :");
            Console.WriteLine(" /p Parameter");
            Console.WriteLine("/Delete [Y/N]");
            Console.WriteLine("          Y : Yes(default)");
            Console.WriteLine("          N : No");
            Console.WriteLine("");
            Console.WriteLine("/u  [Function Id]");
            Console.WriteLine("/file  [File]");
            Console.WriteLine("/f2 [File2]");
            Console.WriteLine("/s  [config File Name]");
            Console.WriteLine("/debug [debug mode]");
            Console.WriteLine("");

        }

        public void PrintConfig(JSONObject obj, int level)
        {
            string tKey = "                     ";
            string iKey = "                     ";
            string sIndent = iKey.Substring(0, level * 3);

            Console.WriteLine(sIndent + "{");
            foreach (string sKey in obj.Names)
            {
                if (obj.IsJSONObject(sKey))
                {
                    tKey = sIndent + sKey + "                     ";
                    Console.WriteLine(sIndent + "   " + tKey.Substring(0, 13) + " = ");
                    PrintConfig(obj.GetJSONObject(sKey), level + 1);
                }
                else
                {
                    tKey = sIndent + sKey + "                     ";
                    Console.WriteLine(sIndent + "   " + tKey.Substring(0, 13) + " = " + obj.GetValue(sKey));
                }
            }
            tKey = "                     ";
            Console.WriteLine(sIndent + "}");
        }

        public void Process(string[] args)
        {
            try
            {

                Arguments _Arguments = new Arguments(args);

                string sCommand    = "";
                string sUID        = "";
                string _debugMode  = "N";
                string _FileName   = "";
                string sMode       = "";
                string sDefineFile = "";
                string sTableName  = "";
                string _IPAddress  = "";
                string Port        = "";
                string sTablePrefix= "";
                string sTemplate   = "";

                if (_Arguments["C"] != null)
                {
                    sCommand = _Arguments["C"].ToUpper();
                }

                if (_Arguments["Build"] != null)
                {
                    sCommand = "B";
                }
                if (_Arguments["Generator"] != null) {
                    sCommand = "G";
                }

                if (_Arguments["H"] != null) {
                    _IPAddress = _Arguments["H"];
                }

                if (_Arguments["P"] != null) {
                    Port = _Arguments["P"];
                }

                if (_Arguments["D"] != null) {
                    sDefineFile = _Arguments["D"];
                }

                if (_Arguments["Run"] != null) {
                    sCommand = "A";
                }
                if (_Arguments["FIELDS"] != null) {
                    sCommand = "FIELDS";
                }
                if (_Arguments["T"] != null)
                {
                    sTableName = _Arguments["T"];
                }

                if (_Arguments["Prefix"] != null)
                {
                    sTablePrefix = _Arguments["Prefix"];
                }

                if (_Arguments["U"] != null) {
                    sUID = _Arguments["U"].ToUpper();
                    sUID = sUID.Replace(".CS", "");

                    if (sUID == "TRUE")
                    {
                        sUID = "A";
                    }
                }

                if (_Arguments["F"] != null)
                {
                    _FileName = _Arguments["F"];
                }

                if (_Arguments["Template"] != null)
                {
                    sTemplate = _Arguments["Template"];
                }

                if (_Arguments["File"] != null)
                {
                    _FileName = _Arguments["File"];
                }

                if (_Arguments["M"] != null)
                {

                    sMode = _Arguments["M"].ToUpper();
                }

                if (_Arguments["MODE"] != null)
                {

                    sMode = _Arguments["MODE"].ToUpper();
                }

                if (_Arguments["Entity"] != null) {

                    sCommand = "T";
                }

                if (_Arguments["DEBUG"] != null) {
                    if (_Arguments["DEBUG"] == "F") {
                        _debugMode = "F";
                    } else {
                        _debugMode = "Y";
                    }
                }
                else
                {
                    _debugMode = "N";
                }

                string fileName = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + ".ini";

                if (_Arguments["S"] != null)
                {
                    fileName = _Arguments["S"];
                }
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("[" + fileName + "] Not Found");
                }
                if (!File.Exists(fileName.Replace(".ini", ".json")))
                {
                    Console.WriteLine("[" + fileName.Replace(".ini", ".json") + "] Not Found");
                }
                AppConfigs AppConfigs = new AppConfigs(fileName.Replace(".ini", ".json"));

                Console.WriteLine("AppSetting : [" + fileName + "]");
                PrintConfig(AppConfigs.JSONObjects, 0);


                switch (sCommand) {
                    case "B": {
                                if (string.IsNullOrEmpty(sTemplate)){
                                    sTemplate = "N";
                                }
                                AutoCoder _AutoCoder  = new AutoCoder();
                                _AutoCoder.AppConfigs = AppConfigs;
                                _AutoCoder.FileName   = _FileName;
                                _AutoCoder.DebugMode  = _debugMode;
                                _AutoCoder.sTemplate  = sTemplate;
                                _AutoCoder.Mode       = sMode;
                                _AutoCoder.Process(sUID);

                                sUID = "";
                                break;
                              }

                    case "G": {
                                  AutoCoder _AutoCoder  = new AutoCoder();
                                  _AutoCoder.AppConfigs = AppConfigs;
                                  _AutoCoder.FileName   = _FileName;
                                  _AutoCoder.DebugMode  = _debugMode;
                                  _AutoCoder.sTemplate  = sTemplate;
                                  _AutoCoder.Mode       = sMode;
                                  _AutoCoder.GeneratorEntityDefinition();
                                  _AutoCoder.Generator(sUID);

                                  sUID = "";
                                  break;
                              }

                    case "T": {
                                Console.WriteLine("sTablePrefix = "+sTablePrefix);
                                Console.WriteLine("sTableName   = "+sTableName);
                                if (string.IsNullOrEmpty(sTemplate)){
                                    sTemplate = "entity";
                                }

                                AutoCoder _AutoCoder  = new AutoCoder();
                                _AutoCoder.AppConfigs = AppConfigs;
                                _AutoCoder.FileName   = _FileName;
                                _AutoCoder.DebugMode  = _debugMode;
                                _AutoCoder.gTableName = sTableName;
                                _AutoCoder.sTemplate  = sTemplate;
                                _AutoCoder.sTablePrefix = sTablePrefix;
                                _AutoCoder.GeneratorEntityDefinition();
                                _AutoCoder.GeneratorEntity();
                                break ;
                              }

                    case "RUN": {

                                    AutoCoder _AutoCoder  = new AutoCoder();
                                    _AutoCoder.AppConfigs = AppConfigs;
                                    _AutoCoder.DebugMode  = _debugMode;
                                    _AutoCoder.FileName   = _FileName;
                                    _AutoCoder.Templates();
                                    Console.WriteLine(_FileName);
                                    break;
                                }


                    case "FIELDS": {

                                AutoCoder _AutoCoder  = new AutoCoder();
                                _AutoCoder.AppConfigs = AppConfigs;
                                _AutoCoder.DebugMode  = _debugMode;
                                _AutoCoder.FileName   = _FileName;
                                _AutoCoder.RefreshSysFields();
                                Console.WriteLine(_FileName);
                                break;
                            }

                    default:
                        {
                            PrintHelp();
                            break;
                        }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception=" + e.ToString());
            }

        }
    }
}
