using UnityEngine;

[ExecuteAlways]
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = null;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView != null)
                cam = sceneView.camera;
        }
        else
        {
            cam = Camera.main;
        }
#else
        cam = Camera.main;
#endif

        if (cam != null)
            transform.forward = cam.transform.forward;
    }
}
