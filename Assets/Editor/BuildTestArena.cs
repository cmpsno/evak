// BuildTestArena.cs — programmatic v0 TestArena construction.
// Run headless: Unity -batchmode -nographics -projectPath <proj> -executeMethod BuildTestArena.Build -quit
// WebGL:        ... -executeMethod BuildTestArena.BuildWebGL -quit
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Evak.Core;
using Evak.HUD;
using Evak.Player;
using Evak.Weapons;

public static class BuildTestArena
{
    const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
    const string RiflePath = "Assets/_Project/Data/Weapons/Rifle.asset";
    const string InputActionsPath = "Assets/_Project/Data/PlayerInputActions.inputactions";
    const string UrpAssetPath = "Assets/_Project/Settings/URP.asset";

    [MenuItem("Evak/Build TestArena")]
    public static void Build()
    {
        EnsureTag("Target");
        EnsureUrpAsset();
        SetInputHandlerToInputSystem();
        var inputAsset = CreateInputActions();
        var rifle = CreateRifleAsset();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildLighting();
        BuildArena();
        var player = BuildPlayer(inputAsset, rifle);
        var hud = BuildHud();
        var wc = player.GetComponentInChildren<WeaponController>();
        var so = new SerializedObject(wc);
        so.FindProperty("hud").objectReferenceValue = hud;
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildTestArena] Scene saved to " + ScenePath);
    }

    [MenuItem("Evak/Build WebGL")]
    public static void BuildWebGL()
    {
        Build(); // ensure scene + assets exist first

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new System.Exception("Could not switch to WebGL target — is the WebGL module installed?");

        // No-compression output: plain http.server can serve it, no gzip headers needed.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "evak-webgl"));
        Directory.CreateDirectory(outDir);

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[BuildTestArena] WebGL build result: " + report.summary.result + " -> " + outDir);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("WebGL build failed: " + report.summary.result);
    }

    // ---------------------------------------------------------------- setup

    public static void EnsureTag(string tag)
    {
        foreach (var t in InternalEditorUtility.tags)
            if (t == tag) return;
        InternalEditorUtility.AddTag(tag);
        Debug.Log("[BuildTestArena] Added tag: " + tag);
    }

    public static void EnsureUrpAsset()
    {
        Directory.CreateDirectory("Assets/_Project/Settings");
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
        if (urp == null)
        {
            // A bare CreateInstance<UniversalRenderPipelineAsset>() has no renderer
            // data -> CreatePipeline() NREs. Wire a real UniversalRendererData first.
            const string rendererPath = "Assets/_Project/Settings/URPRenderer.asset";
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }
            urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var so = new SerializedObject(urp);
            var listProp = so.FindProperty("m_RendererDataList");
            listProp.arraySize = 1;
            listProp.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(urp, UrpAssetPath);
            Debug.Log("[BuildTestArena] Created URP pipeline asset with renderer data.");
        }
        GraphicsSettings.renderPipelineAsset = urp;
        QualitySettings.renderPipeline = urp;
    }

    public static void SetInputHandlerToInputSystem()
    {
        var objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (objs == null || objs.Length == 0) return;
        var so = new SerializedObject(objs[0]);
        var prop = so.FindProperty("activeInputHandler");
        if (prop != null && prop.intValue != 2)
        {
            prop.intValue = 2; // Input System Package (new)
            so.ApplyModifiedProperties();
            Debug.Log("[BuildTestArena] Set activeInputHandler=2 (Input System).");
        }
    }

    public static InputActionAsset CreateInputActions()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("Player");

        var move = map.AddAction("Move", InputActionType.Value);
        move.expectedControlType = "Vector2";
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        move.AddBinding("<Gamepad>/leftStick");

        var look = map.AddAction("Look", InputActionType.Value);
        look.expectedControlType = "Vector2";
        look.AddBinding("<Mouse>/delta");
        look.AddBinding("<Gamepad>/rightStick");

        AddButton(map, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
        AddButton(map, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
        AddButton(map, "Crouch", "<Keyboard>/c", "<Gamepad>/buttonEast");
        AddButton(map, "Fire", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
        AddButton(map, "Reload", "<Keyboard>/r", "<Gamepad>/buttonWest");
        AddButton(map, "ADS", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
        AddButton(map, "Interact", "<Keyboard>/e", "<Gamepad>/buttonNorth");
        // Dialogue choice selection (desktop keys; mobile uses on-screen buttons)
        AddButton(map, "Choice1", "<Keyboard>/1", "<Keyboard>/numpad1");
        AddButton(map, "Choice2", "<Keyboard>/2", "<Keyboard>/numpad2");
        AddButton(map, "Choice3", "<Keyboard>/3", "<Keyboard>/numpad3");

        Directory.CreateDirectory("Assets/_Project/Data");
        // Write JSON, not via AssetDatabase.CreateAsset: the .inputactions importer
        // parses JSON, and CreateAsset writes YAML which fails import (asset loads as
        // null => every input action silently dead).
        if (File.Exists(InputActionsPath))
            AssetDatabase.DeleteAsset(InputActionsPath);
        File.WriteAllText(InputActionsPath, asset.ToJson());
        AssetDatabase.ImportAsset(InputActionsPath);
        var loaded = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        if (loaded == null)
            throw new System.Exception("PlayerInputActions.inputactions failed to import.");
        return loaded;
    }

    static void AddButton(InputActionMap map, string name, params string[] paths)
    {
        var a = map.AddAction(name, InputActionType.Button);
        foreach (var p in paths) a.AddBinding(p);
    }

    public static WeaponData CreateRifleAsset()
    {
        var rifle = ScriptableObject.CreateInstance<WeaponData>();
        rifle.weaponName = "AR-1 Rifle";
        rifle.fireMode = FireMode.Auto;
        rifle.damage = 25f;
        rifle.fireRate = 600f;
        rifle.magazineSize = 30;
        rifle.range = 100f;
        rifle.reloadTimeTactical = 2.0f;
        rifle.reloadTimeEmpty = 2.6f;
        rifle.hipFireSpreadAngle = 2.5f;
        rifle.adsSpreadAngle = 0.4f;
        rifle.adsZoomFOV = 45f;
        rifle.adsTransitionTime = 0.18f;
        rifle.recoilKickPerShot = 0.6f;
        rifle.recoilRecoverySpeed = 6f;

        Directory.CreateDirectory("Assets/_Project/Data/Weapons");
        if (AssetDatabase.LoadAssetAtPath<WeaponData>(RiflePath) != null)
            AssetDatabase.DeleteAsset(RiflePath);
        AssetDatabase.CreateAsset(rifle, RiflePath);
        return rifle;
    }

    // ---------------------------------------------------------------- scene

    public static Material MakeMat(string name, Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return new Material(shader) { name = name, color = c };
    }

    public static void BuildLighting()
    {
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void BuildArena()
    {
        var groundMat = MakeMat("GroundMat", new Color(0.25f, 0.25f, 0.28f));
        var wallMat = MakeMat("WallMat", new Color(0.45f, 0.45f, 0.48f));
        var rampMat = MakeMat("RampMat", new Color(0.30f, 0.40f, 0.55f));
        var platformMat = MakeMat("PlatformMat", new Color(0.40f, 0.50f, 0.40f));
        var targetMat = MakeMat("TargetMat", new Color(0.90f, 0.25f, 0.15f));

        // Ground: 60x60 (Plane primitive is 10x10)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // Ramps (slide testing): 15° and 30°
        AddRamp("Ramp15", new Vector2(12f, 8f), -15f, rampMat);
        AddRamp("Ramp30", new Vector2(-12f, 8f), -30f, rampMat);

        // Stairs: 8 steps, 0.25m rise each
        var stairs = new GameObject("Stairs");
        for (int i = 0; i < 8; i++)
        {
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = "Step" + i;
            step.transform.SetParent(stairs.transform, false);
            step.transform.localScale = new Vector3(3f, 0.25f, 0.4f);
            step.transform.position = new Vector3(-4f, 0.125f + i * 0.25f, 12f + i * 0.4f);
            step.GetComponent<Renderer>().sharedMaterial = wallMat;
        }

        // Low walls: 3 at 1.0m height (crouch testing)
        AddBox("Wall1", new Vector3(6f, 0.5f, -6f), new Vector3(0.4f, 1f, 5f), wallMat);
        AddBox("Wall2", new Vector3(-8f, 0.5f, -14f), new Vector3(5f, 1f, 0.4f), wallMat);
        AddBox("Wall3", new Vector3(16f, 0.5f, -18f), new Vector3(0.4f, 1f, 5f), wallMat);

        // Raised platforms: tops at 1.2m and 2.0m (jump testing)
        AddBox("Platform_1.2m", new Vector3(-14f, 0.95f, -4f), new Vector3(4f, 0.5f, 4f), platformMat);
        AddBox("Platform_2.0m", new Vector3(14f, 1.75f, 18f), new Vector3(4f, 0.5f, 4f), platformMat);

        // Target props: 5 capsules tagged "Target" at varied ranges/elevations
        AddTarget("Target_A", new Vector3(8f, 1f, -14f), targetMat);
        AddTarget("Target_B", new Vector3(-16f, 1f, 18f), targetMat);
        AddTarget("Target_C", new Vector3(2f, 1f, 38f), targetMat);
        AddTarget("Target_D", new Vector3(20f, 1f, 6f), targetMat);
        AddTarget("Target_E", new Vector3(-6f, 1f, 52f), targetMat);
    }

    static void AddRamp(string name, Vector2 xz, float angleDeg, Material mat)
    {
        float length = 9f, thick = 0.5f;
        float rad = Mathf.Abs(angleDeg) * Mathf.Deg2Rad;
        float halfDrop = (length / 2f) * Mathf.Sin(rad) + (thick / 2f) * Mathf.Cos(rad);
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = name;
        ramp.transform.localScale = new Vector3(5f, thick, length);
        ramp.transform.position = new Vector3(xz.x, halfDrop - 0.05f, xz.y);
        ramp.transform.rotation = Quaternion.Euler(angleDeg, 0f, 0f);
        ramp.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void AddBox(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = pos;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void AddTarget(string name, Vector3 pos, Material mat)
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        t.name = name;
        t.tag = "Target";
        t.transform.position = pos;
        t.AddComponent<TargetDummy>();
        t.GetComponent<Renderer>().sharedMaterial = mat;
    }

    public static GameObject BuildPlayer(InputActionAsset inputAsset, WeaponData rifle)
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.05f, -8f);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.3f;

        var playerInput = player.AddComponent<PlayerInput>();
        // Assign via SerializedObject, NOT the `actions` property setter: the setter
        // clones the asset into a transient in-memory copy when the component is
        // enabled, and that reference is lost on scene save (m_Actions == null in
        // the built scene => every input action silently dead). This is what the
        // Inspector does.
        var piSO = new SerializedObject(playerInput);
        piSO.FindProperty("m_Actions").objectReferenceValue = inputAsset;
        piSO.FindProperty("m_DefaultActionMap").stringValue = "Player";
        piSO.ApplyModifiedProperties();

        var inputProvider = player.AddComponent<PlayerInputProvider>();
        player.AddComponent<Evak.Core.WebGLPointerLock>();

        var movement = player.AddComponent<PlayerMovement>();
        SetField(movement, "inputProviderBehaviour", inputProvider);

        // Camera pivot at 1.6m — yaw on body, pitch on pivot
        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        var playerCam = pivot.AddComponent<PlayerCamera>();
        SetField(playerCam, "inputProviderBehaviour", inputProvider);

        var camGO = new GameObject("MainCamera");
        camGO.transform.SetParent(pivot.transform, false);
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.05f;

        // Weapon child of pivot (follows camera)
        var weaponGO = new GameObject("Weapon");
        weaponGO.transform.SetParent(pivot.transform, false);
        weaponGO.transform.localPosition = new Vector3(0.28f, -0.28f, 0.55f);

        var wc = weaponGO.AddComponent<WeaponController>();
        var muzzle = new GameObject("MuzzleSocket");
        muzzle.transform.SetParent(weaponGO.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.05f, 0.7f);

        // Placeholder gun mesh (no collider — must not block raycasts)
        var gunMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gunMesh.name = "GunPlaceholder";
        gunMesh.transform.SetParent(weaponGO.transform, false);
        gunMesh.transform.localScale = new Vector3(0.09f, 0.14f, 0.65f);
        gunMesh.GetComponent<Renderer>().sharedMaterial = MakeMat("GunMat", new Color(0.12f, 0.12f, 0.14f));
        Object.DestroyImmediate(gunMesh.GetComponent<BoxCollider>());

        var so = new SerializedObject(wc);
        so.FindProperty("weaponData").objectReferenceValue = rifle;
        so.FindProperty("playerCamera").objectReferenceValue = cam;
        so.FindProperty("muzzleSocket").objectReferenceValue = muzzle.transform;
        so.FindProperty("inputProviderBehaviour").objectReferenceValue = inputProvider;
        so.ApplyModifiedProperties();

        return player;
    }

    static HUDController BuildHud()
    {
        var canvasGO = new GameObject("HUD");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var hud = canvasGO.AddComponent<HUDController>();
        // NOTE: Resources.GetBuiltinResource<Font>("Arial.ttf") THROWS
        // ArgumentException in 2022.3 ("no longer a valid built in font").
        // LegacyRuntime.ttf is the valid builtin here.
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Crosshair: small centered square
        var crossGO = new GameObject("Crosshair");
        crossGO.transform.SetParent(canvasGO.transform, false);
        var crossRect = crossGO.AddComponent<RectTransform>();
        crossRect.anchorMin = crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.sizeDelta = new Vector2(10f, 10f);
        crossRect.anchoredPosition = Vector2.zero;
        var crossImg = crossGO.AddComponent<Image>();
        crossImg.color = new Color(1f, 1f, 1f, 0.85f);

        // Ammo: bottom-right legacy Text
        var ammoGO = new GameObject("AmmoText");
        ammoGO.transform.SetParent(canvasGO.transform, false);
        var ammoRect = ammoGO.AddComponent<RectTransform>();
        ammoRect.anchorMin = ammoRect.anchorMax = new Vector2(1f, 0f);
        ammoRect.sizeDelta = new Vector2(220f, 50f);
        ammoRect.anchoredPosition = new Vector2(-130f, 45f);
        var ammoText = ammoGO.AddComponent<Text>();
        ammoText.font = font;
        ammoText.fontSize = 28;
        ammoText.alignment = TextAnchor.MiddleRight;
        ammoText.text = "30 / 120";

        // Hitmarker: centered, hidden by default
        var hitGO = new GameObject("Hitmarker");
        hitGO.transform.SetParent(canvasGO.transform, false);
        var hitRect = hitGO.AddComponent<RectTransform>();
        hitRect.anchorMin = hitRect.anchorMax = new Vector2(0.5f, 0.5f);
        hitRect.sizeDelta = new Vector2(28f, 28f);
        hitRect.anchoredPosition = Vector2.zero;
        var hitImg = hitGO.AddComponent<Image>();
        hitImg.color = Color.white;
        hitGO.SetActive(false);

        // Reload text: bottom-center, hidden by default
        var reloadGO = new GameObject("ReloadText");
        reloadGO.transform.SetParent(canvasGO.transform, false);
        var reloadRect = reloadGO.AddComponent<RectTransform>();
        reloadRect.anchorMin = reloadRect.anchorMax = new Vector2(0.5f, 0f);
        reloadRect.sizeDelta = new Vector2(300f, 40f);
        reloadRect.anchoredPosition = new Vector2(0f, 90f);
        var reloadText = reloadGO.AddComponent<Text>();
        reloadText.font = font;
        reloadText.fontSize = 22;
        reloadText.alignment = TextAnchor.MiddleCenter;
        reloadText.text = "RELOADING...";
        reloadGO.SetActive(false);

        var so = new SerializedObject(hud);
        so.FindProperty("crosshair").objectReferenceValue = crossRect;
        so.FindProperty("ammoText").objectReferenceValue = ammoText;
        so.FindProperty("hitmarker").objectReferenceValue = hitImg;
        so.FindProperty("reloadText").objectReferenceValue = reloadText;
        so.ApplyModifiedProperties();

        return hud;
    }

    public static void SetField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogError("[BuildTestArena] Serialized field not found: " + fieldName);
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
