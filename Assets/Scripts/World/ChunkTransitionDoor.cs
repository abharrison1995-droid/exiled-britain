using UnityEngine;
using ExiledAlvaston.Data;

namespace ExiledAlvaston.World
{
    /// <summary>
    /// Generic one-way chunk transition trigger: swaps the active chunk via ChunkManager
    /// and teleports the player to a target spawn point. Used for house interiors and other
    /// instanced spaces that sit outside the North/South/East/West edge grid.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ChunkTransitionDoor : MonoBehaviour
    {
        public MapChunkData TargetChunk;
        public Vector3 TargetSpawnPosition;
        public string Prompt = "Enter";

        private float _readyAt;

        private void Reset()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            // Guards against instantly re-triggering the door you just spawned next to.
            _readyAt = Time.unscaledTime + 0.75f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.unscaledTime < _readyAt) return;

            if (!other.CompareTag("Player") && other.GetComponentInParent<Combat.CombatController>() == null)
                return;

            if (ChunkManager.Instance == null || TargetChunk == null || TargetChunk.ChunkPrefab == null)
            {
                Debug.LogWarning("ChunkTransitionDoor: missing ChunkManager or TargetChunk/prefab — transition aborted.");
                return;
            }

            if (ChunkManager.Instance.CurrentChunkInstance != null)
                Destroy(ChunkManager.Instance.CurrentChunkInstance);

            ChunkManager.Instance.CurrentChunkData = TargetChunk;
            GameObject instance = Instantiate(TargetChunk.ChunkPrefab, Vector3.zero, Quaternion.identity);
            instance.name = TargetChunk.ChunkPrefab.name;
            ChunkManager.Instance.CurrentChunkInstance = instance;

            ChunkManager.Instance.TeleportPlayer(TargetSpawnPosition);

            if (!string.IsNullOrEmpty(Prompt) && ExiledAlvaston.UI.UIManager.Instance != null)
                ExiledAlvaston.UI.UIManager.Instance.LogCombat(Prompt);
        }
    }
}
