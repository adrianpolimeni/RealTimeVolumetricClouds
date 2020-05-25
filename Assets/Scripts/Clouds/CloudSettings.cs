using UnityEngine;

    public class CloudSettings : MonoBehaviour
    {
        public float cloudScale;
        public float densityMultiplier;
        public Vector4 noiseWeights;
        public float detailScale;
        public float detailMultiplier;
        public Vector3 detailNoiseWeights;
        public float volumeOffset;
        public float densityOffset;
        public float heightMapFactor;
        public int marchSteps;
        public float rayOffset;
        public Texture2D blueNoise;
        public float brightness;
        public float transmitThreshold;
        public float inScatterMultiplier;
        public float outScatterMultiplier;
        public float forwardScattering;
        public float backwardScattering;
        public float scatterMultiplier;
        public Vector3 cloudSpeed;
        public Vector3 detailSpeed;
    }