using ColossalFramework;
using UnityEngine;
using ChangeRoadHeight.Enums;

namespace ChangeRoadHeight
{
    // Class name needs to be changed if the mod is reloaded while the game is running (or if you have another version of the mod installed)
    class BuildTool : ToolBase
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
            base.RenderGeometry(cameraInfo);

            if (isHoveringSegment)
            {
                m_toolController.RenderCollidingNotifications(cameraInfo, 0, 0);
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
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

        protected override void OnToolUpdate()
        {
            if (isHoveringSegment)
            {
                Vector3 worldPos = segment.m_bounds.center;

                string text = "";
                if (toolError != ToolError.None) text += "<color #ff7e00>";

                if (toolError == ToolError.Unknown) text += "Unknown error";
                else if (toolError == ToolError.OutOfArea) text += "Out of city limits!";
                else if (toolError == ToolError.AlreadyTwoway) text += "Road is already two-way";
                else if (toolError == ToolError.SameDirection) text += "Road already goes this direction";
                else if (toolError == ToolError.CannotUpgradeThisType)
                {
                    if (toolMode == ToolMode.Oneway) text += "Cannot upgrade this type to one-way road";
                    else if (toolMode == ToolMode.Twoway) text += "Cannot upgrade this type to two-way road";
                }
                else if (toolMode == ToolMode.Oneway) text += "Drag to set one-way road direction";
                else if (toolMode == ToolMode.Twoway) text += "Upgrade to two-way road";

                if (toolError != ToolError.None) text += "</color>";

                ShowToolInfo(true, text, worldPos);
            }
            else
            {
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
