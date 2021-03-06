﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Pathfinding;

namespace Arena
{
    public class BattleManager : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Prefabs")]
        public GameObject prefabBattleCharacter;
        public GameObject prefabLineRenderer;
        public GameObject prefabDirectionalAim;
        public GameObject prefabTopDownAim;
        public GameObject prefabFire;
        public GameObject prefabSmallHitbox;

        [Header("Sprites")]
        public Sprite playerSprite;
        public Sprite enemySprite;

        [Header("Containers")]
        public GameObject miscBattleObjectsContainer;
        public GameObject battleHUDContainer;

        [Header("Gameplay General")]
        public float defaultKnockbackForce;
        public float defualtKnockbackTimeLength;

        [Header("Tools General")]
        public float poisonFrequencyMaxTime;

        [Header("Player General")]
        public float playerWalkSpeed;
        public float playerRunSpeed;
        public float playerMirageMovementSpeed;
        public Vector2 playerSpawnLocation;
        public float baseToolUseSpeed;
        public float baseStaminaRegenRate;
        public float idleStaminaRegenBoost;
        public float runningStaminaCost;

        [Header("Player Hitboxes")]
        public Vector2[] playerMovementEdgeColliderPoints;
        public Vector2 playerCombatHitboxDimensions;
        public Vector2 playerCombatHitboxOffset;
        public LayerMask playerCombatHitboxLayer;

        [Header("Player Tools")]
        public List<GameObject> playerTools;

        [Header("Player Stats")]
        public float maxHealth;
        public float maxStamina;
        public float maxMana;

        [Header("Player Directional Aim")]
        public Vector2 playerDirectionalAimCenter;

        [Header("Player Top Down Aim")]
        public float playerTopDownAimMaxDistance;
        public float playerTopDownAimMovementSpeed;
        public Vector2 playerTopDownAimOffset;

        [Header("Player Mirage Sheet")]
        public float defaultMirageDuration;

        [Header("Default Enemy General")]
        public float tempEnemySpeed;
        public float defaultEnemyDamage = 1;
        public float defaultMeleeTowardsPlayerCooldownTime;
        public LayerMask defaultEnemyLineOfSightLayermask;

        [Header("Default Hitboxes")]
        public Vector2[] defaultEnemyMovementEdgeColliderPoints;
        public Vector2 defaultEnemyCombatHitboxDimensions;
        public LayerMask enemyCombatHitboxLayer;
        public LayerMask flyerHitboxLayer;

        [Header("Default Shield")]
        public GameObject mainShieldBCI;
        public float dualShieldHorOffset;
        public float singleShieldVertOffset;
        public float dualShieldRotationOffset;

        [Header("Physics Materials")]
        public PhysicsMaterial2D physicsMaterialDefaultBattleCollider;

        [Header("Default Spawning")]
        public float baseTimeToSpawnEnemy;
        public List<GameObject> enemyModelsToSpawn;

        [Header("Bottle")]
        public float bottleDrinkDuration;
        public float bottleBuffDefaultDuration;

        [Header("Armor")]
        public float armorDefaultStatEffectiveness;

        [Header("Fire")]
        public float fireScanDelay;

        [Space(10)]

        [Header("(REFERENCE)")]
        [Header("Player General")]
        public GameObject player;
        public GameObject playerLeftTool;
        public GameObject playerRightTool;
        public bool playerIsRunning;
        public bool canPlayerPerformAnyAction;

        [Header("Player Buffs")]
        public GameObject playerCurArmor;
        public List<CombatStats> playerAllBuffs;
        public CombatStats playerCurTotalBuff;

        [Header("Player Stats")]
        public float curStamina;
        public float curMana;

        [Header("Player Directional Aim")]
        public GameObject playerDirectionalAim;
        public float curDirectionX;
        public float curDirectionY;

        [Header("Player Top Down Aim")]
        public GameObject playerTopDownAim;
        public float playerTopDownCurAimAngleRadians;
        public float playerTopDownCurAimDistance;

        [Header("Player Tools")]
        public bool isPlayerCurrentlyUsingTool;
        public GameObject playerToolInUse;
        public CombatStats playerToolInUseCombatStats;
        public float currentToolSpeedFinal = 0;
        public float playerToolCompletionCountdown = 0;

        [Header("Player Shields")]
        public ShieldState playerShieldState;
        public List<GameObject> playerShields;

        [Header("Player Mirage Sheet")]
        public bool isPlayerUsingMirage;
        public float remainingPlayerMirageTime;
        public GameObject playerMirageBC;

        [Header("Tool Quickselect")]
        public bool battleToolQuickSelectActive = false;

        [Header("Battle Objects")]
        public List<GameObject> battleObjects;

        [Header("Battle Colliders")]
        public List<GameObject> battleColliders;

        [Header("Fires")]
        public List<GameObject> activeFires;
        public List<GameObject> unscannedFires;
        public float curFireScanCountdown;
        public bool isFireScanCountdownStarted;

        [Header("Enemy Spawning")]
        public float enemySpawnCountdown;
        public int enemySpawnerTileIndex = 0;

        public static BattleManager singleton;
        void Awake()
        {
            singleton = this;
        }

        public void InitBattle()
        {
            // enable misc related objects
            miscBattleObjectsContainer.SetActive(true);
            battleHUDContainer.SetActive(true);

            // SET VARIOUS BATTLE VALUES TO THEIR DEFAULTS
            curDirectionX = 0;
            curDirectionY = -1;
            curStamina = maxStamina;
            curMana = maxMana;
            if (playerTools[0] != null)
                playerLeftTool = playerTools[0];
            if (playerTools[1] != null)
                playerRightTool = playerTools[1];
            enemySpawnCountdown = 0;

            // INIT PLAYER CHARACTER
            InitBattleCharacter(true, playerSpawnLocation);

            // INIT PLAYER SHIELDS
            InitPlayerShields();

            // INIT PLAYER ARMOR
            if (playerCurArmor != null)
            {
                CombatStats armorReducedCombatStats = MultiplyCombatStats(Instantiate(playerCurArmor.GetComponent<Tool>().combatStats.GetComponent<CombatStats>()));
                InitPlayerBuff(armorReducedCombatStats, true);
            }
        }

