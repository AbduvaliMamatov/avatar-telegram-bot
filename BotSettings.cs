public class BotSettings
{
    public string Token { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public Dictionary<string, string> SupportedCommands { get; set; } = new();
}
