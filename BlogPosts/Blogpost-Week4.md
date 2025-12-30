# Week 4 – Portal Spawn Fix, Weather UI Upgrade (TMP), and URP Portal Rendering
---

## Overview

This week was all about finishing the AR Visualiser.  
I:
1. **Fixed the portal spawn alignment** so it now places correctly on vertical planes.
2. **Replaced the old debug text with a temperature UI** using TextMeshPro (TMP).
3. **Expanded the weather system** to show live hourly temperature data.
4. **Created a custom URP renderer setup** so each level only appears through the portal window.
5. **Designed all four level prefabs** (sunny, cloudy, rainy, snowy) using custom skies and particles.

---
## 1) Fixing the Portal Spawn (Rotation + Placement)

I created an **empty root** object to hold each portal (so I can rotate/scale the whole thing without touching the mesh pivots). The hierarchy for a weather portal looks like this:

- **SunnyPortal (root)**
    - **GameObject** *(root pivot helper)*
        - **Window**
            - **Frame**
            - **Glass** *(set to `PortalWindow` layer)*

 *Sunny portal hierarchy:*  
![Screenshot 2025-11-09 171546.png](Images/Week4/Screenshot%202025-11-09%20171546.png)

Key transforms from the setup:

- **GameObject (helper):** `Rotation Y = -90`
- **Window:** `Rotation X = -90, Y = 90, Z = 90`
- **Frame:** `Rotation Z = 180`, `Scale = 0.75`
- **Glass:** `Rotation Z = 180`, `Scale = 0.75`, **Layer = PortalWindow**

These helpers let me zero out mesh‑specific rotations and keep a clean pivot for placement.

### Spawn code (per‑tap)

I kept the same raycast flow and computed an upright rotation from the plane normal:

```csharp
private void Update()
{
    // Only allow spawning if the portal is chosen AND it hasn't been spawned yet
    if (!isReadyToSpawn || portalHasSpawned || !arRaycastManager) return;

    Vector2 screenPos;

    if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
        screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
    else if (Mouse.current?.leftButton.wasPressedThisFrame == true)
        screenPos = Mouse.current.position.ReadValue();
    else
        return; // nothing pressed this frame

    bool hit = arRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon);
    if (!hit) return;

    Pose hitPose = hits[0].pose;

    // Calculate upright rotation for the portal
    Vector3 wallNormal = hitPose.rotation * Vector3.up;
    Vector3 horizontalNormal = new Vector3(wallNormal.x, 0, wallNormal.z).normalized;
    Quaternion faceWall = Quaternion.LookRotation(horizontalNormal, Vector3.up);
    Quaternion uprightRotation = faceWall * Quaternion.Euler(90, 0, 0);

    // Spawn the chosen portal
    Instantiate(portalToSpawn, hitPose.position, uprightRotation);
    portalHasSpawned = true;
    DisablePlaneDetection();
    Debug.Log("Portal spawned!");

    // Hide temperature text after spawning
    if (temperatureText) temperatureText.gameObject.SetActive(false);
}
```

**Why this rotation?**
- `hitPose.rotation * Vector3.up` = the plane’s **normal**.
- I flatten it to the horizontal plane so the forward vector doesn’t tilt with camera noise.
- `LookRotation` makes the portal **face outward** from the wall; the extra `Euler(90,0,0)` stands my mesh upright because the model originally faced down.

This got me close, but small misalignments still happened because of the mesh’s original orientation and typical **gimbal‑style** pitfalls when mixing euler rotations with quaternions. The empty root helped, but I’ll refine alignment further if needed.

---

## 2. Upgrading the Weather System

I noticed that making daily weather calls is not very accurate to the current weather, so I replaced the old daily forecast system with an **hourly weather API call** from **Open-Meteo**.  
Now the app also displays the **current hour’s temperature** before the portal spawns, using **TextMeshPro (TMP)**.

---

### Coroutine Flow

The coroutine handles everything from **permissions** to **API fetch** in sequence.  
It prevents the app from freezing while waiting for GPS or weather data.

Steps:
1. Request Android **FineLocation** permission.
2. Start **Input.location** with moderate accuracy.
3. Wait for GPS fix (max 20 seconds).
4. Use the coordinates to query **Open-Meteo**.
5. Parse hourly temperature and weather code.
6. Display the temperature in TMP.
7. Prepare the correct portal prefab for spawning.
*

---

### New API URL Example

```
https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&hourly=temperature_2m,weather_code&forecast_days=1
```

**Parsed JSON structure:**
```json
{
  "hourly": {
    "time": ["2025-11-09T12:00"],
    "temperature_2m": [22.3],
    "weather_code": [3]
  }
}
```

