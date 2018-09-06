using System.ServiceModel.Channels;
using System.Xml;

namespace IDeliverable.ForceClient.Core
{
    internal class SessionHeader : MessageHeader
    {
        public SessionHeader(string sessionId)
        {
            mSessionId = sessionId;
        }

        private readonly string mSessionId;

        public override string Name => "SessionHeader";

        public override string Namespace => "http://soap.sforce.com/2006/04/metadata";

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            writer.WriteElementString("sessionId", mSessionId);
        }
    }
}
