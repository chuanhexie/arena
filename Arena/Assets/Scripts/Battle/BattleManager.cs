using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Pathfinding;

namespace Arena
{
    public class BattleManager : MonoBehaviour 
    {
        public GameObject battleCharacterPrefab;
        public GameObject playerDirectionalAimPrefab;
        public GameObject lineRendererPrefab;

        public GameObject player;
        public Sprite playerSprite;
        public float playerSpeed;
        public Vector2 playerSpawnLocation;
        public List<GameObject> playerTools;
        public GameObject playerLeftTool;
        public GameObject playerRightTool;

        public float maxHealth;
        public float curHealth;
        public float maxStamina;
        public float curStamina;
        public float maxMana;
        public float curMana;

        public float curDirectionX;
        public float curDirectionY;
        public GameObject playerDirectionalAim;
        public Sprite enemySprite;
        public float tempEnemySpeed;

        public float baseToolUseSpeed;
        public float baseStaminaRegenRate;
        public float idleStaminaRegenBoost;

        public float baseTimeToSpawnEnemy;
        public int amountOfEnemiesToSpawn;
        public float enemySpawnCountdown;
        public int enemySpawnerTileIndex = 0;

        public List<GameObject> battleObjects;


        public float currentToolSpeedFinal = 0;
        public float playerToolCompletionCountdown = 0;
        public bool isPlayerCurrentlyUsingTool;
        public GameObject playerToolInUse;

        public bool battleToolQuickSelectActive = false;

        public List<GameObject> battleColliders;

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
            curHealth = maxHealth;
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

            // REGEN STAMINA IF NOT USING STAMINA TOOL
            if ((playerToolCompletionCountdown <= 0 || 
                (isPlayerCurrentlyUsingTool && playerToolInUse.GetComponent<Tool>().usesMana)) && curStamina < maxStamina)
            {
                float curStaminaRegen = baseStaminaRegenRate;

                if (player.GetComponent<Rigidbody2D>().velocity == Vector2.zero)
                    curStaminaRegen += idleStaminaRegenBoost;

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


            // IF THE MAX AMOUNT OF ENEMIES TO SPAWN HAS NOT BEEN REACHED, CHECK IF ENEMIES SHOULD BE SPAWNED
            if (amountOfEnemiesToSpawn > 0)
                AdvanceEnemySpawners();
            else
                UIManager.singleton.ActivateCurrentClockArrow(true);


            // ----- BATTLE-OBJECT-LOGIC START ------
            for (var i = battleObjects.Count - 1; i > -1; i--)
            {
                var curBattleObjectGameObject = battleObjects[i];
                var curBattleObjectScript = curBattleObjectGameObject.GetComponent<BattleObject>();
                var battleObjectStatsDisplay = UIManager.singleton.GetStatsDisplayByBattleObject(curBattleObjectGameObject);

                // --------- BATTLE-CHARACTER-LOGIC START ------------
                var curBattleCharacterScript = curBattleObjectGameObject.GetComponent<BattleCharacter>();
                if (curBattleCharacterScript != null)
                {
                    // ----------- STUN-LOGIC START ---------------
                    // DECREASE STUN COUNTDOWN IF STUNNED, IF COUNTDOWN IS BELOW ZERO THEN RESET TO 0
                    if (curBattleCharacterScript.remainingStunTime > 0)
                        curBattleCharacterScript.remainingStunTime -= Time.deltaTime;
                    else
                        curBattleCharacterScript.remainingStunTime = 0;

                    // IF STUN COUNTDOWN IS BELOW ZERO, RESTORE CHARACTER MOVEMENT
                    if (curBattleCharacterScript.remainingStunTime <= 0)
                    {
                        curBattleObjectGameObject.GetComponent<AILerp>().canMove = true;
                    }
                    // ----------- STUN-LOGIC END -----------
                }

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
                if (curBattleObjectScript.curHP <= 0 && !curBattleObjectScript.isDead)
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
                UIManager.singleton.InitCountdownRing(player, toolSpeedFinal, UIManager.singleton.CRtoolColor);
            }
        }

        public void InitBattleCharacter(bool isPlayer, Vector2 spawnLocation)
        {
            var characterGameObject = Instantiate(battleCharacterPrefab);
            battleObjects.Add(characterGameObject);
            var characterScript = characterGameObject.GetComponent<BattleCharacter>();
            var characterAILerp = characterGameObject.GetComponent<AILerp>();
            var characterAIDestinationSetter = characterGameObject.GetComponent<AIDestinationSetter>();
            var characterSpriteRenderer = characterGameObject.GetComponent<SpriteRenderer>();
            var battleObjectScript = characterGameObject.GetComponent<BattleObject>();

            characterGameObject.GetComponent<Rigidbody2D>().freezeRotation = true;

            if (isPlayer == true)
            {
                characterGameObject.name = "Player";
                characterScript.isPlayer = true;
                player = characterGameObject;
                characterAILerp.enabled = false;
                characterSpriteRenderer.sprite = playerSprite;
                var directionalAimGameObject = Instantiate(playerDirectionalAim);
                playerDirectionalAim = directionalAimGameObject;
                playerDirectionalAim.transform.parent = player.transform;
                playerDirectionalAim.transform.localRotation = Quaternion.Euler(0, 0, 180);
                battleObjectScript.combatHitbox.layer = LayerMask.NameToLayer("PlayerCombat");
            }
            else
            {
                characterAILerp.speed = tempEnemySpeed;
                characterAIDestinationSetter.target = player.transform;
                characterSpriteRenderer.sprite = enemySprite;
                UIManager.singleton.InitBattleObjectStatsDisplay(characterGameObject);
            }

            battleObjectScript.curHP = battleObjectScript.maxHP;

            characterGameObject.transform.position = spawnLocation;
        }

