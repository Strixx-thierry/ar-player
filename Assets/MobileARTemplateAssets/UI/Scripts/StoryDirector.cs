using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stages every chapter: who stands where, what they do, and the heart, milk and jar. Everything is a pure
/// function of chapter time, so jumping or scrubbing always shows the right picture. Beats follow the narration lines.
/// </summary>
public class StoryDirector : MonoBehaviour
{
    // Set to 180 if the characters walk backwards on the device.
    const float FacingOffset = 0f;

    class Key
    {
        public float t, dx, dy, dz;
        public string at, anim;
        public bool show = true;
    }

    class Track
    {
        public string actor;
        public StoryData.Location loc;
        public Key[] keys;
    }

    class Actor
    {
        public Transform root;
        public Animation anim;
        public string playing;
        public bool prop;
        public float yaw;
        public Dictionary<string, string> clips = new();
        public Dictionary<string, Vector3> cache = new();
    }

    // Height of each character in diorama units (a hex tile is 2 wide).
    static readonly Dictionary<string, float> Heights = new()
    {
        { "Kigwa", 1.7f }, { "Nkuba", 1.9f }, { "Gasani", 1.6f }, { "BabyKigwa", 0.85f },
        { "Nyampundu", 1.6f }, { "Companion", 1.6f }, { "MubariMan", 1.7f }, { "MubariWoman", 1.6f },
    };

    DioramaStage stage;
    readonly Dictionary<string, Actor> actors = new();
    readonly Dictionary<Transform, Vector3> jarScale = new();
    Track[][] script;

    // Beat times come from the narration lines, so re-recording the audio re-times the staging.
    static float Start(int c, int line) => StoryData.Chapters[c].lines[Mathf.Min(line, StoryData.Chapters[c].lines.Length - 1)].start;
    static float End(int c, int line) => StoryData.Chapters[c].lines[Mathf.Min(line, StoryData.Chapters[c].lines.Length - 1)].end;

    static Key K(float t, string at, string anim, float dx = 0f, float dz = 0f, float dy = 0f) =>
        new Key { t = t, at = at, anim = anim, dx = dx, dz = dz, dy = dy };

    static Key Off(float t) => new Key { t = t, show = false };

    static Track Tr(StoryData.Location loc, string actor, params Key[] keys) =>
        new Track { loc = loc, actor = actor, keys = keys };

