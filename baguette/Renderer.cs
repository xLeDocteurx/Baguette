using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ClickableTransparentOverlay;
using ImGuiNET;
using Swed64;

namespace baguette
{

    public class Renderer : Overlay
    {
        public static ViewMatrix ReadMatrix(IntPtr matrixPointer, Swed swed)
        {
            float[] matrix = swed.ReadMatrix(matrixPointer);

            ViewMatrix viewMatrix = new ViewMatrix();
            viewMatrix.m11 = matrix[0];
            viewMatrix.m12 = matrix[1];
            viewMatrix.m13 = matrix[2];
            viewMatrix.m14 = matrix[3];
            viewMatrix.m21 = matrix[4]; 
            viewMatrix.m22 = matrix[5];
            viewMatrix.m23 = matrix[6];
            viewMatrix.m24 = matrix[7];
            viewMatrix.m31 = matrix[8]; 
            viewMatrix.m32 = matrix[9]; 
            viewMatrix.m33 = matrix[10];
            viewMatrix.m34 = matrix[11];
            viewMatrix.m41 = matrix[12];
            viewMatrix.m42 = matrix[13];
            viewMatrix.m43 = matrix[14];    
            viewMatrix.m44 = matrix[15];
            return viewMatrix;
        }

        public static Vector2 WorldToScreen(ViewMatrix matrix, Vector3 position, Vector2 windowSize)
        {
            float screenW = (matrix.m41 * position.X) + (matrix.m42 * position.Y) + (matrix.m43 * position.Z) + matrix.m44;
            if(screenW > 0.001f)
            {
                float screenX = (matrix.m11 * position.X) + (matrix.m12 * position.Y) + (matrix.m13 * position.Z) + matrix.m14;
                float screenY = (matrix.m21 * position.X) + (matrix.m22 * position.Y) + (matrix.m23 * position.Z) + matrix.m24;

                float halfWidth = windowSize.X / 2;
                float halfHeight = windowSize.Y / 2;

                float X = halfWidth + (halfWidth * screenX / screenW);
                float Y = halfHeight - (halfHeight * screenY / screenW);
                return new Vector2(X, Y);
            } else
            {
                return new Vector2(-99, -99);
            }

        }

        public static List<Vector3> ReadBones(IntPtr boneAddress, Swed swed) {
            byte[] boneBytes = swed.ReadBytes(boneAddress, 27 * 32 + 16);

            List<Vector3> bonesList = new List<Vector3>();
            foreach (var boneId in Enum.GetValues(typeof(BoneIds)))
            {
                float x = BitConverter.ToSingle(boneBytes, (int)boneId * 32 + 0);
                float y = BitConverter.ToSingle(boneBytes, (int)boneId * 32 + 4);
                float z = BitConverter.ToSingle(boneBytes, (int)boneId * 32 + 8);
                Vector3 currentBone = new Vector3(x, y, z);
                // Console.WriteLine($"currentBone : {currentBone}");
                bonesList.Add(currentBone);
            }

            return bonesList;
        }

        public static List<Vector2> ReadBones2D(List<Vector3> bones, ViewMatrix viewMatrix, Vector2 screenSize)
        {
            List<Vector2> bonesList = new List<Vector2>();
            foreach (Vector3 bone in bones)
            {
                Vector2 currentBone2D = WorldToScreen(viewMatrix, bone, screenSize);
                bonesList.Add(currentBone2D);
            }

            return bonesList;
        }

        public Vector2 screenSize = new Vector2(1920, 1080);
        // Load the texture
        bool isInit = false;
        IntPtr logoImgPtr;
        IntPtr bombImgPtr;
        IntPtr diffuserImgPtr;
        IntPtr kevlarImgPtr;
        IntPtr helmetImgPtr;

        private ConcurrentQueue<Entity> _entities = new ConcurrentQueue<Entity>();
        private Entity _localPlayer = new Entity();
        private readonly object _entityLock = new object();

