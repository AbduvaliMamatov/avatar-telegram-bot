namespace TelegramBot;

public class AvatarService
{
    private readonly HttpClient _httpClient;

    public AvatarService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Stream> GetAvatarAsync(string style, string seed, CancellationToken cancellationToken)
    {
        var url = $"https://api.dicebear.com/8.x/{style}/png?seed={Uri.EscapeDataString(seed)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}
