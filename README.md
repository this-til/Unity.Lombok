# Unity.Lombok


源:

    using Til.Lombok;
    using Til.Unity.Lombok;
    namespace AABB {
        [ILombok]
        public partial class Test {
            [BufferField] public int x;
            [BufferField] public int y;
            [BufferField] public int z;
        }
    }

生成：

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
                Unity.Netcode.ByteUnpacker.ReadValuePacked(reader, out isNull);
                if (isNull)
                {
                    value = null!;
                    return;
                }
                value = new Test();
                Unity.Netcode.NetworkVariableSerialization<int>.Read(reader, ref value.x);
                Unity.Netcode.NetworkVariableSerialization<int>.Read(reader, ref value.y);
                Unity.Netcode.NetworkVariableSerialization<int>.Read(reader, ref value.z);
            }
            public static void readDelta(Unity.Netcode.FastBufferReader reader, ref Test value)
            {
                bool isNull;
                Unity.Netcode.ByteUnpacker.ReadValuePacked(reader, out isNull);
                if (isNull)
                {
                    value = null!;
                    return;
                }
                if (value == null)
                {
                    value = new Test();
                }
                int tag;
                Unity.Netcode.ByteUnpacker.ReadValuePacked(reader, out tag);
                if ((tag & (1 << 0)) != 0)
                {
                    Unity.Netcode.NetworkVariableSerialization<int>.ReadDelta(reader, ref value.x);
                }
                if (9tag & (1 << 1)) != 0)
                {
                    Unity.Netcode.NetworkVariableSerialization<int>.ReadDelta(reader, ref value.y);
                }
                if ((tag & (1 << 2)) != 0)
                {
                    Unity.Netcode.NetworkVariableSerialization<int>.ReadDelta(reader, ref value.z);
                }
            }
            public static void write(Unity.Netcode.FastBufferWriter writer, in Test value)
            {
                if (value == null)
                {
                    Unity.Netcode.BytePacker.WriteValuePacked(writer, true);
                    return;
                }
                Unity.Netcode.BytePacker.WriteValuePacked(writer, false);
                Unity.Netcode.NetworkVariableSerialization<int>.Write(writer, ref value.x);
                Unity.Netcode.NetworkVariableSerialization<int>.Write(writer, ref value.y);
                Unity.Netcode.NetworkVariableSerialization<int>.Write(writer, ref value.z);
            }
            public static void writeDelta(Unity.Netcode.FastBufferWriter writer, in Test value, in Test previousValue)
            {
                if (value == null)
                {
                    Unity.Netcode.BytePacker.WriteValuePacked(writer, true);
                    return;
                }
                Unity.Netcode.BytePacker.WriteValuePacked(writer, false);
                int tag = 0;
                if (!object.Equals(previousValue.x, value.x))
                {
                    tag = tag | (1 << 0);
                }
                if (!object.Equals(previousValue.y, value.y))
                {
                    tag = tag | (1 << 1);
                }
                if (!object.Equals(previousValue.z, value.z))
                {
                    tag = tag | (1 << 2);
                }
                Unity.Netcode.BytePacker.WriteValuePacked(writer, tag);
                if ((tag & (1 << 0)) != 0)
                {
                    Unity.Netcode.NetworkVariableSerialization<int>.WriteDelta(writer, ref value.x, ref previousValue.x);
                }
                if ((tag & (1 << 1)) != 0)
                {
                    Unity.Netcode.NetworkVariableSerialization<int>.WriteDelta(writer, ref value.y, ref previousValue.y);
                }
                if ((tag & (1 << 2)) != 0)
                {
                    Unity.Netcode.NetworkVariableSerialization<int>.WriteDelta(writer, ref value.z, ref previousValue.z);
                }
            }
            public static void duplicateValue(in Test value, ref Test duplicatedValue)
            {
                if (value == null)
                {
                    duplicatedValue = null!;
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
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    