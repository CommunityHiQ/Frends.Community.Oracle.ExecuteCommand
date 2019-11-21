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


#pragma warning disable 1591
namespace Frends.Community.Oracle.ExecuteCommand
{



    public class ExecuteCommand
    {
        public static string OracleRefCursorCode()
        {
            // https://docs.oracle.com/database/121/ODPNT/featRefCursor.htm#ODPNT319
            OracleConnection conn = new OracleConnection("");
            //("User Id=scott; Password=tiger; Data Source=oracle");

            conn.Open(); // Open the connection to the database

            // Command text for getting the REF Cursor as OUT parameter
            String cmdTxt1 = "begin open :1 for select col1 from test; end;";

            // Command text to pass the REF Cursor as IN parameter
            String cmdTxt2 = "begin testSP (:1, :2); end;";

            // Create the command object for executing cmdTxt1 and cmdTxt2
            OracleCommand cmd = new OracleCommand(cmdTxt1, conn);

            // Bind the Ref cursor to the PL/SQL stored procedure
            OracleParameter outRefPrm = cmd.Parameters.Add("outRefPrm",
                OracleDbType.RefCursor, DBNull.Value, ParameterDirection.Output);

            cmd.ExecuteNonQuery(); // Execute the anonymous PL/SQL block

            // Reset the command object to execute another anonymous PL/SQL block
            cmd.Parameters.Clear();
            cmd.CommandText = cmdTxt2;

            var cmd2 = new OracleCommand(cmdTxt2, conn);

            // REF Cursor obtained from previous execution is passed to this 
            // procedure as IN parameter
            OracleParameter inRefPrm = cmd2.Parameters.Add("inRefPrm",
                OracleDbType.RefCursor, outRefPrm.Value, ParameterDirection.Input);

            // Bind another Number parameter to get the REF Cursor column value
            OracleParameter outNumPrm = cmd2.Parameters.Add("outNumPrm",
                OracleDbType.Int32, DBNull.Value, ParameterDirection.Output);

            cmd2.ExecuteNonQuery(); //Execute the stored procedure

            // Display the out parameter value
            Console.WriteLine("out parameter is: " + outNumPrm.Value.ToString());

            return outNumPrm.Value.ToString();
        }

        public async static Task<Output> GatAndUseRefCursor()
        {

            string connectionString = "";
         Options _taskOptions = new Options { ThrowErrorOnFailure = true };


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
                CommandOrProcedureName = "testSP",
                oracleConnectionType = OracleConnectionType.UseExistingAndCloseIt,
                Connection = result.Connection,
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
                OutputParameters = new OracleParametersForTask[1]
            };

            secondOutput.OutputParameters[0] = secondOutputParameters;

            var secondResult = await ExecuteCommand.Execute(secondInput, secondOutput, _taskOptions);

            return secondResult;
        }



        public static string Dump(object o)
        {
            string json = JsonConvert.SerializeObject(o, Formatting.Indented);
            Console.WriteLine(json);
            return json;
        }
        /// <summary>
        /// Task for executing non-query commands and stored procedures in Oracle. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.ExecuteCommand
        /// </summary>
        /// <param name="input">The input data for the task</param>
        /// <param name="output">The output of the task</param>
        /// <param name="options">The options for the task</param>
        /// <returns>object { bool Success, string Message, dynamic Result }</returns>
        public async static Task<Output> Execute([PropertyTab] Input input,
            [PropertyTab]OutputProperties output,
            [PropertyTab]Options options)
        {
            try
            {
                return await ExecuteOracleCommand(input, output, options);
            }
            catch (Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw ex;
                return new Output
                {
                    Success = false, Message = ex.Message,
                        Connection = input.Connection
                };
            }
        }

        #region HelperFunctions
        /// <summary>
        /// Method that performs the Oracle command
        /// </summary>
        /// <param name="input">Inputs</param>
        /// <param name="output">Outputs</param>
        /// <param name="options">Options</param>
        /// <returns>object { bool Success, string Message, dynamic Result }</returns>
        private async static Task<Output> ExecuteOracleCommand(Input input, OutputProperties output, Options options)
        {
            //using (OracleConnection oracleConnection = new OracleConnection(input.ConnectionString))
            try
            {
                // Create new connection if asked to do so.
                if (input.oracleConnectionType == OracleConnectionType.CreateNewAndCloseIt ||
                input.oracleConnectionType == OracleConnectionType.CreateNewAndKeepItAlive)
                {
                    input.Connection = new OracleConnection(input.ConnectionString);

                    await input.Connection.OpenAsync();
                }
                // Otherwise check that connection is given
                else if (input.oracleConnectionType == OracleConnectionType.UseExistingAndKeepItAlive ||
                         input.oracleConnectionType == OracleConnectionType.UseExistingAndCloseIt)
                {

                    if (input.Connection == null || !(input.Connection is OracleConnection))
                    {
                        throw new Exception("Connection must be defined in parameters, when using existing connection. It must be instance of OracleConnection and can''t be null.");
                    }
                }

                using (OracleCommand command = new OracleCommand(input.CommandOrProcedureName, input.Connection))
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

                        if (output.DataReturnType == OracleCommandReturnType.AffectedRows)
                        {
                            return new Output
                            {
                                Success = true, Result = affectedRows, Connection = input.Connection
                            };
                        }
                        else if (output.DataReturnType == OracleCommandReturnType.Parameters)
                        {
                            return new Output
                            {
                                Success = true,
                                //Result = command.Parameters,
                                Result = outputOracleParams.ToList(),
                                Connection = input.Connection
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
                            Success = true, Result = commandResult,
                            Connection = input.Connection
                        };

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
            finally
            {
                if (input.oracleConnectionType == OracleConnectionType.UseExistingAndKeepItAlive ||
                    input.oracleConnectionType == OracleConnectionType.CreateNewAndKeepItAlive)
                {
                    // Close connection:
                    input.Connection.Dispose();
                    input.Connection.Close();
                    OracleConnection.ClearPool(input.Connection);
                }
            }
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
