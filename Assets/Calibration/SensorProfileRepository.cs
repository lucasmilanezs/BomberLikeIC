using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IC.Calibration
{
    /// <summary>
    /// Singleton que gerencia perfis de sensor em disco.
    /// Persiste em Application.persistentDataPath/SensorProfiles/
    /// Acesse via SensorProfileRepository.Instance
    /// </summary>
    public class SensorProfileRepository : MonoBehaviour
    {
        public static SensorProfileRepository Instance { get; private set; }

        private const string FolderName = "SensorProfiles";
        private string _folderPath;

        public SensorProfile ActiveProfile { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _folderPath = Path.Combine(Application.persistentDataPath, FolderName);
            Directory.CreateDirectory(_folderPath);
        }

        // --- CRUD ---

        public List<SensorProfile> LoadAll()
        {
            var profiles = new List<SensorProfile>();

            foreach (var file in Directory.GetFiles(_folderPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonUtility.FromJson<SensorProfile>(json);
                    if (profile != null)
                        profiles.Add(profile);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SensorProfileRepository] Erro ao ler {file}: {e.Message}");
                }
            }

            return profiles;
        }

        public void Save(SensorProfile profile)
        {
            if (profile == null) return;

            profile.MarkUpdated();
            var path = ProfilePath(profile.id);
            var json = JsonUtility.ToJson(profile, prettyPrint: true);

            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SensorProfileRepository] Erro ao salvar perfil: {e.Message}");
            }
        }

        public void Delete(string id)
        {
            var path = ProfilePath(id);
            if (File.Exists(path))
                File.Delete(path);

            if (ActiveProfile?.id == id)
                ActiveProfile = null;
        }

        public void SetActive(SensorProfile profile)
        {
            ActiveProfile = profile;
            Debug.Log($"[SensorProfileRepository] Perfil ativo: {profile?.patientName ?? "nenhum"}");
        }

        public SensorProfile CreateAndSave(string patientName)
        {
            var profile = SensorProfile.CreateNew(patientName);
            Save(profile);
            return profile;
        }

        // --- Helpers ---

        private string ProfilePath(string id)
            => Path.Combine(_folderPath, $"{id}.json");
    }
}
