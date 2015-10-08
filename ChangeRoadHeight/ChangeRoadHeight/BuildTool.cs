using ColossalFramework;
using UnityEngine;
using ChangeRoadHeight.Enums;

namespace ChangeRoadHeight
{
    // rename to test while game is running.
    class BuildTool15: ToolBase
    {

        public ToolMode toolMode = ToolMode.None;
        public ToolError toolError = ToolError.None;

        public bool isHoveringSegment = false;
        public int segmentIndex;
        public NetSegment segment;
        public NetTool.ControlPoint startPoint;
        public NetTool.ControlPoint middlePoint;
        public NetTool.ControlPoint endPoint;

        public NetInfo newPrefab;

        // Expose protected property
        public new CursorInfo ToolCursor
        {
            get { return base.ToolCursor; }
            set { base.ToolCursor = value; }
        }

        // Overridden to disable base class behavior
        protected override void OnEnable()
        {
        }

        // Overridden to disable base class behavior
        protected override void OnDisable()
        {
        }

        //public static ToolBase.ToolErrors CreateNode(NetInfo info, NetTool.ControlPoint startPoint, NetTool.ControlPoint middlePoint, NetTool.ControlPoint endPoint, FastList<NetTool.NodePosition> nodeBuffer, 
        //                                             int maxSegments, bool test, bool visualize, bool autoFix, bool needMoney, bool invert, bool switchDir, ushort relocateBuildingID, 
        //                                             out ushort node, out ushort segment, out int cost, out int productionRate)

        public override void SimulationStep()
        {
            ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            base.SimulationStep();

            if (isHoveringSegment)
            {
                ushort node;
                ushort outSegment;
                int cost;
                int productionRate;
                // Initializes colliding arrays
                ToolErrors errors = NetTool.CreateNode(newPrefab != null ? newPrefab : segment.Info, startPoint, middlePoint, endPoint,
                    NetTool.m_nodePositionsSimulation, 1000, true, false, true, false, false, false, (ushort)0, out node, out outSegment, out cost, out productionRate);

            }
        }

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo)
        {
           // ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            base.RenderGeometry(cameraInfo);

            if (isHoveringSegment)
            {
                m_toolController.RenderCollidingNotifications(cameraInfo, 0, 0);
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
           // ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            base.RenderOverlay(cameraInfo);

            if (isHoveringSegment)
            {
                bool warning = toolError == ToolError.AlreadyBuilt;
                bool error = toolError != ToolError.None && toolError != ToolError.AlreadyBuilt;
                Color color = GetToolColor(warning, error);
                NetTool.RenderOverlay(cameraInfo, ref segment, color, color);

                Color color2 = GetToolColor(true, false);
                m_toolController.RenderColliding(cameraInfo, color, color2, color, color2, (ushort)segmentIndex, 0);
            }
        }

        protected override void OnToolUpdate() {
           // ModDebug.LogClassAndMethodName(this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            if (isHoveringSegment) {
                Vector3 worldPos = segment.m_bounds.center;

                string text = "";
                if (toolMode == ToolMode.RoadHeightUp) {
                    text = "Click to increase road height.";
                } else if (toolMode == ToolMode.RoadHeightDown) {
                    text = "Click to lower road height.";
                }
                ShowToolInfo(true, text, worldPos);
            } else {
                ShowToolInfo(false, null, Vector3.zero);
            }
        }

        // Expose protected method
        public new static bool RayCast(ToolBase.RaycastInput input, out ToolBase.RaycastOutput output)
        {
            return NetTool.RayCast(input, out output);
        }
    }
}
