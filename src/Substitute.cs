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


    public class Substitute
    {
        private Dictionary<string, string> _fileNames = new Dictionary<string, string>();
        private Dictionary<string, string> _Codes = new Dictionary<string, string>();
        private JSONArray _Values = new JSONArray();
        private bool init=false;
        public void Initialize()
        {
            if (init){
                return;
            }
            init=true;
            if (File.Exists("fileName.txt"))
            {
                using (StreamReader sr = new StreamReader("fileName.txt"))
                {
                    string content = "";
                    while (content != null)
                    {
                        content = sr.ReadLine();
                        if (content != null)
                        {
                            var c = content.Split('|');
                            if (c.Length > 1)
                            {
                                _fileNames[c[0]] = c[1];
                            }
                        }
                    }
                }
            }
            if (File.Exists("Codes.txt"))
            {
                using (StreamReader sr = new StreamReader("Codes.txt"))
                {
                    string content = "";
                    while (content != null)
                    {
                        content = sr.ReadLine();
                        if (content != null)
                        {
                            var c = content.Split('|');

                            if (c.Length > 1)
                            {
                                string to = c[1];
                                to = to.Replace("\\n", "\n");
                                _Codes[c[0]] = to;
                            }
                        }
                    }
                }
            }
            if (File.Exists("Codes.json"))
            {
                using (StreamReader sr = new StreamReader("Codes.json"))
                {
                    string content = sr.ReadToEnd();
                    _Values = new JSONArray(content);
                }
            }
        }
        public string ReplaceWith(string src, string a, string b)
        {
            if (string.IsNullOrEmpty(src))
            {
                return src;
            }
            return src.Replace(a, b);
        }
        public string fileNameReplace(string src, string fullName)
        {
            if (string.IsNullOrEmpty(src))
            {
                return src;
            }

            string s = src.Trim();
            string sData = "";
            if (File.Exists(fullName))
            {
                StreamReader sr = new StreamReader(fullName);
                sData = sr.ReadToEnd();
            }

            foreach (string f in _fileNames.Keys)
            {
                s = ReplaceWith(s, f, _fileNames[f]);
            }


            return s;
        }
        public string CodeReplace(string src)
        {
            if (string.IsNullOrEmpty(src))
            {
                return src;
            }
            string s = src.Trim();
            foreach (string f in _Codes.Keys)
            {
                s = ReplaceWith(s, f, _Codes[f]);
            }
            foreach (JSONObject v in _Values.JSONObjects)
            {
                s = ReplaceWith(s, v.GetValue("from"), v.GetValue("to"));
            }
            return s;
        }
        public void CopyFile(string src, string dest)
        {
            UTF8Encoding _UTF8Encoding = new UTF8Encoding(false, true);

            using (StreamReader sr = new StreamReader(src))
            {
                string content = "";
                if (!Directory.Exists(Path.GetDirectoryName(dest)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                }
                StreamWriter sw = new StreamWriter(dest, false);
                while (content != null)
                {
                    content = sr.ReadLine();
                    if (content != null)
                    {

                        sw.WriteLine(CodeReplace(content));
                    }
                }
                sw.Close();
            }
        }
    }

}
