#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Replica.AI;
using Replica.Dialogue;
using Replica.Interaction;
using Replica.Player;
using Replica.Quests;
using Replica.UI;

namespace Replica.EditorTools
{
    /// <summary>
    /// Arma automáticamente la escena según la Guía de Integración:
    /// jugador (perro con modelo y animaciones), cámara en tercera persona, colliders del entorno,
    /// NPCs y mascotas con modelos, UI de diálogo/HUD, gestores y NavMesh (AI Navigation 2.x).
    /// Es seguro ejecutarlo varias veces: reutiliza lo que ya existe.
    /// </summary>
    public static class AutoSceneBuilder
    {
        private const string DogKit = "Assets/Bublisher/3D Stylized Animated Dogs Kit/Prefabs/";
        private const string PlayerModelPath = DogKit + "corgi.prefab";
        private const string HumanModelPath = "Assets/ithappy/Creative_Characters_FREE/Prefabs/Base_Mesh.prefab";
        private const string CrowModelPath = "Assets/living birds/resources/lb_crow.prefab";
        private const string TortoiseModelPath = "Assets/Backrock Studios/LowPoly-Animals/Prefabs/Tortoise/Tortoise_v1.prefab";
        private const string ModelChildName = "Model";

        private struct NpcDef
        {
            public string objectName;
            public string displayName;
            public string questId;
            public NPCRole role;
            public CompanionAI.CompanionType companionType;
            public string modelPath;
            public float height;
            public Vector3 offsetFromPlayer;
        }

        // Mascotas primero (los dueños necesitan la referencia a su CompanionAI)
        private static readonly NpcDef[] Pets =
        {
            new NpcDef { objectName = "Cachorro",     displayName = "Cachorro",     questId = "Q1", role = NPCRole.LostPet, companionType = CompanionAI.CompanionType.GroundNavMesh, modelPath = DogKit + "chihuahua.prefab",      height = 0.35f, offsetFromPlayer = new Vector3(-10f, 0f, 12f) },
            new NpcDef { objectName = "Cachetoncito", displayName = "Cachetoncito", questId = "Q2", role = NPCRole.LostPet, companionType = CompanionAI.CompanionType.GroundNavMesh, modelPath = DogKit + "pug.prefab",            height = 0.40f, offsetFromPlayer = new Vector3(12f, 0f, -8f) },
            new NpcDef { objectName = "Perro Guía",   displayName = "Perro Guía",   questId = "Q3", role = NPCRole.LostPet, companionType = CompanionAI.CompanionType.GroundNavMesh, modelPath = DogKit + "germanshepherd.prefab", height = 0.75f, offsetFromPlayer = new Vector3(-14f, 0f, -6f) },
            new NpcDef { objectName = "Cuervo",       displayName = "Cuervo",       questId = "Q4", role = NPCRole.LostPet, companionType = CompanionAI.CompanionType.Flying,        modelPath = CrowModelPath,                    height = 0.35f, offsetFromPlayer = new Vector3(4f, 0f, 16f) },
            new NpcDef { objectName = "Tortuga",      displayName = "Tortuga",      questId = "Q5", role = NPCRole.LostPet, companionType = CompanionAI.CompanionType.Mounted,       modelPath = TortoiseModelPath,                height = 0.30f, offsetFromPlayer = new Vector3(16f, 0f, 6f) },
        };

        private static readonly NpcDef[] Owners =
        {
            new NpcDef { objectName = "Madre",        displayName = "Madre",        questId = "Q1", role = NPCRole.Owner, modelPath = HumanModelPath, height = 1.70f, offsetFromPlayer = new Vector3(6f, 0f, 4f) },
            new NpcDef { objectName = "Anciana",      displayName = "Anciana",      questId = "Q2", role = NPCRole.Owner, modelPath = HumanModelPath, height = 1.55f, offsetFromPlayer = new Vector3(-6f, 0f, 4f) },
            new NpcDef { objectName = "Humano Ciego", displayName = "Humano Ciego", questId = "Q3", role = NPCRole.Owner, modelPath = HumanModelPath, height = 1.75f, offsetFromPlayer = new Vector3(0f, 0f, 8f) },
            new NpcDef { objectName = "Dueño Cuervo", displayName = "Cachorro",     questId = "Q4", role = NPCRole.Owner, modelPath = HumanModelPath, height = 1.20f, offsetFromPlayer = new Vector3(8f, 0f, -4f) },
            new NpcDef { objectName = "Mujer",        displayName = "Mujer",        questId = "Q5", role = NPCRole.Owner, modelPath = HumanModelPath, height = 1.70f, offsetFromPlayer = new Vector3(-8f, 0f, -4f) },
        };

