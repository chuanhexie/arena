using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class BattleCollider : MonoBehaviour 
    {
        public bool playerIsImmune;
        public bool enemyIsImmune;
        public bool destroySelfOnCollision;
        public bool hasContinuousMovement;
        public bool hasContactEffects;

        public bool hasHadInitialPush = false;

        public float curSpeed;
        public Vector3 curDirection;
        public float timeBeforeSelfDestroy;

        public CombatStats combatStats;
        public Tool toolThisWasCreatedFrom;
        public GameObject prefabToSpawn;
        public GameObject prefabBattleColliderInstructionOnSelfDestroy;

        [Header("Fire")]
        public bool spawnsFireOnSelfDestroy;
        public int fireGridCount;
        public float fireGridSpacialSize;

        public List<GameObject> collidedBattleObjects;

        // Use this for initialization
        void Start () 
        {

        }
    }
}