        public void UpdateBattleLogic()
        {
            ParsePlayerInputsForBattle(GameManager.singleton.inputValuesScript);
            CheckPlayerShields();
            UpdateMirage();

            // ---- TOOL-LOGIC START -----
            // DECREASE TOOL COMPLETION COUNTDOWN IF TOOL IS CURRENTLY BEING USED
            if (playerToolCompletionCountdown > 0)
            {
                playerToolCompletionCountdown -= Time.deltaTime;
                if (playerToolCompletionCountdown < 0)
                    playerToolCompletionCountdown = 0;
            }

            // PLAYER ACTIVE BUFF LOGIC
            for (var i = playerAllBuffs.Count - 1; i > -1; i--)
            {
                var curCombatStats = playerAllBuffs[i];
                if (!curCombatStats.isBuffInfinite)
                {
                    curCombatStats.tempBuffDuration -= Time.deltaTime;
                    if (curCombatStats.tempBuffDuration <= 0)
                        EndPlayerBuff(curCombatStats);
                }
            }

            // REGEN STAMINA IF NOT USING STAMINA TOOL OR RUNNING
            if ((playerToolCompletionCountdown <= 0 || 
                (isPlayerCurrentlyUsingTool && playerToolInUse.GetComponent<Tool>().usesMana)) 
                && curStamina < maxStamina
                && (!playerIsRunning || player.GetComponent<Rigidbody2D>().velocity == Vector2.zero))
            {
                float curStaminaRegen = baseStaminaRegenRate;

                curStamina += curStaminaRegen * Time.deltaTime;
                if (curStamina > maxStamina)
                    curStamina = maxStamina;
            }

            // IF TOOL USE COUNTDOWN IS COMPLETE, USE TOOL
            if (isPlayerCurrentlyUsingTool && playerToolCompletionCountdown <= 0)
            {
                UseTool(playerToolInUse, playerToolInUseCombatStats);
                currentToolSpeedFinal = 0;
                isPlayerCurrentlyUsingTool = false;
            }
            // ------ TOOL-LOGIC END ------


            // -------------- PLAYER-RUN-LOGIC START --------------------
            if (playerIsRunning && player.GetComponent<Rigidbody2D>().velocity != Vector2.zero)
            {
                curStamina -= runningStaminaCost * Time.deltaTime;
                if (curStamina < 0)
                    curStamina = 0;
            }
            // -------------- PLAYER-RUN-LOGIC END --------------------


            // IF THE MAX AMOUNT OF ENEMIES TO SPAWN HAS NOT BEEN REACHED, CHECK IF ENEMIES SHOULD BE SPAWNED
            if (enemyModelsToSpawn.Count() > 0)
                AdvanceEnemySpawners();
            else
                UIManager.singleton.ActivateCurrentClockArrow(true);

            // ACTIVE FIRE LOGIC
            HandleActiveFireLogic();


            // ---------- BATTLE-COLLIDER-(OFFENSIVE HITBOXES)-LOGIC START --------------
            for (var i = battleColliders.Count - 1; i > -1; i--)
            {
                GameObject curBattleColliderGO = battleColliders[i];
                var curBattleColliderScript = curBattleColliderGO.GetComponent<BattleCollider>();

                if (curBattleColliderScript.hasContinuousMovement || !curBattleColliderScript.hasHadInitialPush)
                {
                    curBattleColliderGO.GetComponent<Rigidbody2D>().velocity = curBattleColliderScript.curDirection * curBattleColliderScript.curSpeed * Time.deltaTime;
                    curBattleColliderScript.hasHadInitialPush = true;
                }

                if (curBattleColliderScript.canCollideWithBCs)
                {
                    for (var j = battleColliders.Count - 1; j > -1; j--)
                    {
                        CheckBattleColliderCollisionsWithInput(
                            curBattleColliderGO,
                            curBattleColliderGO.transform,
                            curBattleColliderGO.GetComponent<BoxCollider2D>(),
                            false,
                            true);
                    }
                }

                // RUN SELF DESTROY COUNTDOWN, IF BELOW ZERO, DESTROY COLLIDER
                curBattleColliderScript.timeBeforeSelfDestroy -= Time.deltaTime;
                if (!curBattleColliderScript.infiniteDuration && curBattleColliderScript.timeBeforeSelfDestroy < 0)
                    BattleColliderSelfDestroy(curBattleColliderScript, i);
            }
            // ---------- BATTLE-COLLIDER-(OFFENSIVE HITBOXES)-LOGIC END --------------


            // ----- BATTLE-OBJECT-LOGIC START ------
            for (var i = battleObjects.Count - 1; i > -1; i--)
            {
                var curBattleObjectGameObject = battleObjects[i];
                var curBattleObjectTransform = curBattleObjectGameObject.transform;
                var curBattleObjectScript = curBattleObjectGameObject.GetComponent<BattleObject>();
                var battleObjectStatsDisplay = UIManager.singleton.GetStatsDisplayByBattleObject(curBattleObjectGameObject);
                var battleObjectCombatHitbox = curBattleObjectScript.combatHitbox.GetComponent<BoxCollider2D>();

                // --------- BATTLE-CHARACTER-LOGIC START ------------
                var curBattleCharacterScript = curBattleObjectGameObject.GetComponent<BattleCharacter>();
                if (curBattleCharacterScript != null)
                {
                    // --------- RANDOM TIMERS-START ------------
                    if (curBattleCharacterScript.meleeTowardsPlayerCooldown > 0)
                        curBattleCharacterScript.meleeTowardsPlayerCooldown -= Time.deltaTime;
                    else
                        curBattleCharacterScript.meleeTowardsPlayerCooldown = 0;
                    // --------- RANDOM TIMERS-START ------------


                    // ----------- STUN-LOGIC START ---------------
                    // DECREASE STUN COUNTDOWN IF STUNNED, IF COUNTDOWN IS BELOW ZERO THEN RESET TO 0
                    if (curBattleCharacterScript.remainingStunTime > 0)
                        curBattleCharacterScript.remainingStunTime -= Time.deltaTime;
                    else
                        curBattleCharacterScript.remainingStunTime = 0;

                    if (curBattleCharacterScript.knockbackMovementDisabledCountdown > 0)
                        curBattleCharacterScript.knockbackMovementDisabledCountdown -= Time.deltaTime;
                    else
                        curBattleCharacterScript.knockbackMovementDisabledCountdown = 0;

                    // ALLOW BATTLE CHARACTER TO MOVE AGAIN IF THEY FIT CRITERIA (ENEMY LOGIC)
                    if (curBattleCharacterScript.remainingStunTime <= 0 && curBattleCharacterScript.knockbackMovementDisabledCountdown <= 0)
                    {
                        // --------------------- ENEMY-AI-LOGIC START ------------------------------
                        var enemyModelScript = curBattleObjectGameObject.GetComponent<EnemyModel>();
                        if (!curBattleCharacterScript.isPlayer && enemyModelScript != null && enemyModelScript.enabled)
                        {
                            var isCurTargetInLineOfSight = false;
                            var curDistanceFromPlayer = Vector2.Distance(curBattleObjectTransform.position, player.transform.position);
                            var withinAgressiveDefenseArea = false;

                            // check if player is in line of sight for various reasons (not checked every time for efficiency)
                            if (enemyModelScript.aiMovementBehavior == AIMovementBehavior.agressiveDefense
                                || enemyModelScript.aiFiringBehavior != AIFiringBehavior.none)
                            {
                                isCurTargetInLineOfSight = IsTargetInLineOfSight(
                                    curBattleObjectGameObject,
                                    player,
                                    defaultEnemyLineOfSightLayermask);
                            }
                                
                            // stop an agressive defense enemy movement if they are close enough to the player and can get a shot at them
                            if (enemyModelScript.aiMovementBehavior == AIMovementBehavior.agressiveDefense
                                && curDistanceFromPlayer <= enemyModelScript.agressiveDefenseDistance
                                && isCurTargetInLineOfSight)
                                withinAgressiveDefenseArea = true;

                            // ---------- ALLOWING-ENEMY-MOVEMENT-LOGIC START ----------------- 
                            // ---------- FLYING-MOVEMENT-LOGIC START -------------
                            if (curBattleCharacterScript.isFlyer && !withinAgressiveDefenseArea)
                            {
                                var curBattleObjectRigidbody = curBattleObjectGameObject.GetComponent<Rigidbody2D>();
                                if (curBattleObjectRigidbody != null)
                                    curBattleObjectRigidbody.position = Vector2.MoveTowards(
                                        curBattleObjectTransform.position,
                                        player.transform.position,
                                        curBattleCharacterScript.walkSpeed * Time.deltaTime);
                            }
                            // ---------- FLYING-MOVEMENT-LOGIC END ---------------
                            else
                            {
                                if (!withinAgressiveDefenseArea)
                                    curBattleObjectGameObject.GetComponent<AILerp>().canMove = true;
                                else
                                    curBattleObjectGameObject.GetComponent<AILerp>().canMove = false;
                            }
                            // ---------- ALLOWING-ENEMY-MOVEMENT-LOGIC END -----------------


                            // ---------- ENEMY-FIRING-BEHAVIOR-LOGIC START ----------------- 
                            var enemyRangedAttackBCI = enemyModelScript.rangedAttackBCI;
                            if (enemyRangedAttackBCI != null)
                            {
                                var enemyRangedAttackBCIScript = enemyRangedAttackBCI.GetComponent<BattleColliderInstruction>();

                                if (enemyModelScript.aiFiringBehavior == AIFiringBehavior.fireOnSight)
                                {
                                    if (!enemyModelScript.isWindingUpRangedAttack)
                                    {
                                        if (isCurTargetInLineOfSight)   // function below will update ranged attack, but will also init it
                                            HandleEnemyRangedAttack(enemyModelScript, curBattleObjectTransform.position, enemyRangedAttackBCIScript);
                                    }
                                    else
                                        HandleEnemyRangedAttack(enemyModelScript, curBattleObjectTransform.position, enemyRangedAttackBCIScript);
                                }
                            }
                            // ---------- ENEMY-FIRING-BEHAVIOR-LOGIC START ----------------- 
                        }
                        // --------------------- ENEMY-AI-LOGIC END ------------------------------
                    }
                    // ----------- STUN-LOGIC END -----------


                    // ---------- POISON-LOGIC START ---------------
                    if (curBattleCharacterScript.curPoisonDamageCountdown > 0)
                        curBattleCharacterScript.curPoisonDamageCountdown -= Time.deltaTime;
                    else
                        curBattleCharacterScript.curPoisonDamageCountdown = 0;

                    if (curBattleCharacterScript.poisonDamageFrequency > 0 && curBattleCharacterScript.curPoisonDamageCountdown <= 0)
                    {
                        curBattleObjectScript.curHP -= 1;
                        if (curBattleObjectScript.curHP < 0)
                            curBattleObjectScript.curHP = 0;
                        curBattleCharacterScript.curPoisonDamageCountdown = curBattleCharacterScript.poisonDamageFrequency;
                    }
                    // ---------- POISON-LOGIC END ---------------


                    // ----------- ENEMY-TO-PLAYER-DAMAGE START ----------------

                    if (!isPlayerUsingMirage &&
                        battleObjectCombatHitbox.bounds.Intersects(player.GetComponent<BattleObject>().combatHitbox.GetComponent<BoxCollider2D>().bounds) &&
                        !curBattleCharacterScript.isPlayer)
                    {
                        HandleCombatCollision(player, curBattleObjectGameObject);
                    }
                    // ----------- ENEMY-TO-PLAYER-DAMAGE END ----------------
                }
                    
                // ----------- COLLISIONS-WITH-BATTLE-COLLIDER-CHECK LOGIC START ------------------
                if (!curBattleObjectScript.isFire)
                    CheckBattleColliderCollisionsWithInput(
                        curBattleObjectGameObject,
                        curBattleObjectTransform,
                        battleObjectCombatHitbox,
                        curBattleObjectScript.isPlayer,
                        false);
                // ----------- COLLISIONS-WITH-BATTLE-COLLIDER-CHECK LOGIC END --------------------


                // DISPLAY HP TEXT FOR BATTLE OBJECTS THAT DONT DISPLAY HP UNTIL THEY ARE UNDER THEIR MAX HP
                if (battleObjectStatsDisplay != null)
                {
                    var battleObjectStatsDisplayScript = battleObjectStatsDisplay.GetComponent<BattleObjectStatsDisplay>();

                    if (curBattleObjectScript.curHP < curBattleObjectScript.maxHP && battleObjectStatsDisplayScript.hpText.activeSelf == false)
                    {
                        battleObjectStatsDisplayScript.hpText.SetActive(true);
                    }
                }

                // DESTROY BATTLE OBJECT AND BATTLE OBJECT STATS DISPLAY IF HP IS 0 OR LESS
                if (curBattleObjectScript.curHP <= 0 && !curBattleObjectScript.isDead && !curBattleObjectScript.isPlayer)
                {
                    curBattleObjectScript.isDead = true;

                    var tileScript = curBattleObjectGameObject.GetComponent<Tile>();
                    // PERFORM LOGIC IF BATTLE OBJECT IS A CHARACTER OR BLOCK
                    if (tileScript != null && tileScript.isBlock == true
                        || tileScript == null)
                    {
                        // DESTROY STATS DISPLAY BATTLE OBJECT HAS ONE
                        if (battleObjectStatsDisplay != null)
                        {
                            UIManager.singleton.battleObjectStatsDisplays.Remove(battleObjectStatsDisplay);
                            Destroy(battleObjectStatsDisplay);
                        }
                        else
                            Debug.Log(curBattleObjectScript.gameObject.name + " : When a Battle Object died, there was no corrosponding stats display to remove from the stats display list.");

                        // IF BATTLE OBJECT IS BLOCK, CHANGE TO EMPTY TILE
                        if (tileScript != null && tileScript.isBlock == true)
                        {
                            GridManager.singleton.ChangeTileState(curBattleObjectGameObject, false);
                            AstarPath.active.Scan();
                        }
                        // IF BATTLE OBJECT IS NOT TILE, REMOVE FROM BATTLE OBJECTS AND DESTROY
                        else if (tileScript == null)
                        {
                            BattleManager.singleton.battleObjects.Remove(curBattleObjectGameObject);
                            Destroy(curBattleObjectGameObject);
                        }
                    }
                }
            }
            // ----- BATTLE-OBJECT-LOGIC END ------
        }

