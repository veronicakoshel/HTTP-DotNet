using System;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotNetHTTP
{
    class Program
    {
        private static Message[] messages = new Message[5];
        private static int count = 0;
        static void Main(string[] args)
        {
            HttpListener http = new HttpListener();
            http.Prefixes.Add("http://127.0.0.1:8000/");
            http.Prefixes.Add("http://localhost:8000/");
            http.Start();
            Console.WriteLine("Server running at http://127.0.0.1:8000/");
            while (true)
            {
                var context = http.GetContext();
                Task.Run(() =>
                {
                    switch (context.Request.RawUrl)
                    {
                        case "/":
                            context.Response.StatusCode = 200;
                            context.Response.ContentType = "text/html";
                            context.Response.AddHeader("Charset", "UTF-8");
                            BinaryReader reader = new BinaryReader(new FileStream("index.html", FileMode.Open, FileAccess.Read));
                            context.Response.OutputStream.Write(reader.ReadBytes((int)reader.BaseStream.Length), 0, (int)reader.BaseStream.Length);
                            reader.Close();
                            context.Response.Close();
                            break;
                        case "/messages":
                            if (context.Request.HttpMethod == "GET")
                            {
                                context.Response.StatusCode = 200;
                                context.Response.ContentType = "application/json";
                                context.Response.AddHeader("Charset", "UTF-8");
                                if (count < 5)
                                {
                                    Message[] temp = new Message[count];
                                    Array.Copy(messages, temp, count);
                                    context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(temp)));
                                }
                                else
                                    context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messages)));
                                context.Response.Close();
                            }
                            else if (context.Request.HttpMethod == "POST")
                            {
                                byte[] buffer = new byte[1024];
                                int i = 0;
                                for (; ; i++)
                                {
                                    int t = context.Request.InputStream.ReadByte();
                                    if (t == -1)
                                        break;
                                    buffer[i] = (byte)t;
                                }
                                Message m = JsonConvert.DeserializeObject<Message>(Encoding.UTF8.GetString(buffer, 0, i));
                                if (count == 5)
                                {
                                    for (i = messages.Length - 1; i > 0; i--)
                                        messages[i] = messages[i - 1];
                                    messages[0] = m;
                                }
                                else
                                    messages[count++] = m;
                                context.Response.StatusCode = 200;
                                context.Response.Close();
                            }
                            else
                            {
                                context.Response.StatusCode = 400;
                                context.Response.Close();
                            }
                            break;
                        default:
                            context.Response.StatusCode = 404;
                            context.Response.Close();
                            break;
                    }
                });
            }
        }
    }
    class Message
    {
        public string text = "";
    }
}
