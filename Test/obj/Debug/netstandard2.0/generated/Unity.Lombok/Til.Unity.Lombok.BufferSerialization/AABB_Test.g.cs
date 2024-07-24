using Til.Lombok;
using Til.Unity.Lombok;

namespace AABB
{
#nullable enable
    public partial class Test
    {
        public static void read(Unity.Netcode.FastBufferReader reader, out Test value)
        {
            bool isNull;
            Unity.Netcode.ByteUnpacker.ReadValueBitPacked(reader, out isNull);
            if (isNull)
            {
                value = null;
                return;
            }

            value = new Test();
            Unity.Netcode.NetworkVariableSerialization<int>.Read(reader, ref value.x);
            Unity.Netcode.NetworkVariableSerialization<int>.Read(reader, ref value.y);
            Unity.Netcode.NetworkVariableSerialization<int>.Read(reader, ref value.z);
        }

        public static void readDelta(Unity.Netcode.FastBufferReader readDelta, ref Test value)
        {
            bool isNull;
            Unity.Netcode.ByteUnpacker.ReadValueBitPacked(reader, out isNull);
            if (isNull)
            {
                value = null;
                return;
            }

            if (value == null)
            {
                value = new Test();
            }

            Unity.Netcode.NetworkVariableSerialization<int>.ReadDelta(reader, ref value.x);
            Unity.Netcode.NetworkVariableSerialization<int>.ReadDelta(reader, ref value.y);
            Unity.Netcode.NetworkVariableSerialization<int>.ReadDelta(reader, ref value.z);
        }

        public static void write(Unity.Netcode.FastBufferReader writer, in Test value)
        {
            if (value == null)
            {
                Unity.Netcode.BytePacker.WriteValuePacked(reader, true);
                return;
            }

            Unity.Netcode.NetworkVariableSerialization<int>.Write(writer, ref value.x);
            Unity.Netcode.NetworkVariableSerialization<int>.Write(writer, ref value.y);
            Unity.Netcode.NetworkVariableSerialization<int>.Write(writer, ref value.z);
        }

        public static void writeDelta(Unity.Netcode.FastBufferReader writer, in Test value, in Test previousValue)
        {
            if (value == null)
            {
                Unity.Netcode.BytePacker.WriteValuePacked(reader, true);
                return;
            }

            Unity.Netcode.NetworkVariableSerialization<int>.WriteDelta(writer, ref value.x, ref previousValue.x);
            Unity.Netcode.NetworkVariableSerialization<int>.WriteDelta(writer, ref value.y, ref previousValue.y);
            Unity.Netcode.NetworkVariableSerialization<int>.WriteDelta(writer, ref value.z, ref previousValue.z);
        }

        public static void duplicateValue(in Test value, ref Test duplicatedValue)
        {
            if (value == null)
            {
                duplicatedValue = null;
                return;
            }

            if (duplicatedValue == null)
            {
                duplicatedValue = new Test();
            }

            if (!object.Equals(duplicatedValue.x, value.x))
            {
                Unity.Netcode.NetworkVariableSerialization<int>.Duplicate(value.x, ref duplicatedValue.x);
            }

            if (!object.Equals(duplicatedValue.y, value.y))
            {
                Unity.Netcode.NetworkVariableSerialization<int>.Duplicate(value.y, ref duplicatedValue.y);
            }

            if (!object.Equals(duplicatedValue.z, value.z))
            {
                Unity.Netcode.NetworkVariableSerialization<int>.Duplicate(value.z, ref duplicatedValue.z);
            }
        }

        [UnityEditor.InitializeOnLoadAttribute]
        public static class TestInitializeOnLoad
        {
            static TestInitializeOnLoad()
            {
                Unity.Netcode.UserNetworkVariableSerialization<Test>.ReadValue = read;
                Unity.Netcode.UserNetworkVariableSerialization<Test>.WriteValue = write;
                Unity.Netcode.UserNetworkVariableSerialization<Test>.WriteDelta = writeDelta;
                Unity.Netcode.UserNetworkVariableSerialization<Test>.ReadDelta = readDelta;
                Unity.Netcode.UserNetworkVariableSerialization<Test>.DuplicateValue = duplicateValue;
            }
        }
    }
}