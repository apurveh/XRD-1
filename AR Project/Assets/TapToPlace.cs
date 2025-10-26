using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Make sure to keep this if you use 'Text'
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking; // For calling the weather API
using System;
#if UNITY_ANDROID
using UnityEngine.Android; // For asking permission
#endif

// --- C# Classes to read the Open-Meteo JSON response ---
// We only need the 'weather_code' which is inside 'daily'
[System.Serializable]
public class DailyWeather
{
    // Make sure the variable name matches the JSON key
    public List<int> weather_code;
}

[System.Serializable]
public class WeatherResponse
{
    // Make sure the variable name matches the JSON key
    public DailyWeather daily;
}

public class TapToPlace : MonoBehaviour
{
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private Text debugText;
    [SerializeField] private ARPlaneManager arPlaneManager;

    // --- 1. SERIALIZE YOUR 4 PORTALS ---
    [Header("Weather Portals")]
    [SerializeField] private GameObject sunnyWeatherPortal;
    [SerializeField] private GameObject cloudyWeatherPortal;
    [SerializeField] private GameObject rainyWeatherPortal;
    [SerializeField] private GameObject snowyWeatherPortal;

    private static readonly List<ARRaycastHit> hits = new();

    // --- State Flags ---
    private GameObject portalToSpawn; // This will hold our chosen portal
    private bool isReadyToSpawn = false; // Prevents spawning before weather is fetched
    private bool portalHasSpawned = false;

    void Start()
    {
        // Start the whole process
        StartCoroutine(GetWeatherAndPreparePortal());
    }

    /// <summary>
    /// This is the main coroutine that runs in order:
    /// 1. Get Location
    /// 2. Get Weather
    /// 3. Select Portal
    /// </summary>
    IEnumerator GetWeatherAndPreparePortal()
    {
        // --- 2. GET USER LOCATION ---
        // --- Handle Android Permissions ---
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            SetDebugText("Requesting location permission...");
            Permission.RequestUserPermission(Permission.FineLocation);

            float timer = 0;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && timer < 20f)
            {
                // Wait for user to respond
                yield return new WaitForSeconds(1);
                timer++;
            }
        }