        public void ParsePlayerInputsForBattle(InputValues _inputValues)
        {
            // TOOL QUICKSELECT SET STATE (set "init" if the quickselect wasn't open on the last frame, and is now being opened this frame, to init appropriately later below)
            bool battleToolQuickSelectInit = false;
            if (!battleToolQuickSelectActive && _inputValues.quickselectOrMoreInfo)
                battleToolQuickSelectInit = true;
            battleToolQuickSelectActive = _inputValues.quickselectOrMoreInfo;

            // PLAYER RUNNING SET STATE
            BattleManager.singleton.playerIsRunning = ((
                !BattleManager.singleton.isPlayerUsingMirage &&
                _inputValues.runOrCursorSpeed) && 
                !BattleManager.singleton.isPlayerCurrentlyUsingTool &&
                BattleManager.singleton.curStamina > 0);

            // LEFT AND RIGHT TOOL SET STATE
            bool leftToolButtonActive = _inputValues.leftToolOrPageLeft;
            bool rightToolButtonActive = _inputValues.rightToolOrPageRight;

            // PLAYER MOVEMENT SPEED (WALK, RUN, MIRAGE)
            var movementSpeed = playerWalkSpeed;
            if (isPlayerUsingMirage)
                movementSpeed = playerMirageMovementSpeed;
            if (playerIsRunning)
                movementSpeed = playerRunSpeed;

            // MOVE PLAYER BASED ON DIRECTION AND SPEED
            Vector2 movement = new Vector2(_inputValues.horMovement * movementSpeed * Time.deltaTime, 
                _inputValues.vertMovement * movementSpeed * Time.deltaTime);
            player.GetComponent<Rigidbody2D>().velocity = movement;

            // PLAYER AIM (used in fighting, as well as for selecting in the tool quickselect)
            var rawInputAngle = Mathf.Atan2(-_inputValues.horAim, _inputValues.vertAim) * Mathf.Rad2Deg;
            rawInputAngle += 90;
            if (rawInputAngle < 0)
                rawInputAngle += 360;
            if (rawInputAngle > 360)
                rawInputAngle -= 360;
            // if initializing quickselect on this frame, and the player has no aim input, set the quickselect selector to a default angle
            if (battleToolQuickSelectInit && _inputValues.horAim == 0 && _inputValues.vertAim == 0)
                rawInputAngle = 90;

            // PERFORM LOGIC FOR WHEN QUICKSELECT ISNT OPEN (aim is used for aiming player direction)
            if (!battleToolQuickSelectActive)
            {
                // HIDE TOOL QUICKSELECT (player isn't holding quickselect button here)
                UIManager.singleton.toolQuickSelectMenuForBattle.SetActive(false);


                //DIRECTIONAL AIM (controlled by aim stick normally, but controlled by move stick during mirage)
                var dirAimHorInput = _inputValues.horAim;
                var dirAimVertInput = _inputValues.vertAim;
                if (isPlayerUsingMirage)
                {
                    dirAimHorInput = _inputValues.horMovement;
                    dirAimVertInput = _inputValues.vertMovement;
                }

                if (dirAimHorInput != 0 || dirAimVertInput != 0)
                {
                    curDirectionX = dirAimHorInput;
                    curDirectionY = dirAimVertInput;

                    var aimRotation = 0;

                    if (dirAimHorInput == 1 && dirAimVertInput == 1)
                        aimRotation = 315;
                    else if (dirAimHorInput == 1 && dirAimVertInput == 0)
                        aimRotation = 270;
                    else if (dirAimHorInput == 1 && dirAimVertInput == -1)
                        aimRotation = 225;
                    else if (dirAimHorInput == 0 && dirAimVertInput == -1)
                        aimRotation = 180;
                    else if (dirAimHorInput == -1 && dirAimVertInput == -1)
                        aimRotation = 135;
                    else if (dirAimHorInput == -1 && dirAimVertInput == 0)
                        aimRotation = 90;
                    else if (dirAimHorInput == -1 && dirAimVertInput == 1)
                        aimRotation = 45;

                    playerDirectionalAim.transform.localRotation = Quaternion.Euler(0, 0, aimRotation);
                }

                Vector2 directionalAimOffset = new Vector2(curDirectionX / 2, curDirectionY / 2 - 0.5f);
                playerDirectionalAim.transform.localPosition = playerDirectionalAimCenter + directionalAimOffset;

                // TOP DOWN AIM (ALT AIM)
                if (playerTopDownAim.activeInHierarchy)
                    UpdatePlayerTopDownAim(new Vector2(
                        _inputValues.horAim * playerTopDownAimMovementSpeed,
                        _inputValues.vertAim * playerTopDownAimMovementSpeed));

                //TOOLS
                if (!isPlayerUsingMirage)
                {
                    if (leftToolButtonActive)
                        InitToolUse(playerLeftTool);

                    if (rightToolButtonActive)
                        InitToolUse(playerRightTool);
                }
            }
            // PERFORM LOGIC FOR WHEN QUICKSELECT IS OPEN (aim is used for selecting in the quickselect)
            else
            {
                var toolQuickSelectMenuForBattle = UIManager.singleton.toolQuickSelectMenuForBattle;
                var toolQuickSelectMenuForBattleScript = toolQuickSelectMenuForBattle.GetComponent<ToolQuickSelectMenu>();
                toolQuickSelectMenuForBattle.SetActive(true);
                if (_inputValues.horAim != 0 || _inputValues.vertAim != 0)
                    toolQuickSelectMenuForBattleScript.selectionAngle = rawInputAngle;
                else if (battleToolQuickSelectInit)
                    toolQuickSelectMenuForBattleScript.selectionAngle = 90;

                var battleQuickSelectCurrentlySelectedTool = toolQuickSelectMenuForBattleScript.GetCurrentlySelectedTool();
                if (battleQuickSelectCurrentlySelectedTool != null)
                {
                    if (leftToolButtonActive)
                    {
                        if (battleQuickSelectCurrentlySelectedTool == playerRightTool)
                            SwapLeftAndRightTools();
                        else
                        {
                            playerLeftTool = battleQuickSelectCurrentlySelectedTool;
                        }
                    }

                    if (rightToolButtonActive)
                    {
                        if (battleQuickSelectCurrentlySelectedTool == playerLeftTool)
                            SwapLeftAndRightTools();
                        else
                        {
                            playerRightTool = battleQuickSelectCurrentlySelectedTool;
                        }
                    }

                    if (leftToolButtonActive || rightToolButtonActive)
                    {
                        SetPlayerTopDownAim();
                    }
                }
            }
        }

