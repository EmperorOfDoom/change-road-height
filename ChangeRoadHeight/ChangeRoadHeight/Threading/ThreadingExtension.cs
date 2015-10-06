using System;
using System.Collections.Generic;

using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using UnityEngine;
using ChangeRoadHeight.Enums;

namespace ChangeRoadHeight.Threading
{
    public class ThreadingExtension : ThreadingExtensionBase
    {
        public static ThreadingExtension Instance { get; private set; }

        Dictionary<int, string> roadPrefabNames = new Dictionary<int, string>();
        Dictionary<string, NetInfo> roadPrefabs = new Dictionary<string, NetInfo>();

        NetTool netTool = null;
        ToolBase.RaycastService raycastService;

        ToolMode toolMode = ToolMode.None;
        ToolError toolError = ToolError.None;


        string[] twowayNames = { "Basic Road", "Large Road" };
        string[] onewayNames = { "Oneway Road", "Large Oneway" };
        Vector3 hitPosDelta = Vector3.zero;
        Vector3 prevHitPos;
        float prevRebuildTime = 0.0f;
        int prevBuiltSegmentIndex = 0;
        bool mouseRayValid = false;
        Ray mouseRay;
        float mouseRayLength;
        bool mouseDown = false;
        bool dragging = false;
        float currentTime = 0.0f;

        UIPanel roadsPanel = null;

        ModUI ui = new ModUI();
        bool loadingLevel = false;

        BuildTool buildTool = null;

        public void OnLevelUnloading()
        {
            ui.DestroyView();
            toolMode = ToolMode.None;
            loadingLevel = true;
        }

        public void OnLevelLoaded(LoadMode mode)
        {
            loadingLevel = false;
            ModDebug.Log("OnLevelLoaded, UI visible: " + ui.isVisible);
        }

        public override void OnCreated(IThreading threading)
        {
            Instance = this;
            ui.selectedToolModeChanged += (ToolMode newMode) => {
                SetToolMode(newMode);
            };
        }

        public override void OnReleased()
        {
            ui.DestroyView();
            DestroyBuildTool();
        }

        void CreateBuildTool()
        {
            if (buildTool == null)
            {
                buildTool = ToolsModifierControl.toolController.gameObject.GetComponent<BuildTool>();
                if (buildTool == null)
                {
                    buildTool = ToolsModifierControl.toolController.gameObject.AddComponent<BuildTool>();
                    ModDebug.Log("Tool created: " + buildTool);
                }
                else
                {
                    ModDebug.Log("Found existing tool: " + buildTool);
                }
            }
        }

        void DestroyBuildTool()
        {
            if (buildTool != null)
            {
                ModDebug.Log("Tool destroyed");
                BuildTool.Destroy(buildTool);
                buildTool = null;
            }
        }

        void FindRoadPrefabs()
        {
            foreach (NetCollection collection in NetCollection.FindObjectsOfType<NetCollection>())
            {
                if (collection.name == "Road")
                {
                    foreach (NetInfo prefab in collection.m_prefabs)
                    {
                        roadPrefabNames[prefab.GetInstanceID()] = prefab.name;
                        roadPrefabs[prefab.name] = prefab;
                    }
                }
            }

            ModDebug.Log("Found " + roadPrefabs.Count + " road prefabs");
        }

        void SetToolMode(ToolMode mode, bool resetNetToolModeToStraight = false)
        {
            if (mode == toolMode) return;

            if (mode != ToolMode.None)
            {
                FindRoadPrefabs();
                CreateBuildTool();
                ToolsModifierControl.toolController.CurrentTool = buildTool;

                if (mode == ToolMode.Oneway)
                {
                    ModDebug.Log("One-way mode activated");
                    toolMode = ToolMode.Oneway;
                }
                else if (mode == ToolMode.Twoway)
                {
                    ModDebug.Log("Two-way mode activated");
                    toolMode = ToolMode.Twoway;
                }

                ui.toolMode = toolMode;
            }
            else
            {
                ModDebug.Log("Tool disabled");
                toolMode = ToolMode.None;

                if (ToolsModifierControl.toolController.CurrentTool == buildTool || ToolsModifierControl.toolController.CurrentTool == null)
                {
                    ToolsModifierControl.toolController.CurrentTool = netTool;
                }

                DestroyBuildTool();

                ui.toolMode = toolMode;

                if (resetNetToolModeToStraight)
                {
                    netTool.m_mode = NetTool.Mode.Straight;
                    ModDebug.Log("Reseted netTool mode: " + netTool.m_mode);
                }
            }
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            try
            {
                _OnUpdate();
            }
            catch (Exception e)
            {
                ModDebug.Error(e);
            }
        }

