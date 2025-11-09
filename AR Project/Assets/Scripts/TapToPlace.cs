using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking;
using System;
using TMPro;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[System.Serializable]
public class HourlyWeather
{
    public List<string> time;
    public List<float> temperature_2m;
    public List<int> weather_code;
}

[System.Serializable]
public class WeatherResponse
{
    public HourlyWeather hourly;
}

public class TapToPlace : MonoBehaviour
{
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

    // --- State Flags ---
    private GameObject portalToSpawn;
    private bool isReadyToSpawn = false;
    private bool portalHasSpawned = false;

    void Start()
    {
        // Set initial text
        if (temperatureText)
        {
            temperatureText.text = "Fetching weather...";
        }

        // Start the whole process
        StartCoroutine(GetWeatherAndPreparePortal());
    }

    IEnumerator GetWeatherAndPreparePortal()
    {
        // --- GET USER LOCATION ---
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Debug.Log("Requesting location permission...");
            Permission.RequestUserPermission(Permission.FineLocation);

            float timer = 0;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && timer < 20f)
            {
                yield return new WaitForSeconds(1);
                timer++;
            }
        }
#endif

        // --- Check if service is enabled ---
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("Location permission not enabled. Defaulting to rainy.");
            if (temperatureText) temperatureText.text = "Location Off";
            portalToSpawn = rainyWeatherPortal; // Set a default
            isReadyToSpawn = true;
            yield break; // Stop the coroutine
        }

        // --- Start Service ---
        Input.location.Start(500f);
        Debug.Log("Initializing location...");

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
            Debug.LogWarning("Location service timed out. Defaulting to sunny.");
            if (temperatureText) temperatureText.text = "Location Timeout";
            portalToSpawn = sunnyWeatherPortal;
            isReadyToSpawn = true;
            Input.location.Stop();
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to find location. Defaulting to sunny.");
            if (temperatureText) temperatureText.text = "Location Failed";
            portalToSpawn = sunnyWeatherPortal;
            isReadyToSpawn = true;
            Input.location.Stop();
            yield break;
        }

        // --- LOCATION SUCCESS -> GET WEATHER ---
        LocationInfo data = Input.location.lastData;
        Input.location.Stop(); // Stop service to save battery
        Debug.Log("Location found. Fetching weather...");

        string url = $"https://api.open-meteo.com/v1/forecast?latitude={data.latitude}&longitude={data.longitude}&hourly=temperature_2m,weather_code&forecast_days=1";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Success!
                string jsonResponse = www.downloadHandler.text;
                WeatherResponse weather = JsonUtility.FromJson<WeatherResponse>(jsonResponse);

                // --- 4. GET CURRENT HOUR'S DATA ---
                int currentHourIndex = DateTime.Now.Hour;

                // Check if the hourly data is valid
                if (weather.hourly != null &&
                    weather.hourly.weather_code != null &&
                    weather.hourly.weather_code.Count > currentHourIndex &&
                    weather.hourly.temperature_2m != null &&
                    weather.hourly.temperature_2m.Count > currentHourIndex)
                {
                    // Get the CURRENT weather code from the hourly list
                    int weatherCode = weather.hourly.weather_code[currentHourIndex];
                    Debug.Log($"Current hourly weather code: {weatherCode}. Ready to spawn.");

                    // --- CHOOSE THE PORTAL ---
                    SelectPortal(weatherCode);

                    // Get and display the CURRENT temperature
                    float currentTemp = weather.hourly.temperature_2m[currentHourIndex];
                    DisplayCurrentTemperature(currentTemp);
                }
                else
                {
                    // Handle data error
                    Debug.LogError("Could not parse hourly weather from API response. Defaulting to sunny.");
                    if (temperatureText) temperatureText.text = "Weather Error";
                    portalToSpawn = sunnyWeatherPortal;
                }
            }
            else
            {
                // API call failed
                Debug.LogError($"API Error: {www.error}. Defaulting to rainy.");
                if (temperatureText) temperatureText.text = "API Error";
                portalToSpawn = rainyWeatherPortal; // Default on failure
            }
        }

        // --- READY TO SPAWN ---
        isReadyToSpawn = true;
    }

    /// <summary>
    /// Displays the given temperature on the UI text.
    /// (This is now simplified as we pass the float directly)
    /// </summary>
    private void DisplayCurrentTemperature(float currentTemp)
    {
        if (temperatureText == null) return; // No text object to update

        // Format the string to one decimal place with the degree symbol
        temperatureText.text = $"{currentTemp:F1}°C";
        Debug.Log($"Current temperature: {currentTemp:F1}°C");
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
            portalToSpawn = sunnyWeatherPortal; // Default
        }

        if (portalToSpawn == null)
        {
            Debug.LogError("Portal prefab is missing! Defaulting to sunny.");
            portalToSpawn = sunnyWeatherPortal;
        }
    }

    private void Update()
    {
        // Only allow spawning if the portal is chosen AND it hasn't been spawned yet
        if (!isReadyToSpawn || portalHasSpawned || !arRaycastManager)
            return;

        Vector2 screenPos;

        if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
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

            // --- Calculate upright rotation for the portal ---
            Vector3 wallNormal = hitPose.rotation * Vector3.up;
            Vector3 horizontalNormal = new Vector3(wallNormal.x, 0, wallNormal.z).normalized;
            Quaternion faceWall = Quaternion.LookRotation(horizontalNormal, Vector3.up);
            Quaternion uprightRotation = faceWall * Quaternion.Euler(90, 0, 0);

            // --- Spawn the chosen portal ---
            Instantiate(portalToSpawn, hitPose.position, uprightRotation);
            portalHasSpawned = true;
            DisablePlaneDetection();
            Debug.Log("Portal spawned!");

            // Hide the temperature text after spawning
            if (temperatureText)
            {
                temperatureText.gameObject.SetActive(false);
            }
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
}