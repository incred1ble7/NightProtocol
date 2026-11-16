using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local development commands only. No listener or arbitrary code execution.
[InitializeOnLoad]
public static class MechanicsEditorBridge
{
    const string Folder = "C:/Users/aser/Documents/Codex/2026-11-14/files-mentioned-by-the-user-1/work";
    static double nextPoll;
    static MechanicsEditorBridge() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + 0.5;
        var path = Path.Combine(Folder, "unity_command.txt");
        if (!File.Exists(path)) return;
        var command = File.ReadAllText(path).Trim();
        File.Delete(path);
        try
        {
            string result;
            if (command == "inspect") result = Inspect();
            else if (command == "play") { EditorApplication.isPlaying = true; result = "Play requested"; }
            else if (command == "stop") { EditorApplication.isPlaying = false; result = "Stop requested"; }
            else if (command == "setup" || command == "validate" || command == "runtime-test" || command == "capture")
            {
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechanicsSetup")).FirstOrDefault(t => t != null);
                if(type == null) throw new Exception("MechanicsSetup is not implemented yet");
                var method = command == "setup" ? "Build" : command == "validate" ? "Validate" : command == "runtime-test" ? "RuntimeTest" : "Capture";
                result = (string)type.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null,null);
            }
            else throw new Exception("Unknown mechanics command");
            File.WriteAllText(Path.Combine(Folder, "unity_result.txt"), command + "\n" + result);
        }
        catch(Exception e) { File.WriteAllText(Path.Combine(Folder,"unity_result.txt"),"ERROR " + command + "\n" + e); Debug.LogException(e); }
    }
    static string Inspect()
    {
        var lines = new System.Collections.Generic.List<string>();
        var s = SceneManager.GetActiveScene();
        lines.Add("SCENE " + s.path + " dirty=" + s.isDirty + " playing=" + Application.isPlaying);
        foreach(var go in s.GetRootGameObjects())
            lines.Add("ROOT " + go.name + " pos=" + go.transform.position + " size=" + go.transform.localScale);
        foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{"Assets/NightProtocol/Mechanics/Models"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            lines.Add("MODEL " + path);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var t in go.GetComponentsInChildren<Transform>(true)) lines.Add("  " + AnimationUtility.CalculateTransformPath(t,go.transform)+ " pos="+t.localPosition+" rot="+t.localEulerAngles+" scale="+t.localScale);
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")))
            {
                lines.Add("  CLIP " + clip.name + " duration=" + clip.length);
                foreach(var b in AnimationUtility.GetCurveBindings(clip).Take(18)) lines.Add("    " + b.path + " / " + b.propertyName);
            }
        }
        return string.Join("\n",lines);
    }
}