        public void UseTool(GameObject toolGameObject)
        {
            var toolScript = toolGameObject.GetComponent<Tool>();
            var combatStatsScript = toolScript.combatStats.GetComponent<CombatStats>();
            var battleColliderInstructionsScript = toolScript.battleColliderInstructionPrefabs[0].GetComponent<BattleColliderInstruction>();

            Vector2 positionToSpawnBattleCollider = playerDirectionalAim.transform.position;
            if (battleColliderInstructionsScript.usesAltAimReticule)
                Debug.Log("should use alt aim reticule here");

            Vector2 forward = playerDirectionalAim.transform.up;

            if (battleColliderInstructionsScript.isRaycast)
            {
                // RAYCAST
                RaycastHit2D hit = Physics2D.Raycast(
                    positionToSpawnBattleCollider,
                    forward,
                    battleColliderInstructionsScript.raycastLength,
                    battleColliderInstructionsScript.layerMask);
                Vector2 raycastEndLocation = hit.point;
                if (hit.collider == null)
                    raycastEndLocation = positionToSpawnBattleCollider + (forward  * battleColliderInstructionsScript.raycastLength);

                // RENDER RAYCAST LINE
                var lineRenderer = Instantiate(lineRendererPrefab);
                var lineRendererScript = lineRenderer.GetComponent<LineRenderer>();
                UIManager.singleton.customLineRenderers.Add(lineRenderer);
                Vector3[] positions = new Vector3[] { 
                    new Vector3(positionToSpawnBattleCollider.x, positionToSpawnBattleCollider.y, -2),
                    new Vector3(raycastEndLocation.x, raycastEndLocation.y, -2)
                };
                lineRendererScript.SetPositions(positions);

                // DEAL DAMAGE IF HIT
                if (hit.collider != null)
                {
                    var hitBattleObject = hit.collider.transform.parent;
                    if (hitBattleObject != null)
                    {
                        var hitBattleObjectScript = hitBattleObject.GetComponent<BattleObject>();
                        var hitBattleCharacterScript = hitBattleObject.GetComponent<BattleCharacter>();

                        if (hitBattleObject.GetComponent<Tile>() == null)
                        {
                            // STUN
                            hitBattleCharacterScript.remainingStunTime += combatStatsScript.stun;
                            if (hitBattleCharacterScript.remainingStunTime > 0)
                            {
                                UIManager.singleton.InitCountdownRing(hitBattleObject.gameObject, hitBattleCharacterScript.remainingStunTime, UIManager.singleton.CRstunColor, CountdownRingType.stun);
                                hitBattleObject.GetComponent<AILerp>().canMove = false;
                            }
                        }

                        // DAMAGE
                        var originalHitBattleObjectCurHP = hitBattleObjectScript.curHP;
                        hitBattleObjectScript.curHP -= combatStatsScript.damage;
                        if (toolScript.isBleed)
                        {
                            curMana += originalHitBattleObjectCurHP - hitBattleObjectScript.curHP;
                            if (curMana > maxMana)
                                curMana = maxMana;
                        }
                        Debug.Log(hit.collider.transform.parent.name + ": " + hitBattleObjectScript.curHP);
                    }
                }
            }
            else
            {
                var battleColliderGameObject = Instantiate(battleColliderInstructionsScript.prefabToSpawn);
                battleColliders.Add(battleColliderGameObject);
                battleColliderGameObject.transform.localRotation = playerDirectionalAim.transform.rotation;
                battleColliderGameObject.transform.position = positionToSpawnBattleCollider + (forward * battleColliderInstructionsScript.forwardDistanceToSpawn);
                var battleColliderScript = battleColliderGameObject.GetComponent<BattleCollider>();
                battleColliderScript.timeBeforeSelfDestroy = battleColliderInstructionsScript.duration;
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
                InitBattleCharacter(false, spawnerTile.transform.position);
                amountOfEnemiesToSpawn -= 1;

                enemySpawnerTileIndex += 1;
                if (enemySpawnerTileIndex > enemySpawnerTiles.Count() - 1)
                    enemySpawnerTileIndex = 0;

                enemySpawnCountdown = 0;
            }
        }
    }


}