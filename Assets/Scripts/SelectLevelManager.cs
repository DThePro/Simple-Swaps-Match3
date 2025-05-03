using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class SelectLevelManager : MonoBehaviour
{
    [SerializeField] private Button level2, level3, resetButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    private int reset = 0;

    void Start()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Fade in
        var fadeIn = canvasGroup.DOFade(1, fadeDuration).OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        });
        fadeIn.Play();

        CheckSave();
    }

    void CheckSave()
    {
        if (SaveManager.Instance != null)
        {
            int highScore = SaveManager.Instance.Load();
            level2.interactable = highScore >= 350;
            level3.interactable = highScore >= 700;
        }
    }

    public void LoadLevel1() => FadeAndLoad("Level 1");
    public void LoadLevel2() => FadeAndLoad("Level 2");
    public void LoadLevel3() => FadeAndLoad("Level 3");

    private void FadeAndLoad(string sceneName)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var fadeOut = canvasGroup.DOFade(0, fadeDuration).OnComplete(() =>
        {
            SceneManager.LoadScene(sceneName);
        });
        fadeOut.Play();
    }

    public void ResetSaveFile()
    {
        var text = resetButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        if (reset == 0)
        {
            text.SetText("Confirm Reset?");
            reset++;
        }
        else
        {
            SaveManager.Instance.ResetSave();
            text.SetText("Done.");
            CheckSave();
            reset = 0;
        }
    }
}
