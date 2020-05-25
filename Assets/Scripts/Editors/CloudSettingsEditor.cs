using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Clouds
{
    [ExecuteInEditMode]
    [CustomEditor(typeof(CloudSettings))]
    public class CloudSettingsEditor : Editor
    {
        private int tab = 0;
        CloudSettings settings;
        public override void OnInspectorGUI()
        {
          
            settings = (CloudSettings)target;

            tab = GUILayout.Toolbar(tab, new string[] { "Noise", "Ray-March", "Lighting", "Movement" });
            switch (tab)
            {
                case 0:
                    DrawNoiseMenu();
                    break;
                case 1:
                    DrawRayMarchMenu();
                    break;
                case 2:
                    DrawLightingMenu();
                    break;
                case 3:
                    DrawMovementMenu();
                    break;
            }
            SceneView.RepaintAll();
        }


        void DrawNoiseMenu()
        {
            settings.cloudScale = EditorGUILayout.FloatField("Cloud Scale",settings.cloudScale);
            settings.densityMultiplier = EditorGUILayout.FloatField("Cloud Density", settings.densityMultiplier);
            settings.noiseWeights = EditorGUILayout.Vector4Field("Cloud Weights", settings.noiseWeights);
            settings.detailScale = EditorGUILayout.FloatField("Detail Scale", settings.detailScale);
            settings.detailMultiplier = EditorGUILayout.FloatField("Detail Density", settings.detailMultiplier);
            settings.detailNoiseWeights = EditorGUILayout.Vector3Field("Detail Weights", settings.detailNoiseWeights);
            settings.volumeOffset = EditorGUILayout.FloatField("Volume Offset", settings.volumeOffset);
            settings.densityOffset = EditorGUILayout.FloatField("Density Offset", settings.densityOffset);
            settings.heightMapFactor = EditorGUILayout.Slider("Height Map Factor", settings.heightMapFactor, 0, 1);
        }

        void DrawRayMarchMenu()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("March Steps");
            settings.marchSteps = EditorGUILayout.IntSlider(settings.marchSteps, 1, 32);
            EditorGUILayout.LabelField("Ray Offset");
            settings.rayOffset = EditorGUILayout.Slider(settings.rayOffset, 0, 50);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
            // Noise Texture from http://momentsingraphics.de/BlueNoise.html
            settings.blueNoise = TextureField("BlueNoise", settings.blueNoise);
            EditorGUILayout.EndHorizontal();
        }


        void DrawLightingMenu()
        {
            settings.brightness = EditorGUILayout.Slider("Brightness",settings.brightness, 0, 1);
            settings.transmitThreshold = EditorGUILayout.Slider("Transmit Threshold", settings.transmitThreshold, 0, 1);
            settings.inScatterMultiplier = EditorGUILayout.Slider("In-Scatter", settings.inScatterMultiplier, 0, 1);
            settings.outScatterMultiplier = EditorGUILayout.Slider("Out-Scatter", settings.outScatterMultiplier, 0, 1);
            settings.forwardScattering = EditorGUILayout.Slider("Forward-Scatter", settings.forwardScattering, 0, 1);
            settings.backwardScattering = EditorGUILayout.Slider("Backward-Scatter", settings.backwardScattering, 0, 1);
            settings.scatterMultiplier = EditorGUILayout.Slider("Scatter Multiplier", settings.scatterMultiplier, 0, 1);
        }

        void DrawMovementMenu()
        {
            settings.cloudSpeed = EditorGUILayout.Vector3Field("Cloud Speed", settings.cloudSpeed);
            settings.detailSpeed = EditorGUILayout.Vector3Field("Detail Speed", settings.detailSpeed);
        }

        // This method is used to create a texture select in the editor window
        // Found on Unity Forum. Credit to: Doug Richardson
        // https://answers.unity.com/questions/1424385/how-to-display-texture-field-with-label-on-top-lik.html
        private static Texture2D TextureField(string name, Texture2D texture)
        {
            GUILayout.BeginVertical();
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.UpperCenter;
            style.fixedWidth = 64;
            GUILayout.Label(name, style);
            var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64));
            GUILayout.EndVertical();
            return result;
        }

    }
    
}
