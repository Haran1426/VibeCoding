using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ReleaseSceneValidator
{
    private static readonly string[] RequiredBuildScenes =
    {
        "Assets/Scenes/MenuScene/MenuScene.unity",
        "Assets/Scenes/ArenaScene/ArenaScene.unity"
    };

    private static readonly string[] SceneFiles =
    {
        "Assets/Scenes/MenuScene/MenuScene.unity",
        "Assets/Scenes/ArenaScene/ArenaScene.unity",
        "Assets/Scenes/GameScene/GameScene.unity",
        "Assets/Scenes/SampleScene.unity"
    };

    private static readonly string[] ForbiddenSceneTokens =
    {
        "Title_Main_Runtime",
        "Title_Lobby_Runtime",
        "Title_Settings_Runtime",
        "Title Canvas Authored",
        "Arena Runtime Canvas",
        "MobileControls Canvas",
        "WaveSpawner",
        "ExpBar",
        "LocalLobbyUI",
        "LocalArenaBootstrapper",
        "localLobbyUI",
        "playSingleButton",
        "b113ff6b1da50e5499f8adad9139c365",
        "85ce9dc75c2c1ca4faa60cbcb02bf197"
    };

    [MenuItem("Tools/Neon Rewind/Validate Release Scenes")]
    public static void ValidateReleaseScenes()
    {
        var failures = new List<string>();
        ValidateBuildSettings(failures);
        ValidateSceneFiles(failures);

        if (failures.Count > 0)
        {
            string message = "Release scene validation failed:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log("Release scene validation passed.");
    }

    private static void ValidateBuildSettings(List<string> failures)
    {
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length != RequiredBuildScenes.Length)
        {
            failures.Add($"Build Settings should contain {RequiredBuildScenes.Length} scenes, found {scenes.Length}.");
            return;
        }

        for (int i = 0; i < RequiredBuildScenes.Length; i++)
        {
            if (!scenes[i].enabled)
                failures.Add($"Build scene {i} is disabled: {scenes[i].path}");

            if (scenes[i].path != RequiredBuildScenes[i])
                failures.Add($"Build scene {i} should be {RequiredBuildScenes[i]}, found {scenes[i].path}.");
        }
    }

    private static void ValidateSceneFiles(List<string> failures)
    {
        foreach (string scenePath in SceneFiles)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Missing scene file: {scenePath}");
                continue;
            }

            string text = File.ReadAllText(scenePath);
            if (text.Contains("m_Script: {fileID: 0}"))
                failures.Add($"Missing script reference found in {scenePath}.");

            foreach (string token in ForbiddenSceneTokens)
            {
                if (text.Contains(token))
                    failures.Add($"Forbidden legacy token '{token}' found in {scenePath}.");
            }
        }
    }
}
