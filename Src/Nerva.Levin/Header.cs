using System;
using System.Collections.Generic;
using AngryWasp.Helpers;

namespace Nerva.Levin
{
    public class Header
    {
        public ulong Cb { get; set; }
        public bool HaveToReturnData { get; set; }
        public uint Command { get; set; }
        public int ReturnCode { get; set; }
        public uint Flags { get; set; }
    
        public byte[] ToBytes()
        {
            List<byte> ret = new List<byte>();

            ret.AddRange(BitShifter.ToByte(Constants.LEVIN_SIGNATURE));
            ret.AddRange(BitShifter.ToByte(Cb));
            ret.Add(HaveToReturnData ? (byte)0x01 : (byte)0x00);
            ret.AddRange(BitShifter.ToByte(Command));
            ret.AddRange(BitShifter.ToByte(ReturnCode));
            ret.AddRange(BitShifter.ToByte(Flags));
            ret.AddRange(BitShifter.ToByte(Constants.LEVIN_PROTOCOL_VER_1));

            return ret.ToArray();
        }

        public static Header FromBytes(byte[] buffer, ref int offset)
        {
            if (buffer.Length < 33)
                throw new Exception("Buffer is insufficient length");

            Header h = new Header();
            
            ulong levin_sig = BitShifter.ToULong(buffer, ref offset);

            if (levin_sig != Constants.LEVIN_SIGNATURE)
                throw new Exception("Levin signature is incorrect");

            h.Cb = BitShifter.ToULong(buffer, ref offset);
            h.HaveToReturnData = buffer[offset++] == 1 ? true : false;
            h.Command = BitShifter.ToUInt(buffer, ref offset);
            h.ReturnCode = BitShifter.ToInt(buffer, ref offset);
            h.Flags = BitShifter.ToUInt(buffer, ref offset);
            
            uint levin_protocol = BitShifter.ToUInt(buffer, ref offset);

            if (levin_protocol != Constants.LEVIN_PROTOCOL_VER_1)
                throw new Exception("Levin protocol is incorrect");

            return h;
        }
    }
}