using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    void Start()
    {
        // Fade in on start
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.5f).SetEase(Ease.InOutQuad).Play();
    }

    public void LoadLevelSelect()
    {
        // Fade out before loading scene
        canvasGroup.DOFade(0, 0.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            SceneManager.LoadScene("LevelSelection");
        }).Play();
    }
}
