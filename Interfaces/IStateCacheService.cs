namespace VericateChat.Interfaces
{
    public interface IStateCacheService
    {
        bool IsDuplicate(string state);
    }
}
