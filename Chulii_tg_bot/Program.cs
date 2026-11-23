using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chulii_tg_bot.Constants;
using Chulii_tg_bot.Extensions;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Chulii_tg_bot;

public class Program
{
    private static TelegramBotClient _botClient = null!;
    private static string _tgBotAccessKey = string.Empty;
    private static readonly HttpClient Gemini = new() { BaseAddress = new Uri("https://generativelanguage.googleapis.com/"), Timeout = TimeSpan.FromSeconds(40) };
    private static string _geminiApiKey = string.Empty;
    private const string GeminiModel = "gemini-3.1-flash-lite";

    private static async Task Main(string[] args)
    {
        InitializeConfigurationKeys();

        _botClient = await StartTelegramBot();

        await Task.Delay(Timeout.Infinite); // працює і на сервері без консолі
    }

    private static void InitializeConfigurationKeys()
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>()
            .Build();

        _tgBotAccessKey = config["TelegramBotAccessKey"]
            ?? throw new InvalidOperationException("Не задано TelegramBotAccessKey");
        _geminiApiKey  = config["Gemini_ApiKey"]
            ?? throw new InvalidOperationException("Не задано Gemini_ApiKey");
    }

    private static async Task<TelegramBotClient> StartTelegramBot()
    {
        var bot = new TelegramBotClient(_tgBotAccessKey);

        await bot.SetMyCommands(new List<BotCommand>
        {
            new() { Command = TextOptions.StartOption, Description = TextOptions.WelcomeMessage }
        });

        bot.OnError += HandleErrorAsync;
        bot.OnUpdate += HandleUpdateAsync;
        bot.OnMessage += HandleMessageAsync;

        Console.WriteLine("Bot is up and running.");
        return bot;
    }

    private static async Task HandleMessageAsync(Message message, UpdateType type)
    {
        if (type != UpdateType.Message || message.Text is null) return;
        var chatId = message.Chat.Id;

        if (message.Text == TextOptions.StartOption)
        {
            await _botClient.SendTextMessageToAllOptionsAsync(chatId, TextOptions.WelcomeMessage);
            return;
        }

        string answer;
        try
        {
            await _botClient.SendChatAction(chatId, ChatAction.Typing);
            answer = await AskAiAsync(message.Text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI error: {ex.Message}");
            answer = "Не вдалося отримати відповідь від AI. Спробуйте пізніше.";
        }

        await _botClient.SendTextMessageToBackOptionsAsync(chatId, answer);
    }

    private static async Task<string> AskAiAsync(string userText)
    {
        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = userText } } } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{GeminiModel}:generateContent")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", _geminiApiKey);

        var resp = await Gemini.SendAsync(request);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{(int)resp.StatusCode}: {body}");

        var text = JsonNode.Parse(body)?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
                   ?? "(порожня відповідь)";
        return text.Length > 4000 ? text[..4000] + "…" : text;
    }

    private static async Task HandleUpdateAsync(Update update)
    {
        var callbackQuery = update.CallbackQuery;
        if (callbackQuery?.Message is null) return;

        var chatId = callbackQuery.Message.Chat.Id;

        var task = callbackQuery.Data switch
        {
            TextOptions.StudentsOption => _botClient.SendTextMessageToBackOptionsAsync(chatId, TextOptions.StudentsOptionAnswer),
            TextOptions.ItOption       => _botClient.SendTextMessageToBackOptionsAsync(chatId, TextOptions.ItOptionAnswer),
            TextOptions.ContactsOption => _botClient.SendTextMessageToBackOptionsAsync(chatId, TextOptions.ContactsOptionAnswer),
            TextOptions.AiOption   => _botClient.SendMessage(chatId, TextOptions.MessageToAi),
            TextOptions.BackOption     => _botClient.SendTextMessageToAllOptionsAsync(chatId, TextOptions.WelcomeMessage),
            _ => Task.CompletedTask
        };
        await task;

        await _botClient.AnswerCallbackQuery(callbackQuery.Id);
    }

    private static Task HandleErrorAsync(Exception exception, HandleErrorSource source)
    {
        Console.WriteLine($"Something went wrong: {exception.Message}");
        return Task.CompletedTask;
    }
}