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
        private static readonly ConcurrentDictionary<string, OracleConnection> ConnectionCache = new ConcurrentDictionary<string, OracleConnection>();

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
        public async static Task<Output> Execute(Input input, OutputProperties output, Options options)
        {
            OracleConnection Connection = null;

            try
            {
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
                    try
                    {
                        if (input.InputParameters != null)
                            command.Parameters.AddRange(input.InputParameters.Select(x => CreateOracleParam(x)).ToArray());
                    }
                    catch (ArgumentException ex)
                    {
                        throw new ArgumentException("Can't add input parameters'", ex);
                    }

                    try
                    {
                        if (output.OutputParameters != null)
                            command.Parameters.AddRange(output.OutputParameters
                                .Select(x => CreateOracleParam(x, ParameterDirection.Output)).ToArray());
                    }

                    catch (ArgumentException ex)
                    {
                        throw new ArgumentException("Can't add output parameters'", ex);
                    }

                    try
                    {
                        command.BindByName = input.BindParametersByName;
                    }

                    catch (ArgumentException ex)
                    {
                        throw new ArgumentException("Can't bind parameters by name parameters'", ex);
                    }

                    int affectedRows = 0;
                    try
                    {
                        // Oracle command executions are not really async https://stackoverflow.com/questions/29016698/can-the-oracle-managed-driver-use-async-wait-properly/29034412#29034412
                        var runCommand = command.ExecuteNonQueryAsync();
                        affectedRows = await runCommand;

                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error in executing command.", ex);
                    }

                    System.Collections.Generic.IEnumerable<OracleParam> outputOracleParams = null;
                    try
                    {
                        outputOracleParams = command.Parameters.Cast<OracleParam>()
                            .Where(p => p.Direction == ParameterDirection.Output);

                        return HandleDataset(outputOracleParams, affectedRows, output);

                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error in executing processing returned parameters/data.", ex);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        private static Output HandleDataset(IEnumerable<OracleParam> outputOracleParams, int affectedRows, OutputProperties output)
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

        public static async Task<Output> GatAndUseRefCursor(string ConnectionString)
        {

            // Replicate of test of  https://docs.oracle.com/database/121/ODPNT/featRefCursor.htm#ODPNT319

            string connectionString = ConnectionString;
            Options _taskOptions = new Options {ThrowErrorOnFailure = true};

            //////////////////////////////////////////////////
            /// Get refcursor

            var OracleParam = new OracleParametersForTask
            {
                DataType = OracleParametersForTask.ParameterDataType.RefCursor,
                Name = "outRefPrm",
                Value = DBNull.Value,
                Size = 0
            };

            var output = new OutputProperties
            {
                DataReturnType = OracleCommandReturnType.Parameters
            };

            var input = new Input
            {
                ConnectionString = connectionString,
                CommandOrProcedureName = "begin open :1 for select col1 from test; end;",
                CommandType = OracleCommandType.Command,
                BindParametersByName = false,
                TimeoutSeconds = 60
            };

            output.OutputParameters = new OracleParametersForTask[1];
            output.OutputParameters[0] = OracleParam;

            var result = await ExecuteCommand.Execute(input, output, _taskOptions);

            //////////////////////////////////////////////////
            /// Use refcursor

            var secondInput = new Input
            {
                ConnectionString = connectionString,
                CommandOrProcedureName = "testSP",
                CommandType = OracleCommandType.StoredProcedure,
                InputParameters = new OracleParametersForTask[1],
                BindParametersByName = false,
                TimeoutSeconds = 60
            };

            OracleParametersForTask secondInputParameters = new OracleParametersForTask
            {
                DataType = OracleParametersForTask.ParameterDataType.RefCursor,
                Name = "param1",
                // Value = result.Result[0].Value,
                Value = result.Result[0].Value,
                Size = 0
            };

            secondInput.InputParameters[0] = secondInputParameters;

            var secondOutputParameters = new OracleParametersForTask
            {
                DataType = OracleParametersForTask.ParameterDataType.Int32,
                Name = "param2",
                Value = DBNull.Value,
                Size = 0
            };

            var secondOutput = new OutputProperties
            {
                OutputParameters = new OracleParametersForTask[1],
                DataReturnType = OracleCommandReturnType.XmlString
            };

            secondOutput.OutputParameters[0] = secondOutputParameters;

            var secondResult = await ExecuteCommand.Execute(secondInput, secondOutput, _taskOptions);

            return secondResult;
        }
        
        #region HelperFunctions
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
