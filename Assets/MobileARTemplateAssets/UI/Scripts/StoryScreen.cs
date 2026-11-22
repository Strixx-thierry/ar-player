using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// The pages shown over the camera: Scan, Place, Player and Ending, plus the chapter list
/// and tracking-lost overlays. Chapters run on a timer and show their diorama through DioramaStage.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class StoryScreen : MonoBehaviour
{
    enum Page { Scan, Place, Player, Ending }

    public string titleScene = "Title";
    [Tooltip("Seconds before the player controls hide themselves.")]
    public float hideControlsAfter = 5f;

    VisualElement scan, place, player, ending, chaptersOverlay, lost, playerTop, controls, barFill, chapterList;
    Label kicker, chapterTitle, subtitle, time;
    Button ccButton;
    UiIcon playIcon;

    ARPlaneManager planes;
    DioramaStage stage;
    StoryDirector director;
    AudioSource audioSource;
    float hintUntil, lastPinchDist, lastPinchAngle;
    bool pinching, pinchUsed;
    const string Hint = "Pinch to zoom in close. Hold your phone steady, or step closer, if it shakes.";
    VisualElement uiRoot;
    Page page;
    int chapter;
    float t, pageEnter, lastTouch;
    bool playing, subtitlesOn = true, hadTracking;

    void OnEnable()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        StoryData.LoadAudio();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;

        scan = root.Q("scan");
        place = root.Q("place");
        player = root.Q("player");
        ending = root.Q("ending");
        chaptersOverlay = root.Q("chapters-overlay");
        lost = root.Q("lost");
        playerTop = root.Q("player-top");
        controls = root.Q("controls");
        barFill = root.Q("bar-fill");
        chapterList = root.Q("chapter-list");
        kicker = root.Q<Label>("chapter-kicker");
        chapterTitle = root.Q<Label>("chapter-title");
        subtitle = root.Q<Label>("subtitle");
        time = root.Q<Label>("time");
        ccButton = root.Q<Button>("cc");

        UiIcon.AddTo(root.Q("scan-close"), UiIcon.Kind.Close);
        UiIcon.AddTo(root.Q("place-close"), UiIcon.Kind.Close);
        UiIcon.AddTo(root.Q("chapters-close"), UiIcon.Kind.Close);
        UiIcon.AddTo(root.Q("move"), UiIcon.Kind.Move);
        UiIcon.AddTo(root.Q("prev"), UiIcon.Kind.Previous);
        UiIcon.AddTo(root.Q("next"), UiIcon.Kind.Next);
        UiIcon.AddTo(root.Q("list"), UiIcon.Kind.List);
        playIcon = UiIcon.AddTo(root.Q("play"), UiIcon.Kind.Pause);

        // One mark per chapter boundary on the progress bar.
        var bar = root.Q("bar");
        if (bar != null)
        {
            float total = TotalDuration();
            float at = 0f;
            for (int i = 0; i < StoryData.Chapters.Length - 1; i++)
            {
                at += StoryData.Chapters[i].duration;
                var tick = new VisualElement { pickingMode = PickingMode.Ignore };
                tick.AddToClassList("bar-tick");
                tick.style.left = Length.Percent(100f * at / total);
                bar.Add(tick);
            }
        }

        Click(root, "scan-close", ToTitle);
        Click(root, "place-close", ToTitle);
        Click(root, "start", () => { if (stage != null) stage.EnsurePlaced(); Show(Page.Player); SetPlaying(true); });
        Click(root, "move", () => { SetPlaying(false); Show(Page.Place); });
        Click(root, "cc", ToggleSubtitles);
        Click(root, "prev", () => Jump(t > 3f ? chapter : chapter - 1));
        Click(root, "next", () => Jump(chapter + 1));
        Click(root, "play", () => SetPlaying(!playing));
        Click(root, "list", () => OpenChapters(true));
        Click(root, "chapters-close", () => OpenChapters(false));
        Click(root, "lost-replace", () => { StoryData.Show(lost, false); Show(Page.Place); });
        Click(root, "again", () => { Jump(0); Show(Page.Player); SetPlaying(true); });
        Click(root, "ending-chapters", () => OpenChapters(true));
        Click(root, "ending-title", ToTitle);

        var dim = root.Q("chapters-dim");
        if (dim != null) dim.RegisterCallback<PointerDownEvent>(_ => OpenChapters(false));
        root.RegisterCallback<PointerDownEvent>(_ => lastTouch = Time.unscaledTime, TrickleDown.TrickleDown);

        var credits = root.Q<Label>("credits");
        if (credits != null) credits.text = StoryData.Credits;

        planes = FindAnyObjectByType<ARPlaneManager>();
        uiRoot = root;
        stage = FindAnyObjectByType<DioramaStage>();
        if (stage == null) stage = new GameObject("DioramaStage").AddComponent<DioramaStage>();
        director = FindAnyObjectByType<StoryDirector>();
        if (director == null) director = new GameObject("StoryDirector").AddComponent<StoryDirector>();
        chapter = Mathf.Clamp(StoryData.startChapter, 0, StoryData.Chapters.Length - 1);
        t = 0f;
        RefreshChapter();
        Show(Page.Scan);
    }

    void Update()
    {
        if (scan == null) return;
        float now = Time.unscaledTime;

        if (ARSession.state == ARSessionState.SessionTracking) hadTracking = true;

        // Touches on empty screen do not reach the UI, so read them here to bring the controls back.
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) lastTouch = now;

        switch (page)
        {
            case Page.Scan:
                bool found = planes != null && planes.trackables.count > 0;
                // In the editor the simulated room may never report a surface, so move on after a moment.
                if (found || (Application.isEditor && now - pageEnter > 5f))
                    Show(Page.Place);
                break;

            case Page.Place:
                // A tap on the camera view (not on a button) places or moves the diorama. A pinch never counts as a tap.
                Pinch();
                if (Pointer.current != null && stage != null)
                {
                    if (Pointer.current.press.wasPressedThisFrame) pinchUsed = false;
                    if (pinching) pinchUsed = true;
                    if (Pointer.current.press.wasReleasedThisFrame && !pinchUsed)
                    {
                        Vector2 sp = Pointer.current.position.ReadValue();
                        if (!OverUi(sp)) stage.TryPlace(sp);
                    }
                }
                break;

            case Page.Player:
                bool trackingLost = hadTracking && ARSession.state != ARSessionState.SessionTracking;
                StoryData.Show(lost, trackingLost);
                bool run = playing && !trackingLost;
                if (run) Advance();
                else if (audioSource.isPlaying) audioSource.Pause();
                if (page != Page.Player) break;

                Pinch();
                director.Evaluate(chapter, t, run);
                RefreshProgress();

                // Controls step back after a few seconds; any touch brings them back.
                bool show = !playing || now - lastTouch < hideControlsAfter;
                StoryData.Show(controls, show);
                StoryData.Show(playerTop, show);
                break;
        }
    }

    void Show(Page p)
    {
        page = p;
        pageEnter = lastTouch = Time.unscaledTime;
        StoryData.Show(scan, p == Page.Scan);
        StoryData.Show(place, p == Page.Place);
        StoryData.Show(player, p == Page.Player);
        StoryData.Show(ending, p == Page.Ending);
        StoryData.Show(chaptersOverlay, false);
        if (p != Page.Player) StoryData.Show(lost, false);
        if (p != Page.Player && audioSource != null) audioSource.Pause();
        if (p == Page.Player) hintUntil = Time.unscaledTime + 7f;
        // The surface overlay is only for finding a spot; hide it so it cannot flicker over the models.
        if (stage != null) stage.ShowPlanes(p == Page.Scan || p == Page.Place);
    }

    void Jump(int index)
    {
        chapter = Mathf.Clamp(index, 0, StoryData.Chapters.Length - 1);
        t = 0f;
        if (audioSource != null) audioSource.Stop();
        RefreshChapter();
        RefreshProgress();
    }

    /// <summary>Moves the chapter clock on. The narration is the clock when it is playing; the timer covers a missing clip.</summary>
    void Advance()
    {
        var c = StoryData.Chapters[chapter];
        t += Time.deltaTime;
        if (c.clip != null)
        {
            if (audioSource.isPlaying) { if (Mathf.Abs(audioSource.time - t) > 0.3f) t = audioSource.time; }
            else if (t < c.duration - 0.25f)
            {
                audioSource.clip = c.clip;
                audioSource.time = Mathf.Clamp(t, 0f, c.clip.length - 0.05f);
                audioSource.Play();
            }
        }
        if (t < c.duration) return;

        if (chapter >= StoryData.Chapters.Length - 1)
        {
            SetPlaying(false);
            audioSource.Stop();
            Show(Page.Ending);
        }
        else Jump(chapter + 1);
    }

    /// <summary>Two fingers zoom and turn the story; the mouse wheel does the same in the editor.</summary>
    void Pinch()
    {
        pinching = false;
        if (stage == null) return;
        var ts = Touchscreen.current;
        if (ts != null)
        {
            int n = 0;
            Vector2 a = default, b = default;
            foreach (var tc in ts.touches)
            {
                if (!tc.isInProgress) continue;
                if (n == 0) a = tc.position.ReadValue(); else if (n == 1) b = tc.position.ReadValue();
                n++;
            }
            if (n >= 2)
            {
                float d = (a - b).magnitude;
                float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                if (lastPinchDist > 1f) stage.Pinch(d / lastPinchDist, -Mathf.DeltaAngle(lastPinchAngle, ang));
                lastPinchDist = d;
                lastPinchAngle = ang;
                pinching = true;
                return;
            }
            lastPinchDist = 0f;
        }
        var mouse = Mouse.current;
        if (mouse != null && Application.isEditor)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.1f) stage.Pinch(1f + wheel * 0.0015f, 0f);
        }
    }

    void SetPlaying(bool on)
    {
        playing = on;
        if (playIcon != null) playIcon.Set(on ? UiIcon.Kind.Pause : UiIcon.Kind.Play);
    }

    void ToggleSubtitles()
    {
        subtitlesOn = !subtitlesOn;
        if (ccButton != null) ccButton.EnableInClassList("icon-btn-off", !subtitlesOn);
        RefreshProgress();
    }

    void OpenChapters(bool open)
    {
        if (open)
            StoryData.BuildList(chapterList, page == Page.Player ? chapter : -1, i =>
            {
                Jump(i);
                Show(Page.Player);
                SetPlaying(true);
            });
        StoryData.Show(chaptersOverlay, open);
    }

    void RefreshChapter()
    {
        if (kicker != null) kicker.text = "CHAPTER " + (chapter + 1) + " OF " + StoryData.Chapters.Length;
        if (chapterTitle != null) chapterTitle.text = StoryData.Chapters[chapter].title;
        if (stage != null) stage.Show(StoryData.Chapters[chapter].location);
    }

    /// <summary>True when the screen point is over a button or panel rather than the open camera view.</summary>
    bool OverUi(Vector2 screenPos)
    {
        if (uiRoot?.panel == null) return false;
        Vector2 p = RuntimePanelUtils.ScreenToPanel(uiRoot.panel, new Vector2(screenPos.x, Screen.height - screenPos.y));
        var picked = uiRoot.panel.Pick(p);
        return picked != null && picked != uiRoot;
    }

    void RefreshProgress()
    {
        var c = StoryData.Chapters[chapter];

        float before = 0f;
        for (int i = 0; i < chapter; i++) before += StoryData.Chapters[i].duration;
        if (barFill != null) barFill.style.width = Length.Percent(100f * (before + t) / TotalDuration());
        if (time != null) time.text = Clock(t) + " / " + Clock(c.duration);

        string line = null;
        if (subtitlesOn)
            foreach (var l in c.lines)
                if (t >= l.start && t < l.end) { line = l.text; break; }
        if (line == null && Time.unscaledTime < hintUntil) line = Hint;
        if (subtitle != null)
        {
            subtitle.text = line ?? "";
            StoryData.Show(subtitle, line != null);
        }
    }

    static float TotalDuration()
    {
        float total = 0f;
        foreach (var c in StoryData.Chapters) total += c.duration;
        return total;
    }

    static string Clock(float seconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (s / 60) + ":" + (s % 60).ToString("00");
    }

    void ToTitle()
    {
        SceneManager.LoadScene(titleScene);
    }

    static void Click(VisualElement root, string name, System.Action action)
    {
        var b = root.Q<Button>(name);
        if (b != null) b.clicked += action;
    }
}
