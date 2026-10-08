using System.IO;
using UnityEditor;
using UnityEngine;
using Replica.Quests;

namespace Replica.EditorTools
{
    public static class QuestDataGenerator
    {
        private const string FolderPath = "Assets/Data/Quests";

        [MenuItem("Tools/Rplica/Generar Misiones Predefinidas")]
        public static void GenerateDefaultQuests()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            CreateOrUpdateQuest("Q1", "Madre y Nino",
                "Madre", new[] { "Mi nino justo estaba saliendo de clase pero aun no llega.", "Me ayudas a buscarlo? (Presiona [E] para Aceptar Mision)" },
                "Nino", new[] { "Nos dijeron que esperemos a que nos vengan a buscar. Voy contigo?" },
                "Madre", new[] { "Gracias perrito por traer a mi hijo." },
                "Nino", new[] { "Mami!" }
            );

            CreateOrUpdateQuest("Q2", "Anciana y Cachetoncito",
                "Anciana", new[] { "Mi cachetoncitoo, viste a mi cachetoncitoo?", "Me ayudas a encontrarlo? (Presiona [E] para Aceptar Mision)" },
                "Cachetoncito", new[] { "Uy loco mira que esta vieja se asusto y yo me abri, yo me parcho con usted." },
                "Anciana", new[] { "Cachetoncitoo!" },
                "Cachetoncito", new[] { "Noooooo!" }
            );

            CreateOrUpdateQuest("Q3", "Ciego y Perro Guia",
                "Humano Ciego", new[] { "Mi perro de asistencia, debe haberse asustado con el temblor...", "Me ayudas a buscarlo? (Presiona [E] para Aceptar Mision)" },
                "Perro Gua", new[] { "Oye, viste a un humano ciego? Se me perdio..." },
                "Humano Ciego", new[] { "Amigo mo, estas aqu! Gracias, perrito." },
                "Perro Gua", new[] { "Sniff- Guau!" }
            );

            CreateOrUpdateQuest("Q4", "Dueno y Cuervo",
                "Dueno Cuervo", new[] { "Vieron un cuervo que habla mucho?", "Me ayudas a encontrarlo? (Presiona [E] para Aceptar Mision)" },
                "Cuervo", new[] { "Hola canino, se me perdio un humano..." },
                "Dueno Cuervo", new[] { "Chico! Te me habas perdido." },
                "Cuervo", new[] { "Caw! Aqu estoy!" }
            );

            CreateOrUpdateQuest("Q5", "Mujer y Tortuga",
                "Mujer", new[] { "Saque a pasear a mi tortuga y ya no la veo...", "Me ayudas a encontrarla? (Presiona [E] para Aceptar Mision)" },
                "Tortuga", new[] { "Amigo, esa vieja me pateo y quede as. Me puedes llevar en tu lomo?" },
                "Mujer", new[] { "Mira donde estabas, tortuguita." },
                "Tortuga", new[] { "Tetranutria..." }
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuestDataGenerator] Las 5 misiones fueron generadas exitosamente en " + FolderPath + "!");
        }

        private static DialogueLine[] ToLines(string[] strings) {
            var lines = new DialogueLine[strings.Length];
            for (int i=0; i<strings.Length; i++) {
                lines[i] = new DialogueLine { text = strings[i] };
            }
            return lines;
        }

        private static void CreateOrUpdateQuest(
            string questId,
            string title,
            string ownerSpeaker, string[] ownerLines,
            string petSpeaker, string[] petLines,
            string ownerDeliveredSpeaker, string[] ownerDeliveredLines,
            string petDeliveredSpeaker, string[] petDeliveredLines)
        {
            string assetPath = $"{FolderPath}/{questId}_{title.Replace(" ", "_")}.asset";
            var quest = AssetDatabase.LoadAssetAtPath<QuestData>(assetPath);

            if (quest == null)
            {
                quest = ScriptableObject.CreateInstance<QuestData>();
                AssetDatabase.CreateAsset(quest, assetPath);
            }

            quest.questId = questId;
            quest.questTitle = title;
            quest.ownerInitialDialogue = new DialogueData { speakerName = ownerSpeaker, lines = ToLines(ownerLines) };
            quest.petFoundDialogue = new DialogueData { speakerName = petSpeaker, lines = ToLines(petLines) };
            quest.ownerDeliveredDialogue = new DialogueData { speakerName = ownerDeliveredSpeaker, lines = ToLines(ownerDeliveredLines) };
            quest.petDeliveredDialogue = new DialogueData { speakerName = petDeliveredSpeaker, lines = ToLines(petDeliveredLines) };

            EditorUtility.SetDirty(quest);
        }
    }
}
