using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class GameSetup : EditorWindow
{
    [MenuItem("ゲーム/シーンセットアップ")]
    public static void SetupScene()
    {
        // ============================================================
        // 0. Active Input Handling を "Both" に自動変更
        // ============================================================
        Object[] projectSettingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (projectSettingsAssets.Length > 0)
        {
            SerializedObject projectSettings = new SerializedObject(projectSettingsAssets[0]);
            SerializedProperty activeInputHandler = projectSettings.FindProperty("activeInputHandler");
            if (activeInputHandler != null && activeInputHandler.intValue != 2)
            {
                activeInputHandler.intValue = 2; // 2 = Both
                projectSettings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[GameSetup] Active Input Handling を 'Both' に変更しました。");
            }
        }

        // ============================================================
        // 1. 強力なクリーンアップ（非アクティブも含む完全削除で分身バグを根絶）
        // ============================================================
        DeleteAllObjectsByName("Ground");
        DeleteAllObjectsByName("Player");
        DeleteAllObjectsByName("Spawner");
        DeleteAllObjectsByName("GameManager");
        DeleteAllObjectsByName("Canvas");
        DeleteAllObjectsByName("LeftWall");
        DeleteAllObjectsByName("RightWall");
        DeleteAllObjectsByName("EventSystem");

        // クローンや古いEventSystem・落下物も完全に一掃
        EventSystem[] oldES = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var es in oldES) Undo.DestroyObjectImmediate(es.gameObject);

        FallingObject[] oldFO = Object.FindObjectsByType<FallingObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var fo in oldFO) Undo.DestroyObjectImmediate(fo.gameObject);

        // ============================================================
        // 2. シェーダー取得
        // ============================================================
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        // ============================================================
        // 3. カメラ・照明
        // ============================================================
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0f, 6.5f, -14.5f);
        cam.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.06f); // より洗練された暗いトーン
        cam.clearFlags = CameraClearFlags.SolidColor;

        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight == null)
        {
            GameObject lObj = new GameObject("Directional Light");
            dirLight = lObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        dirLight.intensity = 1.5f;
        dirLight.color = new Color(0.85f, 0.9f, 1f);

        // ============================================================
        // 4. ゲームオブジェクトの配置
        // ============================================================
        // 地面
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(20f, 1f, 4f);
        SetMaterial(ground, urpShader, new Color(0.1f, 0.1f, 0.15f), 0.2f);
        Undo.RegisterCreatedObjectUndo(ground, "Ground");

        // プレイヤー
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        SetMaterial(player, urpShader, new Color(0.1f, 0.6f, 1f), 0.9f);

        CapsuleCollider pCol = player.GetComponent<CapsuleCollider>();
        pCol.isTrigger = true;
        Rigidbody pRb = player.AddComponent<Rigidbody>();
        pRb.useGravity = false;
        pRb.isKinematic = true;
        PlayerController playerCtrl = player.AddComponent<PlayerController>();
        Undo.RegisterCreatedObjectUndo(player, "Player");

        // 落下物プレハブ
        GameObject fObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fObj.name = "FallingObject";
        fObj.GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody fRb = fObj.AddComponent<Rigidbody>();
        fRb.useGravity = false;
        fRb.isKinematic = true;
        fObj.AddComponent<FallingObject>();
        SetMaterial(fObj, urpShader, new Color(1f, 0.2f, 0.2f), 0.5f);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(fObj, "Assets/Prefabs/FallingObject.prefab");
        Object.DestroyImmediate(fObj);

        // スポナー
        GameObject spawnerObj = new GameObject("Spawner");
        Spawner spawner = spawnerObj.AddComponent<Spawner>();
        spawner.fallingObjectPrefab = prefab;
        Undo.RegisterCreatedObjectUndo(spawnerObj, "Spawner");

        // GameManager & BGM
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.player = playerCtrl;
        gm.spawner = spawner;
        AudioSource audioSrc = gmObj.AddComponent<AudioSource>();
        AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/BGM.mp3");
        if (bgmClip != null)
        {
            audioSrc.clip = bgmClip;
            audioSrc.loop = true;
            audioSrc.playOnAwake = false;
        }
        else
        {
            Debug.LogWarning("[GameSetup] Assets/BGM.mp3 was not found.");
        }
        gm.bgmSource = audioSrc;
        Undo.RegisterCreatedObjectUndo(gmObj, "GameManager");

        // 壁
        CreateWall("LeftWall", -9f);
        CreateWall("RightWall", 9f);

        // ============================================================
        // 5. UI構築 — iOS 26 Liquid Glass 超透明＆広空間デザイン
        // ============================================================
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasObj, "Canvas");

        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        Undo.RegisterCreatedObjectUndo(esObj, "EventSystem");

        List<Image> allGlassButtons = new List<Image>();
        List<Image> allMobileButtons = new List<Image>();

        // ------- START PANEL（極限まで透明化し空間にゆとりを付与） -------
        GameObject startPanel = CreatePanel("StartPanel", canvasObj.transform);
        Image startBg = startPanel.GetComponent<Image>();
        startBg.color = new Color(0.04f, 0.04f, 0.08f, 0.25f); // 空間的余裕のためにアルファ値を極低に
        gm.startPanel = startPanel;
        gm.startPanelBg = startBg;

        // タイトル (文字サイズと配置をゆったりと)
        CreateText("Title", startPanel.transform,
            "落下回避ゲーム", 84, new Color(0.6f, 0.9f, 1f, 0.9f),
            new Vector2(0.5f, 0.78f), new Vector2(900f, 150f));

        // サブタイトル
        CreateText("Subtitle", startPanel.transform,
            "上から落ちてくる障害物を避けるゲーム", 28, new Color(1f, 1f, 1f, 0.35f),
            new Vector2(0.5f, 0.68f), new Vector2(700f, 50f));

        // モード選択ボタン (マージンを広く持ち、洗練されたレイアウト)
        GameObject kbBtn = CreateGlassButton("KeyboardBtn", startPanel.transform,
            "キーボードでプレイ", new Color(0.3f, 0.5f, 1f, 0.12f),
            new Vector2(0.5f, 0.48f), new Vector2(400f, 80f));
        allGlassButtons.Add(kbBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(kbBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.SelectKeyboardMode));

        GameObject mbBtn = CreateGlassButton("MobileBtn", startPanel.transform,
            "スマホ（ボタン操作）", new Color(0.2f, 0.8f, 0.5f, 0.12f),
            new Vector2(0.5f, 0.38f), new Vector2(400f, 80f));
        allGlassButtons.Add(mbBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(mbBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.SelectMobileButtonsMode));

        GameObject mbJoyBtn = CreateGlassButton("MobileJoyBtn", startPanel.transform,
            "スマホ（スティック操作）", new Color(0.9f, 0.6f, 0.15f, 0.12f),
            new Vector2(0.5f, 0.28f), new Vector2(400f, 80f));
        allGlassButtons.Add(mbJoyBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(mbJoyBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.SelectMobileJoystickMode));

        GameObject mbDragBtn = CreateGlassButton("MobileDragBtn", startPanel.transform,
            "スマホ（ドラッグ操作）", new Color(0.8f, 0.3f, 0.8f, 0.12f),
            new Vector2(0.5f, 0.18f), new Vector2(400f, 80f));
        allGlassButtons.Add(mbDragBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(mbDragBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.SelectMobileDragMode));

        // ------- GAMEPLAY PANEL -------
        GameObject gamePlayPanel = CreateUIObj("GamePlayPanel", canvasObj.transform);
        StretchRectTransform(gamePlayPanel);
        gm.gamePlayPanel = gamePlayPanel;

        // 洗練されたスコア表示
        gm.scoreText = CreateText("ScoreText", gamePlayPanel.transform,
            "0", 56, Color.white,
            new Vector2(0f, 1f), new Vector2(300f, 80f),
            new Vector2(180f, -90f), TextAnchor.UpperLeft);

        CreateText("ScoreLabel", gamePlayPanel.transform,
            "スコア", 18, new Color(1f, 1f, 1f, 0.3f),
            new Vector2(0f, 1f), new Vector2(300f, 30f),
            new Vector2(180f, -130f), TextAnchor.UpperLeft);

        // ポーズボタン (角にゆったりと配置)
        GameObject pauseBtn = CreateGlassButton("PauseBtn", gamePlayPanel.transform,
            "一時停止", new Color(1f, 1f, 1f, 0.1f),
            new Vector2(1f, 1f), new Vector2(180f, 80f), new Vector2(-140f, -90f));
        allGlassButtons.Add(pauseBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(pauseBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.TogglePause));

        // 無敵ステータス表示
        gm.invincibleStatusText = CreateText("InvStatus", gamePlayPanel.transform,
            "無敵モード: OFF", 20, new Color(1f, 0.3f, 0.3f, 0.35f),
            new Vector2(0f, 0f), new Vector2(300f, 40f),
            new Vector2(180f, 80f), TextAnchor.LowerLeft);

        // --- スマホ用左右コントロール（ゆったりと画面の端に余裕を持たせて配置） ---
        GameObject mobCtrl = CreateUIObj("MobileControls", gamePlayPanel.transform);
        RectTransform mcRT = mobCtrl.GetComponent<RectTransform>();
        mcRT.anchorMin = new Vector2(0f, 0f);
        mcRT.anchorMax = new Vector2(1f, 0f);
        mcRT.anchoredPosition = new Vector2(0f, 150f); // 空間的余裕のために少し高めに配置
        mcRT.sizeDelta = new Vector2(0f, 180f);
        gm.mobileControls = mobCtrl;

        // 左ボタン (画面の左端から少し内側へ十分なマージンをとる)
        GameObject leftBtn = CreateGlassButton("LeftBtn", mobCtrl.transform,
            "◀", new Color(0.5f, 0.7f, 1f, 0.1f),
            new Vector2(0.18f, 0.5f), new Vector2(150f, 120f));
        allMobileButtons.Add(leftBtn.GetComponent<Image>());
        gm.leftButton = leftBtn;

        // 右ボタン (画面の右端から少し内側へ十分なマージンをとる)
        GameObject rightBtn = CreateGlassButton("RightBtn", mobCtrl.transform,
            "▶", new Color(0.5f, 0.7f, 1f, 0.1f),
            new Vector2(0.82f, 0.5f), new Vector2(150f, 120f));
        allMobileButtons.Add(rightBtn.GetComponent<Image>());
        gm.rightButton = rightBtn;

        mobCtrl.SetActive(false);

        // --- スマホ用：仮想ジョイスティック ---
        GameObject joystickObj = CreateUIObj("MobileJoystick", gamePlayPanel.transform);
        RectTransform joyRT = joystickObj.GetComponent<RectTransform>();
        joyRT.anchorMin = new Vector2(0.5f, 0f);
        joyRT.anchorMax = new Vector2(0.5f, 0f);
        joyRT.anchoredPosition = new Vector2(0f, 250f);
        joyRT.sizeDelta = new Vector2(300f, 300f);
        VirtualJoystick joystick = joystickObj.AddComponent<VirtualJoystick>();
        gm.joystickContainer = joystickObj;
        gm.virtualJoystick = joystick;

        // ジョイスティック背景
        GameObject joyBgObj = CreateUIObj("JoystickBackground", joystickObj.transform);
        RectTransform joyBgRT = joyBgObj.GetComponent<RectTransform>();
        joyBgRT.anchorMin = new Vector2(0.5f, 0.5f);
        joyBgRT.anchorMax = new Vector2(0.5f, 0.5f);
        joyBgRT.anchoredPosition = Vector2.zero;
        joyBgRT.sizeDelta = new Vector2(250f, 250f);
        Image joyBgImg = joyBgObj.AddComponent<Image>();
        joyBgImg.color = new Color(1f, 1f, 1f, 0.1f);
        allGlassButtons.Add(joyBgImg);
        joystick.joystickBackground = joyBgRT;

        // ジョイスティックつまみ（Knob）
        GameObject joyKnobObj = CreateUIObj("JoystickKnob", joyBgObj.transform);
        RectTransform joyKnobRT = joyKnobObj.GetComponent<RectTransform>();
        joyKnobRT.anchorMin = new Vector2(0.5f, 0.5f);
        joyKnobRT.anchorMax = new Vector2(0.5f, 0.5f);
        joyKnobRT.anchoredPosition = Vector2.zero;
        joyKnobRT.sizeDelta = new Vector2(110f, 110f);
        Image joyKnobImg = joyKnobObj.AddComponent<Image>();
        joyKnobImg.color = new Color(0.4f, 0.7f, 1f, 0.25f);
        allMobileButtons.Add(joyKnobImg);
        joystick.joystickKnob = joyKnobRT;

        // つまみの中に骸骨マークを配置（画像イメージの再現）
        CreateText("KnobSkull", joyKnobObj.transform,
            "💀", 42, new Color(1f, 1f, 1f, 0.9f),
            new Vector2(0.5f, 0.5f), new Vector2(100f, 100f));

        joystickObj.SetActive(false);

        // --- スマホ用：ドラッグ入力パネル ---
        GameObject dragPanelObj = CreateUIObj("DragInputPanel", gamePlayPanel.transform);
        RectTransform dragRT = dragPanelObj.GetComponent<RectTransform>();
        dragRT.anchorMin = new Vector2(0f, 0f);
        dragRT.anchorMax = new Vector2(1f, 0.5f); // 画面下半分
        dragRT.anchoredPosition = Vector2.zero;
        dragRT.sizeDelta = Vector2.zero;
        Image dragBg = dragPanelObj.AddComponent<Image>();
        dragBg.color = new Color(0f, 0f, 0f, 0.01f); // タッチ可能なほぼ透明な背景
        DragInputPanel dragPanel = dragPanelObj.AddComponent<DragInputPanel>();
        gm.dragInputArea = dragPanelObj;
        gm.dragInputPanel = dragPanel;

        // ガイドテキスト
        CreateText("DragHint", dragPanelObj.transform,
            "← 画面をスワイプして移動 →", 28, new Color(1f, 1f, 1f, 0.25f),
            new Vector2(0.5f, 0.3f), new Vector2(800f, 60f));

        dragPanelObj.SetActive(false);

        // ------- PAUSE PANEL -------
        GameObject pausePanel = CreatePanel("PausePanel", canvasObj.transform);
        Image pauseBg = pausePanel.GetComponent<Image>();
        pauseBg.color = new Color(0.03f, 0.03f, 0.05f, 0.22f); // 後ろがはっきり透ける薄いガラス背景
        gm.pausePanel = pausePanel;
        gm.pausePanelBg = pauseBg;

        CreateText("PauseTitle", pausePanel.transform,
            "一時停止中", 64, new Color(0.7f, 0.85f, 1f),
            new Vector2(0.5f, 0.68f), new Vector2(500f, 100f));

        GameObject resumeBtn = CreateGlassButton("ResumeBtn", pausePanel.transform,
            "再開する", new Color(0.2f, 0.8f, 0.5f, 0.12f),
            new Vector2(0.5f, 0.50f), new Vector2(300f, 75f));
        allGlassButtons.Add(resumeBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(resumeBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.TogglePause));

        GameObject invBtn = CreateGlassButton("InvincibleBtn", pausePanel.transform,
            "無敵モード切替", new Color(1f, 0.6f, 0.15f, 0.12f),
            new Vector2(0.5f, 0.38f), new Vector2(300f, 70f));
        allGlassButtons.Add(invBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(invBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.ToggleInvincible));

        // メニューに戻るボタンを追加
        GameObject menuBtn = CreateGlassButton("ReturnToMenuBtn", pausePanel.transform,
            "メニューに戻る", new Color(0.7f, 0.2f, 0.2f, 0.12f),
            new Vector2(0.5f, 0.26f), new Vector2(300f, 70f));
        allGlassButtons.Add(menuBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(menuBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.BackToMenu));

        // ------- GAME OVER PANEL -------
        GameObject goPanel = CreatePanel("GameOverPanel", canvasObj.transform);
        Image goBg = goPanel.GetComponent<Image>();
        goBg.color = new Color(0.1f, 0.02f, 0.02f, 0.3f); // 赤みがかった超透明ガラス
        gm.gameOverPanel = goPanel;
        gm.gameOverPanelBg = goBg;

        gm.gameOverText = CreateText("GOTitle", goPanel.transform,
            "ゲームオーバー", 76, new Color(1f, 0.25f, 0.25f),
            new Vector2(0.5f, 0.72f), new Vector2(600f, 100f));

        gm.restartText = CreateText("GOScore", goPanel.transform,
            "スコア: 0", 38, new Color(1f, 1f, 1f, 0.85f),
            new Vector2(0.5f, 0.58f), new Vector2(500f, 60f));

        gm.highScoreText = CreateText("GOHighScore", goPanel.transform,
            "ハイスコア: 0", 30, new Color(1f, 0.8f, 0.2f, 0.75f),
            new Vector2(0.5f, 0.49f), new Vector2(500f, 50f));

        GameObject retryBtn = CreateGlassButton("RetryBtn", goPanel.transform,
            "もう一度プレイ", new Color(0.2f, 0.75f, 0.5f, 0.15f),
            new Vector2(0.5f, 0.32f), new Vector2(300f, 80f));
        allGlassButtons.Add(retryBtn.GetComponent<Image>());
        UnityEventTools.AddPersistentListener(retryBtn.GetComponent<Button>().onClick,
            new UnityEngine.Events.UnityAction(gm.RetryGame));

        gm.glassButtons = allGlassButtons.ToArray();
        gm.glassMobileButtons = allMobileButtons.ToArray();

        // ============================================================
        // 6. Gameビューフォーカス
        // ============================================================
        System.Type gvType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gvType != null) EditorWindow.GetWindow(gvType);

        EditorUtility.SetDirty(gmObj);

        Debug.Log("[GameSetup] シーンの空間的・ガラス的アップデート完了！");
        EditorUtility.DisplayDialog("セットアップ完了",
            "iOS 26 Liquid Glass 超透明＆広空間デザインで再構築しました！\n\n" +
            "変更点:\n" +
            "・非アクティブを含む古いPlayer等を完全に全消去し、分身バグを修正しました。\n" +
            "・UIパネル全体のアルファ値を下げ、背景の3D空間が透けて見えるようにしました。\n" +
            "・スマホ用ボタンの位置を端から離して空間にゆとりを持たせました。\n" +
            "・ボタンやテキストの間隔にパディングを持たせました。",
            "OK");
    }

    private static void SetMaterial(GameObject obj, Shader shader, Color baseColor, float smoothness)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Smoothness", smoothness);
        rend.material = mat;
    }

    private static void CreateWall(string name, float xPos)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = new Vector3(xPos, 5f, 0f);
        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 20f, 4f);
        Undo.RegisterCreatedObjectUndo(wall, name);
    }

    private static GameObject CreateUIObj(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void StretchRectTransform(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject obj = CreateUIObj(name, parent);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        StretchRectTransform(obj);
        return obj;
    }

    private static Text CreateText(string name, Transform parent,
        string text, int fontSize, Color color,
        Vector2 anchorPos, Vector2 size,
        Vector2? offset = null, TextAnchor align = TextAnchor.MiddleCenter)
    {
        GameObject obj = CreateUIObj(name, parent);
        Text txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = align;
        txt.font = Font.CreateDynamicFontFromOSFont(new string[] { "Yu Gothic", "Meiryo", "MS Gothic", "Arial" }, fontSize);
        txt.fontStyle = FontStyle.Bold;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        shadow.effectDistance = new Vector2(2f, -2f);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset ?? Vector2.zero;
        rt.sizeDelta = size;

        return txt;
    }

    private static GameObject CreateGlassButton(string name, Transform parent,
        string labelText, Color bgColor,
        Vector2 anchorPos, Vector2 size, Vector2? offset = null)
    {
        GameObject obj = CreateUIObj(name, parent);
        Image img = obj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.15f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        GameObject txtObj = CreateUIObj("Label", obj.transform);
        Text txt = txtObj.AddComponent<Text>();
        txt.text = labelText;
        txt.fontSize = 22;
        txt.color = Color.white;
        txt.font = Font.CreateDynamicFontFromOSFont(new string[] { "Yu Gothic", "Meiryo", "MS Gothic", "Arial" }, 22);
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = txtObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        StretchRectTransform(txtObj);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset ?? Vector2.zero;
        rt.sizeDelta = size;

        return obj;
    }

    /// <summary>
    /// シーン内のすべてのTransformを検索し、名前が一致するオブジェクトを非アクティブも含めて完全に消去
    /// </summary>
    private static void DeleteAllObjectsByName(string name)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t != null && t.gameObject.scene.name != null && t.name == name)
            {
                Undo.DestroyObjectImmediate(t.gameObject);
            }
        }
    }
}
