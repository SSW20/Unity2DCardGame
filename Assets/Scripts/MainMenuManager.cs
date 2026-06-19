using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void OnStartClicked()
    {
        SceneManager.LoadScene("GameScene");   // 실제 게임 씬 이름으로 변경
    }

    public void OnExitClicked()
    {
        Debug.Log("Exit 클릭됨");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}