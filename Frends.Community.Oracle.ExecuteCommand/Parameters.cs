using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace Frends.Community.Oracle.ExecuteCommand
{
    #region Enums
    public enum OracleCommandType { StoredProcedure = 4, Command = 1 }
    public enum OracleCommandReturnType { XmlString, XDocument, AffectedRows, JSONString }
    #endregion

    /// <summary>
    /// Inputs for Oracle ExecuteCommand component
    /// </summary>
    public class Input
    {
        /// <summary>
        /// The Oracle DB connection string
        /// </summary>
        [DefaultValue("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;")]
        [DisplayFormat(DataFormatString = "Text")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// The type of execution
        /// </summary>
        [DefaultValue(OracleCommandType.Command)]
        public OracleCommandType CommandType { get; set; }

        /// <summary>
        /// Either the command to execute, or the name of the stored procedure to execute
        /// </summary>
        [DefaultValue("testprocedure")]
        [DisplayFormat(DataFormatString = "Text")]
        public string CommandOrProcedureName { get; set; }

        /// <summary>
        /// The input parameters for the query
        /// </summary>
        public OracleParameter[] InputParameters { get; set; }

        /// <summary>
        /// Whether to bind parameters by name
        /// </summary>
        [DefaultValue(false)]
        public bool BindParametersByName { get; set; }

        /// <summary>
        /// The timeout value for the execution in seconds
        /// </summary>
        [DefaultValue(30)]
        public int TimeoutSeconds { get; set; }
    }

    /// <summary>
    /// Output of Oracle ExecuteCommand component
    /// </summary>
    public class OutputProperties
    {
        /// <summary>
        /// The format in which the component willl return data
        /// </summary>
        [DefaultValue(OracleCommandReturnType.XDocument)]
        public OracleCommandReturnType DataReturnType { get; set; }

        /// <summary>
        /// The output parameters for the query
        /// </summary>
        public OracleParameter[] OutputParameters { get; set; }
    }

    /// <summary>
    /// Options for Oracle ExecuteCommand component
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Choose if error should be thrown if Task failes.
        /// Otherwise returns Object {Success = false }
        /// </summary>
        [DefaultValue(true)]
        public bool ThrowErrorOnFailure { get; set; }
    }

    public class Output
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public dynamic Result { get; set; }
    }

    /// <summary>
    /// Parameters for query
    /// </summary>
    public class OracleParameter
    {
        /// <summary>
        /// The name of the parameter
        /// </summary>
        [DefaultValue("Parameter Name")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Name { get; set; }

        /// <summary>
        /// The value of the parameter
        /// </summary>
        [DefaultValue("Value")]
        [DisplayFormat(DataFormatString = "Text")]
        public dynamic Value { get; set; }

        /// <summary>
        /// The data type of the parameter
        /// </summary>
        [DefaultValue(ParameterDataType.NVarchar2)]
        public ParameterDataType DataType { get; set; }

        /// <summary>
        /// The size of the parameter. Doesn't need to be set for input parameters.
        /// </summary>
        [DefaultValue(0)]
        public int Size { get; set; }

        /// <summary>
        /// Enumeration for specifying the data types
        /// </summary>
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
