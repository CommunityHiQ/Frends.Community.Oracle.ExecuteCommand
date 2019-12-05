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
        private static readonly ConcurrentDictionary<string, OracleConnection> ConnectionCache =
            new ConcurrentDictionary<string, OracleConnection>();

        public static void ClearClientCache()
        {
            ConnectionCache.Clear();
        }

        /// <summary>
        /// Task for executing non-query commands and stored procedures in Oracle. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.ExecuteCommand
        /// </summary>
        /// <param name="input">The input data for the task</param>
        /// <param name="output">The output of the task</param>
        /// <param name="options">The options for the task</param>
        /// <returns>object { bool Success, string Message, dynamic Result }</returns>
        public async static Task<Output> Execute([PropertyTab]Input input, [PropertyTab]OutputProperties output, [PropertyTab]Options options)
        {

            try
            {
                OracleConnection Connection = null;

                // Get connection from cache, or create a new one
                Connection = GetConnection(input.ConnectionString);

                if (Connection.State != ConnectionState.Open)
                {
                    await Connection.OpenAsync();
                }

                using (OracleCommand command = new OracleCommand(input.CommandOrProcedureName, Connection))
                {
                    command.CommandType = (CommandType) input.CommandType;
                    command.CommandTimeout = input.TimeoutSeconds;

                    if (input.InputParameters != null)
                        command.Parameters.AddRange(input.InputParameters.Select(x => CreateOracleParam(x))
                            .ToArray());

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
                throw new ArgumentException("Parameter must be type: OracleParameter.");
            }

            OracleDataReader dataReader = ((OracleRefCursor) input.Refcursor.Value).GetDataReader();

            var RowList = new List<Dictionary<string, object>>();

            while (dataReader.Read())
            {
                var ColList = new Dictionary<string, object>();
                int i = 0;

                // Find the column names.
                foreach (DataRow row in dataReader.GetSchemaTable().Rows)
                {
                    ColList.Add(row[0].ToString(), dataReader[i]);
                    i++;
                }
                RowList.Add(ColList);
            }

            return new Output
            {
                Success = true,
                Result = JToken.FromObject(RowList)
            };
        }

        #region HelperFunctions
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
                    //Result = command.Parameters,
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

        private static OracleConnection GetConnection(string connectionString)
        {
            return ConnectionCache.GetOrAdd(connectionString, (opts) =>
            {
                // might get called more than once if e.g. many process instances execute at once,
                // but that should not matter much, as only one client will get cached

                var connection = new OracleConnection(connectionString);

                connection.Open();

                // TODO: Add event to dispose connection, when it is closed (when timout exeeds)

                return connection;
            });
        }

        private static OracleParam CreateOracleParam(OracleParametersForTask parameter, ParameterDirection? direction = null)
        {
            var newParam = new OracleParam()
            {
                ParameterName = parameter.Name,
                Value = parameter.Value,
                OracleDbType = (OracleDbType)(int)parameter.DataType,
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
