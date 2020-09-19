// Copyright © kumak1. All rights reserved.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CommandBufferTester
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    [AddComponentMenu("CommandBufferTester")]
    public sealed class CommandBufferTester : MonoBehaviour
    {
        public CameraEvent processingOrder = CameraEvent.AfterImageEffects;
        public Material[] materials;

        private const string CommandBufferName = "CommandBufferTester";
        private const string GrabShader = "Grab";
        private readonly int _tempRT0 = Shader.PropertyToID("_Temp0");
        private readonly int _tempRT1 = Shader.PropertyToID("_Temp1");

#if UNITY_EDITOR
        private void OnValidate() => ResetCommandBuffer();

        private void OnRenderObject()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                ResetCommandBuffer();
        }
#endif

        private void Start() => ResetCommandBuffer();

        private void ResetCommandBuffer()
        {
            SetCommandBuffer(processingOrder, GenerateCommandBuffer());
        }

        private static void SetCommandBuffer(CameraEvent cameraEvent, CommandBuffer commandBuffer)
        {
            var camera = Camera.main;

            if (camera == null) return;

            camera.RemoveCommandBuffers(cameraEvent);
            camera.AddCommandBuffer(cameraEvent, commandBuffer);
        }

        private static (int, int) GetScreenSize()
        {
#if UNITY_EDITOR
            // 通常実行時
            var screenRes = UnityStats.screenRes.Split('x');
            return (int.Parse(screenRes[0]), int.Parse(screenRes[1]));
#endif

            return (1920, 1080);
        }

        private static RenderTextureFormat TextureFormat() =>
            SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR)
                ? RenderTextureFormat.DefaultHDR
                : RenderTextureFormat.Default;

        private CommandBuffer GenerateCommandBuffer()
        {
            var buffer = new CommandBuffer {name = CommandBufferName};

            (var width, var height) = GetScreenSize();
            var renderTextureFormat = TextureFormat();
            var renderTextureDescriptor =
                new RenderTextureDescriptor(width, height, renderTextureFormat, 0) {autoGenerateMips = false};

            // TemporaryRTを作成
            buffer.GetTemporaryRT(_tempRT0, renderTextureDescriptor, FilterMode.Bilinear);
            buffer.GetTemporaryRT(_tempRT1, renderTextureDescriptor, FilterMode.Bilinear);

            // TemporaryRT を活用した処理を初期化
            var toggleRenderTexture = false;
            int SrcTempId() => toggleRenderTexture ? _tempRT0 : _tempRT1;
            int DestTempId()
            {
                toggleRenderTexture = !toggleRenderTexture;
                return toggleRenderTexture ? _tempRT0 : _tempRT1;
            }

            // カメラからレンダリング結果をGrab
            buffer.Blit(BuiltinRenderTextureType.CurrentActive, DestTempId());

            // UV が上下逆になってる端末の対処
            buffer.Blit(SrcTempId(), DestTempId(), new Material(Shader.Find(GrabShader)), 0);

            if (materials != null)
            {
                foreach (var material in materials)
                    buffer.Blit(SrcTempId(), DestTempId(), material, 0);
            }

            // PostProcess で処理したレンダリング結果をカメラに戻す
            buffer.Blit(SrcTempId(), BuiltinRenderTextureType.CameraTarget);

            // TemporaryRTを解放
            buffer.ReleaseTemporaryRT(_tempRT0);
            buffer.ReleaseTemporaryRT(_tempRT1);

            return buffer;
        }
    }
}