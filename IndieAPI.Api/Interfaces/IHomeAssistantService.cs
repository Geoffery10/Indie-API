namespace IndieAPI.Api.Interfaces;

public interface IHomeAssistantService
{
    Task<string> GetDailyVerseAsync();
}