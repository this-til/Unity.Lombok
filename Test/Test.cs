using Til.Lombok;
using Til.Unity.Lombok;

namespace AABB {

    [ILombok]
    public partial class Test {

        [BufferField]
        [Get]
        [Set]
        public int x { get; set; }

        [BufferField]
        [Get]
        [Set]
        public int y { get; set; }

        [BufferField]
        [Get]
        [Set]
        public int z { get; set; }

    }

}