#endif

        // --- Check if service is enabled ---
        if (!Input.location.isEnabledByUser)
        {
            SetDebugText("Location permission not enabled. Defaulting to rainy.");
            portalToSpawn = snowyWeatherPortal; // Set a default
            isReadyToSpawn = true;
            yield break; // Stop the coroutine
        }

        // --- Start Service ---
        Input.location.Start(500f); // 500m accuracy is fine
        SetDebugText("Initializing location...");

        // --- Wait for Initialization ---
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // --- Handle Failures ---
        if (maxWait <= 0)
        {
            SetDebugText("Location service timed out. Defaulting to sunny.");
            portalToSpawn = sunnyWeatherPortal;
            isReadyToSpawn = true;
            Input.location.Stop();
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            SetDebugText("Unable to find location. Defaulting to rainy.");
            portalToSpawn = sunnyWeatherPortal;
            isReadyToSpawn = true;
            Input.location.Stop();
            yield break;
        }

        // --- 3. LOCATION SUCCESS -> GET WEATHER ---
        LocationInfo data = Input.location.lastData;
        Input.location.Stop(); // Stop service to save battery
        SetDebugText("Location found. Fetching weather...");

        // Build the API URL string
        string url = $"https://api.open-meteo.com/v1/forecast?latitude={data.latitude}&longitude={data.longitude}&daily=weather_code&forecast_days=1";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Success!
                string jsonResponse = www.downloadHandler.text;
                WeatherResponse weather = JsonUtility.FromJson<WeatherResponse>(jsonResponse);

                // Get the first weather code from the list
                int weatherCode = weather.daily.weather_code[0];
                SetDebugText($"Weather code: {weatherCode}. Ready to spawn.");

                // --- 4. CHOOSE THE PORTAL ---
                SelectPortal(weatherCode);
            }
            else
            {
                // API call failed
                SetDebugText($"API Error: {www.error}. Defaulting to rainy.");
                portalToSpawn = rainyWeatherPortal; // Default on failure
            }
        }

        // --- 5. READY TO SPAWN ---
        isReadyToSpawn = true;
        if (debugText.text.Contains("Ready to spawn"))
        {
            SetDebugText("Tap a surface to spawn the weather portal!");
        }
    }

    /// <summary>
    /// Chooses which portal prefab to use based on the WMO weather code
    /// </summary>
    private void SelectPortal(int code)
    {
        // This logic is based on the WMO Weather code documentation
        // https://open-meteo.com/en/docs
        if (code >= 0 && code <= 1) // 0, 1
        {
            portalToSpawn = sunnyWeatherPortal;
        }
        else if (code >= 2 && code <= 48) // 2, 3, 45, 48
        {
            portalToSpawn = cloudyWeatherPortal;
        }
        else if (code >= 51 && code <= 67) // 51-67 (Rain/Drizzle)
        {
            portalToSpawn = rainyWeatherPortal;
        }
        else if (code >= 71 && code <= 77) // 71-77 (Snow)
        {
            portalToSpawn = snowyWeatherPortal;
        }
        else if (code >= 80 && code <= 99) // 80-99 (Showers/Thunderstorm)
        {
            portalToSpawn = rainyWeatherPortal;
        }
        else
        {
            // Default case if something weird happens
            portalToSpawn = sunnyWeatherPortal;
        }

        // Double-check none are null
        if (portalToSpawn == null)
        {
            SetDebugText("Portal prefab is missing! Defaulting to rainy.");
            portalToSpawn = sunnyWeatherPortal;
        }
    }

    private void Update()
    {
        // Only allow spawning if the portal is chosen AND it hasn't been spawned yet
        if (!isReadyToSpawn || portalHasSpawned || !arRaycastManager)
            return;

        Vector2 screenPos;

        // Touch first
        if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        // Mouse fallback (Editor)
        else if (Mouse.current?.leftButton.wasPressedThisFrame == true)
        {
            screenPos = Mouse.current.position.ReadValue();
        }
        else
        {
            return; // nothing pressed this frame
        }

        bool hit = arRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon);

        if (hit)
        {
            Pose hitPose = hits[0].pose;

            // --- 1. Log the comparison ---
            Debug.Log($"Tap Event: ScreenPos (Vector2) {screenPos} created a hit at WorldPos (Vector3) {hitPose.position}");

            // --- 2. Calculate upright rotation for the portal ---
            // The portal prefab faces downward by default, so we need to:
            // 1. Rotate it 90° around X to make it stand upright
            // 2. Rotate it to face away from the wall

            // Get the wall's normal direction
            Vector3 wallNormal = hitPose.rotation * Vector3.up;

            // Project to horizontal plane to remove any tilt
            Vector3 horizontalNormal = new Vector3(wallNormal.x, 0, wallNormal.z).normalized;

            // Create rotation facing away from wall
            Quaternion faceWall = Quaternion.LookRotation(horizontalNormal, Vector3.up);

            // Add 90° X rotation to stand the portal upright (since it faces down by default)
            Quaternion uprightRotation = faceWall * Quaternion.Euler(90, 0, 0);

            // --- 3. Spawn the chosen portal with corrected rotation ---
            // Store reference to the new portal
            GameObject spawnedPortal = Instantiate(portalToSpawn, hitPose.position, uprightRotation);
            portalHasSpawned = true;
            DisablePlaneDetection();
            SetDebugText($"Portal spawned!");

            // --- 3. Log the REVERSE comparison (World-to-Screen) ---
            // Ask the camera: "Where on the screen is the 3D point I just spawned?"
            // Use Camera.main, which ARFoundation automatically keeps in sync with the device camera
            if (Camera.main != null)
            {
                Vector3 portalScreenPoint = Camera.main.WorldToScreenPoint(spawnedPortal.transform.position);
                Debug.Log($"PROOF: The new portal's 3D position {hitPose.position} appears on screen at {portalScreenPoint.x}, {portalScreenPoint.y} pixels.");
            }
            else
            {
                Debug.LogWarning("Could not find 'MainCamera' to perform reverse (WorldToScreenPoint) check.");
            }
        }
        else
        {
            SetDebugText("No AR plane hit. Try again.");
        }
    }

    // Helper function to turn off all plane detection and visuals
    private void DisablePlaneDetection()
    {
        if (arPlaneManager)
        {
            arPlaneManager.enabled = false;
            foreach (var plane in arPlaneManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
    }

    // Helper function to safely update the debug text
    private void SetDebugText(string message)
    {
        if (debugText)
        {
            debugText.text = message;
        }
        Debug.Log(message); // Also log to console
    }
}