        public void InitToolUse (GameObject tool)
        {
            if (playerToolCompletionCountdown <= 0)
            {
                var toolScript = tool.GetComponent<Tool>();

                // APPLY BUFFS TO BASE COMBAT STATS
                var baseToolStatsScript = toolScript.combatStats.GetComponent<CombatStats>();
                var finalCombatStatsScript = Instantiate(baseToolStatsScript);
                if (playerAllBuffs.Count() > 0)
                    finalCombatStatsScript = AddOrRemoveCombatStats(finalCombatStatsScript, playerCurTotalBuff, false);
                
                float requiredResource = 0;
                if (!toolScript.isBottle)
                {
                    requiredResource = maxStamina;
                    if (toolScript.usesMana)
                        requiredResource = maxMana;
                    requiredResource /= finalCombatStatsScript.resourceEfficiency;
                }

                if (playerToolCompletionCountdown <= 0
                    && ((!toolScript.usesMana && curStamina >= requiredResource)
                    || ((toolScript.usesMana && curMana >= requiredResource)))
                    && (!toolScript.isAmmoBasedTool || toolScript.curAmmoClip > 0 || toolScript.isReloadTool))
                {
                    if (!toolScript.usesMana)
                        curStamina -= requiredResource;
                    else
                        curMana -= requiredResource;

                    playerToolInUse = tool;
                    playerToolInUseCombatStats = finalCombatStatsScript;
                    isPlayerCurrentlyUsingTool = true;

                    // tool usage speed
                    var toolSpeedFinal = bottleDrinkDuration;
                    if (!toolScript.isBottle)
                    {
                        float currentToolSpeedStat = finalCombatStatsScript.useSpeed;
                        if (currentToolSpeedStat <= 0)
                            currentToolSpeedStat = 0.01f;
                        toolSpeedFinal = baseToolUseSpeed / currentToolSpeedStat;
                    }
                    currentToolSpeedFinal = toolSpeedFinal;
                    playerToolCompletionCountdown = toolSpeedFinal;
                    UIManager.singleton.InitCountdownRing(player, toolSpeedFinal, toolSpeedFinal, UIManager.singleton.CRtoolColor);
                }
                else
                    Destroy(finalCombatStatsScript);
            }
        }

        public void InitBattleCharacter(bool _isPlayer, Vector2 _spawnLocation, EnemyModel _enemyModel = null)
        {
            var characterGameObject = Instantiate(prefabBattleCharacter);
            battleObjects.Add(characterGameObject);

            if (_isPlayer == true)
            {
                var characterScript = characterGameObject.GetComponent<BattleCharacter>();
                var characterAILerp = characterGameObject.GetComponent<AILerp>();
                var characterAIDestinationSetter = characterGameObject.GetComponent<AIDestinationSetter>();
                var characterSpriteRenderer = characterGameObject.GetComponent<SpriteRenderer>();
                var battleObjectScript = characterGameObject.GetComponent<BattleObject>();
                var edgeCollider = characterGameObject.GetComponent<EdgeCollider2D>();
                var combatHitbox = battleObjectScript.combatHitbox.GetComponent<BoxCollider2D>();

                characterGameObject.name = "Player";
                characterScript.isPlayer = true;
                battleObjectScript.isPlayer = true;
                player = characterGameObject;
                characterAILerp.enabled = false;
                characterSpriteRenderer.sprite = playerSprite;
                battleObjectScript.maxHP = maxHealth;

                // PLAYER DIRECTIONAL AIM
                var directionalAimGameObject = Instantiate(prefabDirectionalAim);
                playerDirectionalAim = directionalAimGameObject;
                playerDirectionalAim.transform.parent = player.transform;
                playerDirectionalAim.transform.localRotation = Quaternion.Euler(0, 0, 180);

                // PLAYER TOP DOWN AIM
                var topDownAimGameObject = Instantiate(prefabTopDownAim);
                playerTopDownAim = topDownAimGameObject;
                playerTopDownAim.transform.parent = player.transform;
                UIManager.singleton.InitPlayerTopDownAimBoundary();
                SetPlayerTopDownAim();

                // HITBOXES
                edgeCollider.points = playerMovementEdgeColliderPoints;
                combatHitbox.size = playerCombatHitboxDimensions;
                combatHitbox.offset = playerCombatHitboxOffset;
                combatHitbox.gameObject.layer = GameManager.layermaskToLayer(playerCombatHitboxLayer);

                battleObjectScript.curHP = battleObjectScript.maxHP;
            }
            else
                InitEnemy(characterGameObject, _enemyModel);

            characterGameObject.GetComponent<Rigidbody2D>().freezeRotation = true;
            characterGameObject.transform.position = _spawnLocation;
        }

        public void UseTool(GameObject _toolGameObject, CombatStats _toolCombatStats)
        {
            var toolScript = _toolGameObject.GetComponent<Tool>();
            var toolBattleColliderInstructions = toolScript.battleColliderInstructionPrefabs;

            // if tool doesn't have to reload, then perform function, otherwise use this action to reload clip
            if (!toolScript.isAmmoBasedTool ||
                toolScript.curAmmoClip > 0)
            {
                // ACTIVATE MIRAGE SHEET ABILITY IF APPLICABLE
                if (toolScript.isMirageSheet)
                    ActivateMirage();

                // ACTIVATE BOTTLE ABILITY IF APPLICABLE
                if (toolScript.isBottle)
                    InitPlayerBuff(_toolCombatStats);

                // if tool has a battle collider instruction, perform instructions
                if (toolBattleColliderInstructions.Count() > 0)
                {
                    var battleColliderInstructionsScript = toolBattleColliderInstructions[0].GetComponent<BattleColliderInstruction>();
                    Vector2 positionToSpawnBattleCollider = playerDirectionalAim.transform.position;
                    if (battleColliderInstructionsScript.usesAltAimReticule)
                        positionToSpawnBattleCollider = playerTopDownAim.transform.position;
                    float aimCenterAngle = playerDirectionalAim.transform.eulerAngles.z + 90;

                    InitBattleColliderInstruction(
                        battleColliderInstructionsScript,
                        positionToSpawnBattleCollider,
                        GameManager.radianToDirection(aimCenterAngle, true),
                        aimCenterAngle,
                        _toolCombatStats,
                        toolScript);
                }

                // EXPEND AMMO IF APPLICABLE
                if (toolScript.isAmmoBasedTool && toolScript.curAmmoClip > 0)
                    toolScript.curAmmoClip -= 1;
            }
            // if tool can be reloaded, and has an empty clip, reload
            else if (toolScript.isReloadTool)
            {
                Debug.Log("used action to reload");
                toolScript.curAmmoClip = toolScript.maxAmmoCLip;
            }
        }

        public void SwapLeftAndRightTools()
        {
            var playerLeftToolTempForSwap = playerLeftTool;
            playerLeftTool = playerRightTool;
            playerRightTool = playerLeftToolTempForSwap;
        }

        public void AdvanceEnemySpawners()
        {
            var gridManager = GridManager.singleton;
            List<GameObject> enemySpawnerTiles = gridManager.GetAllSpawnerTiles();
            GameObject currentEnemySpawnerTile = enemySpawnerTiles[enemySpawnerTileIndex];

            // HANDLE CLOCK ARROWS
            UIManager.singleton.ActivateCurrentClockArrow();

            enemySpawnCountdown += Time.deltaTime;
            if (enemySpawnCountdown >= baseTimeToSpawnEnemy)
            {
                GameObject spawnerTile = enemySpawnerTiles[enemySpawnerTileIndex];

                // spawn enemy and remove from spawn list
                InitBattleCharacter(
                    false,
                    spawnerTile.transform.position,
                    enemyModelsToSpawn[0].GetComponent<EnemyModel>());

                enemyModelsToSpawn.RemoveAt(0);

                // update enemy graphic in spawner gate
                var curSpawnerGates = UIManager.singleton.curSpawnerGateSystem.GetComponent<SpawnerGateSystem>().spawnerGates;;
                var curSpawnerGate = curSpawnerGates[enemySpawnerTileIndex];
                if (curSpawnerGate != null)
                {
                    if (enemyModelsToSpawn.Count() >= enemySpawnerTiles.Count())
                    {
                        var nextEnemyModelIndex = enemySpawnerTiles.Count() - 1;
                        if (nextEnemyModelIndex > enemyModelsToSpawn.Count() - 1)
                            nextEnemyModelIndex = enemyModelsToSpawn.Count() - 1;
                        UIManager.singleton.UpdateSpawnerGateEnemy(true, curSpawnerGate, enemyModelsToSpawn[nextEnemyModelIndex]);
                    }
                    else
                        UIManager.singleton.UpdateSpawnerGateEnemy(false, curSpawnerGate);
                }
                else
                    Debug.Log("A spawner gate had no linked spawner tile when trying to update its visuals.");

                enemySpawnerTileIndex += 1;
                if (enemySpawnerTileIndex > enemySpawnerTiles.Count() - 1)
                    enemySpawnerTileIndex = 0;

                enemySpawnCountdown = 0;
            }
        }

