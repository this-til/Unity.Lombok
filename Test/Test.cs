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