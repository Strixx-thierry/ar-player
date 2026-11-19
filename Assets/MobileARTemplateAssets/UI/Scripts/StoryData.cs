using UnityEngine.UIElements;

/// <summary>The chapters of the telling. Timings and lines are placeholders until the narration is recorded.</summary>
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
        public string title;
        public Location location;
        public float duration;
        public Line[] lines;
        public Chapter(string title, Location location, float duration, params Line[] lines)
        { this.title = title; this.location = location; this.duration = duration; this.lines = lines; }
    }

    // The legend of Kigwa, sections 1 and 2 (birth in the world above, the fall to Mubari).
    // Lines and timings are drafts until the narration is recorded.
    public static readonly Chapter[] Chapters =
    {
        new Chapter("The World Above", Location.SkyWorld, 35,
            new Line(2, 9, "Long ago, above the clouds, there was another country."),
            new Line(10, 18, "Its lord was Nkuba, master of thunder. Some call him Shyerezo."),
            new Line(20, 30, "He had his huts, his cattle and his milk, as people do below.")),
        new Chapter("The Heart in the Jar", Location.SkyWorld, 45,
            new Line(2, 10, "One day a bull was offered."),
            new Line(11, 20, "Its heart was hidden in a jar carved from umuko wood."),
            new Line(22, 32, "Every morning, fresh milk was poured into the jar."),
            new Line(34, 42, "And every morning, the jar was closed again.")),
        new Chapter("The Child from the Jar", Location.SkyWorld, 50,
            new Line(2, 10, "Nine months went by."),
            new Line(11, 20, "Then, from the jar, came a little boy."),
            new Line(22, 31, "His mother was Gasani. They called him Kigwa."),
            new Line(33, 46, "But Nkuba would not accept the boy as his son.")),
        new Chapter("The Secret", Location.SkyWorld, 45,
            new Line(2, 11, "Kigwa grew up without knowing how he was born."),
            new Line(12, 21, "One day, by chance, he learned the secret."),
            new Line(23, 32, "He was angry, even with his mother."),
            new Line(34, 42, "So he chose to leave the world above.")),
        new Chapter("The Fall", Location.Fall, 40,
            new Line(2, 10, "Kigwa came down from the sky with his companions."),
            new Line(12, 22, "His sister Nyampundu was with him, some tellers say."),
            new Line(24, 36, "They fell a long way, and they landed safe and sound.")),
        new Chapter("Ikinani", Location.Mubari, 45,
            new Line(2, 11, "They came down in the east, in the old land of Mubari, near Lake Ihema."),
            new Line(13, 23, "Under a great rock called Ikinani, they found their first shelter."),
            new Line(25, 38, "That is why his name is Kigwa: the one who fell.")),
        new Chapter("Ibimanuka", Location.Mubari, 50,
            new Line(2, 11, "The people of the land came out to see the strangers."),
            new Line(12, 22, "They called them Ibimanuka: those who came down from the sky."),
            new Line(25, 36, "That is how my elders told it, and their elders before them."),
            new Line(38, 47, "Other tellers say it differently. Listen to them too.")),
    };

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