        // ───────────────────────────── MENÚS ─────────────────────────────

        [MenuItem("Tools/Réplica/Configuración Automática de Escena", false, 0)]
        public static void SetupScene()
        {
            if (!EnsureTMPEssentials()) return;

            // 1. Misiones
            QuestDataGenerator.GenerateDefaultQuests();
            var quests = LoadQuests();

            // 3a. Colliders del entorno (los assets de la ciudad no traen colliders: sin esto el perro cae al vacío)
            int colliders = AddEnvironmentCollidersInternal();

            // 2. Jugador + cámara
            GameObject player = SetupPlayer();
            Camera cam = SetupCamera(player);
            var pcSo = new SerializedObject(player.GetComponent<PlayerController>());
            pcSo.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
            pcSo.ApplyModifiedProperties();

            // 4. NPCs y mascotas
            var companions = new Dictionary<string, CompanionAI>();
            foreach (var def in Pets)
            {
                SetupNpc(def, player.transform, quests, null, out var companion);
                companions[def.questId] = companion;
            }
            foreach (var def in Owners)
            {
                companions.TryGetValue(def.questId, out var linked);
                SetupNpc(def, player.transform, quests, linked, out _);
            }

            // 5. UI
            SetupUI();

            // 6. Gestores
            SetupManagers(quests, cam);

            // 3b. NavMesh
            BakeNavMeshInternal(false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = player;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

            EditorUtility.DisplayDialog("Réplica - Escena configurada",
                $"Listo. Se agregaron {colliders} colliders al entorno y se está horneando el NavMesh.\n\n" +
                "1. Guarda la escena (Ctrl+S).\n" +
                "2. Presiona Play.\n\n" +
                "Controles:\n• WASD / Flechas: mover\n• Shift: correr\n• Mouse: girar cámara (Esc libera el cursor)\n• Rueda: zoom\n• E: hablar / continuar diálogo\n\n" +
                "Al iniciar aparece el prólogo: presiona E para avanzarlo (mientras hay diálogo el perro no se mueve).\n\n" +
                "Mueve los NPCs a donde quieras en el mapa y vuelve a usar Tools > Réplica > Hornear NavMesh.",
                "OK");
        }

        [MenuItem("Tools/Réplica/Hornear NavMesh (Bake)", false, 20)]
        public static void BakeNavMesh()
        {
            BakeNavMeshInternal(true);
        }

        [MenuItem("Tools/Réplica/Agregar Colliders al Entorno", false, 21)]
        public static void AddEnvironmentColliders()
        {
            int count = AddEnvironmentCollidersInternal();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[AutoSceneBuilder] Se agregaron {count} MeshColliders al entorno.");
        }

        // ───────────────────────────── TMP ─────────────────────────────

        private static bool EnsureTMPEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return true;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            EditorUtility.DisplayDialog("Réplica",
                "Se están importando los recursos esenciales de TextMesh Pro (necesarios para los textos de diálogo y HUD).\n\n" +
                "Cuando termine la importación, vuelve a ejecutar:\nTools > Réplica > Configuración Automática de Escena", "OK");
            return false;
        }

        // ───────────────────────────── MISIONES ─────────────────────────────

        private static Dictionary<string, QuestData> LoadQuests()
        {
            var result = new Dictionary<string, QuestData>(System.StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("t:QuestData"))
            {
                var q = AssetDatabase.LoadAssetAtPath<QuestData>(AssetDatabase.GUIDToAssetPath(guid));
                if (q != null && !string.IsNullOrEmpty(q.questId))
                    result[q.questId] = q;
            }
            return result;
        }

        // ───────────────────────────── JUGADOR ─────────────────────────────

