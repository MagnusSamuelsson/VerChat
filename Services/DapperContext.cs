using MySqlConnector;
using System.Data;

namespace VericateChat.Repositories
{
    public class DapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") ?? "";
        }

        public IDbConnection CreateConnection()
            => new MySqlConnection(_connectionString);
    }
}
