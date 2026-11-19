using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>Title page: Begin, Chapters and About.</summary>
[RequireComponent(typeof(UIDocument))]
public class TitleScreen : MonoBehaviour
{
    public string storyScene = "Story";

    VisualElement chapters, about;

    void OnEnable()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;

        chapters = root.Q("chapters-overlay");
        about = root.Q("about-overlay");

        Click(root, "begin", () => Play(0));
        Click(root, "chapters", () => StoryData.Show(chapters, true));
        Click(root, "about", () => StoryData.Show(about, true));
        Click(root, "about-close", () => StoryData.Show(about, false));
        Click(root, "chapters-close", () => StoryData.Show(chapters, false));
        UiIcon.AddTo(root.Q("chapters-close"), UiIcon.Kind.Close);

        var dim = root.Q("chapters-dim");
        if (dim != null) dim.RegisterCallback<PointerDownEvent>(_ => StoryData.Show(chapters, false));

        var text = root.Q<Label>("about-text");
        if (text != null) text.text = StoryData.Credits;

        StoryData.BuildList(root.Q("chapter-list"), -1, Play);
    }

    void Play(int chapter)
    {
        StoryData.startChapter = chapter;
        SceneManager.LoadScene(storyScene);
    }

    static void Click(VisualElement root, string name, System.Action action)
    {
        var b = root.Q<Button>(name);
        if (b != null) b.clicked += action;
    }
}
