using UnityEngine;

namespace Tripolygon.UModelerX.Runtime.PureCode
{
    /// <summary>Unity Object ID API 차이를 한 곳에서 처리한다.</summary>
    public static class UMXUnityObjectIdUtility
    {
        public static bool UsesEntityId
        {
            get
            {
#if UNITY_6000_4_OR_NEWER
                return true;
#else
                return false;
#endif
            }
        }

        public static int GetInstanceId(Object obj)
        {
            if (obj == null)
                return 0;

#if UNITY_6000_4_OR_NEWER
            return 0;
#else
            return obj.GetInstanceID();
#endif
        }

        public static ulong GetEntityId(Object obj)
        {
            if (obj == null)
                return 0;

#if UNITY_6000_4_OR_NEWER
            return EntityId.ToULong(obj.GetEntityId());
#else
            return 0;
#endif
        }

        public static string GetObjectIdString(Object obj)
        {
            if (UsesEntityId)
            {
                var entityId = GetEntityId(obj);
                return entityId != 0 ? entityId.ToString() : null;
            }

            var objectId = GetInstanceId(obj);
            return objectId != 0 ? objectId.ToString() : null;
        }

        public static bool HasObjectId(Object obj, int objectId)
        {
            return objectId != 0 && GetInstanceId(obj) == objectId;
        }

        public static bool HasEntityId(Object obj, ulong entityId)
        {
            return entityId != 0 && GetEntityId(obj) == entityId;
        }
    }
}
