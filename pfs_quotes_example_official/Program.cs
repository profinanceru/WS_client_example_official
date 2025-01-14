using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Globalization;
using System.Security.Cryptography;

using PFSQuotes;
using System.Collections;

namespace pfs_quotes_example
{
    class Program
    {
        /// <summary>
        /// URL сервера котировок ProFinance. Запрашивается у техподдержки 
        /// </summary>
        const string PFS_URL = "<request url from PFS>";
        /// <summary>
        /// Ваш персональный идентификатор доступа. Запрашивается у техподдержки
        /// </summary>
        const string PFS_ID = "<request ID from PFS>";

        /// <summary>
        /// Тестовые тикеры
        /// </summary>
        static readonly string[] TICKERS = new string[]
            {
               "JPYRUB", "gold", "goldgrrub", "silver"
            };


        static void Main(string[] args)
        {
            HashSet<char> keys = new HashSet<char>(new char[]{'1', '2', '3'});
            ConsoleKeyInfo key;
            do
            {
                System.Console.Write("\nВыберите опцию:\n 1-Онлайн\n 2-Снапшот\n 3-Завершить\nВыбор:");
                key = System.Console.ReadKey();
            } while (!keys.Contains(key.KeyChar));
            if (key.KeyChar == '1')
                Online();
            else if (key.KeyChar == '2')
                Snapshot();
        }

        static void Snapshot()
        {
            System.Console.WriteLine();
            using (PFSQuotesAPI quotes = new PFSQuotesAPI(PFS_URL, PFS_ID, true))
            {
                try
                {
                    var list = quotes.GetMarketData(TICKERS);
                    if (list != null)
                    {
                        System.Console.WriteLine("-------- Рыночные данные -----------");
                        foreach (var item in list)
                            System.Console.WriteLine(item);
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex);
                }
                System.Console.WriteLine("Нажмите ENTER для завершения...");
                // Ждем нажатия на ентер
                System.Console.ReadLine();
            }
        }

        static void Online()
        {
            System.Console.WriteLine();
            // Создаем интерфейс доступа к котировкам
            using (PFSQuotesAPI quotes = new PFSQuotesAPI(PFS_URL, PFS_ID))
            {
                // События котировок/пинга
                quotes.OnQuoteEvent += Quotes_OnQuoteEvent;
                quotes.OnLastPriceEvent += Quotes_OnLastPriceEvent;
                quotes.OnTradeEvent += Quotes_OnTradeEvent;
                quotes.OnPingEvent += Quotes_OnPingEvent;
                quotes.OnResultEvent += Quotes_OnResultEvent;
                quotes.OnSessionFinishedByNewLoginEvent += Quotes_OnSessionFinishedByNewLoginEvent;

                // События состояния соединения
                quotes.OnStartConnectEvent += Quotes_OnStartConnectEvent;
                quotes.OnConnectedEvent += Quotes_OnConnectedEvent;
                quotes.OnOpenedEvent += Quotes_OnOpenedEvent;
                quotes.OnReconnectFailedEvent += Quotes_OnReconnectFailedEvent;
                quotes.OnConnectionLostEvent += Quotes_OnConnectionLostEvent;
                quotes.OnDisposedEvent += Quotes_OnDisposedEvent;
                // Запускаем подключение 
                quotes.Start("PFS Example");
                // Ждем нажатия на ентер
                System.Console.WriteLine("Нажмите ENTER для завершения...");
                System.Console.ReadLine();
            }
        }

        private static void Quotes_OnSessionFinishedByNewLoginEvent(PFSQuotesAPI obj)
        {
            System.Console.WriteLine($"Текущая сессия закрыта, потому что на сервере была открыта новая сессия для данного <id>.");
        }

        private static void Quotes_OnResultEvent(PFSQuotesAPI arg1, Result arg2)
        {
            System.Console.WriteLine($"Result: {arg2}");
        }

        #region // События приема сообщений с сервера

        private static void Quotes_OnPingEvent(PFSQuotesAPI sender, Ping ping)
        {
            System.Console.WriteLine($"Ping: {ping}");
        }

        private static void Quotes_OnTradeEvent(PFSQuotesAPI sender, Trade trade)
        {
            System.Console.WriteLine($"Trade Price: {trade}");
        }

        private static void Quotes_OnLastPriceEvent(PFSQuotesAPI sender, LastPrice lastPrice)
        {
            System.Console.WriteLine($"Last Price: {lastPrice}");
        }

        private static void Quotes_OnQuoteEvent(PFSQuotesAPI sender, Quote quote)
        {
            System.Console.WriteLine($"Quote: {quote}");
        }

        #endregion

        #region // События изменения статуса

        private static void Quotes_OnStartConnectEvent(PFSQuotesAPI sender)
        {
            System.Console.WriteLine($"Подключаемся к серверу...");
        }

        private static void Quotes_OnConnectedEvent(PFSQuotesAPI sender)
        {
            System.Console.WriteLine($"Соединение с сервером установлено. Открываем сессию...");
        }

        private static string[] Quotes_OnOpenedEvent(PFSQuotesAPI sender, string sessionId)
        {
            System.Console.WriteLine($"Сессия открыта. Session Id {sessionId}");
            // для того, чтобы автоматически переподписаться на инструменты, 
            // можно вернуть список тикеров в этом событии
            return TICKERS;
            // Если этого делать не нужно - то достаточно вернуть null;
            return null;
        }

        private static void Quotes_OnConnectionLostEvent(PFSQuotesAPI sender, Exception ex)
        {
            System.Console.WriteLine($"Соединение утеряно: {ex.Message}");
        }

        private static void Quotes_OnReconnectFailedEvent(PFSQuotesAPI sender, Exception ex)
        {
            System.Console.WriteLine($"Повторное установление соединения не удалось. Причина: {ex.Message}");
        }

        private static void Quotes_OnDisposedEvent(PFSQuotesAPI obj)
        {
            System.Console.WriteLine($"PFS API завершён.");
        }


        #endregion
    }
}
