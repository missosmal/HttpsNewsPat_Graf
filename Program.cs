using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    internal class Program
    {
        private static HttpClient httpClient = new HttpClient();
        public static string Token;

        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener("log.txt"));
            Help();
            while (true)
                SetComand();
        }

        static void Help()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Command to the client: ");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("/signin");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  - авторизация на сайте");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("/content");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  - получение контента с сайта");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("/addnew");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  - добавление новой записи");
        }

        static async void SetComand()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                string Command = Console.ReadLine();
                if (Command.Contains("/signin")) await SignIn("user", "user");
                if (Command.Contains("/content")) ParsingHtml(await GetContent());
                if (Command.Contains("/addnew")) AddNew();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Request error: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static async Task SignIn(string login, string password)
        {
            string url = "http://127.0.0.1/ajax/login.php";
            WriteLog($"Выполняем запрос: {url}");

            var postData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("login", login),
                new KeyValuePair<string, string>("password", password)
            });

            HttpResponseMessage response = await httpClient.PostAsync(url, postData);
            WriteLog($"Статус выполнения: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                string cookies = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
                if (!string.IsNullOrEmpty(cookies))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Token = cookies.Split(';')[0].Split('=')[1];
                    Console.WriteLine("Печенька: токен = " + Token);
                    Console.ForegroundColor = ConsoleColor.Green;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка выполнения запроса: {response.StatusCode}");
            }
        }

        public static async Task<string> GetContent()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                string url = "http://127.0.0.1/main";
                WriteLog($"Выполняем запрос: {url}");
                httpClient.DefaultRequestHeaders.Add("token", Token);

                HttpResponseMessage response = await httpClient.GetAsync(url);
                WriteLog($"Статус выполнения: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Ошибка выполнения запроса: {response.StatusCode}");
                    return string.Empty;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка выполнения запроса: не авторизован");
                return string.Empty;
            }
        }

        public static void ParsingHtml(string htmlCode)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            HtmlNode document = html.DocumentNode;
            IEnumerable<HtmlNode> divsNews = document.Descendants().Where(n => n.HasClass("news"));

            string content = "";
            foreach (HtmlNode divNews in divsNews)
            {
                string src = divNews.ChildNodes[1].GetAttributeValue("src", "none");
                string name = divNews.ChildNodes[3].InnerText;
                string description = divNews.ChildNodes[5].InnerText;

                content += $"{name}\nИзображение: {src}\nОписание: {description}\n";
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(content);
            Console.ForegroundColor = ConsoleColor.Green;
            WriteToFile(content);
        }

        public static async void AddNew()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                string name;
                string description;
                string image;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Укажите наименование новости");
                name = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Укажите описание новости");
                description = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Укажите адрес картинки");
                image = Console.ReadLine();
                string url = "http://127.0.0.1/ajax/add.php";
                WriteLog($"Выполняем запрос: {url}");
                var postData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("name", name),
                    new KeyValuePair<string, string>("description", description),
                    new KeyValuePair<string, string>("src", image),
                    new KeyValuePair<string, string>("token", Token)
                });
                HttpResponseMessage response = await httpClient.PostAsync(url, postData);
                WriteLog($"Статус выполнения: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Запрос выполнен успешно");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Ошибка выполнения запроса: {response.StatusCode}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка выполнения запроса: не авторизован");
            }
        }

        public static void WriteToFile(string content)
        {
            File.WriteAllText(Environment.CurrentDirectory + "parsedfile.txt", content);
        }

        public static void WriteLog(string debugContent)
        {
            Debug.WriteLine(debugContent);
            Debug.Flush();
        }
    }
}