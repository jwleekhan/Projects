using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] string nextScene;
    [SerializeField] Image fadeImage;
    [SerializeField] float fadeInDuration = 1.5f;
    [SerializeField] float fadeOutDuration = 4f;

    bool isFadingIn = true;
    bool isFadingOut = false;

    void Start()
    {
        isFadingIn = true;
        fadeImage.color = Color.black;
    }

    void Update()
    {
        if (isFadingIn) // 입장 시 페이드 인
        {
            float alpha = fadeImage.color.a - Time.deltaTime / fadeInDuration;
            fadeImage.color = new Color(0, 0, 0, alpha);
            if (alpha <= 0)
            {
                isFadingIn = false;
            }
        }
        else if (isFadingOut) // 페이드 아웃 후 다음 페이즈로 전환
        {
            float alpha = fadeImage.color.a + Time.deltaTime / fadeOutDuration;
            fadeImage.color = new Color(0, 0, 0, alpha);
            if (alpha >= 1)
            {
                //SoundManager.Instance.StopBGM();
                SceneManager.LoadScene(nextScene);
            }
        }
    }

    public void StartFadeOut()
    {
        if (nextScene == "")
            nextScene = SceneManager.GetActiveScene().name;
        isFadingOut = true;
    }
}
