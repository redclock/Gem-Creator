using RedGame.GemCreator;
using UnityEditor;
using UnityEngine;

namespace RedGame.EditorTools
{
    public class CreateGemWindow : EditorWindow
    {
        [MenuItem("Tools/Create Gem")]
        private static void ShowWindow()
        {
            var window = GetWindow<CreateGemWindow>();
            window.titleContent = new GUIContent("Create Gem");
            window.Show();
        }
        
        private GemSetting _setting = new GemSetting();
        private GemShape _shape = new GemShape();
        private GemPreview _preview = new GemPreview();
        private GemNormalRender _normalRender = new GemNormalRender();
        
        private bool _previewOutline = true;
        private bool _previewFaceNormal = true;
        
        private RenderTexture _rtRender;
        private RenderTexture _rtNormal;
        private Material _renderMaterial;
        private static readonly int s_mainTex = Shader.PropertyToID("_MainTex");

        private void OnEnable()
        {
            CreateRenderTextures();
        }

        private void CreateRenderTextures()
        {
            if (!_rtNormal)
                _rtNormal = new RenderTexture(256, 256, 0, 
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            //_rtNormal.antiAliasing = 8;

            if (!_rtRender)
                _rtRender = new RenderTexture(_rtNormal.width, _rtNormal.height, 0, 
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        }

        private void OnDisable()
        {
            DestroyImmediate(_rtNormal);
            DestroyImmediate(_rtRender);
            DestroyImmediate(_renderMaterial);
            _rtRender = null;
            _rtNormal = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUI.indentLevel++;
            DrawSettingsGUI(_setting);
            EditorGUI.indentLevel--;

            bool shapeDirty = false;
            if (EditorGUI.EndChangeCheck() || !_shape.IsCreated)
            {
                _shape.CreateShape(_setting);
                shapeDirty = true;
            }

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _previewOutline = EditorGUILayout.Toggle("Preview Outline", _previewOutline);
            _previewFaceNormal = EditorGUILayout.Toggle("Preview Face Normal", _previewFaceNormal);
            EditorGUI.indentLevel--;
            DrawPreview();
            
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginChangeCheck();
            
            _setting.smoothDistance = EditorGUILayout.Slider("Smooth Distance", _setting.smoothDistance, 0, 0.2f);
            _setting.smoothPower = EditorGUILayout.Slider("Smooth Power", _setting.smoothPower, 0.1f, 2.0f);
            
            CreateRenderTextures();
            if (EditorGUI.EndChangeCheck() || shapeDirty)
            {
                Graphics.SetRenderTarget(_rtNormal);
                GL.Clear(true, true, Color.clear);
                Rect rt = new Rect(0, 0, _rtNormal.width, _rtNormal.height);
                _normalRender.RenderNormalRt(rt, _shape, _setting.innerHeight, _setting.smoothDistance, _setting.smoothPower);
                Graphics.SetRenderTarget(null);
            }
            
            EditorGUI.indentLevel--;
            DrawRtToGUI();
            
            if (GUILayout.Button("Save PNG"))
            {
                string fileName = EditorUtility.SaveFilePanel("Save Gem", "", "gem.png", "png");
                if (!string.IsNullOrEmpty(fileName))
                {
                    SaveRtToPng(fileName);
                }
            }
        }

        private static void DrawSettingsGUI(GemSetting setting)
        {
            setting.borderCount = EditorGUILayout.IntSlider("Border Count", setting.borderCount, 3, 10);
            setting.bevelIter = EditorGUILayout.IntSlider("Bevel Iteration", setting.bevelIter, 0, 3);
            setting.bevelFactor = EditorGUILayout.Slider("Bevel Factor", setting.bevelFactor, 0, 0.25f);
            setting.innerLen = EditorGUILayout.Slider("Inner Length", setting.innerLen, 0, 1.0f);
            setting.innerHeight = EditorGUILayout.Slider("Inner Height", setting.innerHeight, 0, 0.5f);
            setting.scaleWidth = EditorGUILayout.Slider("Width", setting.scaleWidth, 0.5f, 4.0f);
        }

        private void DrawPreview()
        {
            var rect = GetTextureRect();
            EditorGUI.DrawRect(rect, Color.black);
            _preview.DrawPreview(_shape, rect, _previewFaceNormal, _previewOutline);
        }

        private void DrawRtToGUI()
        {
            if (!_rtNormal)
                return;
            
            Rect rect = GetTextureRect();
            EditorGUI.DrawPreviewTexture(rect, _rtNormal);
            
            _renderMaterial = EditorGUILayout.ObjectField("Material", _renderMaterial, typeof(Material), false) as Material;

            if (_renderMaterial && _rtRender)
            {
                _rtRender.DiscardContents();
                var oldTexture = _renderMaterial.GetTexture(s_mainTex);
                _renderMaterial.SetTexture(s_mainTex, _rtNormal);
                RenderTexture oldRt = RenderTexture.active;
                RenderTexture.active = _rtRender;
                GL.Clear(true, true, Color.clear);
                Graphics.Blit(_rtNormal, _rtRender, _renderMaterial);
                RenderTexture.active = oldRt;
                _renderMaterial.SetTexture(s_mainTex, oldTexture);
                rect = GetTextureRect();
                EditorGUI.DrawPreviewTexture(rect, _rtRender);
            }
        }

        private Rect GetTextureRect()
        {
            if (!_rtNormal)
                return Rect.zero;
            float aspectRatio = (float)_rtNormal.width / _rtNormal.height;
            float rectHeight = 100;
            float rectWidth = rectHeight * aspectRatio;
            //var rect = GUILayoutUtility.GetRect(rectWidth, rectHeight);
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Width(rectWidth), GUILayout.Height(rectHeight));

            float lineCenter = EditorGUIUtility.currentViewWidth / 2;
            rect.x = lineCenter - rect.width / 2;
            return rect;
        }

        private void SaveRtToPng(string fileName)
        {
            var oldRt = RenderTexture.active;
            RenderTexture.active = _rtNormal;
            int width = _rtNormal.height;
            int x = (_rtNormal.width - width) / 2;
            Texture2D tex = new Texture2D(width, _rtNormal.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(x, 0, width, _rtNormal.height), 0, 0);
            tex.Apply();
            RenderTexture.active = oldRt;
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(fileName, bytes);
        }
    }
}
