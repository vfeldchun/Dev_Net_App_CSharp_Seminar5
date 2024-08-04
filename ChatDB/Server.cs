using System.Net.Sockets;
using System.Net;
using System.Text;
using ChatDB.Models;

namespace ChatDB
{
    internal static class Server
    {
        private static IPEndPoint receiverEndPoint = new IPEndPoint(IPAddress.Any, 12345);
        private static  UdpClient udpClient = new UdpClient(12345);

        private static CancellationTokenSource cts = new CancellationTokenSource();
        private static CancellationToken ct;
        private static Dictionary<string, IPEndPoint> _userDict = new Dictionary<string, IPEndPoint>();

        static async Task Register(Message message, IPEndPoint fromep)
        {
            Console.WriteLine("Message Register, name = " + message.SenderName);

            if (!_userDict.ContainsKey(message.SenderName))
            {
                _userDict.Add(message.SenderName, fromep);

                using (var ctx = new ChatDbContext())
                {
                    if (ctx.Users.FirstOrDefault(x => x.Name == message.SenderName) != null) 
                    {
                        await SendMessageAsync(fromep, new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} зарегестрирован!", message.SenderName));
                        return;
                    }                    

                    ctx.Add(new User { Name = message.SenderName });
                    ctx.SaveChanges();
                }                
            }
            else
            {
                _userDict[message.SenderName] = fromep;                
            }
            
            await SendMessageAsync(fromep, new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} зарегестрирован!", message.SenderName));
        }

        static async Task Unregister(Message message)
        {
            IPEndPoint fromep = _userDict[message.SenderName];

            Console.WriteLine("Message Unegister, name = " + message.SenderName);

            if (_userDict.ContainsKey(message.SenderName))
            {
                _userDict.Remove(message.SenderName);

                using (var ctx = new ChatDbContext())
                {
                    if (ctx.Users.FirstOrDefault(x => x.Name == message.SenderName) == null)
                    {                        
                        await SendMessageAsync(fromep, new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} не был зарегестрирован и не может быть удален!", message.SenderName));
                        return;
                    }                    

                    ctx.Users.Remove(ctx.Users.FirstOrDefault(x => x.Name == message.SenderName));
                    ctx.SaveChanges();
                }

                await SendMessageAsync(fromep, new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} удален!", message.SenderName));
            }
            else
            {                
                await SendMessageAsync(fromep, new Message(GlobalVariables.SERVER_NAME, $"Пользователь {message.SenderName} не был зарегестрирован и не может быть удален!", message.SenderName));
            }
        }

        static void ConfirmMessageReceived(int? id)
        {
            Console.WriteLine("Message confirmation id=" + id);

            using (var ctx = new ChatDbContext())
            {
                var msg = ctx.Messages.FirstOrDefault(x => x.Id == id);

                if (msg != null)
                {
                    msg.Received = true;
                    ctx.SaveChanges();
                }
            }
        }

        static async Task RelyMessage(Message message)
        {
            int? id = null;           

            if (_userDict.TryGetValue(message.RecipientName, out IPEndPoint ep))
            {
                using (var ctx = new ChatDbContext())
                {
                    var fromUser = ctx.Users.First(x => x.Name == message.SenderName);
                    var toUser = ctx.Users.First(x => x.Name == message.RecipientName);

                    var msg = new Models.Message { FromUser = fromUser, ToUser = toUser, Received = false, Text = message.MessageText! };
                    ctx.Messages.Add(msg);

                    ctx.SaveChanges();

                    id = msg.Id;
                }

                var forwardMessage = message.Clone() as Message;
                forwardMessage.Id = id;                
                await SendMessageAsync(ep, forwardMessage);
                // udpClient.Send(forwardBytes, forwardBytes.Length, ep);

                Console.WriteLine($"Message Relied, from = {message.SenderName} to = {message.RecipientName}");
            }
            else
            {
                Console.WriteLine("Пользователь не найден.");
            }
        }

        static async Task ForwardAllNotReceivedMessagesToUser(string userName, IPEndPoint ep)
        {
            using (var ctx = new ChatDbContext())
            {
                int userId = ctx.Users.Where(r => r.Name == userName).Select(x => x.Id).SingleOrDefault();
                var notReceivedMessages = ctx.Messages.Where(r => r.Received == false && r.ToUserId == userId).Select(x => x).ToList();

                if (notReceivedMessages.Count > 0)
                {
                    await SendMessageAsync(ep, new Message(GlobalVariables.SERVER_NAME, $"У вас есть не полученные сообщения"));
                    foreach (var msg in notReceivedMessages)
                    {                        
                        var fromUser = ctx.Users.Where(r => r.Id == msg.FromUserId).Select(x => x.Name).SingleOrDefault();
                        var newMessage = new Message() { Id = msg.Id, Command = Command.Message, MessageText = msg.Text, SenderName = fromUser, RecipientName = userName };
                        newMessage.MessageTime = DateTime.Now;
                        await SendMessageAsync(ep, newMessage);
                    }
                }
            }
        }

        static async Task ListUsers(Message message)
        {
            IPEndPoint ep = _userDict[message.SenderName];

            string userList = "[ ";
            foreach (var key in _userDict.Keys)
                userList += key + ", ";
            userList += "]";

            await SendMessageAsync(ep, new Message(GlobalVariables.SERVER_NAME, $"Список зарегестрированных пользователей\n{userList}", message.SenderName));
        }

