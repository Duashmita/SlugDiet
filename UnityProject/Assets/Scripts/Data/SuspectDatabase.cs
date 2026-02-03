using UnityEngine;

namespace UndercoverBarber.Data
{
    [CreateAssetMenu(fileName = "SuspectDatabase", menuName = "Undercover Barber/Suspect Database")]
    public class SuspectDatabase : ScriptableObject
    {
        [SerializeField] private SuspectProfile[] suspects;

        public SuspectProfile GetRandomSuspect()
        {
            if (suspects == null || suspects.Length == 0)
            {
                Debug.LogError("No suspects in database!");
                return null;
            }

            return suspects[Random.Range(0, suspects.Length)];
        }

        public SuspectProfile GetSuspectByCodename(string codename)
        {
            foreach (var suspect in suspects)
            {
                if (suspect.codename == codename)
                    return suspect;
            }
            return null;
        }

        public int Count => suspects?.Length ?? 0;
    }
}
