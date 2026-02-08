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
        PlayerPrefs.SetInt(SAVE_KEY, 1);
        PlayerPrefs.SetInt("IS_NEW_GAME", 1);
        PlayerPrefs.Save();

        // Set flag supaya GameManager tau ini new game

        SceneManager.LoadScene("GamePlay");
    }


    // LOAD GAME
    public void LoadGame()
    {
        if (PlayerPrefs.GetInt(SAVE_KEY, 0) == 0)
            return;

        PlayerPrefs.SetInt("IS_LOAD_GAME", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("GamePlay");
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GamePlay")
        {
            BuildingManager buildingManager = GameObject.FindFirstObjectByType<BuildingManager>();
            GridManager gridManager = GameObject.FindFirstObjectByType<GridManager>();

            if (buildingManager != null && gridManager != null)
            {
                SaveSystem.LoadGame(buildingManager, gridManager);
            }
            else
            {
                Debug.LogWarning("BuildingManager/GridManager tidak ditemukan saat load!");
            }

            // lepas listener agar tidak dipanggil berkali-kali
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
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
