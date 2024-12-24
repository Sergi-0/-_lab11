using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace YourNamespace // Çàìåíèòå íà âàøå ïðîñòðàíñòâî èìåí
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
        public int? tickerId { get; set; }
        public double price { get; set; }
        public string? date { get; set; }
        public string? ticker { get; set; }
    }
    public class Ticker
    {
        public int Id { get; set; }
        public string? ticker { get; set; }
    }
    public class TodaysCondition
    {
        public int Id { get; set; }
        public int? tickerId { get; set; }
        public double? state { get; set; }
        public string? ticker { get; set; }
    }

    public class Program
    {
        static async Task Main()
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
            using Socket tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                tcpListener.Bind(ipPoint);
                tcpListener.Listen();
                Console.WriteLine("Ñåðâåð çàïóùåí. Îæèäàíèå ïîäêëþ÷åíèé... ");

                while (true)
                {
                    // ïîëó÷àåì âõîäÿùåå ïîäêëþ÷åíèå
                    using var tcpClient = await tcpListener.AcceptAsync();

                    var buffer = new List<byte>();
                    // áóôåð äëÿ ñ÷èòûâàíèÿ îäíîãî áàéòà
                    var bytesRead = new byte[1];
                    // ñ÷èòûâàåì äàííûå äî êîíå÷íîãî ñèìâîëà
                    while (true)
                    {
                        var count = await tcpClient.ReceiveAsync(bytesRead);
                        // ñìîòðèì, åñëè ñ÷èòàííûé áàéò ïðåäñòàâëÿåò êîíå÷íûé ñèìâîë, âûõîäèì
                        if (count == 0 || bytesRead[0] == '\n') break;
                        // èíà÷å äîáàâëÿåì â áóôåð
                        buffer.Add(bytesRead[0]);
                    }

                    var tickrr = Encoding.UTF8.GetString(buffer.ToArray());
                    Console.WriteLine($"Ïîëó÷åíî ñîîáùåíèå: {tickrr}");
                    string? message;
                    using (ApplicationContext db = new ApplicationContext())
                    {
                        var prid = db.Tickers.FirstOrDefault(t => t.ticker == tickrr);
                        var todaystate = db.Prices.FirstOrDefault(t => t.Id == prid.Id);
                        Console.WriteLine(todaystate.price);
                        message = todaystate.price.ToString();
                    }

                    // îïðåäåëÿåì äàííûå äëÿ îòïðàâêè - òåêóùåå âðåìÿ
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await tcpClient.SendAsync(data);
                    Console.WriteLine($"Êëèåíòó {tcpClient.RemoteEndPoint} îòïðàâëåíû äàííûå");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
