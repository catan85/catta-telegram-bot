using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;


public static class Program
{
    private static TelegramBotClient Bot;
    private static GoogleCloud googleCloud = new GoogleCloud();
    public static async Task Main()
    {

        Console.WriteLine();
        Console.WriteLine("GetEnvironmentVariables: ");
        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            Console.WriteLine("  {0} = {1}", de.Key, de.Value);

        string googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_CONTENT");
        string googleApiKeyStoragePath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        System.IO.File.WriteAllText(googleApiKeyStoragePath, googleApiKey);


        Bot = new TelegramBotClient(Configuration.BotToken);


        var me = await Bot.GetMeAsync();
        Console.Title = me.Username;

        Bot.OnMessage += BotOnMessageReceived;
        Bot.OnMessageEdited += BotOnMessageReceived;
        Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
        Bot.OnInlineQuery += BotOnInlineQueryReceived;
        Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
        Bot.OnReceiveError += BotOnReceiveError;

        Bot.StartReceiving(Array.Empty<UpdateType>());
        Console.WriteLine($"Start listening for @{me.Username}");

        System.Timers.Timer t = new System.Timers.Timer(60000);
        t.Elapsed += T_Elapsed;
        Console.ReadLine();
        Bot.StopReceiving();
    }

    private static void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        AnotherHourMessageToCattaGroup();
    }

    private static void AnotherHourMessageToCattaGroup()
    {
        Bot.SendTextMessageAsync(-1001194124577, "Another hour has passed..");
    }

    private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
    {
        var message = messageEventArgs.Message;

        if (message != null && message.Type == MessageType.Voice)
        {
            await ManageVoiceMessage(message);
        }

        if (message != null && message.Type == MessageType.Text)
        {
            await ManageTextMessage(message);
        }

        if (message == null || message.Type != MessageType.Text)
            return;

    }

    private static async Task ManageTextMessage(Message message)
    {
        switch (message.Text.Split(' ').First())
        {
            // Send inline keyboard
            case "/inline":
                await SendInlineKeyboard(message);
                break;

            // send custom keyboard
            case "/keyboard":
                await SendReplyKeyboard(message);
                break;

            // send a photo
            case "/photo":
                await SendDocument(message);
                break;

            // request location or contact
            case "/request":
                await RequestContactAndLocation(message);
                break;

            default:
                await Other(message);
                break;
        }
    }


    private static async Task ManageVoiceMessage(Message message)
    {
        Console.WriteLine("Received voice message:" + message.Voice.FileId);

        var client = new RestClient($"https://api.telegram.org/bot{Configuration.BotToken}");
        var request = new RestRequest("getFile", Method.GET);
        request.AddQueryParameter("file_id", message.Voice.FileId);
        var queryResult = client.Execute(request);
        var fileInfo = JObject.Parse(queryResult.Content);

        var getFileUrl = $"https://api.telegram.org/file/bot{Configuration.BotToken}/{fileInfo["result"]["file_path"].ToString()}";
        var client2 = new RestClient(getFileUrl);
        var request2 = new RestRequest("", Method.GET);
        request2.AddQueryParameter("file_id", message.Voice.FileId);

        var queryResult2 = client2.Execute(request2);

        if (!Directory.Exists(Configuration.TempFileFolder))
        {
            Directory.CreateDirectory(Configuration.TempFileFolder);
        }

        string fullFileName = $"{Configuration.TempFileFolder}{fileInfo["result"]["file_path"].ToString().Replace("/", "_")}";

        queryResult2.RawBytes.SaveAs(fullFileName);
        WaveConverter conv = new WaveConverter();


        var filePath = System.IO.Path.GetDirectoryName(fullFileName)+"\\";
        var fileOgg = fullFileName;
        var fileWav = $"{filePath}{System.IO.Path.GetFileNameWithoutExtension(fileOgg)}.wav";

        conv.ConvertToWave(fileOgg, fileWav);

        googleCloud.UploadFileToGoogleStorage(fileWav);

        string voiceText = googleCloud.AudioFileToText(System.IO.Path.GetFileName(fileWav));
        Console.WriteLine(voiceText);

        System.IO.File.Delete(fileOgg);
        System.IO.File.Delete(fileWav);
        googleCloud.DeleteFileFromGoogleStorage(System.IO.Path.GetFileName(fileWav));


        await Bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"{message.From.FirstName} ha detto:\n" + voiceText,
            replyMarkup: new ReplyKeyboardRemove());

        if (voiceText.Contains("film"))
        {
            await SendFilmInformations(message, voiceText);
        }

    }

    // Send inline keyboard
    // You can process responses in BotOnCallbackQueryReceived handler
    static async Task SendInlineKeyboard(Message message)
    {
        await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

        // Simulate longer running task
        await Task.Delay(500);

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
                // first row
                new []
                {
                    InlineKeyboardButton.WithCallbackData("1.1", "11"),
                    InlineKeyboardButton.WithCallbackData("1.2", "12"),
                },
                // second row
                new []
                {
                    InlineKeyboardButton.WithCallbackData("2.1", "21"),
                    InlineKeyboardButton.WithCallbackData("2.2", "22"),
                }
            });
        await Bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Choose",
            replyMarkup: inlineKeyboard
        );
    }

    static async Task SendReplyKeyboard(Message message)
    {
        var replyKeyboardMarkup = new ReplyKeyboardMarkup(
            new KeyboardButton[][]
            {
                    new KeyboardButton[] { "1.1", "1.2" },
                    new KeyboardButton[] { "2.1", "2.2" },
            },
            resizeKeyboard: true
        );

        await Bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Choose",
            replyMarkup: replyKeyboardMarkup

        );
    }

    static async Task SendDocument(Message message)
    {
        await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

        const string filePath = @"Files/tux.png";
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
        await Bot.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: new InputOnlineFile(fileStream, fileName),
            caption: "Nice Picture"
        );
    }

    static async Task RequestContactAndLocation(Message message)
    {
        var RequestReplyKeyboard = new ReplyKeyboardMarkup(new[]
        {
                KeyboardButton.WithRequestLocation("Location"),
                KeyboardButton.WithRequestContact("Contact"),
            });
        await Bot.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Who or Where are you?",
            replyMarkup: RequestReplyKeyboard
        );
    }

    static async Task Other(Message message)
    {
        string lowerCaseMessage = message.Text.ToLower();

        if (lowerCaseMessage.Contains("help") ||
            lowerCaseMessage.Contains("aiuto") ||
            lowerCaseMessage.Contains("comandi") ||
            lowerCaseMessage.Contains("bot"))
        {
            const string usage = "Sono il tuo umile servitore, questi sono i comandi dispobili:\n" +
                    "/inline   - ti richiederò di inserire qualcosa con una tastiera inline\n" +
                    "/keyboard - ti richiederò di inserire qualcosa con una tastiera custom\n" +
                    "/photo    - ti mando un'immagine\n" +
                    "/request  - ti richiederò la posizione o il tuo contatto";
            await Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove());
        }

        if (lowerCaseMessage.Contains("film"))
        {
            await SendFilmInformations(message, lowerCaseMessage);
        }
    }


    private static async Task SendFilmInformations(Message message, string lowerCaseMessage)
    {
        var titolo = "";
        if (lowerCaseMessage.Contains("films"))
        {
            var posTitolo = lowerCaseMessage.IndexOf("films");
            titolo = lowerCaseMessage.Substring(posTitolo + 6);
        }
        else
        {
            var posTitolo = lowerCaseMessage.IndexOf("film");
            titolo = lowerCaseMessage.Substring(posTitolo + 5);
        }

        List<string> ids = OmdbApi.SearchImdbIds(titolo);
        string filmDataString = "";
        foreach (string id in ids.Distinct())
        {
            JObject filmData = OmdbApi.GetFilmData(id);

            filmDataString += string.Format(@$"
----
Titolo: {filmData["Title"]}
Anno:   {filmData["Year"]}
Durata: {filmData["Runtime"]}
");
            if (filmData.ContainsKey("Ratings") && filmData["Ratings"].HasValues)
            {
                foreach (var rating in filmData["Ratings"])
                {
                    filmDataString += rating["Source"] + "\t" + rating["Value"] + "\n";
                    filmDataString = filmDataString.Replace("Internet Movie Database", "IMDB");
                    filmDataString = filmDataString.Replace("Rotten Tomatoes", "ROTT");
                    filmDataString = filmDataString.Replace("Metacritic", "METC");
                }
            }
        }

        await Bot.SendTextMessageAsync(
        chatId: message.Chat.Id,
        text: filmDataString,
        replyMarkup: new ReplyKeyboardRemove());
    }


    // Process Inline Keyboard callback data
    private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
    {
        var callbackQuery = callbackQueryEventArgs.CallbackQuery;

        await Bot.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}"
        );

        await Bot.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: $"Received {callbackQuery.Data}"
        );
    }

    #region Inline Mode

    private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
    {
        Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

        InlineQueryResultBase[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "3",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent(
                    "hello"
                )
            )
        };
        await Bot.AnswerInlineQueryAsync(
            inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
            results: results,
            isPersonal: true,
            cacheTime: 0
        );
    }

    private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
    {
        Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
    }

    #endregion

    private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
    {
        Console.WriteLine("Received error: {0} — {1}",
            receiveErrorEventArgs.ApiRequestException.ErrorCode,
            receiveErrorEventArgs.ApiRequestException.Message
        );
    }
}