    void Build()
    {
        const StoryData.Location Sky = StoryData.Location.SkyWorld, Fall = StoryData.Location.Fall, Mub = StoryData.Location.Mubari;
        script = new Track[7][];

        script[0] = new[]
        {
            Tr(Sky, "Nkuba", K(0, "Spot_Nkuba", "Idle"),
                K(Start(0, 2), "Spot_Nkuba", "Walking_A"), K(Start(0, 2) + 2.5f, "Spot_Nkuba", "Idle", 1.8f)),
        };

        script[1] = new[]
        {
            Tr(Sky, "Nkuba", K(0, "Spot_Nkuba", "Idle", 1.8f)),
            Tr(Sky, "Gasani", Off(0), K(Mathf.Max(0.5f, Start(1, 2) - 3f), "Spot_Gate", "Walking_A"),
                K(Start(1, 2), "Spot_Jar", "Use_Item", 1.2f), K(End(1, 3), "Spot_Jar", "Idle", 1.2f)),
            Tr(Sky, "Heart", Off(0), K(Start(1, 1), "@JarTop", "", 0, 0, 2.2f),
                K(End(1, 1), "@JarTop", "", 0, 0, 0.3f), Off(End(1, 1) + 0.4f)),
            Tr(Sky, "Milk", Off(0), K(Start(1, 2), "@JarTop", "", 0, 0, 0.9f), Off(End(1, 2))),
        };

        script[2] = new[]
        {
            Tr(Sky, "Nkuba", K(0, "Spot_Nkuba", "Idle", 1.8f),
                K(Start(2, 3), "Spot_Nkuba", "Walking_A", 1.8f), K(Start(2, 3) + 3f, "Spot_Gate", "Idle")),
            Tr(Sky, "Gasani", K(0, "Spot_Jar", "Idle", 1.2f), K(Start(2, 2), "Spot_Jar", "Cheer", 1.2f)),
            Tr(Sky, "BabyKigwa", Off(0), K(Start(2, 1), "@JarTop", "Jump_Idle", 0, 0, 0.3f),
                K(Start(2, 1) + 1.2f, "Spot_Jar", "Cheer", -0.9f), K(Start(2, 2) + 2f, "Spot_Jar", "Idle", -0.9f)),
        };

        script[3] = new[]
        {
            Tr(Sky, "Gasani", K(0, "Spot_Jar", "Idle", 1.2f)),
            Tr(Sky, "Kigwa", K(0, "Spot_Jar", "Idle", -0.9f), K(Start(3, 1), "Spot_Jar", "Interact", -0.9f),
                K(Start(3, 2), "Spot_Jar", "Hit_A", -0.9f), K(Start(3, 3), "Spot_Jar", "Walking_A", -0.9f),
                K(Start(3, 4), "Spot_Gate", "Walking_A"), K(Mathf.Max(Start(3, 4) + 3f, End(3, 4) - 0.5f), "CloudGap", "Idle")),
        };

        // Falling: three figures drift down the marker path, then cheer on landing.
        var fall = new[] { "FallPath_Start", "FallPath_01", "FallPath_02", "FallPath_03", "FallPath_04", "FallPath_End" };
        script[4] = new[]
        {
            FallTrack(Fall, "Kigwa", fall, 0f), FallTrack(Fall, "Nyampundu", fall, 1.2f), FallTrack(Fall, "Companion", fall, -1.2f),
        };

        script[5] = new[]
        {
            Tr(Mub, "Kigwa", K(0, "LandingSpot", "Cheer"), K(Start(5, 1), "LandingSpot", "Walking_A"),
                K(Start(5, 1) + 4f, "IkinaniShelter", "Sit_Floor_Down"), K(Start(5, 1) + 5.5f, "IkinaniShelter", "Sit_Floor_Idle")),
            Tr(Mub, "Nyampundu", K(0, "LandingSpot", "Cheer", 1.2f), K(Start(5, 1), "LandingSpot", "Walking_A", 1.2f),
                K(Start(5, 1) + 4f, "IkinaniShelter", "Sit_Floor_Down", 1.2f), K(Start(5, 1) + 5.5f, "IkinaniShelter", "Sit_Floor_Idle", 1.2f)),
            Tr(Mub, "Companion", K(0, "LandingSpot", "Cheer", -1.2f), K(Start(5, 1), "LandingSpot", "Walking_A", -1.2f),
                K(Start(5, 1) + 4f, "IkinaniShelter", "Sit_Floor_Down", -1.2f), K(Start(5, 1) + 5.5f, "IkinaniShelter", "Sit_Floor_Idle", -1.2f)),
        };

        script[6] = new[]
        {
            Tr(Mub, "Kigwa", K(0, "IkinaniShelter", "Sit_Floor_Idle"), K(Start(6, 2), "IkinaniShelter", "Cheer")),
            Tr(Mub, "Nyampundu", K(0, "IkinaniShelter", "Sit_Floor_Idle", 1.2f), K(Start(6, 2), "IkinaniShelter", "Cheer", 1.2f)),
            Tr(Mub, "Companion", K(0, "IkinaniShelter", "Sit_Floor_Idle", -1.2f), K(Start(6, 2), "IkinaniShelter", "Cheer", -1.2f)),
            Tr(Mub, "MubariMan", Off(0), K(Start(6, 0), "Spot_Homestead", "Walking_A"),
                K(Start(6, 1), "IkinaniShelter", "Idle", 3f, 1f), K(Start(6, 2), "IkinaniShelter", "Cheer", 3f, 1f)),
            Tr(Mub, "MubariWoman", Off(0), K(Start(6, 0), "Spot_Homestead", "Walking_A", 1f),
                K(Start(6, 1), "IkinaniShelter", "Idle", 3f, -1.2f), K(Start(6, 2), "IkinaniShelter", "Cheer", 3f, -1.2f)),
        };
    }

    static Track FallTrack(StoryData.Location loc, string who, string[] path, float dx)
    {
        float a = Start(4, 0), b = Mathf.Max(a + 3f, Start(4, 3) - 0.5f);
        var keys = new List<Key>();
        for (int i = 0; i < path.Length; i++)
            keys.Add(K(Mathf.Lerp(a, b, i / (path.Length - 1f)), path[i], "Jump_Idle", dx));
        keys.Add(K(b + 0.5f, "FallPath_End", "Cheer", dx));
        return Tr(loc, who, keys.ToArray());
    }

