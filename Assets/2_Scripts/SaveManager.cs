using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    
    
    // Check if a key exists
    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    // Delete a specific key
    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
    
    // Delete all saved data
    public static void DeleteAllKeys()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }


    // Save methods for different data types
    public static void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public static void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public static void SaveString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public static void SaveBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Load methods for different data types
    public static int LoadInt(string key, int defaultValue = 0)
    {
        // Check if the key exists
        if (!HasKey(key))
        {
            PlayerPrefs.SetInt(key, defaultValue);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public static float LoadFloat(string key, float defaultValue = 0f)
    {
        // Check if the key exists
        if (!HasKey(key))
        {
            PlayerPrefs.SetFloat(key, defaultValue);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    public static string LoadString(string key, string defaultValue = "")
    {
        // Check if the key exists
        if (!HasKey(key))
        {
            PlayerPrefs.SetString(key, defaultValue);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetString(key, defaultValue);
    }

    public static bool LoadBool(string key, bool defaultValue = false)
    {
        // Check if the key exists
        if (!HasKey(key))
        {
            PlayerPrefs.SetInt(key, defaultValue ? 1 : 0);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }


}

