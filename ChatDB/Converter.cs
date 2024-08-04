
namespace ChatDB
{
    abstract class Converter
    {
        public abstract string Serialize(Message msg);
        public abstract Message? Deserialize(string msg);
    }
}
