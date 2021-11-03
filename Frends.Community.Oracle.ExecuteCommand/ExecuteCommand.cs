using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ComponentModel;
using OracleParam = Oracle.ManagedDataAccess.Client.OracleParameter;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Types;


#pragma warning disable 1591
namespace Frends.Community.Oracle.ExecuteCommand
{
    public class ExecuteCommand
    {
        private static readonly ConcurrentDictionary<string, Lazy<OracleConnection>> LazyConnectionCache =
            new ConcurrentDictionary<string, Lazy<OracleConnection>>();

        public static void ClearClientCache()
        {
            LazyConnectionCache.Clear();
        }

        /// <summary>
        /// Task for executing non-query commands and stored procedures in Oracle. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.ExecuteCommand
        /// </summary>
        /// <param name="input">The input data for the task</param>
        /// <param name="output">The output of the task</param>
        /// <param name="options">The options for the task</param>
        /// <returns>object { bool Success, string Message, dynamic Result }</returns>
        public async static Task<Output> Execute([PropertyTab] Input input, [PropertyTab] OutputProperties output,
            [PropertyTab] Options options)
        {

            try
            {
                OracleConnection connection = null;

                // Get connection from cache, or create a new one
                connection = GetLazyConnection(input.ConnectionString);

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using (OracleCommand command = new OracleCommand(input.CommandOrProcedureName, connection))
                {
                    command.CommandType = (CommandType) input.CommandType;
                    command.CommandTimeout = input.TimeoutSeconds;

                    if (input.InputParameters != null)
                    {
                        command.Parameters.AddRange(input.InputParameters.Select(x => CreateOracleParam(x))
                            .ToArray());
                    }

                    if (output.OutputParameters != null)
                        command.Parameters.AddRange(output.OutputParameters
                            .Select(x => CreateOracleParam(x, ParameterDirection.Output)).ToArray());

                    command.BindByName = input.BindParametersByName;

                    int affectedRows = 0;

                    // Oracle command executions are not really async https://stackoverflow.com/questions/29016698/can-the-oracle-managed-driver-use-async-wait-properly/29034412#29034412
                    var runCommand = command.ExecuteNonQueryAsync();
                    affectedRows = await runCommand;

                    IEnumerable<OracleParam> outputOracleParams = null;

                    outputOracleParams = command.Parameters.Cast<OracleParam>()
                        .Where(p => p.Direction == ParameterDirection.Output);

                    return HandleDataset(outputOracleParams, affectedRows, output);
                }

            }
            catch (Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw ex;
                return new Output {Success = false, Message = ex.Message};
            }

        }

        /// <summary>
        /// Reads data using ref cursor. Connection used to get ref cursor must be still open, when using this task. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.ExecuteCommand
        /// </summary>
        /// <param name="input">The ref cursor.</param>
        /// <returns>object { bool Success, string Message, dynamic Result }</returns>
        public static Output RefCursorToJToken(RefCursorToJTokenInput input)
        {
            if (input.Refcursor.GetType() != typeof(OracleParameter))
            {
                throw new ArgumentException("Parameter 'Refcursor' must be type: OracleParameter.");
            }

            OracleDataReader dataReader = ((OracleRefCursor) input.Refcursor.Value).GetDataReader();

            var rowList = new List<Dictionary<string, object>>();

            while (dataReader.Read())
            {
                var colList = new Dictionary<string, object>();
                int i = 0;

                // Find the column names.
                foreach (DataRow row in dataReader.GetSchemaTable().Rows)
                {
                    colList.Add(row[0].ToString(), dataReader[i]);
                    i++;
                }

                rowList.Add(colList);
            }

            return new Output
            {
                Success = true,
                Result = JToken.FromObject(rowList)
            };
        }

