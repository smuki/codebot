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
            Console.WriteLine(" /build     Build activity");
            Console.WriteLine("    Parameter:");
            Console.WriteLine("      /u  [activity id]");
            Console.WriteLine(" /generator generator activity");
            Console.WriteLine("    Parameter:");
            Console.WriteLine("      /u  [activity id]");
            Console.WriteLine("");
            Console.WriteLine(" /entity    build entity");
            Console.WriteLine("");
            Console.WriteLine("/u  [activity id]");
            Console.WriteLine("/s  [config file name]");
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
                string sTableName  = "";

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
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("AppSetting : [" + fileName + "] Not Found!!");
                    Console.WriteLine("exit....");
                    return;
                }
                AppConfigs AppConfigs = new AppConfigs(fileName);

                Console.WriteLine("AppSetting : [" + fileName + "]");
                Console.WriteLine("sCommand   : [" + sCommand + "]");
                if (_debugMode=="Y"){
                    PrintConfig(AppConfigs.JSONObjects, 0);
                }

                switch (sCommand) {
                    case "B": {
                                AutoCoder _AutoCoder  = new AutoCoder();
                                _AutoCoder.AppConfigs = AppConfigs;
                                _AutoCoder.DebugMode  = _debugMode;
                                _AutoCoder.Process(sUID);

                                sUID = "";
                                break;

                              }

                    case "G": {

                                  AutoGenerator _AutoGenerator  = new AutoGenerator();
                                  _AutoGenerator.AppConfigs = AppConfigs;
                                  _AutoGenerator.DebugMode  = _debugMode;
                                  _AutoGenerator.Generator(sUID);

                                  AutoCoder _AutoCoder  = new AutoCoder();
                                  _AutoCoder.AppConfigs = AppConfigs;
                                  _AutoCoder.DebugMode  = _debugMode;
                                  _AutoCoder.Process(sUID);

                                  sUID = "";
                                  break;
                              }

                    case "T": {

                                AutoGenerator _AutoGenerator  = new AutoGenerator();
                                _AutoGenerator.AppConfigs = AppConfigs;
                                _AutoGenerator.DebugMode  = _debugMode;
                                _AutoGenerator.gTableName = sTableName;
                                _AutoGenerator.GeneratorEntityDefinition();

                                AutoCoder _AutoCoder  = new AutoCoder();
                                _AutoCoder.AppConfigs = AppConfigs;
                                _AutoCoder.DebugMode  = _debugMode;
                                _AutoCoder.GeneratorEntity();
                                break ;
                              }

                    case "FIELDS": {

                                AutoGenerator _AutoGenerator  = new AutoGenerator();
                                _AutoGenerator.AppConfigs = AppConfigs;
                                _AutoGenerator.DebugMode  = _debugMode;
                                _AutoGenerator.RefreshSysFields();
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
