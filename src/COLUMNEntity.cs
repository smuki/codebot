using System;
using System.Collections.Generic;
using System.Text;

namespace Volte.Bot.Term
{
    public class COLUMNEntity
    {
        public string EnableMode { get; set; }
        public bool ColumnNullable { get; set; }
        public bool bAutoIdentity { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public int Length { get; set; }
        public int MaxLength { get; set; }
        public int ColumnScale { get; set; }
        public string DataTypeCode { get; set; }
        public string DataTypeChar { get; set; }
        public string CaptionCode { get; set; }
        public string LNK_ColumnName { get; set; }
        public string Options { get; set; }
        public string OptionType { get; set; }
        public int Sequency { get; set; }
        public int Index { get; set; }
        public string sRefCheck { get; set; }
        public string sRefViewer { get; set; }
        public string sRefBrowse { get; set; }
        public string sRefBrowseType { get; set; }
        public string sColumnGroup { get; set; }

        public bool bHasCaption { get; set; }
        public bool bWriteable { get; set; }
        public bool bPrimaryKey { get; set; }
        public bool Release { get; set; }
        public string ZZVIEW { get; set; }

        public string TableColumnName { get { return this.TableName + "_" + this.ColumnName; } }

    }
}
