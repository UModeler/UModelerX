#if HDRP_ENABLED && UNITY_EDITOR
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Tripolygon.UModelerX.Runtime;

namespace Tripolygon.UModelerX.Runtime.HDRPFaceIDRenderer
{
    [UnityEditor.InitializeOnLoad]
    public class HDRPCustomRenderer : Tripolygon.UModelerX.Runtime.IRenderTextureRenderer
    {
        private Camera renderCamera = null;
        private HDAdditionalCameraData cameraData = null;
        private Color backgroundColor;
        private bool clearDepth;
        private bool clearColor;
        private string stringShaderTagID;
        public static HDRPCustomRenderer Current { get; set; }

        static HDRPCustomRenderer()
        {
            Current = new HDRPCustomRenderer();
        }

        public HDRPCustomRenderer()
        {
            Tripolygon.UModelerX.Runtime.CustomRenderContext.externalRenderer = this;
        }

        bool IRenderTextureRenderer.EnableCustomRenderer => MaterialSettings.GetRenderpipeMode() == MaterialSettings.UnityRenderpipeMode.HDRP;
        bool IRenderTextureRenderer.SetCamera(GameObject renderCameraGameObject, GameObject cameraSourceGameObject)
        {
            if (renderCameraGameObject == null || cameraSourceGameObject == null)
            {
                return false;
            }
            this.renderCamera = renderCameraGameObject.GetComponent<Camera>();
            var sourceCamera = cameraSourceGameObject.GetComponent<Camera>();
            if (this.renderCamera == null || sourceCamera == null)
            {
                return false;
            }
            this.backgroundColor = this.renderCamera.backgroundColor;
            var cameraSourceData = cameraSourceGameObject.GetComponent<HDAdditionalCameraData>();
            this.cameraData = renderCameraGameObject.GetComponent<HDAdditionalCameraData>();
            if (this.cameraData == null)
            {
                this.cameraData = renderCameraGameObject.AddComponent<HDAdditionalCameraData>();
            }
            if (this.cameraData == null)
            {
                return false;
            }
            if (cameraSourceData != null)
            {
                cameraSourceData.CopyTo(this.cameraData);
            }

            switch (this.renderCamera.clearFlags)
            {
//                case CameraClearFlags.SolidColor:
                case CameraClearFlags.Color:
                    this.cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                    this.clearColor = true;
                    this.clearDepth = this.cameraData.clearDepth = true;
                    break;
                case CameraClearFlags.Skybox:
                    this.cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
                    this.clearColor = true;
                    this.clearDepth = this.cameraData.clearDepth = true;
                    break;
                case CameraClearFlags.Depth:
                    this.cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                    this.clearColor = false;
                    this.clearDepth  = this.cameraData.clearDepth = true;
                    break;
                case CameraClearFlags.Nothing:
                    this.cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                    this.clearColor = false;
                    this.clearDepth = this.cameraData.clearDepth = false;
                    break;
            }
            this.cameraData.flipYMode = HDAdditionalCameraData.FlipYMode.ForceFlipY;

            return true;
        }
        //void IRenderTextureRenderer.CopyCamera(GameObject cameraGameObject)
        //{
        //    if (this.renderCamera != null && cameraGameObject.GetComponent<Camera>() != null)
        //    {
        //        this.renderCamera.CopyFrom(cameraGameObject.GetComponent<Camera>());
        //    }
        //    if (this.cameraData != null && cameraGameObject.GetComponent<HDAdditionalCameraData>() != null)
        //    {
        //        cameraGameObject.GetComponent<HDAdditionalCameraData>().CopyTo(this.cameraData);
        //    }
        //}
        void IRenderTextureRenderer.Render(string shaderTag)
        {
            this.renderCamera.renderingPath = RenderingPath.Forward;
            this.renderCamera.enabled = false;
            //this.renderCamera.clearFlags = CameraClearFlags.SolidColor;
            this.renderCamera.backgroundColor = this.backgroundColor;
            this.renderCamera.allowHDR = false;
            this.renderCamera.allowMSAA = false;
            this.renderCamera.forceIntoRenderTexture = true;
            this.stringShaderTagID = shaderTag;
            this.cameraData.customRender += CustomRenderPass;
            try
            {
                this.renderCamera.Render();
            }
            finally
            {
                this.cameraData.customRender -= CustomRenderPass;
            }
        }
        private void CustomRenderPass(UnityEngine.Rendering.ScriptableRenderContext scriptableRenderContext, UnityEngine.Rendering.HighDefinition.HDCamera camera)
        {
            scriptableRenderContext.SetupCameraProperties(camera.camera);

            UnityEngine.Rendering.CommandBuffer commandBuffer = new UnityEngine.Rendering.CommandBuffer();
            commandBuffer.ClearRenderTarget(this.clearDepth, this.clearColor, this.backgroundColor);
            scriptableRenderContext.ExecuteCommandBuffer(commandBuffer);
            scriptableRenderContext.Submit();

            UnityEngine.Rendering.DrawingSettings drawSettings = new UnityEngine.Rendering.DrawingSettings();
            //drawSettings.SetShaderPassName(0, new UnityEngine.Rendering.ShaderTagId("Always"));
            drawSettings.SetShaderPassName(0, new UnityEngine.Rendering.ShaderTagId(this.stringShaderTagID));

            UnityEngine.Rendering.FilteringSettings filterSettings = UnityEngine.Rendering.FilteringSettings.defaultValue;

            if (camera.camera.TryGetCullingParameters(out UnityEngine.Rendering.ScriptableCullingParameters cullParams))
            {
                UnityEngine.Rendering.CullingResults cullResuts = scriptableRenderContext.Cull(ref cullParams);
                scriptableRenderContext.DrawRenderers(cullResuts, ref drawSettings, ref filterSettings);
            }
        }
    }
}

#endif