        private static GameObject SetupPlayer()
        {
            var existing = Object.FindFirstObjectByType<PlayerController>();
            GameObject player = existing != null ? existing.gameObject : GameObject.Find("Player");
            bool created = player == null;
            if (created) player = new GameObject("Player");

            player.tag = "Player";

            // Colocar sobre el suelo, donde estás mirando en la Scene View
            if (created || player.transform.position == Vector3.zero)
            {
                Vector3 focus = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
                if (TryGroundPoint(focus, 100f, out Vector3 ground))
                    player.transform.position = ground + Vector3.up * 0.05f;
            }

            // Modelo del perro (corgi del kit de perros animados)
            GameObject model = EnsureModel(player, PlayerModelPath, 0.55f);

            // CharacterController (según la guía)
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.height = 0.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.4f, 0f);
            cc.skinWidth = 0.03f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0f;

            // Puntos de montura
            Transform mountBack = EnsureChild(player.transform, "Mount_Back", new Vector3(0f, 0.5f, -0.05f));
            Transform mountHead = EnsureChild(player.transform, "Mount_Head", new Vector3(0f, 0.7f, 0.3f));

            var pc = player.GetComponent<PlayerController>();
            if (pc == null) pc = player.AddComponent<PlayerController>();
            pc.mountPointBack = mountBack;
            pc.mountPointHead = mountHead;

            var so = new SerializedObject(pc);
            so.FindProperty("walkSpeed").floatValue = 3.5f;
            so.FindProperty("runSpeed").floatValue = 7.0f;
            Animator anim = model != null ? model.GetComponentInChildren<Animator>() : player.GetComponentInChildren<Animator>();
            if (anim != null) so.FindProperty("animator").objectReferenceValue = anim;
            so.ApplyModifiedProperties();

            // Interacción
            var im = player.GetComponent<InteractionManager>();
            if (im == null) im = player.AddComponent<InteractionManager>();
            var imSo = new SerializedObject(im);
            imSo.FindProperty("detectionRadius").floatValue = 2.5f;
            imSo.ApplyModifiedProperties();

            var sphere = player.GetComponent<SphereCollider>();
            if (sphere == null) sphere = player.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 2.5f;
            sphere.center = new Vector3(0f, 0.4f, 0f);

            EnsureIgnoredByNavMesh(player);
            EditorUtility.SetDirty(player);
            return player;
        }

        // ───────────────────────────── CÁMARA ─────────────────────────────

        private static Camera SetupCamera(GameObject player)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.gameObject.tag = "MainCamera";
            cam.nearClipPlane = 0.05f;

