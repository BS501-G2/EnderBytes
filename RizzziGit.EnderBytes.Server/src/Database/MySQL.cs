using System.Data.Common;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes.DatabaseWrappers;

public sealed class MySQLDatabase(MySqlConnectionStringBuilder connectionStringBuilder) : Database(connectionStringBuilder)
{
	public override string ToParameterName(string name) => $"@{name}";

	protected override DbParameter InternalCreateParameter(string name, object? value) => new MySqlParameter(name, value);
	protected override DbConnection InternalCreateConnection(string connectionString) => new MySqlConnection(connectionString);
}