        public void HandleCombatCollision(
            GameObject _targetGameObject,
            GameObject _offensiveGameObject,
            Vector2 _hitPosition = new Vector2(),
            CombatStats _usedCombatStatsScript = null,
            Tool _usedToolScript = null)
        {
            var targetBattleObjectScript = _targetGameObject.GetComponent<BattleObject>();
            if (targetBattleObjectScript != null && !targetBattleObjectScript.isInvincible)
            {
                var targetBattleCharacterScript = _targetGameObject.GetComponent<BattleCharacter>();
                var offensiveBattleCharacterScript = _offensiveGameObject.GetComponent<BattleCharacter>();

                // DEFAULTS TO BE OVERIDDEN IF APPROPRIATE
                float damage = defaultEnemyDamage;
                if (offensiveBattleCharacterScript.hasCustomDamage)
                    damage = offensiveBattleCharacterScript.customDamage;
                float curKnockbackForce = defaultKnockbackForce;
                if (offensiveBattleCharacterScript.hasCustomKnockback)
                    curKnockbackForce = offensiveBattleCharacterScript.customKnockback;
                float curMeleeTowardsCooldownTime = defaultMeleeTowardsPlayerCooldownTime;
                if (offensiveBattleCharacterScript.hasCustomMeleeCooldown)
                    curMeleeTowardsCooldownTime = offensiveBattleCharacterScript.customMeleeCooldown;

                if (_usedToolScript != null)
                {
                    float curCombatStatsReduction = _usedToolScript.combatStatsReduction == 0 ? 1 : _usedToolScript.combatStatsReduction;
                    damage = _usedCombatStatsScript.damage * curCombatStatsReduction;

                    // CHANGE PUSHBACK TO TOOL'S STAT
                    curKnockbackForce = _usedToolScript.knockbackForce;

                    // IF TARGET ISN'T A TILE, APPLY STATUS EFFECTS
                    if (_targetGameObject.GetComponent<Tile>() == null)
                    {
                        // ------- STUN-APPLY-START ----------
                        targetBattleCharacterScript.remainingStunTime += _usedCombatStatsScript.stun * curCombatStatsReduction;
                        if (targetBattleCharacterScript.remainingStunTime > 0 && _usedCombatStatsScript.stun > 0)
                        {
                            UIManager.singleton.InitCountdownRing(_targetGameObject, 
                                targetBattleCharacterScript.remainingStunTime,
                                targetBattleCharacterScript.remainingStunTime,
                                UIManager.singleton.CRstunColor,
                                false,
                                CountdownRingType.stun);
                            _targetGameObject.GetComponent<AILerp>().canMove = false;
                        }
                        // ------- STUN-APPLY-END ----------

                        // ------- POISON-APPLY-START ----------
                        if (_usedCombatStatsScript.poison > 0)
                        {
                            targetBattleCharacterScript.basePoisonPoints += _usedCombatStatsScript.poison * curCombatStatsReduction;

                            float newPoisonFrequency = poisonFrequencyMaxTime / targetBattleCharacterScript.basePoisonPoints;

                            // IF TARGET HASN'T BEEN POISONED, DO SIMPLE ADDITION
                            if (targetBattleCharacterScript.poisonDamageFrequency <= 0)
                            {
                                targetBattleCharacterScript.poisonDamageFrequency = newPoisonFrequency;
                                targetBattleCharacterScript.curPoisonDamageCountdown = targetBattleCharacterScript.poisonDamageFrequency;
                            }
                            // IF TARGET HAS BEEN POISONED, PROPORTIONALLY MODIFY CURRENT POISON PROGRESS
                            else
                            {
                                var originalPoisonFreq = targetBattleCharacterScript.poisonDamageFrequency;
                                targetBattleCharacterScript.poisonDamageFrequency = newPoisonFrequency;
                                // increase poison damage frequency while adjusting the current progress towards that goal to be proportional to the new goal
                                var newCurPoisonCountdown = 
                                    (targetBattleCharacterScript.curPoisonDamageCountdown * targetBattleCharacterScript.poisonDamageFrequency)
                                    / originalPoisonFreq;
                                targetBattleCharacterScript.curPoisonDamageCountdown = newCurPoisonCountdown;
                            }

                            UIManager.singleton.InitCountdownRing(_targetGameObject, 
                                targetBattleCharacterScript.curPoisonDamageCountdown, 
                                targetBattleCharacterScript.poisonDamageFrequency, 
                                UIManager.singleton.CRpoisonColor,
                                true,
                                CountdownRingType.poison);
                        }
                        // ------- POISON-APPLY-END ----------
                    }
                }

                // DAMAGE (IF BY AN AI, THEY MUST NOT HAVE HIT WITH MELEE IN THE PAST X SECONDS)
                if (offensiveBattleCharacterScript.meleeTowardsPlayerCooldown <= 0 && offensiveBattleCharacterScript.remainingStunTime <= 0)
                {
                    var originalHitBattleObjectCurHP = targetBattleObjectScript.curHP;
                    targetBattleObjectScript.curHP -= damage;

                    // PLAYER FUNTION: GAIN MP WITH BLEED WEAPON
                    if (_usedToolScript != null && _usedToolScript.isBleed)
                    {
                        curMana += originalHitBattleObjectCurHP - targetBattleObjectScript.curHP;
                        if (curMana > maxMana)
                            curMana = maxMana;
                    }

                    // KNOCKBACK
                    if (targetBattleObjectScript.isPlayer ||
                        (_usedToolScript != null && curKnockbackForce > 0))
                        HandleKnockback(_targetGameObject, _offensiveGameObject.transform.position, curKnockbackForce);

                    // IF AI IS ATTACKING, SET COOLDOWN BEFORE THEY CAN HIT WITH MELEE AGAIN
                    if (!offensiveBattleCharacterScript.isPlayer)
                        offensiveBattleCharacterScript.meleeTowardsPlayerCooldown = curMeleeTowardsCooldownTime;
                }
            }
        }

        public void HandleKnockback(GameObject _battleObjectToKnockback, Vector2 _knockbackOrigin, float pushForce)
        {
            var battleObjectScript = _battleObjectToKnockback.GetComponent<BattleObject>();
            var battleObjectRigidbody = _battleObjectToKnockback.gameObject.GetComponent<Rigidbody2D>();

            if (battleObjectRigidbody != null)
            {
                Vector2 pos = _battleObjectToKnockback.transform.position;
                Vector2 dir = (pos - _knockbackOrigin).normalized;

                battleObjectRigidbody.velocity = (dir * pushForce);

                if (!battleObjectScript.isPlayer)
                {
                    var battleObjectAILerp = _battleObjectToKnockback.GetComponent<AILerp>();
                    Debug.Log(battleObjectAILerp);
                    if (battleObjectAILerp != null)
                    {
                        battleObjectAILerp.canMove = false;
                        _battleObjectToKnockback.gameObject.GetComponent<BattleCharacter>().knockbackMovementDisabledCountdown = defualtKnockbackTimeLength;
                    }
                }
            }
        }

        public GameObject GetPlayer()
        {
            return battleObjects.FirstOrDefault(x => x.GetComponent<BattleObject>().isPlayer == true);
        }

        public void HandleActiveFireLogic()
        {
            // ------------ FIRE SCAN START ---------------
            if (unscannedFires.Count() > 0)
            {
                if (!isFireScanCountdownStarted)
                {
                    curFireScanCountdown = fireScanDelay;
                    isFireScanCountdownStarted = true;
                }

                curFireScanCountdown -= Time.deltaTime;

                if (curFireScanCountdown <= 0)
                {
                    unscannedFires.Clear();
                    AstarPath.active.Scan();
                    isFireScanCountdownStarted = false;
                }
            }
            // ------------ FIRE SCAN END ---------------

            for (var i = activeFires.Count - 1; i > -1; i--)
            {
                GameObject curFireGameObject = activeFires[i];
                var curFireBattleObjectScript = curFireGameObject.GetComponent<BattleObject>();
                var curFireCombatHitbox = curFireGameObject.GetComponent<BoxCollider2D>();

                // DECREASE FIRE LIFE COUNTDOWN, DESTROY AT 0
                if (curFireBattleObjectScript.curHP > 0)
                    curFireBattleObjectScript.curHP -= Time.deltaTime;
                else
                {
                    GameObject fireStatsDisplay = UIManager.singleton.GetStatsDisplayByBattleObject(curFireGameObject);
                    UIManager.singleton.battleObjectStatsDisplays.Remove(fireStatsDisplay);
                    Destroy(fireStatsDisplay);
                    curFireBattleObjectScript.curHP = 0;
                    battleObjects.Remove(curFireGameObject);
                    activeFires.RemoveAt(i);
                    Destroy(curFireGameObject);
                    AstarPath.active.Scan();
                }
            }
        }

        public void SetPlayerTopDownAim()
        {
            if (DoesToolUseAltAim(playerLeftTool) || DoesToolUseAltAim(playerRightTool))
            {
                playerTopDownAim.SetActive(true);
                UIManager.singleton.topDownAimBoundary.SetActive(true);
                UpdatePlayerTopDownAim(Vector2.zero, true);
            }
            else if (playerTopDownAim.activeInHierarchy)
            {
                playerTopDownAim.transform.localPosition = playerTopDownAimOffset;
                playerTopDownAim.SetActive(false);
                if (UIManager.singleton.topDownAimBoundary)
                    UIManager.singleton.topDownAimBoundary.SetActive(false);
                playerTopDownCurAimDistance = 0;
            }
        }

