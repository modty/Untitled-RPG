using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake() {
        if (SceneManager.GetActiveScene().name != "SceneManagement") {
            AsyncOperation addLoadingScene = SceneManager.LoadSceneAsync("SceneManagement", LoadSceneMode.Additive);
        }
    }

    public void LoadLevel (string levelName) {
        Vector3 playerPos;
        Quaternion playerRot;
        switch (levelName) {
            case "City":
                playerPos = new Vector3(-560,10,-120);
                playerRot = Quaternion.identity;
                break;
            default:
                playerPos = new Vector3(0,1,0);
                playerRot = Quaternion.identity;
                break;
        }
        ScenesManagement.instance.LoadLevel(levelName);
    }

    public void ExitGame () {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
