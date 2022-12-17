﻿using FargowiltasSouls.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Graphics.Shaders;

// Lowest namespace for convenience. 
namespace FargowiltasSouls
{
    public class PrimDrawer
    {
        #region Fields/Properties

        public BasicEffect BaseEffect;
        public MiscShaderData Shader;
        public WidthTrailFunction WidthFunc;
        public ColorTrailFunction ColorFunc;

        /// <summary>
        /// This allows the width to dynamically change along the trail if desired.
        /// </summary>
        /// <param name="trailInterpolant">How far (0-1) the current position is on the trail</param>
        /// <returns></returns>
        public delegate float WidthTrailFunction(float trailInterpolant);

        /// <summary>
        /// This allows the color to dynamically change along the trail if desired.
        /// </summary>
        /// <param name="trailInterpolant">How far (0-1) the current position is on the trail</param>
        /// <returns></returns>
        public delegate Color ColorTrailFunction(float trailInterpolant);
        #endregion

        #region Methods
        /// <summary>
        /// Cache this and use it to call draw from.
        /// </summary>
        /// <param name="widthFunc">The width function</param>
        /// <param name="colorFunc">The color function</param>
        /// <param name="shader">The shader, if any</param>
        public PrimDrawer(WidthTrailFunction widthFunc, ColorTrailFunction colorFunc, MiscShaderData shader = null)
        {
            WidthFunc = widthFunc;
            ColorFunc = colorFunc;
            Shader = shader;
            // Create a basic effect.
            BaseEffect = new BasicEffect(Main.instance.GraphicsDevice)
            {
                VertexColorEnabled = true,
                TextureEnabled = false
            };
            UpdateBaseEffect(out _, out _);
        }

        private void UpdateBaseEffect(out Matrix effectProjection, out Matrix effectView)
        {
            // Get the screen bounds.
            int height = Main.instance.GraphicsDevice.Viewport.Height;

            // Get the zoom and the scaling zoom matrix from it.
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

            // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
            effectView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

            // Offset the matrix to the appropriate position based off the height.
            effectView *= Matrix.CreateTranslation(0f, -height, 0f);

            // Flip the matrix around 180 degrees.
            effectView *= Matrix.CreateRotationZ(MathHelper.Pi);

            // Account for the inverted gravity effect.
            if (Main.LocalPlayer.gravDir == -1f)
                effectView *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

            // And account for the current zoom.
            effectView *= zoomScaleMatrix;

            // Create a projection in 2D using the screen width/height, and the zoom.
            effectProjection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth * zoom.X, 0f, Main.screenHeight * zoom.Y, 0f, 1f) * zoomScaleMatrix;
            BaseEffect.View = effectView;
            BaseEffect.Projection = effectProjection;
        }

        public void DrawPrims(List<Vector2> basePoints, Vector2 baseOffset, int totalTrailPoints)
        {
            // Set the correct rasterizer state.
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            // First, we need to offset the points by the base offset. This is almost always going to be -Main.screenPosition, but is changeable for flexability.
            List<Vector2> drawPointsList = CorrectlyOffsetPoints(basePoints, baseOffset, totalTrailPoints);

            // If the list is too short, any points in it are NaNs, or they are all the same point, return.
            if (drawPointsList.Count < 2 || drawPointsList.Any((drawPoint) => drawPoint.HasNaNs()) || drawPointsList.All(point => point == drawPointsList[0]))
                return;

            UpdateBaseEffect(out Matrix projection, out Matrix view);

            // Get an array of primitive triangles to pass through. Color data etc is stored in the struct.
            BasePrimTriangle[] pointVertices = CreatePrimitiveVertices(drawPointsList);
            // Get an array of the indices for each primitive triangle.
            short[] triangleIndices = CreatePrimitiveIndices(drawPointsList.Count);

            // If these are too short, or the indices isnt fully completed, return.
            if (triangleIndices.Length % 6 != 0 || pointVertices.Length <= 3)
                return;

            // If the shader exists, set the correct view and apply it.
            if (Shader != null)
            {
                Shader.Shader.Parameters["uWorldViewProjection"].SetValue(view * projection);
                Shader.Apply();
            }
            // Else, apply the base effect.
            else
                BaseEffect.CurrentTechnique.Passes[0].Apply();

            // Draw the prims! Also apply the main pixel shader. Specify the type of primitives this should be expecting, and pass through the array of the struct using the correct interface.
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, pointVertices, 0, pointVertices.Length, triangleIndices, 0, triangleIndices.Length / 3);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        private static List<Vector2> CorrectlyOffsetPoints(List<Vector2> basePoints, Vector2 baseOffset, int totalPoints)
        {
            List<Vector2> newList = new();
            for (int i = 0; i < basePoints.Count; i++)
            {
                // Don't incorporate points that are zeroed out.
                // They are almost certainly a result of incomplete oldPos arrays.
                if (basePoints.ElementAt(i) == Vector2.Zero)
                    continue;

                newList.Add(basePoints.ElementAt(i) + baseOffset);
            }
            // Smooth the list using a bezier curve. More info on how this works can be found in the class.
            PointSmoother pointSmoother = new(newList.ToArray());
            return newList.Count <= 1 ? newList : pointSmoother.GetSmoothedPoints(totalPoints);
        }

