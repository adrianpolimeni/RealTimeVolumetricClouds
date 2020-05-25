
Shader "Hidden/Clouds"
{
	// Setup Vertex and Fragment shader
	// Resources: https://docs.unity3d.com/Manual/SL-VertexFragmentShaderExamples.html
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
	
			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float3 view : TEXCOORD1;
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata v) {
				v2f output;
				output.pos = UnityObjectToClipPos(v.vertex);
				output.uv = v.uv;
				float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
				output.view = mul(unity_CameraToWorld, float4(viewVector,0));
				return output;
			}

			// ----- Init Variables -----

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;

			// Noise
			Texture3D<float4> NoiseTex;
			SamplerState samplerNoiseTex;
			float scale;
			float densityMultiplier;
			float4 noiseWeights;

			// Detailed Noise
			Texture3D<float4> DetailNoiseTex;
			SamplerState samplerDetailNoiseTex;
			float detailNoiseScale;
			float detailNoiseMultiplier;
			float3 detailWeights;

			// Hight and Density
			float volumeOffset;
			float densityOffset;
			float heightMapFactor;

			// Ray-March
			int marchSteps;
			float rayOffset;
			Texture2D<float4> BlueNoise;
			SamplerState samplerBlueNoise;

			// Lighting 
			float brightness;
			float transmitThreshold;
			float inScatterMultiplier;
			float outScatterMultiplier;
			float forwardScatter;
			float backwardScatter;
			float scatterMultiplier;
			float4 _LightColor0;

			// Transform
			float3 boundsMin;
			float3 boundsMax;
			float timeScale;
			float3 cloudSpeed;
			float3 detailSpeed;

			// ----- Helper Methods -----

			// Used to scale the blue-noise to fit the view
			float2 scaleUV(float2 uv, float scale) {
				float x = uv.x * _ScreenParams.x;
				float y = uv.y * _ScreenParams.y;
				return float2 (x,y)/scale;
			}

			float maxComponent(float3 vec) {
				return max(max(vec.x, vec.y), vec.z);
			}
			float minComponent(float3 vec) {
				return min(min(vec.x, vec.y), vec.z);
			}

			// Returns min and max t distances
			float2 slabs(float3 p1, float3 p2, float3 rayPos, float3 invRayDir) {
				float3 t1 = (p1 - rayPos) * invRayDir;
				float3 t2 = (p2 - rayPos) * invRayDir;
				return float2(maxComponent(min(t1, t2)), minComponent(max(t1, t2)));
			}

			// Returns the distance to cloud box (x) and distance inside cloud box (y)
			float2 rayBox(float3 boundsMin, float3 boundsMax, float3 rayPos, float3 invRayDir) {
				float2 slabD = slabs(boundsMin, boundsMax, rayPos, invRayDir);
				float toBox = max(0, slabD.x);
				return float2(toBox, max(0, slabD.y - toBox));
			}

			float lerp(float a, float b, float t) {
				return a * (1 - t) + b * t;
			}

			float henyeyGreenstein(float g, float angle) {
				return (1.0f - pow(g,2)) / (4.0f * 3.14159 * pow(1 + pow(g, 2) - 2.0f * g * angle, 1.5f));
			}

			float hgScatter(float angle) {
				float scatterAverage = (henyeyGreenstein(forwardScatter, angle) + henyeyGreenstein(-backwardScatter, angle)) / 2.0f;
				// Scale the brightness by sun position
				float sunPosModifier = 1.0f;
				if (_WorldSpaceLightPos0.y < 0) {
					sunPosModifier = pow(_WorldSpaceLightPos0.y + 1,3);
				}
				return brightness * sunPosModifier + scatterAverage * scatterMultiplier;
			}

			float beer(float d) {
				return exp(-d);
			}

			float heightMap(float h) {
				return lerp(1,(1 - beer(1 * h)) * beer(4 * h), heightMapFactor);
			}

			float densityAtPosition(float3 rayPos) {
				float time = _Time.x * timeScale;
				float3 uvw = ((boundsMax - boundsMin) / 2.0f + rayPos) * scale / 1000.0;
				float3 cloudPosition = uvw  + cloudSpeed * time;	
				float width = min(rayPos.x - boundsMin.x, boundsMax.x - rayPos.x);
				float height = (rayPos.y - boundsMin.y) / (boundsMax.y - boundsMin.y);
				float depth = min( rayPos.z - boundsMin.z, boundsMax.z - rayPos.z);
				float edgeDistance = minComponent(float3(100, width, depth)) / 100;
				float heightMapValue = heightMap(height);
				// Density at point 
				float4 noise = NoiseTex.SampleLevel(samplerNoiseTex, cloudPosition, 0);
				float FBM = dot(noise, normalize(noiseWeights)) * volumeOffset * edgeDistance * heightMapValue; 
				float cloudDensity = FBM + densityOffset * 0.05;

				if (cloudDensity <= 0) {
					return 0;
				}
			
				float3 detailPosition = uvw * detailNoiseScale +  detailSpeed * time;
				float4 detailNoise = DetailNoiseTex.SampleLevel(samplerDetailNoiseTex, detailPosition, 0);
				float detailFBM = dot(detailNoise, normalize(detailWeights)) * (1-heightMapValue);

				// Combine the normal and detail
				float density = cloudDensity - detailFBM * pow(1- FBM, 3) * detailNoiseMultiplier;

				return density * densityMultiplier * 0.1 ;
			}

			// Calculate proportion of light that reaches the given point from the lightsource
			float lightmarch(float3 position) {
				float3 L = _WorldSpaceLightPos0.xyz;
				float stepSize = rayBox(boundsMin, boundsMax, position, 1 / L).y / marchSteps;

				float density = 0;

				for (int i = 0; i < marchSteps; i++) {
					position += L * stepSize;
					density += max(0, densityAtPosition(position) * stepSize);
				}

				float transmit = beer(density * (1 - outScatterMultiplier));
				return lerp(transmit, 1, transmitThreshold);
			} 

			// Fragment Shader
			float4 frag(v2f i) : SV_Target
			{
				// Ray-cast
				float3 E = _WorldSpaceCameraPos;
				float3 D = i.view / length(i.view); 	

				// Ray-box intersection
				float depthTex = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float depth = LinearEyeDepth(depthTex) * length(i.view);
				float2 rayToBox = rayBox(boundsMin, boundsMax, E, 1.0f / D);
				
				// Break early if ray-box intersection is   false 
				if (rayToBox.y == 0) {
					return tex2D(_MainTex, i.uv);
				}
				float3 boxHit = E + D * rayToBox.x;

				// Henyey-Greenstein scatter
				float scatter = hgScatter(dot(D, _WorldSpaceLightPos0.xyz));

				// Blue Noise
				float randomOffset = BlueNoise.SampleLevel(samplerBlueNoise, scaleUV(i.uv, 72), 0);
				float offset = randomOffset * rayOffset;

				float stepLimit = min(depth - rayToBox.x, rayToBox.y);
				float stepSize = 12; 
				float transmit = 1;

				float3 I = 0; // Illumination
				for(int steps = offset; steps < stepLimit; steps+=stepSize) {
					float3 pos = boxHit + D * steps;
					float density = densityAtPosition(pos);

					if (density > 0) {
						I += density * transmit * lightmarch(pos) * scatter;
						transmit *= beer(density  * (1 - inScatterMultiplier));
					}
				}
				float3 color = (I * _LightColor0) + tex2D(_MainTex, i.uv) * transmit;
				return float4(color,0);
			}
			ENDCG
		}
	}
}