using System.Reflection;
using System.Runtime.InteropServices;

namespace TheBetterRoles;

public static class CustomSoundsManager
{
    private static readonly string SOUNDS_PATH = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}/BepInEx/resources/";
    public static void Play(string sound)
    {
        if (!Constants.ShouldPlaySfx()) return;
        var path = SOUNDS_PATH + sound + ".wav";
        if (!Directory.Exists(SOUNDS_PATH)) Directory.CreateDirectory(SOUNDS_PATH);
        DirectoryInfo folder = new(SOUNDS_PATH);
        if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            folder.Attributes = FileAttributes.Hidden;
        if (!File.Exists(path))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TheBetterRoles.Resources.Sounds." + sound + ".wav");
            if (stream == null)
            {
                Logger.Log($"Sound file missing：{sound}", "CustomSounds");
                return;
            }
            var fs = File.Create(path);
            stream.CopyTo(fs);
            fs.Close();
        }
        StartPlay(path);
        Logger.Log($"play sound：{sound}", "CustomSounds");
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern bool PlaySound(string Filename, int Mod, int Flags);
    private static void StartPlay(string path) => PlaySound(@$"{path}", 0, 1);

}