        public void UpdatePlayerTopDownAim(Vector2 projectedMovement, bool isCenter = false)
        {
            Transform playerTransform = player.transform;
            Vector2 playerPosition = playerTransform.position;
            Transform playerTopDownAimTransform = playerTopDownAim.transform;
            Vector2 playerTopDownAimPosition = playerTopDownAimTransform.position;

            if (!isCenter)
            {
                // get distance of projected aim-position from player, if it exceeds max distance allowed, set to max distance
                Vector2 projectedAimPosition = playerTopDownAimPosition + projectedMovement;
                float aimDistanceFromPlayer = Vector2.Distance(projectedAimPosition, playerPosition);
                playerTopDownCurAimDistance = aimDistanceFromPlayer;
                if (aimDistanceFromPlayer > playerTopDownAimMaxDistance)
                    playerTopDownCurAimDistance = playerTopDownAimMaxDistance;

                // either way, set the angle of the aim from the player to the angle of the current projected aim position, only update angle when there is input
                if (projectedMovement != Vector2.zero)
                {
                    Vector2 dirFromPlayer = playerPosition - projectedAimPosition;
                    playerTopDownCurAimAngleRadians = Mathf.Atan2(-dirFromPlayer.y, -dirFromPlayer.x);
                }
            }
            else
            {
                playerTopDownCurAimAngleRadians = 0;
                playerTopDownCurAimDistance = 0;
            }

            // move player top down aim based on angle and distance from player, rather than a set distance, in order to enforce a circular boundary
            Vector2 directionToMoveAim = new Vector2((float)Mathf.Cos(playerTopDownCurAimAngleRadians), (float)Mathf.Sin(playerTopDownCurAimAngleRadians));
            playerTopDownAimTransform.position = playerPosition + (directionToMoveAim  * playerTopDownCurAimDistance);
        }

        public bool DoesToolUseAltAim(GameObject toolToCheckForAltAimGO)
        {
            return toolToCheckForAltAimGO.GetComponent<Tool>().battleColliderInstructionPrefabs.Any(x => x.GetComponent<BattleColliderInstruction>().usesAltAimReticule);
        }

        public void HandleAttackCompletion(
            Transform _hitBattleObjectTransform, 
            Vector2 _hitPosition = new Vector2(), 
            CombatStats _usedCombatStatsScript = null,
            Tool _usedToolScript = null)
        {
            // DEAL DAMAGE AND APPLICABLE EFFECTS IF HIT
            if (_hitBattleObjectTransform != null)
                HandleCombatCollision(_hitBattleObjectTransform.gameObject, GetPlayer(), _hitPosition, _usedCombatStatsScript, _usedToolScript);
        }

        public void SpawnFires(Vector2 _hitPosition, CombatStats _usedCombatStatsScript, Tool _usedToolScript, BattleCollider _battleCollider = null)
        {
            if (_usedCombatStatsScript.fire > 0)
            {
                int curFireGridCount = 1;
                float fireGridSpacialSize = 1;
                if (_battleCollider != null)
                {
                    curFireGridCount = _battleCollider.fireGridCount;
                    fireGridSpacialSize = _battleCollider.fireGridSpacialSize;
                }
                float leftSideSpawnX = _hitPosition.x - (fireGridSpacialSize / 2);
                float topSideSpawnY = _hitPosition.y + (fireGridSpacialSize / 2);
                float spawnPadding = fireGridSpacialSize;
                Vector2 curFireSpawnPoint = new Vector2(leftSideSpawnX, topSideSpawnY);

                if (curFireGridCount == 1)
                    curFireSpawnPoint = _hitPosition;

                for (var i = 0; i < curFireGridCount; i++)
                {
                    for (var j = 0; j < curFireGridCount; j++)
                    {
                        var newFireGameObject = Instantiate(prefabFire);
                        var newFireBattleObjectScript = newFireGameObject.GetComponent<BattleObject>();

                        activeFires.Add(newFireGameObject);
                        battleObjects.Add(newFireGameObject);

                        newFireGameObject.transform.position = curFireSpawnPoint;
                        newFireBattleObjectScript.maxHP = _usedCombatStatsScript.fire *
                        (_usedToolScript.combatStatsReduction == 0 ? 1 : _usedToolScript.combatStatsReduction);
                        newFireBattleObjectScript.curHP = newFireBattleObjectScript.maxHP;

                        UIManager.singleton.InitBattleObjectStatsDisplay(newFireGameObject);

                        // put fire in docket to be scanned in a small amount of time, instead of scanning every time a fire is created
                        unscannedFires.Add(newFireGameObject);

                        curFireSpawnPoint.x += spawnPadding;
                    }

                    curFireSpawnPoint.x = leftSideSpawnX;
                    curFireSpawnPoint.y -= spawnPadding;
                }
            }
        }

        public GameObject InitHitbox(
            BattleColliderInstruction _battleColliderInstructionsScript,
            Vector2 _spawnPosition,
            Vector2 _spawnDirection,
            CombatStats _usedCombatStats = null,
            Tool _usedToolScript = null)
        {
            var battleColliderGameObject = Instantiate(prefabSmallHitbox);
            battleColliders.Add(battleColliderGameObject);

            Rigidbody2D battleColliderRigidbody = battleColliderGameObject.AddComponent<Rigidbody2D>();
            battleColliderRigidbody.freezeRotation = true;
            battleColliderRigidbody.mass = 5;
            battleColliderRigidbody.angularDrag = 0.0f;
            battleColliderRigidbody.drag = 5;
            battleColliderRigidbody.sharedMaterial = physicsMaterialDefaultBattleCollider;

            if (!_battleColliderInstructionsScript.usesEdgeCollider)
            {
                EdgeCollider2D curEdgeCollider = battleColliderGameObject.GetComponent<EdgeCollider2D>();
                if (curEdgeCollider != null)
                    curEdgeCollider.enabled = false;
            }
                
            battleColliderGameObject.transform.position = _spawnPosition + (_spawnDirection * _battleColliderInstructionsScript.forwardDistanceToSpawn);
            battleColliderGameObject.transform.localScale = _battleColliderInstructionsScript.hitboxScale;
            if (_battleColliderInstructionsScript.isChildOfPlayerDirectionalAim)
            {
                battleColliderGameObject.transform.parent = playerDirectionalAim.transform;
                battleColliderGameObject.transform.localPosition = new Vector2(0, .07f);
                Destroy(battleColliderRigidbody);
            }

            var battleColliderScript = battleColliderGameObject.GetComponent<BattleCollider>();
            battleColliderScript.destroySelfOnCollision = _battleColliderInstructionsScript.destroySelfOnCollision;

            battleColliderScript.isShield = _battleColliderInstructionsScript.isShield;
            battleColliderScript.isDestroyedByShield = _battleColliderInstructionsScript.isDestroyedByShield;

            battleColliderScript.canCollideWithBCs = _battleColliderInstructionsScript.canCollideWithBCs;

            battleColliderScript.infiniteDuration = _battleColliderInstructionsScript.infiniteDuration;
            battleColliderScript.timeBeforeSelfDestroy = _battleColliderInstructionsScript.duration;
            battleColliderScript.curSpeed = _battleColliderInstructionsScript.startingSpeed;
            battleColliderScript.hasContinuousMovement = _battleColliderInstructionsScript.hasContinuousMovement;
            battleColliderScript.hasContactEffects = _battleColliderInstructionsScript.hasContactEffects;
            battleColliderScript.prefabBattleColliderInstructionOnSelfDestroy = _battleColliderInstructionsScript.prefabBattleColliderInstructionOnSelfDestroy;
            battleColliderScript.playerIsImmune = _battleColliderInstructionsScript.playerIsImmune;

            battleColliderScript.fireGridCount = _battleColliderInstructionsScript.fireGridCount;
            battleColliderScript.fireGridSpacialSize = _battleColliderInstructionsScript.fireGridSpacialSize;
            battleColliderScript.spawnsFireOnSelfDestroy = _battleColliderInstructionsScript.spawnsFireOnSelfDestroy;

            battleColliderScript.curDirection = _spawnDirection;

            if (_usedToolScript != null)
            {
                battleColliderScript.combatStats = _usedCombatStats;
                battleColliderScript.toolThisWasCreatedFrom = _usedToolScript;
            }

            battleColliderGameObject.layer = GameManager.layermaskToLayer(_battleColliderInstructionsScript.layerMask);

            return battleColliderGameObject;
        }

        public void BattleColliderSelfDestroy(BattleCollider _curBattleColliderScript, int battleColliderIndex)
        {
            if (_curBattleColliderScript.spawnsFireOnSelfDestroy)
            {
                SpawnFires(
                    _curBattleColliderScript.transform.position,
                    _curBattleColliderScript.combatStats,
                    _curBattleColliderScript.toolThisWasCreatedFrom,
                    _curBattleColliderScript);
            }

            if (_curBattleColliderScript.prefabBattleColliderInstructionOnSelfDestroy != null)
            {
                InitHitbox(
                    _curBattleColliderScript.prefabBattleColliderInstructionOnSelfDestroy.GetComponent<BattleColliderInstruction>(),
                    _curBattleColliderScript.transform.position,
                    Vector2.zero,
                    _curBattleColliderScript.combatStats,
                    _curBattleColliderScript.toolThisWasCreatedFrom);
            }

            battleColliders.RemoveAt(battleColliderIndex);
            Destroy(_curBattleColliderScript.gameObject);
        }

