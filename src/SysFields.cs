
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;

using Volte.Data.Dapper;
using Volte.Data.Json;

namespace Volte.Bot.Term
{
    [Serializable]
        [AttributeMapping(TableName="sysfields")]
            public class SysFields: EntityObject
        {

            private bool _bAutoIdentity=false;

            private bool _bColumnNullable=false;

            private bool _bColumnUsage=false;

            private bool _bPrimaryKey=false;

            private bool _bVerified=false;

            private DateTime? _dAdd_Date=null;

            private DateTime? _dChg_Date=null;

            private int _nColumnLength=0;

            private int _nScale=0;

            private int _nSequency=0;

            private string _sAdd_User="";

            private string _sCaptionCode="";

            private string _sChg_Flag="";

            private string _sChgUser="";

            private string _sColumnClass="";

            private string _sColumnName="";

            private string _sColumnTypeCode="";

            private string _sRefBrowse="";

            private string _sTableName="";

            //<Summary>
            //SysFields,bAutoIdentity
            //</Summary>
            [AttributeMapping(Type=DbType.Boolean)]
                public bool bAutoIdentity
                {
                    get { return _bAutoIdentity; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_bAutoIdentity)
                            {
                                _bAutoIdentity = value;
                                this.PropertyChange("bAutoIdentity");
                            }
                        }else{
                            _bAutoIdentity = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,bColumnNullable
            //</Summary>
            [AttributeMapping(Type=DbType.Boolean)]
                public bool bColumnNullable
                {
                    get { return _bColumnNullable; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_bColumnNullable)
                            {
                                _bColumnNullable = value;
                                this.PropertyChange("bColumnNullable");
                            }
                        }else{
                            _bColumnNullable = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,bColumnUsage
            //</Summary>
            [AttributeMapping(Type=DbType.Boolean)]
                public bool bColumnUsage
                {
                    get { return _bColumnUsage; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_bColumnUsage)
                            {
                                _bColumnUsage = value;
                                this.PropertyChange("bColumnUsage");
                            }
                        }else{
                            _bColumnUsage = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,bPrimaryKey
            //</Summary>
            [AttributeMapping(Type=DbType.Boolean)]
                public bool bPrimaryKey
                {
                    get { return _bPrimaryKey; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_bPrimaryKey)
                            {
                                _bPrimaryKey = value;
                                this.PropertyChange("bPrimaryKey");
                            }
                        }else{
                            _bPrimaryKey = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,bVerified
            //</Summary>
            [AttributeMapping(Type=DbType.Boolean)]
                public bool bVerified
                {
                    get { return _bVerified; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_bVerified)
                            {
                                _bVerified = value;
                                this.PropertyChange("bVerified");
                            }
                        }else{
                            _bVerified = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,dAddDate
            //</Summary>
            [AttributeMapping(Type=DbType.DateTime)]
                public DateTime? dAddDate
                {
                    get { return _dAdd_Date; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_dAdd_Date)
                            {
                                _dAdd_Date = value;
                                this.PropertyChange("dAddDate");
                            }
                        }else{
                            _dAdd_Date = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,dChgDate
            //</Summary>
            [AttributeMapping(Type=DbType.DateTime)]
                public DateTime? dChgDate
                {
                    get { return _dChg_Date; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_dChg_Date)
                            {
                                _dChg_Date = value;
                                this.PropertyChange("dChgDate");
                            }
                        }else{
                            _dChg_Date = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,nColumnLength
            //</Summary>
            [AttributeMapping(Type=DbType.Int32)]
                public int nColumnLength
                {
                    get { return _nColumnLength; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_nColumnLength)
                            {
                                _nColumnLength = value;
                                this.PropertyChange("nColumnLength");
                            }
                        }else{
                            _nColumnLength = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,nScale
            //</Summary>
            [AttributeMapping(Type=DbType.Int32)]
                public int nScale
                {
                    get { return _nScale; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_nScale)
                            {
                                _nScale = value;
                                this.PropertyChange("nScale");
                            }
                        }else{
                            _nScale = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,nSequency
            //</Summary>
            [AttributeMapping(Type=DbType.Int32)]
                public int nSequency
                {
                    get { return _nSequency; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_nSequency)
                            {
                                _nSequency = value;
                                this.PropertyChange("nSequency");
                            }
                        }else{
                            _nSequency = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sAddUser
            //</Summary>
            [AttributeMapping(Type=DbType.String)]
                public string sAddUser
                {
                    get { return _sAdd_User; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sAdd_User)
                            {
                                _sAdd_User = value;
                                this.PropertyChange("sAddUser");
                            }
                        }else{
                            _sAdd_User = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sCaptionCode
            //</Summary>
            [AttributeMapping(Type=DbType.String)]
                public string sCaptionCode
                {
                    get { return _sCaptionCode; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sCaptionCode)
                            {
                                _sCaptionCode = value;
                                this.PropertyChange("sCaptionCode");
                            }
                        }else{
                            _sCaptionCode = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sChgFlag
            //</Summary>
            [AttributeMapping(Type=DbType.String)]
                public string sChgFlag
                {
                    get { return _sChg_Flag; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sChg_Flag)
                            {
                                _sChg_Flag = value;
                                this.PropertyChange("sChgFlag");
                            }
                        }else{
                            _sChg_Flag = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sChgUser
            //</Summary>
            [AttributeMapping(Type=DbType.String)]
                public string sChgUser
                {
                    get { return _sChgUser; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sChgUser)
                            {
                                _sChgUser = value;
                                this.PropertyChange("sChgUser");
                            }
                        }else{
                            _sChgUser = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sColumnClass
            //</Summary>
            [AttributeMapping(Type=DbType.String)]
                public string sColumnClass
                {
                    get { return _sColumnClass; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sColumnClass)
                            {
                                _sColumnClass = value;
                                this.PropertyChange("sColumnClass");
                            }
                        }else{
                            _sColumnClass = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sColumnName
            //</Summary>

            [AttributeMapping(PrimaryKey=true)]
                public string sColumnName
                {
                    get { return _sColumnName; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sColumnName)
                            {
                                _sColumnName = value;
                                this.PropertyChange("sColumnName");
                            }
                        }else{
                            _sColumnName = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sDataType
            //</Summary>
            [AttributeMapping(Type=DbType.String)]
                public string sDataType
                {
                    get { return _sColumnTypeCode; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sColumnTypeCode)
                            {
                                _sColumnTypeCode = value;
                                this.PropertyChange("sDataType");
                            }
                        }else{
                            _sColumnTypeCode = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sRefBrowse
            //</Summary>
            [AttributeMapping(Type=DbType.String)]
                public string sRefBrowse
                {
                    get { return _sRefBrowse; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sRefBrowse)
                            {
                                _sRefBrowse = value;
                                this.PropertyChange("sRefBrowse");
                            }
                        }else{
                            _sRefBrowse = value;
                        }
                    }
                }

            //<Summary>
            //SysFields,sTableName
            //</Summary>

            [AttributeMapping(PrimaryKey=true)]
                public string sTableName
                {
                    get { return _sTableName; }
                    set
                    {
                        if (this.Verified)
                        {
                            if (value!=_sTableName)
                            {
                                _sTableName = value;
                                this.PropertyChange("sTableName");
                            }
                        }else{
                            _sTableName = value;
                        }
                    }
                }

        }
}

