
using UnityEngine;

namespace Tripolygon.UModelerX.Runtime.PureCode
{

    public static class DotnetCodesInitailize
    {
#if NET_STANDARD_2_1 || NETSTANDARD2_1
        public static bool DotNetVersion = true;
        public static void Fill<T>(T[] array, T value)
        {
            System.Array.Fill<T>(array, value);
        }

        public static void Fill<T>(T[] array, T value, int startIndex, int count)
        {
            System.Array.Fill<T>(array, value, startIndex, count);
        }
#else
        public static bool DotNetVersion = false;
        public static void Fill<T>(T[] array, T value)
        {
        }

        public static void Fill<T>(T[] array, T value, int startIndex, int count)
        {
        }
#endif
    }


}
