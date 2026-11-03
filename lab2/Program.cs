using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Szeminarium;

namespace lab2
{
    internal class Program
    {
        private static IWindow graphicWindow;
        private static GL Gl;

        struct SmallCubeData
        {
            public uint Vao;
            public Vector3D<float> Position;
        }

        private static List<SmallCubeData> rubikCubes = new List<SmallCubeData>();

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec4 outCol;
        
        void main()
        {
            outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        
        in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        private static uint program;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Lab2_1 Rubik Kocka";
            windowOptions.Size = new Vector2D<int>(800, 800);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            Gl.DeleteProgram(program);
        }

        private static float[] GenerateRubikColors(int x, int y, int z)
        {
            float[] colors = new float[24 * 4];
            
            float[] black = { 0f, 0f, 0f, 1.0f };
            float[] red = { 1.0f, 0.0f, 0.0f, 1.0f };       //front
            float[] yellow = { 1.0f, 1.0f, 0.0f, 1.0f };    // back
            float[] white = { 1.0f, 1.0f, 1.0f, 1.0f };     // left
            float[] orange = { 1.0f, 0.5f, 0.0f, 1.0f };    // right
            float[] blue = { 0.0f, 0.0f, 1.0f, 1.0f };      // top
            float[] green = { 0.0f, 1.0f, 0.0f, 1.0f };     // bottom

            void SetFace(int faceIndex, float[] c)
            {
                for (int i = 0; i < 4; i++)
                {
                    colors[(faceIndex * 4 + i) * 4 + 0] = c[0];
                    colors[(faceIndex * 4 + i) * 4 + 1] = c[1];
                    colors[(faceIndex * 4 + i) * 4 + 2] = c[2];
                    colors[(faceIndex * 4 + i) * 4 + 3] = c[3];
                }
            }

            // ha kilog a szelere megkapja a szint kulonben szurke
            SetFace(0, z == 1 ? red : black);
            SetFace(1, z == -1 ? orange : black);
            SetFace(2, x == -1 ? blue : black);
            SetFace(3, x == 1 ? green : black);
            SetFace(4, y == 1 ? white : black);
            SetFace(5, y == -1 ? yellow : black);

            return colors;
        }

        private static unsafe void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.LightGray);
            
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            
            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            float[] vertices = {
                // front
                -0.5f, -0.5f,  0.5f,   0.5f, -0.5f,  0.5f,   0.5f,  0.5f,  0.5f,  -0.5f,  0.5f,  0.5f,
                // back
                -0.5f, -0.5f, -0.5f,  -0.5f,  0.5f, -0.5f,   0.5f,  0.5f, -0.5f,   0.5f, -0.5f, -0.5f,
                // left
                -0.5f, -0.5f, -0.5f,  -0.5f, -0.5f,  0.5f,  -0.5f,  0.5f,  0.5f,  -0.5f,  0.5f, -0.5f,
                // right
                 0.5f, -0.5f, -0.5f,   0.5f,  0.5f, -0.5f,   0.5f,  0.5f,  0.5f,   0.5f, -0.5f,  0.5f,
                // top
                -0.5f,  0.5f, -0.5f,  -0.5f,  0.5f,  0.5f,   0.5f,  0.5f,  0.5f,   0.5f,  0.5f, -0.5f,
                // bottom
                -0.5f, -0.5f, -0.5f,   0.5f, -0.5f, -0.5f,   0.5f, -0.5f,  0.5f,  -0.5f, -0.5f,  0.5f
            };

            uint[] indices = {
                 0,  1,  2,  0,  2,  3, // front
                 4,  5,  6,  4,  6,  7, // back
                 8,  9, 10,  8, 10, 11, // left
                12, 13, 14, 12, 14, 15, // Right
                16, 17, 18, 16, 18, 19, // top
                20, 21, 22, 20, 22, 23  // bottom
            };

            uint posVbo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, posVbo);
            fixed (float* v = vertices)
                Gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, GLEnum.StaticDraw);

            uint ebo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, ebo);
            fixed (uint* i = indices)
                Gl.BufferData(GLEnum.ArrayBuffer,(nuint)(indices.Length * sizeof(uint)), i,GLEnum.StaticDraw);

            // a 27 kocka vao es pozicio beallitasa
            float offset = 1.05f; // res a kockak kozott
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        uint vao = Gl.GenVertexArray();
                        Gl.BindVertexArray(vao);

                        Gl.BindBuffer(GLEnum.ArrayBuffer, posVbo);
                        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(0);

                        float[] colors = GenerateRubikColors(x, y, z);
                        uint colorVbo = Gl.GenBuffer();
                        Gl.BindBuffer(GLEnum.ArrayBuffer, colorVbo);
                        fixed (float* c = colors)
                            Gl.BufferData(GLEnum.ArrayBuffer, (nuint)(colors.Length * sizeof(float)), c, GLEnum.StaticDraw);
                        
                        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(1);

                        Gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);

                        rubikCubes.Add(new SmallCubeData 
                        { 
                            Vao = vao, 
                            Position = new Vector3D<float>(x * offset, y * offset, z * offset) 
                        });
                    }
                }
            }
            Gl.BindVertexArray(0);

            Gl.UseProgram(program);
            
            // erdekes szog
            var viewMatrix = Matrix4X4.CreateLookAt(new Vector3D<float>(4f, 3.5f, 5f), Vector3D<float>.Zero, Vector3D<float>.UnitY);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 4), 800f / 800f, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            foreach (var cube in rubikCubes)
            {
                var modelMatrix = Matrix4X4.CreateTranslation(cube.Position);
                SetMatrix(modelMatrix, ModelMatrixVariableName);

                Gl.BindVertexArray(cube.Vao);
                Gl.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, null);
            }
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}