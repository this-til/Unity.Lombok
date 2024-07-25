using Til.Lombok;
using Til.Unity.Lombok;

namespace AABB {
    [ILombok]
    public partial class Test {
        [BufferField] [Get] [Set] public int x;
        [BufferField] [Get] [Set] public int y;
        [BufferField] [Get] [Set] public int z;
    }
}