I map `DateTime.Now.Hour` to the correct hourly index and display that hour’s data.

**Serializable classes:**
```csharp
[System.Serializable]
public class HourlyWeather {
    public List<string> time;
    public List<float> temperature_2m;
    public List<int> weather_code;
}

[System.Serializable]
public class WeatherResponse {
    public HourlyWeather hourly;
}
```

![Screenshot 2025-11-09 195214.png](Images/Week4/Screenshot%202025-11-09%20195214.png)

---

## 3. Full TapToPlace Script (Final Version)

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystemS;
using UnityEngine.Networking;
using System;
using TMPro;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[System.Serializable]
public class HourlyWeather {
    public List<string> time;
    public List<float> temperature_2m;
    public List<int> weather_code;
}

[System.Serializable]
public class WeatherResponse {
    public HourlyWeather hourly;
}

public class TapToPlace : MonoBehaviour {
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private ARPlaneManager arPlaneManager;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI temperatureText;

    [Header("Weather Portals")]
    [SerializeField] private GameObject sunnyWeatherPortal;
    [SerializeField] private GameObject cloudyWeatherPortal;
    [SerializeField] private GameObject rainyWeatherPortal;
    [SerializeField] private GameObject snowyWeatherPortal;

    private static readonly List<ARRaycastHit> hits = new();
    private GameObject portalToSpawn;
    private bool isReadyToSpawn = false;
    private bool portalHasSpawned = false;

    void Start() {
        if (temperatureText) temperatureText.text = "Fetching weather...";
        StartCoroutine(GetWeatherAndPreparePortal());
    }

