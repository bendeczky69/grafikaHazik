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
        private float[] colorArray = Array.Empty<float>();
        private GL Gl = null!;

        public unsafe static ModelObjectDescriptor CreateCube(GL Gl)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float[] vertexArray = new float[] {
                // felso lap 0->3
                -0.5f, 0.5f, 0.5f, 0f, 1f, 0f,
                 0.5f, 0.5f, 0.5f, 0f, 1f, 0f,
                 0.5f, 0.5f, -0.5f, 0f, 1f, 0f,
                -0.5f, 0.5f, -0.5f, 0f, 1f, 0f,

                // elso lap
                -0.5f, 0.5f, 0.5f, 0f, 0f, 1f,
                -0.5f, -0.5f, 0.5f, 0f, 0f, 1f,
                 0.5f, -0.5f, 0.5f, 0f, 0f, 1f,
                 0.5f, 0.5f, 0.5f, 0f, 0f, 1f,

                // bal oldali lap
                -0.5f, 0.5f, 0.5f, -1f, 0f, 0f,
                -0.5f, 0.5f, -0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, -0.5f, -1f, 0f, 0f,
                -0.5f, -0.5f, 0.5f, -1f, 0f, 0f,

                // also lap
                -0.5f, -0.5f, 0.5f, 0f, -1f, 0f,
                 0.5f, -0.5f, 0.5f, 0f, -1f, 0f,
                 0.5f, -0.5f, -0.5f, 0f, -1f, 0f,
                -0.5f, -0.5f, -0.5f, 0f, -1f, 0f,

                // hatso lap
                 0.5f, 0.5f, -0.5f, 0f, 0f, -1f,
                -0.5f, 0.5f, -0.5f, 0f, 0f, -1f,
                -0.5f, -0.5f, -0.5f, 0f, 0f, -1f,
                 0.5f, -0.5f, -0.5f, 0f, 0f, -1f,

                // jobb oldali lap
                 0.5f, 0.5f, 0.5f, 1f, 0f, 0f,
                 0.5f, 0.5f, -0.5f, 1f, 0f, 0f,
                 0.5f, -0.5f, -0.5f, 1f, 0f, 0f,
                 0.5f, -0.5f, 0.5f, 1f, 0f, 0f,
            };

            float[] cubeColors = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,

                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,

                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,

                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
            };

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                10, 11, 8,

                12, 14, 13,
                12, 15, 14,

                17, 16, 19,
                17, 19, 18,

                20, 22, 21,
                20, 23, 22
            };

            // pozicio es normalis buffer feltoltese es attributumok beallitasa (0 = pozicio, 2 = normalis)
            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);

            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint vertexSize = offsetNormals + 3 * sizeof(float);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            // szin buffer feltoltese es attributum beallitasa (1 = szin)
            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)cubeColors.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            // index buffer feltoltese
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
                colorArray = cubeColors,
                Gl = Gl
            };
        }

        // frissiti a kocka felso oldalanak 4 csucsszinet a gpu bufferben
        public unsafe void UpdateTopFaceColor(Vector3 rgb)
        {
            if (colorArray.Length < 16 || Colors == 0)
                return;

            for (int v = 0; v < 4; v++)
            {
                int idx = v * 4;
                colorArray[idx + 0] = rgb.X;
                colorArray[idx + 1] = rgb.Y;
                colorArray[idx + 2] = rgb.Z;
                colorArray[idx + 3] = 1.0f;
            }

            // csak a modositott szinadatokat kuldjuk at a gpu-ra
            Gl.BindBuffer(GLEnum.ArrayBuffer, Colors);
            fixed (float* ptr = colorArray)
            {
                Gl.BufferSubData(GLEnum.ArrayBuffer, 0, (nuint)(16 * sizeof(float)), ptr);
            }
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
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