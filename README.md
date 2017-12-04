# Frends.Community.Oracle.ExecuteCommand
FRENDS4 Oracle task for executing commands in a database

## Installing
You can install the task via FRENDS UI Task View or you can find the nuget package from the following nuget feed
'Insert nuget feed here'

## Building
Clone a copy of the repo

`git clone https://github.com/CommunityHiQ/Frends.Community.Oracle.ExecuteCommand`

Restore dependencies

`nuget restore frends.community.oracle.executecommand`

Rebuild the project

Run Tests with nunit3. Tests can be found under

`Frends.Community.Oracle.QueryData.Tests\bin\Release\Frends.Community.Oracle.ExecuteCommand.Tests`

Create a nuget package

`nuget pack nuspec/Frends.Community.Oracle.ExecuteCommand.nuspec`

## Contributing
When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

1. Fork the repo on GitHub
2. Clone the project to your own machine
3. Commit changes to your own branch
4. Push your work back up to your fork
5. Submit a Pull request so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!

## Documentation

### Task Properties

#### Input

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| ConnectionString | string | Connection string to the oracle database | Data Source=localhost;User Id=<userid>;Password=<password>;Persist Security Info=True; |
| CommandOrProcedureName | string | The SQL command or stored procedure to execute | INSERT INTO TestTable (textField) VALUES (:param1) |
| CommandType | enum | The type of command to execute: command or stored procedure | Command |
| DataReturnType | OracleCommandReturnType | Specifies in what format to return the results | XMLDocument |
| BindParametersByName | bool | Whether to bind the parameters by name | false |
| TimeoutSeconds | integer | The amount of seconds to let a query run before timeout | 666 |
| InputParameters | OracleParameter[] |  Array with the oracle input parameters | n/a |
| OutputParameters | OracleParameter[] |  Array with the oracle input parameters | n/a |

NOTE: the correct notation to use parameters in PL/SQL is :parameterName, not @parameterName as in T-SQL. See example query above.

#### OracleParameter

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| Name | string | Name of the parameter | ParamName |
| Value | dynamic | Value of the parameter | 1 |
| DataType | ParameterDataType | Specifies the Oracle type of the parameter using the ParameterDataType enumeration | NVarchar |
| Size | int | Specifies the size of the parameter | 255 |

### Oracle.ExecuteCommand.Execute

#### Example usage

#### Result

| Property/Method | Type | Description | Example |
| ---------------------| ---------------------| ----------------------- | -------- |
| Success | boolean | Task execution result. | true |
| Message | string | Failed task execution message (if throw exception on error is false). | "Connection failed" |
| Result | variable | The resultset in the format specified in the Options of the input | <?xml version="1.0"?><root> <row>  <ID>0</ID>  <TABLEID>20013</TABLEID>  <FIELDNAME>AdminStatus</FIELDNAME>  <CODE>0</CODE>  <ATTRTYPE>0</ATTRTYPE>  <ACTIVEUSE>1</ACTIVEUSE>  <LANGUAGEID>fin</LANGUAGEID> </row></root>|
