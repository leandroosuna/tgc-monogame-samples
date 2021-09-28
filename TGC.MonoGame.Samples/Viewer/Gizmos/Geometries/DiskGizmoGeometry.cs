using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.Samples.Viewer.Gizmos.Geometries
{
    /// <summary>
    ///     Gizmo for drawing Wire Disks.
    /// </summary>
    class DiskGizmoGeometry : RadialGizmoGeometry
    {
        /// <summary>
        ///     Creates a Wire Disk geometry.
        /// </summary>
        /// <param name="graphicsDevice">The GraphicsDevice to bind the geometry.</param>
        /// <param name="subdivisions">The amount of subdivisions the wire disk will take when forming a circle.</param>
        public DiskGizmoGeometry(GraphicsDevice graphicsDevice, int subdivisions) : base(graphicsDevice)
        {
            var positions = GeneratePolygonPositions(subdivisions);
            var originalIndices = GeneratePolygonIndices(subdivisions);

            var subdivisionsTimesTwo = subdivisions * 2;

            var indices = new ushort[subdivisionsTimesTwo];
            var vertices = new VertexPosition[subdivisions];

            Array.Copy(originalIndices, 0, indices, 0, subdivisionsTimesTwo);

            Array.Copy(positions
                .Select(position => new VertexPosition(new Vector3(position.Y, position.X, 0f)))
                .ToArray(), 0, vertices, 0, subdivisions);
            
            InitializeVertices(vertices);
            InitializeIndices(indices);
        }


        /// <summary>
        ///     Calculates the World matrix for the Disk. Note that is initially XZ oriented.
        /// </summary>
        /// <param name="origin">The position in world space.</param>
        /// <param name="normal">The normal of the Disk. The circle will face this vector.</param>
        /// <param name="scale">The radius of the Disk.</param>
        /// <returns>The calculated World matrix</returns>
        public static Matrix CalculateWorld(Vector3 origin, Vector3 normal, float radius)
        {
            var rotation = Matrix.Identity;
            if (!normal.Equals(Vector3.Backward))
            {
                // Rotate our disk!
                // Pretty sure this can be optimized
                
                //rotation.Up = normal;
                var newLeft = Vector3.Cross(Vector3.Up, normal);
                var newUp = Vector3.Cross(normal, newLeft);

                rotation = Matrix.CreateLookAt(origin, origin + normal, newLeft);
                //rotation = Matrix.CreateFromAxisAngle(new Vector3(-0.1169998f, 0.4435054f, 0.4435054f), 0.7700111f);

                Matrix m = Matrix.Identity;
                m.M31 = -normal.X;
                m.M32 = -normal.Y;
                m.M33 = -normal.Z;

                var Right = Vector3.Cross(Vector3.Up, -normal);
                var right = Right / MathF.Sqrt(MathF.Max(0.00001f, Vector3.Dot(Right, Right)));
                m.M11 = right.X;
                m.M12 = right.Y;
                m.M13 = right.Z;

                var c = Vector3.Cross(-normal, right);
                m.M21 = c.X;
                m.M22 = c.Y;
                m.M23 = c.Z;

                rotation = m;
            }

            return Matrix.CreateScale(radius) * rotation;
        }
    }
}