        ImDrawListPtr drawListPtr;

        private bool _espEnabled = true;
        private bool _espBoxeEnabled = true;
        private bool _espLinesEnabled = true;
        private bool _espHealthBarEnabmled = true;
        private bool _espArmorBarEnabmled = true;
        private bool _espHeadEnabled = true;
        private bool _espBonesEnabled = true;
        private bool _espBombEnabled = true;
        private bool _espKevlarEnabled = true;

        private bool _tiggerBotEnabled = false;

        private Vector4 _enemyTeamColor = new Vector4(1, 0, 0, 1);
        private Vector4 _allyTeamColor = new Vector4(0, 1, 0, 1);
        private Vector4 bombColor = new Vector4(0, 0, 1, 1);

        public void init()
        {
            AddOrGetImagePointer("./assets/baguette3.png", false, out logoImgPtr, out _, out _);

            AddOrGetImagePointer("./assets/Bombdefusal.png", false, out bombImgPtr, out _, out _);
            AddOrGetImagePointer("./assets/cisors.png", false, out diffuserImgPtr, out _, out _);
            AddOrGetImagePointer("./assets/Armor_hud_css.png", false, out kevlarImgPtr, out _, out _);
            AddOrGetImagePointer("./assets/Armor_hud_helmet_css.png", false, out helmetImgPtr, out _, out _);
        }

        protected override void Render()
        {
            if(!isInit)
            {
                init();
                isInit = true;
            }


            ImGui.Begin("Baguette");
            
            ImGui.Image(logoImgPtr, new Vector2(200, 200));

            ImGui.NewLine();
            ImGui.Checkbox("Enable ESP", ref _espEnabled);
            if(ImGui.CollapsingHeader("Esp"))
            {
                /*
                if (ImGui.CollapsingHeader("Enemy team color"))
                {
                    ImGui.ColorPicker4("##color", ref _enemyTeamColor);
                }
                if (ImGui.CollapsingHeader("Ally team color"))
                {
                    ImGui.ColorPicker4("##color", ref _allyTeamColor);
                }
                */
                ImGui.Checkbox("Enable Boxe ESP", ref _espBoxeEnabled);
                ImGui.Checkbox("Enable ESP Lines", ref _espLinesEnabled);
                ImGui.Checkbox("Enable Healthbar", ref _espHealthBarEnabmled);
                ImGui.Checkbox("Enable Armor", ref _espArmorBarEnabmled);
                ImGui.Checkbox("Enable Heads", ref _espHeadEnabled);
                ImGui.Checkbox("Enable Bones", ref _espBonesEnabled);
                ImGui.Checkbox("Enable Bomb & Diffusers", ref _espBombEnabled);
                ImGui.Checkbox("Enable Kevlar & Helmet", ref _espKevlarEnabled);
            }
            ImGui.NewLine();
            ImGui.Checkbox("Enable Trigger Bot", ref _tiggerBotEnabled);
            if(ImGui.CollapsingHeader("Trigger Bot"))
            {
            }
            ImGui.End();

            // DrawOverlay
            DrawOverlay();
            drawListPtr = ImGui.GetWindowDrawList();
            if(_espEnabled)
            {

                foreach (Entity entity in _entities)
                {
                    if(EntityOnScreen(entity))
                    {
                        if (_espBoxeEnabled) {
                            DrawBox(entity);
                        }

                        // DrawName

                        if (_espLinesEnabled)
                        {
                            DrawLine(entity);
                        }

                        if (_espHealthBarEnabmled)
                        {
                            DrawHealthBar(entity);
                        }

                        if (_espArmorBarEnabmled)
                        {
                            DrawArmorBar(entity);
                        }

                        if (_espHeadEnabled)
                        {
                            DrawHead(entity);
                        }

                        if (_espBonesEnabled)
                        {
                            DrawBones(entity);
                        }

                        if (_espBombEnabled)
                        {
                            DrawBombOrDiffuser(entity);
                        }

                        if(_espKevlarEnabled)
                        {
                            DrawKevlarAndHelmet(entity);
                        }
                    }
                }

            }

            // Thread.Sleep(1000);
        }

