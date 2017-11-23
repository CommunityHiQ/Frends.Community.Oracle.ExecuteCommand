using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Oracle.ExecuteCommand.Tests
{
    [TestClass]
    public class ExecuteQueryTests
    {
        string connectionString = "Data Source=localhost;User Id=SYSTEM;Password=salasana1;Persist Security Info=True;";

        [TestMethod]
        public void ExecuteOracleCommand()
        {
            // No way to automate this test without an Oracle instance.So it's just commented out.

            string query = "INSERT INTO TestTable (textField) VALUES ('unit test text')";

            Options Options = new Options();
            Input Inputs = new Input();

            Inputs.ConnectionString = connectionString;
            Inputs.CommandOrProcedureName = query;

            Options.CommandType = OracleCommandType.Command;
            Options.TimeoutSeconds = 60;

            var result = ExecuteCommand.Execute(Inputs, Options);

            Assert.AreEqual(System.Threading.Tasks.TaskStatus.RanToCompletion, result.Status);
        }

        [TestMethod]
        public void ExecuteOracleCommandWithParameters()
        {
            // No way to automate this test without an Oracle instance.So it's just commented out.

            string query = "INSERT INTO TestTable (textField) VALUES (:param1)";

            OracleParameter OracleParam = new OracleParameter();
            OracleParam.DataType = OracleParameter.ParameterDataType.Varchar2;
            OracleParam.Name = "param1";
            OracleParam.Value = "Text from parameter";

            Options Options = new Options();
            Input Inputs = new Input();

            Inputs.ConnectionString = connectionString;
            Inputs.CommandOrProcedureName = query;

            Options.CommandType = OracleCommandType.Command;
            Options.TimeoutSeconds = 60;
            Options.InputParameters = new OracleParameter[1];
            Options.InputParameters[0] = OracleParam;           

            var result = ExecuteCommand.Execute(Inputs, Options);

            Assert.AreEqual(System.Threading.Tasks.TaskStatus.RanToCompletion, result.Status);
        }
    }
}
