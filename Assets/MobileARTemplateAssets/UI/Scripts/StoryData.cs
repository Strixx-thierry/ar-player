using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The chapters of the telling, with the narration lines timed to the recording.</summary>
public static class StoryData
{
    public class Line
    {
        public float start, end;
        public string text;
        public Line(float start, float end, string text) { this.start = start; this.end = end; this.text = text; }
    }

    /// <summary>Which diorama a chapter plays on. Names match the FBX files in ArtSource/Environment.</summary>
    public enum Location { SkyWorld, Fall, Mubari }

    public class Chapter
    {
        public string title, clipName;
        public Location location;
        public float duration;
        public AudioClip clip;
        public Line[] lines;
        public Chapter(string title, Location location, string clipName, float duration, params Line[] lines)
        { this.title = title; this.location = location; this.clipName = clipName; this.duration = duration; this.lines = lines; }
    }

    // The legend of Kigwa, sections 1 and 2. Line times are seconds into the chapter's narration clip in Assets/Resources.
    public static readonly Chapter[] Chapters =
    {
        new Chapter("The World Above", Location.SkyWorld, "01_TheWorldAbove", 18.6f,
            new Line(0.2f, 3.9f, "Listen, and I will tell you how the first king came to us."),
            new Line(4.4f, 7.7f, "Long ago, above the clouds, there was another country."),
            new Line(8.1f, 13.5f, "Its lord was Nkuba, master of thunder. Some call him Shyerezo."),
            new Line(13.8f, 18.6f, "He had his huts, his cattle and his milk, just as people do down here.")),
        new Chapter("The Heart in the Jar", Location.SkyWorld, "02_TheHeartInTheJar", 15.1f,
            new Line(0.0f, 2.1f, "One day, a bull was offered."),
            new Line(2.3f, 6.5f, "Its heart was taken and hidden in a jar carved from umuko wood."),
            new Line(6.9f, 10.0f, "Every morning, fresh milk was poured into the jar."),
            new Line(10.0f, 12.7f, "And every morning, the jar was closed again."),
            new Line(13.1f, 15.1f, "No one knew what was growing inside.")),
        new Chapter("The Child from the Jar", Location.SkyWorld, "03_TheChildFromTheJar", 13.0f,
            new Line(0.1f, 1.5f, "Nine months went by."),
            new Line(1.7f, 4.9f, "Then, one morning, a little boy came out of the jar."),
            new Line(5.3f, 9.9f, "His mother was Gasani, and she loved him. They called him Kigwa."),
            new Line(10.1f, 13.0f, "But Nkuba would not accept the boy as his son.")),
        new Chapter("The Secret", Location.SkyWorld, "04_TheSecret", 16.5f,
            new Line(0.2f, 3.0f, "Kigwa grew up without knowing how he was born."),
            new Line(3.6f, 7.0f, "One day, by chance, he learned the secret of the jar."),
            new Line(7.3f, 10.5f, "He was hurt, and he was angry, even with his mother."),
            new Line(10.6f, 12.9f, "So he chose to leave the world above."),
            new Line(13.2f, 16.5f, "He walked to the edge of the clouds, and he did not look back.")),
        new Chapter("The Fall", Location.Fall, "05_TheFall", 15.4f,
            new Line(0.2f, 3.3f, "Kigwa came down from the sky, and he was not alone."),
            new Line(3.7f, 8.4f, "His sister Nyampundu came with him, and a companion too, some tellers say."),
            new Line(8.7f, 13.4f, "They fell a long, long way, past the clouds and past the birds."),
            new Line(13.6f, 15.4f, "And they landed safe and sound.")),
        new Chapter("Ikinani", Location.Mubari, "06_Ikinani", 13.8f,
            new Line(0.1f, 4.7f, "They came down in the east, in the old land of Mubari, near Lake Ihema."),
            new Line(5.0f, 10.4f, "There stood a great rock called Ikinani. Under it, they found their first shelter."),
            new Line(10.6f, 13.8f, "That is why his name is Kigwa: the one who fell.")),
        new Chapter("Ibimanuka", Location.Mubari, "07_Ibimanuka", 16.9f,
            new Line(0.1f, 2.8f, "The people of the land came out to see the strangers."),
            new Line(3.1f, 6.0f, "They had never seen anyone come down from the sky."),
            new Line(6.1f, 10.3f, "So they called them Ibimanuka: those who came down from the sky."),
            new Line(10.5f, 13.9f, "That is how my elders told it, and their elders before them."),
            new Line(14.2f, 16.9f, "Other tellers say it differently. Listen to them too.")),
    };

    /// <summary>Loads each chapter's narration and takes its length as the chapter length. Without a clip the written length stays.</summary>
    public static void LoadAudio()
    {
        foreach (var c in Chapters)
        {
            if (c.clip == null) c.clip = Resources.Load<AudioClip>(c.clipName);
            if (c.clip != null) c.duration = c.clip.length;
        }
    }

    public const string Credits =
        "Umwami's Ascend retells the opening of the Rwandan legend of Kigwa and the Ibimanuka: " +
        "his birth from a jar of milk in the world above, and his fall to Mubari. " +
        "These stories were passed down by voice over many centuries, and they differ from teller to teller. " +
        "Our version follows the written accounts of Alexis Kagame and later scholars, simplified for a short telling. " +
        "If this sparked your curiosity, seek out the real tellings from the elders who keep them.";

    /// <summary>Chapter the Story scene starts on. Set by the title screen.</summary>
    public static int startChapter;

    /// <summary>Fills a container with one row per chapter. onPick gets the chapter index.</summary>
    public static void BuildList(VisualElement list, int current, System.Action<int> onPick)
    {
        if (list == null) return;
        list.Clear();
        for (int i = 0; i < Chapters.Length; i++)
        {
            int index = i;
            var row = new Button(() => onPick(index));
            row.AddToClassList("chap-row");
            if (i == current) row.AddToClassList("chap-row-current");

            var num = new Label((i + 1).ToString());
            num.AddToClassList("chap-num");
            var name = new Label(Chapters[i].title);
            name.AddToClassList("serif");
            name.AddToClassList("chap-name");
            var status = new Label(i == current ? "Playing" : "");
            status.AddToClassList("chap-status");
            num.pickingMode = name.pickingMode = status.pickingMode = PickingMode.Ignore;

            row.Add(num);
            row.Add(name);
            row.Add(status);
            list.Add(row);
        }
    }

    public static void Show(VisualElement e, bool on)
    {
        if (e != null) e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
