using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonActions : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("MenuButtonActions: scene name is blank.", this);
            return;
        }

        ResetGameStateForMenu();
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByIndex(int buildIndex)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"MenuButtonActions: build index {buildIndex} is out of range.", this);
            return;
        }

        ResetGameStateForMenu();
        SceneManager.LoadScene(buildIndex);
    }

    public void LoadNextScene()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("MenuButtonActions: no next scene in Build Settings. Going to main menu instead.", this);
            LoadMainMenu();
            return;
        }

        ResetGameStateForMenu();
        SceneManager.LoadScene(nextIndex);
    }

    public void ReloadCurrentScene()
    {
        ResetGameStateForMenu();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        LoadSceneByName(mainMenuSceneName);
    }

    public void QuitGame()
    {
        ResetGameStateForMenu();

#if UNITY_EDITOR
        Debug.Log("MenuButtonActions: QuitGame called. Application.Quit() does nothing inside the Unity editor.", this);
#else
        Application.Quit();
#endif
    }

    public void SetObjectActive(GameObject target)
    {
        if (target != null)
            target.SetActive(true);
    }

    public void SetObjectInactive(GameObject target)
    {
        if (target != null)
            target.SetActive(false);
    }

    public void ToggleObject(GameObject target)
    {
        if (target != null)
            target.SetActive(!target.activeSelf);
    }

    private static void ResetGameStateForMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
