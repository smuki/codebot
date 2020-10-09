using System;
using System.Collections.Generic;
using System.Text;

namespace Volte.Bot.Term
{
    public class COLUMNEntity {
        private bool   _ColumnNullable = true;
        private bool   _IsPKColumn     = false;
        private bool   _NewLine        = false;
        private bool   _HasCaption     = false;
        private bool   _Writeable      = false;
        private bool   _Release        = false;
        private int    _ColSpan        = 0;
        private int    _ColumnScale    = 0;
        private int    _Height         = 0;
        private int    _Length         = 0;
        private int    _maxLength      = 0;
        private int    _Index          = 0;
        private int    _RowSpan        = 0;
        private int    _Sequency       = 0;
        private string _OptionType     = "";
        private string _ColumnName     = "";
        private string _DataTypeCode   = "";
        private string _DataTypeChar   = "";
        private string _CaptionCode    = "";
        private string _LNK_ColumnName = "";
        private string _EnableMode     = "";
        private string _TableName      = "";
        private string _Options        = "";
        private string _ZZVIEW         = "";
        private string _ColumnAlign    = "";
        private string _CaptionAlign   = "";
        private string _RefBrowse      = "";
        private string _RefBrowseType  = "";
        private string _RefViewer      = "";
        private string _RefCheck       = "";
        private string _ColumnGroup    = "";

        public string EnableMode     { get { return _EnableMode;     } set { _EnableMode     = value; }  }
        public bool   ColumnNullable { get { return _ColumnNullable; } set { _ColumnNullable = value; }  }
        public string TableName      { get { return _TableName;      } set { _TableName      = value; }  }
        public string ColumnName     { get { return _ColumnName;     } set { _ColumnName     = value; }  }
        public int    Height         { get { return _Height;         } set { _Height         = value; }  }
        public int    Length         { get { return _Length;         } set { _Length         = value; }  }
        public int    MaxLength      { get { return _maxLength;      } set { _maxLength      = value; }  }
        public int    ColumnScale    { get { return _ColumnScale;    } set { _ColumnScale    = value; }  }
        public string DataTypeCode   { get { return _DataTypeCode;   } set { _DataTypeCode   = value; }  }
        public string DataTypeChar   { get { return _DataTypeChar;   } set { _DataTypeChar   = value; }  }
        public int    RowSpan        { get { return _RowSpan;        } set { _RowSpan        = value; }  }
        public int    ColSpan        { get { return _ColSpan;        } set { _ColSpan        = value; }  }
        public bool   NewLine        { get { return _NewLine;        } set { _NewLine        = value; }  }
        public string CaptionCode    { get { return _CaptionCode;    } set { _CaptionCode    = value; }  }
        public string LNK_ColumnName { get { return _LNK_ColumnName; } set { _LNK_ColumnName = value; }  }
        public string Options        { get { return _Options;        } set { _Options        = value; }  }
        public string OptionType     { get { return _OptionType;     } set { _OptionType     = value; }  }
        public int    Sequency       { get { return _Sequency;       } set { _Sequency       = value; }  }
        public int    Index          { get { return _Index;          } set { _Index          = value; }  }
        public string sRefCheck      { get { return _RefCheck;       } set { _RefCheck       = value; }  }
        public string sRefViewer     { get { return _RefViewer;      } set { _RefViewer      = value; }  }
        public string sRefBrowse     { get { return _RefBrowse;      } set { _RefBrowse      = value; }  }
        public string sRefBrowseType { get { return _RefBrowseType;  } set { _RefBrowseType  = value; }  }
        public string sColumnGroup   { get { return _ColumnGroup;    } set { _ColumnGroup    = value; }  }
        public string ColumnAlign    { get { return _ColumnAlign;    } set { _ColumnAlign    = value; }  }
        public string CaptionAlign   { get { return _CaptionAlign;   } set { _CaptionAlign   = value; }  }
        public bool   bHasCaption    { get { return _HasCaption;     } set { _HasCaption     = value; }  }
        public bool   bWriteable     { get { return _Writeable;      } set { _Writeable      = value; }  }
        public bool   IsPKColumn     { get { return _IsPKColumn;     } set { _IsPKColumn     = value; }  }
        public bool   Release        { get { return _Release;        } set { _Release        = value; }  }
        public string ZZVIEW         { get { return _ZZVIEW;         } set { _ZZVIEW         = value; }  }

        public string TableColumnName { get { return _TableName + "_" + _ColumnName; }  }

    }
}