        void _OnUpdate()
        {
            if (loadingLevel) return;

            /*if (Input.GetKeyDown(KeyCode.Delete)) {
                if (UIInput.hoveredComponent != null) {
                    ModDebug.Log(UIUtils.Instance.GetTransformPath(UIInput.hoveredComponent.transform) + " (" + UIInput.hoveredComponent.GetType() + ")");
                }
            }*/

            if (Input.GetKeyDown(KeyCode.Return))
            {
                ModDebug.Log(netTool.m_prefab + " " + netTool.m_mode);
            }

            if (roadsPanel == null)
            {
                roadsPanel = UIView.Find<UIPanel>("RoadsPanel");
            }

            if (roadsPanel == null || !roadsPanel.isVisible)
            {
                if (toolMode != ToolMode.None)
                {
                    ModDebug.Log("Roads panel no longer visible");
                    SetToolMode(ToolMode.None, true);
                }
                return;
            }

            if (netTool == null)
            {
                foreach (var tool in ToolsModifierControl.toolController.Tools)
                {
                    NetTool nt = tool as NetTool;
                    if (nt != null && nt.m_prefab != null)
                    {
                        ModDebug.Log("NetTool found: " + nt.name);
                        netTool = nt;
                        break;
                    }
                }

                if (netTool == null) return;

                raycastService = new ToolBase.RaycastService(netTool.m_prefab.m_class.m_service, netTool.m_prefab.m_class.m_subService, netTool.m_prefab.m_class.m_layer);

                ModDebug.Log("UI visible: " + ui.isVisible);
            }

            if (!ui.isVisible)
            {
                ui.Show();
            }

            /*if (Input.GetKeyDown(KeyCode.Comma)) {
                SetToolMode(ToolMode.Twoway);
            }
            else if (Input.GetKeyDown(KeyCode.Period)) {
                SetToolMode(ToolMode.Oneway);
            }*/

            if (toolMode != ToolMode.None)
            {
                mouseDown = Input.GetMouseButton(0);
                mouseRayValid = !ToolsModifierControl.toolController.IsInsideUI && Cursor.visible;
                mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                mouseRayLength = Camera.main.farClipPlane;
                currentTime = Time.time;

                if (ToolsModifierControl.toolController.CurrentTool != buildTool)
                {
                    ModDebug.Log("Another tool selected");
                    SetToolMode(ToolMode.None);
                }
            }
            else
            {
                ui.toolMode = ToolMode.None;

                if (ToolsModifierControl.toolController.CurrentTool == buildTool)
                {
                    ToolsModifierControl.toolController.CurrentTool = netTool;
                }
            }
        }

        public override void OnBeforeSimulationTick()
        {
            try
            {
                _OnBeforeSimulationTick();
            }
            catch (Exception e)
            {
                ModDebug.Error(e);
            }
        }

        void _OnBeforeSimulationTick()
        {
            if (toolMode == ToolMode.None) return;

            if (!mouseDown)
            {
                dragging = false;
                prevBuiltSegmentIndex = 0;
            }

            if (buildTool != null)
            {
                buildTool.isHoveringSegment = false;
                buildTool.toolMode = toolMode;
                buildTool.ToolCursor = netTool.m_upgradeCursor;
            }

            if (!mouseRayValid) return;

            ToolBase.RaycastInput raycastInput = new ToolBase.RaycastInput(mouseRay, mouseRayLength);
            raycastInput.m_netService = raycastService;
            raycastInput.m_ignoreTerrain = true;
            raycastInput.m_ignoreNodeFlags = NetNode.Flags.All;
            raycastInput.m_ignoreSegmentFlags = NetSegment.Flags.Untouchable;

            ToolBase.RaycastOutput raycastOutput;
            if (BuildTool.RayCast(raycastInput, out raycastOutput))
            {

                int segmentIndex = raycastOutput.m_netSegment;
                if (segmentIndex != 0)
                {

                    NetManager net = Singleton<NetManager>.instance;
                    NetInfo newRoadPrefab = null;

                    NetTool.ControlPoint startPoint;
                    NetTool.ControlPoint middlePoint;
                    NetTool.ControlPoint endPoint;

                    GetSegmentControlPoints(segmentIndex, out startPoint, out middlePoint, out endPoint);

                    ushort node;
                    ushort outSegment;
                    int cost;
                    int productionRate;
                    // Check for out-of-area error and initialized collide arrays for visualization
                    ToolBase.ToolErrors errors = NetTool.CreateNode(net.m_segments.m_buffer[segmentIndex].Info,
                        startPoint, middlePoint, endPoint, NetTool.m_nodePositionsSimulation, 1000,
                        true, false, true, false, false, false, (ushort)0, out node, out outSegment, out cost, out productionRate);

                    if ((errors & ToolBase.ToolErrors.OutOfArea) != 0)
                    {
                        toolError = ToolError.OutOfArea;
                    }
                    else
                    {
                        if (mouseDown)
                        {
                            HandleMouseDrag(ref raycastOutput, ref toolError, false, ref newRoadPrefab, ref segmentIndex);

                            if (segmentIndex == prevBuiltSegmentIndex)
                            {
                                toolError = ToolError.AlreadyBuilt;
                            }
                        }
                        else
                        {
                            HandleMouseDrag(ref raycastOutput, ref toolError, true, ref newRoadPrefab, ref segmentIndex);
                        }
                    }

                    if (buildTool != null)
                    {
                        buildTool.segment = net.m_segments.m_buffer[segmentIndex];
                        buildTool.segmentIndex = segmentIndex;
                        buildTool.isHoveringSegment = toolError != ToolError.Unknown;
                        if (newRoadPrefab != null) buildTool.newPrefab = newRoadPrefab;
                        GetSegmentControlPoints(segmentIndex, out buildTool.startPoint, out buildTool.middlePoint, out buildTool.endPoint);
                    }
                }
            }

            if (buildTool != null)
            {
                buildTool.toolError = toolError;
            }
        }

