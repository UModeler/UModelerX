using Tripolygon.UModelerX.Runtime;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Tripolygon.UModelerX.PureCode.Editor
{
    /// <summary>UnityEditor Object ID API 차이를 PureCode Editor 조립체에서 처리한다.</summary>
    [InitializeOnLoad]
    internal static class UMXEditorObjectIdUtilityRegistrar
    {
        static UMXEditorObjectIdUtilityRegistrar()
        {
            UMXEditorObjectIdUtility.Register(
                ObjectIdToObject,
                ObjectIdToObject,
                TryGetUpdatePrefabInstanceObjectIds,
                TryExtractHierarchyObjectId,
                TryGetChangeGameObjectOrComponentPropertiesObjectId);
        }

        private static Object ObjectIdToObject(long objectId)
        {
            if (objectId == 0)
                return null;

#if UNITY_6000_4_OR_NEWER
            return ObjectIdToObject(EditorObjectId.FromEntityId(unchecked((ulong)objectId)));
#else
            if (objectId < int.MinValue || objectId > int.MaxValue)
                return null;

            return ObjectIdToObject(EditorObjectId.FromInstanceId((int)objectId));
#endif
        }

        private static Object ObjectIdToObject(EditorObjectId id)
        {
            if (id.IsValid == false)
                return null;

            return id.Kind switch
            {
                EditorObjectIdKind.EntityId => EntityIdToObject(id.EntityId),
                EditorObjectIdKind.InstanceId => InstanceIdToObject(id.InstanceId),
                _ => null
            };
        }

        private static Object InstanceIdToObject(int instanceId)
        {
            if (instanceId == 0)
                return null;

#if UNITY_6000_4_OR_NEWER
            return EditorUtility.EntityIdToObject(EntityId.FromULong(unchecked((uint)instanceId)));
#else
            return EditorUtility.InstanceIDToObject(instanceId);
#endif
        }

#if UNITY_6000_4_OR_NEWER
        private static Object OpenAssetIdToObject(EntityId openAssetId)
        {
            return EntityIdToObject(EntityId.ToULong(openAssetId));
        }
#else
        private static Object OpenAssetIdToObject(int openAssetId)
        {
            return InstanceIdToObject(openAssetId);
        }
#endif

#if UNITY_6000_4_OR_NEWER
        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId entityId, int line)
        {
            return UMXEditorObjectIdUtility.InvokeOpenAssetRequested(OpenAssetIdToObject(entityId), line);
        }
#else
        [OnOpenAsset]
        private static bool OnOpenAsset(int openAssetId, int line)
        {
            return UMXEditorObjectIdUtility.InvokeOpenAssetRequested(OpenAssetIdToObject(openAssetId), line);
        }
#endif

        private static bool TryGetUpdatePrefabInstanceObjectIds(object streamObject, int index, out EditorObjectId[] objectIds)
        {
            if ((streamObject is ObjectChangeEventStream stream) == false)
            {
                objectIds = System.Array.Empty<EditorObjectId>();
                return false;
            }

            stream.GetUpdatePrefabInstancesEvent(index, out var updateData);

#if UNITY_6000_4_OR_NEWER
            int count = updateData.entityIds.Length;
            objectIds = new EditorObjectId[count];
            for (int i = 0; i < count; i++)
            {
                objectIds[i] = EntityIdToObjectId(updateData.entityIds[i]);
            }
#else
            int count = updateData.instanceIds.Length;
            objectIds = new EditorObjectId[count];
            for (int i = 0; i < count; i++)
            {
                objectIds[i] = EditorObjectId.FromInstanceId(updateData.instanceIds[i]);
            }
#endif
            return objectIds.Length > 0;
        }

        private static bool TryExtractHierarchyObjectId(object streamObject, int index, object kindObject, out EditorObjectId objectId, out bool requiresTrackedValidation)
        {
            requiresTrackedValidation = true;
            objectId = default;

            if ((streamObject is ObjectChangeEventStream stream) == false)
                return false;
            if ((kindObject is ObjectChangeKind kind) == false)
                return false;

            switch (kind)
            {
                case ObjectChangeKind.CreateGameObjectHierarchy:
                    stream.GetCreateGameObjectHierarchyEvent(index, out var createData);
#if UNITY_6000_4_OR_NEWER
                    objectId = EntityIdToObjectId(createData.entityId);
#else
                    objectId = EditorObjectId.FromInstanceId(createData.instanceId);
#endif
                    requiresTrackedValidation = false;
                    return objectId.IsValid;

                case ObjectChangeKind.ChangeGameObjectStructure:
                    stream.GetChangeGameObjectStructureEvent(index, out var structureData);
#if UNITY_6000_4_OR_NEWER
                    objectId = EntityIdToObjectId(structureData.entityId);
#else
                    objectId = EditorObjectId.FromInstanceId(structureData.instanceId);
#endif
                    return objectId.IsValid;

                case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    stream.GetChangeGameObjectStructureHierarchyEvent(index, out var hierarchyData);
#if UNITY_6000_4_OR_NEWER
                    objectId = EntityIdToObjectId(hierarchyData.entityId);
#else
                    objectId = EditorObjectId.FromInstanceId(hierarchyData.instanceId);
#endif
                    return objectId.IsValid;

                case ObjectChangeKind.DestroyGameObjectHierarchy:
                    stream.GetDestroyGameObjectHierarchyEvent(index, out var destroyData);
#if UNITY_6000_4_OR_NEWER
                    objectId = EntityIdToObjectId(destroyData.entityId);
#else
                    objectId = EditorObjectId.FromInstanceId(destroyData.instanceId);
#endif
                    return objectId.IsValid;

                case ObjectChangeKind.ChangeGameObjectParent:
                    stream.GetChangeGameObjectParentEvent(index, out var parentData);
#if UNITY_6000_4_OR_NEWER
                    objectId = EntityIdToObjectId(parentData.entityId);
#else
                    objectId = EditorObjectId.FromInstanceId(parentData.instanceId);
#endif
                    return objectId.IsValid;

                case ObjectChangeKind.UpdatePrefabInstances:
                    return false;
            }

            return false;
        }

        private static bool TryGetChangeGameObjectOrComponentPropertiesObjectId(object streamObject, int index, out EditorObjectId objectId)
        {
            objectId = default;
            if ((streamObject is ObjectChangeEventStream stream) == false)
                return false;

            stream.GetChangeGameObjectOrComponentPropertiesEvent(index, out var data);

#if UNITY_6000_4_OR_NEWER
            objectId = EntityIdToObjectId(data.entityId);
#else
            objectId = EditorObjectId.FromInstanceId(data.instanceId);
#endif
            return objectId.IsValid;
        }

        private static Object EntityIdToObject(ulong entityId)
        {
            if (entityId == 0)
                return null;

#if UNITY_6000_4_OR_NEWER
            return EditorUtility.EntityIdToObject(EntityId.FromULong(entityId));
#else
            return null;
#endif
        }

#if UNITY_6000_4_OR_NEWER
        private static EditorObjectId EntityIdToObjectId(EntityId entityId)
        {
            return EditorObjectId.FromEntityId(EntityId.ToULong(entityId));
        }
#endif
    }
}
