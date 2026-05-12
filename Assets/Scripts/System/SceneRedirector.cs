using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRedirector : MonoBehaviour
{
    [SerializeField] private string targetScene = "MenuScene";
    [SerializeField] private float delaySeconds = 1.25f;

    private float _timer;

    void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer < delaySeconds) return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(targetScene);
    }
}
