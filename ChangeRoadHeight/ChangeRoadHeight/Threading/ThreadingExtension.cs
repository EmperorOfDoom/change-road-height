using System;
using System.Collections.Generic;

using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using UnityEngine;
using ChangeRoadHeight.Enums;
using ChangeRoadHeight.UI;
using System.Reflection;

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

        Vector3 hitPosDelta = Vector3.zero;
        float prevRebuildTime = 0.0f;
        int prevBuiltSegmentIndex = 0;
        bool mouseRayValid = false;
        Ray mouseRay;
        float mouseRayLength;
        bool mouseDown = false;
        float currentTime = 0.0f;

        UIPanel roadsPanel = null;

        ModUI ui = new ModUI();
        bool loadingLevel = false;

        BuildTool21 buildTool = null;

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
            ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            if (buildTool == null)
            {
                buildTool = ToolsModifierControl.toolController.gameObject.GetComponent<BuildTool21>();
                if (buildTool == null)
                {
                    buildTool = ToolsModifierControl.toolController.gameObject.AddComponent<BuildTool21>();
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
            ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            if (buildTool != null)
            {
                ModDebug.Log("Tool destroyed");
                BuildTool21.Destroy(buildTool);
                buildTool = null;
            }
        }

        void FindRoadPrefabs()
        {
            ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
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
            ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            if (mode == toolMode) return;

            if (mode != ToolMode.None)
            {
                FindRoadPrefabs();
                CreateBuildTool();
                ToolsModifierControl.toolController.CurrentTool = buildTool;

                if (mode == ToolMode.RoadHeightDown)
                {
                    ModDebug.Log("move road height down mode activated");
                    toolMode = ToolMode.RoadHeightDown;
                }
                else if (mode == ToolMode.RoadHeightUp)
                {
                    ModDebug.Log("Move road height up mode activated");
                    toolMode = ToolMode.RoadHeightUp;
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
             ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            if (loadingLevel) return;

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
       
            //ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);

            if (toolMode == ToolMode.None) return;

            if (!mouseDown)
            {
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

            if (BuildTool21.RayCast(raycastInput, out raycastOutput))
            {

                int segmentIndex = raycastOutput.m_netSegment;
                if (segmentIndex != 0)
                {

                    NetManager net = Singleton<NetManager>.instance;
                    NetInfo newRoadPrefab = null;
                    NetInfo prefab = net.m_segments.m_buffer[segmentIndex].Info;

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

                    string prefabName = null;
                    if (!roadPrefabNames.TryGetValue(prefab.GetInstanceID(), out prefabName) || prefabName == null)
                    {
                        ModDebug.Error("Prefab name not found");
                        toolError = ToolError.Unknown;
                        return;
                    }

                    NetInfo newPrefab;
                    if (!roadPrefabs.TryGetValue(prefabName, out newPrefab) || newPrefab == null)
                    {
                        ModDebug.Error("Prefab not found: " + prefabName);
                        toolError = ToolError.Unknown;
                        return;
                    }

                    newRoadPrefab = newPrefab;

                      //  ModDebug.Log("Going to rebuild segment");
                        int newIndex = RebuildSegment(segmentIndex, newPrefab, raycastOutput.m_hitPos, hitPosDelta, ref toolError);
                    
                        if (newIndex != 0)
                        {
                        //    ModDebug.Log("newIndex: " + newIndex);
                            if (toolError != ToolError.None) return;

                            prevBuiltSegmentIndex = segmentIndex;
                            prevRebuildTime = currentTime;
                            segmentIndex = newIndex;
                        }
                   
                    if (buildTool != null)
                    {
                        // ModDebug.Log("Using segment from buffer");
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

        void GetSegmentControlPoints(int segmentIndex, out NetTool.ControlPoint startPoint, out NetTool.ControlPoint middlePoint, out NetTool.ControlPoint endPoint)
        {
           // ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
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

        int RebuildSegment(int segmentIndex, NetInfo newPrefab, Vector3 directionPoint, Vector3 direction, ref ToolError error)
        {
          //  ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);

            NetManager net = Singleton<NetManager>.instance;
            NetInfo prefab = net.m_segments.m_buffer[segmentIndex].Info;

            NetTool.ControlPoint startPoint;
            NetTool.ControlPoint middlePoint;
            NetTool.ControlPoint endPoint;

            MethodInfo dynMethod = netTool.GetType().GetMethod("ChangeElevation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (toolMode == ToolMode.RoadHeightUp)
            {
                Singleton<SimulationManager>.instance.AddAction<bool>((IEnumerator<bool>)dynMethod.Invoke(new NetTool(), new object[] { 1 }));
            }
            else if (toolMode == ToolMode.RoadHeightDown)
            {
                Singleton<SimulationManager>.instance.AddAction<bool>((IEnumerator<bool>)dynMethod.Invoke(new NetTool(), new object[] { -1 }));
            }

            GetSegmentControlPoints(segmentIndex, out startPoint, out middlePoint, out endPoint);

            if (direction.magnitude > 0.0f)
            {
                float dot = Vector3.Dot(direction.normalized, (endPoint.m_position - startPoint.m_position).normalized);
                float threshold = Mathf.Cos(Mathf.PI / 4);

                if (dot > -threshold && dot < threshold) return 0;
            }

            ushort node = 0;
            ushort segment = 0;
            int cost = 0;
            int productionRate = 0;

            //  CreateNode(NetInfo info, NetTool.ControlPoint startPoint, NetTool.ControlPoint middlePoint, NetTool.ControlPoint endPoint, FastList<NetTool.NodePosition> nodeBuffer, int maxSegments, bool test, bool visualize, bool autoFix, bool needMoney, bool invert, bool switchDir, ushort relocateBuildingID, out ushort firstNode, out ushort lastNode, out ushort segment, out int cost, out int productionRate)
            if (mouseDown && ((currentTime - prevRebuildTime) > 0.4f))
            {
                newPrefab.m_minHeight = 12.0f;
                NetTool.CreateNode(newPrefab, startPoint, middlePoint, endPoint, NetTool.m_nodePositionsSimulation, 1000, false, false, true, false, false, false, (ushort)0, out node, out segment, out cost, out productionRate);
            }
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
