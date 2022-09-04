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

namespace Volte.Bot.Term
{
    public class Util {

        public string LoadJSONFile(string fileName)
        {
            if (File.Exists(fileName)) {
                string s = "";
                string j = "";

                using(StreamReader sr = new StreamReader(fileName , Encoding.UTF8)) {
                    while ((s = sr.ReadLine()) != null) {
                        j += s.Trim();
                    }
                    return j;
                }
            }
            return "{}";
        }

        public JSONObject FileToJSONObject(string fileName)
        {
            if (File.Exists(fileName)) {
                string s = "";
                string j = "";

                using(StreamReader sr = new StreamReader(fileName , Encoding.UTF8)) {
                    while ((s = sr.ReadLine()) != null)
                    {
                        j += s.Trim();
                    }
                    return new JSONObject(j);
                }
            }
            return new JSONObject();
        }

        public bool Exists(string fileName)
        {
            return File.Exists(fileName);
        }


        public static string Separator(string value)
        {
            value = value.Replace("\\", Path.DirectorySeparatorChar.ToString());
            value = value.Replace("/", Path.DirectorySeparatorChar.ToString());
            string SeparatorChar=Path.DirectorySeparatorChar.ToString();

            return value.Replace(SeparatorChar + SeparatorChar,SeparatorChar);
        }
    }
}
