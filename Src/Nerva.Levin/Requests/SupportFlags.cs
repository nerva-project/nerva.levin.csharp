using System;

namespace Nerva.Levin.Requests
{
    public class SupportFlags
    {
        public byte[] Create(uint port = 17565)
        {
            throw new NotImplementedException();
        }

        public Section Read(Header header, byte[] buffer, ref int offset)
        {
            LevinReader reader = new LevinReader();
            reader.ReadPayload(header, buffer, ref offset);
            return reader.Output;
        }
    }
}