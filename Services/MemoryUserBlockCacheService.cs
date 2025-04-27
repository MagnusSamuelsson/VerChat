using Microsoft.Extensions.Caching.Memory;
using System.Data;
using VericateChat.Interfaces;
using VericateChat.Repositories;
using Dapper;

namespace VericateChat.Services
{
    public class MemoryUserBlockCacheService(DapperContext dbh) : IUserBlockCacheService
    {
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());

        private readonly DapperContext _dbh = dbh;

        public bool IsBlocked(string state)
        {
            if (_cache.TryGetValue(state, out _))
            {
                return true;
            }
            return false;
        }

        public async Task Block(string ip, string? reason = null)
        {
            _cache.Set(ip, true, TimeSpan.FromMinutes(60));
            try
            {
                reason ??= "Ingen anledning angiven";
                using IDbConnection db = _dbh.CreateConnection();
                string sql = "INSERT INTO blocked_user_log (ip, reason) VALUES (@ip, @reason)";
                var parameters = new { ip, reason };
                await db.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Misslyckades att logga blockering: {ex}");
            }
            return;
        }
    }
}