        public void InitEnemy(GameObject _battleCharacter, EnemyModel _enemyModel)
        {
            var BCEnemyModelScript = _battleCharacter.GetComponent<EnemyModel>();
            var battleCharacterScript = _battleCharacter.GetComponent<BattleCharacter>();
            var battleObjectScript = _battleCharacter.GetComponent<BattleObject>();
            var enemyAILerp = _battleCharacter.GetComponent<AILerp>();
            var enemyAIDestinationSetter = _battleCharacter.GetComponent<AIDestinationSetter>();
            var enemySpriteRenderer = _battleCharacter.GetComponent<SpriteRenderer>();
            var edgeCollider = _battleCharacter.GetComponent<EdgeCollider2D>();
            var combatHitbox = battleObjectScript.combatHitbox.GetComponent<BoxCollider2D>();

            // ALTER MODELS (for possible future reference)
            battleCharacterScript.sprite = _enemyModel.sprite;
            battleCharacterScript.walkSpeed = _enemyModel.walkSpeed;
            battleCharacterScript.isFlyer = _enemyModel.isFlyer;

            battleObjectScript.maxHP = _enemyModel.maxHP;
            battleObjectScript.curHP = battleObjectScript.maxHP;

            BCEnemyModelScript.rangedAttackBCI = _enemyModel.rangedAttackBCI;
            BCEnemyModelScript.rangedAttackWindupTime = _enemyModel.rangedAttackWindupTime;
            BCEnemyModelScript.rangedAttackDamage = _enemyModel.rangedAttackDamage;
            BCEnemyModelScript.aiFiringBehavior = _enemyModel.aiFiringBehavior;
            BCEnemyModelScript.aiMovementBehavior = _enemyModel.aiMovementBehavior;
            BCEnemyModelScript.agressiveDefenseDistance = _enemyModel.agressiveDefenseDistance;

            // DIRECTLY ALTER STATS
            enemyAILerp.speed = _enemyModel.walkSpeed;
            enemyAIDestinationSetter.target = player.transform;
            enemySpriteRenderer.sprite = _enemyModel.sprite;

            // UI
            UIManager.singleton.InitBattleObjectStatsDisplay(_battleCharacter);

            // HITBOXES
            // if a flying enemy, disabled movement hitboxes and pathfinding components
            if (battleCharacterScript.isFlyer)
            {
                enemyAILerp.enabled = false;
                enemyAIDestinationSetter.enabled = false;
                _battleCharacter.layer = GameManager.layermaskToLayer(flyerHitboxLayer);
            }
            else
                edgeCollider.points = defaultEnemyMovementEdgeColliderPoints;

            combatHitbox.size = defaultEnemyCombatHitboxDimensions;
            combatHitbox.gameObject.layer = GameManager.layermaskToLayer(enemyCombatHitboxLayer);
            combatHitbox.size = defaultEnemyCombatHitboxDimensions;
        }

        public void InitBattleColliderInstruction(
            BattleColliderInstruction _battleColliderInstructionScript,
            Vector2 _spawnPosition,
            Vector2 _spawnDirection,
            float _optionalSpawnDirectionAngle = 0,
            CombatStats _usedCombatStatsScript = null,
            Tool _usedToolScript = null)
        {
            if (_battleColliderInstructionScript.isRaycast)
            {
                int raycastsToCreateCount = _battleColliderInstructionScript.raycastCount;
                if (raycastsToCreateCount <= 0)
                    raycastsToCreateCount = 1;

                float curMultiRaycastAngle = 0;
                if (raycastsToCreateCount > 1)
                    curMultiRaycastAngle = _optionalSpawnDirectionAngle - (_battleColliderInstructionScript.multiRaycastSpreadAngle / 2);

                Vector2 curRaycastSpawnDirection = _spawnDirection;

                for (var i = 0; i < raycastsToCreateCount; i++)
                {
                    // if spawning multiple raycasts, generate spawn direction from changing spawn angle (reassigned at bottom of loop)
                    curRaycastSpawnDirection = _spawnDirection;
                    if (raycastsToCreateCount > 1)
                        curRaycastSpawnDirection = GameManager.radianToDirection(curMultiRaycastAngle, true);

                    // RAYCAST
                    var curRaycastLayerMask = _battleColliderInstructionScript.layerMask;
                    if (isPlayerUsingMirage)
                        curRaycastLayerMask = GameManager.RemoveFromLayerMask(curRaycastLayerMask, playerCombatHitboxLayer);

                    RaycastHit2D hit = Physics2D.Raycast(
                        _spawnPosition,
                        curRaycastSpawnDirection,
                        _battleColliderInstructionScript.raycastLength,
                        curRaycastLayerMask);
                    Vector2 raycastEndLocation = hit.point;
                    if (hit.collider == null)
                        raycastEndLocation = _spawnPosition +
                            (curRaycastSpawnDirection * _battleColliderInstructionScript.raycastLength);

                    // RENDER RAYCAST LINE
                    var lineRenderer = Instantiate(prefabLineRenderer);
                    var lineRendererScript = lineRenderer.GetComponent<LineRenderer>();
                    UIManager.singleton.customLineRenderers.Add(lineRenderer);
                    Vector3[] positions = new Vector3[]
                        { 
                            new Vector3(_spawnPosition.x, _spawnPosition.y, -2),
                            new Vector3(raycastEndLocation.x, raycastEndLocation.y, -2)
                        };
                    lineRendererScript.SetPositions(positions);

                    // PERFORM HIT LOGIC WHETHER A HIT RETURNS AN OBJECT OR NOT (AS LONG AS THE COLLIDER DOESN'T INEXPLICABLY EQUAL NULL)
                    if (hit.collider != null)
                        HandleAttackCompletion(hit.collider.transform.parent, hit.point, _usedCombatStatsScript, _usedToolScript);

                    // SPAWN FIRE WHETHER ATTACK HITS OR NOT
                    if (_usedCombatStatsScript != null && _usedToolScript != null)
                        SpawnFires(raycastEndLocation, _usedCombatStatsScript, _usedToolScript);

                    // ADVANCE MULTI RAYCAST SPAWN ANGLE FOR MULTIPLE RAYCAST SPAWN
                    curMultiRaycastAngle += (_battleColliderInstructionScript.multiRaycastSpreadAngle / (_battleColliderInstructionScript.raycastCount - 1));
                }
            }
            else
            {
                // if a fire mirage sheet was used, spawn a fire at the player's position on usage
                if (_usedToolScript != null && _usedToolScript.isMirageSheet && _usedCombatStatsScript.fire > 0)
                    SpawnFires(
                        player.transform.position,
                        _usedCombatStatsScript,
                        _usedToolScript);

                var curNewHitbox = InitHitbox(
                    _battleColliderInstructionScript,
                    _spawnPosition,
                    _spawnDirection,
                    _usedCombatStatsScript,
                    _usedToolScript);

                if (isPlayerUsingMirage)
                    playerMirageBC = curNewHitbox;
            }
        }

        public bool IsTargetInLineOfSight (GameObject _sourceGO, GameObject _targetGO, LayerMask _layerMask)
        {
            Vector2 sourceTransformPosition = _sourceGO.transform.position;
            var targetTransform = _targetGO.transform;
            Vector2 directionTowardsTarget = GameManager.getDirectionTowardsPosition(sourceTransformPosition, targetTransform.position);

            RaycastHit2D hit = Physics2D.Raycast(
                sourceTransformPosition + (directionTowardsTarget * 0.8f),
                directionTowardsTarget,
                300,
                _layerMask);

            var output = false;
            if (hit.collider != null)
            {
                if (hit.collider.transform != null)
                {
                    if (hit.collider.transform.parent != null)
                        output = hit.collider.transform.parent.gameObject == _targetGO;
                }
            }
            return output;
        }

        public void HandleEnemyRangedAttack(EnemyModel _enemyModelScript, Vector2 _enemyPosition, BattleColliderInstruction _rangedBCIScript)
        {
            if (_enemyModelScript.isWindingUpRangedAttack)
            {
                if (_enemyModelScript.rangedAttackCurWindupTimeRem <= 0)
                {
                    _enemyModelScript.isWindingUpRangedAttack = false;

                    var curDir = GameManager.getDirectionTowardsPosition(_enemyPosition, player.transform.position);
                    Vector2 curPos = _enemyPosition; 

                    InitBattleColliderInstruction(
                        _rangedBCIScript,
                        curPos + (curDir * _rangedBCIScript.forwardDistanceToSpawn),
                        curDir);
                }
                else
                {
                    _enemyModelScript.rangedAttackCurWindupTimeRem -= Time.deltaTime;
                    if (_enemyModelScript.rangedAttackCurWindupTimeRem < 0)
                        _enemyModelScript.rangedAttackCurWindupTimeRem = 0;
                }
            }
            else
            {
                _enemyModelScript.isWindingUpRangedAttack = true;
                _enemyModelScript.rangedAttackCurWindupTimeRem = _enemyModelScript.rangedAttackWindupTime;
            }
        }

