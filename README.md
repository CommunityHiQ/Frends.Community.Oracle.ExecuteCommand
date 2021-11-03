# Frends.Community.Oracle.ExecuteCommand

FRENDS Oracle task for executing commands in a database

- [Frends.Community.Oracle.ExecuteCommand](#frendscommunityoracleexecutecommand)
- [Tasks](#tasks)
  - [Execute](#execute)
  - [RefCursorToJToken](#refcursortojtoken)
  - [ExecuteAndRefCursorToJToken](#executeandrefcursortojtoken)
- [License](#license)
- [Contributing](#contributing)
- [Changelog](#changelog)


# Tasks

### Execute

Task will execute command or stored procedure on Oracle database. Task will keep connections to database in cache, and connections remains open until timeout (defined in connection string) is reached. If connection is then needed it will be reopened. Connections are identified by connection strings. Task will support returning of ref cursors, but they are not suported in input parameters.

NOTE: the correct notation to use parameters in PL/SQL is :parameterName, not @parameterName as in T-SQL.


#### Input

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| ConnectionString | `string` | Connection string to the oracle database. | `Data Source=localhost;User Id=<userid>;Password=<password>;Persist Security Info=True;` |
| CommandType | `enum` | The type of command to execute: command or stored procedure. | `Command` |
| CommandOrProcedureName | `string` | The SQL command or stored procedure to execute. | `INSERT INTO TestTable (textField) VALUES (:param1)` |
| InputParameters | `OracleParametersForTask[]` |  Array with the oracle input parameters. | See bellow. |
| BindParametersByName | `bool` | Whether to bind the parameters by name. | `false` |
| TimeoutSeconds | `integer` | The amount of seconds to let a query run before timeout. | `60` |

#### OutputProperties

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| DataReturnType | `OracleCommandReturnType` | Specifies in what format to return the results. | `XMLDocument` |
| OutputParameters | `OracleParametersForTask[]` |  Array with the oracle input parameters. | See bellow. |


#### OracleParametersForTask

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| Name | `string` | Name of the parameter | `ParamName` |
| Value | `dynamic` | Value of the parameter | `1` |
| DataType | `enum` | Specifies the Oracle type of the parameter. Possible values are Xml string, XDocument, affected rows, JSON string and parameters (plain parameter from driver). | `NVarchar` |
| Size | `int` | Specifies the size of the parameter | `255` |

#### Options

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| ThrowErrorOnFailure | `bool` | Choose if error should be thrown if Task failes. | `false` |

#### Result

| Property/Method | Type | Description | Example |
| ---------------------| ---------------------| ----------------------- | -------- |
| Success | `boolean` | Task execution result. | true |
| Message | `string` | Failed task execution message (if `ThrowErrorOnFailure` is false). | "Connection failed" |
| Result | `dynamic` | The resultset in the format specified by the `DataReturnType` in the `OutputProperties`. | `<?xml version="1.0"?><root> <row>  <ID>0</ID>  <TABLEID>20013</TABLEID>  <FIELDNAME>AdminStatus</FIELDNAME>  <CODE>0</CODE>  <ATTRTYPE>0</ATTRTYPE>  <ACTIVEUSE>1</ACTIVEUSE>  <LANGUAGEID>fin</LANGUAGEID> </row></root>`|
 
### RefCursorToJToken

Task will read table defined by ref cursor and return data as a JToken.

Formally task has dynamic parameter, because on process level FRENDS can't include external libraries and thus use their datatypes. Ref cursor is usually aqquired by Execute task, by defining return type to parameters and then using output parameter with type RefCursor. Connection to Oracle database must be open when when using this task, or othervice ref cursor can't be used.

#### Input

| Property/Method | Type | Description | Example |
| ---------------------| ---------------------| ----------------------- | -------- |
| Refcursor | `dynamic`  | Ref cursor. Parameter must be type `OracleParameter`. | `#result.Result[0]` |


### ExecuteAndRefCursorToJToken

Combines Execute and RefCursorToJToken tasks together, so you don't have to pass the Oracle DB connection from one task to another.'

NOTE: the correct notation to use parameters in PL/SQL is :parameterName, not @parameterName as in T-SQL.


#### Input

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| ConnectionString | `string` | Connection string to the oracle database. | `Data Source=localhost;User Id=<userid>;Password=<password>;Persist Security Info=True;` |
| CommandType | `enum` | The type of command to execute: command or stored procedure. | `Command` |
| CommandOrProcedureName | `string` | The SQL command or stored procedure to execute. | `INSERT INTO TestTable (textField) VALUES (:param1)` |
| InputParameters | `OracleParametersForTask[]` |  Array with the oracle input parameters. | See bellow. |
| BindParametersByName | `bool` | Whether to bind the parameters by name. | `false` |
| TimeoutSeconds | `integer` | The amount of seconds to let a query run before timeout. | `60` |

#### OutputProperties

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| OutputParameters | `OracleParametersForTaskWithoutDataType[]` |  Array with the oracle input parameters. | See bellow. |


#### OracleParametersForTaskWithoutDataType

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| Name | `string` | Name of the parameter | `ParamName` |
| Value | `dynamic` | Value of the parameter | `1` |
| Size | `int` | Specifies the size of the parameter | `255` |

#### Options

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| ThrowErrorOnFailure | `bool` | Choose if error should be thrown if Task failes. | `false` |

#### Result

| Property/Method | Type | Description | Example |
| ---------------------| ---------------------| ----------------------- | -------- |
| Success | `boolean` | Task execution result. | true |
| Message | `string` | Failed task execution message (if `ThrowErrorOnFailure` is false). | "Connection failed" |
| Result | `dynamic` | Evaluated Ref Cursor as JToken. | [{"COL1": 1}] |

## Installing
You can install the task via FRENDS UI Task View or you can find the nuget package from the following nuget feed https://www.myget.org/F/frends-community/api/v2.

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

# Change Log

| Version             | Changes                 |
| ---------------------| ---------------------|
| 1.0.0 | Initial version of ExecuteCommand |
| 1.1.0 | Added description of return object to XML summary |
| 1.2.0 | Reverted Frends.Tasks.Attributes to 1.2.0 |
| 1.3.0 | Replaced Frends.Tasks.Attributes with System.ComponentModel |
| 2.0.0 | Task now support returning ref cursor, also connection management was added.|

