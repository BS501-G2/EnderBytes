using System.Net;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes.Core;

public sealed partial class Server
{
	public sealed record HttpsClientPort(
	string CertificatePath,
	string CertificatePassword,
	int Port = 8443
	);

	public sealed record ServerConfiguration(
	MySqlConnectionStringBuilder DatabaseConnectionStringBuilder,

	string? WorkingPath = null,

	int KeyGeneratorThreads = 4,
	int MaxPregeneratedKeyCount = 1000,

	int HttpClientPort = 8080,
	HttpsClientPort? HttpsClient = null
	)
	{
		public string WorkingPath = WorkingPath ?? Path.Join(Environment.CurrentDirectory, ".EnderBytes");
	}
}