        static async Task ProcessMessage(Message message, IPEndPoint fromep)
        {
            Console.WriteLine($"Получено сообщение от {message.SenderName} для {message.RecipientName} с командой {message.Command}:");
            // Console.WriteLine(message);

            if (message.Command == Command.Register)
                await Register(message, new IPEndPoint(fromep.Address, fromep.Port));

            if (message.Command == Command.Unregister)
                await Unregister(message);

            if (message.Command == Command.List)
                await ListUsers(message);

            if (message.Command == Command.Confirmation && message.SenderName != GlobalVariables.SERVER_NAME)
            {
                Console.WriteLine("Confirmation received");
                ConfirmMessageReceived(message.Id);
            }

            if (message.Command == Command.Message)
                await RelyMessage(message);

        }
        private static void LoadUsersFromDb()
        {
            using (var ctx = new ChatDbContext())
            {
                var userList = ctx.Users.Select(x => x.Name).ToList();

                foreach (var user in userList)
                {
                    _userDict.Add(user, null);
                }
            }
        }

        private static async Task SendMessageAsync(IPEndPoint remoteEndPoint, Message message)
        {
            string resultString = "";

            if (GlobalVariables.SerializingFormat == "XML")
            {
                Converter converter = new XmlConverter();
                resultString = converter.Serialize(message);
            }
            else
            {
                Converter converter = new JsonConverter();
                resultString = converter.Serialize(message);
            }
            // string jsonMsg = message.GetJson();
            
            byte[] respondBytes = Encoding.UTF8.GetBytes(resultString);
            await udpClient.SendAsync(respondBytes, remoteEndPoint);            
        }

        public static async Task UdpRecieverAsync()
        {
            //IPEndPoint receiverEndPoint = new IPEndPoint(IPAddress.Any, 12345);
            //UdpClient udpClient = new UdpClient(12345);           

            LoadUsersFromDb();

            Console.WriteLine(GlobalVariables.SERVER_START_MESSAGE);

            ct = cts.Token;

            new Task(() =>
            {
                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)                           
                        break;
                }

                // Отправка сообщения о завершении работы в консоль сервера                         
                Message escapeMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.SERVER_ESC_MESSAGE);
                Console.WriteLine("x" + escapeMessage);
                Environment.Exit(0);                
            }).Start();               

            while (ct.IsCancellationRequested != true)
            {
                try
                {
                    // byte[] bytes = udpClient.Receive(ref receiverEndPoint);
                    var receiveResult = await udpClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(receiveResult.Buffer);

                    // Обрабатываем формат запроса формата обмена сообщениями от клиента
                    if (message == GlobalVariables.CLIENT_REQUEST_MESSAGE_FORMAT)
                    {
                        byte[] exchangeMethodBytes = Encoding.UTF8.GetBytes(GlobalVariables.SerializingFormat);
                        await udpClient.SendAsync(exchangeMethodBytes, receiveResult.RemoteEndPoint);

                        // Запрашиваем имя пользователя у клиента - ДЗ Семинар 5
                        byte[] requestUserIdBytes = Encoding.UTF8.GetBytes(GlobalVariables.SERVER_REQUEST_USER_NAME);
                        await udpClient.SendAsync(requestUserIdBytes, receiveResult.RemoteEndPoint);
                    }
                    // Если есть не полученные сообщения у коиента то отправляем их все клиенту - ДЗ Семинар 5
                    else if (message.Contains(GlobalVariables.SERVER_REQUEST_USER_NAME))
                    {                        
                        if (message.Split(':').Length > 1)
                        {
                            var userName = message.Split(':')[1].Trim();

                            // Если имя есть в списке зарегестрированных полтзователей то оправляем сообщения - ДЗ Семинар 5
                            if (_userDict.ContainsKey(userName))
                            {
                                await ForwardAllNotReceivedMessagesToUser(userName, receiveResult.RemoteEndPoint);
                            }
                        }
                    }
                    else
                    {
                        await Task.Run(async () =>
                        {
                            // Message? newMessage = Message.GetMessage(message);
                            Message? newMessage;

                            // Factory method
                            if (GlobalVariables.SerializingFormat == "XML")
                            {
                                Converter converter = new XmlConverter();
                                newMessage = converter.Deserialize(message);
                            }
                            else
                            {
                                Converter converter = new JsonConverter();
                                newMessage = converter.Deserialize(message);
                            }

                            if (newMessage?.MessageText?.ToLower() == GlobalVariables.USER_SERVER_SHUTDOWN_COMMAND)
                            {
                                cts.Cancel();

                                // Отправка подтверждения получения сообщения завершения работы сервера
                                Message acceptMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.SERVER_SHUTDOWN_MESSAGE);
                                await SendMessageAsync(receiveResult.RemoteEndPoint, acceptMessage);
                                Console.WriteLine(acceptMessage);
                                Thread.Sleep(500);
                            }
                            else
                            {
                                if (newMessage != null)
                                {  
                                    await ProcessMessage(newMessage, receiveResult.RemoteEndPoint);

                                    Console.WriteLine(newMessage);

                                    // Отправка подтверждения получения сообщения
                                    Message acceptMessage = new Message(GlobalVariables.SERVER_NAME, GlobalVariables.CONFIRMATION_MESSAGE, Command.Confirmation);
                                    acceptMessage.RecipientName = newMessage.SenderName;
                                    await SendMessageAsync(receiveResult.RemoteEndPoint, acceptMessage);
                                }
                                else
                                    Console.WriteLine(GlobalVariables.SERVER_COMMON_ERROR_MESSAGE);
                            }
                        });
                    }                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }


            }
        }
    }
}
