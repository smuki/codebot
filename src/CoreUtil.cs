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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

using Volte.Bot.Tpl;
using Volte.Data.Dapper;

namespace Volte.Bot.Term
{
    public class CoreUtil {

        const string   ZFILE_NAME = "CoreUtil";
        private static readonly MD5 _md5 = MD5.Create();

        public static string Hash(string sValue)
        {
            var hashBytes    = _md5.ComputeHash(Encoding.UTF8.GetBytes(sValue.ToUpper()));
            StringBuilder sb = new StringBuilder();
            for (int i = 4; i < hashBytes.Length-6; i++) {
                sb.Append(hashBytes[i].ToString("x2"));
            }

            return sb.ToString().ToUpper();
        }

        public static void CreateDir(string sPathName)
        {
            try {
                if (!Directory.Exists(sPathName)) {
                    Directory.CreateDirectory(sPathName);
                }
            } catch (Exception ex) {
            }
        }
    }
}
