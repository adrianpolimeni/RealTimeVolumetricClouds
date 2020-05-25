using Assets.Scripts.Clouds;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class Clouds : MonoBehaviour
{
    public Shader shader;  
    public GameObject cloudBox;
    private CloudSettings cs;

    public int avgFrameRate;
    public Text displayText;
    private float fpsDisplayUpdate = 1;

    [HideInInspector]
    public Material material;
    
    public void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // Validate inputs
        if (material == null)
        {
            material = new Material(shader);
        }
        // Noise
        var noise = FindObjectOfType<Noise>();
        noise.UpdateNoise();

        Vector3 size = cloudBox.transform.localScale;
        Vector3 position = cloudBox.transform.position;

        material.SetTexture("NoiseTex", noise.shapeTexture);
        material.SetTexture("DetailNoiseTex", noise.detailTexture);
        material.SetFloat("scale", Settings.cloudScale);
        material.SetFloat("densityMultiplier", Settings.densityMultiplier);
        material.SetFloat("densityOffset", Settings.densityOffset);
        material.SetFloat("volumeOffset", Settings.volumeOffset);
        material.SetFloat("detailNoiseScale", Settings.detailScale);
        material.SetFloat("detailNoiseMultiplier", Settings.detailMultiplier);
        material.SetVector("detailWeights", Settings.detailNoiseWeights);
        material.SetVector("noiseWeights", Settings.noiseWeights);
        material.SetVector("boundsMin", position - size / 2);
        material.SetVector("boundsMax", position + size / 2);
        material.SetFloat("heightMapFactor", Settings.heightMapFactor);

        material.SetInt("marchSteps", Settings.marchSteps);
        material.SetFloat("rayOffset", Settings.rayOffset);
        material.SetTexture("BlueNoise", Settings.blueNoise);
        material.SetFloat("brightness", Settings.brightness);
        material.SetFloat("transmitThreshold", Settings.transmitThreshold);
        material.SetFloat("inScatterMultiplier", Settings.inScatterMultiplier);
        material.SetFloat("outScatterMultiplier", Settings.outScatterMultiplier);
        material.SetFloat("forwardScatter", Settings.forwardScattering);
        material.SetFloat("backwardScatter", Settings.backwardScattering);
        material.SetFloat("scatterMultiplier", Settings.scatterMultiplier);
        material.SetFloat("timeScale", (Application.isPlaying) ? 1 : 0); // Prevent cloud movement during editing 
        material.SetVector("cloudSpeed", Settings.cloudSpeed);
        material.SetVector("detailSpeed", Settings.detailSpeed);

        displayFPS();

        Graphics.Blit (src, dest, material);
    }

    private void displayFPS() {
        if (displayText == null)
            return;
        float current = (int)(1f / Time.unscaledDeltaTime);
        avgFrameRate = (int)current;

        if (Time.time >= fpsDisplayUpdate && Application.isPlaying)
        {
            
            fpsDisplayUpdate = Time.time + 0.5f;
            displayText.text = "FPS: " + avgFrameRate.ToString();
        }
    }

    private CloudSettings Settings {
        get {
            if( cs == null)
                cs = cloudBox.GetComponent<CloudSettings>();
            return cs;
        }
    }
}