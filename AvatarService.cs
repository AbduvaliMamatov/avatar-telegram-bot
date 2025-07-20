using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace TelegramBot;

public class AvatarService
{
    private readonly HttpClient httpClient;
    private readonly string baseUrl;
    private readonly ILogger<AvatarService> logger;

    public AvatarService(HttpClient httpClient, IOptions<BotSettings> options, ILogger<AvatarService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
        baseUrl = options.Value.ApiBaseUrl.TrimEnd('/');
    }

    public async Task<Stream> GetAvatarAsync(
        string style,
        string seed,
        string format,
        string? backgroundColor,
        CancellationToken cancellationToken)
    {
        // DiceBear API v8: /{style}/{format}?seed={seed}
        var url = $"{baseUrl}/{style}/{format}?seed={Uri.EscapeDataString(seed)}";

        if (!string.IsNullOrWhiteSpace(backgroundColor))
        {
            url += $"&backgroundColor={backgroundColor}";
        }
        else if (style.Equals("avataaars", StringComparison.OrdinalIgnoreCase))
        {
            url += "&transparent=true";
        }

        logger.LogInformation("Emoji so‘rovi URL: {Url}", url);

        var response = await httpClient.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}
