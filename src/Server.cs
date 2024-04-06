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
var echoed = data.Split("/")[1];
string result = string.Empty; 
result = $"HTTP/1.1 200 OK\r\n\r\nContent-Type: text/plain\r\n\r\nContent-Length: {echoed.Length}\r\n\r\n\r\n\r\n{echoed}";
var bytes = Encoding.UTF8.GetBytes(result);
socket.Send(bytes);
Console.ReadLine();