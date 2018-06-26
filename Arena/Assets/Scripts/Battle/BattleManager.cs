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

        [Header("Gameplay General")]
        public float defaultKnockbackForce;
        public float defualtKnockbackTimeLength;

        [Header("Tools General")]
        public float poisonFrequencyMaxTime;

        [Header("Player General")]
        public float playerWalkSpeed;
        public float playerRunSpeed;
        public Vector2 playerSpawnLocation;
        public float baseToolUseSpeed;
        public float baseStaminaRegenRate;
        public float idleStaminaRegenBoost;
        public float runningStaminaCost;

        [Header("Player Hitboxes")]
        public Vector2[] playerMovementEdgeColliderPoints;
        public Vector2 playerCombatHitboxDimensions;
        public LayerMask playerCombatHitboxLayer;

        [Header("Player Tools")]
        public List<GameObject> playerTools;

        [Header("Player Stats")]
        public float maxHealth;
        public float maxStamina;
        public float maxMana;

        [Header("Player Top Down Aim")]
        public float playerTopDownAimMaxDistance;
        public float playerTopDownAimMovementSpeed;
        public Vector2 playerTopDownAimOffset;

        [Header("Default Enemy General")]
        public float tempEnemySpeed;
        public float defaultEnemyDamage = 1;
        public float defaultMeleeTowardsPlayerCooldownTime;

        [Header("Default Hitboxes")]
        public Vector2[] defaultEnemyMovementEdgeColliderPoints;
        public Vector2 defaultEnemyCombatHitboxDimensions;
        public LayerMask enemyCombatHitboxLayer;

        [Header("Physics Materials")]
        public PhysicsMaterial2D physicsMaterialDefaultBattleCollider;

        [Header("Default Spawning")]
        public float baseTimeToSpawnEnemy;
        public List<GameObject> enemyModelsToSpawn;

        [Space(10)]

        [Header("(REFERENCE)")]
        [Header("Player General")]
        public GameObject player;
        public GameObject playerLeftTool;
        public GameObject playerRightTool;
        public bool playerIsRunning;

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
        public float currentToolSpeedFinal = 0;
        public float playerToolCompletionCountdown = 0;

        [Header("Tool Quickselect")]
        public bool battleToolQuickSelectActive = false;

        [Header("Battle Objects")]
        public List<GameObject> battleObjects;

        [Header("Battle Colliders")]
        public List<GameObject> battleColliders;

        [Header("Fires")]
        public List<GameObject> activeFires;

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
        }

        public void UpdateBattleLogic()
        {
            // ---- TOOL-LOGIC START -----
            // DECREASE TOOL COMPLETION COUNTDOWN IF TOOL IS CURRENTLY BEING USED
            if (playerToolCompletionCountdown > 0)
            {
                playerToolCompletionCountdown -= Time.deltaTime;
                if (playerToolCompletionCountdown < 0)
                    playerToolCompletionCountdown = 0;
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
                UseTool(playerToolInUse);
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

                // RUN SELF DESTROY COUNTDOWN, IF BELOW ZERO, DESTROY COLLIDER
                curBattleColliderScript.timeBeforeSelfDestroy -= Time.deltaTime;
                if (curBattleColliderScript.timeBeforeSelfDestroy < 0)
                    BattleColliderSelfDestroy(curBattleColliderScript, i);
            }
            // ---------- BATTLE-COLLIDER-(OFFENSIVE HITBOXES)-LOGIC END --------------


            // ----- BATTLE-OBJECT-LOGIC START ------
            for (var i = battleObjects.Count - 1; i > -1; i--)
            {
                var curBattleObjectGameObject = battleObjects[i];
                var curBattleObjectScript = curBattleObjectGameObject.GetComponent<BattleObject>();
                var battleObjectStatsDisplay = UIManager.singleton.GetStatsDisplayByBattleObject(curBattleObjectGameObject);
                var battleObjectDefensiveHitbox = curBattleObjectScript.defensiveCombatHitbox.GetComponent<BoxCollider2D>();

                // --------- BATTLE-CHARACTER-LOGIC START ------------
                var curBattleCharacterScript = curBattleObjectGameObject.GetComponent<BattleCharacter>();
                if (curBattleCharacterScript != null)
                {
                    var battleObjectOffensiveHitbox = curBattleObjectScript.offensiveCombatHitbox.GetComponent<BoxCollider2D>();

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

                    // ALLOW AI TO MOVE AGAIN IF THEY FIT CRITERIA
                    if (curBattleCharacterScript.remainingStunTime <= 0 && curBattleCharacterScript.knockbackMovementDisabledCountdown <= 0)
                    {
                        curBattleObjectGameObject.GetComponent<AILerp>().canMove = true;
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

                    if (battleObjectOffensiveHitbox.bounds.Intersects(player.GetComponent<BattleObject>().defensiveCombatHitbox.GetComponent<BoxCollider2D>().bounds) &&
                        !curBattleCharacterScript.isPlayer)
                    {
                        HandleCombatCollision(player, curBattleObjectGameObject);
                    }
                    // ----------- ENEMY-TO-PLAYER-DAMAGE END ----------------
                }


                // ----------- COLLISIONS-WITH-BATTLE-COLLIDER-CHECK LOGIC START ------------------
                for (var j = battleColliders.Count - 1; j > -1; j--)
                {
                    var curBattleColliderGO = battleColliders[j];
                    var curBattleColliderScript = curBattleColliderGO.GetComponent<BattleCollider>();

                    if (!curBattleColliderScript.collidedBattleObjects.Contains(curBattleObjectGameObject) &&
                        curBattleColliderScript.hasContactEffects &&
                        battleObjectDefensiveHitbox.isActiveAndEnabled &&
                        battleObjectDefensiveHitbox.bounds.Intersects(curBattleColliderGO.GetComponent<BoxCollider2D>().bounds))
                    {
                        HandleAttackCompletion(
                            curBattleObjectGameObject.transform,
                            curBattleColliderGO.transform.position,
                            curBattleColliderScript.combatStats,
                            curBattleColliderScript.toolThisWasCreatedFrom);

                        curBattleColliderScript.collidedBattleObjects.Add(curBattleObjectGameObject);

                        if (curBattleColliderScript.destroySelfOnCollision)
                            BattleColliderSelfDestroy(curBattleColliderScript, j);
                    }
                }
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

        public void InitToolUse (GameObject tool)
        {
            var toolScript = tool.GetComponent<Tool>();
            var toolStatsScript = toolScript.combatStats.GetComponent<CombatStats>();
            var requiredResource = maxStamina;
            if (toolScript.usesMana)
                requiredResource = maxMana;
            requiredResource /= toolStatsScript.resourceEfficiency;

            if (playerToolCompletionCountdown <= 0
                && ((!toolScript.usesMana && curStamina >= requiredResource) 
                    || ((toolScript.usesMana && curMana >= requiredResource))))
            {
                if (!toolScript.usesMana)
                    curStamina -= requiredResource;
                else
                    curMana -= requiredResource;

                playerToolInUse = tool;
                isPlayerCurrentlyUsingTool = true;
                float currentToolSpeedStat = toolStatsScript.useSpeed;
                if (currentToolSpeedStat <= 0)
                    currentToolSpeedStat = 0.01f;
                var toolSpeedFinal = baseToolUseSpeed / currentToolSpeedStat;
                currentToolSpeedFinal = toolSpeedFinal;
                playerToolCompletionCountdown = toolSpeedFinal;
                UIManager.singleton.InitCountdownRing(player, toolSpeedFinal, toolSpeedFinal, UIManager.singleton.CRtoolColor);
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
                var defensiveCombatHitbox = battleObjectScript.defensiveCombatHitbox.GetComponent<BoxCollider2D>();
                var offensiveCombatHitbox = battleObjectScript.offensiveCombatHitbox.GetComponent<BoxCollider2D>();

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
                SetPlayerTopDownAim();

                // HITBOXES
                edgeCollider.points = playerMovementEdgeColliderPoints;
                defensiveCombatHitbox.size = playerCombatHitboxDimensions;
                defensiveCombatHitbox.gameObject.layer = GameManager.layermaskToLayer(playerCombatHitboxLayer);
                offensiveCombatHitbox.gameObject.SetActive(false);

                battleObjectScript.curHP = battleObjectScript.maxHP;
            }
            else
                InitEnemy(characterGameObject, _enemyModel);

            characterGameObject.GetComponent<Rigidbody2D>().freezeRotation = true;
            characterGameObject.transform.position = _spawnLocation;
        }

        public void UseTool(GameObject toolGameObject)
        {
            var toolScript = toolGameObject.GetComponent<Tool>();
            var battleColliderInstructionsScript = toolScript.battleColliderInstructionPrefabs[0].GetComponent<BattleColliderInstruction>();
            var combatStatsScript = toolScript.combatStats.GetComponent<CombatStats>();

            // if tool doesn't have to reload, then perform function, otherwise use this action to reload clip
            if (!toolScript.isReloadTool ||
                toolScript.curAmmoClip >= toolScript.maxAmmoCLip)
            {
                Vector2 positionToSpawnBattleCollider = playerDirectionalAim.transform.position;
                if (battleColliderInstructionsScript.usesAltAimReticule)
                    positionToSpawnBattleCollider = playerTopDownAim.transform.position;

                Vector2 forward = playerDirectionalAim.transform.up;
                float aimCenterAngle = playerDirectionalAim.transform.eulerAngles.z + 90;

                if (battleColliderInstructionsScript.isRaycast)
                {
                    int raycastsToCreateCount = battleColliderInstructionsScript.raycastCount;
                    if (raycastsToCreateCount <= 0)
                        raycastsToCreateCount = 1;

                    float curRaycastAngle = aimCenterAngle - (battleColliderInstructionsScript.multiRaycastSpreadAngle / 2);

                    for (var i = 0; i < raycastsToCreateCount; i++)
                    {
                        // RAYCAST
                        RaycastHit2D hit = Physics2D.Raycast(
                                               positionToSpawnBattleCollider,
                                               GameManager.radianToDirection(curRaycastAngle, true),
                                               battleColliderInstructionsScript.raycastLength,
                                               battleColliderInstructionsScript.layerMask);
                        Vector2 raycastEndLocation = hit.point;
                        if (hit.collider == null)
                            raycastEndLocation = positionToSpawnBattleCollider +
                                (GameManager.radianToDirection(curRaycastAngle, true) * battleColliderInstructionsScript.raycastLength);

                        // RENDER RAYCAST LINE
                        var lineRenderer = Instantiate(prefabLineRenderer);
                        var lineRendererScript = lineRenderer.GetComponent<LineRenderer>();
                        UIManager.singleton.customLineRenderers.Add(lineRenderer);
                        Vector3[] positions = new Vector3[]
                        { 
                            new Vector3(positionToSpawnBattleCollider.x, positionToSpawnBattleCollider.y, -2),
                            new Vector3(raycastEndLocation.x, raycastEndLocation.y, -2)
                        };
                        lineRendererScript.SetPositions(positions);

                        // PERFORM HIT LOGIC WHETHER A HIT RETURNS AN OBJECT OR NOT (AS LONG AS THE COLLIDER DOESN'T INEXPLICABLY EQUAL NULL)
                        if (hit.collider != null)
                            HandleAttackCompletion(hit.collider.transform.parent, hit.point, combatStatsScript, toolScript);
                        SpawnFires(raycastEndLocation, combatStatsScript, toolScript);

                        // ADVANCE RAYCAST SPAWN ANGLE FOR MULTIPLE RAYCAST SPAWN
                        curRaycastAngle += (battleColliderInstructionsScript.multiRaycastSpreadAngle / (battleColliderInstructionsScript.raycastCount - 1));
                    }
                }
                else
                {
                    InitHitbox(battleColliderInstructionsScript, positionToSpawnBattleCollider, forward, toolScript);
                }

                // EXPEND AMMO IF APPLICABLE
                if (toolScript.isReloadTool)
                    toolScript.curAmmoClip = 0;
            }
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
            var targetBattleCharacterScript = _targetGameObject.GetComponent<BattleCharacter>();
            var offensiveBattleCharacterScript = _offensiveGameObject.GetComponent<BattleCharacter>();

            // DEFAULTS TO BE OVERIDDEN IF APPROPRIATE
            float damage = defaultEnemyDamage;
            float curKnockbackForce = defaultKnockbackForce;

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

                        Debug.Log("Max: " + targetBattleCharacterScript.poisonDamageFrequency);
                        Debug.Log("Cur: " + targetBattleCharacterScript.curPoisonDamageCountdown);
                        Debug.Log("----------------");

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
                    (_usedToolScript != null && _usedToolScript.knockbackForce > 0))
                    HandleKnockback(_targetGameObject, _offensiveGameObject.transform.position, curKnockbackForce);

                // IF AI IS ATTACKING, SET COOLDOWN BEFORE THEY CAN HIT WITH MELEE AGAIN
                if (!offensiveBattleCharacterScript.isPlayer)
                    offensiveBattleCharacterScript.meleeTowardsPlayerCooldown = defaultMeleeTowardsPlayerCooldownTime;
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
            for (var i = activeFires.Count - 1; i > -1; i--)
            {
                GameObject curFireGameObject = activeFires[i];
                var curFireBattleObjectScript = curFireGameObject.GetComponent<BattleObject>();

                // DECREASE FIRE LIFE COUNTDOWN, DESTROY AT 0
                if (curFireBattleObjectScript.curHP > 0)
                    curFireBattleObjectScript.curHP -= Time.deltaTime;
                else
                {
                    GameObject fireStatsDisplay = UIManager.singleton.GetStatsDisplayByBattleObject(curFireGameObject);
                    UIManager.singleton.battleObjectStatsDisplays.Remove(fireStatsDisplay);
                    Destroy(fireStatsDisplay);
                    curFireBattleObjectScript.curHP = 0;
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
                Debug.Log(leftSideSpawnX);
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

                        newFireGameObject.transform.position = curFireSpawnPoint;
                        newFireBattleObjectScript.maxHP = _usedCombatStatsScript.fire *
                        (_usedToolScript.combatStatsReduction == 0 ? 1 : _usedToolScript.combatStatsReduction);
                        newFireBattleObjectScript.curHP = newFireBattleObjectScript.maxHP;
                        newFireBattleObjectScript.defensiveCombatHitbox.GetComponent<BoxCollider2D>().enabled = false;

                        UIManager.singleton.InitBattleObjectStatsDisplay(newFireGameObject);

                        // TODO: don't scan every single time, kills performance (I think that's what's doing it, anyway)
                        AstarPath.active.Scan();

                        curFireSpawnPoint.x += spawnPadding;
                    }

                    curFireSpawnPoint.x = leftSideSpawnX;
                    curFireSpawnPoint.y -= spawnPadding;
                }
            }
        }

        public void InitHitbox(BattleColliderInstruction _battleColliderInstructionsScript, Vector2 _spawnPosition, Vector2 _spawnDirection, Tool _usedToolScript = null)
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

            var battleColliderScript = battleColliderGameObject.GetComponent<BattleCollider>();
            battleColliderScript.destroySelfOnCollision = _battleColliderInstructionsScript.destroySelfOnCollision;
            battleColliderScript.timeBeforeSelfDestroy = _battleColliderInstructionsScript.duration;
            battleColliderScript.curSpeed = _battleColliderInstructionsScript.startingSpeed;
            battleColliderScript.hasContinuousMovement = _battleColliderInstructionsScript.hasContinuousMovement;
            battleColliderScript.hasContactEffects = _battleColliderInstructionsScript.hasContactEffects;
            battleColliderScript.prefabBattleColliderInstructionOnSelfDestroy = _battleColliderInstructionsScript.prefabBattleColliderInstructionOnSelfDestroy;

            battleColliderScript.fireGridCount = _battleColliderInstructionsScript.fireGridCount;
            battleColliderScript.fireGridSpacialSize = _battleColliderInstructionsScript.fireGridSpacialSize;
            battleColliderScript.spawnsFireOnSelfDestroy = _battleColliderInstructionsScript.spawnsFireOnSelfDestroy;

            battleColliderScript.curDirection = _spawnDirection;

            battleColliderScript.combatStats = _usedToolScript.combatStats.GetComponent<CombatStats>();
            battleColliderScript.toolThisWasCreatedFrom = _usedToolScript;

            //battleColliderGameObject.layer = GameManager.layermaskToLayer(battleColliderInstructionsScript.layerMask);
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
            var defensiveCombatHitbox = battleObjectScript.defensiveCombatHitbox.GetComponent<BoxCollider2D>();
            var offensiveCombatHitbox = battleObjectScript.offensiveCombatHitbox.GetComponent<BoxCollider2D>();

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

            // DIRECTLY ALTER STATS
            enemyAILerp.speed = _enemyModel.walkSpeed;
            enemyAIDestinationSetter.target = player.transform;
            enemySpriteRenderer.sprite = _enemyModel.sprite;

            // UI
            UIManager.singleton.InitBattleObjectStatsDisplay(_battleCharacter);

            //HITBOXES
            edgeCollider.points = defaultEnemyMovementEdgeColliderPoints;
            defensiveCombatHitbox.size = defaultEnemyCombatHitboxDimensions;
            defensiveCombatHitbox.gameObject.layer = GameManager.layermaskToLayer(enemyCombatHitboxLayer);
            offensiveCombatHitbox.size = defaultEnemyCombatHitboxDimensions;
            offensiveCombatHitbox.gameObject.layer = GameManager.layermaskToLayer(playerCombatHitboxLayer);
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
}