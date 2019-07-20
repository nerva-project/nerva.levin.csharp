using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AngryWasp.Helpers;

namespace Nerva.Levin
{
    public class LevinWriter
    {
        private List<byte> buffer = new List<byte>();
        public List<byte> Output => buffer;
        
        public void WritePayload(Section section)
        {
            buffer.AddRange(BitShifter.ToByte(Constants.PORTABLE_STORAGE_SIGNATUREA));
            buffer.AddRange(BitShifter.ToByte(Constants.PORTABLE_STORAGE_SIGNATUREB));
            buffer.Add(Constants.PORTABLE_STORAGE_FORMAT_VER);
            WriteSection(section);
        }

        private void WriteSection(Section section)
        {
            buffer.AddRange(VarInt.To((long)section.Entries.Count));
            foreach (var e in section.Entries)
            {
                byte[] key = Encoding.ASCII.GetBytes(e.Key);
                buffer.Add((byte)key.Length);
                buffer.AddRange(key);
                SerializedWrite(e.Value);
            }
        }

        private void SerializedWrite(object value)
        {
            if (value is Section)
            {
                buffer.Add(Constants.SERIALIZE_TYPE_OBJECT);
                WriteSection((Section)value);
                return;
            }

            Type t = value.GetType();

            if (!Constants.BoostTypes.ContainsKey(t))
                throw new Exception("Unable to cast input to serialized data");

            byte boostType = Constants.BoostTypes[t];
            buffer.Add(boostType);

            if (boostType == Constants.SERIALIZE_TYPE_STRING)
            {
                byte[] sb = ((HexString)value).ToBytes();
                buffer.AddRange(VarInt.To(sb.LongLength));
                buffer.AddRange(sb);
            }
            else
            {
                switch (boostType)
                {
                    case Constants.SERIALIZE_TYPE_UINT64:
                        buffer.AddRange(BitShifter.ToByte((ulong)value));
                        break;
                    case Constants.SERIALIZE_TYPE_INT64:
                        buffer.AddRange(BitShifter.ToByte((long)value));
                        break;
                    case Constants.SERIALIZE_TYPE_UINT32:
                        buffer.AddRange(BitShifter.ToByte((uint)value));
                        break;
                    case Constants.SERIALIZE_TYPE_INT32:
                        buffer.AddRange(BitShifter.ToByte((int)value));
                        break;
                    case Constants.SERIALIZE_TYPE_UINT16:
                        buffer.AddRange(BitShifter.ToByte((ushort)value));
                        break;
                    case Constants.SERIALIZE_TYPE_INT16:
                        buffer.AddRange(BitShifter.ToByte((short)value));
                        break;
                    case Constants.SERIALIZE_TYPE_UINT8:
                    case Constants.SERIALIZE_TYPE_INT8:
                        buffer.Add((byte)value);
                        break;
                }
            }
        }
    }
}