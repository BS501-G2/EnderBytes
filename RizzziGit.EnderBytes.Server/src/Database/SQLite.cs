// using System.Data.Common;
// using System.Data.SQLite;

// namespace RizzziGit.EnderBytes.DatabaseWrappers;

// public sealed class SQLiteDatabase(SQLiteConnectionStringBuilder connectionStringBuilder) : Database(connectionStringBuilder)
// {
//	public override string ToParameterName(string name) => $"${name}";

//	protected override DbParameter InternalCreateParameter(string name, object? value) => new SQLiteParameter(name, value);
//	protected override DbConnection InternalCreateConnection(string connectionString) => new SQLiteConnection(connectionString);
// }
