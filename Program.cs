using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MarketDataExchanger
{
    public class StockIdentifier
    {
        [Key]
        public int UniqueId { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string MarketSymbol { get; set; } = string.Empty;
    }

    public class MarketQuote
    {
        [Key]
        public int QuoteId { get; set; }
        
        public int IdentifierId { get; set; }
        
        [Required]
        public decimal QuoteValue { get; set; }
        
        public DateTime QuoteTimestamp { get; set; }
    }

    public class MarketDataContext : DbContext
    {
        public DbSet<StockIdentifier> MarketSymbols { get; set; }
        public DbSet<MarketQuote> QuoteHistory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder configurator)
        {
            configurator.UseSqlServer(
                "Server=localhost,1433;Database=MarketDataDb;User Id=SA;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;"
            );
        }
    }

    class MarketDataBroadcaster
    {
        private TcpListener _marketListener;
        private const int BROADCAST_PORT = 8888;

        public MarketDataBroadcaster()
        {
            _marketListener = new TcpListener(IPAddress.Loopback, BROADCAST_PORT);
        }

        public void LaunchBroadcast()
        {
            try
            {
                _marketListener.Start();
                Console.WriteLine($"Market Data Service Active on Port {BROADCAST_PORT}");

                while (true)
                {
                    TcpClient marketClient = _marketListener.AcceptTcpClient();
                    Thread clientProcessingThread = new Thread(new ParameterizedThreadStart(ProcessMarketRequest));
                    clientProcessingThread.Start(marketClient);
                }
            }
            catch (Exception communicationError)
            {
                Console.WriteLine($"Broadcast Interruption: {communicationError.Message}");
            }
            finally
            {
                _marketListener.Stop();
            }
        }

        private void ProcessMarketRequest(object marketConnection)
        {
            TcpClient marketClient = (TcpClient)marketConnection;
            NetworkStream dataStream = marketClient.GetStream();

            try
            {
                byte[] requestBuffer = new byte[1024];
                int receivedBytes = dataStream.Read(requestBuffer, 0, requestBuffer.Length);
                string requestedSymbol = Encoding.ASCII.GetString(requestBuffer, 0, receivedBytes).Trim().ToUpper();

                Console.WriteLine($"Market Data Request: {requestedSymbol}");

                decimal latestQuotation = RetrieveLatestQuotation(requestedSymbol);

                byte[] responseData = Encoding.ASCII.GetBytes(latestQuotation.ToString());
                dataStream.Write(responseData, 0, responseData.Length);
            }
            catch (Exception processingError)
            {
                Console.WriteLine($"Request Processing Error: {processingError.Message}");
                byte[] errorResponse = Encoding.ASCII.GetBytes("MARKET_DATA_UNAVAILABLE");
                dataStream.Write(errorResponse, 0, errorResponse.Length);
            }
            finally
            {
                marketClient.Close();
            }
        }

        private decimal RetrieveLatestQuotation(string marketSymbol)
        {
            using (var marketDataContext = new MarketDataContext())
            {
                var symbolEntity = marketDataContext.MarketSymbols
                    .FirstOrDefault(symbol => symbol.MarketSymbol == marketSymbol);

                if (symbolEntity == null)
                {
                    Console.WriteLine($"Symbol Not Found: {marketSymbol}");
                    return 0;
                }

                var latestQuote = marketDataContext.QuoteHistory
                    .Where(quote => quote.IdentifierId == symbolEntity.UniqueId)
                    .OrderByDescending(quote => quote.QuoteTimestamp)
                    .FirstOrDefault();

                return latestQuote?.QuoteValue ?? 0;
            }
        }
    }

    class MarketDataClient
    {
        static void Main()
        {
            Thread marketBroadcastThread = new Thread(() =>
            {
                var marketBroadcaster = new MarketDataBroadcaster();
                marketBroadcaster.LaunchBroadcast();
            });
            marketBroadcastThread.Start();

            Thread.Sleep(1000);

            while (true)
            {
                Console.Write("Enter Market Symbol: ");
                string marketSymbol = Console.ReadLine().Trim().ToUpper();

                try
                {
                    TcpClient marketConnection = new TcpClient();
                    marketConnection.Connect(IPAddress.Loopback, 8888);

                    NetworkStream communicationStream = marketConnection.GetStream();

                    byte[] symbolData = Encoding.ASCII.GetBytes(marketSymbol);
                    communicationStream.Write(symbolData, 0, symbolData.Length);

                    byte[] responseBuffer = new byte[1024];
                    int receivedBytes = communicationStream.Read(responseBuffer, 0, responseBuffer.Length);
                    string marketQuotation = Encoding.ASCII.GetString(responseBuffer, 0, receivedBytes);

                    Console.WriteLine($"Latest Quotation for {marketSymbol}: {marketQuotation}");

                    marketConnection.Close();
                }
                catch (Exception communicationError)
                {
                    Console.WriteLine($"Connection Error: {communicationError.Message}");
                }
            }
        }
    }
}
