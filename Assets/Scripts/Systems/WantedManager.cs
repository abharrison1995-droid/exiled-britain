using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ExiledAlvaston.Data;
using ExiledAlvaston.World;

namespace ExiledAlvaston.Systems
{
    /// <summary>
    /// Manages the "Knives" notoriety system, police escalation, and evasion logic.
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
        
        [Tooltip("Radius multiplier for scaling the firearm sound blast per knife level")]
        public float BaseSoundRadius = 20f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Called when the player fires a gun. Alerts nearby police NPCs.
        /// </summary>
        public void TriggerFirearmSound(Vector3 position)
        {
            // Escalate if not maxed
            if (CurrentKnives < 5)
            {
                CurrentKnives++;
                UpdateUIIndicator();
            }

            // Calculate dynamic radius based on animation curve or linear scaling
            float actualRadius = BaseSoundRadius * CurrentKnives;

            // Alert nearby police
            Collider[] hitColliders = Physics.OverlapSphere(position, actualRadius, LayerMask.GetMask("Police"));
            foreach (var col in hitColliders)
            {
                // col.GetComponent<PoliceAI>().SetAggressive(true);
                Debug.Log($"Alerted Police NPC: {col.gameObject.name}");
            }
            
            // Spawn additional police if needed based on Knife level
            SpawnEscalationUnits();
        }

        private void SpawnEscalationUnits()
        {
            // Logic to spawn riot police, helicopters, etc. based on CurrentKnives
            // Only happens if the current chunk is a City.
            if (ChunkManager.Instance != null && ChunkManager.Instance.CurrentChunkData != null)
            {
                if (ChunkManager.Instance.CurrentChunkData.IsCity)
                {
                    Debug.Log($"Spawning reinforcement tier {CurrentKnives}");
                }
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

        private void OnDrawGizmos()
        {
            // Debug the current blast radius if wanted
            if (CurrentKnives > 0)
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                // Hardcoded origin point for debug, in reality, use the player position.
                Gizmos.DrawWireSphere(Vector3.zero, BaseSoundRadius * CurrentKnives);
            }
        }
    }
}
