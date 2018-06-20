using UnityEngine;
using UnityEditor;

public class DisableShortcutsInEditor : MonoBehaviour {

	[MenuItem("Disabled in Play Mode/Scene %1")]
	static void WindowScene()
	{
        Debug.Log("in");

        if (!Application.isPlaying)
        {
            EditorApplication.ExecuteMenuItem("Window/Scene");
        }
        else
        {
            Debug.Log("in");
        }
	}

}