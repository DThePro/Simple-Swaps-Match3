using System;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { set; get; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Instance = this;
        Load(); 
    }

    public void Save(string saveItem = null)
    {
        var raw = saveItem ?? HighScore.Instance.HScore.ToString();
        var encrypted = EncryptionUtility.EncryptString(raw);
        PlayerPrefs.SetString("highScore", encrypted);
        PlayerPrefs.Save();
    }

    public int Load()
    {
        if (PlayerPrefs.HasKey("highScore"))
        {
            var encrypted = PlayerPrefs.GetString("highScore");
            try
            {
                var decrypted = EncryptionUtility.DecryptString(encrypted);
                return int.Parse(decrypted);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to decrypt highScore: {e.Message}");
                return 0;
            }
        }
        else
        {
            Save("0");  // Save 0 if no highscore found.
            Debug.Log("No save file found. Creating a new one.");
            return 0;
        }
    }

    public void ResetSave()
    {
        var raw = "0";
        var encrypted = EncryptionUtility.EncryptString(raw);
        PlayerPrefs.SetString("highScore", encrypted);
        PlayerPrefs.Save();
    }
}
