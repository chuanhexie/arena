using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arena
{
    public class ToolSelectOptionDisplay : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Child Plugins")]
        public GameObject toolThumbnailGameObject;
        public GameObject stamOrManaFlag;
        public GameObject isEquippedFlag;
        public GameObject isBleedFlag;

        [Space(10)]

        [Header("(REFERENCE)")]
        public GameObject representedPlayerTool;
        public float angleInQuickselect;

        // Use this for initialization
        void Start () 
        {
            
        }

        // Update is called once per frame
        void Update () 
        {

        }
    }
}