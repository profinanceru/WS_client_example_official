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
using System.Net.NetworkInformation;

namespace pfs_quotes_example
{
    class Program
    {
        /// <summary>
        /// URL сервера котировок ProFinance. Уточняется у техподдержки 
        /// </summary>
        const string PFS_URL = @"<request url from PFS>";
        /// <summary>
        /// Ваш персональный идентификатор доступа. Уточняется у техподдержки 
        /// </summary>
        const string PFS_ID = "<request ID from PFS>";
        /// <summary>
        /// Тестовые тикеры - для примера 
        /// Список доступных тикеров и их имена необходимо учтонять в техподдержке
        /// </summary>
        static readonly string[] TICKERS = new string[]
            {
                "JPYRUB", "goldgrrub", "gold", "silver"
            };

        static void Main(string[] args)
        {
            // Создаем интерфейс доступа к котировкам
            using (PFSQuotesAPI quotes = new PFSQuotesAPI(PFS_URL, PFS_ID))
            {
                //// Подключаемся к событиям
                // События котировок/пинга
                quotes.OnQuoteEvent += Quotes_OnQuoteEvent;
                quotes.OnLastPriceEvent += Quotes_OnLastPriceEvent;
                quotes.OnTradeEvent += Quotes_OnTradeEvent;
                quotes.OnPingEvent += Quotes_OnPingEvent;
                // События состояния соединения
                quotes.OnStartConnectEvent += Quotes_OnStartConnectEvent;
                quotes.OnConnectedEvent += Quotes_OnConnectedEvent;
                quotes.OnOpenedEvent += Quotes_OnOpenedEvent;
                quotes.OnReconnectFailedEvent += Quotes_OnReconnectFailedEvent;
                quotes.OnConnectionLostEvent += Quotes_OnConnectionLostEvent;
                quotes.OnDisposedEvent += Quotes_OnDisposedEvent;
                // Запускаем подключение 
                quotes.Start("PFS Example");

                System.Console.ReadLine();
            }
            System.Console.ReadLine();
        }

        #region // События приема сообщений с сервера

        private static void Quotes_OnPingEvent(PFSQuotesAPI sender, PFSQuotes.Ping ping)
        {
            System.Console.WriteLine($"Ping: {ping}");
        }

        private static void Quotes_OnTradeEvent(PFSQuotesAPI sender, Trade trade)
        {
            System.Console.WriteLine($"Last Price: {trade}");
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
            System.Console.WriteLine($"Connecting to the server...");
        }

        private static void Quotes_OnConnectedEvent(PFSQuotesAPI sender)
        {
            System.Console.WriteLine($"Connection to the server is established. Opening session...");
        }

        private static string[] Quotes_OnOpenedEvent(PFSQuotesAPI sender, string sessionId)
        {
            System.Console.WriteLine($"Session is open. Session Id {sessionId}");
            // для того, чтобы автоматически переподписаться на инструменты, 
            // можно вернуть список тикеров в этом событии
            return TICKERS;
            // Если этого делать не нужно - то достаточно вернуть null;
            return null;
        }

        private static void Quotes_OnConnectionLostEvent(PFSQuotesAPI sender, Exception ex)
        {
            System.Console.WriteLine($"Connection Lost: {ex.Message}");
        }

        private static void Quotes_OnReconnectFailedEvent(PFSQuotesAPI sender, Exception ex)
        {
            System.Console.WriteLine($"Reconnection failed with: {ex.Message}");
        }

        private static void Quotes_OnDisposedEvent(PFSQuotesAPI obj)
        {
            System.Console.WriteLine($"PFS API is disposed.");
        }


        #endregion
    }
}
