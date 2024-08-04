using System.Xml.Serialization;

namespace ChatDB
{
    internal class XmlConverter : Converter
    {
        public override Message? Deserialize(string xmlString)
        {
            var serializer = new XmlSerializer(typeof(Message));            

            using (StringReader messageReader = new StringReader(xmlString))
            {
                return serializer.Deserialize(messageReader) as Message;                
            }            
        }

        public override string Serialize(Message msg)
        {
            var serializer = new XmlSerializer(typeof(Message));

            using (StringWriter messageWriter = new StringWriter())
            {
                serializer.Serialize(messageWriter, msg);
                return messageWriter.ToString();
            }                
        }
    }
}
