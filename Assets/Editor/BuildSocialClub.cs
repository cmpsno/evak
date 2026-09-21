// BuildSocialClub.cs — v0.1 greybox social club (Newark, 1972) scene builder.
// Reuses v0 player (movement/camera/input) via BuildTestArena helpers.
// Run headless: Unity -batchmode -nographics -projectPath <proj> -executeMethod BuildSocialClub.BuildWebGL -quit
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CODClone.Core;
using CODClone.Interaction;
using CODClone.UI;
using Campusano.Dialogue;
using Campusano.Missions;
using Campusano.Objectives;
using Campusano.Persistence;

public static class BuildSocialClub
{
    const string ScenePath = "Assets/_Project/Scenes/SocialClub.unity";

    [MenuItem("CODClone/Build SocialClub")]
    public static void Build()
    {
        BuildTestArena.EnsureTag("Target");
        BuildTestArena.EnsureUrpAsset();
        BuildTestArena.SetInputHandlerToInputSystem();
        var inputAsset = BuildTestArena.CreateInputActions(); // includes Interact (E)
        var rifle = BuildTestArena.CreateRifleAsset();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildTestArena.BuildLighting();
        BuildClubGeometry();

        var player = BuildTestArena.BuildPlayer(inputAsset, rifle);
        player.transform.position = new Vector3(0f, 0.05f, -2f);
        player.transform.rotation = Quaternion.identity; // face +z, into the club
        BuildTestArena.EnsureTag("Player");
        player.tag = "Player";

        // v0.1: weapon exists but is disabled in this scene (no shooting yet)
        var weaponGO = player.transform.Find("CameraPivot/Weapon");
        if (weaponGO != null) weaponGO.gameObject.SetActive(false);

        var inputProvider = player.GetComponent<PlayerInputProvider>();
        var interactor = player.AddComponent<PlayerInteractor>();
        BuildTestArena.SetField(interactor, "inputProviderBehaviour", inputProvider);
        player.AddComponent<DebugPositionLogger>(); // TEMP: verification only, delete before ship

        BuildNpcC();
        BuildDialogueUI(inputProvider);
        BuildEpisodeSystems();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/_Project/Scenes/TestArena.unity", true),
            new EditorBuildSettingsScene(ScenePath, true),
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildSocialClub] Scene saved to " + ScenePath);
    }

    [MenuItem("CODClone/Build WebGL SocialClub")]
    public static void BuildWebGL()
    {
        Build();

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new System.Exception("Could not switch to WebGL target — is the WebGL module installed?");

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "cod-mobile-webgl"));
        Directory.CreateDirectory(outDir);

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[BuildSocialClub] WebGL build result: " + report.summary.result + " -> " + outDir);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("WebGL build failed: " + report.summary.result);
    }

    // ---------------------------------------------------------- greybox level

    static void BuildClubGeometry()
    {
        var floorMat = BuildTestArena.MakeMat("ClubFloorMat", new Color(0.30f, 0.30f, 0.32f));
        var wallMat = BuildTestArena.MakeMat("ClubWallMat", new Color(0.50f, 0.50f, 0.52f));
        var furnMat = BuildTestArena.MakeMat("ClubFurnMat", new Color(0.42f, 0.38f, 0.34f));

        // Floor: one big slab under everything (top at y=0)
        AddBox("Floor", new Vector3(5.5f, -0.1f, 6.5f), new Vector3(29f, 0.2f, 23f), floorMat);

        // ---- Bar room: x -6..6, z 0..9 ----
        AddBox("Wall_Bar_W", new Vector3(-6f, 1.5f, 4.5f), new Vector3(0.3f, 3f, 9.3f), wallMat);
        AddBox("Wall_Bar_E", new Vector3(6f, 1.5f, 4.5f), new Vector3(0.3f, 3f, 9.3f), wallMat);
        // South wall with entrance door gap x -1..1
        AddBox("Wall_Bar_S1", new Vector3(-3.5f, 1.5f, 0f), new Vector3(5f, 3f, 0.3f), wallMat);
        AddBox("Wall_Bar_S2", new Vector3(3.5f, 1.5f, 0f), new Vector3(5f, 3f, 0.3f), wallMat);
        // North wall with hallway doorway gap x 1..3
        AddBox("Wall_Bar_N1", new Vector3(-2.5f, 1.5f, 9f), new Vector3(7f, 3f, 0.3f), wallMat);
        AddBox("Wall_Bar_N2", new Vector3(4.5f, 1.5f, 9f), new Vector3(3f, 3f, 0.3f), wallMat);

        // Bar counter along the west wall + stools
        AddBox("BarCounter", new Vector3(-5f, 0.5f, 4.5f), new Vector3(1.2f, 1f, 4f), furnMat);
        for (int i = 0; i < 4; i++)
            AddBox("Stool_" + i, new Vector3(-3.8f, 0.225f, 2.5f + i * 1.3f), new Vector3(0.45f, 0.45f, 0.45f), furnMat);

        // Round tables (flattened cylinders) + chairs
        AddCylinder("Table_0", new Vector3(0f, 0.4f, 3f), 0.9f, 0.8f, furnMat);
        AddCylinder("Table_1", new Vector3(2.5f, 0.4f, 6f), 0.9f, 0.8f, furnMat);
        AddBox("Chair_0a", new Vector3(-1.2f, 0.25f, 3f), new Vector3(0.5f, 0.5f, 0.5f), furnMat);
        AddBox("Chair_0b", new Vector3(1.2f, 0.25f, 3f), new Vector3(0.5f, 0.5f, 0.5f), furnMat);
        AddBox("Chair_1a", new Vector3(1.3f, 0.25f, 6f), new Vector3(0.5f, 0.5f, 0.5f), furnMat);
        AddBox("Chair_1b", new Vector3(3.7f, 0.25f, 6f), new Vector3(0.5f, 0.5f, 0.5f), furnMat);

        // ---- Hallway A (north): x 1..3, z 9..13 ----
        AddBox("Wall_HallA_W", new Vector3(1f, 1.5f, 11f), new Vector3(0.3f, 3f, 4f), wallMat);
        // East wall with a gap z 11..13 where hallway B joins (the dogleg) --
        // a solid wall here seals the back room off entirely.
        AddBox("Wall_HallA_E1", new Vector3(3f, 1.5f, 10f), new Vector3(0.3f, 3f, 2f), wallMat);
        // North cap so players can't wander out onto the slab past z=13
        AddBox("Wall_HallA_N", new Vector3(2f, 1.5f, 13f), new Vector3(2.3f, 3f, 0.3f), wallMat);

        // ---- Hallway B (east, the dogleg): x 3..9, z 11..13 ----
        AddBox("Wall_HallB_N", new Vector3(6f, 1.5f, 13f), new Vector3(6f, 3f, 0.3f), wallMat);
        AddBox("Wall_HallB_S", new Vector3(6f, 1.5f, 11f), new Vector3(6f, 3f, 0.3f), wallMat);

        // ---- Back room: x 9..17, z 7..15 ----
        // West wall with doorway gap z 11..13
        AddBox("Wall_Back_W1", new Vector3(9f, 1.5f, 9f), new Vector3(0.3f, 3f, 4f), wallMat);
        AddBox("Wall_Back_W2", new Vector3(9f, 1.5f, 14f), new Vector3(0.3f, 3f, 2f), wallMat);
        AddBox("Wall_Back_E", new Vector3(17f, 1.5f, 11f), new Vector3(0.3f, 3f, 8f), wallMat);
        AddBox("Wall_Back_N", new Vector3(13f, 1.5f, 15f), new Vector3(8f, 3f, 0.3f), wallMat);
        AddBox("Wall_Back_S", new Vector3(13f, 1.5f, 7f), new Vector3(8f, 3f, 0.3f), wallMat);

        // Card table + chairs (Mook's game lives here next handoff)
        AddCylinder("CardTable", new Vector3(13f, 0.4f, 11f), 1.1f, 0.8f, furnMat);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2f / 5f;
            AddBox("CardChair_" + i,
                new Vector3(13f + Mathf.Cos(a) * 1.8f, 0.25f, 11f + Mathf.Sin(a) * 1.8f),
                new Vector3(0.5f, 0.5f, 0.5f), furnMat);
        }
    }

    static void BuildNpcC()
    {
        var npcMat = BuildTestArena.MakeMat("NpcMat", new Color(0.25f, 0.25f, 0.28f));
        var c = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        c.name = "NPC_C";
        c.transform.position = new Vector3(13f, 0.9f, 12.8f);
        c.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // face the hallway doorway
        c.transform.localScale = new Vector3(1f, 0.9f, 1f);
        c.GetComponent<Renderer>().sharedMaterial = npcMat;
        c.AddComponent<InteractableNPC>();
    }

    // ------------------------------------------------------------------ UI

    static void BuildDialogueUI(PlayerInputProvider inputProvider)
    {
        var canvasGO = new GameObject("DialogueUI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Prompt: centered, hidden by default
        var promptGO = new GameObject("InteractPrompt");
        promptGO.transform.SetParent(canvasGO.transform, false);
        var promptRect = promptGO.AddComponent<RectTransform>();
        promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.sizeDelta = new Vector2(400f, 40f);
        promptRect.anchoredPosition = new Vector2(0f, 60f);
        var promptText = promptGO.AddComponent<Text>();
        promptText.font = font;
        promptText.fontSize = 24;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.text = "";
        promptGO.SetActive(false);

        // Dialogue panel: bottom third
        var panelGO = new GameObject("DialoguePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.sizeDelta = new Vector2(0f, 180f);
        panelRect.anchoredPosition = new Vector2(0f, 90f);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);

        var speakerGO = new GameObject("SpeakerText");
        speakerGO.transform.SetParent(panelGO.transform, false);
        var speakerRect = speakerGO.AddComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.sizeDelta = new Vector2(-40f, 36f);
        speakerRect.anchoredPosition = new Vector2(0f, -28f);
        var speakerText = speakerGO.AddComponent<Text>();
        speakerText.font = font;
        speakerText.fontSize = 22;
        speakerText.color = new Color(1f, 0.85f, 0.4f);
        speakerText.text = "";

        var lineGO = new GameObject("LineText");
        lineGO.transform.SetParent(panelGO.transform, false);
        var lineRect = lineGO.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.sizeDelta = new Vector2(-40f, -70f);
        lineRect.anchoredPosition = new Vector2(0f, -15f);
        var lineText = lineGO.AddComponent<Text>();
        lineText.font = font;
        lineText.fontSize = 20;
        lineText.color = Color.white;
        lineText.text = "";
        panelGO.SetActive(false);

        // Job popup: centered, hidden by default
        var jobGO = new GameObject("JobPopup");
        jobGO.transform.SetParent(canvasGO.transform, false);
        var jobRect = jobGO.AddComponent<RectTransform>();
        jobRect.anchorMin = jobRect.anchorMax = new Vector2(0.5f, 0.6f);
        jobRect.sizeDelta = new Vector2(560f, 60f);
        jobRect.anchoredPosition = Vector2.zero;
        var jobImg = jobGO.AddComponent<Image>();
        jobImg.color = new Color(0f, 0f, 0f, 0.8f);
        var jobTextGO = new GameObject("JobText");
        jobTextGO.transform.SetParent(jobGO.transform, false);
        var jobTextRect = jobTextGO.AddComponent<RectTransform>();
        jobTextRect.anchorMin = Vector2.zero;
        jobTextRect.anchorMax = Vector2.one;
        jobTextRect.sizeDelta = Vector2.zero;
        jobTextRect.anchoredPosition = Vector2.zero;
        var jobText = jobTextGO.AddComponent<Text>();
        jobText.font = font;
        jobText.fontSize = 24;
        jobText.alignment = TextAnchor.MiddleCenter;
        jobText.color = new Color(1f, 0.85f, 0.4f);
        jobGO.SetActive(false);

        var ui = canvasGO.AddComponent<DialogueUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("inputProviderBehaviour").objectReferenceValue = inputProvider;
        so.FindProperty("promptText").objectReferenceValue = promptText;
        so.FindProperty("dialoguePanel").objectReferenceValue = panelGO;
        so.FindProperty("speakerText").objectReferenceValue = speakerText;
        so.FindProperty("lineText").objectReferenceValue = lineText;
        so.FindProperty("jobPopup").objectReferenceValue = jobGO;
        so.FindProperty("jobText").objectReferenceValue = jobText;
        so.ApplyModifiedProperties();
    }

    // ------------------------------------------------- Ep1 systems

    static void BuildEpisodeSystems()
    {
        var saves = new GameObject("SaveManager");
        saves.AddComponent<SaveManager>();

        var objectives = new GameObject("ObjectiveManager");
        objectives.AddComponent<ObjectiveManager>();

        var missions = new GameObject("MissionStateMachine");
        missions.AddComponent<MissionStateMachine>();

        var dialogue = new GameObject("DialogueController");
        var controller = dialogue.AddComponent<DialogueController>();
        controller.RegisterNodes(Ep1DialogueBank.Build());

        // Trigger volumes: hallway doorway (M1-S8) and back-room door (M2-S1).
        AddTriggerVolume("Vol_HallwayDoor", new Vector3(2f, 1f, 9f), new Vector3(2f, 3f, 1f));
        AddTriggerVolume("Vol_BackRoomDoor", new Vector3(9f, 1f, 12f), new Vector3(1f, 3f, 2f));
    }

    static void AddTriggerVolume(string volumeId, Vector3 center, Vector3 size)
    {
        var go = new GameObject("Trigger_" + volumeId);
        go.transform.position = center;
        var vol = go.AddComponent<TriggerVolume>();
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = Vector3.zero;
        col.size = size;
        var so = new SerializedObject(vol);
        so.FindProperty("volumeId").stringValue = volumeId;
        so.ApplyModifiedProperties();
    }

    // ---------------------------------------------------------------- helpers

    static GameObject AddBox(string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject AddCylinder(string name, Vector3 center, float radius, float height, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}
