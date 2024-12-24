using System.Net.Sockets;
using System.Text;

using var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
try
{
    await clientSocket.ConnectAsync("127.0.0.1", 8888);

    var tickerSymbol = "AACI\n";
    // Преобразуем строку в массив байт
    byte[] requestData = Encoding.UTF8.GetBytes(tickerSymbol);
    // Отправляем данные
    await clientSocket.SendAsync(requestData);
    Console.WriteLine("Сообщение отправлено");

    // Буфер для считывания данных
    byte[] responseBuffer = new byte[512];

    int bytesReceived = await clientSocket.ReceiveAsync(responseBuffer);

    string receivedPrice = Encoding.UTF8.GetString(responseBuffer, 0, bytesReceived);
    Console.WriteLine($"Цена: {receivedPrice}");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка при подключении: {ex.Message}");
}
