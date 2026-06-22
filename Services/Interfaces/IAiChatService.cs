namespace SportHub.Services.Interfaces
{
    public interface IAiChatService
    {
        Task<string> ChatAsync(string systemPrompt, List<AiChatHistoryItem> history, string userMessage, string? imageBase64 = null, string? imageMimeType = null);
    }

    public class AiChatHistoryItem
    {
        public string Role { get; set; } = string.Empty;    // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }
}
