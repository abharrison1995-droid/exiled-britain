using UnityEngine;
using ExiledAlvaston.Combat;
using ExiledAlvaston.World;

namespace ExiledAlvaston.Flow
{
    /// <summary>
    /// Lightweight checkpoint save/load via PlayerPrefs — chunk, position, health/mana/stamina only.
    /// Not a full save-file system; auto-saves on every chunk transition so "Load Last Game" means
    /// "back to where I entered my current area."
    /// </summary>
    public static class SaveGameManager
    {
        private const string KeyHasSave = "EA_HasSave";
        private const string KeyChunkName = "EA_ChunkName";
        private const string KeyPosX = "EA_PosX";
        private const string KeyPosY = "EA_PosY";
        private const string KeyPosZ = "EA_PosZ";
        private const string KeyHealth = "EA_Health";
        private const string KeyMana = "EA_Mana";
        private const string KeyStamina = "EA_Stamina";

        public static bool HasSave => PlayerPrefs.GetInt(KeyHasSave, 0) == 1;

        public static void Save()
        {
            ChunkManager chunkMgr = ChunkManager.Instance;
            CombatController player = CombatController.Instance;
            if (chunkMgr == null || player == null || chunkMgr.CurrentChunkData == null) return;

            Vector3 pos = player.transform.position;
            PlayerPrefs.SetString(KeyChunkName, chunkMgr.CurrentChunkData.ChunkName);
            PlayerPrefs.SetFloat(KeyPosX, pos.x);
            PlayerPrefs.SetFloat(KeyPosY, pos.y);
            PlayerPrefs.SetFloat(KeyPosZ, pos.z);
            PlayerPrefs.SetInt(KeyHealth, player.CurrentHealth);
            PlayerPrefs.SetInt(KeyMana, player.CurrentMana);
            PlayerPrefs.SetInt(KeyStamina, player.CurrentStamina);
            PlayerPrefs.SetInt(KeyHasSave, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Loads the checkpoint into the current scene. Needs ChunkManager.AllChunks populated to resolve the chunk by name.</summary>
        public static bool Load()
        {
            if (!HasSave) return false;

            ChunkManager chunkMgr = ChunkManager.Instance;
            CombatController player = CombatController.Instance;
            if (chunkMgr == null || player == null) return false;

            string chunkName = PlayerPrefs.GetString(KeyChunkName, "");
            Data.MapChunkData chunk = chunkMgr.FindChunkByName(chunkName);
            if (chunk == null || chunk.ChunkPrefab == null) return false;

            if (chunkMgr.CurrentChunkInstance != null)
                Object.Destroy(chunkMgr.CurrentChunkInstance);

            chunkMgr.CurrentChunkData = chunk;
            GameObject instance = Object.Instantiate(chunk.ChunkPrefab, Vector3.zero, Quaternion.identity);
            instance.name = chunk.ChunkPrefab.name;
            chunkMgr.CurrentChunkInstance = instance;

            Vector3 pos = new Vector3(
                PlayerPrefs.GetFloat(KeyPosX, 0f),
                PlayerPrefs.GetFloat(KeyPosY, 1f),
                PlayerPrefs.GetFloat(KeyPosZ, 0f));
            chunkMgr.TeleportPlayer(pos);

            player.ReviveFull();
            Health health = player.GetComponent<Health>();
            int savedHealth = Mathf.Max(1, PlayerPrefs.GetInt(KeyHealth, player.CurrentHealth));
            if (health != null)
            {
                health.Revive(savedHealth);
                player.CurrentHealth = health.CurrentHealth;
            }
            player.CurrentMana = PlayerPrefs.GetInt(KeyMana, player.CurrentMana);
            player.CurrentStamina = PlayerPrefs.GetInt(KeyStamina, player.CurrentStamina);

            return true;
        }

        public static void ClearSave()
        {
            PlayerPrefs.DeleteKey(KeyHasSave);
        }
    }
}
