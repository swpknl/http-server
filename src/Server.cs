using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.IO.Compression;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
while (true)
{
    server.BeginAcceptSocket(ar => AcceptClient(ar, args), server); // wait for client    
}


void AcceptClient(IAsyncResult ar, string[] args)
{
    var buffer = new byte[1024];
    var listener = (TcpListener)ar.AsyncState;
    var socket = listener.EndAcceptSocket(ar);
    var received = socket.Receive(buffer);
    var request = Encoding.UTF8.GetString(buffer);
    var array = request.Split("\r\n");
    var data = request.Split(" ")[1];
    string result = string.Empty;
    string directory = "";
    if (args != null && args.Length == 2)
    {
        directory = args[1];
    }

    if (data.StartsWith("/file"))
    {
        if (request.StartsWith("GET"))
        {
            Console.WriteLine(directory);
            var file = data.Replace("/files/", string.Empty);
            try
            {
                var directoryInfo = new DirectoryInfo(directory);
                if (directoryInfo.Exists)
                {
                    var filePath = Path.Combine(directory, file);
                    var fileInfo = new FileInfo(filePath);
                    var fileData = File.ReadAllText(fileInfo.FullName);
                    result = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileData.Length}\r\n\r\n{fileData}";
                }
                else
                {
                    var echoed = "test";
                    result = $"HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\nContent-Length: {echoed.Length}\r\n\r\n{echoed}";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                var echoed = "test";
                result = $"HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\nContent-Length: {echoed.Length}\r\n\r\n{echoed}";
            }
        }
        else
        {
            string requestBody = request.Split("\r\n\r\n")[1].TrimEnd();
            requestBody = requestBody.Replace("\0", string.Empty);
            Console.WriteLine(requestBody);
            string filename = data.Split("/files/")[1];
            var file = Path.Combine(directory, filename);
            var fileInfo = new FileInfo(file);
            if (fileInfo.Exists)
            {
                Console.WriteLine("File Exists");
                File.Delete(fileInfo.FullName);
            }
            File.WriteAllBytes(fileInfo.FullName, Encoding.UTF8.GetBytes(requestBody));
            //File.WriteAllBytes(Path.Combine(directory, file), Encoding.UTF8.GetBytes(fileDataToWrite));
            result =
                $"HTTP/1.1 201 Created\r\n\r\n";
        }
    }
    else if (data.StartsWith("/echo"))
    {
        Console.WriteLine("in echo");
        var echoed = data.Replace("/echo/", string.Empty);
        var encodingHeader = string.Empty;
        if (array.Any(x => x.ToLower().Contains("accept-encoding")))
        {
            var compression = array.First(x => x.ToLower().Contains("accept-encoding"));
            var compressionValue = compression.ToLower().Replace("accept-encoding: ", string.Empty).Split(" ");
            if (compressionValue.Any(x => x.Contains("gzip")))
            {
                encodingHeader = $"Content-Encoding: gzip";
                var compressed = BitConverter.ToString(Compress(echoed)).Replace("-", string.Empty);
                result = $"HTTP/1.1 200 OK\r\n{encodingHeader}\r\nContent-Type: text/plain\r\nContent-Length: {compressed.Length}\r\n\r\n{compressed}";
            }
            else
            {
                result = $"HTTP/1.1 200 OK{encodingHeader}\r\nContent-Type: text/plain\r\nContent-Length: {echoed.Length}\r\n\r\n{echoed}";
            }
        }
        else
        {
            result = $"HTTP/1.1 200 OK{encodingHeader}\r\nContent-Type: text/plain\r\nContent-Length: {echoed.Length}\r\n\r\n{echoed}";
        }

    }
    else if (data.StartsWith("/user"))
    {
        var userAgent = request.Split("\r\n")[2];
        var userAgentValue = userAgent.Split(":")[1].TrimStart();
        result = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgentValue.Length}\r\n\r\n{userAgentValue}";
    }
    else
    {
        if (data.Length == 1 && data == "/")
        {
            result = "HTTP/1.1 200 OK\r\n\r\n";
        }
        else
        {
            result = "HTTP/1.1 404 Not Found\r\n\r\n";
        }
    }

    var bytes = Encoding.UTF8.GetBytes(result);
    socket.Send(bytes);
    //server.EndAcceptSocket(ar);
    Console.ReadLine();
}

string RemoveBom(string p)
{
    string BOMMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
    if (p.StartsWith(BOMMarkUtf8, StringComparison.Ordinal))
        p = p.Remove(0, BOMMarkUtf8.Length);
    return p.Replace("\0", "");
}

static byte[] Compress(string echoData)
{
    using (MemoryStream ms = new MemoryStream())
    {
        using (GZipStream gzip =
               new GZipStream(ms, CompressionMode.Compress, true))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(echoData);
            gzip.Write(bytes, 0, bytes.Length);
        }

        // Get the gzip compressed data
        byte[] gzipData = ms.ToArray();
        return gzipData;
    }
}

static byte[] CompressCore(string info)
    {
        // Compressing the body
        byte[] data = Encoding.UTF8.GetBytes(info);
        Console.WriteLine($"word to compress: {info}");
        MemoryStream compressedBody = new MemoryStream();
        GZipStream compressor =
            new GZipStream(compressedBody, CompressionMode.Compress);
        compressor.Write(data, 0, data.Length);
        compressor.Flush();
        compressor.Close();
        return compressedBody.ToArray();
    }