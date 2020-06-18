using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace M8.CrossHatch.Universal.RenderFeatures {
    public class CrossHatchOutline : ScriptableRendererFeature {
        class OutlinePass : ScriptableRenderPass {
            const string commandBufferName = "M8.CrossHatch Outline";

            private RenderTargetIdentifier mSrc;
            private RenderTargetHandle mDest;

            private Material mOutlineMaterial = null;

            private RenderTargetHandle mTempColorTexture;

            public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination) {
                mSrc = source;
                mDest = destination;
            }

            public OutlinePass(Material outlineMaterial) {
                mOutlineMaterial = outlineMaterial;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
                var cmd = CommandBufferPool.Get(commandBufferName);

                var opaqueDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDescriptor.depthBufferBits = 0;

                if(mDest == RenderTargetHandle.CameraTarget) {
                    cmd.GetTemporaryRT(mTempColorTexture.id, opaqueDescriptor, FilterMode.Point);
                    Blit(cmd, mSrc, mTempColorTexture.Identifier(), mOutlineMaterial, 0);
                    Blit(cmd, mTempColorTexture.Identifier(), mSrc);
                }
                else 
                    Blit(cmd, mSrc, mDest.Identifier(), mOutlineMaterial, 0);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd) {
                if(mDest == RenderTargetHandle.CameraTarget) {
                    cmd.ReleaseTemporaryRT(mTempColorTexture.id);
                }
            }
        }

        [Header("Settings")]
        public Shader outlineScreenShader;

        public Color edgeColor = Color.black;

        public int thickness = 1;

        [Header("Depth")]
        public bool useDepth = true;
        public float minDepthThreshold = 0f;
        public float maxDepthThreshold = 1f;

        const float depthThresholdScale = 1e-3f;

        [Header("Normals")]
        public bool useNormals = false;
        public float minNormalsThreshold = 0f;
        public float maxNormalsThreshold = 1f;

        private Material mMat;
        private OutlinePass mOutlinePass;
        private RenderTargetHandle mOutlineTexture;

        private int mEdgeColorID = Shader.PropertyToID("_EdgeColor");
        private int mThicknessID = Shader.PropertyToID("_Thickness");
        private int mDepthThresholdMin = Shader.PropertyToID("_DepthThresholdMin");
        private int mDepthThresholdMax = Shader.PropertyToID("_DepthThresholdMax");
        private int mNormalThresholdMin = Shader.PropertyToID("_NormalThresholdMin");
        private int mNormalThresholdMax = Shader.PropertyToID("_NormalThresholdMax");

        public override void Create() {
            if(!outlineScreenShader) {
                Debug.LogWarning("CrossHatchOutline: outlineScreenShader is not defined.");
                return;
            }

            InitMaterial();

            mOutlinePass = new OutlinePass(mMat);
            mOutlinePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;//RenderPassEvent.AfterRenderingTransparents;

            mOutlineTexture.Init("_OutlineTexture");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if(!outlineScreenShader) {
                Debug.LogWarning("CrossHatchOutline: outlineScreenShader is not defined.");
                return;
            }

            InitMaterial();

            mOutlinePass.Setup(renderer.cameraColorTarget, RenderTargetHandle.CameraTarget);
            renderer.EnqueuePass(mOutlinePass);
        }

        const string depthKeyword = "USE_DEPTH";
        const string normalsKeyword = "USE_NORMALS";

        private void InitMaterial() {
            if(!mMat)
                mMat = new Material(outlineScreenShader);

            if(!mMat) {
                Debug.LogWarning("CrossHatchOutline: Unable to create material.");
                return;
            }

            
            if(useDepth)
                mMat.EnableKeyword(depthKeyword);
            else
                mMat.DisableKeyword(depthKeyword);

            
            if(useNormals)
                mMat.EnableKeyword(normalsKeyword);
            else
                mMat.DisableKeyword(normalsKeyword);

            mMat.SetColor(mEdgeColorID, edgeColor);

            mMat.SetFloat(mThicknessID, thickness);

            mMat.SetFloat(mDepthThresholdMin, minDepthThreshold * depthThresholdScale);
            mMat.SetFloat(mDepthThresholdMax, maxDepthThreshold * depthThresholdScale);

            mMat.SetFloat(mNormalThresholdMin, minNormalsThreshold);
            mMat.SetFloat(mNormalThresholdMax, maxNormalsThreshold);
        }
    }
}