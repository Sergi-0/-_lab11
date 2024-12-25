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
        // Создаем конечную точку для прослушивания на всех доступных сетевых интерфейсах на порту 8888
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, 8888);

        // Создаем сокет для прослушивания входящих TCP-подключений
        // AddressFamily.InterNetwork - IPv4
        // SocketType.Stream - потоковый сокет (TCP)
        // ProtocolType.Tcp - используем TCP-протокол
        using Socket tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            // Привязываем сокет к заранее созданной конечной точке
            tcpListener.Bind(serverEndPoint);

            // Начинаем прослушивание входящих подключений
            // Указываем максимальную длину очереди ожидающих подключений
            tcpListener.Listen();

            // Выводим сообщение о запуске сервера
            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            // Бесконечный цикл обработки подключений
            while (true)
            {
                // Асинхронно принимаем входящее подключение
                // AcceptAsync() ожидает подключения клиента и возвращает сокет для взаимодействия
                using var clientSocket = await tcpListener.AcceptAsync();

                // Создаем список для накопления полученных байт
                var receivedDataBuffer = new List<byte>();

                // Создаем буфер для чтения одного байта за раз
                // Используем массив из одного элемента для построчного чтения
                var singleByteBuffer = new byte[1];
                
                // Цикл чтения данных от клиента
                while (true)
                {
                    // Асинхронно получаем один байт
                    var bytesReceived = await clientSocket.ReceiveAsync(singleByteBuffer);
                    
                    // Условия завершения чтения:
                    // 1. Байты закончились (bytesReceived == 0)
                    // 2. Встретили символ новой строки
                    if (bytesReceived == 0 || singleByteBuffer[0] == '\n') break;
                    
                    // Добавляем полученный байт в буфер
                    receivedDataBuffer.Add(singleByteBuffer[0]);
                }

                // Преобразуем полученные байты в строку (символ тикера)
                var receivedTickerSymbol = Encoding.UTF8.GetString(receivedDataBuffer.ToArray());

                // Выводим полученное сообщение в консоль
                Console.WriteLine($"Получено сообщение: {receivedTickerSymbol}");
                
                // Переменная для хранения ответного сообщения
                string? responseMessage;

                // Блок работы с базой данных
                using (ApplicationContext database = new ApplicationContext())
                {
                    // Находим запись тикера по символу
                    var tickerRecord = database.Tickers.FirstOrDefault(t => t.TickerSymbol == receivedTickerSymbol);

                    // Находим запись цены по идентификатору тикера
                    var priceRecord = database.Prices.FirstOrDefault(t => t.Id == tickerRecord.Id);

                    // Выводим значение цены в консоль
                    Console.WriteLine(priceRecord.PriceValue);
                    
                    // Преобразуем значение цены в строку для ответа
                    responseMessage = priceRecord.PriceValue.ToString();
                }

                // Преобразуем ответное сообщение в байты
                byte[] responseData = Encoding.UTF8.GetBytes(responseMessage);
                
                // Асинхронно отправляем данные клиенту
                await clientSocket.SendAsync(responseData);
                
                // Выводим информацию об отправке в консоль
                Console.WriteLine($"Клиенту {clientSocket.RemoteEndPoint} отправлены данные");
            }
        }
        catch (Exception ex)
        {
            // Обрабатываем и выводим любые исключения
            Console.WriteLine(ex.Message);
        }
    }
}
