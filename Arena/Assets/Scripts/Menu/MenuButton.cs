using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEngine.UI;

namespace Arena
{
    public class MenuButton : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("General")]
        public MenuButtonType menuButtonType;

        [Header("Store Item")]
        public bool isItemPurchased;

        [Header("Tool")]
        public Tool representedToolScript;

        [Header("REFERENCE")]
        [Header("Scripts")]
        public Button unityButtonScript;
        public RectTransform rectTransformScript;

    }
}