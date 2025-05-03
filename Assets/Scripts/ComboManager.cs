using DG.Tweening;
using TMPro;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [SerializeField]
    private string[] comboTexts;
    [SerializeField]
    private Color[] comboColors;
    [SerializeField]
    private AudioClip[] comboSounds;
    [SerializeField]
    private TextMeshProUGUI comboText;
    [SerializeField]
    private RectTransform comboTextContainer;
    [SerializeField]
    private AudioSource audioSource;

    private int _comboIndex = -3;

    public int ComboIndex
    {
        get => _comboIndex;
        set
        {
            if (_comboIndex == value) return;
            _comboIndex = (value <= 6) ? value : 6;

            if (_comboIndex >= 0)
            {
                comboText.SetText(comboTexts[_comboIndex]);
                comboText.color = comboColors[_comboIndex];
                audioSource.PlayOneShot(comboSounds[_comboIndex]);
                comboTextContainer.gameObject.SetActive(true);
                comboTextContainer.DOKill();

                comboTextContainer.localScale = Vector3.zero;
                comboTextContainer.localRotation = Quaternion.identity;

                float randomZ = Random.Range(-15f, 15f);

                // Main pop-in sequence
                var seq = DOTween.Sequence()
                    .Append(comboTextContainer.DOScale(1f, 0.4f).SetEase(Ease.OutBack))
                    .Join(comboTextContainer.DOLocalRotate(
                          new Vector3(0, 0, randomZ), 0.4f).SetEase(Ease.OutQuad));

                // If combo >=4, add a quick shake after pop-in
                if (_comboIndex >= 4)
                {
                    seq.AppendInterval(0.1f)
                       .Append(
                           comboTextContainer
                               .DOShakeRotation(
                                   duration: 0.5f,
                                   strength: new Vector3(0, 0, 10f),
                                   vibrato: 10,
                                   randomness: 90f
                               )
                       );
                }

                seq.Play();
            }
            else
            {
                Debug.Log("KILL THE COMBO!");
                comboTextContainer.DOKill();

                comboTextContainer.gameObject.SetActive(true);
                comboTextContainer.localScale = Vector3.one;
                comboTextContainer.localRotation = Quaternion.identity;

                // Tween scale down to zero
                var seqOut = DOTween.Sequence()
                    .Append(comboTextContainer.DOScale(0f, 0.3f).SetEase(Ease.InBack))
                    .OnStart(() =>
                    {
                        comboText.SetText("");
                    })
                    .OnComplete(() =>
                    {
                        comboTextContainer.gameObject.SetActive(false);
                    });
                seqOut.Play();
            }
        }
    }

    private void Awake() => Instance = this;
}
