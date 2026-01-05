# 🌦️ AR Weather Visualiser

**AR Weather Visualiser** is a mobile Augmented Reality (AR) application that blends real-world weather data with immersive 3D visuals.  
By detecting a vertical surface, users can tap to spawn a **portal window** that reveals a live AR environment matching their **current weather** — sunny, cloudy, rainy, or snowy.

---

## 🧠 Concept

The goal of this project was to explore how **markerless AR** can merge real-time data with interactive 3D visualization.  
Each portal acts as a window into another world, showing different scenes that react to the user’s actual environment and location.

---

## 🎯 Core Features

- **Vertical Plane Detection:**  
  Detects walls or other vertical surfaces using Unity’s AR Foundation.

- **Weather API Integration:**  
  Fetches real-time hourly weather and temperature using the [Open-Meteo API](https://open-meteo.com/).

- **Dynamic Portal Spawning:**  
  Tap on a detected wall to spawn a portal that displays a 3D environment representing the current weather condition.

- **Live Temperature Display:**  
  Displays the current temperature via a TextMeshPro (TMP) label before spawning the portal.

---

## 🧩 Weather Environments

Each portal loads one of four environment prefabs:

| Weather | Visual Style                       |
|----------|------------------------------------|
| ☀️ **Sunny** | Bright lighting, clear sky         |
| ☁️ **Cloudy** | Overcast sky, softer lighting      |
| 🌧️ **Rainy** | Overcast sky, rain particle system |
| ❄️ **Snowy** | Bright sky, falling snow particles |

All environments share the same base layout for performance consistency and use custom sky planes and particle systems to differentiate them.

---

## 🧱 Technical Stack

- **Engine:** Unity (AR Foundation + ARCore)
- **Rendering:** Universal Render Pipeline (URP)
- **Language:** C#
- **Libraries/Tools:**
    - TextMeshPro (TMP)
    - UnityWebRequest (for API calls)
    - Open-Meteo API (for weather data)

---

## 🏗️ How It Works

1. The app starts by requesting location access.
2. It fetches the current weather and temperature via Open-Meteo’s hourly forecast.
3. A weather-appropriate portal prefab is chosen.
4. The user taps on a detected vertical surface.
5. The portal appears on the wall and displays the 3D environment through a masked window.

---

## 🧠 Learning Focus

This project helped me practice:

- AR Foundation workflows (plane detection, raycasting, and placement)
- Real-time data integration with APIs
- Custom URP renderer setup and stencil masking
- UI feedback and interaction design using TMP
- Managing AR performance and user experience on mobile

---

## 📸 Demo

- **AR project video:** https://drive.google.com/file/d/1d1T9XLg5tizTB8M4Bz9okE1U6FnTbXh-/view?usp=sharing
- **VR project video:** https://drive.google.com/file/d/1SDOlnKsA1FRAVjnPXHZoSw7s93iGwXVm/view?usp=sharing

---

## 📝 Blog Posts & Personal Reflection

- [Blogpost Week 1](BlogPosts/Blogpost-Week1.md)
- [Blogpost Week 2](BlogPosts/Blogpost-Week2.md)
- [Blogpost Week 3](BlogPosts/Blogpost-Week3.md)
- [Blogpost Week 4](BlogPosts/Blogpost-Week4.md)
- [Blogpost Week 5](BlogPosts/Blogpost-Week5.md)
- [Blogpost Week 6](BlogPosts/Blogpost-Week6.md)
- [Blogpost Week 7](BlogPosts/Blogpost-Week7.md)
- [Personal Reflection](BlogPosts/Personal-Reflection.md)

---

## Assets Used
| Asset | Purpose                                                           | Link |
|--------|-------------------------------------------------------------------|------|
| **Modular House Pack 1** | Environment base for portal scenes                                | [Unity Asset Store → Modular House Pack 1](https://assetstore.unity.com/packages/3d/environments/urban/modular-house-pack-1-236466) |
| **AllSky Free – 10 Sky / Skybox Set** | Skyboxes for sunny, cloudy, rainy, and snowy conditions           | [Unity Asset Store → AllSky Free Skybox Set](https://assetstore.unity.com/packages/2d/textures-materials/sky/allsky-free-10-sky-skybox-set-146014) |
| **Unity Learn – Mobile AR Development Pathway** | Guidance for AR Foundation setup and workflows,used custom shader | [Unity Learn → Mobile AR Development Pathway](https://learn.unity.com/pathway/mobile-ar-development) |


## 👨‍💻 Author

**Apurva Mishra**  
Built as part of the **XRD course** in VIA University College.  
Uses Unity’s AR Foundation and Open-Meteo API to visualise live weather through AR.

---
