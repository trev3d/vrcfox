using UnityEngine;

public class KeepSceneAlive : MonoBehaviour
{
    #if UNITY_EDITOR
    public bool KeepSceneViewActive;

    void Start()
    {
        if (this.KeepSceneViewActive && Application.isEditor)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
    }
    #endif
}