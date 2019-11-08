using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
                return new Output { Success = false, Message = ex.Message };
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
            using (OracleConnection oracleConnection = new OracleConnection(input.ConnectionString))
            try
            {

                if (input.oracleConnectionType == OracleConnectionType.CreateNewAndCloseIt ||
                input.oracleConnectionType == OracleConnectionType.CreateNewAndKeepItAlive)
                {
                    input.oracleConnection = new OracleConnection(input.ConnectionString);
                    await input.oracleConnection.OpenAsync();
                }
                using (OracleCommand command = new OracleCommand(input.CommandOrProcedureName, input.oracleConnection))
                {
                        command.CommandType = (CommandType)input.CommandType;
                    command.CommandTimeout = input.TimeoutSeconds;
                    if (input.InputParameters != null) command.Parameters.AddRange(input.InputParameters.Select(x => CreateOracleParam(x)).ToArray());
                    if (output.OutputParameters != null) command.Parameters.AddRange(output.OutputParameters.Select(x => CreateOracleParam(x, ParameterDirection.Output)).ToArray());
                    command.BindByName = input.BindParametersByName;

                    // Oracle command executions are not really async https://stackoverflow.com/questions/29016698/can-the-oracle-managed-driver-use-async-wait-properly/29034412#29034412
                    var runCommand = command.ExecuteNonQueryAsync();
                    int affectedRows = await runCommand;

                    var outputOracleParams = command.Parameters.Cast<OracleParam>().Where(p => p.Direction == ParameterDirection.Output);

                    if (output.DataReturnType == OracleCommandReturnType.AffectedRows)
                    {
                        return new Output { Success = true, Result = affectedRows };
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

                    return new Output { Success = true, Result = commandResult };
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
                    input.oracleConnection.Dispose();
                    input.oracleConnection.Close();
                    OracleConnection.ClearPool(input.oracleConnection);
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
