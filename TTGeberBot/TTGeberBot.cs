using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace TTGeberBot
{
    internal class TTGeberBot : TelegramBotClient
    {
        private List<string> PATTERN = new List<string>() { "tiktok" };

        public TTGeberBot(string token, HttpClient? httpClient = null) : base(token, httpClient)
        {
        }

        private bool getTiktokUrl(Uri videoUrl, out string downloadUrl)
        {
            try
            {
                HttpClient cl = new HttpClient();
                cl.DefaultRequestHeaders.Add(
                       "accept-encoding", "utf-8");
                cl.DefaultRequestHeaders.Add(
                       "User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36");
                var res = cl.GetAsync(videoUrl).Result;
                string src = res.Content.ReadAsStringAsync().Result;

                Regex re = new(@"<script id=""SIGI_STATE"" .*?>(.*?)</script>");
                if (!re.IsMatch(src)) throw new Exception("Ссылка не найдена");

                string innerurl = re.Match(src).Groups[1].Value;
                var doc = JsonDocument.Parse(innerurl);

                var keyword = doc.RootElement.GetProperty("ItemList").GetProperty("video").GetProperty("keyword").GetString();
                var url = doc.RootElement.GetProperty("ItemModule").GetProperty(keyword).GetProperty("video").GetProperty("downloadAddr").GetString();

                string outp = Encoding.ASCII.GetString(Encoding.Convert(Encoding.UTF8, Encoding.ASCII, Encoding.UTF8.GetBytes(url)));

                downloadUrl = outp;
                return true;
            }
            catch
            {
                downloadUrl = "";
                return false;
            }
        }

        private string getFirstUrlWithPattern(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            text = text.ToLower();
            foreach (string st in PATTERN)
            {
                string pattern = st.ToLower();
                int pos = text.IndexOf(pattern);
                if (pos == -1)
                {
                    continue;
                }
                int left = pos;
                while (left > 0 && text[left] != ' ')
                {
                    left--;
                }
                int right = pos;
                while (right < text.Length - 1 && text[right] != ' ')
                {
                    right++;
                }
                return text.Substring(left, right - left);
            }
            return "";
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
            {
                return;
            }
            if (message.Text is not { } messageText)
            {
                return;
            }

            var chatId = message.Chat.Id;
            if (chatId == 0)
            {
                return;
            }
            if (!message.Text.StartsWith("ß"))
            {
                return;
            }

            if (message.ReplyToMessage == null)
            {
                return;
            }
            Uri uri;            
            bool urires = Uri.TryCreate(getFirstUrlWithPattern(message.ReplyToMessage.Text), UriKind.Absolute, out uri);
            if (!urires)
            {
                return;
            }
            string downloadUrl = "";
            bool tiktokres = getTiktokUrl(uri, out downloadUrl);
            if (!tiktokres)
            {
                return;
            }

            (bool streamRes, Stream? stream) = getHttpStream(downloadUrl);
            if (streamRes && stream != null)
            {
                await this.SendChatActionAsync(update.Message.Chat.Id, ChatAction.UploadVideo, cancellationToken: cancellationToken);
                await this.SendVideoAsync(update.Message.Chat.Id, new InputOnlineFile(stream), replyToMessageId: message.MessageId, cancellationToken: cancellationToken);
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private (bool, Stream?) getHttpStream(string url)
        {
            try
            {
                HttpClient cli = new HttpClient();
                var res = cli.GetAsync(url).Result;
                Stream stream = res.Content.ReadAsStreamAsync().Result;
                return (true, stream);
            }
            catch
            {
                Stream? stream = null;
                return (false, stream);
            }
        }
    }
}