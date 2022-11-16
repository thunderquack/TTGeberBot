using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>(),
    ThrowPendingUpdates = true
};
using var cts = new CancellationTokenSource();

TTGeberBot.TTGeberBot botClient = new TTGeberBot.TTGeberBot(File.ReadAllText("apikey.txt"));
botClient.StartReceiving(
    updateHandler: botClient.HandleUpdateAsync,
    pollingErrorHandler: botClient.HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

cts.Cancel();