
namespace ChatDB
{
    internal static class GlobalVariables
    {
        public const string SERVER_NAME = "Server";
        public const string SERVER_START = "--server-start";
        public const string CLIENT_START = "--client-start";

        public const string BROADCAST_USER_NAME = "To all users";
        public const string USER_REGISTER_COMMAND = "register";
        public const string USER_UNREGISTER_COMMAND = "delete";
        public const string USER_LIST_COMMAND = "list";
        public const string USER_SERVER_SHUTDOWN_COMMAND = "shutdown";
        public const string CLIENT_EXIT_COMMAND = "exit";

        public const string SERVER_START_MESSAGE = "Receiver is waiting for messages...";
        public const string SERVER_ESC_MESSAGE = "Getting Esc...!\nServer shutdown!";
        public const string SERVER_SHUTDOWN_MESSAGE = "Shutdown command got!";
        public const string CONFIRMATION_MESSAGE = "Message accepted!";
        public const string SERVER_COMMON_ERROR_MESSAGE = "Somthing went wrong with message!";
        public const string CLIENT_INPUT_MESSAGE = "Введите сообщение:";
        public const string CLIENT_REQUEST_MESSAGE_FORMAT = "GetExchangeFormat";
        public const string SERVER_REQUEST_USER_NAME = "GetUserName";
        public const string SERIALIZATION_FORMAT_MESSAGE = "Формат сериализации при обменен с сервером";

        private static Random random = new Random();

        public const int SERVER_RECEIVER_PORT = 12345;
        public const int SERVER_UDP_CLIENT_PORT = SERVER_RECEIVER_PORT;
        // Для клиента получаем случайный порт иначе не получиться запустить
        // несколько клиентов на одном хосте
        public static readonly int CLIENT_RECEIVER_PORT = random.Next(10000, 64000);
        public static readonly int CLIENT_UDP_CLIENT_PORT = CLIENT_RECEIVER_PORT;

        public static string SerializingFormat { get; set; } = "JSON";
        public static bool IsExchangeFormatSync { get; set; } = false;
    }
}