            // Desactivar otros scripts de cámara (ej. cámaras libres de las escenas demo) que pelearían con el seguimiento
            foreach (var mb in cam.GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb is ThirdPersonCamera) continue;
                string ns = mb.GetType().Namespace ?? string.Empty;
                if (ns.StartsWith("UnityEngine") || ns.StartsWith("Unity.")) continue;
                mb.enabled = false;
                Debug.Log($"[AutoSceneBuilder] Se desactivó el script '{mb.GetType().Name}' de la cámara para que no interfiera con ThirdPersonCamera.");
            }

            var tpc = cam.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;

            // Posición inicial detrás del perro
            Transform p = player.transform;
            cam.transform.position = p.position - p.forward * 4f + Vector3.up * 2f;
            cam.transform.LookAt(p.position + Vector3.up * 0.6f);

            EditorUtility.SetDirty(cam.gameObject);
            return cam;
        }

        // ───────────────────────────── NPCs ─────────────────────────────

        private static GameObject SetupNpc(NpcDef def, Transform player, Dictionary<string, QuestData> quests,
            CompanionAI linkedCompanion, out CompanionAI companion)
        {
            GameObject npc = GameObject.Find(def.objectName);
            bool created = npc == null;
            if (created) npc = new GameObject(def.objectName);

            if (created || npc.transform.position == Vector3.zero)
            {
                Vector3 target = player.position + def.offsetFromPlayer;
                npc.transform.position = TryGroundPoint(target, 3f, out Vector3 ground) ? ground : target;
                Vector3 look = player.position - npc.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) npc.transform.rotation = Quaternion.LookRotation(look);
            }

            EnsureModel(npc, def.modelPath, def.height);

            // Collider trigger para que el InteractionManager lo detecte (OverlapSphere)
            var capsule = npc.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = npc.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.height = Mathf.Max(def.height, 0.6f);
            capsule.radius = Mathf.Clamp(def.height * 0.35f, 0.25f, 0.45f);
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

            companion = null;
            if (def.role == NPCRole.LostPet)
            {
                var agent = npc.GetComponent<NavMeshAgent>();
                if (agent == null) agent = npc.AddComponent<NavMeshAgent>();
                agent.radius = 0.25f;
                agent.height = Mathf.Max(def.height, 0.3f);
                agent.baseOffset = 0f;

                companion = npc.GetComponent<CompanionAI>();
                if (companion == null) companion = npc.AddComponent<CompanionAI>();
                var aiSo = new SerializedObject(companion);
                aiSo.FindProperty("companionType").enumValueIndex = (int)def.companionType;
                aiSo.ApplyModifiedProperties();
            }
            else
            {
                EnsureIgnoredByNavMesh(npc);
            }

            var interactable = npc.GetComponent<InteractableNPC>();
            if (interactable == null) interactable = npc.AddComponent<InteractableNPC>();

            var so = new SerializedObject(interactable);
            so.FindProperty("role").enumValueIndex = (int)def.role;
            so.FindProperty("questId").stringValue = def.questId;
            so.FindProperty("npcDisplayName").stringValue = def.displayName;
            if (quests.TryGetValue(def.questId, out var qd))
                so.FindProperty("questData").objectReferenceValue = qd;
            so.FindProperty("companionAI").objectReferenceValue = def.role == NPCRole.LostPet ? companion : linkedCompanion;

            if (def.role == NPCRole.Owner)
            {
                Transform dp = EnsureChild(npc.transform, "DeliveryPoint", new Vector3(1.2f, 0f, 0.8f));
                so.FindProperty("deliveryDestinationPoint").objectReferenceValue = dp;
            }
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(npc);
            return npc;
        }

        // ───────────────────────────── UI ─────────────────────────────

        private static void SetupUI()
        {
            // Canvas principal
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null) canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (canvasGo.GetComponent<GraphicRaycaster>() == null) canvasGo.AddComponent<GraphicRaycaster>();

            // ── Diálogo ──
            RectTransform dmRect = GetOrCreateRect("DialogueManager", canvasGo.transform);
            Stretch(dmRect, Vector2.zero, Vector2.one);
            var ds = dmRect.GetComponent<DialogueSystem>();
            if (ds == null) ds = dmRect.gameObject.AddComponent<DialogueSystem>();

            RectTransform panel = GetOrCreateRect("DialoguePanel", dmRect);
            Stretch(panel, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.27f));
            var panelImg = GetOrAdd<Image>(panel.gameObject);
            panelImg.color = new Color(0f, 0f, 0f, 0.78f);
            panelImg.raycastTarget = false;

            var speaker = GetOrCreateText("SpeakerName", panel, "Narrador", 34, TextAlignmentOptions.TopLeft, new Color(1f, 0.85f, 0.4f), FontStyles.Bold);
            Stretch(speaker.rectTransform, new Vector2(0.03f, 0.70f), new Vector2(0.97f, 0.95f));
            var content = GetOrCreateText("DialogueText", panel, "Texto del diálogo...", 30, TextAlignmentOptions.TopLeft, Color.white, FontStyles.Normal);
            Stretch(content.rectTransform, new Vector2(0.03f, 0.15f), new Vector2(0.97f, 0.70f));
            var cont = GetOrCreateText("ContinueIndicator", panel, "[E] Continuar", 22, TextAlignmentOptions.BottomRight, new Color(1f, 1f, 1f, 0.8f), FontStyles.Italic);
            Stretch(cont.rectTransform, new Vector2(0.70f, 0.03f), new Vector2(0.98f, 0.18f));

            var dsSo = new SerializedObject(ds);
            dsSo.FindProperty("dialoguePanel").objectReferenceValue = panel.gameObject;
            dsSo.FindProperty("speakerNameText").objectReferenceValue = speaker;
            dsSo.FindProperty("dialogueContentText").objectReferenceValue = content;
            dsSo.FindProperty("continueIndicator").objectReferenceValue = cont.gameObject;
            dsSo.FindProperty("typingSpeed").floatValue = 0.02f;
            dsSo.ApplyModifiedProperties();
            panel.gameObject.SetActive(false);

            // ── HUD ──
            RectTransform hudRect = GetOrCreateRect("HUD", canvasGo.transform);
            Stretch(hudRect, Vector2.zero, Vector2.one);
            var hud = GetOrAdd<GameHUD>(hudRect.gameObject);

            var counter = GetOrCreateText("QuestCounter", hudRect, "Rescates: 0 / 5", 32, TextAlignmentOptions.TopLeft, Color.white, FontStyles.Bold);
            Stretch(counter.rectTransform, new Vector2(0.02f, 0.90f), new Vector2(0.40f, 0.98f));

            var controls = GetOrCreateText("ControlsHint", hudRect, "WASD: mover  |  Shift: correr  |  Mouse: cámara  |  E: hablar", 18, TextAlignmentOptions.TopRight, new Color(1f, 1f, 1f, 0.7f), FontStyles.Normal);
            Stretch(controls.rectTransform, new Vector2(0.55f, 0.94f), new Vector2(0.98f, 0.99f));

            RectTransform promptPanel = GetOrCreateRect("InteractionPromptPanel", hudRect);
            Stretch(promptPanel, new Vector2(0.36f, 0.30f), new Vector2(0.64f, 0.36f));
            var promptImg = GetOrAdd<Image>(promptPanel.gameObject);
            promptImg.color = new Color(0f, 0f, 0f, 0.6f);
            promptImg.raycastTarget = false;
            var promptText = GetOrCreateText("InteractionPromptText", promptPanel, "[E] Hablar", 26, TextAlignmentOptions.Center, Color.white, FontStyles.Normal);
            Stretch(promptText.rectTransform, Vector2.zero, Vector2.one);

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("interactionPromptText").objectReferenceValue = promptText;
            hudSo.FindProperty("interactionPromptPanel").objectReferenceValue = promptPanel.gameObject;
            hudSo.FindProperty("questCounterText").objectReferenceValue = counter;
            hudSo.ApplyModifiedProperties();
            promptPanel.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvasGo);
        }

        // ───────────────────────────── GESTORES ─────────────────────────────

        private static void SetupManagers(Dictionary<string, QuestData> quests, Camera gameplayCam)
        {
            // FinalSequenceManager
            GameObject finalGo = GameObject.Find("FinalSequenceManager");
            if (finalGo == null) finalGo = new GameObject("FinalSequenceManager");
            var fsc = GetOrAdd<FinalSequenceController>(finalGo);

            // Cámara cinemática (desactivada hasta la secuencia final)
            Transform cineT = finalGo.transform.Find("CinematicCamera");
            GameObject cine;
            if (cineT == null)
            {
                cine = new GameObject("CinematicCamera");
                cine.transform.SetParent(finalGo.transform, false);
                cine.AddComponent<Camera>();
                cine.transform.SetPositionAndRotation(gameplayCam.transform.position + Vector3.up * 3f, gameplayCam.transform.rotation);
            }
            else cine = cineT.gameObject;
            cine.SetActive(false);

            // Canvas de fin de juego
            GameObject endGo = GameObject.Find("EndScreenCanvas");
            if (endGo == null)
            {
                endGo = new GameObject("EndScreenCanvas", typeof(RectTransform));
                endGo.layer = LayerMask.NameToLayer("UI");
                var c = endGo.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 10;
                var sc = endGo.AddComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1920f, 1080f);

                RectTransform bg = GetOrCreateRect("Background", endGo.transform);
                Stretch(bg, Vector2.zero, Vector2.one);
                GetOrAdd<Image>(bg.gameObject).color = Color.black;
                var title = GetOrCreateText("Title", bg, "Réplica\n\nGracias por jugar", 64, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
                Stretch(title.rectTransform, Vector2.zero, Vector2.one);
            }
            endGo.SetActive(false);

            var fscSo = new SerializedObject(fsc);
            fscSo.FindProperty("gameplayCamera").objectReferenceValue = gameplayCam.gameObject;
            fscSo.FindProperty("cinematicCamera").objectReferenceValue = cine;
            fscSo.FindProperty("endScreenCanvas").objectReferenceValue = endGo;
            fscSo.ApplyModifiedProperties();

            // GameManager + QuestManager
            GameObject gm = GameObject.Find("GameManager");
            if (gm == null) gm = new GameObject("GameManager");
            var qm = GetOrAdd<QuestManager>(gm);
            var qmSo = new SerializedObject(qm);
            qmSo.FindProperty("playIntroOnStart").boolValue = true;
            var list = qmSo.FindProperty("quests");
            string[] ids = { "Q1", "Q2", "Q3", "Q4", "Q5" };
            list.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = quests.TryGetValue(ids[i], out var q) ? q : null;
            qmSo.FindProperty("finalSequenceController").objectReferenceValue = fsc;
            qmSo.ApplyModifiedProperties();
        }

        // ───────────────────────────── NAVMESH ─────────────────────────────

        private static void BakeNavMeshInternal(bool selectSurface)
        {
            AddEnvironmentCollidersInternal();

            var surface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                var go = new GameObject("NavMesh");
                surface = go.AddComponent<NavMeshSurface>();
            }
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;

            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) EnsureIgnoredByNavMesh(player.gameObject);
            foreach (var npc in Object.FindObjectsByType<InteractableNPC>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                EnsureIgnoredByNavMesh(npc.gameObject);

            Physics.SyncTransforms();
            NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { surface });

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (selectSurface) Selection.activeGameObject = surface.gameObject;
            Debug.Log("[AutoSceneBuilder] Horneando NavMesh... (objeto 'NavMesh' con NavMeshSurface). Guarda la escena al terminar.");
        }

        private static void EnsureIgnoredByNavMesh(GameObject go)
        {
            var mod = go.GetComponent<NavMeshModifier>();
            if (mod == null) mod = go.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;
            mod.applyToChildren = true;
        }

        // ───────────────────────────── COLLIDERS DEL ENTORNO ─────────────────────────────

        private static int AddEnvironmentCollidersInternal()
        {
            int count = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (mf.sharedMesh == null) continue;
                GameObject go = mf.gameObject;
                if (go.GetComponent<MeshRenderer>() == null) continue;
                if (go.GetComponent<Collider>() != null) continue;
                if (IsCharacterOrUI(go.transform)) continue;

                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                count++;
            }
            if (count > 0) Physics.SyncTransforms();
            return count;
        }

        private static bool IsCharacterOrUI(Transform t)
        {
            return t.GetComponentInParent<PlayerController>(true) != null
                || t.GetComponentInParent<InteractableNPC>(true) != null
                || t.GetComponentInParent<CompanionAI>(true) != null
                || t.GetComponentInParent<Canvas>(true) != null
                || t.GetComponentInParent<Camera>(true) != null
                || t.GetComponentInParent<Animator>(true) != null;
        }

        // ───────────────────────────── UTILIDADES ─────────────────────────────

        private static bool TryGroundPoint(Vector3 around, float startHeight, out Vector3 point)
        {
            Physics.SyncTransforms();
            var origin = new Vector3(around.x, around.y + startHeight, around.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, startHeight + 200f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (IsCharacterOrUI(h.collider.transform)) continue;
                point = h.point;
                return true;
            }
            point = around;
            return false;
        }

        private static GameObject EnsureModel(GameObject root, string prefabPath, float targetHeight)
        {
            Transform existing = root.transform.Find(ModelChildName);
            if (existing != null) return existing.gameObject;
            if (root.GetComponentInChildren<Renderer>() != null) return null; // ya tiene un modelo propio

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[AutoSceneBuilder] No se encontró el modelo '{prefabPath}' para '{root.name}'. Arrastra un modelo como hijo manualmente.");
                return null;
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.scene);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = ModelChildName;
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // Quitar scripts/física del modelo para que no interfieran con nuestros componentes
            foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            foreach (var col in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);

            NormalizeHeight(root.transform, model, targetHeight);
            return model;
        }

        private static void NormalizeHeight(Transform root, GameObject model, float targetHeight)
        {
            if (!TryGetBounds(model, out Bounds b) || b.size.y < 0.0001f) return;
            model.transform.localScale *= targetHeight / b.size.y;
            if (TryGetBounds(model, out b))
                model.transform.position += Vector3.up * (root.position.y - b.min.y); // apoyar en el suelo
        }

        private static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }

        private static Transform EnsureChild(Transform parent, string name, Vector3 localPos)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(parent, false);
                t.localPosition = localPos;
            }
            return t;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static RectTransform GetOrCreateRect(string name, Transform parent)
        {
            Transform t = parent.Find(name);
            GameObject go;
            if (t != null) go = t.gameObject;
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI GetOrCreateText(string name, Transform parent, string text, float size,
            TextAlignmentOptions alignment, Color color, FontStyles style)
        {
            RectTransform rt = GetOrCreateRect(name, parent);
            var tmp = GetOrAdd<TextMeshProUGUI>(rt.gameObject);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
#endif
