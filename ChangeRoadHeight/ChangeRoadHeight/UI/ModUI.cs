using System;
using System.Collections.Generic;

using ColossalFramework.UI;
using UnityEngine;
using ChangeRoadHeight.Enums;

namespace ChangeRoadHeight
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
                    if (_toolMode != ToolMode.None) {
                        if (builtinTabstrip.selectedIndex >= 0) {
                            originalBuiltinTabsripSelectedIndex = builtinTabstrip.selectedIndex;
                        }
                        IgnoreBuiltinTabStripEvents(-1);
                    }
                    else if (builtinTabstrip.selectedIndex < 0 && originalBuiltinTabsripSelectedIndex >= 0) {
                        IgnoreBuiltinTabStripEvents(originalBuiltinTabsripSelectedIndex);
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

            tabstrip = UIUtils.Instance.FindComponent<UITabstrip>("ExtendedRoadUpgradePanel");
            if (tabstrip != null) {
                DestroyView();
            }

            CreateView();
            if (tabstrip == null) return false; 

            return true;
        }

        void CreateView() {
            ModDebug.Log("Creating view");

            GameObject rootObject = new GameObject("ExtendedRoadUpgradePanel");
            tabstrip = rootObject.AddComponent<UITabstrip>();

            UIButton tabTemplate = (UIButton)builtinTabstrip.tabs[0];

            int spriteWidth = 31;
            int spriteHeight = 31;

            UITextureAtlas atlas = CreateTextureAtlas("sprites.png", "ExtendedRoadUpgradeUI", tabTemplate.atlas.material, spriteWidth, spriteHeight);

            List<UIButton> tabs = new List<UIButton>();
            tabs.Add(tabstrip.AddTab("", null, false));
            tabs.Add(tabstrip.AddTab("", null, false));

            foreach (UIButton tab in tabs) {
                tab.name = "ChangeRoadHeightButton";
                tab.atlas = atlas;
                tab.size = new Vector2(spriteWidth, spriteHeight);
                tab.normalBgSprite = SpriteName.ButtonBackground.ToString();
                tab.disabledBgSprite = SpriteName.ButtonBackground.ToString();
                tab.hoveredBgSprite = SpriteName.ButtonBackgroundHovered.ToString();
                tab.pressedBgSprite = SpriteName.ButtonBackgroundPressed.ToString();
                tab.focusedBgSprite = SpriteName.ButtonBackgroundPressed.ToString();
                tab.playAudioEvents = true;
            }

            tabs[0].name = "ChangeRoadHeightButtonUp";
            tabs[0].tooltip = "Move road up";
            tabs[0].normalFgSprite = tabs[0].disabledFgSprite = tabs[0].hoveredFgSprite = SpriteName.IconRoadUp.ToString();
            tabs[0].pressedFgSprite = tabs[0].focusedFgSprite = SpriteName.IconRoadUpPressed.ToString();

            tabs[1].name = "ChangeRoadHeightButtonDown";
            tabs[1].tooltip = "Move road down";
            tabs[1].normalFgSprite = tabs[1].disabledFgSprite = tabs[1].hoveredFgSprite = SpriteName.IconRoadDown.ToString();
            tabs[1].pressedFgSprite = tabs[1].focusedFgSprite = SpriteName.IconRoadDownPressed.ToString();


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

        System.Collections.IEnumerator FinishCreatingView() {
            yield return null;
            tabstrip.selectedIndex = -1;
            tabstrip.eventSelectedIndexChanged += (UIComponent component, int index) => {
                ToolMode newMode = (ToolMode)(index + 1);
                ModDebug.Log("tabstrip.eventSelectedIndexChanged: " + newMode);
                if (selectedToolModeChanged != null) selectedToolModeChanged(newMode);
            };
        }

        UITextureAtlas CreateTextureAtlas(string textureFile, string atlasName, Material baseMaterial, int spriteWidth, int spriteHeight) {
            string[] spriteNames = Enum.GetNames(typeof(SpriteName));
            Texture2D tex = new Texture2D(spriteWidth * spriteNames.Length, spriteHeight, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;

            { // LoadTexture
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.IO.Stream textureStream = assembly.GetManifestResourceStream("ChangeRoadHeight." + textureFile);

                byte[] buf = new byte[textureStream.Length];  //declare arraysize
                textureStream.Read(buf, 0, buf.Length); // read from stream to byte array

                tex.LoadImage(buf);

                tex.Apply(true, true);
            }

            UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas>();

            { // Setup atlas
                Material material = (Material)Material.Instantiate(baseMaterial);
                material.mainTexture = tex;

                atlas.material = material;
                atlas.name = atlasName;
            }

            // Add sprites
            for (int i = 0; i < spriteNames.Length; ++i) {
                float uw = 1.0f / spriteNames.Length;

                var spriteInfo = new UITextureAtlas.SpriteInfo() {
                    name = spriteNames[i],
                    texture = tex,
                    region = new Rect(i * uw, 0, uw, 1),
                };

                atlas.AddSprite(spriteInfo);
            }

            return atlas;
        }

        private void IgnoreBuiltinTabStripEvents(int selectedIndex)
        {
            ignoreBuiltinTabstripEvents = true;
            ModDebug.Log("Setting builtin tabstrip mode: " + (selectedIndex));
            builtinTabstrip.selectedIndex = selectedIndex;
            ignoreBuiltinTabstripEvents = false;
        }
    }
}
