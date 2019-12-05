using System;
using System.Collections;
using NUnit.Framework;
using TestConfigurationHandler;
using Oracle.ManagedDataAccess.Client;
using OracleParam = Oracle.ManagedDataAccess.Client.OracleParameter;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Frends.Community.Oracle.ExecuteCommand.Tests
{
    [TestFixture]
    //[Ignore("No way to automate this test without an Oracle instance.")]
    public class ExecuteQueryTests
    {
        //string connectionString = "Data Source=localhost;User Id=SYSTEM;Password=salasana1;Persist Security Info=True;";
        private string connectionString = ConfigHandler.ReadConfigValue("HiQ.OracleDb.connectionString");
        private Options _taskOptions;

        [SetUp]
        public void TestSetupAsync()
        {
            _taskOptions = new Options { ThrowErrorOnFailure = true };
        }

        /// <summary>
        /// Creates a table that is used in other tests.
        /// </summary>
        [Test, Order(5)]
        public async System.Threading.Tasks.Task CreateTable()
        {
            var query = "CREATE TABLE TestTable(textField VARCHAR(255))";

            var output = new OutputProperties();
            var input = new Input
            {
                ConnectionString = connectionString,
                CommandOrProcedureName = query,

                CommandType = OracleCommandType.Command,
                TimeoutSeconds = 60
            };
            var result = await ExecuteCommand.Execute(input, output, _taskOptions);

            Assert.AreEqual(true, result.Success);
        }

        /// <summary>
        /// Creates a stored procedure UnitTestProc that is used in other tests.
        /// </summary>
        [Test, Order(10)]
        public async System.Threading.Tasks.Task CreateProcedure()
        {
            var query = @"
                            CREATE PROCEDURE UnitTestProc (returnVal OUT VARCHAR2) AS
                            BEGIN
                            SELECT TEXTFIELD INTO returnVal FROM TESTTABLE WHERE ROWNUM = 1;
                            END;";

            var output = new OutputProperties();
            var input = new Input
            {
                ConnectionString = connectionString,
                CommandOrProcedureName = query,

                CommandType = OracleCommandType.Command,
                TimeoutSeconds = 60
            };

            var result = await ExecuteCommand.Execute(input, output, _taskOptions);

            Assert.AreEqual(true, result.Success);
        }

        /// <summary>
        /// Insert data to the TestTable.
        /// </summary>
        [Test, Order(15)]
        public async System.Threading.Tasks.Task InsertValues()
        {
            var query = "INSERT INTO TestTable (textField) VALUES ('unit test text')";

            var output = new OutputProperties();
            var input = new Input
            {
                ConnectionString = connectionString,
                CommandOrProcedureName = query,

                CommandType = OracleCommandType.Command,
                TimeoutSeconds = 60
            };
            var result = await ExecuteCommand.Execute(input, output, _taskOptions);

            Assert.AreEqual(true, result.Success);
        }

        /// <summary>
        /// Insert data to the TestTable using task parameters.
        /// </summary>
        [Test, Order(15)]
        public async System.Threading.Tasks.Task InsertValuesViaParametersAsync()
        {
            var query = "INSERT INTO TestTable (textField) VALUES (:param1)";

            OracleParametersForTask ownOracleParam = new OracleParametersForTask
            {
                DataType = OracleParametersForTask.ParameterDataType.Varchar2,
                Name = "param1",
                Value = "Text from parameter"
            };

            var input = new Input
            {
                ConnectionString = connectionString,
                CommandOrProcedureName = query,

                CommandType = OracleCommandType.Command,
                TimeoutSeconds = 60,
                InputParameters = new OracleParametersForTask[1]
            };
            input.InputParameters[0] = ownOracleParam;

            var output = new OutputProperties();

            var result = await ExecuteCommand.Execute(input, output, _taskOptions);

            Assert.AreEqual(true, result.Success);
        }

        /// <summary>
        /// Execute stored procedure UnitTestProc.
        /// </summary>
        [Test, Order(20)]
        public async System.Threading.Tasks.Task ExecuteStoredProcedureWithOutputParamAsync()
        {
            var query = "UnitTestProc";


            var input = new Input
            {
                ConnectionString = connectionString,
                CommandOrProcedureName = query,

                CommandType = OracleCommandType.StoredProcedure,
                TimeoutSeconds = 60
            };

            var OracleParam = new OracleParametersForTask
            {
                DataType = OracleParametersForTask.ParameterDataType.Varchar2,
                Name = "returnVal",
                Size = 255
            };

            var output = new OutputProperties();

            output.OutputParameters = new OracleParametersForTask[1];
            output.OutputParameters[0] = OracleParam;

            var result = await ExecuteCommand.Execute(input, output, _taskOptions);

            Assert.AreEqual(true, result.Success);
        }

        /// <summary>
        /// Get ref cursor and pass it to another task.
        /// </summary>
        [Test, Order(20)]
        public async System.Threading.Tasks.Task OracleRefCursorCode()
        {
            
            OracleConnection conn = new OracleConnection(connectionString);
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
        }

        /// <summary>
        /// Get ref cursor and pass it to another task.
        /// </summary>
        [Test, Order(20)]
        public async Task GatAndUseRefCursor()
        {

            // Replicate of test of  https://docs.oracle.com/database/121/ODPNT/featRefCursor.htm#ODPNT319

            // create table and procedure:
            /*
            connect scott/tiger@oracle
            create table test (col1 number);
            insert into test(col1) values (1);
            commit;
             
            create or replace package testPkg as type empCur is REF Cursor;
            end testPkg;
            /
             
            create or replace procedure testSP(param1 IN testPkg.empCur, param2 OUT NUMBER)
            as
            begin
            FETCH param1 into param2;
            end;
            /
            */

            // Note this kind of usage of refcursors don't work in frends.

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

            Assert.AreEqual("<Root>\r\n  <param2>1</param2>\r\n</Root>", secondResult.Result);
        }

        /*
        /// <summary>
        /// TOFO
        /// </summary>
        [Test, Order(20)]
        public async System.Threading.Tasks.Task GatAndUseRefCursorToJtoken()
        {

            // Replicate of test of  https://docs.oracle.com/database/121/ODPNT/featRefCursor.htm#ODPNT319

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

            var refOpt = new RefCursorToJTokenOptions
            {
                PathToRefCursor = "result.Result[0]",
                IndexOfRefcursor = 0
            };

            var result = await ExecuteCommand.RefCursorToJToken(input, output, _taskOptions, refOpt);


            //var resultSuper = ExecuteCommand.RefCursorToJTokenHelper(result.Result[0]);

        }
        */

        /// <summary>
        /// Get ref cursor and pass it to another task.
        /// </summary>
        [Test, Order(20)]
        public async System.Threading.Tasks.Task GatAndUseRefCursorToJtoken()
        {

            // Replicate of test of  https://docs.oracle.com/database/121/ODPNT/featRefCursor.htm#ODPNT319

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

            var secondInput = new RefCursorToJTokenInput
            {
                Refcursor = result.Result[0]
            };

            var seconResult = ExecuteCommand.RefCursorToJToken(secondInput);
            
            StringAssert.Contains(@"[{""COL1"":1.0}]", JsonConvert.SerializeObject(seconResult.Result));
        }

        /// <summary>
        /// Drop TestTable and UnitTestProc.
        /// </summary>
        [Test, Order(50)]
        public async System.Threading.Tasks.Task TestTearDownAsync()
        {
            var query = "DROP TABLE TestTable";
            var output = new OutputProperties();
            var input = new Input
            {
                ConnectionString = connectionString,
                CommandOrProcedureName = query,

                CommandType = OracleCommandType.Command,
                TimeoutSeconds = 60
            };
            var result = await ExecuteCommand.Execute(input, output, _taskOptions);

            Assert.AreEqual(true, result.Success);

            input.CommandOrProcedureName =
                "DROP PROCEDURE UnitTestProc";

            result = await ExecuteCommand.Execute(input, output, _taskOptions);

            Assert.AreEqual(true, result.Success);
        }

    }
}
