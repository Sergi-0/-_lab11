using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace StockDataServer
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Ticker> Tickers => Set<Ticker>();
        public DbSet<Price> Prices => Set<Price>();
        public DbSet<TodaysCondition> TodaysConditions => Set<TodaysCondition>();
        public ApplicationContext() => Database.EnsureCreated();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=C:\\Users\\user\\source\\repos\\ConsoleApp21\\bin\\Debug\\net8.0\\helloapp.db");
        }
    }

    public class Price
    {
        public int Id { get; set; }
        public int? TickerId { get; set; }
        public double PriceValue { get; set; }
        public string? RecordDate { get; set; }
        public string? TickerSymbol { get; set; }
    }

    public class Ticker
    {
        public int Id { get; set; }
        public string? TickerSymbol { get; set; }
    }

    public class TodaysCondition
    {
        public int Id { get; set; }
        public int? TickerId { get; set; }
        public double? PriceState { get; set; }
        public string? TickerSymbol { get; set; }
    }

    public class Program
    {
        static async Task Main()
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, 8888);
            using Socket tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                tcpListener.Bind(serverEndPoint);
                tcpListener.Listen();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    // Получаем входящее подключение
                    using var clientSocket = await tcpListener.AcceptAsync();

                    var receivedDataBuffer = new List<byte>();
                    // Буфер для считывания одного байта
                    var singleByteBuffer = new byte[1];
                    
                    // Считываем данные до конечного символа
                    while (true)
                    {
                        var bytesReceived = await clientSocket.ReceiveAsync(singleByteBuffer);
                        
                        // Если байты закончились или встретили символ новой строки, выходим
                        if (bytesReceived == 0 || singleByteBuffer[0] == '\n') break;
                        
                        // Иначе добавляем в буфер
                        receivedDataBuffer.Add(singleByteBuffer[0]);
                    }

                    var receivedTickerSymbol = Encoding.UTF8.GetString(receivedDataBuffer.ToArray());
                    Console.WriteLine($"Получено сообщение: {receivedTickerSymbol}");
                    
                    string? responseMessage;
                    using (ApplicationContext database = new ApplicationContext())
                    {
                        var tickerRecord = database.Tickers.FirstOrDefault(t => t.TickerSymbol == receivedTickerSymbol);
                        var priceRecord = database.Prices.FirstOrDefault(t => t.Id == tickerRecord.Id);
                        Console.WriteLine(priceRecord.PriceValue);
                        responseMessage = priceRecord.PriceValue.ToString();
                    }

                    // Отправляем данные клиенту
                    byte[] responseData = Encoding.UTF8.GetBytes(responseMessage);
                    await clientSocket.SendAsync(responseData);
                    Console.WriteLine($"Клиенту {clientSocket.RemoteEndPoint} отправлены данные");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
