using System;
using System.Collections.Generic;
using System.Text;

namespace Volte.Bot.Term
{
    public class COLUMNEntity
    {
        public string sTableName { get; set; }
        public string sColumnName { get; set; }
        public string sCamelTableName { get; set; }
        public string sCamelColumnName { get; set; }
        public string sEnableMode { get; set; }
        public bool   bNullable { get; set; }
        public bool   bAutoIdentity { get; set; }
        public string sDataType { get; set; }
        public string sDescriptionId { get; set; }
        public string sComment { get; set; }
        public string LNK_ColumnName { get; set; }
        public string Options { get; set; }
        public string OptionType { get; set; }
        public string sRefCheck { get; set; }
        public string sRefViewer { get; set; }
        public string sRefBrowse { get; set; }
        public string sRefBrowseType { get; set; }
        public string sColumnGroup { get; set; }
        public string sAlias { get; set; }

        public bool bWriteable  { get; set; }
        public bool bPrimaryKey { get; set; }
        public bool bIndexes    { get; set; }
        public int  nMaxLength  { get; set; }

        public string TableColumnName { get { return this.sTableName + "_" + this.sColumnName; } }

    }
}
