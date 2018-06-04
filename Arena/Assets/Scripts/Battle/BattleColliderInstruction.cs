using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class BattleColliderInstruction : MonoBehaviour 
    {
        public LayerMask layerMask;
        public bool usesAltAimReticule;
        public bool isChildOfPlayer;
        public bool isPassiveSpawn;
        public bool playerIsImmune;
        public bool enemyIsImmune;
        public float startingSpeed;
        public float duration;
        public float forwardDistanceToSpawn;
        public bool isRaycast;
        public float raycastLength;
        public GameObject prefabToSpawn;

        public GameObject prefabBattleColliderInstructionOnSelfDestroy;

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