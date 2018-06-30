using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arena
{
    public class EnemyModel : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("General")]
        public Sprite sprite;
        public float maxHP;
        public float walkSpeed;
        public bool isFlyer;

        [Header("Ranged Attacks")]
        public BattleColliderInstruction rangedAttackBCI;
        public float rangedAttackWindupTime;
        public float rangedAttackCurWindupTimeRem;
        public bool isWindingUpRangedAttack;
        public float rangedAttackDamage;

        [Header("Behaviors")]
        public AIMovementBehavior aiMovementBehavior;
        public AIFiringBehavior aiFiringBehavior;

        [Header("Agressive Defense")]
        public float agressiveDefenseDistance;
    }
}