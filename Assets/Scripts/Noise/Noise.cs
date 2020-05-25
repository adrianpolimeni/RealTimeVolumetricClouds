using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class Noise : MonoBehaviour
{
    [HideInInspector]
    public enum NoiseType { Shape, Detail }
    [HideInInspector]
    public enum NoiseChannel { R, G, B, A }

    public ComputeShader noiseCompute;

    [HideInInspector]
    public NoiseType activeTextureType;
    [HideInInspector]
    public NoiseChannel activeChannel;
    [HideInInspector]
    public NoiseSettings activeSettings;

    readonly int[] TEXTURE_SIZE = { 128, 64 };

    public ComputeShader crossSection;

    [SerializeField, HideInInspector]
    public NoiseSettings[] settingsList;
    bool updateNoise;
    List<ComputeBuffer> buffers;

    [SerializeField, HideInInspector]
    public RenderTexture shapeTexture;
    [SerializeField, HideInInspector]
    public RenderTexture detailTexture;

    void Awake()
    {
        shapeTexture = CreateTexture(TEXTURE_SIZE[0]);
        detailTexture = CreateTexture(TEXTURE_SIZE[1]);

        settingsList = LoadSettings();
        foreach (var settings in settingsList)
        {
            if (settings != null)
                ForceUpdate(settings);
            else
                ForceUpdate(new NoiseSettings());
        }
        updateNoise = true;
        activeSettings.Set(settingsList[0]);
    }

    public void UpdateNoise(NoiseSettings settings = null)
    {
        settings = settings == null ? activeSettings : settings;

        if (updateNoise && noiseCompute && settings != null)
        {
            RenderTexture texture = GetTexture(settings.type);
            updateNoise = false;
            buffers = new List<ComputeBuffer>();

            noiseCompute.SetFloat("layerMix", settings.mix);
            noiseCompute.SetInt("resolution", TEXTURE_SIZE[settings.type]);
            noiseCompute.SetVector("channelMask", ChannelMask((NoiseChannel)settings.channel));
            noiseCompute.SetTexture(0, "result", texture);
            var limitsBuffer = SetBuffer(new int[] { int.MaxValue, 0 }, sizeof(int), "limits");
            UpdateProperties(settings);
 
            int threads = Mathf.CeilToInt(TEXTURE_SIZE[settings.type] / 8.0f);

            noiseCompute.Dispatch(0, threads, threads, threads);
           
            noiseCompute.SetBuffer(1, "limits", limitsBuffer);
            noiseCompute.SetTexture(1, "result", texture);
            noiseCompute.Dispatch(1, threads, threads, threads);

            foreach (var buffer in buffers)
                buffer.Release();
        }
    }

    public RenderTexture GetTexture(int index)
    {
        if (index == 0)
        {
            return shapeTexture;
        }
        return detailTexture;
    }

    public NoiseSettings GetSetting(int shapeIndex, int channelIndex)
    {
        if (shapeIndex > 1 || channelIndex > 3 - shapeIndex)
            return null;
        return settingsList[shapeIndex * 4 + channelIndex];
    }

    public Vector4 ChannelMask(NoiseChannel index)
    {
        Vector4 channelWeight = new Vector4();
        channelWeight[(int)index] = 1;
        return channelWeight;
    }

    void UpdateProperties(NoiseSettings settings)
    {
        System.Random rand = new System.Random(settings.seed);
        GenerateRandomPoints(rand, settings.frequencyA, "pointsA");
        GenerateRandomPoints(rand, settings.frequencyB, "pointsB");
        GenerateRandomPoints(rand, settings.frequencyC, "pointsC");

        noiseCompute.SetInt("frequencyA", settings.frequencyA);
        noiseCompute.SetInt("frequencyB", settings.frequencyB);
        noiseCompute.SetInt("frequencyC", settings.frequencyC);
    }

    void GenerateRandomPoints(System.Random rand, int numCells, string buffer)
    {
        Vector3[] points = new Vector3[(int)Math.Pow(numCells, 3)];
        
        for (int x = 0; x < numCells; x++)
        {
            for (int y = 0; y < numCells; y++)
            {
                for (int z = 0; z < numCells; z++)
                {
                    Vector3 randomPosition = new Vector3(
                        (float)rand.NextDouble(),
                        (float)rand.NextDouble(),
                        (float)rand.NextDouble());
                    int index = x + numCells * (y + z * numCells);
                    points[index] = (new Vector3(x, y, z) + randomPosition) / (float)numCells;
                }
            }
        }

        SetBuffer(points, sizeof(float) * 3, buffer);
    }

    ComputeBuffer SetBuffer(Array data, int stride, string bufferName)
    {
        var buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Structured);
        buffer.SetData(data);
        buffers.Add(buffer);
        noiseCompute.SetBuffer(0, bufferName, buffer);
        return buffer;
    }

    RenderTexture CreateTexture(int size)
    {
        RenderTexture output = new RenderTexture(size, size, 0);
        output.wrapMode = TextureWrapMode.Repeat;
        output.filterMode = FilterMode.Bilinear;
        output.volumeDepth = size;
        output.enableRandomWrite = true;
        output.dimension = TextureDimension.Tex3D;
        output.graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
        output.Create();
        return output;
    }

    public void ForceUpdate(NoiseSettings settings = null)
    {
        if (settings == null)
            settings = activeSettings;
        updateNoise = true;
        UpdateNoise(settings);
    }

    public bool OnSettingsChange()
    {
        if (activeTextureType != (NoiseType)activeSettings.type || activeChannel != (NoiseChannel)activeSettings.channel)
        {
            NoiseSettings settings;

            settings = GetSetting((int)activeTextureType, (int)activeChannel);
            if (settings == null)
            {
                return false;
            }
            activeSettings = settings;
            return true;
        }
        updateNoise = true;
        return false;
    }

    public void SaveSettings()
    {
        NoiseSettingsCollection save = new NoiseSettingsCollection(settingsList);
        string jsonString = JsonUtility.ToJson(save);
        System.IO.File.WriteAllText(Application.dataPath + "/Settings/" + SceneManager.GetActiveScene().name + ".json", jsonString);
    }

    public NoiseSettings[] LoadSettings()
    {
        string path = Application.dataPath + "/Settings/" + SceneManager.GetActiveScene().name + ".json";
        NoiseSettings[] output;
        try
        {
            string contents = File.ReadAllText(path);
            output = JsonUtility.FromJson<NoiseSettingsCollection>(contents).settings;
        }
        catch (FileNotFoundException) { return null; }

        return output;
    }
}


[Serializable]
public class NoiseSettingsCollection
{
    public NoiseSettings[] settings;
    public NoiseSettingsCollection(NoiseSettings[] settings) {
        this.settings = settings;
    }
}

[Serializable]
public class NoiseSettings
{
    public int type;
    public int channel;
    public int seed;
    public float mix;
    public int frequencyA;
    public int frequencyB;
    public int frequencyC;

    public NoiseSettings Clone()
    {
        return JsonUtility.FromJson<NoiseSettings>(JsonUtility.ToJson(this));
    }
    public void Set(NoiseSettings settings)
    {
        type = settings.type;
        channel = settings.channel;
        seed = settings.seed;
        mix = settings.mix;
        frequencyA = settings.frequencyA;
        frequencyB = settings.frequencyB;
        frequencyC = settings.frequencyC;
    }
}
