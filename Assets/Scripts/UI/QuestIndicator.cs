using UnityEngine;
using Replica.Interaction;

namespace Replica.UI {
    public class QuestIndicator : MonoBehaviour {
        private InteractableNPC npc;
        private Transform diamond;
        
        void Start() {
            npc = GetComponentInParent<InteractableNPC>();
            
            // Create diamond visual
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            diamond = go.transform;
            diamond.SetParent(transform, false);
            diamond.localScale = Vector3.one * 0.3f;
            diamond.localPosition = Vector3.up * 2.2f;
            
            // Yellow color
            var rnd = go.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.yellow;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.yellow * 0.5f);
            rnd.sharedMaterial = mat;
        }

        void Update() {
            if (diamond != null) {
                diamond.Rotate(Vector3.up * (90f * Time.deltaTime), Space.World);
                float bob = Mathf.Sin(Time.time * 3f) * 0.1f;
                diamond.localPosition = new Vector3(0, 2.2f + bob, 0);

                // Hide if quest is accepted/in progress
                if (npc != null && Replica.Quests.QuestManager.Instance != null) {
                    var status = Replica.Quests.QuestManager.Instance.GetQuestState(npc.QuestId);
                    diamond.gameObject.SetActive(status == Replica.Quests.QuestState.NotStarted && npc.Role == NPCRole.Owner);
                }
            }
        }
    }
}
