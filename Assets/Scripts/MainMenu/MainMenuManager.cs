using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartCollectMode()
    {
        PlayerPrefs.SetString("GameMode", "Collect");
        PlayerPrefs.Save();
        SceneManager.LoadScene("testParticles"); // اسم مشهد اللعبة
    }

    public void StartSmashMode()
    {
        PlayerPrefs.SetString("GameMode", "Smash");
        PlayerPrefs.Save();
        SceneManager.LoadScene("testParticles"); // اسم مشهد اللعبة
    }

        public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); 
    }
}
