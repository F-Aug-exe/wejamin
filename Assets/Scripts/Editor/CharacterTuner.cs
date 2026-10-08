using UnityEditor;
using UnityEngine;

namespace Replica.Editor
{
    public static class CharacterTuner
    {
        [MenuItem("Tools/Réplica/Tunear Personajes (Color y Ropa)", false, 5)]
        public static void TuneAllCharacters()
        {
            var characters = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int tuned = 0;

            foreach (var t in characters)
            {
                if (t.name == "Madre" || t.name == "Anciana" || t.name == "Humano Ciego" ||
                    t.name.Contains("Dueño Cuervo") || t.name == "Mujer" || t.name == "Niño" ||
                    t.name == "DuenoFalso_PuntoA" || t.name == "DuenoToby_PuntoB")
                {
                    TuneCharacter(t.gameObject);
                    tuned++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[CharacterTuner] Se tunearon exitosamente {tuned} personajes con accesorios, ropa y variantes visuales.");
        }

        public static void TuneCharacter(GameObject character)
        {
            Transform baseMesh = character.transform.Find("Model/Base_Mesh");
            if (baseMesh == null)
            {
                foreach (var t in character.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Base_Mesh") { baseMesh = t; break; }
                }
            }
            if (baseMesh == null) return;

            string n = character.name;

            bool hat = false;
            bool mustache = false;
            bool outerwear = false;
            bool tshirt = false;
            bool glasses = false;
            bool accessories = false;

            if (n.Contains("Madre"))
            {
                outerwear = true;
                accessories = true;
            }
            else if (n.Contains("Anciana"))
            {
                outerwear = true;
                glasses = true;
            }
            else if (n.Contains("Ciego"))
            {
                tshirt = true;
                glasses = true;
            }
            else if (n.Contains("Cuervo") || n.Contains("Dueño"))
            {
                hat = true;
                mustache = true;
                outerwear = true;
            }
            else if (n.Contains("Mujer"))
            {
                tshirt = true;
                accessories = true;
            }
            else if (n.Contains("Niño") || n.Contains("Nino"))
            {
                hat = true;
                tshirt = true;
            }
            else if (n.Contains("DuenoFalso"))
            {
                hat = true;
                mustache = true;
                outerwear = true;
            }
            else if (n.Contains("DuenoToby"))
            {
                tshirt = true;
                glasses = true;
            }

            foreach (Transform child in baseMesh)
            {
                string part = child.name;
                if (part == "Skeleton" || part == "Body" || part == "Faces" || part == "Pants" || part == "Shoes" || part == "Hairstyle")
                {
                    child.gameObject.SetActive(true);
                }
                else if (part == "Full_body" || part == "Gloves")
                {
                    child.gameObject.SetActive(false);
                }
                else if (part == "Accessories") child.gameObject.SetActive(accessories);
                else if (part == "Glasses") child.gameObject.SetActive(glasses);
                else if (part == "Hat") child.gameObject.SetActive(hat);
                else if (part == "Mustache") child.gameObject.SetActive(mustache);
                else if (part == "Outerwear") child.gameObject.SetActive(outerwear);
                else if (part == "T_Shirt") child.gameObject.SetActive(tshirt);
            }
        }
    }
}
