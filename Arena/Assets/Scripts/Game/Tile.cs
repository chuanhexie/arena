using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class Tile : MonoBehaviour 
    {
        [Header("(REFERENCE)")]
        [Header("General Flags")]
        public bool isBlock;
        public bool isSpawner;

        [Header("Coordinates in Grid (not scene)")]
        public int locationX;
        public int locationY;

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