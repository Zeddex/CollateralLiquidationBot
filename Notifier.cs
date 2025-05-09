public class Notifier
{
    private readonly string? _telegramBotToken = Environment.GetEnvironmentVariable("TG_BOT_TOKEN");
    private readonly string? _chatId = Environment.GetEnvironmentVariable("TG_CHAT_ID");

    public async Task SendMessageAsync(string message)
    {
        using var httpClient = new HttpClient();

        var url = $"https://api.telegram.org/bot{_telegramBotToken}/sendMessage";

        var data = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("chat_id", _chatId),
            new KeyValuePair<string, string>("text", message),
            new KeyValuePair<string, string>("parse_mode", "Markdown")
        ]);

        try
        {
            var response = await httpClient.PostAsync(url, data);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Telegram Error: {ex.Message}");
        }
    }
}