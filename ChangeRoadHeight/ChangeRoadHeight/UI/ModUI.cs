using System;
using System.Collections.Generic;
using System.Collections;


using ColossalFramework.UI;
using UnityEngine;
using ChangeRoadHeight.Enums;

namespace ChangeRoadHeight.UI
{
    public class ModUI {
        public bool isVisible { get; private set; }

        ToolMode _toolMode = ToolMode.None;
        public ToolMode toolMode {
            get { return _toolMode; }
            set {
                if (value == _toolMode) return;

                _toolMode = value;
                if (tabstrip != null) {
                    tabstrip.selectedIndex = (int)_toolMode - 1;
                }

                if (builtinTabstrip != null) {
                    if (_toolMode != ToolMode.None)
                    {
                        if (builtinTabstrip.selectedIndex >= 0)
                        {
                            originalBuiltinTabsripSelectedIndex = builtinTabstrip.selectedIndex;
                        }
                        IgnoreBuiltinTabstrip(-1);

                    }
                    else if (builtinTabstrip.selectedIndex < 0 && originalBuiltinTabsripSelectedIndex >= 0)
                    {
                        IgnoreBuiltinTabstrip(originalBuiltinTabsripSelectedIndex);
                    }
                }
            }
        }

        public event System.Action<ToolMode> selectedToolModeChanged;

        bool initialized {
            get { return tabstrip != null; }
        }

        bool ignoreBuiltinTabstripEvents = false;
        int originalBuiltinTabsripSelectedIndex = -1;
        UIComponent roadsOptionPanel = null;
        UITabstrip builtinTabstrip = null;
        UITabstrip tabstrip = null;
        private static readonly int spriteWidth = 31;
        private static readonly int spriteHeight = 31;
        private static readonly string panelName = "ChangeRoadHeightPanel";

        public void Show() {
            if (!initialized) {
                if (!Initialize()) return;
            }

            ModDebug.Log("Showing UI");
            isVisible = true;
        }

        PropertyChangedEventHandler<int> builtinModeChangedHandler = null;

        public void DestroyView() {
            if (tabstrip != null) {
                if (builtinTabstrip != null) {
                    builtinTabstrip.eventSelectedIndexChanged -= builtinModeChangedHandler;
                }

                UIView.Destroy(tabstrip);
                tabstrip = null;
            }
            isVisible = false;
        }

        bool Initialize() {
            ModDebug.Log("Initializing UI");

            if (UIUtils.Instance == null) return false;

            roadsOptionPanel = UIUtils.Instance.FindComponent<UIComponent>("RoadsOptionPanel", null, UIUtils.FindOptions.NameContains);
            if (roadsOptionPanel == null || !roadsOptionPanel.gameObject.activeInHierarchy) return false;

            builtinTabstrip = UIUtils.Instance.FindComponent<UITabstrip>("ToolMode", roadsOptionPanel);
            if (builtinTabstrip == null || !builtinTabstrip.gameObject.activeInHierarchy) return false;

            tabstrip = UIUtils.Instance.FindComponent<UITabstrip>(panelName);
            if (tabstrip != null) {
                DestroyView();
            }

            CreateView();
            if (tabstrip == null) return false; 

            return true;
        }

        void CreateView() {
            ModDebug.Log("Creating view");

            GameObject rootObject = new GameObject(panelName);
            tabstrip = rootObject.AddComponent<UITabstrip>();

            CreateButtons();

            roadsOptionPanel.AttachUIComponent(tabstrip.gameObject);
            tabstrip.relativePosition = new Vector3(169, 38);
            tabstrip.width = 80;
            tabstrip.selectedIndex = -1;
            tabstrip.padding = new RectOffset(0, 1, 0, 0);

            if (builtinModeChangedHandler == null) {
                builtinModeChangedHandler = (UIComponent component, int index) => {
                    if (!ignoreBuiltinTabstripEvents) {
                        if (selectedToolModeChanged != null) selectedToolModeChanged(ToolMode.None);
                    }
                };
            }

            builtinTabstrip.eventSelectedIndexChanged += builtinModeChangedHandler;

            // Setting selectedIndex needs to be delayed for some reason
            tabstrip.StartCoroutine(FinishCreatingView());
        }

        private void CreateButtons()
        {
            UIButton tabTemplate = (UIButton)builtinTabstrip.tabs[0];
            UITextureAtlas atlas = AtlasCreator.CreateTextureAtlas("sprites.png", "ChangeRoadHeightUI", tabTemplate.atlas.material, spriteWidth, spriteHeight);
            List<UIButton> buttons = new List<UIButton>();

            buttons.Add(tabstrip.AddTab("", null, false));
            buttons.Add(tabstrip.AddTab("", null, false));
            foreach (UIButton button in buttons)
            {
                SetDefaultSettingsForButton(button, atlas);
            }
            SetButtonSpecificProperties(buttons[0], "ChangeRoadHeightButtonUp", "Move road up", SpriteName.IconRoadUp, SpriteName.IconRoadUpPressed);
            SetButtonSpecificProperties(buttons[1], "ChangeRoadHeightButtonDown", "Move road down", SpriteName.IconRoadDown, SpriteName.IconRoadDownPressed);
        }

        private void SetDefaultSettingsForButton(UIButton button, UITextureAtlas atlas)
        {
            button.name = "ChangeRoadHeightButton";
            button.atlas = atlas;
            button.size = new Vector2(spriteWidth, spriteHeight);
            button.normalBgSprite = SpriteName.ButtonBackground.ToString();
            button.disabledBgSprite = SpriteName.ButtonBackground.ToString();
            button.hoveredBgSprite = SpriteName.ButtonBackgroundHovered.ToString();
            button.pressedBgSprite = SpriteName.ButtonBackgroundPressed.ToString();
            button.focusedBgSprite = SpriteName.ButtonBackgroundPressed.ToString();
            button.playAudioEvents = true;
        }

        private void SetButtonSpecificProperties(UIButton button, String name, String tooltip, SpriteName normalSprite, SpriteName hoveredSprite)
        {
            button.name = name;
            button.tooltip = tooltip;
            button.normalFgSprite = button.disabledFgSprite = button.hoveredFgSprite = normalSprite.ToString();
            button.pressedFgSprite = button.focusedFgSprite = hoveredSprite.ToString();
        }

        private IEnumerator FinishCreatingView() {
            yield return null;
            tabstrip.selectedIndex = -1;
            tabstrip.eventSelectedIndexChanged += (UIComponent component, int index) => {
                ToolMode newMode = (ToolMode)(index + 1);
                ModDebug.Log("tabstrip.eventSelectedIndexChanged: " + newMode);
                if (selectedToolModeChanged != null) selectedToolModeChanged(newMode);
            };
        }

        private void IgnoreBuiltinTabstrip(int selectedIndex)
        {
            ignoreBuiltinTabstripEvents = true;
            ModDebug.Log("Setting builtin tabstrip mode: " + (selectedIndex));
            builtinTabstrip.selectedIndex = selectedIndex;
            ignoreBuiltinTabstripEvents = false;
        }
    }
}
