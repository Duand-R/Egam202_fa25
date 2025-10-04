using UnityEngine;
using UnityEngine.SceneManagement;

public class RelodeButton : MonoBehaviour
{
    // Called by UI Button OnClick
    public void OnRelode()
    {
        ReloadScene();
    }

    void Update()
    {
        // Press R key to reload
        if (Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;  
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

    }

    void ReloadScene()
    {
        Time.timeScale = 1f;
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneIndex);
    }
}
