using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Arena
{
    public class ToolQuickSelectMenu : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Child Plugins")]
        public GameObject toolQuickselectRing;
        public GameObject selectionReticule;

        [Space(10)]

        [Header("(REFERENCE)")]
        public float selectionAngle;
        public List<GameObject> toolSelectOptionDisplays;

        public void PositionSelectionReticule (float inputAngle)
        {
            var inputAngleInRadians = inputAngle * Mathf.Deg2Rad;
            Vector2 directionToSpawnSelectionReticule = new Vector2((float)Mathf.Cos(inputAngleInRadians), (float)Mathf.Sin(inputAngleInRadians));
            Vector2 selectionReticuleSpawnPosition = (directionToSpawnSelectionReticule  * UIManager.singleton.toolSelectRingRadius);

            selectionReticule.transform.localPosition = selectionReticuleSpawnPosition;
        }

        public GameObject GetCurrentlySelectedTool()
        {
            var selectedToolDisplay = toolSelectOptionDisplays.FirstOrDefault(x => x.GetComponent<ToolSelectOptionDisplay>().angleInQuickselect == selectionAngle);
            GameObject selectedTool = null;
            if (selectedToolDisplay != null)
                selectedTool = selectedToolDisplay.GetComponent<ToolSelectOptionDisplay>().representedPlayerTool;

            return selectedTool;
        }
    }
}