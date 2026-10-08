using System.IO;
using UnityEditor;
using UnityEngine;
using Replica.Quests;

namespace Replica.EditorTools
{
    public static class QuestDataGenerator
    {
        private const string FolderPath = "Assets/Data/Quests";
        private const string AudioFolder = "Assets/Audio/Dialogues";

        [MenuItem("Tools/Réplica/Generar Misiones Predefinidas")]
        public static void GenerateDefaultQuests()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            // Q1: Madre y Niño
            CreateOrUpdateQuest("Q1", "Madre y Nino",
                "Madre", new[]
                {
                    ("El hijo mio esta saliendo de clase...", LoadAudio("VO_Q1_Madre_Initial_01.wav")),
                    ("Pero aun no llega", LoadAudio("VO_Q1_Madre_Initial_02.wav"))
                },
                "Niño", new[]
                {
                    ("Nos dijeron que esperaramos a que nos vengan a buscar. Voy contigo?", LoadAudio("VO_Q1_Nino_Found_01.wav"))
                },
                "Madre", new[]
                {
                    ("Gracias perrito!", LoadAudio("VO_Q1_Madre_Delivered_01.wav"))
                },
                "Niño", new[]
                {
                    ("¡Gracias perrito!", LoadAudio("VO_Q1_Nino_Delivered_01.wav"))
                }
            );

            // Q2: Anciana y Cachetoncito
            CreateOrUpdateQuest("Q2", "Anciana y Cachetoncito",
                "Anciana", new[]
                {
                    ("Mi cachetoncitoo, ¿viste a mi cachetoncitoo?", LoadAudio("VO_Q2_Anciana_Initial_01.wav")),
                    ("Cachetoncito!!!", LoadAudio("VO_Q2_Anciana_Initial_02.wav"))
                },
                "Cachetoncito", new[]
                {
                    ("Uy loco mira que esta vieja se asusto y yo me abri, yo me parcho con usted", LoadAudio("VO_Q2_Cachetoncito_Found_01.wav"))
                },
                "Anciana", new[]
                {
                    ("¡Cachetoncitoo!", LoadAudio("VO_Q2_Anciana_Delivered_01.wav"))
                },
                "Cachetoncito", new[]
                {
                    ("¡Noooooo!", LoadAudio("VO_Q2_Cachetoncito_Delivered_01.wav"))
                }
            );

            // Q3: Ciego y Perro Guía
            CreateOrUpdateQuest("Q3", "Ciego y Perro Guia",
                "Humano Ciego", new[]
                {
                    ("Mi perro de asistencia, debe haberse asustado...", LoadAudio("VO_Q3_Ciego_Initial_01.wav")),
                    ("¿Alguien ayudelo por favor...", LoadAudio("VO_Q3_Ciego_Initial_02.wav"))
                },
                "Perro Guía", new[]
                {
                    ("Oye, ¿viste a un humano ciego? Se me perdio", LoadAudio("VO_Q3_PerroGuia_Found_01.wav"))
                },
                "Humano Ciego", new[]
                {
                    ("¡Amigo mío, estás aquí! Gracias, perrito.", (AudioClip)null)
                },
                "Perro Guía", new[]
                {
                    ("Sniff- ¡Guau!", (AudioClip)null)
                }
            );

            // Q4: Dueño Cuervo y Cuervo
            CreateOrUpdateQuest("Q4", "Dueno y Cuervo",
                "Dueño Cuervo", new[]
                {
                    ("¿Vieron un cuervo que habla mucho?", LoadAudio("VO_Q4_DuenoCuervo_Initial_01.wav"))
                },
                "Cuervo", new[]
                {
                    ("Hola canino, se me perdió un humano. Voy a volar contigo mientras lo busco, si no hay problema", LoadAudio("VO_Q4_Cuervo_Found_01.wav"))
                },
                "Dueño Cuervo", new[]
                {
                    ("¡Chico! Te me habías perdido.", (AudioClip)null)
                },
                "Cuervo", new[]
                {
                    ("¡Chico, te me habias perdido!", LoadAudio("VO_Q4_Cuervo_Delivered_01.wav"))
                }
            );

            // Q5: Mujer y Tortuga
            CreateOrUpdateQuest("Q5", "Mujer y Tortuga",
                "Mujer", new[]
                {
                    ("Saqué a pasear a mi tortuga y no la veo", LoadAudio("VO_Q5_Mujer_Initial_01.wav")),
                    ("Ayuda!", LoadAudio("VO_Q5_Mujer_Initial_02.wav"))
                },
                "Tortuga", new[]
                {
                    ("Amigo, esa vieja me pateó y quede asi... ¿Me puedes llevar en tu lomo?", LoadAudio("VO_Q5_Tortuga_Found_01.wav"))
                },
                "Mujer", new[]
                {
                    ("Mira! dónde estabas?", LoadAudio("VO_Q5_Mujer_Delivered_01.wav"))
                },
                "Tortuga", new[]
                {
                    ("Tetranutria...", LoadAudio("VO_Q5_Tortuga_Delivered_01.wav"))
                }
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuestDataGenerator] Las 5 misiones fueron generadas con sus textos y audios oficiales!");
        }

        private static AudioClip LoadAudio(string fileName)
        {
            string path = $"{AudioFolder}/{fileName}";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[QuestDataGenerator] No se encontró el clip de audio en: {path}");
            }
            return clip;
        }

        private static DialogueLine[] ToLines((string text, AudioClip clip)[] data)
        {
            var lines = new DialogueLine[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                lines[i] = new DialogueLine
                {
                    text = data[i].text,
                    voiceOrSfx = data[i].clip
                };
            }
            return lines;
        }

        private static void CreateOrUpdateQuest(
            string questId,
            string title,
            string ownerSpeaker, (string text, AudioClip clip)[] ownerLines,
            string petSpeaker, (string text, AudioClip clip)[] petLines,
            string ownerDeliveredSpeaker, (string text, AudioClip clip)[] ownerDeliveredLines,
            string petDeliveredSpeaker, (string text, AudioClip clip)[] petDeliveredLines)
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