        void HandleMouseDrag(ref ToolBase.RaycastOutput raycastOutput, ref ToolError error, bool test, ref NetInfo newRoadPrefab, ref int newSegmentIndex)
        {
            if (!test)
            {
                if (currentTime - prevRebuildTime < 0.1f) return;

                if (dragging)
                {
                    hitPosDelta = raycastOutput.m_hitPos - prevHitPos;
                    if (hitPosDelta.magnitude < 12.0f) return;
                }
                else
                {
                    prevHitPos = raycastOutput.m_hitPos;
                    dragging = true;
                    if (toolMode == ToolMode.Oneway) return;
                }

                prevHitPos = raycastOutput.m_hitPos;
            }

            int segmentIndex = raycastOutput.m_netSegment;
            if (segmentIndex != 0)
            {
                NetManager net = Singleton<NetManager>.instance;
                NetInfo prefab = net.m_segments.m_buffer[segmentIndex].Info;

                bool isOneway = !prefab.m_hasForwardVehicleLanes || !prefab.m_hasBackwardVehicleLanes;


                string prefabName = null;
                if (!roadPrefabNames.TryGetValue(prefab.GetInstanceID(), out prefabName) || prefabName == null)
                {
                    ModDebug.Error("Prefab name not found");
                    error = ToolError.Unknown;
                    return;
                }

                string newPrefabName = null;
                if (toolMode == ToolMode.Oneway)
                {
                    if (!isOneway) newPrefabName = FindMatchingName(prefabName, twowayNames, onewayNames);
                    else newPrefabName = prefabName;
                }
                else
                {
                    if (isOneway)
                    {
                        newPrefabName = FindMatchingName(prefabName, onewayNames, twowayNames);
                    }
                    else
                    {
                        toolError = ToolError.AlreadyTwoway;
                        return;
                    }
                }

                if (newPrefabName != null)
                {
                    if (test)
                    {
                        toolError = ToolError.None;
                        return;
                    }

                    NetInfo newPrefab;
                    if (!roadPrefabs.TryGetValue(newPrefabName, out newPrefab) || newPrefab == null)
                    {
                        ModDebug.Error("Prefab not found: " + newPrefabName);
                        error = ToolError.Unknown;
                        return;
                    }

                    newRoadPrefab = newPrefab;

                    int newIndex = RebuildSegment(segmentIndex, newPrefab, toolMode == ToolMode.Oneway, raycastOutput.m_hitPos, hitPosDelta, ref error);

                    if (newIndex != 0)
                    {
                        if (error != ToolError.None) return;

                        prevBuiltSegmentIndex = newSegmentIndex;
                        prevRebuildTime = currentTime;
                        newSegmentIndex = newIndex;
                    }
                }
                else
                {
                    toolError = ToolError.CannotUpgradeThisType;
                    return;
                }
            }
        }

        string FindMatchingName(string originalName, string[] fromNames, string[] toNames)
        {
            for (int i = 0; i < fromNames.Length; ++i)
            {
                if (originalName.Contains(fromNames[i]))
                {
                    return originalName.Replace(fromNames[i], toNames[i]);
                }
            }
            return null;
        }

