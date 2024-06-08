using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args) {
        Program program = new Program();

        await program.TestHttpPerformance();
    }

    private async Task TestHttpPerformance()
    {
        string serverUrl = "https://localhost:7278/api/Test";
        string dataValue = "Hello nhố";
        int numberOfRequests = 100;

        // Các biến để lưu trữ thông tin đo lường
        long totalResponseTime = 0;
        long totalExecutionTime = 0;
        long totalBytesSent = 0;
        long totalBytesReceived = 0;
        int requestsSent = 0;
        int responsesReceived = 0;

        HttpClient httpClient = new HttpClient();
        var jsonData = JsonSerializer.Serialize(new { Value = dataValue });
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        for (int i = 0; i < numberOfRequests; i++)
        {
            try
            {
                long startExecution = Stopwatch.GetTimestamp();

                // Gửi yêu cầu POST đến máy chủ
                HttpResponseMessage response = await httpClient.PostAsync(serverUrl, content);

                long endExecution = Stopwatch.GetTimestamp();
                long executionTime = endExecution - startExecution;
                totalExecutionTime += executionTime;

                if (response.IsSuccessStatusCode)
                {
                    responsesReceived++;
                    totalBytesSent += content.Headers.ContentLength.GetValueOrDefault();
                    totalBytesReceived += response.Content.Headers.ContentLength.GetValueOrDefault();

                    long responseTime = executionTime * 1000 / Stopwatch.Frequency; // Chuyển đổi sang milliseconds
                    totalResponseTime += responseTime;
                }
            }
            catch (HttpRequestException)
            {
                // Xử lý khi không nhận được phản hồi từ máy chủ (có thể là mất gói)
                Console.WriteLine("Packet loss detected");
            }
            requestsSent++;
        }

        double averageResponseTime = totalResponseTime / (double)responsesReceived;
        double averageExecutionTime = totalExecutionTime * 1000 / (double)requestsSent / Stopwatch.Frequency; 
        double packetLossRate = (requestsSent - responsesReceived) / (double)requestsSent * 100;

        // In ra kết quả
        Console.WriteLine($"Average HTTP POST response time: {averageResponseTime} milliseconds");
        Console.WriteLine($"Average HTTP POST execution time: {averageExecutionTime} milliseconds");
        Console.WriteLine($"Total bytes sent: {totalBytesSent} bytes");
        Console.WriteLine($"Total bytes received: {totalBytesReceived} bytes");
        Console.WriteLine($"Total bandwidth used: {totalBytesSent + totalBytesReceived} bytes");
        Console.WriteLine($"Requests sent: {requestsSent}");
        Console.WriteLine($"Responses received: {responsesReceived}");
        Console.WriteLine($"Packet loss rate: {packetLossRate}%");
    }
    private async Task TestHTTP()
    {
        string serverUrl = "https://localhost:7278/api/Test";
        string data = "Hello nhố";
        int numberOfRequests = 100;

        HttpClient httpClient = new HttpClient();

        double totalHttpResponseTime = 0;
        double totalExecutionTime = 0;
        long totalBytesSent = 0;
        long totalBytesReceived = 0;
        int requestsSent = 0;
        int requestsReceived = 0;

        for (int i = 0; i < numberOfRequests; i++)
        {
            try
            {
                HttpContent content = new StringContent(data, Encoding.UTF8, "application/json");

                Stopwatch stopwatch = Stopwatch.StartNew();

                // Gửi yêu cầu HTTP POST
                requestsSent++;
                HttpResponseMessage response = await httpClient.PostAsync(serverUrl, content);

                stopwatch.Stop();
                totalExecutionTime += stopwatch.ElapsedMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    requestsReceived++;
                    totalHttpResponseTime += stopwatch.ElapsedMilliseconds;

                    // Tính toán số byte gửi đi và nhận về
                    totalBytesSent += content.Headers.ContentLength ?? 0;
                    totalBytesReceived += response.Content.Headers.ContentLength ?? 0;
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Packet loss detected");
            }
        }

        // Tính toán các giá trị trung bình
        double averageHttpResponseTime = totalHttpResponseTime / requestsReceived;
        double averageExecutionTime = totalExecutionTime / numberOfRequests;
        double packetLossRate = (requestsSent - requestsReceived) / (double)requestsSent * 100;

        Console.WriteLine($"Average HTTP response time: {averageHttpResponseTime} milliseconds");
        Console.WriteLine($"Average HTTP execution time: {averageExecutionTime} milliseconds");
        Console.WriteLine($"Total bytes sent: {totalBytesSent} bytes");
        Console.WriteLine($"Total bytes received: {totalBytesReceived} bytes");
        Console.WriteLine($"Total bandwidth used: {totalBytesSent + totalBytesReceived} bytes");
        Console.WriteLine($"Requests sent: {requestsSent}");
        Console.WriteLine($"Requests received: {requestsReceived}");
        Console.WriteLine($"Packet loss rate: {packetLossRate}%");
    }

    private async Task TestWebSocket()
    {
        string serverUrl = "ws://localhost:6969/ws";
        string data = "Hello nhố";
        int numberOfMessages = 100;
        long excuteAv = 0;
        long responseAv = 0;
        long totalBytesSent = 0;
        long totalBytesReceived = 0;
        int packetsSent = 0;
        int packetsReceived = 0;

        using (var ws = new ClientWebSocket())
        {
            try
            {
                await ws.ConnectAsync(new Uri(serverUrl), CancellationToken.None);

                Stopwatch stopwatch = Stopwatch.StartNew();
                long startTime = stopwatch.ElapsedMilliseconds;

                for (int i = 0; i < numberOfMessages; i++)
                {
                    long startExxcute = stopwatch.ElapsedMilliseconds;
                    byte[] buffer = Encoding.UTF8.GetBytes(data);
                    ArraySegment<byte> payload = new ArraySegment<byte>(buffer);

                    // Gửi dữ liệu qua WebSocket
                    await ws.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);

                    totalBytesSent += payload.Count;
                    packetsSent++;

                    // Nhận phản hồi từ máy chủ
                    byte[] bufferRes = new byte[1024];
                    var receiveSegment = new ArraySegment<byte>(bufferRes);
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await ws.ReceiveAsync(receiveSegment, CancellationToken.None);

                        totalBytesReceived += result.Count;
                        packetsReceived++;

                        string receivedData = Encoding.UTF8.GetString(bufferRes, 0, result.Count);

                        long endExcute = stopwatch.ElapsedMilliseconds;
                        long excuteTime = endExcute - startExxcute;

                        excuteAv += excuteTime;
                        responseAv += (endExcute);
                    }
                    catch (WebSocketException)
                    {
                        // Xử lý khi không nhận được phản hồi từ máy chủ (có thể là mất gói)
                        Console.WriteLine("Packet loss detected");
                    }
                    stopwatch.Stop();

                }

                Console.WriteLine($"Average response {numberOfMessages} times send message: {responseAv / numberOfMessages} milliseconds");
                Console.WriteLine($"Average execute {numberOfMessages} times send message: {excuteAv / numberOfMessages} milliseconds");
                Console.WriteLine($"Total bytes sent: {totalBytesSent} bytes");
                Console.WriteLine($"Total bytes received: {totalBytesReceived} bytes");
                Console.WriteLine($"Total bandwidth used: {totalBytesSent + totalBytesReceived} bytes");
                Console.WriteLine($"Packets sent: {packetsSent}");
                Console.WriteLine($"Packets received: {packetsReceived}");
                Console.WriteLine($"Packet loss rate: {(packetsSent - packetsReceived) / (double)packetsSent * 100}%");
            }
            catch (WebSocketException ex)
            {
                // Xử lý các ngoại lệ WebSocket
                Console.WriteLine($"WebSocket Exception: {ex.Message}");
            }
        }
    }

    private async Task TestWebSocketPerformance()
    {
        string serverUrl = "ws://localhost:6969/ws";
        string data = "Hello nhố";
        int numberOfMessages = 100;

        // Các biến để lưu trữ thông tin đo lường
        long totalResponseTime = 0;
        long totalExecutionTime = 0;
        long totalBytesSent = 0;
        long totalBytesReceived = 0;
        int messagesSent = 0;
        int messagesReceived = 0;

        using (var ws = new ClientWebSocket())
        {
            try
            {
                await ws.ConnectAsync(new Uri(serverUrl), CancellationToken.None);

                // Gửi nhiều tin nhắn và đo thời gian trả lời của từng tin nhắn
                for (int i = 0; i < numberOfMessages; i++)
                {
                    try
                    {
                        long startExecution = Stopwatch.GetTimestamp();

                        // Tạo payload và gửi dữ liệu qua WebSocket
                        byte[] buffer = Encoding.UTF8.GetBytes(data);
                        ArraySegment<byte> payload = new ArraySegment<byte>(buffer);
                        await ws.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
                        totalBytesSent += payload.Count;
                        messagesSent++;

                        // Nhận phản hồi từ máy chủ
                        byte[] bufferRes = new byte[1024];
                        var receiveSegment = new ArraySegment<byte>(bufferRes);
                        WebSocketReceiveResult result = await ws.ReceiveAsync(receiveSegment, CancellationToken.None);

                        long endExecution = Stopwatch.GetTimestamp();
                        long executionTime = endExecution - startExecution;
                        totalExecutionTime += executionTime;

                        totalBytesReceived += result.Count;
                        messagesReceived++;

                        long responseTime = executionTime * 1000 / Stopwatch.Frequency;
                        totalResponseTime += responseTime;
                    }
                    catch (WebSocketException)
                    {
                        // Xử lý khi không nhận được phản hồi từ máy chủ (có thể là mất gói)
                        Console.WriteLine("Packet loss detected");
                    }
                }

                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);


                double averageResponseTime = totalResponseTime / (double)messagesReceived;
                double averageExecutionTime = totalExecutionTime * 1000 / (double)numberOfMessages / Stopwatch.Frequency; // Chuyển đổi sang milliseconds
                double packetLossRate = (messagesSent - messagesReceived) / (double)messagesSent * 100;

                // In ra kết quả
                Console.WriteLine($"Average WebSocket response time: {averageResponseTime} milliseconds");
                Console.WriteLine($"Average WebSocket execution time: {averageExecutionTime} milliseconds");
                Console.WriteLine($"Total bytes sent: {totalBytesSent} bytes");
                Console.WriteLine($"Total bytes received: {totalBytesReceived} bytes");
                Console.WriteLine($"Total bandwidth used: {totalBytesSent + totalBytesReceived} bytes");
                Console.WriteLine($"Messages sent: {messagesSent}");
                Console.WriteLine($"Messages received: {messagesReceived}");
                Console.WriteLine($"Packet loss rate: {packetLossRate}%");
            }
            catch (WebSocketException ex)
            {
                // Xử lý các ngoại lệ WebSocket
                Console.WriteLine($"WebSocket Exception: {ex.Message}");
            }
        }
    }
}

