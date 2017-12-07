using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Frends.Tasks.Attributes;
using OracleParam = Oracle.ManagedDataAccess.Client.OracleParameter;

namespace Frends.Community.Oracle.ExecuteCommand
{
    public class ExecuteCommand
    {
        /// <summary>
        /// Task for executing non-query commands and stored procedures in Oracle. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.ExecuteCommand
        /// </summary>
        /// <param name="input">The input data for the task</param>
        /// <param name="output">The options for the task</param>
        /// <returns>The data returned by the query as specified by the OptionData input DataReturnType</returns>
        public async static Task<Output> Execute([CustomDisplay(DisplayOption.Tab)] Input input, 
            [CustomDisplay(DisplayOption.Tab)]OutputProperties output,
            [CustomDisplay(DisplayOption.Tab)]Options options)
        {
            try
            {
                using (OracleConnection oracleConnection = new OracleConnection(input.ConnectionString))
                {
                    await oracleConnection.OpenAsync();

                    using (OracleCommand command = new OracleCommand(input.CommandOrProcedureName, oracleConnection))
                    {
                        command.CommandType = (CommandType)input.CommandType;
                        command.CommandTimeout = input.TimeoutSeconds;
                        if (input.InputParameters != null) command.Parameters.AddRange(input.InputParameters.Select(x => CreateOracleParam(x)).ToArray());
                        if (output.OutputParameters != null) command.Parameters.AddRange(output.OutputParameters.Select(x => CreateOracleParam(x, ParameterDirection.Output)).ToArray());
                        command.BindByName = input.BindParametersByName;

                        var runCommand = command.ExecuteNonQueryAsync();
                        int affectedRows = await runCommand;

                        var outputOracleParams = command.Parameters.Cast<OracleParam>().Where(p => p.Direction == ParameterDirection.Output);

                        // Close connection:
                        oracleConnection.Dispose();
                        oracleConnection.Close();
                        OracleConnection.ClearPool(oracleConnection);

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
            }catch(Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw ex;
                return new Output { Success = false, Message = ex.Message };
            }
        }

        #region HelperFunctions
        private static OracleParam CreateOracleParam(OracleParameter parameter, ParameterDirection? direction = null)
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