        void DrawOverlay()
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        }

        void DrawLine(Entity entity)
        {
            Vector4 lineColor = _localPlayer.Team == entity.Team ? _allyTeamColor : entity.hasBomb ? bombColor : _enemyTeamColor;
            drawListPtr.AddLine(new Vector2(screenSize.X / 2, screenSize.Y), entity.PositionV2, ImGui.ColorConvertFloat4ToU32(lineColor));
        }

        void DrawBox(Entity entity)
        {
            //Vector4 boxColor = _localPlayer.Team == entity.Team ? _allyTeamColor : _enemyTeamColor;
            Vector4 boxColor = new Vector4(0, 0, 0, 1);
            Vector4 boxFillColor = new Vector4(0, 0, 0, 0.25f);

            float entityHeight = entity.ViewOffsetV2.Y - entity.PositionV2.Y;
            float entityWidth = entityHeight / 4;

            Vector2 rectTop = new Vector2(entity.ViewOffsetV2.X - entityWidth, entity.ViewOffsetV2.Y);
            Vector2 rectBottom = new Vector2(entity.PositionV2.X + entityWidth, entity.PositionV2.Y);

            drawListPtr.AddRectFilled(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxFillColor));
            drawListPtr.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
        }

        void DrawHealthBar(Entity entity)
        {
            Vector4 boxColor = new Vector4(0, 0, 0, 1);
            Vector4 boxFillColor = new Vector4(0, 0, 0, 0.25f);
            Vector4 healthBarFillColor = new Vector4(0, 1, 0, 1);

            float entityHeight = entity.ViewOffsetV2.Y - entity.PositionV2.Y;
            float entityWidth = entityHeight / 4;
            float healthFillHeight = entityHeight * (entity.Health / 100.0f);
            float fillWidth = entityWidth / 4;

            Vector2 healthRectTop = new Vector2(entity.ViewOffsetV2.X + entityWidth + fillWidth, entity.ViewOffsetV2.Y);
            Vector2 healthRectBottom = new Vector2(entity.PositionV2.X + entityWidth, entity.PositionV2.Y);
            Vector2 healthFillTop = new Vector2(entity.ViewOffsetV2.X + entityWidth + fillWidth, entity.PositionV2.Y + healthFillHeight);
            Vector2 healthFillBottom = new Vector2(entity.PositionV2.X + entityWidth, entity.PositionV2.Y);

            drawListPtr.AddRectFilled(healthRectTop, healthRectBottom, ImGui.ColorConvertFloat4ToU32(boxFillColor));
            drawListPtr.AddRectFilled(healthFillTop, healthFillBottom, ImGui.ColorConvertFloat4ToU32(healthBarFillColor));
            drawListPtr.AddRect(healthRectTop, healthRectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
        }

        void DrawArmorBar(Entity entity)
        {
            Vector4 boxColor = new Vector4(0, 0, 0, 1);
            Vector4 boxFillColor = new Vector4(0, 0, 0, 0.25f);
            Vector4 armorBarFillColor = new Vector4(1, 1, 0, 1);

            float entityHeight = entity.ViewOffsetV2.Y - entity.PositionV2.Y;
            float entityWidth = entityHeight / 4;
            float armorFillHeight = entityHeight * (entity.Armor / 100.0f);
            float fillWidth = entityWidth / 4;

            Vector2 armorRectTop = new Vector2(entity.ViewOffsetV2.X + entityWidth + (fillWidth * 2) + fillWidth, entity.ViewOffsetV2.Y);
            Vector2 armorRectBottom = new Vector2(entity.PositionV2.X + entityWidth + (fillWidth * 2), entity.PositionV2.Y);
            Vector2 armorFillTop = new Vector2(entity.ViewOffsetV2.X + entityWidth + (fillWidth * 2) + fillWidth, entity.PositionV2.Y + armorFillHeight);
            Vector2 armorFillBottom = new Vector2(entity.PositionV2.X + entityWidth + (fillWidth * 2), entity.PositionV2.Y);

            drawListPtr.AddRectFilled(armorRectTop, armorRectBottom, ImGui.ColorConvertFloat4ToU32(boxFillColor));
            drawListPtr.AddRectFilled(armorFillTop, armorFillBottom, ImGui.ColorConvertFloat4ToU32(armorBarFillColor));
            drawListPtr.AddRect(armorRectTop, armorRectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
        }

        void DrawHead(Entity entity)
        {
            Vector4 fillColor = _localPlayer.Team == entity.Team ? _allyTeamColor : _enemyTeamColor;
            fillColor.W = 0.25f;
            Vector4 outlineColor = new Vector4(0, 0, 0, 1);

            uint currentBoneColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 1, 1));
            uint currentBoneColorBis = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1));
            float currentBoneThickness = 4 / entity.Distance;

            drawListPtr.AddCircle(entity.bones2D[2], currentBoneThickness * 10, currentBoneColorBis);
        }

        void DrawBombOrDiffuser(Entity entity)
        {
            if(entity.hasBomb)
            {
                float entityHeight = entity.ViewOffsetV2.Y - entity.PositionV2.Y;
                float entityWidth = entityHeight / 4;

                float circleRadius = (-entityHeight) / 10;

                Vector2 rectTop = new Vector2(entity.ViewOffsetV2.X - entityWidth, entity.ViewOffsetV2.Y);
                Vector2 rectBottom = new Vector2(entity.PositionV2.X + entityWidth, entity.PositionV2.Y);

                Vector2 imgTop = new Vector2(entity.ViewOffsetV2.X - circleRadius, entity.ViewOffsetV2.Y - circleRadius - 100);
                Vector2 imgBottom = new Vector2(entity.ViewOffsetV2.X + circleRadius, entity.ViewOffsetV2.Y + circleRadius - 100);

                drawListPtr.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(bombColor));
                // TODO : Remove ?
                drawListPtr.AddCircleFilled(entity.PositionV2, circleRadius, ImGui.ColorConvertFloat4ToU32(bombColor));

                // Define UV coordinates for the image (usually [0, 0] for top-left and [1, 1] for bottom-right)
                Vector2 uv0 = new Vector2(0, 0);
                Vector2 uv1 = new Vector2(1, 1);

                drawListPtr.AddImage(bombImgPtr, imgTop, imgBottom, uv0, uv1, ImGui.ColorConvertFloat4ToU32(bombColor));
                // drawListPtr.AddImage(bombImgPtr, imgTop, imgBottom, uv0, uv1);
            }
            if(entity.hasDiffuser)
            {
                float entityHeight = entity.ViewOffsetV2.Y - entity.PositionV2.Y;
                float entityWidth = entityHeight / 4;

                float circleRadius = (-entityHeight) / 10;

                Vector2 imgTop = new Vector2(entity.ViewOffsetV2.X - circleRadius, entity.ViewOffsetV2.Y - circleRadius - 100);
                Vector2 imgBottom = new Vector2(entity.ViewOffsetV2.X + circleRadius, entity.ViewOffsetV2.Y + circleRadius - 100);

                Vector2 uv0 = new Vector2(0, 0);
                Vector2 uv1 = new Vector2(1, 1);

                drawListPtr.AddImage(diffuserImgPtr, imgTop, imgBottom, uv0, uv1, ImGui.ColorConvertFloat4ToU32(bombColor));
                // drawListPtr.AddImage(diffuserImgPtr, imgTop, imgBottom, uv0, uv1);
            }
        }

        void DrawKevlarAndHelmet(Entity entity)
        {
            if (entity.hasHelmet)
            {
                float entityHeight = entity.ViewOffsetV2.Y - entity.PositionV2.Y;
                float entityWidth = entityHeight / 4;

                float circleRadius = (-entityHeight) / 10;

                Vector2 rectTop = new Vector2(entity.ViewOffsetV2.X + entityWidth - circleRadius, entity.ViewOffsetV2.Y - circleRadius);
                Vector2 rectBottom = new Vector2(entity.ViewOffsetV2.X + entityWidth + circleRadius, entity.ViewOffsetV2.Y + circleRadius);

                Vector2 uv0 = new Vector2(0, 0);
                Vector2 uv1 = new Vector2(1, 1);

                // drawListPtr.AddImage(helmetImgPtr, rectTop, rectBottom, uv0, uv1, ImGui.ColorConvertFloat4ToU32(diffuserColor));
                drawListPtr.AddImage(helmetImgPtr, rectTop, rectBottom, uv0, uv1);
            }
            else if (entity.hasArmor)
            {
                float entityHeight = entity.ViewOffsetV2.Y - entity.PositionV2.Y;
                float entityWidth = entityHeight / 4;

                float circleRadius = (-entityHeight) / 10;

                Vector2 rectTop = new Vector2(entity.ViewOffsetV2.X + entityWidth - circleRadius, entity.ViewOffsetV2.Y - circleRadius);
                Vector2 rectBottom = new Vector2(entity.ViewOffsetV2.X + entityWidth + circleRadius, entity.ViewOffsetV2.Y + circleRadius);

                Vector2 uv0 = new Vector2(0, 0);
                Vector2 uv1 = new Vector2(1, 1);

                // drawListPtr.AddImage(kevlarImgPtr, rectTop, rectBottom, uv0, uv1, ImGui.ColorConvertFloat4ToU32(diffuserColor));
                drawListPtr.AddImage(kevlarImgPtr, rectTop, rectBottom, uv0, uv1);
            }
        }

        void DrawBones(Entity entity)
        {
            uint currentBoneColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 1, 1));
            uint currentBoneColorBis = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1));
            float currentBoneThickness = 4 / entity.Distance;

            /*
            foreach (Vector2 joint in entity.bones2D)
            {
                drawListPtr.AddCircleFilled(joint, 5, currentBoneColor);
           }
            */

            drawListPtr.AddLine(entity.bones2D[1], entity.bones2D[2], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[1], entity.bones2D[3], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[1], entity.bones2D[6], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[3], entity.bones2D[4], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[6], entity.bones2D[7], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[4], entity.bones2D[5], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[7], entity.bones2D[8], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[1], entity.bones2D[0], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[0], entity.bones2D[9], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[0], entity.bones2D[11], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[9], entity.bones2D[10], currentBoneColorBis, currentBoneThickness);
            drawListPtr.AddLine(entity.bones2D[11], entity.bones2D[12], currentBoneColorBis, currentBoneThickness);

            drawListPtr.AddCircle(entity.bones2D[2], currentBoneThickness * 10, currentBoneColorBis);

        }

        public ConcurrentQueue<Entity> GetEntities()
        {
            return _entities;
        }
        public void UpdateLocalEntities(IEnumerable<Entity> entities)
        {
            _entities = new ConcurrentQueue<Entity>(entities);
        }

        public Entity GetLocalPlayer()
        {
            lock (_entityLock)
            {
                return _localPlayer;
            }
        }

        public void UpdateLocalPlayer(Entity entity)
        {
            lock (_entityLock)
            {
                _localPlayer = entity;
            }
        }

        bool EntityOnScreen(Entity entity)
        {
            return (entity.PositionV2.X > 0 && entity.PositionV2.X < screenSize.X && entity.PositionV2.Y > 0 && entity.PositionV2.Y < screenSize.Y);
        }
    }
}
