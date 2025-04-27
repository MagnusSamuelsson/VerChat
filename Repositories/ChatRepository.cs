using System.Data;
using Dapper;
using MySqlConnector;
using Newtonsoft.Json;
using VericateChat.Interfaces;
using VericateChat.Models;

namespace VericateChat.Repositories
{
    public class ChatRepository(DapperContext dbh) : IChatRepository
    {
        private readonly DapperContext _dbh = dbh;

        public async Task<SqlResult> RunQueryAsync(string sql)
        {
            if (!IsAllowedSqlQuery(sql))
            {
                return SqlResult.Failure("Ogiltig SQL-fråga.");
            }
            try
            {
                using IDbConnection conn = _dbh.CreateConnection();
                var trimmed = sql.TrimStart().ToUpperInvariant();
                var rows = (await conn.QueryAsync(sql)).ToList();
                string? jsonData = JsonConvert.SerializeObject(rows);
                return new SqlResult(success: true, jsonData);
            }
            catch (MySqlException ex) when (ex.Number == 1064)
            {
                return SqlResult.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                return SqlResult.Failure(ex.Message);
            }
        }

        private static bool IsAllowedSqlQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false;

            sql = sql.TrimStart().ToUpperInvariant();

            string[] allowedStarts = ["SELECT", "SHOW", "DESCRIBE", "EXPLAIN"];

            return allowedStarts.Any(prefix => sql.StartsWith(prefix));
        }
    }
}