        void GetSegmentControlPoints(int segmentIndex, out NetTool.ControlPoint startPoint, out NetTool.ControlPoint middlePoint, out NetTool.ControlPoint endPoint)
        {
            NetManager net = Singleton<NetManager>.instance;
            NetInfo prefab = net.m_segments.m_buffer[segmentIndex].Info;

            startPoint.m_node = net.m_segments.m_buffer[segmentIndex].m_startNode;
            startPoint.m_segment = 0;
            startPoint.m_position = net.m_nodes.m_buffer[startPoint.m_node].m_position;
            startPoint.m_direction = net.m_segments.m_buffer[segmentIndex].m_startDirection;
            startPoint.m_elevation = net.m_nodes.m_buffer[startPoint.m_node].m_elevation;
            startPoint.m_outside = (net.m_nodes.m_buffer[startPoint.m_node].m_flags & NetNode.Flags.Outside) != NetNode.Flags.None;

            endPoint.m_node = net.m_segments.m_buffer[segmentIndex].m_endNode;
            endPoint.m_segment = 0;
            endPoint.m_position = net.m_nodes.m_buffer[endPoint.m_node].m_position;
            endPoint.m_direction = -net.m_segments.m_buffer[segmentIndex].m_endDirection;
            endPoint.m_elevation = net.m_nodes.m_buffer[endPoint.m_node].m_elevation;
            endPoint.m_outside = (net.m_nodes.m_buffer[endPoint.m_node].m_flags & NetNode.Flags.Outside) != NetNode.Flags.None;

            middlePoint.m_node = 0;
            middlePoint.m_segment = (ushort)segmentIndex;
            middlePoint.m_position = startPoint.m_position + startPoint.m_direction * (prefab.GetMinNodeDistance() + 1f);
            middlePoint.m_direction = startPoint.m_direction;
            middlePoint.m_elevation = Mathf.Lerp(startPoint.m_elevation, endPoint.m_elevation, 0.5f);
            middlePoint.m_outside = false;
        }

        int RebuildSegment(int segmentIndex, NetInfo newPrefab, bool roadDirectionMatters, Vector3 directionPoint, Vector3 direction, ref ToolError error)
        {
            NetManager net = Singleton<NetManager>.instance;

            NetInfo prefab = net.m_segments.m_buffer[segmentIndex].Info;

            NetTool.ControlPoint startPoint;
            NetTool.ControlPoint middlePoint;
            NetTool.ControlPoint endPoint;
            GetSegmentControlPoints(segmentIndex, out startPoint, out middlePoint, out endPoint);

            if (direction.magnitude > 0.0f)
            {
                float dot = Vector3.Dot(direction.normalized, (endPoint.m_position - startPoint.m_position).normalized);
                float threshold = Mathf.Cos(Mathf.PI / 4);

                if (dot > -threshold && dot < threshold) return 0;

                if (roadDirectionMatters)
                {
                    bool inverted = (net.m_segments.m_buffer[segmentIndex].m_flags & NetSegment.Flags.Invert) != 0;

                    if (Singleton<SimulationManager>.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.True)
                    {
                        inverted = !inverted; // roads need to be placed in the opposite direction with left-hand traffic
                    }

                    bool reverseDirection = inverted ? (dot > 0.0f) : (dot < -0.0f);

                    if (reverseDirection)
                    {
                        var tmp = startPoint;
                        startPoint = endPoint;
                        endPoint = tmp;

                        startPoint.m_direction = -startPoint.m_direction;
                        endPoint.m_direction = -endPoint.m_direction;
                        middlePoint.m_direction = startPoint.m_direction;
                    }
                    else
                    {
                        if (prefab == newPrefab)
                        {
                            error = ToolError.SameDirection;
                            return 0;
                        }
                    }
                }
            }

            bool test = false;
            bool visualize = false;
            bool autoFix = true;
            bool needMoney = false;
            bool invert = false;

            ushort node = 0;
            ushort segment = 0;
            int cost = 0;
            int productionRate = 0;

            NetTool.CreateNode(newPrefab, startPoint, middlePoint, endPoint, NetTool.m_nodePositionsSimulation, 1000, test, visualize, autoFix, needMoney, invert, false, (ushort)0, out node, out segment, out cost, out productionRate);

            if (segment != 0)
            {
                if (newPrefab.m_class.m_service == ItemClass.Service.Road)
                {
                    Singleton<CoverageManager>.instance.CoverageUpdated(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Level.None);
                }

                error = ToolError.None;
                return segment;
            }

            return 0;
        }
    }
}
