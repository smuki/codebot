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
using System.Security.Cryptography;

using Volte.Bot.Tpl;
using Volte.Data.Dapper;
using Volte.Data.Json;
using Volte.Utils;

namespace Volte.Bot.Term
{

    public class FileNameValue {

        public string RelativePath { get { return _RelativePath; } set { _RelativePath = value; }  }
        public string Type         { get { return _type;         } set { _type         = value; }  }
        public string Name         { get { return _name;         } set { _name         = value; }  }
        public string FileName     { get { return _FileName;     } set { _FileName     = value; }  }
        public string FullName     { get { return _FullName;     } set { _FullName     = value; }  }
        public bool   Compress     { get { return _Compress;     } set { _Compress     = value; }  }

        private string _name         = "";
        private string _type         = "";
        private string _RelativePath = "";
        private string _FileName     = "";
        private string _FullName     = "";
        private bool   _Compress     = false;

    }

}
