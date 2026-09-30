using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Scrapshift
{
    public static class SaveStore
    {
        public static void Write(string path, YardState state)
        {
            YardModel.Validate(state);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(state, true));
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            // Same-volume atomic replacement preserves the last successful save as backup.
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }

        public static YardState Read(string path, out string message)
        {
            message = "New yard. Start at the delivery crate.";
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return new YardState();
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    string json = File.ReadAllText(candidate);
                    if (!json.Contains("\"version\"") || !json.Contains("\"items\"")) throw new InvalidDataException("Missing save fields.");
                    var state = JsonUtility.FromJson<YardState>(json);
                    YardModel.Validate(state);
                    message = candidate == path ? "Yard restored." : "Restored backup; latest save was unreadable.";
                    return state;
                }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
                {
                    Debug.LogWarning("Could not load " + Path.GetFileName(candidate) + ": " + ex.Message);
                }
            }
            // Never silently replace unreadable saves with a fresh game.
            throw new InvalidDataException("Save and backup are unreadable. Move them aside or choose New Game.");
        }
    }
}
