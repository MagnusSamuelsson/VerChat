namespace VericateChat.Interfaces
{
    public interface IUserBlockCacheService
    {
        bool IsBlocked(string state);
        Task Block(string state, string? reason = null);
    }
}
