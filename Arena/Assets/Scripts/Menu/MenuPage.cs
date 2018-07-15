using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace Arena
{
    public class MenuPage : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        public List<GameObject> buttonContainerGOs;

        [Header("(REFERENCE)")]
        public List<ButtonContainer> buttonContainerScripts;
    }
}