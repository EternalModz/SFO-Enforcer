using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SFO_Enforcer
{
    [DebuggerDisplay("{StringValue}", Name = "{Key}")]
    public class sfoEntry
    {
        private sfoEntry() { }

        private ushort KeyOffset { get; set; }
        public entryFormat ValueFormat { get; private set; }
        public int ValueLength { get; private set; }
        public int ValueMaxLength { get; private set; }
        private int ValueOffset { get; set; }
        public string Key { get; set; }
        public byte[] BinaryValue { get; set; }

        private static readonly char[] Whitespace = { '\r', '\n', '\0', '\t', ' ' };

        public string StringValue
        {
            get
            {
                if (ValueFormat == entryFormat.Utf8 ||
                    ValueFormat == entryFormat.Utf8Null)
                {
                    var result = Encoding.UTF8.GetString(BinaryValue, 0, ValueLength)
                                     .Trim(Whitespace);
                    if (Environment.NewLine != "\n")
                        result = result.Replace("\n", Environment.NewLine);
                    return result;
                }

                throw new InvalidOperationException(
                    "Current value format is not a string");
            }
            set
            {
                if (ValueFormat != entryFormat.Utf8 &&
                    ValueFormat != entryFormat.Utf8Null)
                    throw new InvalidOperationException(
                        "Current value format is not a string");

                var saneValue = value?.Trim(Whitespace) ?? "";
                if (Environment.NewLine != "\n")
                    saneValue = saneValue.Replace(Environment.NewLine, "\n");

                var tmp = new UTF8Encoding(false).GetBytes(saneValue);
                var newLen = tmp.Length;
                if (ValueFormat == entryFormat.Utf8Null)
                    newLen++;
                if (newLen > ValueMaxLength)
                    throw new FormatException(
                        $"Current value length is {newLen} bytes, but maximum value length is {ValueMaxLength} bytes");

                Buffer.BlockCopy(tmp, 0, BinaryValue, 0, tmp.Length);
                if (tmp.Length < BinaryValue.Length)
                    Array.Clear(BinaryValue, tmp.Length, BinaryValue.Length - tmp.Length);
                ValueLength = newLen;
            }
        }

        public static sfoEntry Read(BinaryReader reader, sfoWorker paramSfo,
                                    int itemNumber)
        {
            const int indexOffset = 0x14;
            const int indexEntryLength = 0x10;
            reader.BaseStream.Seek(indexOffset + indexEntryLength * itemNumber,
                                   SeekOrigin.Begin);
            var result = new sfoEntry();
            result.KeyOffset = reader.ReadUInt16();
            result.ValueFormat = (entryFormat)reader.ReadUInt16();
            result.ValueLength = reader.ReadInt32();
            result.ValueMaxLength = reader.ReadInt32();
            result.ValueOffset = reader.ReadInt32();

            reader.BaseStream.Seek(paramSfo.KeysOffset + result.KeyOffset,
                                   SeekOrigin.Begin);
            byte tmp;
            var sb = new StringBuilder(32);
            while ((tmp = reader.ReadByte()) != 0) sb.Append((char)tmp);
            result.Key = sb.ToString();

            reader.BaseStream.Seek(paramSfo.ValuesOffset + result.ValueOffset,
                                   SeekOrigin.Begin);
            result.BinaryValue = reader.ReadBytes(result.ValueMaxLength);

            return result;
        }
    }
}