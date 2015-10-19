//#define NET_4_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LiteDB
{
#if NET_4_0
    internal static class BinaryReaderExtensions
    {
        public static string ReadString(this BinaryReader reader, int size)
        {
            var bytes = reader.ReadBytes(size);
            return Encoding.UTF8.GetString(bytes);
        }

        public static Guid ReadGuid(this BinaryReader reader)
        {
            return new Guid(reader.ReadBytes(16));
        }

        public static ObjectId ReadObjectId(this BinaryReader reader)
        {
            return new ObjectId(reader.ReadBytes(12));
        }

        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            return new DateTime(reader.ReadInt64());
        }

        public static PageAddress ReadPageAddress(this BinaryReader reader)
        {
            return new PageAddress(reader.ReadUInt32(), reader.ReadUInt16());
        }

        public static BsonValue ReadBsonValue(this BinaryReader reader, ushort length)
        {
            var type = (BsonType)reader.ReadByte();

            switch (type)
            {
                case BsonType.Null: return BsonValue.Null;

                case BsonType.Int32: return reader.ReadInt32();
                case BsonType.Int64: return reader.ReadInt64();
                case BsonType.Double: return reader.ReadDouble();

                case BsonType.String: return reader.ReadString(length);

                case BsonType.Document: return new BsonReader().ReadDocument(reader);
                case BsonType.Array: return new BsonReader().ReadArray(reader);

                case BsonType.Binary: return reader.ReadBytes(length);
                case BsonType.ObjectId: return reader.ReadObjectId();
                case BsonType.Guid: return reader.ReadGuid();

                case BsonType.Boolean: return reader.ReadBoolean();
                case BsonType.DateTime: return reader.ReadDateTime();

                case BsonType.MinValue: return BsonValue.MinValue;
                case BsonType.MaxValue: return BsonValue.MaxValue;
            }

            throw new NotImplementedException();
        }
    }
#else 
    internal static class BinaryReaderExtensions
    {
        public static string ReadString( BinaryReader reader, int size)
        {
            var bytes = reader.ReadBytes(size);
            return Encoding.UTF8.GetString(bytes);
        }

        public static Guid ReadGuid( BinaryReader reader)
        {
            return new Guid(reader.ReadBytes(16));
        }

        public static ObjectId ReadObjectId( BinaryReader reader)
        {
            return new ObjectId(reader.ReadBytes(12));
        }

        public static DateTime ReadDateTime( BinaryReader reader)
        {
            return new DateTime(reader.ReadInt64());
        }

        public static PageAddress ReadPageAddress( BinaryReader reader)
        {
            return new PageAddress(reader.ReadUInt32(), reader.ReadUInt16());
        }

        public static BsonValue ReadBsonValue( BinaryReader reader, ushort length)
        {
            var type = (BsonType)reader.ReadByte();

            switch (type)
            {
                case BsonType.Null: return BsonValue.Null;

                case BsonType.Int32: return reader.ReadInt32();
                case BsonType.Int64: return reader.ReadInt64();
                case BsonType.Double: return reader.ReadDouble();

                case BsonType.String: return ReadString(reader,length);

                case BsonType.Document: return new BsonReader().ReadDocument(reader);
                case BsonType.Array: return new BsonReader().ReadArray(reader);

                case BsonType.Binary: return reader.ReadBytes(length);
                case BsonType.ObjectId: return ReadObjectId(reader);
                case BsonType.Guid: return ReadGuid(reader);

                case BsonType.Boolean: return reader.ReadBoolean();
                case BsonType.DateTime: return ReadDateTime(reader);

                case BsonType.MinValue: return BsonValue.MinValue;
                case BsonType.MaxValue: return BsonValue.MaxValue;
            }

            throw new NotImplementedException();
        }
    }
#endif
}
