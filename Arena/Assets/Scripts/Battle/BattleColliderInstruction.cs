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
        public bool hasContactEffects;
        public bool isShield;
        public bool isDestroyedByShield;
        public bool canCollideWithBCs;

        [Header("General Stats")]
        public bool infiniteDuration = false;
        public float duration;
        public float forwardDistanceToSpawn;

        [Header("Hitbox")]
        public float startingSpeed;
        public bool hasContinuousMovement;
        public Vector3 hitboxScale;
        public bool usesEdgeCollider;

        [Header("Raycast")]
        public bool isRaycast;
        public float raycastLength;
        public int raycastCount;
        public float multiRaycastSpreadAngle;

        [Header("Fire")]
        public bool spawnsFireOnSelfDestroy = true;
        public int fireGridCount = 1;
        public float fireGridSpacialSize;

        [Header("Prefabs")]
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