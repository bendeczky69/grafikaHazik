using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace GrafikaSzeminarium
{
    internal class ModelObjectDescriptor : IDisposable
    {
        private bool disposedValue;

        public uint Vao { get; private set; }
        public uint Vertices { get; private set; }
        public uint Colors { get; private set; }
        public uint Indices { get; private set; }
        public uint IndexArrayLength { get; private set; }

        private GL Gl;

        public unsafe static ModelObjectDescriptor CreateBarrelPlank(GL Gl, bool flatNormals, float width = 1.0f, float height = 2.0f, int plankCount = 18)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            // A fél lépésszög (360 / (2 * 18) = 10 fok) radiánban
            float halfAngleRad = MathF.PI / 180.0f * (180.0f / plankCount);
            float outX = MathF.Sin(halfAngleRad);
            float outZ = MathF.Cos(halfAngleRad);

            Vector3 leftNormal = flatNormals
                ? new Vector3(0f, 0f, 1f)
                : new Vector3(-outX, 0f, outZ);

            Vector3 rightNormal = flatNormals
                ? new Vector3(0f, 0f, 1f)
                : new Vector3(outX, 0f, outZ);

            // 4 csucs pozicio
            float[] vertexArray = new float[]
            {
                -halfW, -halfH, 0f,  leftNormal.X,  leftNormal.Y,  leftNormal.Z,
                -halfW,  halfH, 0f,  leftNormal.X,  leftNormal.Y,  leftNormal.Z,
                 halfW,  halfH, 0f,  rightNormal.X, rightNormal.Y, rightNormal.Z,
                 halfW, -halfH, 0f,  rightNormal.X, rightNormal.Y, rightNormal.Z,
            };

            float[] colorArray = new float[]
            {
                0.78f, 0.52f, 0.27f, 1.0f,
                0.78f, 0.52f, 0.27f, 1.0f,
                0.78f, 0.52f, 0.27f, 1.0f,
                0.78f, 0.52f, 0.27f, 1.0f,
            };

            uint[] indexArray = new uint[]
            {
                0, 3, 2,
                0, 2, 1
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);

            uint offsetPos = 0;
            uint offsetNormals = 3 * sizeof(float);
            uint vertexSize = 6 * sizeof(float);

            // 0 = position, 2 = normal (a VertexShader.vert alapján)
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);

            // 1 = color
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            Gl.BindVertexArray(0);

            return new ModelObjectDescriptor()
            {
                Vao = vao,
                Vertices = vertices,
                Colors = colors,
                Indices = indices,
                IndexArrayLength = (uint)indexArray.Length,
                Gl = Gl
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (Gl != null)
                {
                    Gl.DeleteBuffer(Vertices);
                    Gl.DeleteBuffer(Colors);
                    Gl.DeleteBuffer(Indices);
                    Gl.DeleteVertexArray(Vao);
                }

                disposedValue = true;
            }
        }

        ~ModelObjectDescriptor()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}