    /// <summary>Put everything where it belongs for chapter c at time t.</summary>
    public void Evaluate(int c, float t, bool playing)
    {
        if (stage == null) stage = FindAnyObjectByType<DioramaStage>();
        if (stage == null || !stage.Placed) return;
        if (script == null) Build();
        var tracks = script[Mathf.Clamp(c, 0, script.Length - 1)];
        var loc = StoryData.Chapters[c].location;
        var dio = stage.Diorama(loc);
        if (dio == null) return;

        // Hide everything in this diorama first; tracks switch their own actors back on.
        foreach (var kv in actors)
            if (kv.Value != null && kv.Key.StartsWith(loc + "/")) kv.Value.root.gameObject.SetActive(false);

        foreach (var track in tracks)
        {
            var a = GetActor(dio, track.loc, track.actor);
            if (a != null) Apply(dio, a, track, t, playing);
        }

        // The bull is gone once its heart is taken (chapter 2 onward).
        var bull = DioramaStage.FindDeep(dio, "Sky_Bull");
        if (bull != null) bull.gameObject.SetActive(!(c > 1 || (c == 1 && t >= Start(1, 1))));

        // The jar stirs while something grows inside.
        var jar = DioramaStage.FindDeep(dio, "Umuko_Jar");
        if (jar != null)
        {
            bool stir = (c == 1 && t >= Start(1, 4) && t < End(1, 4)) || (c == 2 && t >= Start(2, 0) && t < End(2, 0));
            if (!jarScale.ContainsKey(jar)) jarScale[jar] = jar.localScale;
            jar.localScale = jarScale[jar] * (stir ? 1f + 0.05f * Mathf.Sin(t * 8f) : 1f);
        }
    }

    Actor GetActor(Transform dio, StoryData.Location loc, string name)
    {
        string id = loc + "/" + name;
        if (actors.TryGetValue(id, out var a)) return a;

        a = new Actor();
        if (name == "Heart" || name == "Milk")
        {
            a.prop = true;
            a.root = name == "Heart" ? MakeHeart(dio) : MakeMilk(dio);
        }
        else
        {
            var prefab = Resources.Load<GameObject>(name);
            if (prefab == null) { Debug.LogWarning("Character missing from Resources: " + name); actors[id] = null; return null; }
            var go = Instantiate(prefab, dio);
            go.name = name;
            DioramaStage.Retexture(go, name + "_texture");
            FitHeight(go.transform, dio, Heights.TryGetValue(name, out var h) ? h : 1.7f);
            // Wrap in a pivot so scale and facing stay separate from the model's own transform.
            var pivot = new GameObject(name + "_Actor").transform;
            pivot.SetParent(dio, false);
            go.transform.SetParent(pivot, true);
            a.root = pivot;
            a.anim = go.GetComponent<Animation>() ?? go.GetComponentInChildren<Animation>();
            if (a.anim != null)
            {
                a.anim.playAutomatically = false;
                foreach (AnimationState s in a.anim)
                {
                    s.wrapMode = WrapMode.Loop;
                    a.clips[s.name.Substring(s.name.LastIndexOf('|') + 1)] = s.name;
                }
            }
            else Debug.LogWarning(name + " has no Animation. Check that the FBX uses the Legacy animation type.");
        }
        actors[id] = a;
        return a;
    }

    /// <summary>Scale a model so it stands the given height (diorama units) next to the buildings.</summary>
    static void FitHeight(Transform model, Transform dio, float height)
    {
        var rends = model.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        if (b.size.y <= 0f) return;
        model.localScale *= height * dio.lossyScale.y / b.size.y;
    }

