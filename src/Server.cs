using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class HttpServer
{
    private const int BufferSize = 1024;
    private const int Port = 4221;

    public static void Main(string[] args)
    {
        Console.WriteLine("Logs from your program will appear here!");

        TcpListener server = new TcpListener(IPAddress.Any, Port);
        server.Start();
        while (true)
        {
            server.BeginAcceptSocket(AcceptClientCallback, new object[] { server, args });
        }
    }

    private static void AcceptClientCallback(IAsyncResult ar)
    {
        object[] state = (object[])ar.AsyncState;
        TcpListener listener = (TcpListener)state[0];
        string[] args = (string[])state[1];

        using (Socket socket = listener.EndAcceptSocket(ar))
        {
            byte[] buffer = new byte[BufferSize];
            int received = socket.Receive(buffer);
            string request = Encoding.UTF8.GetString(buffer, 0, received);
            string response = HandleRequest(request, args, out byte[] compressed);

            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            socket.Send(responseBytes);
            if (compressed != null)
            {
                socket.Send(compressed);
            }
        }
    }

    private static string HandleRequest(string request, string[] args, out byte[] compressed)
    {
        string[] requestLines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
        string[] requestLine = requestLines[0].Split(' ');
        string method = requestLine[0];
        string url = requestLine[1];
        string directory = args.Length == 2 ? args[1] : string.Empty;
        compressed = null;

        if (url.StartsWith("/files"))
        {
            return HandleFileRequest(method, url, request, directory);
        }
        if (url.StartsWith("/echo"))
        {
            return HandleEchoRequest(url, requestLines, out compressed);
        }
        if (url.StartsWith("/user"))
        {
            return HandleUserRequest(requestLines);
        }
        return HandleDefaultRequest(url);
    }

    private static string HandleFileRequest(string method, string url, string request, string directory)
    {
        string filePath = Path.Combine(directory, url.Replace("/files/", string.Empty));

        if (method == "GET")
        {
            return HandleFileGet(filePath);
        }
        if (method == "POST")
        {
            string requestBody = request.Split(new[] { "\r\n\r\n" }, StringSplitOptions.None)[1].TrimEnd('\0');
            return HandleFilePost(filePath, requestBody);
        }

        return "HTTP/1.1 405 Method Not Allowed\r\n\r\n";
    }

    private static string HandleFileGet(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string fileData = File.ReadAllText(filePath);
                return $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileData.Length}\r\n\r\n{fileData}";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        string notFoundMessage = "File not found";
        return $"HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\nContent-Length: {notFoundMessage.Length}\r\n\r\n{notFoundMessage}";
    }

    private static string HandleFilePost(string filePath, string requestBody)
    {
        try
        {
            File.WriteAllText(filePath, requestBody);
            return "HTTP/1.1 201 Created\r\n\r\n";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return "HTTP/1.1 500 Internal Server Error\r\n\r\n";
        }
    }

    private static string HandleEchoRequest(string url, string[] requestLines, out byte[] compressedResponse)
    {
        string echoed = url.Replace("/echo/", string.Empty);
        string acceptEncodingHeader = requestLines.FirstOrDefault(x => x.StartsWith("Accept-Encoding:", StringComparison.OrdinalIgnoreCase));
        compressedResponse = null;

        if (acceptEncodingHeader != null && acceptEncodingHeader.Contains("gzip", StringComparison.OrdinalIgnoreCase))
        {
            byte[] compressedData = Compress(echoed);
            compressedResponse = compressedData;
            return
                $"HTTP/1.1 200 OK\r\nContent-Encoding: gzip\r\nContent-Type: text/plain\r\nContent-Length: {compressedData.Length}\r\n\r\n";
        }

        return $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {echoed.Length}\r\n\r\n{echoed}";
    }

    private static string HandleUserRequest(string[] requestLines)
    {
        string userAgentHeader = requestLines.FirstOrDefault(x => x.StartsWith("User-Agent:", StringComparison.OrdinalIgnoreCase));
        if (userAgentHeader != null)
        {
            string userAgent = userAgentHeader.Split(':')[1].Trim();
            return $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}";
        }

        return "HTTP/1.1 400 Bad Request\r\n\r\n";
    }

    private static string HandleDefaultRequest(string url)
    {
        if (url == "/")
        {
            return "HTTP/1.1 200 OK\r\n\r\n";
        }

        return "HTTP/1.1 404 Not Found\r\n\r\n";
    }

    private static byte[] Compress(string data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                gzip.Write(bytes, 0, bytes.Length);
            }

            return ms.ToArray();
        }
    }
}
