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

    [Tooltip("How far the viewer can zoom into the diorama.")]
    public float minZoom = 0.5f, maxZoom = 12f;

    readonly Dictionary<StoryData.Location, GameObject> dioramas = new();
    static readonly List<ARRaycastHit> hits = new();
    // Material name fragment -> texture in Resources. The atlases are embedded in the FBX files but Unity leaves them off the materials.
    static readonly (string key, string texture)[] Atlases =
    {
        ("hexagon", "hexagons_medieval"), ("halloween", "halloweenbits_texture"), ("restaurant", "restaurantbits_texture"),
    };
    ARRaycastManager raycaster;
    ARAnchorManager anchors;
    ARPlaneManager planes;
    ARAnchor anchor;
    Transform root;
    StoryData.Location? showing;
    float zoom = 1f;

    public bool Placed { get; private set; }
    public Transform Root => root;

    void Awake()
    {
        raycaster = FindAnyObjectByType<ARRaycastManager>();
        if (raycaster == null)
        {
            var origin = FindAnyObjectByType<XROrigin>();
            if (origin != null) raycaster = origin.gameObject.AddComponent<ARRaycastManager>();
        }
        planes = FindAnyObjectByType<ARPlaneManager>();
        var rig = FindAnyObjectByType<XROrigin>();
        anchors = FindAnyObjectByType<ARAnchorManager>();
        if (anchors == null && rig != null) anchors = rig.gameObject.AddComponent<ARAnchorManager>();

        // A tight depth range stops the small models flickering (z-fighting) and lets the viewer get close.
        var cam = Camera.main;
        if (cam != null) { cam.nearClipPlane = 0.03f; cam.farClipPlane = 15f; }

        root = new GameObject("StoryStage").transform;   // stays active while measuring: inactive renderers report empty bounds
        foreach (StoryData.Location loc in System.Enum.GetValues(typeof(StoryData.Location)))
        {
            var prefab = Resources.Load<GameObject>(loc.ToString());
            if (prefab == null) { Debug.LogWarning("Diorama missing from Resources: " + loc); continue; }
            var go = Instantiate(prefab, root);
            go.name = loc.ToString();
            Retexture(go);
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
        root.SetParent(null, true);   // leave the old anchor before it is replaced
        root.position = position;
        if (cam != null)
        {
            // Face the viewer: the dioramas are built facing -Z in Unity after FBX import.
            Vector3 toCam = Vector3.ProjectOnPlane(cam.transform.position - position, Vector3.up);
            if (toCam.sqrMagnitude > 1e-4f) root.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
        root.localScale = Vector3.one * (zoom = 1f);
        root.gameObject.SetActive(true);
        Placed = true;
        if (showing.HasValue) Show(showing.Value);
        Anchor(new Pose(root.position, root.rotation));
    }

    /// <summary>Pin the stage to the real world so tracking corrections do not make it drift or shake.</summary>
    async void Anchor(Pose pose)
    {
        if (anchors == null || !anchors.enabled) return;
        var old = anchor;
        var result = await anchors.TryAddAnchorAsync(pose);
        if (result.status.IsSuccess() && root != null)
        {
            anchor = result.value;
            root.SetParent(anchor.transform, true);
        }
        if (old != null) anchors.TryRemoveAnchor(old);
    }

    /// <summary>Two-finger pinch and twist: zoom towards where the viewer is looking, so they can get down to ground level.</summary>
    public void Pinch(float ratio, float twistDegrees)
    {
        if (!Placed) return;
        float next = Mathf.Clamp(zoom * ratio, minZoom, maxZoom);
        ratio = next / zoom;
        zoom = next;
        Vector3 pivot = LookPoint();
        root.localScale = Vector3.one * zoom;
        root.position = pivot + (root.position - pivot) * ratio;
        root.RotateAround(pivot, Vector3.up, twistDegrees);
    }

    /// <summary>Where the camera's centre ray meets the table, or the stage centre when it points away.</summary>
    Vector3 LookPoint()
    {
        var cam = Camera.main;
        if (cam == null) return root.position;
        var ray = new Ray(cam.transform.position, cam.transform.forward);
        return new Plane(Vector3.up, root.position).Raycast(ray, out float d) && d < 20f ? ray.GetPoint(d) : root.position;
    }

    /// <summary>The plane overlay is only for finding a surface; hide it afterwards so it cannot flicker over the models.</summary>
    public void ShowPlanes(bool on)
    {
        if (planes == null) return;
        planes.enabled = on;
        planes.SetTrackablesActive(on);
    }

    /// <summary>Give materials that lost their embedded atlas the texture from Resources. One name textures the whole model.</summary>
    public static void Retexture(GameObject go, string textureName = null)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var m in r.materials)
            {
                string tex = textureName;
                if (tex == null)
                    foreach (var a in Atlases)
                        if (m.name.ToLowerInvariant().Contains(a.key)) tex = a.texture;
                var t = tex != null ? Resources.Load<Texture2D>(tex) : null;
                if (t == null) continue;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            }
        }
    }

    /// <summary>The diorama object for a location, so actors can be placed inside it.</summary>
    public Transform Diorama(StoryData.Location loc) => dioramas.TryGetValue(loc, out var go) ? go.transform : null;

    /// <summary>Show the diorama for a location and hide the others.</summary>
    public void Show(StoryData.Location loc)
    {
        showing = loc;
        foreach (var kv in dioramas) kv.Value.SetActive(kv.Key == loc);
    }

    public void Hide() => root.gameObject.SetActive(false);
}
