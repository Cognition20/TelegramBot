using Telegram.Bot.Types.ReplyMarkups;

namespace Chulii_tg_bot.Constants;

public static class ButtonOptions
{
    public static InlineKeyboardMarkup AllOptions()
    {
        var buttons = new InlineKeyboardButton[][]
        {
            [InlineKeyboardButton.WithCallbackData(TextOptions.StudentsOption)],
            [InlineKeyboardButton.WithCallbackData(TextOptions.ItOption)],
            [InlineKeyboardButton.WithCallbackData(TextOptions.ContactsOption)],
            [InlineKeyboardButton.WithCallbackData(TextOptions.AiOption)]
        };
        
        return buttons;
    }

    public static InlineKeyboardMarkup BackOption()
    {
        var back = new InlineKeyboardButton[][]
        {
            [InlineKeyboardButton.WithCallbackData(TextOptions.BackOption)]
        };
        
        return back;
    }
}