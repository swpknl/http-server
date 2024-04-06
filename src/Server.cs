using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
var socket = await server.AcceptSocketAsync(); // wait for client
//var buffer = new ArraySegment<byte>();
//var data = await socket.ReceiveAsync(buffer, SocketFlags.None);
var result = "HTTP/1.1 200 OK\\r\\n\\r\\n";
var bytes = Encoding.UTF8.GetBytes(result.ToCharArray());
var sent = socket.Send(new ArraySegment<byte>(bytes));
Console.ReadLine();