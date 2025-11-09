# Week 3 – Spawning the Portal and Fetching Weather Data

---

## Overview

This week, my focus was on making the **portal appear on the surface the user taps** and linking it to **real-time weather data** using an external API.  
The main goals were:
1. Fetching the user’s location.
2. Calling the **Open-Meteo API** to get local weather data.
3. Selecting a suitable **portal prefab** based on the weather condition.
4. Instantiating the chosen portal at the detected surface.

This week was challenging — while the API and spawning logic worked, I struggled with rotation alignment, which caused the portal to spawn at an awkward angle. I’ll fix this next week.

---

## Setting Up Internet and Portal Prefabs

First, I enabled **internet access** in *Project Settings → Player → Other Settings → Internet Access → Require*. Without this, UnityWebRequest can’t connect to external APIs on Android.

Then, I repurposed a **window frame model** from an existing asset as the visual base for my portal. I created four prefabs:
-  **SunnyWeatherPortal**
-  **CloudyWeatherPortal**
- ️ **RainyWeatherPortal**
-  **SnowyWeatherPortal**

Each corresponds to a different weather type determined by the API.

![Screenshot 2025-11-09 151522.png](Images/Week3/Screenshot%202025-11-09%20151522.png)
![Screenshot 2025-11-09 151821.png](Images/Week3/Screenshot%202025-11-09%20151821.png)

---

## Implementation Details

The logic lives inside `TapToPlace.cs`. Lets break down major function of the script.

### 1. Detecting User Location

Unity’s **location services** are accessed through `Input.location`. Since Android requires explicit permission, I used this section to handle user approval and timeout cases:

- Requested `Permission.FineLocation` if not granted.
- Waited up to 20 seconds for user response.
- Started the service with moderate accuracy (`Input.location.Start(500f)`).
- Handled fallback defaults (rainy/sunny) if location failed.

This was all done inside a **coroutine** so the app didn’t freeze while waiting for permission or GPS initialization.

---

### 2. Coroutine Flow – `GetWeatherAndPreparePortal()`

This coroutine runs the core logic in sequence:

1. Get the device’s location.
2. Use the coordinates to call the weather API.
3. Parse the response and determine which portal prefab to use.
4. Set a flag `isReadyToSpawn = true` so the player can tap to place the portal.

The reason behind using coroutine is to keep Unity from freezing while waiting for location or weather data


---

### 3. Fetching and Parsing Weather Data

I used **Open-Meteo’s API** to get daily weather codes. Example request:
```
https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&daily=weather_code&forecast_days=1
```

The JSON response looks like this:
```json
{
  "daily": {
    "weather_code": [2]
  }
}
```

To read this, I created two serializable C# classes:

```csharp
[System.Serializable]
public class DailyWeather { public List<int> weather_code; }
[System.Serializable]
public class WeatherResponse { public DailyWeather daily; }
```

After converting the JSON string using `JsonUtility.FromJson<WeatherResponse>()`, I extracted the first weather code from the list.

---

### 4. Selecting the Correct Portal

The `SelectPortal(int code)` method maps **WMO weather codes** to portal prefabs.

```csharp
if (code >= 0 && code <= 1) portalToSpawn = sunnyWeatherPortal;
else if (code >= 2 && code <= 48) portalToSpawn = cloudyWeatherPortal;
else if (code >= 51 && code <= 67) portalToSpawn = rainyWeatherPortal;
else if (code >= 71 && code <= 77) portalToSpawn = snowyWeatherPortal;
else if (code >= 80 && code <= 99) portalToSpawn = rainyWeatherPortal;
else portalToSpawn = sunnyWeatherPortal;
```

This guarantees the scene always has a matching visual regardless of the weather. I also added a null-check fallback to ensure the app doesn’t crash if a prefab reference is missing.

---

### 5. Spawning the Portal on Tap

Once the system is ready, the user can tap on a detected AR plane to spawn the relevant portal prefab. I ensured this by setting each of the portal prefabs as the default and re-running the app and testing.

The script uses `ARRaycastManager.Raycast()` to find a hit point on the plane and then spawns the prefab at that world position.  
However, rotation proved to be a challenge:

- The default prefab orientation faced downward.
- I rotated it into various angles to try and align it with the detected wall.
- I then used `Quaternion.LookRotation()` to make it face outward from the detected wall.

Even with these adjustments, the portal didn’t always align perfectly. Sometimes it tilted slightly or faced a random direction depending on how the plane was detected.

![Screenshot 2025-11-09 154944.png](Images/Week3/Screenshot%202025-11-09%20154944.png)

---

## Results

- Weather data fetched and parsed successfully.  
- Correct portal prefab chosen based on weather code.  
- Portal spawned at correct surface position.  
- Rotation still inconsistent due to gimbal lock.

Despite the issues, this week’s work connected the app to **live real-world data**, a major step toward creating a dynamic AR experience.

---

## Reflection

This week pushed me to combine **networking, geolocation, and AR interaction** into a single workflow.  
It deepened my understanding of:
- How Unity handles **asynchronous coroutines**.
- The structure of **JSON APIs**.
- How **AR Foundation raycasts** interact with 3D space.
- The limits of **rotation calculations** when working with real-world plane normals.

Next week, I’ll focus on fixing the rotation issue and ensuring the portal aligns perfectly with vertical surfaces before working on its internal visual effects.

---