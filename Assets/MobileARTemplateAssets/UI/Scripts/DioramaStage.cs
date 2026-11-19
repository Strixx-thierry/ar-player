using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Holds the three story dioramas (SkyWorld, Fall, Mubari from Assets/Resources), places them on a
/// tapped surface and shows the one the current chapter plays on.
/// </summary>
public class DioramaStage : MonoBehaviour
{
    [Tooltip("Width of a diorama on the table, in metres.")]
    public float targetWidth = 0.35f;
    [Tooltip("Editor only: distance in front of the camera when there is no real surface.")]
    public float editorDistance = 0.8f;

    readonly Dictionary<StoryData.Location, GameObject> dioramas = new();
    static readonly List<ARRaycastHit> hits = new();
    ARRaycastManager raycaster;
    Transform root;
    StoryData.Location? showing;

    public bool Placed { get; private set; }

    void Awake()
    {
        raycaster = FindAnyObjectByType<ARRaycastManager>();
        if (raycaster == null)
        {
            var origin = FindAnyObjectByType<XROrigin>();
            if (origin != null) raycaster = origin.gameObject.AddComponent<ARRaycastManager>();
        }
        root = new GameObject("StoryStage").transform;   // stays active while measuring: inactive renderers report empty bounds
        foreach (StoryData.Location loc in System.Enum.GetValues(typeof(StoryData.Location)))
        {
            var prefab = Resources.Load<GameObject>(loc.ToString());
            if (prefab == null) { Debug.LogWarning("Diorama missing from Resources: " + loc); continue; }
            var go = Instantiate(prefab, root);
            go.name = loc.ToString();
            Fit(go.transform);
            go.SetActive(false);
            dioramas[loc] = go;
        }
        root.gameObject.SetActive(false);
    }

    /// <summary>Scale a diorama so its footprint is targetWidth wide, centred on the stage with its base on the surface.</summary>
    void Fit(Transform t)
    {
        var rends = t.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        float width = Mathf.Max(b.size.x, b.size.z);
        if (width <= 0f) return;
        float s = targetWidth / width;
        t.localScale *= s;
        // Recentre: the bounds were measured at the old scale with the stage at the origin.
        Vector3 c = b.center * s;
        float floor = GroundLevel(t, b) * s;
        t.localPosition -= new Vector3(c.x, floor, c.z);
    }

    /// <summary>The land, not the clouds under it, should sit on the table. Uses a LandingSpot/FallPath_End marker when there is one.</summary>
    static float GroundLevel(Transform t, Bounds b)
    {
        foreach (var name in new[] { "LandingSpot", "FallPath_End", "Spot_Jar", "CloudGap" })
        {
            var m = FindDeep(t, name);
            if (m != null) return m.position.y;
        }
        return b.min.y;
    }

    public static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var f = FindDeep(c, name);
            if (f != null) return f;
        }
        return null;
    }

    /// <summary>Place (or move) the stage where the screen was tapped. Returns true when a surface was hit.</summary>
    public bool TryPlace(Vector2 screenPos)
    {
        var cam = Camera.main;
        if (raycaster != null && raycaster.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            var pose = hits[0].pose;
            PlaceAt(pose.position, cam);
            return true;
        }
        if (Application.isEditor && cam != null)
        {
            var p = cam.transform.position + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * editorDistance;
            p.y = cam.transform.position.y - 0.6f;
            PlaceAt(p, cam);
            return true;
        }
        return false;
    }

    /// <summary>Place in front of the camera if the viewer starts without tapping.</summary>
    public void EnsurePlaced()
    {
        if (Placed) return;
        var cam = Camera.main;
        if (cam == null) return;
        var p = cam.transform.position + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * editorDistance;
        p.y = cam.transform.position.y - 0.6f;
        PlaceAt(p, cam);
    }

    void PlaceAt(Vector3 position, Camera cam)
    {
        root.position = position;
        if (cam != null)
        {
            // Face the viewer: the dioramas are built facing -Z in Unity after FBX import.
            Vector3 toCam = Vector3.ProjectOnPlane(cam.transform.position - position, Vector3.up);
            if (toCam.sqrMagnitude > 1e-4f) root.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
        root.gameObject.SetActive(true);
        Placed = true;
        if (showing.HasValue) Show(showing.Value);
    }

    /// <summary>Show the diorama for a location and hide the others.</summary>
    public void Show(StoryData.Location loc)
    {
        showing = loc;
        foreach (var kv in dioramas) kv.Value.SetActive(kv.Key == loc);
    }

    public void Hide() => root.gameObject.SetActive(false);
}
