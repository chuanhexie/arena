using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class InputValues : MonoBehaviour 
    {
        [Header("(REFERENCE)")]
        public float horMovement;
        public float vertMovement;
        public float horAim;
        public float vertAim;

        public bool leftToolOrPageLeft;
        public bool rightToolOrPageRight;
        public bool quickselectOrMoreInfo;
        public bool runOrCursorSpeed;
        public bool heal;
        public bool pause;
    }
}