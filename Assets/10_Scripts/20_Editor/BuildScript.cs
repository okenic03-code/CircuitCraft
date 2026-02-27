#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CircuitCraft.Editor
{
    public static class BuildScript
    {
        [MenuItem("Tools/CircuitCraft/Build/Windows x64")]
        public static void PerformWindowsBuild()
        {
            string[] scenes = new string[]
            {
                "Assets/30_Scenes/00_Menu/MainMenu.unity",
                "Assets/30_Scenes/10_Game/StageSelect.unity",
                "Assets/30_Scenes/10_Game/GamePlay.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Build/Windows/CircuitCraft.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.totalSize} bytes, {summary.totalTime}");
            }
            else
            {
                Debug.LogError($"Build failed: {summary.result}");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error)
                            Debug.LogError(msg.content);
                    }
                }
            }
        }
    }
}
#endif
