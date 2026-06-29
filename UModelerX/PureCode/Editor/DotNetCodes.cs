
using Tripolygon.UModelerX.Runtime;
using Tripolygon.UModelerX.Runtime.PureCode;

namespace Tripolygon.UModelerX.Editor
{
    [UnityEditor.InitializeOnLoad]
    public class DotnetCodes : IArray
    {
        public static DotnetCodes Current { get; set; }
        static DotnetCodes()
        {
            if (DotnetCodesInitailize.DotNetVersion)
            {
                Current = new DotnetCodes();
            }
        }

        public DotnetCodes()
        {
            UMXArray.array = this;
        }

        void IArray.Fill<T>(T[] array, T value)
        {
            DotnetCodesInitailize.Fill<T>(array, value);
        }

        void IArray.Fill<T>(T[] array, T value, int startIndex, int count)
        {
            DotnetCodesInitailize.Fill<T>(array, value, startIndex, count);
        }
    }
}
