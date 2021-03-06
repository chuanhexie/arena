﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Arena
{
    public class UIManager : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Canvas")]
        public RectTransform canvas;

        [Header("Prefabs")]
        public GameObject prefabBattleObjectStatsDisplay;
        public GameObject prefabToolSelectOptionDisplay;
        public GameObject prefabToolQuickSelectMenu;
        public GameObject prefabSpawnerGate;
        public GameObject prefabClockArrow;
        public GameObject prefabCountdownRing;
        public GameObject prefabSpawnerGateSystem;
        public GameObject prefabTopDownAimBoundary;

        [Header("HUD Plugins")]
        public GameObject staminaText;
        public GameObject manaText;
        public GameObject healthText;
        public GameObject enemySpawnCountdownText;
        public GameObject remainingEnemiesCount;
        public GameObject selectedToolLeftHUD;
        public GameObject selectedToolRightHUD;

        [Header("Countdown Rings")]
        public float countdownRingMaxScale;
        public Color CRtoolColor;
        public Color CRstunColor;
        public Color CRpoisonColor;

        [Header("Player Attachments")]
        public float topDownAimBoundaryScale;

        [Header("Spawner Gate Systems")]
        public Sprite spriteInactiveClockArrow;
        public Sprite spriteActiveClockArrow;
        public bool isFirstSpawnerGateHor;
        public float firstSpawnerGateRotation = 90;
        public int standardHorClockArrowHalfCount;
        public int standardVerClockArrowHalfCount;
        public float standardHorHalfLength;
        public float standardVerHalfLength;

        [Header("Tool Quickselect")]
        public float toolSelectRingRadius;
        public GameObject toolQuickSelectMenuForBattle;

        [Header("Tool Quickselect Options")]
        public Sprite toolSelectBleedFlag;
        public Sprite toolSelectStamFlag;
        public Sprite toolSelectManaFlag;
        public Sprite toolSelectLeftFlag;
        public Sprite toolSelectRightFlag;

        [Space(10)]

        [Header("(REFERENCE)")]
        public List<GameObject> battleObjectStatsDisplays;
        public List<GameObject> spawnerGates;
        public List<GameObject> customLineRenderers;
        public List<GameObject> countdownRings;
        public GameObject curSpawnerGateSystem;

        [Header("Player Attachments")]
        public GameObject topDownAimBoundary;


        // FPS
		float deltaTime = 0.0f;

		
		void Update()
		{
			deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
		}
		
		void OnGUI()
		{
			int w = Screen.width, h = Screen.height;
	 
			GUIStyle style = new GUIStyle();
	 
			Rect rect = new Rect(0, 0, w, h * 2 / 100);
			style.alignment = TextAnchor.UpperLeft;
			style.fontSize = h * 2 / 100;
            style.normal.textColor = Color.white;
			float msec = deltaTime * 1000.0f;
			float fps = 1.0f / deltaTime;
			string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
			GUI.Label(rect, text, style);
		}


        public void UpdateUILogic()
        {
            // POSITION SELECTION RETICULE OF BATTLE QUICKSELECT
            var curToolQuickSelectForBattleScript = UIManager.singleton.toolQuickSelectMenuForBattle.GetComponent<ToolQuickSelectMenu>();
            curToolQuickSelectForBattleScript.PositionSelectionReticule(curToolQuickSelectForBattleScript.selectionAngle);

            // LOOP THROUGH CUSTOM LINE RENDERERS AND PERFORM APPROPRIATE LOGIC
            for (var i = UIManager.singleton.customLineRenderers.Count - 1; i > -1; i--)
            {
                var curLineRendererObject = UIManager.singleton.customLineRenderers[i];
                var curLineRendererCustomScript = curLineRendererObject.GetComponent<LineRendererCustom>();

                // RUN SELF DESTROY COUNTDOWN, IF BELOW ZERO, DESTROY LINE RENDERER
                curLineRendererCustomScript.timeBeforeSelfDestroy -= Time.deltaTime;
                if (curLineRendererCustomScript.timeBeforeSelfDestroy < 0)
                {
                    UIManager.singleton.customLineRenderers.RemoveAt(i);
                    Destroy(curLineRendererObject);
                }
            }

            UpdateBattleObjectStatsDisplays();
            UpdateToolQuickSelectMenu(UIManager.singleton.toolQuickSelectMenuForBattle);
            UpdateCountdownRings();
            UpdateHUD();
        }

        public void InitBattleObjectStatsDisplays()
        {
            var allBattleObjects = BattleManager.singleton.battleObjects.Where(x =>
                x.GetComponent<Tile>() == null ||
                (x.GetComponent<Tile>() != null && x.GetComponent<Tile>().isBlock));
            foreach (GameObject battleObject in allBattleObjects)
            {
                InitBattleObjectStatsDisplay(battleObject);
            }
        }

        public void InitBattleObjectStatsDisplay(GameObject battleObject)
        {
            if (!battleObject.GetComponent<BattleObject>().isInvincible)
            {
                var battleObjectStatsDisplay = Instantiate(prefabBattleObjectStatsDisplay);
                var battleObjectStatsDisplayScript = battleObjectStatsDisplay.GetComponent<BattleObjectStatsDisplay>();
                var tileScript = battleObject.GetComponent<Tile>();
                var battleCharacterScript = battleObject.GetComponent<BattleCharacter>();

                battleObjectStatsDisplay.transform.parent = canvas.transform;
                battleObjectStatsDisplays.Add(battleObjectStatsDisplay);
                battleObjectStatsDisplayScript.representedBattleObject = battleObject;


                if (tileScript != null && tileScript.isBlock)
                {
                    battleObjectStatsDisplayScript.hpText.SetActive(false);
                }

                if (battleCharacterScript != null && battleCharacterScript.isPlayer)
                {
                    battleObjectStatsDisplayScript.hpText.SetActive(false);
                }
            }
        }

        public void UpdateBattleObjectStatsDisplays()
        {
            foreach (GameObject battleObjectStatsDisplay in battleObjectStatsDisplays)
            {
                var battleObjectStatsDisplayScript = battleObjectStatsDisplay.GetComponent<BattleObjectStatsDisplay>();
                var representedBattleObject = battleObjectStatsDisplayScript.representedBattleObject;
                var representedBattleObjectScript = representedBattleObject.GetComponent<BattleObject>();

                // CONTENT
                battleObjectStatsDisplayScript.hpText.GetComponent<Text>().text = representedBattleObjectScript.curHP.ToString("0.00");

                // POSITION
                Vector2 localOffset = new Vector2(2.5f, -0.5f);
                Vector2 screenOffset = new Vector2(0, 0);

                Vector2 worldPoint = representedBattleObject.transform.TransformPoint(localOffset);

                Vector2 viewportPoint = Camera.main.WorldToViewportPoint(worldPoint);

                viewportPoint -= Vector2.one;

                Rect rect = canvas.rect;
                viewportPoint.x *= rect.width;
                viewportPoint.y *= rect.height;

                battleObjectStatsDisplay.transform.localPosition = viewportPoint + screenOffset;

            }
        }

        public void UpdateHUD()
        {
            var battleManager = BattleManager.singleton;

            enemySpawnCountdownText.GetComponent<Text>().text = battleManager.enemySpawnCountdown.ToString("0.00");
            remainingEnemiesCount.GetComponent<Text>().text = battleManager.enemyModelsToSpawn.Count().ToString();
            staminaText.GetComponent<Text>().text = battleManager.curStamina.ToString("0.00");
            manaText.GetComponent<Text>().text = battleManager.curMana.ToString("0.00");
            healthText.GetComponent<Text>().text = battleManager.GetPlayer().GetComponent<BattleObject>().curHP.ToString("0.00");

            var selectedToolLeftThumbnailSpriteRenderer = selectedToolLeftHUD.GetComponent<SpriteRenderer>();
            selectedToolLeftThumbnailSpriteRenderer.sprite = battleManager.playerLeftTool.GetComponent<Tool>().thumbnail;
            selectedToolLeftThumbnailSpriteRenderer.color = GetToolColor(battleManager.playerLeftTool);

            var selectedToolRightThumbnailSpriteRenderer = selectedToolRightHUD.GetComponent<SpriteRenderer>();
            selectedToolRightThumbnailSpriteRenderer.sprite = battleManager.playerRightTool.GetComponent<Tool>().thumbnail;
            selectedToolRightThumbnailSpriteRenderer.color = GetToolColor(battleManager.playerRightTool);
        }

        public GameObject GetStatsDisplayByBattleObject(GameObject battleObject)
        {
            return this.battleObjectStatsDisplays.FirstOrDefault(x => x.GetComponent<BattleObjectStatsDisplay>().representedBattleObject == battleObject);
        }

        public static UIManager singleton;
        void Awake()
        {
            singleton = this;
        }

        public void InitCountdownRing(GameObject _targetGameObject,
            float _curCountdownValue,
            float _maxCountdownValue,
            Color _color,
            bool isReoccurring = false,
            CountdownRingType _countdownRingType = CountdownRingType.basic)
        {
            var createNewCountdownRing = true;
            foreach (Transform child in _targetGameObject.transform)
            {
                var countdownRingScript = child.gameObject.GetComponent<CountdownRing>();
                if (countdownRingScript != null && countdownRingScript.type == _countdownRingType)
                {
                    createNewCountdownRing = false;
                    countdownRingScript.currentCountdownValue = _curCountdownValue;
                    countdownRingScript.initialCountdownValue = _maxCountdownValue;
                    countdownRingScript.isReoccurring = isReoccurring;
                    break;
                }
            }

            if (createNewCountdownRing)
            {
                GameObject newCountdownRingGameObject = Instantiate(prefabCountdownRing);
                var newCountdownRingScript = newCountdownRingGameObject.GetComponent<CountdownRing>();
                newCountdownRingScript.targetGameObject = _targetGameObject;
                newCountdownRingGameObject.transform.parent = _targetGameObject.transform;
                newCountdownRingGameObject.transform.localPosition = Vector2.zero;
                newCountdownRingGameObject.transform.localScale = Vector2.zero;
                newCountdownRingScript.currentCountdownValue = _curCountdownValue;
                newCountdownRingScript.initialCountdownValue = _maxCountdownValue;
                newCountdownRingScript.isReoccurring = isReoccurring;
                newCountdownRingScript.type = _countdownRingType;
                newCountdownRingGameObject.GetComponent<SpriteRenderer>().color = _color;
                countdownRings.Add(newCountdownRingGameObject);
            }
        }

        public void UpdateCountdownRings()
        {
            for (var i = countdownRings.Count - 1; i > -1; i--)
            {
                if (countdownRings[i] != null)
                    UpdateCountdownRing(countdownRings[i]);
                else
                    countdownRings.RemoveAt(i);
            }
        }

        public void UpdateCountdownRing(GameObject _countdownRingGameObject)
        {
            var countdownRingScript = _countdownRingGameObject.GetComponent<CountdownRing>();
            countdownRingScript.currentCountdownValue -= Time.deltaTime;
            
            if (countdownRingScript.currentCountdownValue > 0 && countdownRingScript.initialCountdownValue > 0)
            {
                var newScale = (countdownRingMaxScale * countdownRingScript.currentCountdownValue) / countdownRingScript.initialCountdownValue;
                _countdownRingGameObject.transform.localScale = new Vector2(newScale, newScale);
            }
            else if (countdownRingScript.isReoccurring)
            {
                countdownRingScript.currentCountdownValue = countdownRingScript.initialCountdownValue;
            }
            else
            {
                countdownRings.Remove(_countdownRingGameObject);
                Destroy(_countdownRingGameObject);
            }
        }

        public GameObject InitToolQuickSelectMenu()
        {
            GameObject toolQuickSelectMenuGameObject = Instantiate(prefabToolQuickSelectMenu);
            var toolQuickSelectMenuScript = toolQuickSelectMenuGameObject.GetComponent<ToolQuickSelectMenu>();
            List<GameObject> playerTools = BattleManager.singleton.playerTools;

            var i = 0;
            foreach (GameObject tool in playerTools)
            {
                var toolOptionTotalDegrees = 360 / 8;

                var toolOptionCenterAngle = 90 - (toolOptionTotalDegrees * i);
                if (toolOptionCenterAngle > 360)
                    toolOptionCenterAngle -= 360;
                if (toolOptionCenterAngle < 0)
                    toolOptionCenterAngle += 360;

                var toolSelectOption = Instantiate(prefabToolSelectOptionDisplay);
                toolQuickSelectMenuScript.toolSelectOptionDisplays.Add(toolSelectOption);
                toolSelectOption.transform.parent = toolQuickSelectMenuGameObject.transform;
                var toolSelectOptionScript = toolSelectOption.GetComponent<ToolSelectOptionDisplay>();

                toolSelectOptionScript.angleInQuickselect = toolOptionCenterAngle;

                var toolOptionCenterInRadians = toolOptionCenterAngle * Mathf.Deg2Rad;
                Vector2 directionToSpawnToolOption = new Vector2((float)Mathf.Cos(toolOptionCenterInRadians), (float)Mathf.Sin(toolOptionCenterInRadians));
                Vector2 toolOptionSpawnPosition = (directionToSpawnToolOption  * toolSelectRingRadius);

                toolSelectOption.transform.localPosition = toolOptionSpawnPosition;

                toolSelectOptionScript.representedPlayerTool = tool;
                var representedPlayerToolGameObject = toolSelectOptionScript.representedPlayerTool;
                var representedPlayerToolScript = representedPlayerToolGameObject.GetComponent<Tool>();

                var toolSelectOptionSpriteRenderer = toolSelectOptionScript.toolThumbnailGameObject.GetComponent<SpriteRenderer>();
                toolSelectOptionSpriteRenderer.sprite = representedPlayerToolScript.thumbnail;
                toolSelectOptionSpriteRenderer.color = GetToolColor(tool);

                toolSelectOptionScript.isBleedFlag.SetActive(representedPlayerToolScript.isBleed);
                toolSelectOptionScript.isBleedFlag.GetComponent<SpriteRenderer>().sprite = toolSelectBleedFlag;
                Sprite stamOrManaFlagSprite = toolSelectStamFlag;
                if (representedPlayerToolScript.usesMana)
                    stamOrManaFlagSprite = toolSelectManaFlag;
                toolSelectOptionScript.stamOrManaFlag.GetComponent<SpriteRenderer>().sprite = stamOrManaFlagSprite;


                UpdateToolQuickSelectMenu(toolQuickSelectMenuGameObject);

                i++;
            }

            return toolQuickSelectMenuGameObject;
        }

        public void UpdateToolQuickSelectMenu(GameObject toolSelectMenu)
        {
            var toolSelectMenuScript = toolSelectMenu.GetComponent<ToolQuickSelectMenu>();

            foreach (GameObject toolSelectOption in toolSelectMenuScript.toolSelectOptionDisplays)
            {
                var toolSelectOptionScript = toolSelectOption.GetComponent<ToolSelectOptionDisplay>();
                var representedPlayerToolGameObject = toolSelectOptionScript.representedPlayerTool;
                var toolSelectOptionSpriteRenderer = toolSelectOptionScript.isEquippedFlag.GetComponent<SpriteRenderer>();

                bool showEquipFlag = false;
                Sprite equipFlagSprite = new Sprite();
                if (representedPlayerToolGameObject == BattleManager.singleton.playerLeftTool)
                {
                    equipFlagSprite = toolSelectLeftFlag;
                    showEquipFlag = true;
                }
                else if (representedPlayerToolGameObject == BattleManager.singleton.playerRightTool)
                {
                    equipFlagSprite = toolSelectRightFlag;
                    showEquipFlag = true;
                }

                if (showEquipFlag)
                {
                    toolSelectOptionSpriteRenderer.enabled = true;
                    toolSelectOptionSpriteRenderer.sprite = equipFlagSprite;
                }
                else
                {
                    toolSelectOptionSpriteRenderer.enabled = false;
                }
            }
        }

        public Color GetToolColor(GameObject tool)
        {
            Color output = new Color(127, 127, 127);
            var toolScript = tool.GetComponent<Tool>();
            var toolStatsScript = toolScript.combatStats.GetComponent<CombatStats>();

            float primaryColorMultiplier = 10;
            float blackMultiplier = 10;

            output.r += (toolStatsScript.fire * primaryColorMultiplier) - (toolStatsScript.damage * blackMultiplier);
            output.g += (toolStatsScript.poison * primaryColorMultiplier) - (toolStatsScript.damage * blackMultiplier);
            output.b += (toolStatsScript.stun * primaryColorMultiplier) - (toolStatsScript.damage * blackMultiplier);
                

            output.r /= 255;
            output.g /= 255;
            output.b /= 255;

            return output;
        }

        public void InitSpawnerGateSystem()
        {
            GameObject newSpawnerGateSystemGameObject = Instantiate(prefabSpawnerGateSystem);
            var newSpawnerGateSystemScript = newSpawnerGateSystemGameObject.GetComponent<SpawnerGateSystem>();
            List<GameObject> spawnersInGrid = GridManager.singleton.spawners;

            bool isCurSpawnerGateHor = isFirstSpawnerGateHor;
            float spawnerGateRotation = firstSpawnerGateRotation;
            bool isFirstLoop = true;
            for (var i = spawnersInGrid.Count() - 1; i < spawnersInGrid.Count();)
            {
                // GATE
                GameObject curSpawnerGameObject = spawnersInGrid[i];
                var curSpawnerScript = curSpawnerGameObject.GetComponent<Spawner>();

                GameObject newSpawnerGateGameObject = Instantiate(prefabSpawnerGate);
                var newSpawnerGateScript = newSpawnerGateGameObject.GetComponent<SpawnerGate>();
                newSpawnerGateGameObject.transform.position = curSpawnerScript.sender.transform.position;
                newSpawnerGateGameObject.transform.rotation = Quaternion.Euler(0, 0, spawnerGateRotation);

                newSpawnerGateScript.enemyGraphicObject.transform.rotation = Quaternion.Euler(0, 0, 0);

                newSpawnerGateScript.linkedSpawner = curSpawnerGameObject;

                newSpawnerGateSystemScript.spawnerGates.Add(newSpawnerGateGameObject);
                newSpawnerGateGameObject.transform.SetParent(newSpawnerGateSystemGameObject.transform);


                // ARROWS
                if (!isFirstLoop) // DON'T ADD ON LEFT FOR FIRST GATE, ARROWS NEED T BE CREATED IN SEQUENCE
                    AddClockArrowsToOneSideOfGate(newSpawnerGateSystemGameObject, newSpawnerGateGameObject, true, isCurSpawnerGateHor);
                AddClockArrowsToOneSideOfGate(newSpawnerGateSystemGameObject, newSpawnerGateGameObject, false, isCurSpawnerGateHor);

                // ROTATE GATE AND ARROWS
                spawnerGateRotation -= 90;

                // SWITCH TO HOR OR VERT FOR NEXT GATE
                isCurSpawnerGateHor = !isCurSpawnerGateHor;

                // RESET LOOP (HAD TO START WITH LAST GATE IN ORDER TO CREATE THE ARROWS LEADING TO THE FIRST GATE
                if (isFirstLoop)
                {
                    i = 0;
                    isFirstLoop = false;
                }
                else if (i == spawnersInGrid.Count() - 2)
                    break;
                else
                    i += 1;

            }

            // ADD LEFT CLOCK ARROWS FOR LAST GATE (RIGHT ARROWS FOR THE LAST GATE WERE ADDED FIRST TO START THE ARROW SEQUENCE LEADING UP TO THE FIRST GATE)
            AddClockArrowsToOneSideOfGate(
                newSpawnerGateSystemGameObject,
                newSpawnerGateSystemScript.spawnerGates[0],
                true,
                isCurSpawnerGateHor);

            curSpawnerGateSystem = newSpawnerGateSystemGameObject;
            var curSpawnerGateSystemScript = curSpawnerGateSystem.GetComponent<SpawnerGateSystem>();

            // move first list item to the end, to fix the gate order due to the out of order way they needed to be constructed to order the arrows properly
            var tempFirstSpawnerGate = curSpawnerGateSystemScript.spawnerGates[0];
            curSpawnerGateSystemScript.spawnerGates.RemoveAt(0);
            curSpawnerGateSystemScript.spawnerGates.Add(tempFirstSpawnerGate);

            // update gate enemy graphics
            var battleEnemyLineup = BattleManager.singleton.enemyModelsToSpawn;
            for (var i = 0; i < spawnersInGrid.Count() && i < battleEnemyLineup.Count(); i++)
                UpdateSpawnerGateEnemy(true, curSpawnerGateSystemScript.spawnerGates[i], battleEnemyLineup[i]);
        }

        public void AddClockArrowsToOneSideOfGate(GameObject spawnerGateSystemGameObject, GameObject parentSpawnerGateGameObject, bool addOnLeft, bool isHorEdge)
        {
            var spawnerGateSystemScript = spawnerGateSystemGameObject.GetComponent<SpawnerGateSystem>();

            int clockArrowHalfCount = standardHorClockArrowHalfCount;
            float xSpawnPosInterval = standardHorHalfLength / (clockArrowHalfCount + 1);
            Vector2 leftInitialSpawnPos = new Vector2(-standardHorHalfLength, 0);
            if (!isHorEdge)
            {
                clockArrowHalfCount = standardVerClockArrowHalfCount;
                xSpawnPosInterval = standardVerHalfLength / (clockArrowHalfCount + 1);
                leftInitialSpawnPos = new Vector2(-standardVerHalfLength, 0);
            }
                
            Vector2 nextArrowSpawnPos = leftInitialSpawnPos;
            if (!addOnLeft)
                nextArrowSpawnPos = Vector2.zero;
            
            for (var i = 0; i < clockArrowHalfCount; i++)
            {
                nextArrowSpawnPos += new Vector2(xSpawnPosInterval, 0);

                GameObject newClockArrow = Instantiate(prefabClockArrow);
                spawnerGateSystemScript.clockArrows.Add(newClockArrow);
                newClockArrow.transform.parent = parentSpawnerGateGameObject.transform;
                newClockArrow.transform.localPosition = nextArrowSpawnPos;
                newClockArrow.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }

        public void ActivateCurrentClockArrow(bool deactivateAll = false)
        {
            var battleManagerSingleton = BattleManager.singleton;
            var UIManagerSingleton = UIManager.singleton;
            var curSpawnerGateSystemScript = curSpawnerGateSystem.GetComponent<SpawnerGateSystem>();
            List<GameObject> clockArrows = curSpawnerGateSystemScript.clockArrows;

            float amountOfSpawners = GridManager.singleton.spawners.Count;
            float fullSpawningRotationTime = amountOfSpawners * battleManagerSingleton.baseTimeToSpawnEnemy;
            float fullSpawningRotationCurSecond = (battleManagerSingleton.baseTimeToSpawnEnemy * battleManagerSingleton.enemySpawnerTileIndex)
                + battleManagerSingleton.enemySpawnCountdown;
            float fullSpawningRotationPercentComplete = 1 / (fullSpawningRotationTime / fullSpawningRotationCurSecond);

            int clockArrowIndex = Mathf.FloorToInt(fullSpawningRotationPercentComplete * clockArrows.Count());
            for (var i = 0; i < clockArrows.Count(); i++)
            {
                if (i == clockArrowIndex && !deactivateAll)
                    clockArrows[i].GetComponent<SpriteRenderer>().sprite = spriteActiveClockArrow;
                else
                    clockArrows[i].GetComponent<SpriteRenderer>().sprite = spriteInactiveClockArrow;
            }
        }

        public void InitPlayerTopDownAimBoundary()
        {
            var newTopDownAimBoundary = Instantiate(prefabTopDownAimBoundary);
            Transform topDownAimBoundaryTransform = newTopDownAimBoundary.transform;
            topDownAimBoundaryTransform.transform.localScale = new Vector3(topDownAimBoundaryScale, topDownAimBoundaryScale, topDownAimBoundaryScale);
            topDownAimBoundaryTransform.transform.parent = BattleManager.singleton.player.transform;
            topDownAimBoundaryTransform.transform.localPosition = Vector2.zero;
            topDownAimBoundary = newTopDownAimBoundary;
            topDownAimBoundary.SetActive(false);
        }

        public void UpdateSpawnerGateEnemy(bool isNewEnemy, GameObject _curSpawnerGate, GameObject _enemyModel = null)
        {
            var curSpawnerGateScript = _curSpawnerGate.GetComponent<SpawnerGate>();

            if (isNewEnemy)
            {
                Sprite enemySprite = _enemyModel.GetComponent<EnemyModel>().sprite;
                curSpawnerGateScript.enemyGraphicObject.GetComponent<SpriteRenderer>().sprite = enemySprite;
                curSpawnerGateScript.enemyGraphicObject.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
                curSpawnerGateScript.enemyGraphicObject.GetComponent<SpriteRenderer>().sprite = null;
        }
    }

    public enum CountdownRingType
    {
        basic,stun,poison
    }
}