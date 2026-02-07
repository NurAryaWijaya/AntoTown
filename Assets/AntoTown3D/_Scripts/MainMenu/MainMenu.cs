using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI")]
    public Button loadGameButton;

    private const string SAVE_KEY = "HAS_SAVE";

    private void Start()
    {
        CheckSaveData();
    }

    void CheckSaveData()
    {
        bool hasSave = PlayerPrefs.GetInt(SAVE_KEY, 0) == 1;

        loadGameButton.interactable = hasSave;
    }

    // NEW GAME
    public void NewGame()
    {
        // Kalau mau reset save lama
        PlayerPrefs.SetInt(SAVE_KEY, 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("GamePlay");
    }

    // LOAD GAME
    public void LoadGame()
    {
        if (PlayerPrefs.GetInt(SAVE_KEY, 0) == 0)
            return;

        SceneManager.LoadScene("GamePlay");
    }

    // QUIT GAME
    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
