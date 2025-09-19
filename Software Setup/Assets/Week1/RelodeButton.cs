using UnityEngine;
using UnityEngine.SceneManagement;

public class RelodeButton : MonoBehaviour
{
    public void OnRelode()
    {
        // Get the current scene's build index
        int sceneindex = SceneManager.GetActiveScene().buildIndex;

        // Relode the scene
        SceneManager.LoadScene(sceneindex);
    }
}
