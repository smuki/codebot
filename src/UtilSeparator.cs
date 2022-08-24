using System.IO;
using System.Text;

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
    }
}