    Material PropMaterial(Transform dio, Color color)
    {
        var rend = dio.GetComponentInChildren<Renderer>(true);
        var shader = rend != null ? rend.sharedMaterial.shader : Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(shader) { color = color };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 0.6f); }
        return m;
    }

    static Transform Part(PrimitiveType type, Transform parent, Material m, Vector3 pos, Vector3 scale, float roll = 0f)
    {
        var p = GameObject.CreatePrimitive(type).transform;
        Destroy(p.GetComponent<Collider>());
        p.SetParent(parent, false);
        p.localPosition = pos;
        p.localScale = scale;
        p.localRotation = Quaternion.Euler(0, 0, roll);
        p.GetComponent<Renderer>().sharedMaterial = m;
        return p;
    }

    // A heart from two spheres and a turned cube, red and glowing.
    Transform MakeHeart(Transform dio)
    {
        var m = PropMaterial(dio, new Color(0.8f, 0.08f, 0.1f));
        var root = new GameObject("Heart").transform;
        root.SetParent(dio, false);
        var shape = new GameObject("Shape").transform;
        shape.SetParent(root, false);
        shape.localScale = Vector3.one * 0.55f;
        Part(PrimitiveType.Cube, shape, m, new Vector3(0, -0.05f, 0), new Vector3(0.5f, 0.5f, 0.5f), 45f);
        Part(PrimitiveType.Sphere, shape, m, new Vector3(-0.177f, 0.3f, 0), Vector3.one * 0.5f);
        Part(PrimitiveType.Sphere, shape, m, new Vector3(0.177f, 0.3f, 0), Vector3.one * 0.5f);
        return root;
    }

    // A thin white stream falling into the jar.
    Transform MakeMilk(Transform dio)
    {
        var m = PropMaterial(dio, new Color(0.97f, 0.97f, 0.93f));
        var root = new GameObject("Milk").transform;
        root.SetParent(dio, false);
        Part(PrimitiveType.Cylinder, root, m, Vector3.zero, new Vector3(0.12f, 0.45f, 0.12f));
        return root;
    }

    void Apply(Transform dio, Actor a, Track track, float t, bool playing)
    {
        var keys = track.keys;
        if (t < keys[0].t || !keys[0].show && (keys.Length == 1 || t < keys[1].t)) return;

        int i = 0;
        while (i + 1 < keys.Length && t >= keys[i + 1].t) i++;
        var k = keys[i];
        if (!k.show) return;

        Vector3 from = Pos(dio, a, k);
        Vector3 pos = from;
        string anim = k.anim;
        Vector3 heading = Vector3.zero;
        if (i + 1 < keys.Length && keys[i + 1].show)
        {
            var n = keys[i + 1];
            float u = Mathf.InverseLerp(k.t, n.t, t);
            Vector3 to = Pos(dio, a, n);
            pos = Vector3.Lerp(from, to, u);
            heading = to - from;
        }

        a.root.gameObject.SetActive(true);
        a.root.localPosition = pos;

        if (a.prop)
        {
            // Props pulse instead of animating.
            a.root.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(t * 7f));
            return;
        }

        heading.y = 0f;
        float targetYaw;
        if (heading.sqrMagnitude > 1e-3f)
            targetYaw = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
        else
        {
            // At rest, look at the viewer.
            var cam = Camera.main;
            Vector3 d = cam != null ? dio.InverseTransformPoint(cam.transform.position) - pos : Vector3.back;
            d.y = 0f;
            targetYaw = d.sqrMagnitude > 1e-4f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : a.yaw;
        }
        a.yaw = Mathf.MoveTowardsAngle(a.yaw, targetYaw, 360f * Time.deltaTime);
        a.root.localRotation = Quaternion.Euler(0f, a.yaw + FacingOffset, 0f);

        if (a.anim != null)
        {
            a.anim.enabled = playing || a.playing == null;
            if (anim != a.playing && a.clips.TryGetValue(anim, out var clip))
            {
                a.anim.CrossFade(clip, 0.25f);
                a.playing = anim;
            }
        }
    }

    /// <summary>Position of a key in diorama space. "@JarTop" is the mouth of the umuko jar.</summary>
    Vector3 Pos(Transform dio, Actor a, Key k)
    {
        string id = k.at + "|" + k.dx + "|" + k.dy + "|" + k.dz;
        if (a.cache.TryGetValue(id, out var cached)) return cached;

        Vector3 p = Vector3.zero;
        if (k.at == "@JarTop")
        {
            var jar = DioramaStage.FindDeep(dio, "Umuko_Jar");
            if (jar != null)
            {
                var rends = jar.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    var b = rends[0].bounds;
                    foreach (var r in rends) b.Encapsulate(r.bounds);
                    p = dio.InverseTransformPoint(new Vector3(b.center.x, b.max.y, b.center.z));
                }
            }
        }
        else
        {
            var m = DioramaStage.FindDeep(dio, k.at);
            if (m != null) p = dio.InverseTransformPoint(m.position);
            else Debug.LogWarning("Marker missing: " + k.at);
        }
        p += new Vector3(k.dx, k.dy, k.dz);
        a.cache[id] = p;
        return p;
    }
}
