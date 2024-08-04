using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ChatDB
{
    internal static class Client
    {
        static private IPEndPoint udpServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), GlobalVariables.SERVER_RECEIVER_PORT);
        static private UdpClient udpClient = new UdpClient(GlobalVariables.CLIENT_UDP_CLIENT_PORT);
        static private string _clientName = "";

        static private CancellationTokenSource cts = new CancellationTokenSource();
        static private CancellationToken ct;

        private static async Task UdpClientRecieverAsync()
        {
            while (ct.IsCancellationRequested != true)
            {
                try
                {
                    var receiveResult = await udpClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(receiveResult.Buffer);                    

                    if (message == "XML" || message == "JSON")
                    {
                        GlobalVariables.SerializingFormat = message;
                        GlobalVariables.IsExchangeFormatSync = true;
                    }
                    // ДЗ Семинар 5
                    else if (message == GlobalVariables.SERVER_REQUEST_USER_NAME)
                    {
                        byte[] requestedUserNameBytes = Encoding.UTF8.GetBytes(GlobalVariables.SERVER_REQUEST_USER_NAME + ":" + _clientName);
                        await udpClient.SendAsync(requestedUserNameBytes, receiveResult.RemoteEndPoint);
                    }
                    else
                    {
                        Converter converter;

                        if (GlobalVariables.SerializingFormat == "XML")
                            converter = new XmlConverter();
                        else
                            converter = new JsonConverter();

                        Message? newMessage = converter.Deserialize(message);

                        if (newMessage?.MessageText == GlobalVariables.SERVER_SHUTDOWN_MESSAGE)
                        {
                            Console.WriteLine(newMessage);
                            cts.Cancel();
                        }
                        else if (newMessage?.Command == Command.Confirmation) continue;                        
                        else
                        {
                            Console.WriteLine(newMessage);
                            // Console.WriteLine($"Message Id: {newMessage.Id}");

                            var confirmationMessage = newMessage?.Clone() as Message;
                            if (confirmationMessage != null)
                            {
                                confirmationMessage.Command = Command.Confirmation;
                                await SendMessageAsync(confirmationMessage, converter);
                            }                            

                            Console.WriteLine(GlobalVariables.CLIENT_INPUT_MESSAGE);
                        }
                    }                                        
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }


        }

        private static async Task SendMessageAsync(Message message, Converter converter)
        {            
            string newMsg = converter.Serialize(message);
            byte[] bytes = Encoding.UTF8.GetBytes(newMsg);
            await udpClient.SendAsync(bytes, udpServerEndPoint);
        }

        private static bool IsRecipientName (string message, out string RecipientName)
        {
            var stringArray = message.Split(":");

            if (stringArray.Length > 1)
            {
                RecipientName = stringArray[0];
                return true;
            }
            RecipientName = null;
            return false;
        }

        public static async Task UdpSenderAsync(string name)
        {
            // IPEndPoint udpServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), GlobalVariables.SERVER_RECEIVER_PORT);
            // UdpClient udpClient = new UdpClient(GlobalVariables.CLIENT_UDP_CLIENT_PORT);
            Message newMessage;
            _clientName = name;

            ct = cts.Token;

            // Запускаем локальный получатель сообщений клиента
            new Task(async () => { await UdpClientRecieverAsync(); }).Start();

            // Запрос типа сериализации у сервера            
            byte[] jsonExchangeMsgBytes = Encoding.UTF8.GetBytes(GlobalVariables.CLIENT_REQUEST_MESSAGE_FORMAT);
            await udpClient.SendAsync(jsonExchangeMsgBytes, udpServerEndPoint);

            while (!GlobalVariables.IsExchangeFormatSync) { }
            Console.WriteLine($"{GlobalVariables.SERIALIZATION_FORMAT_MESSAGE}: {GlobalVariables.SerializingFormat}");

            // Factory method
            Converter converter;
            if (GlobalVariables.SerializingFormat == "XML")
                converter = new XmlConverter();
            else
                converter = new JsonConverter();



            while (ct.IsCancellationRequested != true)
            {
                Console.WriteLine(GlobalVariables.CLIENT_INPUT_MESSAGE);
                string? messageText = Console.ReadLine();

                if (messageText?.ToLower() == GlobalVariables.CLIENT_EXIT_COMMAND)
                {
                    cts.Cancel();
                    Message exitMessage = new Message(name, messageText);
                    await SendMessageAsync(exitMessage, converter);
                }
                else if (messageText == null) continue;
                else
                {
                    if (messageText.ToLower().Contains(GlobalVariables.USER_REGISTER_COMMAND.ToLower()))
                    {
                        newMessage = new Message(name, messageText, Command.Register);
                        newMessage.RecipientName = GlobalVariables.SERVER_NAME;
                    }
                    else if (messageText.ToLower().Contains(GlobalVariables.USER_UNREGISTER_COMMAND.ToLower()))
                    {
                        newMessage = new Message(name, messageText, Command.Unregister);
                        newMessage.RecipientName = GlobalVariables.SERVER_NAME;
                    }
                    else if (messageText.ToLower().Contains(GlobalVariables.USER_LIST_COMMAND.ToLower()))
                    {
                        newMessage = new Message(name, messageText, Command.List);
                        newMessage.RecipientName = GlobalVariables.SERVER_NAME;
                    }
                    else
                    {
                        newMessage = new Message(name, messageText);
                    }
                    await SendMessageAsync(newMessage, converter);
                }                                
            }
        }
    }
}
