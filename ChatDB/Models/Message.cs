

namespace ChatDB.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public int? ToUserId { get; set; }
        public int? FromUserId { get; set; }
        public bool Received { get; set; }
        public virtual User? ToUser { get; set;}
        public virtual User? FromUser { get; set; }
    }
}
