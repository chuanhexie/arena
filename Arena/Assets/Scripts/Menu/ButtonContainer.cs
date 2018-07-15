using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace Arena
{
    public class ButtonContainer : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        public ButtonContainerType buttonContainerType;
        public Vector2 buttonDimensions;

        [Header("(REFERENCE)")]
        public List<MenuButton> childMenuButtonScripts;
    }
}