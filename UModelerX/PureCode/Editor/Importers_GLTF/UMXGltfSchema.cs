using System;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    [Serializable]
    internal sealed class UMXGltfRoot
    {
        public UMXGltfAccessor[] accessors;
        public UMXGltfAnimation[] animations;
        public UMXGltfAsset asset;
        public UMXGltfBuffer[] buffers;
        public UMXGltfBufferView[] bufferViews;
        public string[] extensionsRequired;
        public string[] extensionsUsed;
        public UMXGltfImage[] images;
        public UMXGltfMaterial[] materials;
        public UMXGltfMesh[] meshes;
        public UMXGltfNode[] nodes;
        public UMXGltfScene[] scenes;
        public int scene = -1;
        public UMXGltfSampler[] samplers;
        public UMXGltfSkin[] skins;
        public UMXGltfTexture[] textures;
    }

    [Serializable]
    internal sealed class UMXGltfSkin
    {
        public int inverseBindMatrices = -1;
        public int[] joints;
        public int skeleton = -1;
        public string name;
    }

    [Serializable]
    internal sealed class UMXGltfAnimation
    {
        public string name;
        public UMXGltfAnimationChannel[] channels;
        public UMXGltfAnimationSampler[] samplers;
    }

    [Serializable]
    internal sealed class UMXGltfAnimationChannel
    {
        public int sampler = -1;
        public UMXGltfAnimationChannelTarget target;
    }

    [Serializable]
    internal sealed class UMXGltfAnimationChannelTarget
    {
        public int node = -1;
        public string path;
    }

    [Serializable]
    internal sealed class UMXGltfAnimationSampler
    {
        public int input = -1;
        public int output = -1;
        public string interpolation = "LINEAR";
    }

    [Serializable]
    internal sealed class UMXGltfAsset
    {
        public string generator;
        public string version;
    }

    [Serializable]
    internal sealed class UMXGltfScene
    {
        public int[] nodes;
        public string name;
    }

    [Serializable]
    internal sealed class UMXGltfNode
    {
        public int[] children;
        public float[] matrix;
        public int mesh = -1;
        public string name;
        public float[] rotation;
        public float[] scale;
        public int skin = -1;
        public float[] translation;
    }

    [Serializable]
    internal sealed class UMXGltfMesh
    {
        public string name;
        public UMXGltfPrimitive[] primitives;
    }

    [Serializable]
    internal sealed class UMXGltfPrimitive
    {
        public UMXGltfAttributes attributes;
        public int indices = -1;
        public int material = -1;
        public int mode = 4;
        public UMXGltfAttributes[] targets;
    }

    [Serializable]
    internal sealed class UMXGltfAttributes
    {
        public int COLOR_0 = -1;
        public int JOINTS_0 = -1;
        public int NORMAL = -1;
        public int POSITION = -1;
        public int TANGENT = -1;
        public int TEXCOORD_0 = -1;
        public int WEIGHTS_0 = -1;
    }

    [Serializable]
    internal sealed class UMXGltfAccessor
    {
        public int bufferView = -1;
        public int byteOffset;
        public int componentType;
        public int count;
        public bool normalized;
        public UMXGltfAccessorSparse sparse;
        public string type;
    }

    [Serializable]
    internal sealed class UMXGltfAccessorSparse
    {
        public int count;
    }

    [Serializable]
    internal sealed class UMXGltfBufferView
    {
        public int buffer;
        public int byteLength;
        public int byteOffset;
        public int byteStride;
        public int target;
    }

    [Serializable]
    internal sealed class UMXGltfBuffer
    {
        public int byteLength;
        public string uri;
    }

    [Serializable]
    internal sealed class UMXGltfMaterial
    {
        public bool doubleSided;
        public float alphaCutoff = 0.5f;
        public string alphaMode;
        public float[] emissiveFactor;
        public string name;
        public UMXGltfTextureInfo emissiveTexture;
        public UMXGltfNormalTextureInfo normalTexture;
        public UMXGltfOcclusionTextureInfo occlusionTexture;
        public UMXGltfPbrMetallicRoughness pbrMetallicRoughness;
    }

    [Serializable]
    internal sealed class UMXGltfPbrMetallicRoughness
    {
        public float[] baseColorFactor;
        public UMXGltfTextureInfo baseColorTexture;
        public float metallicFactor = 1.0f;
        public UMXGltfTextureInfo metallicRoughnessTexture;
        public float roughnessFactor = 1.0f;
    }

    [Serializable]
    internal class UMXGltfTextureInfo
    {
        public int index = -1;
        public int texCoord;
    }

    [Serializable]
    internal sealed class UMXGltfNormalTextureInfo : UMXGltfTextureInfo
    {
        public float scale = 1.0f;
    }

    [Serializable]
    internal sealed class UMXGltfOcclusionTextureInfo : UMXGltfTextureInfo
    {
        public float strength = 1.0f;
    }

    [Serializable]
    internal sealed class UMXGltfTexture
    {
        public int sampler = -1;
        public int source = -1;
        public string name;
    }

    [Serializable]
    internal sealed class UMXGltfImage
    {
        public int bufferView = -1;
        public string mimeType;
        public string name;
        public string uri;
    }

    [Serializable]
    internal sealed class UMXGltfSampler
    {
        public int magFilter;
        public int minFilter;
        public int wrapS = 10497;
        public int wrapT = 10497;
    }
}