        public void CheckBattleColliderCollisionsWithInput(GameObject _curObjectGO, Transform _curTransform, BoxCollider2D _curHitbox, bool curObjectIsPlayer, bool curObjectIsBC)
        {
            for (var j = battleColliders.Count - 1; j > -1; j--)
            {
                var curBattleColliderGO = battleColliders[j];
                var curBattleColliderScript = curBattleColliderGO.GetComponent<BattleCollider>();

                if ((!isPlayerUsingMirage || !curObjectIsPlayer) &&
                    (!curBattleColliderScript.playerIsImmune || !curObjectIsPlayer) &&
                    !curBattleColliderScript.collidedBattleObjects.Contains(_curObjectGO) &&
                    curBattleColliderScript.hasContactEffects &&
                    _curHitbox.isActiveAndEnabled &&
                    _curHitbox.bounds.Intersects(curBattleColliderGO.GetComponent<BoxCollider2D>().bounds))
                {
                    var isCurGOAffected = true;
                    var isCurGOShield = false;
                    if (curBattleColliderScript.isDestroyedByShield || curBattleColliderScript.destroySelfOnCollision)
                    {
                        var curObjectPossibleBCScript = _curObjectGO.GetComponent<BattleCollider>();
                        if (curObjectPossibleBCScript != null && curObjectPossibleBCScript.isShield)
                        {
                            isCurGOAffected = false;
                            isCurGOShield = true;
                        }
                    }

                    if (isCurGOAffected)
                        HandleAttackCompletion(
                            _curTransform,
                            curBattleColliderGO.transform.position,
                            curBattleColliderScript.combatStats,
                            curBattleColliderScript.toolThisWasCreatedFrom);

                    curBattleColliderScript.collidedBattleObjects.Add(_curObjectGO);

                    if (curBattleColliderScript.destroySelfOnCollision &&
                        (!isCurGOShield || (isCurGOShield && curBattleColliderScript.isDestroyedByShield)))
                        BattleColliderSelfDestroy(curBattleColliderScript, j);
                }
            }
        }

        public void InitPlayerShields()
        {
            float aimCenterAngle = playerDirectionalAim.transform.eulerAngles.z + 90;

            for (var i = 1; i <= 2; i++)
            {
                GameObject newPlayerShield = InitHitbox(
                      mainShieldBCI.GetComponent<BattleColliderInstruction>(),
                      player.transform.position,
                      GameManager.radianToDirection(aimCenterAngle, true));
                playerShields.Add(newPlayerShield);
                newPlayerShield.SetActive(false);
            }
        }

        public void CheckPlayerShields()
        {
            Tool playerLeftToolScript = playerLeftTool.GetComponent<Tool>();
            Tool playerRightToolScript = playerRightTool.GetComponent<Tool>();

            // REMOVE ALL SHIELDS (IF ATTACKING WITH SHIELD, RETAIN ONE SHIELD
            if ((playerShieldState != ShieldState.none
                && ((!playerLeftToolScript.isShield && !playerRightToolScript.isShield)
                    || isPlayerCurrentlyUsingTool))
                || isPlayerUsingMirage)
            {
                playerShieldState = ShieldState.none;
                bool willKeepOneShield = false;
                if ((playerLeftToolScript.isShield && playerLeftTool == playerToolInUse) 
                    || (playerRightToolScript.isShield && playerRightTool == playerToolInUse))
                    willKeepOneShield = true;

                foreach (GameObject curShieldGO in playerShields)
                {
                    if (willKeepOneShield)
                    {
                        playerShieldState = ShieldState.singlewield;
                        RepositionShield(curShieldGO, new Vector2(0, singleShieldVertOffset), 0);
                    }
                    else
                        curShieldGO.SetActive(false);
                }
            }
            // DUAL WIELD SHIELDS
            else if (!isPlayerCurrentlyUsingTool && playerLeftToolScript.isShield && playerRightToolScript.isShield)
            {
                if (playerShieldState != ShieldState.dualwield)
                {
                    playerShieldState = ShieldState.dualwield;

                    RepositionShield(playerShields[0], new Vector2(dualShieldHorOffset, 0), -dualShieldRotationOffset);
                    RepositionShield(playerShields[1], new Vector2(-dualShieldHorOffset, 0), dualShieldRotationOffset);
                }
            }
            // SINGLE WIELD SHIELD
            else if (!isPlayerCurrentlyUsingTool &&
                playerShieldState != ShieldState.singlewield
                && (playerLeftToolScript.isShield || playerRightToolScript.isShield))
            {
                playerShieldState = ShieldState.singlewield;

                playerShields[1].SetActive(false);
                var curShieldGO = playerShields[0];
                RepositionShield(curShieldGO, new Vector2(0, singleShieldVertOffset), 0);
            }
        }

        public void RepositionShield(GameObject _shieldGO, Vector2 _position, float rotation)
        {
            var curShieldTransform = _shieldGO.transform;
            curShieldTransform.localPosition = _position;
            curShieldTransform.localEulerAngles = new Vector3(0, 0, rotation);
            _shieldGO.SetActive(true);
        }

        public void ActivateMirage()
        {
            isPlayerUsingMirage = true;
            remainingPlayerMirageTime = defaultMirageDuration;
            player.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255, 130) / 255;
        }

        public void UpdateMirage()
        {
            remainingPlayerMirageTime -= Time.deltaTime;
            if (remainingPlayerMirageTime <= 0)
            {
                // if mirage attack hitbox never hit anything, destroy it
                if (playerMirageBC != null)
                {
                    battleColliders.Remove(playerMirageBC);
                    Destroy(playerMirageBC);
                }

                isPlayerUsingMirage = false;
                remainingPlayerMirageTime = 0;
                player.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255, 255) / 255;
            }
        }

        public void InitPlayerBuff(CombatStats _combatStatsScript, bool isInfinite = false)
        {
            CombatStats newPlayerBuffGO = Instantiate(_combatStatsScript);
            CombatStats newPlayerBuffScript = newPlayerBuffGO.GetComponent<CombatStats>();

            newPlayerBuffScript.isBuff = true;
            newPlayerBuffScript.isBuffInfinite = isInfinite;
            if (!isInfinite)
                newPlayerBuffScript.tempBuffDuration = bottleBuffDefaultDuration;

            playerAllBuffs.Add(newPlayerBuffScript);
            newPlayerBuffGO.name = "PlayerBuff-" + playerAllBuffs.Count();

            if (playerCurTotalBuff != null)
                playerCurTotalBuff = AddOrRemoveCombatStats(playerCurTotalBuff, newPlayerBuffScript, false);
            else
                playerCurTotalBuff = newPlayerBuffScript;

            Destroy(_combatStatsScript.gameObject);
        }

        public void EndPlayerBuff(CombatStats _buffScript)
        {
            GameObject buffGO = _buffScript.gameObject;
            playerCurTotalBuff = AddOrRemoveCombatStats(playerCurTotalBuff, _buffScript, true);
            playerAllBuffs.Remove(_buffScript);
            Destroy(buffGO);
        }

        public CombatStats AddOrRemoveCombatStats(CombatStats _baseStats, CombatStats _statsToAddOrRemove, bool removeStats = false)
        {
            float posOrNegMultiplier = 1;
            if (removeStats)
                posOrNegMultiplier = -1;

            _baseStats.damage = GameManager.NegativeToZero(_baseStats.damage + (_statsToAddOrRemove.damage * posOrNegMultiplier));
            _baseStats.stun = GameManager.NegativeToZero(_baseStats.stun + (_statsToAddOrRemove.stun * posOrNegMultiplier));
            _baseStats.fire = GameManager.NegativeToZero(_baseStats.fire + (_statsToAddOrRemove.fire * posOrNegMultiplier));
            _baseStats.poison = GameManager.NegativeToZero(_baseStats.poison + (_statsToAddOrRemove.poison * posOrNegMultiplier));
            _baseStats.useSpeed = GameManager.NegativeToZero(_baseStats.useSpeed + (_statsToAddOrRemove.useSpeed * posOrNegMultiplier));
            _baseStats.resourceEfficiency = GameManager.NegativeToZero(_baseStats.resourceEfficiency + (_statsToAddOrRemove.resourceEfficiency * posOrNegMultiplier));

            return _baseStats;
        }

        public CombatStats MultiplyCombatStats(CombatStats _baseStats)
        {
            _baseStats.damage *= armorDefaultStatEffectiveness;
            _baseStats.stun *= armorDefaultStatEffectiveness;
            _baseStats.fire *= armorDefaultStatEffectiveness;
            _baseStats.poison *= armorDefaultStatEffectiveness;
            _baseStats.useSpeed *= armorDefaultStatEffectiveness;
            _baseStats.resourceEfficiency *= armorDefaultStatEffectiveness;

            return _baseStats;
        }
    }

    public enum AIMovementBehavior
    {
        directAgressive,agressiveDefense
    }

    public enum AIFiringBehavior
    {
        none,alwaysFiring,fireOnSight
    }

    public enum ShieldState
    {
        none,singlewield,dualwield
    }
}