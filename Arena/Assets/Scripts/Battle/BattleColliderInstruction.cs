using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class BattleColliderInstruction : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("General Flags")]
        public LayerMask layerMask;
        public bool usesAltAimReticule;
        public bool isChildOfPlayer;
        public bool isPassiveSpawn;
        public bool playerIsImmune;
        public bool enemyIsImmune;
        public bool destroySelfOnCollision;

        [Header("General Stats")]
        public float duration;
        public float forwardDistanceToSpawn;

        [Header("Hitbox")]
        public float startingSpeed;

        [Header("Raycast")]
        public bool isRaycast;
        public float raycastLength;
        public int raycastCount;
        public float multiRaycastSpreadAngle;

        [Header("Prefabs")]
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