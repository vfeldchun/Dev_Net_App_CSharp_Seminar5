using System.Xml.Serialization;

namespace ChatDB
{
    public class Message : ICloneable
    {
        [XmlElement]
        public Command Command {  get; set; }
        [XmlElement]
        public int? Id {  get; set; }
        [XmlElement]
        public string SenderName { get; set; }
        [XmlElement]
        public string? RecipientName { get; set; }
        [XmlElement]
        public string? MessageText { get; set; }
        [XmlElement]
        public DateTime MessageTime { get; set; }

        // Вводим конструктор без параметров для XML сериализации
        public Message() 
        {}

        public Message(string senderName, string messageText, Command command)
        {
            SenderName = senderName;
            MessageTime = DateTime.Now;
            MessageText = messageText;
            RecipientName = "";
            Command = command;            
        }

        public Message(string senderName, string messageText, string recipientName = "")
        {
            SenderName = senderName;            
            MessageTime = DateTime.Now;

            if (messageText.Split(':').Length > 1)
            {                
                RecipientName = messageText.Split(":")[0].Trim();
                MessageText = messageText.Split(":")[1].Trim();                
            }
            else
            {
                MessageText = messageText;
                RecipientName = recipientName;                
            }

            Command = Command.Message;
        }
        
        public override string ToString()
        {
            return $"От: {SenderName} Кому: {RecipientName} ({MessageTime.ToString("HH:mm:ss")}) Сообщение: {MessageText}";
        }

        public object Clone()
        {
            var newMessage = new Message()
            {
              Id = this.Id,
              SenderName = this.SenderName, 
              RecipientName = this.RecipientName!, 
              MessageText = this.MessageText,
              MessageTime = this.MessageTime,
              Command = this.Command 
            };
            
            return newMessage;
        }
    }
}
