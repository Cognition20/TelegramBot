using Chulii_tg_bot.Constants;
using Telegram.Bot;

namespace Chulii_tg_bot.Extensions;

public static class TelegramBotClientExtensions
{
    public static async Task SendTextMessageToAllOptionsAsync(this ITelegramBotClient client, 
        long chatId, 
        string text)
    {
        await client.SendMessage(
            chatId,
            text,
            replyMarkup: ButtonOptions.AllOptions()
        );
    }

    public static async Task SendTextMessageToBackOptionsAsync(this ITelegramBotClient client,
        long chatId,
        string text)
    {
        await client.SendMessage(
            chatId,
            text,
            replyMarkup: ButtonOptions.BackOption()
        );
    }
}