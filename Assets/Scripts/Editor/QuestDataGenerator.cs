#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using Replica.Quests;

namespace Replica.EditorTools
{
    public static class QuestDataGenerator
    {
        private const string FolderPath = "Assets/Data/Quests";

        [MenuItem("Tools/Réplica/Generar Misiones Predefinidas")]
        public static void GenerateDefaultQuests()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            // Q1: Madre y Cachorro
            CreateOrUpdateQuest("Q1", "Madre y Cachorro",
                "Madre", new[] { "El cachorro mío justo ta saliendo de clase pero aún no llega." },
                "Cachorro", new[] { "Nos dijeron que esperemos a que nos vengan a buscar. ¿Voy contigo?" },
                "Madre", new[] { "Gracias perrito." },
                "Cachorro", new[] { "¡Mami!" }
            );

            // Q2: Anciana y Cachetoncito
            CreateOrUpdateQuest("Q2", "Anciana y Cachetoncito",
                "Anciana", new[] { "Mi cachetoncitoo, ¿viste a mi cachetoncitoo?" },
                "Cachetoncito", new[] { "Uy loco mirá que está vieja se asustó y yo me abrí, yo me parcho con usted." },
                "Anciana", new[] { "¡Cachetoncitoo!" },
                "Cachetoncito", new[] { "¡Noooooo!" }
            );

            // Q3: Ciego y Perro de Asistencia
            CreateOrUpdateQuest("Q3", "Ciego y Perro Guía",
                "Humano Ciego", new[] { "Mi perro de asistencia, debe haberse asustado con el temblor..." },
                "Perro Guía", new[] { "Oye, ¿viste a un humano ciego? Se me perdió..." },
                "Humano Ciego", new[] { "¡Amigo mío, estás aquí! Gracias, perrito." },
                "Perro Guía", new[] { "Sniff- ¡Guau!" }
            );

            // Q4: Cachorro y Cuervo
            CreateOrUpdateQuest("Q4", "Cachorro y Cuervo",
                "Cachorro", new[] { "¿Vieron un cuervo que habla mucho?" },
                "Cuervo", new[] { "Hola canino, se me perdió un humano..." },
                "Cachorro", new[] { "¡Chico! Te me habías perdido." },
                "Cuervo", new[] { "¡Caw! ¡Aquí estoy!" }
            );

            // Q5: Mujer y Tortuga
            CreateOrUpdateQuest("Q5", "Mujer y Tortuga",
                "Mujer", new[] { "Saqué a pasear a mi tortuga y ya no la veo..." },
                "Tortuga", new[] { "Amigo, esa vieja me pateó y quedé así. ¿Me puedes llevar en tu lomo?" },
                "Mujer", new[] { "Mira dónde estabas, tortuguita." },
                "Tortuga", new[] { "Tetranutria..." }
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuestDataGenerator] ¡Las 5 misiones fueron generadas exitosamente en " + FolderPath + "!");
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

            quest.ownerInitialDialogue = new DialogueData { speakerName = ownerSpeaker, lines = ownerLines };
            quest.petFoundDialogue = new DialogueData { speakerName = petSpeaker, lines = petLines };
            quest.ownerDeliveredDialogue = new DialogueData { speakerName = ownerDeliveredSpeaker, lines = ownerDeliveredLines };
            quest.petDeliveredDialogue = new DialogueData { speakerName = petDeliveredSpeaker, lines = petDeliveredLines };

            EditorUtility.SetDirty(quest);
        }
    }
}
#endif
