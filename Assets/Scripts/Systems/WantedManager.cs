using UnityEngine;
using ExiledAlvaston.Data;
using ExiledAlvaston.World;

namespace ExiledAlvaston.Systems
{
    /// <summary>
    /// Manages the "Knives" notoriety system and city evasion logic.
    /// </summary>
    public class WantedManager : MonoBehaviour
    {
        public static WantedManager Instance { get; private set; }

        [Header("Wanted State")]
        [Range(0, 5)]
        public int CurrentKnives = 0;
        
        [Header("Cooldown Config")]
        [Tooltip("Base cooldown applied to a chunk per Knife level when evading (in seconds)")]
        public float CooldownPerKnife = 60f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Raises the Knives level by one (capped at 5) and refreshes the HUD. Used when the
        /// player does something the law frowns on — e.g. slinging magic in the city.
        /// </summary>
        public void SpikeKnives()
        {
            if (CurrentKnives < 5)
            {
                CurrentKnives++;
                UpdateUIIndicator();
            }
        }

        /// <summary>
        /// Hook called by the ChunkManager when the player transitions between grid chunks.
        /// Evaluates evasion logic.
        /// </summary>
        public void OnChunkTransition(MapChunkData previousChunk, MapChunkData newChunk)
        {
            if (previousChunk == null || newChunk == null) return;
            if (CurrentKnives == 0) return; // Not wanted

            // If escaping a City into a Wilderness chunk
            if (previousChunk.IsCity && !newChunk.IsCity)
            {
                Debug.Log("Evaded Police by entering a wilderness chunk!");

                // Apply cooldown to the city chunk we just left
                float cooldown = CurrentKnives * CooldownPerKnife;
                ChunkManager.Instance.ApplyCityLockout(previousChunk, cooldown);

                // Clear wanted level
                CurrentKnives = 0;
                UpdateUIIndicator();
            }
            // If entering a new City while already wanted, notoriety persists.
            else if (!previousChunk.IsCity && newChunk.IsCity)
            {
                Debug.Log("Entered a city while still wanted. Re-initiating pursuit.");
            }
        }

        private void UpdateUIIndicator()
        {
            if (ExiledAlvaston.UI.UIManager.Instance != null)
                ExiledAlvaston.UI.UIManager.Instance.UpdateKnivesUI(CurrentKnives);
        }
    }
}