        /// <summary>
        /// Execute query and evaluate Ref Cursor to JToken. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.ExecuteCommand
        /// </summary>
        /// <param name="input">The input data for the task</param>
        /// <param name="output">The output of the task</param>
        /// <param name="options">The options for the task</param>
        /// <returns>object { bool Success, string Message, dynamic Result }</returns>
        public async static Task<Output> ExecuteAndRefCursorToJToken([PropertyTab] Input input, [PropertyTab] OutputPropertiesWithoutDataType output, [PropertyTab] Options options)
        {
            List<OracleParametersForTask> parameters = new List<OracleParametersForTask>();
            foreach (var outputParam in output.OutputParameters)
            {
                parameters.Add(new OracleParametersForTask
                {
                    Name = outputParam.Name,
                    Value = outputParam.Value,
                    DataType = OracleParametersForTask.ParameterDataType.RefCursor,
                    Size = outputParam.Size
                });
            }
            var executeOutput = new OutputProperties
            {
                DataReturnType = OracleCommandReturnType.Parameters,
                OutputParameters = parameters.ToArray()
            };
            var connection = new OracleConnection(input.ConnectionString);
            connection.Open();
            var execute = await ExecuteWithoutLazyConnection(input, executeOutput, options, connection);
            var secondInput = new RefCursorToJTokenInput
            {
                Refcursor = execute.Result[0]
            };
            var secondResult = RefCursorToJToken(secondInput);
            connection.Close();
            return secondResult;
        }

        #region HelperFunctions

        private async static Task<Output> ExecuteWithoutLazyConnection(Input input, OutputProperties output,
            Options options, OracleConnection connection)
        {
            try
            {
                OracleCommand command = new OracleCommand(input.CommandOrProcedureName, connection)
                {
                    CommandType = (CommandType)input.CommandType,
                    CommandTimeout = input.TimeoutSeconds
                };

                if (input.InputParameters != null)
                    {
                        command.Parameters.AddRange(input.InputParameters.Select(x => CreateOracleParam(x))
                            .ToArray());
                    }

                if (output.OutputParameters != null)
                    command.Parameters.AddRange(output.OutputParameters
                        .Select(x => CreateOracleParam(x, ParameterDirection.Output)).ToArray());

                command.BindByName = input.BindParametersByName;

                int affectedRows = 0;

                // Oracle command executions are not really async https://stackoverflow.com/questions/29016698/can-the-oracle-managed-driver-use-async-wait-properly/29034412#29034412
                var runCommand = command.ExecuteNonQueryAsync();
                affectedRows = await runCommand;

                IEnumerable<OracleParam> outputOracleParams = null;

                outputOracleParams = command.Parameters.Cast<OracleParam>()
                    .Where(p => p.Direction == ParameterDirection.Output);

                return HandleDataset(outputOracleParams, affectedRows, output);
            }
            catch (Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw ex;
                return new Output { Success = false, Message = ex.Message };
            }

        }

        private static Output HandleDataset(IEnumerable<OracleParam> outputOracleParams, int affectedRows,
            OutputProperties output)
        {
            if (output.DataReturnType == OracleCommandReturnType.AffectedRows)
            {
                return new Output
                {
                    Success = true,
                    Result = affectedRows
                };
            }
            else if (output.DataReturnType == OracleCommandReturnType.Parameters)
            {
                return new Output
                {
                    Success = true,
                    Result = outputOracleParams.ToList()

                };
            }

            //Builds xml document from Oracle output parameters
            var xDoc = new XDocument();
            var root = new XElement("Root");
            xDoc.Add(root);
            outputOracleParams.ToList().ForEach(p => root.Add(ParameterToXElement(p)));

            dynamic commandResult;
            // Affected rows are handled above!
            switch (output.DataReturnType)
            {
                case OracleCommandReturnType.JSONString:
                    commandResult = JsonConvert.SerializeObject(outputOracleParams);
                    break;
                case OracleCommandReturnType.XDocument:
                    commandResult = xDoc;
                    break;
                case OracleCommandReturnType.XmlString:
                    commandResult = xDoc.ToString();
                    break;
                default:
                    throw new Exception("Unsupported DataReturnType.");
            }

            return new Output
            {
                Success = true,
                Result = commandResult
            };
        }
        
        private static OracleConnection GetLazyConnection(string connectionString)
        {
            var retretret = LazyConnectionCache.GetOrAdd(connectionString, (opts) =>
            {
                return new Lazy<OracleConnection>(
                    () =>
                    {
                        var connection = new OracleConnection(connectionString);
                        connection.Open();

                        // TODO: Add event to dispose connection, when it is closed (when timout exeeds)
                        return connection;
                    });
            });
            return retretret.Value;
        }

