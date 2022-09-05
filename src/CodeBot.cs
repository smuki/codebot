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

        public void PrintConfig(JSONArray obj, int nIndent)
        {
            string tKey = "                     ";
            string iKey = "                     ";
            string sIndent = iKey.Substring(0, nIndent);

            Console.WriteLine(sIndent + "[");
            foreach (JSONObject its in obj.JSONObjects)
            {
                PrintConfig(its, nIndent + 3);
            }
            foreach (string sValue in obj.Names)
            {
                Console.WriteLine(sIndent +"   "+ sValue);
            }
            tKey = "                     ";
            Console.WriteLine(sIndent + "]");            
        }
        public void PrintConfig(JSONObject obj, int nIndent)
        {
            string tKey = "                              ";
            string iKey = "                              ";
            string sIndent = iKey.Substring(0, nIndent);

            Console.WriteLine(sIndent + "{");
            int max=3;
            foreach (string sKey in obj.Names)
            {
                max=Math.Max(max,sKey.Length);
            }
            foreach (string sKey in obj.Names)
            {
                if (obj.IsJSONObject(sKey))
                {
                    Console.WriteLine(sIndent + "   " + sKey);
                    PrintConfig(obj.GetJSONObject(sKey), nIndent + 3);
                }else if (obj.IsJSONArray(sKey))
                {
                    Console.WriteLine(sIndent + "   " + sKey);
                    PrintConfig(obj.GetJSONArray(sKey), nIndent + 3);
                }
                else
                {
                    tKey = sKey + "                     ";
                    Console.WriteLine(sIndent + "   " + tKey.Substring(0, max) + " = " + obj.GetValue(sKey));
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

                if (_Arguments["tpl"] != null)
                {
                    sTemplate = _Arguments["tpl"];
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

                string fileName = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + ".json";

                if (_Arguments["S"] != null)
                {
                    fileName = _Arguments["S"];
                }
                if (!File.Exists(fileName))
                {
                    if (File.Exists(fileName+".json"))
                    {
                        fileName = fileName+".json";
                    }else{
                        Console.WriteLine("[" + fileName + "] Not Found!");
                    }
                }

                AppConfigs AppConfigs = new AppConfigs(fileName.Replace(".ini", ".json"));

                Console.WriteLine("AppSetting : [" + fileName + "]");
                Console.WriteLine("sCommand : [" + sCommand + "]");
                PrintConfig(AppConfigs.JSONObjects, 0);


                switch (sCommand) {
                    case "B": {
                                if (string.IsNullOrEmpty(sTemplate)){
                                    sTemplate = AppConfigs.GetValue("sTemplate");
                                }
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

                                  AutoGenerator _AutoGenerator  = new AutoGenerator();
                                  _AutoGenerator.AppConfigs = AppConfigs;
                                  _AutoGenerator.FileName   = _FileName;
                                  _AutoGenerator.DebugMode  = _debugMode;
                                  _AutoGenerator.sTemplate  = sTemplate;
                                  _AutoGenerator.Mode       = sMode;
                                  _AutoGenerator.GeneratorActivityDefinition(sUID);

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

                    case "T": {
                                Console.WriteLine("sTableName   = "+sTableName);
                                if (string.IsNullOrEmpty(sTemplate)){
                                    sTemplate = AppConfigs.GetValue("sTemplate");
                                }
                                if (string.IsNullOrEmpty(sTemplate)){
                                    sTemplate = "entity";
                                }

                                AutoGenerator _AutoGenerator  = new AutoGenerator();
                                _AutoGenerator.AppConfigs = AppConfigs;
                                _AutoGenerator.FileName   = _FileName;
                                _AutoGenerator.DebugMode  = _debugMode;
                                _AutoGenerator.gTableName = sTableName;
                                _AutoGenerator.sTemplate  = sTemplate;
                                _AutoGenerator.GeneratorEntityDefinition();

                                AutoCoder _AutoCoder  = new AutoCoder();
                                _AutoCoder.AppConfigs = AppConfigs;
                                _AutoCoder.FileName   = _FileName;
                                _AutoCoder.DebugMode  = _debugMode;
                                _AutoCoder.gTableName = sTableName;
                                _AutoCoder.sTemplate  = sTemplate;
                                _AutoCoder.GeneratorEntity();
                                break ;
                              }

                    case "FIELDS": {

                                AutoGenerator _AutoGenerator  = new AutoGenerator();
                                _AutoGenerator.AppConfigs = AppConfigs;
                                _AutoGenerator.DebugMode  = _debugMode;
                                _AutoGenerator.FileName   = _FileName;
                                _AutoGenerator.RefreshSysFields();
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
