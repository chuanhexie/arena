using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Pathfinding;
using Rewired;
using UnityEngine.UI;

namespace Arena
{
    public class MenuManager : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("General")]
        public GameObject eventSystem;
        public GameObject defaultMenuPage;
        public GameObject shopMenuMainPage;

        [Header("Prefabs")]
        public GameObject prefabMenuButton;

        [Header("(REFERENCE)")]
        public List<MenuPage> curMenuPages; // 0 is lowest layer, increasing layer height with each index
        public RectTransform mainCanvas;
        public Transform mainCanvasTransform;
        public UnityEngine.EventSystems.EventSystem eventSystemScript;

        public static MenuManager singleton;
        void Awake()
        {
            singleton = this;
        }

        public void InitMenuManager()
        {
            MenuPage curMenuPageScript = defaultMenuPage.GetComponent<MenuPage>();
            eventSystemScript = eventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>();

            InitMenuPage(defaultMenuPage, curMenuPageScript);
            curMenuPages.Add(curMenuPageScript);

            // assign first button selection to the menu page / menu container / menu button in each respective first index
            if (curMenuPages.Count() > 0)
            {
                List<ButtonContainer> curMenuPageButtonContainers = curMenuPages[0].buttonContainerScripts;
                if (curMenuPageButtonContainers.Count() > 0)
                {
                    if (curMenuPageButtonContainers[0].childMenuButtonScripts.Count > 0)
                        eventSystemScript.SetSelectedGameObject(curMenuPageButtonContainers[0].childMenuButtonScripts[0].gameObject);
                    else
                        Debug.Log("there were no buttons in the first index button container to assign first selection");
                }
                else
                    Debug.Log("there were no button containers in the first index menu page to assign first selection");
            }
            else
                Debug.Log("there were no menu pages created in order to assign a first selection");
            
        }

        public void InitMenuPage(GameObject _menuPageGO, MenuPage _menuPageScript)
        {
            Transform curMenuPageTransform = _menuPageGO.GetComponent<Transform>();
            curMenuPageTransform.SetParent(mainCanvasTransform, false);

            foreach (GameObject curButtonContainerGO in _menuPageScript.buttonContainerGOs)
            {
                Transform curButtonContainerTransform = curButtonContainerGO.GetComponent<Transform>();
                curButtonContainerTransform.SetParent(curMenuPageTransform);
                _menuPageScript.buttonContainerScripts.Add(InitButtonContainer(curButtonContainerGO, curButtonContainerTransform));
            }
        }

        public ButtonContainer InitButtonContainer(GameObject _buttonContainerGO, Transform _buttonContainerTransform)
        {
            ButtonContainer curButtonContainerScript = _buttonContainerGO.GetComponent<ButtonContainer>();
            int buttonsToCreateCount = 0;
            List<MenuButton> newMenuButtonScripts = new List<MenuButton>();

            // set amount of buttons to create based on a condition of what these buttons will represent
            if (curButtonContainerScript.buttonContainerType == ButtonContainerType.playerToolsInShop)
            {
                foreach (Tool curTool in GameManager.singleton.playerAllTools)
                {
                    MenuButton newMenuButtonScript = InstantiateMenuButton(_buttonContainerGO, curButtonContainerScript, _buttonContainerTransform);
                    newMenuButtonScript.menuButtonType = MenuButtonType.item;

                    newMenuButtonScript.representedToolScript = curTool;

                    newMenuButtonScripts.Add(newMenuButtonScript);
                }
            }
                
            curButtonContainerScript.childMenuButtonScripts = newMenuButtonScripts;
            return curButtonContainerScript;
        }

        public MenuButton InstantiateMenuButton(GameObject _parentButtonContainerGO, ButtonContainer _parentButtonContainerScript, Transform _buttonContainerTransform)
        {
            GameObject newMenuButtonGO = Instantiate(prefabMenuButton);
            MenuButton newMenuButtonScript = newMenuButtonGO.GetComponent<MenuButton>();

            Button newMenuButtonUnityButtonScript = newMenuButtonGO.GetComponent<Button>();
            newMenuButtonScript.unityButtonScript = newMenuButtonUnityButtonScript;
            newMenuButtonUnityButtonScript.onClick.AddListener(HandleMenuButtonClick);

            RectTransform newMenuButtonRectTransform = newMenuButtonGO.GetComponent<Image>().rectTransform;
            newMenuButtonScript.rectTransformScript = newMenuButtonRectTransform;
            newMenuButtonRectTransform.sizeDelta = _parentButtonContainerScript.buttonDimensions;

            newMenuButtonGO.GetComponent<Transform>().SetParent(_buttonContainerTransform);

            return newMenuButtonScript;
        }

        public void HandleMenuButtonClick()
        {
            GameObject currentlySelectedButtonGO = eventSystemScript.currentSelectedGameObject;
            if (currentlySelectedButtonGO != null)
            {
                MenuButton currentlySelectedButtonScript = currentlySelectedButtonGO.GetComponent<MenuButton>();
                if (currentlySelectedButtonScript != null)
                {
                    if (currentlySelectedButtonScript.menuButtonType == MenuButtonType.item)
                    {
                        Debug.Log(currentlySelectedButtonScript.representedToolScript.name);
                    }
                }
                else
                    Debug.Log("a button was clicked, but its gameobject had no MenuButton script");
            }
            else
                Debug.Log("a button was clicked, but there was no currently selected gameobject, somehow");
        }
    }

    public enum ButtonContainerType
    {
        playerToolsInShop,purchasableTools,enchantMaterialStandard,enchantMaterialExpired,shopActions
    }

    public enum MenuButtonType
    {
        item
    }
}