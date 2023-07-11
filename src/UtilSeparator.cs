using System;
using System.IO;
using System.Text;
using Volte.Data.Json;

namespace Volte.Bot.Term
{
    public class UtilSeparator
    {
        public static string Separator(string value, char DirectorySeparatorChar = char.MinValue)
        {
            if (DirectorySeparatorChar == char.MinValue)
            {
                DirectorySeparatorChar = Path.DirectorySeparatorChar;
            }
            string SeparatorChar = DirectorySeparatorChar.ToString();

            value = value.Replace("\\", SeparatorChar.ToString());
            value = value.Replace("/", SeparatorChar.ToString());

            return value.Replace(SeparatorChar + SeparatorChar, SeparatorChar);
        }

        public static string SearchFile(string sPath,string fileName)
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

        public static JSONObject FileToJSONObject(string fileName)
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

        
        public static string TrimStart(string str,string sStartValue)
        {
            if (string.IsNullOrEmpty(sStartValue))
            {
                return str;
            }
            if (str.StartsWith(sStartValue))
            {
                str = str.Remove(0, sStartValue.Length);
            }
            return str;
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
