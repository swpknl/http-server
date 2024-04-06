using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
var socket = server.AcceptSocket(); // wait for client
var buffer = new byte[1024];
var received = socket.Receive(buffer);
var data = Encoding.UTF8.GetString(buffer).Split(" ")[1];
string result = string.Empty;
if (data.StartsWith("/echo"))
{
    Console.WriteLine("in echo");
    var echoed = data.Replace("/echo/", string.Empty);
    result = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {echoed.Length}\r\n\r\n{echoed}";
}
else
{
    if (data.Length == 1 && data == "/")
    {
        result = "HTTP/1.1 200 OK\r\n\r\n";
    }
    else
    {
        result = "HTTP/1.1 404 OK\r\n\r\n";
    }
}
var bytes = Encoding.UTF8.GetBytes(result);
socket.Send(bytes);
Console.ReadLine();