        private BasePrimTriangle[] CreatePrimitiveVertices(List<Vector2> points)
        {
            List<BasePrimTriangle> rectPrims = new();

            // Loop throught the points, ignoring the final one as it doesnt need to connect to anything.
            for (int i = 0; i < points.Count - 1; i++)
            {
                // How far along in the list of points we are.
                float trailCompletionRatio = i / (float)points.Count;

                // Get the current width and color from the delegates.
                float width = WidthFunc(trailCompletionRatio);
                Color color = ColorFunc(trailCompletionRatio);

                // Get the current point, and the point ahead (next in the list).
                Vector2 point = points[i];
                Vector2 aheadPoint = points[i + 1];

                // Get the direction to the ahead point, not calling DirectionTo for performance.
                Vector2 directionToAhead = (aheadPoint - point).SafeNormalize(Vector2.Zero);

                // Get the left and right coordinates, with the current trail completion for the X value.
                Vector2 leftCurrentTextureCoord = new(trailCompletionRatio, 0f);
                Vector2 rightCurrentTextureCoord = new(trailCompletionRatio, 1f);

                // Point 90 degrees away from the direction towards the next point, and use it to mark the edges of a rectangle.
                // This doesn't use RotatedBy for the sake of performance as well.
                Vector2 sideDirection = new(-directionToAhead.Y, directionToAhead.X);

                // This is defining a rectangle based on two triangles.
                // See https://cdn.discordapp.com/attachments/770382926545813515/1050185533780934766/a.png for a visual of this.
                // The two triangles can be imagined as the point being the tip, and the sides being the opposite side.
                // How to connect it all is defined in the CreatePrimitiveIndices() function.
                // The resulting rectangles combined are what make the trail itself.
                rectPrims.Add(new BasePrimTriangle(point - sideDirection * width, color, leftCurrentTextureCoord));
                rectPrims.Add(new BasePrimTriangle(point + sideDirection * width, color, rightCurrentTextureCoord));
            }

            return rectPrims.ToArray();
        }

        private static short[] CreatePrimitiveIndices(int totalPoints)
        {
            // What this is doing is basically representing each point on the vertices list as
            // indices. These indices should come together to create a tiny rectangle that acts
            // as a segment on the trail. This is achieved here by splitting the indices (or rather, points)
            // into 2 triangles, which requires 6 points. This is the aforementioned connecting of the
            // triangles using the indices.

            // Get the total number of indices, -1 because the last point doesn't connect to anything, and
            // * 6 because each point has 6 indices.
            int totalIndices = (totalPoints - 1) * 6;

            // Create an array to hold them with the correct size.
            short[] indices = new short[totalIndices];

            // Loop through the points, creating each indice.
            for (int i = 0; i < totalPoints - 2; i++)
            {
                // This might look confusing, but its basically going around the rectangle, and adding the points in the appropriate place.
                // Use this as a visual aid. https://cdn.discordapp.com/attachments/864078125657751555/1050218596623716413/image.png
                int startingTriangleIndex = i * 6;
                int connectToIndex = i * 2;
                indices[startingTriangleIndex] = (short)connectToIndex;
                indices[startingTriangleIndex + 1] = (short)(connectToIndex + 1);
                indices[startingTriangleIndex + 2] = (short)(connectToIndex + 2);
                indices[startingTriangleIndex + 3] = (short)(connectToIndex + 2);
                indices[startingTriangleIndex + 4] = (short)(connectToIndex + 1);
                indices[startingTriangleIndex + 5] = (short)(connectToIndex + 3);
            }
            // Return the array.
            return indices;
        }
        #endregion
    }
}