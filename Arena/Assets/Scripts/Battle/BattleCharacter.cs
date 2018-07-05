using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace Arena
{
    public class BattleCharacter : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        public bool hasCustomDamage;
        public float customDamage;
        public bool hasCustomKnockback;
        public float customKnockback;
        public bool hasCustomMeleeCooldown;
        public float customMeleeCooldown;

        [Header("(REFERENCE)")]
        public bool isPlayer;

        [Header("General")]
        public Sprite sprite;
        public float walkSpeed;
        public float runSpeed;
        public bool isFlyer;

        [Header("Status Ailments")]
        public float remainingStunTime;
        public float basePoisonPoints;
        public float poisonDamageFrequency;
        public float curPoisonDamageCountdown;

        [Header("Relationship to Player")]
        public float meleeTowardsPlayerCooldown;

        [Header("AI / Enemy")]
        public EnemyModel enemyModel;
        public float knockbackMovementDisabledCountdown;

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