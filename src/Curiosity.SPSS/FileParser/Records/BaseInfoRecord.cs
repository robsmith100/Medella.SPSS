using System;
using System.IO;

namespace Curiosity.SPSS.FileParser.Records
{
    public abstract class BaseInfoRecord : EncodeEnabledRecord, IRecord
    {
        protected int ItemCount;

        protected int ItemSize;
        public abstract int SubType { get; }
        public RecordType RecordType => RecordType.InfoRecord;

        public void WriteRecord(BinaryWriter writer)
        {
            writer.Write(RecordType);
            writer.Write(SubType);
            writer.Write(ItemSize);
            writer.Write(ItemCount);

            WriteInfo(writer);
        }

        public void FillRecord(BinaryReader reader)
        {
            ItemSize = reader.ReadInt32();
            ItemCount = reader.ReadInt32();

            FillInfo(reader);
        }

        public virtual void RegisterMetadata(MetaData metaData)
        {
            metaData.InfoRecords.Add(this);
            Metadata = metaData;
        }

        protected void CheckInfoHeader(int itemSize = -1, int itemCount = -1)
        {
            if (itemSize >= 0 && itemSize != ItemSize) throw new SpssFileFormatException($"Wrong info record subtype {ItemSize}. Expected {itemSize}.");

            if (itemCount >= 0 && itemCount != ItemCount) throw new SpssFileFormatException($"Wrong info record subtype {ItemCount}. Expected {itemCount}.");
        }

        protected abstract void WriteInfo(BinaryWriter writer);
        protected abstract void FillInfo(BinaryReader reader);
    }

    public class UnknownInfoRecord : BaseInfoRecord
    {
        internal UnknownInfoRecord(int subType)
        {
            SubType = subType;
        }

        internal UnknownInfoRecord(int subType, int itemSize, int itemCount)
        {
            SubType = subType;
            ItemSize = itemSize;
            ItemCount = itemCount;
        }

        public byte[] Data { get; private set; } = Array.Empty<byte>();

        public override int SubType { get; }

        public byte[] this[int i]
        {
            get
            {
                if (ItemSize * i > Data.Length) throw new IndexOutOfRangeException();
                var result = new byte[ItemSize];
                Buffer.BlockCopy(Data, i * ItemSize, result, 0, ItemSize);
                return result;
            }
        }

        protected override void WriteInfo(BinaryWriter writer)
        {
            // TODO: check data length
            writer.Write(Data);
        }

        protected override void FillInfo(BinaryReader reader)
        {
            Data = reader.ReadBytes(ItemCount * ItemSize);
        }
    }
}