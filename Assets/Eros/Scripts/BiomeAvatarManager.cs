using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class BiomeAvatarManager : MonoBehaviour
{
    [Header("Google Sheets CSV")]
    [Tooltip("Pega aquí tu link CSV publicado")]
    public string csvUrl = "PASTE_YOUR_CSV_URL_HERE";

    [Header("Polling")]
    [Tooltip("How often to check the CSV (seconds).")]
    public float pollSeconds = 5f;

    [Header("Avatar Prefabs")]
    public List<AnimalPrefabEntry> animalPrefabs;

    [Header("Layout")]
    public float radius = 6f;

    private Dictionary<string, GameObject> prefabLookup;

    // Runtime state
    private Dictionary<string, GameObject> spawnedByName = new();
    private Dictionary<string, string> animalByName = new();

    void Start()
    {
        BuildLookup();
        StartCoroutine(PollLoop());
    }

    void BuildLookup()
    {
        prefabLookup = new Dictionary<string, GameObject>();
        foreach (var entry in animalPrefabs)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.animalKey) || entry.prefab == null) continue;
            prefabLookup[entry.animalKey.Trim()] = entry.prefab;
        }
    }

    IEnumerator PollLoop()
    {
        while (true)
        {
            yield return LoadCSVOnce();
            yield return new WaitForSeconds(pollSeconds);
        }
    }

    IEnumerator LoadCSVOnce()
    {
        // --- CACHE BUSTER ---
        // Hace que cada request sea una URL única para evitar recibir CSV viejo de caches/proxies.
        string urlNoCache = csvUrl +
            (csvUrl.Contains("?") ? "&" : "?") +
            "t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        using UnityWebRequest req = UnityWebRequest.Get(urlNoCache);

        // Hints para caches intermedios (no garantiza 100% pero ayuda bastante)
        req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        req.SetRequestHeader("Pragma", "no-cache");
        req.SetRequestHeader("Expires", "0");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("CSV load failed: " + req.error);
            yield break;
        }

        ApplyCSV(req.downloadHandler.text);
    }

    void ApplyCSV(string csv)
    {
        var parsed = ParseCSVToDict(csv); // name -> animalKey

        // 1) Add / Update existing
        int index = 0;
        foreach (var kv in parsed)
        {
            string name = kv.Key;
            string animalKey = kv.Value;

            if (!spawnedByName.ContainsKey(name))
            {
                SpawnAvatar(name, animalKey, index);
            }
            else
            {
                // Update if animal changed
                if (animalByName.TryGetValue(name, out string oldAnimal) && oldAnimal != animalKey)
                {
                    ReplaceAvatar(name, animalKey);
                }
            }
            index++;
        }

        // 2) Remove: si ya no está en la sheet, se borra del mundo
        var toRemove = new List<string>();
        foreach (var existingName in spawnedByName.Keys)
        {
            if (!parsed.ContainsKey(existingName))
                toRemove.Add(existingName);
        }

        foreach (var name in toRemove)
        {
            if (spawnedByName.TryGetValue(name, out var go) && go != null)
                Destroy(go);

            spawnedByName.Remove(name);
            animalByName.Remove(name);
        }
    }

    Dictionary<string, string> ParseCSVToDict(string csv)
    {
        var result = new Dictionary<string, string>();

        string[] lines = csv.Split('\n');
        for (int i = 1; i < lines.Length; i++) // skip header
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // CSV SIMPLE: Name,Animal
            // (Si luego meten comas en el nombre, lo refinamos, pero para clase está bien.)
            string[] cols = line.Split(',');
            if (cols.Length < 2) continue;

            string name = cols[0].Trim();
            string animal = cols[1].Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(animal)) continue;

            // Si hay nombres repetidos, el último gana
            result[name] = animal;
        }

        return result;
    }

    void SpawnAvatar(string displayName, string animalKey, int index)
    {
        if (!prefabLookup.TryGetValue(animalKey, out GameObject prefab))
        {
            Debug.LogWarning("No prefab for animal: " + animalKey);
            return;
        }

        Vector3 pos = PositionForIndex(index);

        GameObject avatar = Instantiate(prefab, pos, Quaternion.identity, transform);
        avatar.name = $"Avatar_{displayName}";

        AttachNameLabel(avatar, displayName);

        spawnedByName[displayName] = avatar;
        animalByName[displayName] = animalKey;
    }

    void ReplaceAvatar(string displayName, string newAnimalKey)
    {
        // Keep the same position
        Vector3 pos = spawnedByName[displayName].transform.position;

        // Destroy old
        Destroy(spawnedByName[displayName]);

        // Spawn new
        if (!prefabLookup.TryGetValue(newAnimalKey, out GameObject prefab))
        {
            Debug.LogWarning("No prefab for animal: " + newAnimalKey);
            return;
        }

        GameObject avatar = Instantiate(prefab, pos, Quaternion.identity, transform);
        avatar.name = $"Avatar_{displayName}";

        AttachNameLabel(avatar, displayName);

        spawnedByName[displayName] = avatar;
        animalByName[displayName] = newAnimalKey;
    }

    Vector3 PositionForIndex(int index)
    {
        // Simple ring layout
        float angle = index * Mathf.PI * 2f / 12f;
        return new Vector3(Mathf.Cos(angle) * radius, 2.5f, Mathf.Sin(angle) * radius);
    }

    void AttachNameLabel(GameObject avatar, string displayName)
    {
        // Name label (TMP 3D)
        GameObject labelGO = new GameObject("NameLabel");
        labelGO.transform.SetParent(avatar.transform);
        labelGO.transform.localPosition = Vector3.up * 1.5f;
        labelGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        TextMeshPro tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text = displayName;
        tmp.fontSize = 2.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
    }
}

[System.Serializable]
public class AnimalPrefabEntry
{
    public string animalKey;
    public GameObject prefab;
}