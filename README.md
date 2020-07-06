# Real-Time Volumetric Clouds

![First-Person Demo](https://github.com/adrianpolimeni/RealTimeVolumetricClouds/blob/master/ScreenShots/FirstPersonScene2.png)

### Introduction
From classic 8-bit Super Mario Bros, to the ultra realistic AAA titles of today, graphical representation of clouds are ubiquitous in games. Over the past decade a variety of methods have been used to create graphically realistic volumetric clouds in real-time. The aim of my project is to create a working demo of these clouds using a variety of methods as described in other scholarly material. This paper will primarily focus on the methods I used in my implementation.

### Theory & Strategy
Before diving into the implementation, I briefly want to look at the physical properties of clouds. Clouds are a collection of water droplets (or ice crystals) in the atmosphere. Individually shaped clouds form when water vapor joins together. There are many different types of clouds, however I will focus on creating cumulus clouds in my implementation. 

The general approach to rendering clouds is to generate a geometry using noise, then  ray-march to determine the density and lighting at each sample position. I will use Fredrik Häggström’s paper (2018) as a primary resource for this project. I have decided to use the Unity Game Engine (version 2019.1.3f1) to build my implementation. Unity provides various frameworks and libraries. This will be helpful in managing 3D textures during noise generation. Unity also supports shader code, which we will use for ray-marching and noise generation.        

### Noise
The geometry of clouds are random, yet somewhat predictable. The goal is to generate random geometries which are also billowy in nature. Noise is the preferred method to create this geometry, as we can tweak the algorithm to output visually desired properties yet still appear random. Most resources that I found use layered 3D Worley noise to create the cloud geometry. The cellular look of Worley noise will be utilized to create billowy clumps of clouds. If noise is layered in varying sizes, the resulting texture will contain more detailed variation. I like to think that this gives the clouds more personality.   

Creating Worley noise was the first challenge of my implementation. The noise needed to be tileable, which required a slightly more advanced implementation (Vivo & Lowe, 2015a). The first step in creating Worley noise was to evenly divide a cubic area into cells. The next step was to assign one point with a random position inside every cell. Once these parameters were determined, a shader could then be used to determine each fragment of the noise texture. Each fragment of the Worley noise texture is assigned a grayscale color value. This value is the distance between the fragment and the position of the closest random point, mapped to range between zero and one. 

After implementing Worley noise generation, I got started on Perlin noise. I was able to build a Perlin noise function that utilizes the same parameters as the Worley noise function. Both methods of noise generation utilizes a grid (or cells) to organize random values. With some basic vector math, I converted each random point to a random gradient vector. I used this gradient vector to compute a pseudo-random color value for each fragment using a similar implementation as my Worley noise function (Vivo & Lowe, 2015b).

The next step was to combine layer the noise. There are two stages where noise is combined. In the first stage, three layers of noise are combined (by interpolation) within each separate RGBA channels of a 3D texture. Two layers of Perlin noise is combined with a layer of Worley noise to form the low frequency red channel of the texture. The remaining channels contain 3 layers of higher frequency Worley noise. In the second stage, each channel of noise is combined using a technique known as fractal brownian motion (FBM). FBM is a simple method of combining various frequencies, where each frequency is first multiplied by some arbitrary amplitude before being added to the final result (Vivo & Lowe, 2015c). 

![Figure 1](https://github.com/adrianpolimeni/RealTimeVolumetricClouds/blob/master/Misc/Fig1.png)

**Figure 1**: 2D cross-sections of 3D Perlin Noise (left), Inverted Worley Noise (center), and Perlin-Worley Noise (right).

### Cloud Density
The basic approach in visualizing clouds is to raycast from the view to the random noise texture, and apply lighting to the result. The first step in this approach is to specify an area in which clouds can exist. This area contains the random 3D noise. I refer to this area as the “cloud-box”. For each ray cast into the scene, a ray-box intersection algorithm is used to determine if the ray hits the cloud-box (Majercik, 2018). Raycasting volumetric clouds requires multiple samples to be taken along the ray. Each sample contains the cloud’s density at that position. If the density is zero, no clouds will be drawn. The density of a sample is determined by multiple factors which can be categorized into: noise, distance to box’s edge, and sample height.

Following Häggström’s paper, I used multiple layers of noise to achieve realistic cloud density. This includes a high resolution (1283 pixels) layered noise texture, and a low resolution (643 pixels) layered noise texture. The high resolution noise texture (a.k.a Shape noise) determines larger cloud features, while the low resolution noise texture (a.k.a Detail noise) is used to provide finer detail. The resulting difference between these two textures resembles realistic clouds.          

There were visible artifacts along the sides of the cloud-box after sampling the noise. Since there was no gradient between areas of high density and the outside of the cloud-box, clouds along the edges of the cloud-box would appear to be cut off. The solution to this problem was to interpolate between zero and the noise density by the sample’s distance to the edge. This was a simple but effective way to apply smoothness to the result.

![Figure 2](https://github.com/adrianpolimeni/RealTimeVolumetricClouds/blob/master/Misc/Fig2.png)

**Figure 2**: Cloud-box without gradient edges (left), and with gradient edges (right).

At this point of the implementation the shape of the clouds were looking fairly realistic. The one issue I had was that the clouds were evenly distributed throughout the height of the cloud-box. Häggström and many others use something known as a weather-map to control the parameters that affect the overall look of the clouds, this includes cloud distribution. However, I decided not to implement a full weather-map system as I was only concerned about the vertical distribution of the clouds. I was able to find an alternative method in a paper by Rikard Olajos where a height distribution (HD) function is used instead (Olajos, 2016). 

As a result of this function, cloud density is greater at the bottom of the cloud-box than at the top. This greatly improves the shape of the clouds, as they exhibit flatter bottoms and occasional towering peaks. 

![Figure 3](https://github.com/adrianpolimeni/RealTimeVolumetricClouds/blob/master/Misc/Fig3.png)

**Figure 3**: Cloud-box without HD function (left), and with HD function (right).

### Blue Noise
The ray-marching algorithm used to sample density is not perfect and occasional visual artifacts can be seen in the form of jagged edges. A high frequency noise known as blue noise can be used to randomly offset the starting position of the ray-march. This straightforward implementation solved the problem. However, one of the drawbacks of this implementation was that the render was slightly more noisy. 

### Lighting
Lighting the density was the final challenge of my implementation. Since clouds are volumetric, light will be able to reach the center of clouds, requiring a more advanced method of lighting. The general method for lighting the clouds is to ray-march towards our light source while sampling densities along the ray. However, light is absorbed as it passes through a cloud, resulting in a lighter exterior and darker interior. Various paper’s on the topic recommend using Beer’s law to calculate attenuation. Using Beer’s law, our final density will be equal to the natural number to the power of the negative sum of densities to the light source. Most implementations will multiply the sum of densities by an artistic term used to manipulate the look of the clouds. I used many of these terms throughout my project to experiment with the weights of functions.

Beer’s law is complemented with Henyey-Greenstein’s phase function in many other implementations of lighting. This function is used to calculate the scattering of light inside a medium. This function creates a silver-lining around clouds that are between the viewer and the light source. I was able to add various weighted terms to the phase function so that I could manually tweak the appearance of the light. 

![Figure 4](https://github.com/adrianpolimeni/RealTimeVolumetricClouds/blob/master/Misc/Fig4.png)

**Figure 4**: Using Beer’s law for attenuation (left). Beer’s law complemented with Henyey-Greenstein’s phase function (right).

### Additional Features
During implementation I noticed that I was rendering the clouds on top of all other objects in the scene. Unity has a built-in depth texture function, which stores the Z-value of each pixel in the view. I was able to use the depth texture values to limit the ray-marching steps. 

The Unity engine enabled me to create an extensive UI in the editor to control various parameters. The UI was separated into subsections for controlling: noise, ray-marching, lighting and movement. I also added a function for saving noise parameters by writing values to a json file.

### Results and Conclusion
My goal at the start of the project was very lenient. I would consider the implementation a success if I could render a cloud at an acceptable frame rate (anything above 30 FPS). After many iterations, I was able to successfully render volumetric clouds at a consistent 60 FPS. The method used to light the clouds worked great, resulting in clouds with very realistic appearances.

### Works Cited

[Häggström, F. (2018). Real-time rendering of volumetric clouds.](https://umu.diva-portal.org/smash/get/diva2:1223894/FULLTEXT01.pdf)

[Majercik, A., Crassin, C., Shirley, P., & McGuire, M. (2018). A ray-box intersection algorithm and efficient dynamic voxel rendering. Journal of Computer Graphics Techniques Vol, 7(3). Olajos, R. (2016). Real-Time Rendering of Volumetric Clouds. LU-CS-EX 2016-42, 20.](http://lup.lub.lu.se/luur/download?func=downloadFile&recordOId=8893256&fileOId=8893258)

[Vivo, P.G.  and  Lowe J. (2015). The Book of Shaders.](https://thebookofshaders.com/)

 

