## Individual Reflection

I enjoyed working on both the AR and VR projects during this course. Having completed the GMD course in the previous semester, I already had a solid foundation in Unity, which gave me a head start when setting up scenes, prefabs, and core interaction logic. However, unlike a typical group setup, I developed both XR applications alone. While this made the workload heavier, it also meant that I was directly involved in all technical, design, and architectural decisions. This gave me a much deeper understanding of XR development and a strong sense of ownership over the final results.

---

### AR Project Reflection

The idea behind the AR project was grounded in a personal, everyday problem. I live in a basement apartment and often have no visual sense of what the weather looks like outside. Checking a traditional weather app feels abstract and unengaging, as it reduces the experience to numbers and icons. I wanted to explore how markerless AR could provide a more spatial and immersive way to perceive weather conditions. By allowing the user to place a portal on a real wall and look into a 3D environment that reflects the current weather, the application uses XR to add value beyond what a 2D interface can offer. For me, seeing the weather visually would make it more motivating to actually go outside.

Technically, the biggest challenge in this project was portal placement and rotation on vertical surfaces. I used a window asset as the portal, consisting of a frame, glass, and a parent object. When attempting to rotate these objects directly through the Inspector and code, I experienced unstable rotations and unintended positional offsets. This was a classic case of rotation dependency and gimbal-related issues caused by mixing Euler rotations with complex object hierarchies. Debugging this was time-consuming and frustrating. After discussing the problem with my supervisor, I resolved it by introducing an empty parent GameObject to act as a clean pivot. I then applied rotation logic only at this level through code, which stabilized placement and made the system predictable. This decision improved both maintainability and spatial correctness.

As I researched markerless AR further, I studied Unity’s Mobile AR Development course, which introduced a portal-based rendering approach. Although the course used a single level and visual scripting, the underlying concept aligned well with my project goals. I adapted the idea to support multiple weather-based environments and implemented it using C# and URP. The course also introduced a custom glass shader using ZWrite Off. I understood its purpose at a conceptual level but did not have sufficient time or shader expertise to recreate it from scratch. Instead, I reused the shader and focused my effort on configuring a custom URP renderer that controlled render order and layer visibility. This allowed portal contents to render only within the glass area, which became one of the core technical features of the AR application.

Another issue was that the sky environments inside the portals were not rendering correctly. Initially, the sky appeared missing when viewed through the portal. Due to time constraints and limited insight into the deeper URP configuration at that stage, I resolved this pragmatically by adjusting the sky geometry closer to the camera. While not an ideal solution, it allowed the environment to render consistently and kept the project moving forward.

---

### VR Project Reflection

The VR project progressed more smoothly, largely because I had prior experience developing traditional games. The primary challenge here was not learning game development itself, but adapting established mechanics to virtual reality. The concept was intentionally simple: a western quickdraw duel where reaction time determines survival. The idea was driven by my interest in fast, high-tension gameplay and my curiosity about how VR amplifies presence and pressure compared to flat-screen games.

One of the main challenges was making the enemy feel believable. I spent considerable time finding suitable Mixamo animations that conveyed intent, timing, and threat. To enhance realism, I implemented logic that made the enemy continuously face the player while remaining upright, reinforcing the sense that the enemy was actively aiming at the user. I also wanted the enemy to draw their weapon from a holster rather than having it visible at all times. This was achieved by separating visual responsibility into a dedicated script that toggles gun objects between holster and hand states, synchronized using animation events. This reinforced the quickdraw theme and improved visual clarity.

Compared to the AR project, I made a more conscious effort to respect the Single Responsibility Principle. Systems such as gun handling, duel logic, enemy visuals, and player interaction were separated into distinct scripts. This made the codebase easier to reason about and modify. Enforcing game rules, such as preventing the player from shooting before the draw signal, was another key challenge. This required careful state management and timing control to ensure fairness and clarity, especially in VR where feedback must be immediate and unambiguous.

---

### Overall Reflection

Working on both projects highlighted how XR development differs from traditional application and game development. Spatial reasoning, rendering order, tracking, and user perception all play a much larger role. While the AR project pushed me to think more about sensors, rendering pipelines, and real-world alignment, the VR project emphasized interaction design, timing, and player feedback. Together, these projects strengthened my understanding of how and when XR adds value, and how technical constraints directly shape user experience.