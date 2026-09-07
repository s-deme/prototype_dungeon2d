using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using System.IO;

namespace LanternDepths.Editor
{
    public static class BuildGame
    {
        [MenuItem("Lantern Depths/Open Main Scene")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene("Assets/Game/Scenes/Main.unity");
        }
        [MenuItem("Lantern Depths/Build Windows")]
        public static void Windows()
        {
            Build("Builds/Windows/LanternDepths.exe", BuildOptions.None);
        }
        public static void WindowsSmoke()
        {
            Build("Builds/Smoke/LanternDepths.exe", BuildOptions.Development);
        }
        private static void Build(string path, BuildOptions options)
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Game/Scenes/Main.unity" },
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = options
            });
            if (report.summary.result != BuildResult.Succeeded) throw new System.InvalidOperationException("Windows build failed: " + report.summary.result);
            string output = Path.GetDirectoryName(path);
            File.Copy("docs/PLAYER_GUIDE.txt", Path.Combine(output, "READ_ME.txt"), true);
            File.Copy("docs/CREDITS.txt", Path.Combine(output, "CREDITS.txt"), true);
            File.Copy(Path.Combine(EditorApplication.applicationContentsPath, "Resources/legal.txt"), Path.Combine(output, "Unity-Third-Party-Notices.txt"), true);
            File.WriteAllText(Path.Combine(output, "BUILD_INFO.txt"), $"Lantern Depths {UnityEngine.Application.version}\nUnity {UnityEngine.Application.unityVersion}\nWindows x64\nSave format 2 (imports format 1)\nDevelopment build: {options.HasFlag(BuildOptions.Development)}\n");
        }
    }
}
