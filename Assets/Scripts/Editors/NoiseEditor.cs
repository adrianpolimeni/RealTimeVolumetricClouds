using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editors
{
    [ExecuteInEditMode]
    [CustomEditor(typeof(Noise))]
    public class NoiseEditor : Editor
    {
        Noise noise;
        Texture2D[] slices;
        int sliceIndex = 0;
        int texTab = 0;
        int shapeTab = 0;
        int detailTab = 0;
        bool textureFoldout = true;
        bool defaultFoldout = false;
        System.Random rand;
       
        public override void OnInspectorGUI()
        {
            noise = (Noise)target;
            texTab = GUILayout.Toolbar(texTab, new string[] { "Shape", "Detail" });
            noise.activeTextureType = (Noise.NoiseType)texTab;
            switch (texTab)
            {
                case 0:
                    DrawShapeMenu();
                    break;
                case 1:
                    DrawDetailMenu();
                    break;
            }
            if (defaultFoldout = EditorGUILayout.Foldout(defaultFoldout, "Shader Setup"))
            {
                DrawDefaultInspector();
            }
            if (GUILayout.Button("Save Noise Settings")) {
                noise.SaveSettings();
            }
        }

        void DrawShapeMenu() {
            shapeTab = GUILayout.Toolbar(shapeTab, new string[] { "Low", "Medium", "High", "Highest" });
            noise.activeChannel = (Noise.NoiseChannel)shapeTab;
            DrawChannelMenu(0, shapeTab);
        }

        void DrawDetailMenu()
        {
            detailTab = GUILayout.Toolbar(detailTab, new string[] { "Low", "Medium", "High"});
            noise.activeChannel = (Noise.NoiseChannel)detailTab;
            DrawChannelMenu(1, detailTab);
        }

        void DrawChannelMenu(int type, int channel) {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Noise Settings", EditorStyles.boldLabel);
            noise.OnSettingsChange();
            NoiseSettings noiseSettings = noise.activeSettings;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Seed");
            if (GUILayout.Button("Generate New Seed")) {
                rand = new System.Random(noiseSettings.seed);
                noiseSettings.seed = rand.Next();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                    GUILayout.Label("Cell Divisions of Layers");
                    noiseSettings.frequencyA = EditorGUILayout.IntSlider( noiseSettings.frequencyA, 1, 64);
                    EditorGUILayout.Separator();
                    noiseSettings.frequencyB = EditorGUILayout.IntSlider( noiseSettings.frequencyB, 1, 64);
                    EditorGUILayout.Separator();
                    noiseSettings.frequencyC = EditorGUILayout.IntSlider( noiseSettings.frequencyC, 1, 64);
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                    GUILayout.Label("Mix");
                    noiseSettings.mix = GUILayout.VerticalSlider( noiseSettings.mix, 0, 1);
                EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (textureFoldout = EditorGUILayout.Foldout(textureFoldout, "View Cross Section"))
            {
                Texture2D display = GetCrossSection(type, channel, sliceIndex);

                if (display != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (sliceIndex > display.width - 1)
                        sliceIndex = 0;
                    sliceIndex = (int)GUILayout.VerticalSlider(sliceIndex, 0, display.width - 1);
                    GUILayout.Label(display);
                    EditorGUILayout.EndHorizontal();
                }
                else
                    EditorGUILayout.HelpBox("Texture not found!", MessageType.Warning); 
            }
        }

        public Texture2D GetChannelTexture(Texture2D inputTexture, int index) {
            if (inputTexture == null) {
                return null;
            }
            int size = inputTexture.width;
            Texture2D output = new Texture2D(size, size);

            Color[] pixels = inputTexture.GetPixels();
            Color[] channel = new Color[pixels.Length];
            for (int j = 0; j < pixels.Length; j++)
            {
                float val = pixels[j][index];
                channel[j] = new Color(val, val, val);
            }
            output.SetPixels(channel);
            output.Apply();
            
            return output;
        }


        public Texture2D GetCrossSection(int type, int channel, int zIndex) {
            ComputeShader slicer = noise.crossSection;
            if (slicer == null)
                return null;
            RenderTexture noiseTexture = noise.GetTexture(type);
            int size = noiseTexture.width;
        
            slicer.SetTexture(0, "noiseTexture", noiseTexture);
            RenderTexture crossSection = new RenderTexture(size, size, 0);

            crossSection.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            crossSection.enableRandomWrite = true;
            crossSection.Create();

            slicer.SetTexture(0, "crossSection", crossSection);
            slicer.SetInt("zIndex", zIndex);
            int numThreadGroups = Mathf.CeilToInt(size / 32f);
            slicer.Dispatch(0, numThreadGroups, numThreadGroups, 1);

            return GetChannelTexture(ToTexture2D(crossSection), channel);
        }

        Texture2D ToTexture2D(RenderTexture rendered)
        {
            Texture2D output = new Texture2D(rendered.width, rendered.height);
            RenderTexture.active = rendered;
            output.ReadPixels(new Rect(0, 0, rendered.width, rendered.height), 0, 0);
            output.Apply();
            RenderTexture.active = null;
            return output;
        }
    }
}
