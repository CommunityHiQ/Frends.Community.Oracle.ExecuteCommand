using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frends.Community.Oracle.ExecuteCommand
{
    #region Enums
    public enum OracleCommandType { StoredProcedure = 4, Command = 1 }
    public enum OracleCommandReturnType { XmlString, XDocument, AffectedRows, JSONString }
    #endregion

    public class Input
    {
        [PasswordPropertyText(true)]
        [DefaultValue("\"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;\"")]
        public String ConnectionString { get; set; }

        [DefaultValue("\"testprocedure\"")]
        public String CommandOrProcedureName { get; set; }
    }

    public class Options
    {
        public OracleCommandType CommandType { get; set; }

        public OracleCommandReturnType DataReturnType { get; set; }

        public bool BindParametersByName { get; set; }

        [DefaultValue(30)]
        public Int32 TimeoutSeconds { get; set; }

        public OracleParameter[] InputParameters { get; set; }
        public OracleParameter[] OutputParameters { get; set; }
    }

    public class OracleParameter
    {
        public String Name { get; set; }
        public dynamic Value { get; set; }

        public ParameterDataType DataType { get; set; }

        public enum ParameterDataType
        {
            BFile = 101,
            Blob = 102,
            Byte = 103,
            Char = 104,
            Clob = 105,
            Date = 106,
            Decimal = 107,
            Double = 108,
            Long = 109,
            LongRaw = 110,
            Int16 = 111,
            Int32 = 112,
            Int64 = 113,
            IntervalDS = 114,
            IntervalYM = 115,
            NClob = 116,
            NChar = 117,
            NVarchar2 = 119,
            Raw = 120,
            RefCursor = 121,
            Single = 122,
            TimeStamp = 123,
            TimeStampLTZ = 124,
            TimeStampTZ = 125,
            Varchar2 = 126,
            XmlType = 127,
            BinaryDouble = 132,
            BinaryFloat = 133
        }

    }
}