        private static OracleDbType ConvertParameterDataTypeToOracleDbType(OracleParametersForTask.ParameterDataType DataType)
        {
            switch (DataType)
            {
                case OracleParametersForTask.ParameterDataType.BFile:
                    return OracleDbType.BFile;
                case OracleParametersForTask.ParameterDataType.Blob:
                    return OracleDbType.Blob;
                case OracleParametersForTask.ParameterDataType.Byte:
                    return OracleDbType.Byte;
                case OracleParametersForTask.ParameterDataType.Char:
                    return OracleDbType.Char;
                case OracleParametersForTask.ParameterDataType.Clob:
                    return OracleDbType.Clob;
                case OracleParametersForTask.ParameterDataType.Date:
                    return OracleDbType.Date;
                case OracleParametersForTask.ParameterDataType.Decimal:
                    return OracleDbType.Decimal;
                case OracleParametersForTask.ParameterDataType.Double:
                    return OracleDbType.Double;
                case OracleParametersForTask.ParameterDataType.Long:
                    return OracleDbType.Long;
                case OracleParametersForTask.ParameterDataType.LongRaw:
                    return OracleDbType.LongRaw;
                case OracleParametersForTask.ParameterDataType.Int16:
                    return OracleDbType.Int16;
                case OracleParametersForTask.ParameterDataType.Int32:
                    return OracleDbType.Int32;
                case OracleParametersForTask.ParameterDataType.Int64:
                    return OracleDbType.Int64;
                case OracleParametersForTask.ParameterDataType.IntervalDS:
                    return OracleDbType.IntervalDS;
                case OracleParametersForTask.ParameterDataType.IntervalYM:
                    return OracleDbType.IntervalYM;
                case OracleParametersForTask.ParameterDataType.NClob:
                    return OracleDbType.NClob;
                case OracleParametersForTask.ParameterDataType.NChar:
                    return OracleDbType.NChar;
                case OracleParametersForTask.ParameterDataType.NVarchar2:
                    return OracleDbType.NVarchar2;
                case OracleParametersForTask.ParameterDataType.Raw:
                    return OracleDbType.Raw;
                case OracleParametersForTask.ParameterDataType.RefCursor:
                    return OracleDbType.RefCursor;
                case OracleParametersForTask.ParameterDataType.Single:
                    return OracleDbType.Single;
                case OracleParametersForTask.ParameterDataType.TimeStamp:
                    return OracleDbType.TimeStamp;
                case OracleParametersForTask.ParameterDataType.TimeStampLTZ:
                    return OracleDbType.TimeStampLTZ;
                case OracleParametersForTask.ParameterDataType.TimeStampTZ:
                    return OracleDbType.TimeStampTZ;
                case OracleParametersForTask.ParameterDataType.Varchar2:
                    return OracleDbType.Varchar2;
                case OracleParametersForTask.ParameterDataType.XmlType:
                    return OracleDbType.XmlType;
                case OracleParametersForTask.ParameterDataType.BinaryDouble:
                    return OracleDbType.BinaryDouble;
                case OracleParametersForTask.ParameterDataType.BinaryFloat:
                    return OracleDbType.BinaryFloat;
                case OracleParametersForTask.ParameterDataType.Boolean:
                    return OracleDbType.Boolean;
            }
            // you should newer reach this.
            throw new Exception("Can't convert ParameterDataType to OracleDbType");
        }

        private static OracleParam CreateOracleParam(OracleParametersForTask parameter, ParameterDirection? direction = null)
        {
            var newParam = new OracleParam()
            {
                ParameterName = parameter.Name,
                Value = parameter.Value,
                OracleDbType = ConvertParameterDataTypeToOracleDbType(parameter.DataType),
                Size = parameter.Size
            };
            if (direction.HasValue)
                newParam.Direction = direction.Value;
            return newParam;
        }

        private static XElement ParameterToXElement(OracleParam parameter)
        {
            var xelem = new XElement(parameter.ParameterName);
            if (parameter.OracleDbType == OracleDbType.Clob)
            {
                var reader = new StreamReader((Stream)parameter.Value, Encoding.Unicode);
                xelem.Value = reader.ReadToEnd();
            }
            else
            {
                xelem.Value = parameter.Value.ToString();
            }
            return xelem;
        }
        #endregion
    }
}
