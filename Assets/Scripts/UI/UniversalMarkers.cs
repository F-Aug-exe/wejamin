using UnityEngine;
using UnityEngine.UI;
using Replica.Interaction;
using Replica.Quests;

namespace Replica.UI {
    public class UniversalMarkers : MonoBehaviour {
        [Header("Configuracion")]
        public Color petColor = Color.green;
        public Color ownerColor = Color.blue;
        public Color unacceptedColor = Color.yellow;
        
        [Header("UI Prefab")]
        public GameObject markerPrefab;

        private InteractableNPC[] npcs;
        private RectTransform canvasRect;

        private class MarkerInstance {
            public GameObject go;
            public Image img;
            public RectTransform rt;
        }
        private System.Collections.Generic.Dictionary<InteractableNPC, MarkerInstance> markers = new System.Collections.Generic.Dictionary<InteractableNPC, MarkerInstance>();

        void Start() {
            npcs = FindObjectsByType<InteractableNPC>(FindObjectsInactive.Exclude);
            canvasRect = GetComponent<RectTransform>();
            
            if (markerPrefab == null) {
                var go = new GameObject("DefaultMarker", typeof(RectTransform), typeof(Image));
                var img = go.GetComponent<Image>();
                markerPrefab = go;
                markerPrefab.SetActive(false);
            }
        }

        void Update() {
            if (Object.FindAnyObjectByType<QuestManager>() == null || Camera.main == null) return;
            
            foreach (var npc in npcs) {
                if (npc == null) continue;
                
                var state = Object.FindAnyObjectByType<QuestManager>().GetQuestState(npc.QuestId);
                bool shouldShow = false;
                Color mColor = Color.white;

                if (npc.Role == NPCRole.Owner) {
                    if (state == QuestState.NotStarted) {
                        shouldShow = true;
                        mColor = unacceptedColor;
                    } else if (state == QuestState.CompanionRecruited) {
                        shouldShow = true;
                        mColor = ownerColor;
                    }
                } else { // Pet
                    if (state == QuestState.InProgress) {
                        shouldShow = true;
                        mColor = petColor;
                    }
                }

                if (!shouldShow) {
                    if (markers.ContainsKey(npc)) {
                        markers[npc].go.SetActive(false);
                    }
                    continue;
                }

                if (!markers.ContainsKey(npc)) {
                    var go = Instantiate(markerPrefab, transform);
                    var inst = new MarkerInstance { go = go, rt = go.GetComponent<RectTransform>(), img = go.GetComponent<Image>() };
                    inst.rt.sizeDelta = new Vector2(40, 40);
                    markers[npc] = inst;
                }

                var marker = markers[npc];

                Vector3 screenPos = Camera.main.WorldToScreenPoint(npc.transform.position + Vector3.up * 2f);
                
                // Si la camara no lo esta mirando (z < 0), lo ocultamos. 
                // Al estar en ScreenSpaceOverlay, siempre atraviesa paredes.
                if (screenPos.z < 0) {
                    marker.go.SetActive(false);
                } else {
                    marker.go.SetActive(true);
                    marker.img.color = mColor;
                    marker.rt.position = new Vector3(screenPos.x, screenPos.y, 0);
                    marker.rt.rotation = Quaternion.Euler(0, 0, 45);
                }
            }
        }
    }
}

