using UnityEngine;

namespace UndercoverBarber.Data
{
    [CreateAssetMenu(fileName = "NewSuspect", menuName = "Undercover Barber/Suspect Profile")]
    public class SuspectProfile : ScriptableObject
    {
        [Header("Identity")]
        public string codename;
        public Sprite silhouette;

        [Header("Identifying Traits")]
        [TextArea(2, 4)]
        public string[] traits;

        [Header("Background")]
        [TextArea(3, 5)]
        public string lastKnownActivity;

        [Header("Suspect Dialogue Clues")]
        [TextArea(2, 3)]
        public string[] smallTalkClues;
        [TextArea(2, 3)]
        public string[] probeClues;
        [TextArea(2, 3)]
        public string[] directClues;

        [Header("Special")]
        public string specialHaircutRequest;

        public string GetRandomClue(DialogueType type)
        {
            string[] pool = type switch
            {
                DialogueType.SmallTalk => smallTalkClues,
                DialogueType.Probe => probeClues,
                DialogueType.Direct => directClues,
                _ => smallTalkClues
            };

            if (pool == null || pool.Length == 0) return "";
            return pool[Random.Range(0, pool.Length)];
        }
    }

    public enum DialogueType
    {
        SmallTalk,
        Probe,
        Direct
    }
}
