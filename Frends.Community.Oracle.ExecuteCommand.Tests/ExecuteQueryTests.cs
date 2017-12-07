using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Oracle.ExecuteCommand.Tests
{
    [TestClass]
    public class ExecuteQueryTests
    {
        string connectionString = "Data Source=localhost;User Id=<username>;Password=<password>;Persist Security Info=True;";

        [TestMethod]
        public void ExecuteOracleCommand()
        {
            /* Create test table with the following script before running the test:
             * CREATE TABLE TestTable ( textField VARCHAR(255) );
             */
            // No way to automate this test without an Oracle instance.So it's just commented out.

            //string query = "INSERT INTO TestTable (textField) VALUES ('unit test text')";

            //var output = new Output();
            //var input = new Input();

            //input.ConnectionString = connectionString;
            //input.CommandOrProcedureName = query;

            //input.CommandType = OracleCommandType.Command;
            //input.TimeoutSeconds = 60;

            //var result = ExecuteCommand.Execute(input, output);

            //Assert.AreEqual(System.Threading.Tasks.TaskStatus.RanToCompletion, result.Status);
        }

        [TestMethod]
        public void ExecuteOracleCommandWithParameters()
        {
            // No way to automate this test without an Oracle instance.So it's just commented out.

            //string query = "INSERT INTO TestTable (textField) VALUES (:param1)";

            //OracleParameter OracleParam = new OracleParameter();
            //OracleParam.DataType = OracleParameter.ParameterDataType.Varchar2;
            //OracleParam.Name = "param1";
            //OracleParam.Value = "Text from parameter";

            //var output = new Output();
            //var input = new Input();

            //input.ConnectionString = connectionString;
            //input.CommandOrProcedureName = query;

            //input.CommandType = OracleCommandType.Command;
            //input.TimeoutSeconds = 60;
            //input.InputParameters = new OracleParameter[1];
            //input.InputParameters[0] = OracleParam;           

            //var result = ExecuteCommand.Execute(input, output);

            //Assert.AreEqual(System.Threading.Tasks.TaskStatus.RanToCompletion, result.Status);
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

            //string query = "UnitTestProc";

            //OracleParameter OracleParam = new OracleParameter();
            //OracleParam.DataType = OracleParameter.ParameterDataType.Varchar2;
            //OracleParam.Name = "returnVal";
            //OracleParam.Size = 255;

            //var output = new Output();
            //var input = new Input();

            //input.ConnectionString = connectionString;
            //input.CommandOrProcedureName = query;

            //input.CommandType = OracleCommandType.StoredProcedure;
            //input.TimeoutSeconds = 60;

            //output.OutputParameters = new OracleParameter[1];
            //output.OutputParameters[0] = OracleParam;            

            //var result = ExecuteCommand.Execute(input, output);

            //Assert.AreEqual(System.Threading.Tasks.TaskStatus.RanToCompletion, result.Status);
        }
    }
}
