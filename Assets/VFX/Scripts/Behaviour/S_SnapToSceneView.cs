using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SnapToSceneViewRuntime : MonoBehaviour
{
    [Header("Editor Only")]
    public bool snapNow = false;
    public bool liveFollow = false;

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (snapNow)
            {
                Snap();
                snapNow = false;
            }

            if (liveFollow)
            {
                Snap();
            }
        }
#endif
    }

#if UNITY_EDITOR
    void Snap()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null) return;

        transform.position = sv.camera.transform.position;
        transform.rotation = sv.camera.transform.rotation;

        EditorUtility.SetDirty(transform);
    }
#endif
}