    IEnumerator GetWeatherAndPreparePortal() {
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation)) {
            Permission.RequestUserPermission(Permission.FineLocation);
            float timer = 0;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && timer < 20f) {
                yield return new WaitForSeconds(1);
                timer++;
            }
        }
        #endif

        if (!Input.location.isEnabledByUser) {
            if (temperatureText) temperatureText.text = "Location Off";
            portalToSpawn = rainyWeatherPortal; isReadyToSpawn = true; yield break;
        }

        Input.location.Start(500f);
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait-- > 0)
            yield return new WaitForSeconds(1);

        if (maxWait <= 0 || Input.location.status == LocationServiceStatus.Failed) {
            if (temperatureText) temperatureText.text = "Location Failed";
            portalToSpawn = sunnyWeatherPortal; isReadyToSpawn = true; Input.location.Stop(); yield break;
        }

        var data = Input.location.lastData;
        Input.location.Stop();

        string url = $"https://api.open-meteo.com/v1/forecast?latitude={data.latitude}&longitude={data.longitude}&hourly=temperature_2m,weather_code&forecast_days=1";
        using (var www = UnityWebRequest.Get(url)) {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                var weather = JsonUtility.FromJson<WeatherResponse>(www.downloadHandler.text);
                int idx = DateTime.Now.Hour;
                if (weather?.hourly?.weather_code?.Count > idx && weather.hourly.temperature_2m?.Count > idx) {
                    int code = weather.hourly.weather_code[idx];
                    SelectPortal(code);
                    float temp = weather.hourly.temperature_2m[idx];
                    if (temperatureText) temperatureText.text = $"{temp:F1}°C";
                } else {
                    if (temperatureText) temperatureText.text = "Weather Error";
                    portalToSpawn = sunnyWeatherPortal;
                }
            } else {
                if (temperatureText) temperatureText.text = "API Error";
                portalToSpawn = rainyWeatherPortal;
            }
        }
        isReadyToSpawn = true;
    }

    private void SelectPortal(int code) {
        if (code >= 0 && code <= 1) portalToSpawn = sunnyWeatherPortal;
        else if (code >= 2 && code <= 48) portalToSpawn = cloudyWeatherPortal;
        else if (code >= 51 && code <= 67) portalToSpawn = rainyWeatherPortal;
        else if (code >= 71 && code <= 77) portalToSpawn = snowyWeatherPortal;
        else if (code >= 80 && code <= 99) portalToSpawn = rainyWeatherPortal;
        else portalToSpawn = sunnyWeatherPortal;
        if (!portalToSpawn) portalToSpawn = sunnyWeatherPortal;
    }

    void Update() {
        if (!isReadyToSpawn || portalHasSpawned || !arRaycastManager) return;

        Vector2 screenPos;
        if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        else if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            screenPos = Mouse.current.position.ReadValue();
        else return;

        if (!arRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon)) return;

        var hitPose = hits[0].pose;
        var wallNormal = hitPose.rotation * Vector3.up;
        var horizontalNormal = new Vector3(wallNormal.x, 0, wallNormal.z).normalized;
        var faceWall = Quaternion.LookRotation(horizontalNormal, Vector3.up);
        var uprightRotation = faceWall * Quaternion.Euler(90, 0, 0);

        Instantiate(portalToSpawn, hitPose.position, uprightRotation);
        portalHasSpawned = true;
        if (arPlaneManager) { arPlaneManager.enabled = false; foreach (var p in arPlaneManager.trackables) p.gameObject.SetActive(false); }
        if (temperatureText) temperatureText.gameObject.SetActive(false);
    }
}
```

---

## 4. Designing Level Prefabs

I built the four levels from the same base environment to save time. The **differences** come from **sky planes** and **particles**:

- **Sunny:** blue sky, no rain/snow.  
  ![Screenshot 2025-11-09 172726.png](Images/Week4/Screenshot%202025-11-09%20172726.png)
- **Rainy:** grey sky + rain particle system.  
 ![Screenshot 2025-11-09 172820.png](Images/Week4/Screenshot%202025-11-09%20172820.png)
- **Cloudy:** grey sky without particles.  
  ![Screenshot 2025-11-09 172921.png](Images/Week4/Screenshot%202025-11-09%20172921.png)
- **Snowy:** blue sky, edit rain particle system to act like snow.
![Screenshot 2025-11-09 173054.png](Images/Week4/Screenshot%202025-11-09%20173054.png)

Each portal prefab references the **matching level** and I angled the level root so, when you look through the glass, the street scene lines up with the user’s view.

- ![Screenshot 2025-11-09 210755.png](Images/Week4/Screenshot%202025-11-09%20210755.png)
---

## 5. “Only show the level through the glass” – URP setup

I used a simple **layer + renderer feature** approach (a stencil‑style mask) so portal contents render **only where the glass is**.

### Layers
- **PortalWindow** — assigned to the **Glass** mesh.
- **PortalContents** — assigned to **everything inside the level** you want visible through the window.

### Material for Portal Window
The glass's material uses a **custom shader** from Unity’s AR Essentials course with **`ZWrite Off`**.
- Turning off depth writes means the glass **doesn’t block** the scene, but we can still use it as a mask surface.

### URP Renderer (custom)
I was inspired by Unity´s AR Essentials course so I created a URP Renderer Data (e.g., **“URP Portal Renderer”**) and added these **Renderer Features**:

1) **Portal Window (Render Objects)** – writes the mask
    - **Event:** *BeforeRenderingOpaques*
    - **Filters:** *Queue = Transparent*, *Layer Mask = PortalWindow*
    - **Overrides:** **Stencil ON**, **Pass = Replace**, **Value = 1**
    - Goal: wherever the glass renders, write the stencil value **1**.  
    - ![Screenshot 2025-11-09 212257.png](Images/Week4/Screenshot%202025-11-09%20212257.png)
2) **Portal Contents (Render Objects)** – draw the level, but **only when stencil == 1**
    - **Event:** *BeforeRenderingOpaques*
    - **Filters:** *Queue = Opaque*, *Layer Mask = PortalContents*
    - **Overrides:** **Stencil ON**, **Compare = Equal**, **Value = 1**, **Pass = Keep**
    - Goal: draw level geometry **only** inside the window shape.
    - ![Screenshot 2025-11-09 212312.png](Images/Week4/Screenshot%202025-11-09%20212312.png)

3) **Portal Transparent Contents (Render Objects)** – for particle system
    - **Event:** *BeforeRenderingTransparents*
    - **Filters:** *Queue = Transparent*, *Layer Mask = PortalContents*
    - **Overrides:** keep stencil compare **Equal** (or use a material override).
    - ![Screenshot 2025-11-09 212335.png](Images/Week4/Screenshot%202025-11-09%20212335.png)

> In short: **pass 1** marks the window area, **pass 2/3** draw only where the mark exists. The rest of the scene ignores it.

Also I made sure the **AR Background Renderer Feature** is enabled in this URP Renderer so the camera feed draws behind 3D content.

- 

## Results

-  Portal spawns cleanly and upright.
- Temperature and weather displayed live before spawning.
- Each portal shows its unique environment only through the window.
- Full end-to-end flow: GPS → API → TMP UI → portal spawn → URP render.

---

## Reflection

This week tied everything together.  
I learned how to blend **real weather data** with **visual immersion**.  
The TMP UI makes feedback instant, and the portal feels connected to the real world.

If I had more time, I’d:
- Improve rotation alignment further with quaternion math only.
- Add edge glow or distortion around the glass for depth.
- Optimize API (maybe).

---
