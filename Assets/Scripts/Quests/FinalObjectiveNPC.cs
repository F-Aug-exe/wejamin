using UnityEngine;
using Replica.Interaction;

namespace Replica.Quests
{
    public class FinalObjectiveNPC : MonoBehaviour, IInteractable
    {
        public enum ObjectiveType
        {
            FalseOwner,
            RealOwner
        }

        [SerializeField] private ObjectiveType objectiveType = ObjectiveType.FalseOwner;
        [SerializeField] private FinalSequenceController finalSequenceController;
        [SerializeField] private string promptText = "[E] Hablar";

        private bool interacted = false;

        private void Start()
        {
            if (finalSequenceController == null)
            {
                finalSequenceController = Object.FindAnyObjectByType<FinalSequenceController>();
            }
        }

        public string GetInteractionPrompt()
        {
            return CanInteract() ? promptText : "";
        }

        public bool CanInteract()
        {
            return !interacted && gameObject.activeInHierarchy;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract()) return;
            interacted = true;

            Vector3 look = (interactor.transform.position - transform.position).normalized;
            look.y = 0;
            if (look.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(look);
            }

            if (finalSequenceController == null)
            {
                finalSequenceController = Object.FindAnyObjectByType<FinalSequenceController>();
            }

            if (finalSequenceController != null)
            {
                if (objectiveType == ObjectiveType.FalseOwner)
                {
                    finalSequenceController.OnFalseOwnerInteracted();
                }
                else
                {
                    finalSequenceController.OnRealOwnerInteracted();
                }
            }
        }
    }
}
