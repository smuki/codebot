using System;
using System.Text;
using Volte.Data.Json;

namespace Volte.Bot.Term
{

    public class CommandEntity {
        public string sArguments = "";
        public string sCommand   = "";
        public string sDirectory = "";
        public string Message    = "";
        public bool   SUCCESS    = false;
    }
    public class ShellRunner {

        const string ZFILE_NAME  = "ShellRunner";
        public string sDirectory = "";

        public CommandEntity Execute(CommandEntity entity)
        {
            ZZLogger.Debug(ZFILE_NAME , entity.sCommand);
            ZZLogger.Debug(ZFILE_NAME , entity.sArguments);

            StringBuilder s=new StringBuilder();
            try {

                System.Diagnostics.Process Process2       = new System.Diagnostics.Process();
                Process2.StartInfo.FileName               = entity.sCommand;
                Process2.StartInfo.Arguments              = entity.sArguments;
                Process2.StartInfo.UseShellExecute        = false;
                Process2.StartInfo.RedirectStandardOutput = true;
                Process2.Start();

                System.IO.StreamReader sr = Process2.StandardOutput;
                string re = sr.ReadToEnd();
                sr.Close();

                ZZLogger.Debug(ZFILE_NAME , re);

                s.AppendLine(re);

                s.AppendLine("------------------------------------------------------------");

                int warning = 0;
                int error   = 0;

                foreach (string tmp in re.Split('\n')) {

                    if (tmp.IndexOf("): warning ") > 0) {
                        s.AppendLine(tmp);
                        warning++;
                    } else if (tmp.IndexOf("): error ") > 0) {
                        s.AppendLine(tmp);
                        error++;
                    }
                }

                s.AppendLine("");
                s.Append(error);
                s.Append(" error(s)  ");

                s.Append(warning);
                s.AppendLine(" warning(s)");

                if (error == 0) {
                    entity.SUCCESS=true;
                    s.Append("BUILD SUCCEEDED.");
                } else {
                    entity.SUCCESS=false;
                    s.Append("BUILD FAILED.");
                    s.AppendLine(entity.sArguments);
                }

                s.AppendLine("");

            } catch (Exception e) {
                s.AppendLine(entity.sCommand);
                s.AppendLine(e.Message);
            }
            entity.Message = s.ToString();

            return entity;
        }
    }
}
