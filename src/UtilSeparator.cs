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
    }
}
