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

        [TestMethod]
        public void ExecuteOracleStoredProcedureWithOutParam()
        {
            // Create this procedure:
            /*
             * CREATE PROCEDURE UnitTestProc (returnVal OUT VARCHAR2) AS
             * BEGIN
             * SELECT TEXTFIELD INTO returnVal FROM TESTTABLE WHERE ROWNUM = 1;
             * END;
             */
            // No way to automate this test without an Oracle instance.So it's just commented out.

            string query = "UnitTestProc";

            OracleParameter OracleParam = new OracleParameter();
            OracleParam.DataType = OracleParameter.ParameterDataType.Varchar2;
            OracleParam.Name = "returnVal";
            OracleParam.Size = 255;

            Options OracleOptions = new Options();
            Input Inputs = new Input();

            Inputs.ConnectionString = connectionString;
            Inputs.CommandOrProcedureName = query;

            OracleOptions.CommandType = OracleCommandType.StoredProcedure;
            OracleOptions.TimeoutSeconds = 60;

            OracleOptions.OutputParameters = new OracleParameter[1];
            OracleOptions.OutputParameters[0] = OracleParam;            

            var result = ExecuteCommand.Execute(Inputs, OracleOptions);

            Assert.AreEqual(System.Threading.Tasks.TaskStatus.RanToCompletion, result.Status);
        }
    }
}
