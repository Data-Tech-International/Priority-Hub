namespace PriorityHub.Ui.Services;

/// <summary>Represents a single emoji with its name and searchable keywords.</summary>
public sealed record EmojiEntry(string Emoji, string Name, string[] Keywords);

/// <summary>A named group of related emoji entries.</summary>
public sealed record EmojiCategory(string Name, EmojiEntry[] Emojis);

/// <summary>
/// Provides a curated set of emoji organised by category for use in the EmojiPicker component.
/// </summary>
public static class EmojiData
{
    public static readonly IReadOnlyList<EmojiCategory> Categories =
    [
        new("Connectors", [
            new("🔷", "Blue diamond", ["blue", "diamond", "azure", "devops", "ado"]),
            new("🐙", "Octopus", ["octopus", "github", "git"]),
            new("📋", "Clipboard", ["clipboard", "jira", "tasks", "list"]),
            new("📌", "Pushpin", ["pushpin", "pin", "trello", "board"]),
            new("✅", "Check mark", ["check", "done", "tasks", "microsoft", "todo"]),
            new("📧", "Email", ["email", "mail", "outlook", "letter"]),
            new("🔗", "Link", ["link", "chain", "connect"]),
            new("🛠️", "Tools", ["tools", "build", "settings", "configure"]),
        ]),
        new("Smileys & People", [
            new("😀", "Grinning face", ["happy", "smile", "grin"]),
            new("😊", "Smiling face", ["smile", "happy", "blush"]),
            new("😍", "Heart eyes", ["love", "heart", "eyes", "excited"]),
            new("🤩", "Star struck", ["star", "excited", "amazing"]),
            new("😎", "Cool face", ["cool", "sunglasses", "awesome"]),
            new("🥳", "Partying face", ["party", "celebrate", "fun"]),
            new("🤔", "Thinking face", ["think", "ponder", "question"]),
            new("😤", "Steam face", ["frustrated", "angry", "steam"]),
            new("🤯", "Exploding head", ["mind blown", "explode", "shocked"]),
            new("👍", "Thumbs up", ["like", "approve", "good", "yes"]),
            new("👎", "Thumbs down", ["dislike", "disapprove", "bad", "no"]),
            new("👏", "Clapping hands", ["clap", "applause", "bravo"]),
            new("🙌", "Raised hands", ["celebrate", "hooray", "hands"]),
            new("💪", "Flexed bicep", ["strong", "muscle", "flex", "power"]),
            new("🫡", "Saluting face", ["salute", "respect", "acknowledge"]),
        ]),
        new("Animals & Nature", [
            new("🦊", "Fox", ["fox", "cunning", "orange"]),
            new("🐝", "Bee", ["bee", "busy", "honey", "yellow"]),
            new("🦅", "Eagle", ["eagle", "bird", "fly", "freedom"]),
            new("🦁", "Lion", ["lion", "strong", "king", "brave"]),
            new("🐉", "Dragon", ["dragon", "fire", "fantasy"]),
            new("🦋", "Butterfly", ["butterfly", "transform", "change", "beauty"]),
            new("🦄", "Unicorn", ["unicorn", "magic", "special", "rare"]),
            new("🐺", "Wolf", ["wolf", "pack", "howl"]),
            new("🌟", "Glowing star", ["star", "glow", "shine", "bright"]),
            new("⭐", "Star", ["star", "favorite", "important"]),
            new("🌈", "Rainbow", ["rainbow", "colors", "diversity", "hope"]),
            new("🌊", "Wave", ["wave", "ocean", "water", "flow"]),
            new("🔥", "Fire", ["fire", "hot", "trending", "lit"]),
            new("❄️", "Snowflake", ["snow", "cold", "ice", "winter"]),
            new("🌸", "Cherry blossom", ["flower", "bloom", "spring", "pink"]),
            new("🍀", "Four leaf clover", ["clover", "luck", "lucky", "green"]),
            new("🌿", "Herb", ["leaf", "green", "nature", "plant"]),
            new("☀️", "Sun", ["sun", "sunny", "bright", "warm", "day"]),
            new("🌙", "Crescent moon", ["moon", "night", "dark", "sleep"]),
        ]),
        new("Objects", [
            new("💻", "Laptop", ["laptop", "computer", "work", "code"]),
            new("🖥️", "Desktop", ["desktop", "computer", "monitor", "screen"]),
            new("📱", "Phone", ["phone", "mobile", "smartphone"]),
            new("⌚", "Watch", ["watch", "time", "clock"]),
            new("🔑", "Key", ["key", "access", "lock", "secret"]),
            new("🔒", "Locked", ["lock", "secure", "private", "locked"]),
            new("🔓", "Unlocked", ["unlock", "open", "public", "access"]),
            new("🔔", "Bell", ["bell", "notification", "alert", "ring"]),
            new("📚", "Books", ["books", "study", "read", "knowledge"]),
            new("📝", "Memo", ["memo", "note", "write", "pencil"]),
            new("📊", "Bar chart", ["chart", "data", "statistics", "graph"]),
            new("📈", "Chart up", ["chart", "up", "growth", "increase", "trend"]),
            new("📉", "Chart down", ["chart", "down", "decrease", "drop"]),
            new("📂", "Open folder", ["folder", "files", "organize", "directory"]),
            new("📎", "Paperclip", ["paperclip", "attach", "link", "clip"]),
            new("🔧", "Wrench", ["wrench", "fix", "tool", "repair", "configure"]),
            new("🔬", "Microscope", ["microscope", "science", "research", "detail"]),
            new("🔭", "Telescope", ["telescope", "look", "far", "discover"]),
            new("🎮", "Video game", ["game", "controller", "play", "fun"]),
            new("📷", "Camera", ["camera", "photo", "picture", "snapshot"]),
        ]),
        new("Activities & Sports", [
            new("🚀", "Rocket", ["rocket", "launch", "fast", "space", "startup"]),
            new("⚡", "Lightning", ["lightning", "fast", "electric", "energy", "power"]),
            new("🎯", "Direct hit", ["target", "goal", "focus", "aim", "hit"]),
            new("🏆", "Trophy", ["trophy", "win", "award", "champion", "success"]),
            new("🎉", "Party popper", ["party", "celebrate", "confetti", "success"]),
            new("🎓", "Graduation cap", ["graduation", "learn", "education", "diploma"]),
            new("⚽", "Soccer ball", ["soccer", "football", "sport", "ball"]),
            new("🏋️", "Weightlifter", ["lift", "strong", "workout", "gym"]),
            new("🏊", "Swimmer", ["swim", "water", "sport", "pool"]),
            new("🎸", "Guitar", ["guitar", "music", "rock", "instrument"]),
            new("🎨", "Artist palette", ["art", "paint", "creative", "design", "color"]),
            new("🏄", "Surfer", ["surf", "wave", "ride", "board"]),
        ]),
        new("Symbols", [
            new("❤️", "Red heart", ["heart", "love", "red", "important"]),
            new("💛", "Yellow heart", ["heart", "yellow", "happy"]),
            new("💚", "Green heart", ["heart", "green", "nature"]),
            new("💙", "Blue heart", ["heart", "blue", "calm"]),
            new("💜", "Purple heart", ["heart", "purple", "spirit"]),
            new("✅", "Check mark button", ["check", "yes", "done", "complete", "tick"]),
            new("❌", "Cross mark", ["cross", "no", "wrong", "delete", "cancel"]),
            new("⚠️", "Warning", ["warning", "caution", "alert", "danger"]),
            new("❓", "Question mark", ["question", "help", "unknown", "ask"]),
            new("❗", "Exclamation mark", ["exclamation", "important", "urgent", "alert"]),
            new("🔷", "Blue diamond", ["blue", "diamond", "shape"]),
            new("🔶", "Orange diamond", ["orange", "diamond", "shape"]),
            new("🔴", "Red circle", ["red", "circle", "stop", "danger"]),
            new("🟢", "Green circle", ["green", "circle", "go", "ok", "success"]),
            new("🟡", "Yellow circle", ["yellow", "circle", "caution", "pending"]),
            new("🔵", "Blue circle", ["blue", "circle", "info"]),
            new("⭕", "Circle", ["circle", "ring", "loop"]),
            new("💯", "Hundred points", ["100", "perfect", "full", "complete"]),
            new("♾️", "Infinity", ["infinity", "forever", "loop", "endless"]),
        ]),
    ];

    /// <summary>
    /// Searches emoji across all categories using a keyword query.
    /// Returns filtered categories containing only matching emoji.
    /// </summary>
    public static IReadOnlyList<EmojiCategory> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Categories;

        var q = query.Trim().ToLowerInvariant();
        var results = new List<EmojiCategory>();

        foreach (var category in Categories)
        {
            var matching = category.Emojis
                .Where(e =>
                    e.Emoji.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    e.Keywords.Any(k => k.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (matching.Length > 0)
                results.Add(new EmojiCategory(category.Name, matching));
        }

        return results;
    }

    /// <summary>
    /// Returns true when <paramref name="value"/> is a single emoji grapheme cluster.
    /// An empty or null value returns false.
    /// </summary>
    public static bool IsValidEmoji(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(value);
        var count = 0;
        while (enumerator.MoveNext())
        {
            count++;
            if (count > 1) return false;
        }

        return count == 1;
    }
}
