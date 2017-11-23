using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OracleParam = Oracle.ManagedDataAccess.Client.OracleParameter;

namespace Frends.Community.Oracle.ExecuteCommand
{
    public class ExecuteCommand
    {
        /// <summary>
        /// Task for executing non-query commands and stored procedures in Oracle. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.ExecuteCommand
        /// </summary>
        /// <param name="InputData">The input data for the task</param>
        /// <param name="OptionData">The options for the task</param>
        /// <returns>The data returned by the query as specified by the OptionData input DataReturnType</returns>
        public async static Task<dynamic> Execute(Input InputData, Options OptionData)
        {
            using (OracleConnection oracleConnection = new OracleConnection(InputData.ConnectionString))
            {
                await oracleConnection.OpenAsync();

                using (OracleCommand command = new OracleCommand(InputData.CommandOrProcedureName, oracleConnection))
                {
                    command.CommandType = (CommandType)OptionData.CommandType;
                    command.CommandTimeout = OptionData.TimeoutSeconds;
                    if (OptionData.InputParameters != null) command.Parameters.AddRange(OptionData.InputParameters.Select(x => CreateOracleParam(x)).ToArray());
                    if (OptionData.OutputParameters != null) command.Parameters.AddRange(OptionData.OutputParameters.Select(x => CreateOracleParam(x, ParameterDirection.Output)).ToArray());
                    command.BindByName = OptionData.BindParametersByName;


                    var runCommand = command.ExecuteNonQueryAsync(); 
                    int affectedRows = await runCommand;

                    var outputOracleParams = command.Parameters.Cast<OracleParam>().Where(p => p.Direction == ParameterDirection.Output);

                    // Close connection:
                    oracleConnection.Dispose();
                    oracleConnection.Close();
                    OracleConnection.ClearPool(oracleConnection);

                    if (OptionData.DataReturnType == OracleCommandReturnType.AffectedRows)
                    {
                        return affectedRows;
                    }

                    //Builds xml document from Oracle output parameters
                    var xDoc = new XDocument();
                    var root = new XElement("Root");
                    xDoc.Add(root);
                    outputOracleParams.ToList().ForEach(p => root.Add(ParameterToXElement(p)));

                    if (OptionData.DataReturnType == OracleCommandReturnType.XDocument)
                    {
                        return xDoc;
                    }
                    else if (OptionData.DataReturnType == OracleCommandReturnType.XmlString)
                    {
                        return xDoc.ToString();
                    }
                    else if (OptionData.DataReturnType == OracleCommandReturnType.JSONString)
                    {
                        return JsonConvert.SerializeObject(outputOracleParams);
                    }
                    else
                    {
                        throw new Exception("Unsupported DataReturnType.");
                    }
                }
            }
        }

        #region HelperFunctions
        private static OracleParam CreateOracleParam(OracleParameter parameter, ParameterDirection? direction = null)
        {
            var newParam = new OracleParam()
            {
                ParameterName = parameter.Name,
                Value = parameter.Value,
                OracleDbType = (OracleDbType)(int)parameter.DataType
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
                xelem.Value = (string)parameter.Value;
            }
            return xelem;
        }
        #endregion

    }
}
