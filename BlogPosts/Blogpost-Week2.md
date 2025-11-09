# Week 2 – Plane Detection and Touch Input Setup

---

## Overview

This week, I focused on setting up **plane detection** and **touch input** for my AR Visualiser project.  
The goal was to build the base system that can detect **vertical surfaces** and respond when the user taps on them — later, this interaction will trigger the **portal** that leads to different AR levels.

---

## Setting Up Plane Detection

The first step was to configure the AR environment using **AR Foundation**.  
I created an **AR Default Plane** prefab that acts as a visual representation of detected surfaces.

I attached these key components:

- **AR Plane** – detects and tracks surfaces
- **AR Plane Mesh Visualizer** – renders the detected plane mesh
- **Mesh Collider**, **Mesh Filter**, and **Mesh Renderer** – handle collision, geometry, and rendering
- **Line Renderer** – outlines detected planes (used for visual feedback)

Then, I linked this prefab in the **AR Plane Manager** under the **XR Origin (AR Rig)** and set the **Detection Mode** to `Vertical`.  
This ensures the system detects walls and other upright surfaces instead of floors or tables.

<br>

![Screenshot 2025-11-09 124524.png](Images/Week2/Screenshot%202025-11-09%20124524.png)

<br>

![Screenshot 2025-11-09 130453.png](Images/Week2/Screenshot%202025-11-09%20130453.png)

---

## Detecting User Touch Input

After confirming that plane detection worked, I moved on to detecting user taps.  
For this, I added an **AR Raycast Manager** to the XR Origin. It casts a ray from the device’s camera through the touch point and checks for intersections with detected planes.

To debug touch input, I built a **simple UI** using Unity’s Canvas system:

- A **Panel** as the background
- A **Text** element that updates dynamically with raycast results

<br>

![Screenshot 2025-11-09 134208.png](Images/Week2/Screenshot%202025-11-09%20134208.png)

<br>

![Screenshot 2025-11-09 134239.png](Images/Week2/Screenshot%202025-11-09%20134239.png)

---

## Implementation – The `TapToPlace` Script

The input and raycast logic are handled by the `TapToPlace` script.  
It supports both **mobile touch** and **mouse input** in the Editor, making testing easier.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlace : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager arRaycastManager;

    [Header("UI")]
    [SerializeField] private Text debugText;

    private static readonly List<ARRaycastHit> hits = new();

    private void Update()
    {
        if (!arRaycastManager) return;

        Vector2 screenPos;

        // Capture Input (Touch or Mouse)
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
            return;
        }

        if (arRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            if (debugText) debugText.text = $"Hit World Pos: {hitPose.position}";
            Debug.Log($"AR Hit at 3D Position: {hitPose.position}");
        }
        else
        {
            if (debugText) debugText.text = "Touched, but missed AR planes";
        }
    }
}
```

<br>

![Screenshot 2025-11-09 140409.png](Images/Week2/Screenshot%202025-11-09%20140409.png)

---

## Results

Once the script and UI were connected, every tap on a detected plane displayed its **3D world coordinates** in the debug text and console.  
If the tap missed a plane, the UI showed a message confirming the input was registered but not over a plane.

This verified that **plane detection**, **input handling**, and **raycasting** were all working together correctly.

---

## Reflection

The main challenge this week was figuring out how to detect **vertical surfaces**.  
I learned how AR Foundation uses **trackable types** to distinguish between surfaces and how the **AR Raycast Manager** ties user interaction to detected geometry.

---
