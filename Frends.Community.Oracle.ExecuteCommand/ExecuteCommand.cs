using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using OracleParam = Oracle.ManagedDataAccess.Client.OracleParameter;

namespace Frends.Community.Oracle.ExecuteCommand
{
    public class ExecuteCommand
    {
        public async static Task<dynamic> Execute(OracleCommandData OracleCommandData, OracleParameter[] Parameters, OracleParameter[] outputParameters, ConnectionInformation ConnectionInformation, CancellationToken cancellationToken)
        {
            using (OracleConnection oracleConnection = new OracleConnection(ConnectionInformation.ConnectionString))
            {
                await oracleConnection.OpenAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                using (OracleCommand command = new OracleCommand(OracleCommandData.CommandOrProcedureName, oracleConnection))
                {
                    command.CommandType = (CommandType)OracleCommandData.CommandType;
                    command.CommandTimeout = ConnectionInformation.TimeoutSeconds;
                    command.Parameters.AddRange(Parameters.Select(x => CreateOracleParam(x)).ToArray());
                    command.Parameters.AddRange(outputParameters.Select(x => CreateOracleParam(x, ParameterDirection.Output)).ToArray());
                    command.BindByName = OracleCommandData.BindParametersByName;


                    var runCommand = command.ExecuteNonQueryAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    int affectedRows = await runCommand;

                    var outputOracleParams = command.Parameters.Cast<OracleParam>().Where(p => p.Direction == ParameterDirection.Output);

                    // Close connection:
                    oracleConnection.Dispose();
                    oracleConnection.Close();
                    OracleConnection.ClearPool(oracleConnection);

                    if (OracleCommandData.DataReturnType == OracleCommandReturnType.AffectedRows)
                    {
                        return affectedRows;
                    }

                    //Builds xml document from Oracle output parameters
                    var xDoc = new XDocument();
                    var root = new XElement("Root");
                    xDoc.Add(root);
                    outputOracleParams.ToList().ForEach(p => root.Add(ParameterToXElement(p)));

                    if (OracleCommandData.DataReturnType == OracleCommandReturnType.XDocument)
                    {
                        return xDoc;
                    }
                    else if (OracleCommandData.DataReturnType == OracleCommandReturnType.XmlString)
                    {
                        return xDoc.ToString();
                    }
                    else if (OracleCommandData.DataReturnType == OracleCommandReturnType.JSONString)
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

    }
}
