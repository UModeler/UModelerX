using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal static class UMXGltfCoordinateUtility
    {
        public static Vector3 ToUnityPosition(Vector3 value)
        {
            return new Vector3(value.x, value.y, -value.z);
        }

        public static Vector3 ToUnityNormal(Vector3 value)
        {
            return new Vector3(value.x, value.y, -value.z);
        }

        public static Vector4 ToUnityTangent(Vector4 value)
        {
            return new Vector4(value.x, value.y, -value.z, -value.w);
        }

        public static Vector2 ToUnityUv(Vector2 value)
        {
            return new Vector2(value.x, 1.0f - value.y);
        }

        public static Quaternion ToUnityRotation(float[] value)
        {
            if (value == null || value.Length != 4)
            {
                return Quaternion.identity;
            }

            return new Quaternion(-value[0], -value[1], value[2], value[3]);
        }

        public static Vector3 ToUnityTranslation(float[] value)
        {
            if (value == null || value.Length != 3)
            {
                return Vector3.zero;
            }

            return new Vector3(value[0], value[1], -value[2]);
        }

        public static Vector3 ToUnityScale(float[] value)
        {
            if (value == null || value.Length != 3)
            {
                return Vector3.one;
            }

            return new Vector3(value[0], value[1], value[2]);
        }

        /// <summary>
        /// glTF 의 inverseBindMatrix(MAT4, mesh-local → joint-local) 를 Unity 좌표계로 변환한다.
        /// 다른 좌표계 변환(translation/rotation/normal 등)이 모두 z 축 부호 반전 패턴이므로,
        /// 동일한 F = diag(1, 1, -1, 1) 행렬의 conjugation 으로 일관되게 처리한다 — F = F^-1 이므로
        /// M_unity = F * M_gltf * F. position/rotation 변환과 수학적으로 동치이며 회전 분해를 거치지 않아
        /// 정밀도 손실이 없다.
        /// </summary>
        public static Matrix4x4 ToUnityBindpose(Matrix4x4 gltfMatrix)
        {
            var f = Matrix4x4.identity;
            f.m22 = -1f;
            return f * gltfMatrix * f;
        }

        public static void ReverseTriangleWinding(int[] indices)
        {
            if (indices == null)
            {
                return;
            }

            for (var i = 0; i + 2 < indices.Length; i += 3)
            {
                var temp = indices[i];
                indices[i] = indices[i + 2];
                indices[i + 2] = temp;
            }
        }
    }
}
