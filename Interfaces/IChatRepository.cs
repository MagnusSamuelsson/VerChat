using VericateChat.Models;

namespace VericateChat.Interfaces
{
    public interface IChatRepository
    {
        Task<SqlResult> RunQueryAsync(string sql);
    }
}
