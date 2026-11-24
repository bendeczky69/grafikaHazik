using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Szeminarium;
using System.Numerics;

namespace lab2
{
    internal class Program
    {
        private static IWindow graphicWindow = null!;
        private static GL Gl = null!;
        private static ImGuiController imGuiController = null!;

        struct SmallCubeData
        {
            public uint Vao;
            public Vector3D<float> Position;
            public int LogicalX;
        }

        private static IKeyboard? primaryKeyboard;

        private static Vector3D<float> cameraPos = new Vector3D<float>(4f, 3.5f, 5f);
        private static Vector3D<float> cameraFront = new Vector3D<float>(-0.6f, -0.4f, -0.7f);
        private static Vector3D<float> cameraUp = Vector3D<float>.UnitY;
        private static float yaw = -128.0f;
        private static float pitch = -30.0f;

        // szelet forgatasi animacio valtozoi
        private static bool isAnimating = false;
        private static float currentAngle = 0f;
        private static float targetAngle = 0f;
        private static float animationSpeed = (float)Math.PI;

        private static List<SmallCubeData> rubikCubes = new List<SmallCubeData>();

        // shader uniform nevek
        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string SliceMatrixVariableName = "uSliceRotation";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShininessVariableName = "uShininess";
        private const string AmbientStrengthVariableName = "uAmbientStrength";
        private const string DiffuseStrengthVariableName = "uDiffuseStrength";
        private const string SpecularStrengthVariableName = "uSpecularStrength";

        // phong fenyforras es anyagi parameterek
        private static Vector3 lightColor = new Vector3(1.0f, 1.0f, 1.0f);
        private static Vector3 lightPos = new Vector3(4.0f, 6.0f, 5.0f);
        private static float shininess = 32.0f;
        private static float ambientStrength = 0.25f;
        private static float diffuseStrength = 0.75f;
        private static float specularStrength = 0.5f;

        // vertex shader: pozicio, szin es normalis atadasa vilagkoordinataba
        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNormal;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uSliceRotation;

        out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPos;

        void main()
        {
            outCol = vCol;
            vec4 transformedPos = uSliceRotation * uModel * vec4(vPos, 1.0);
            outWorldPos = transformedPos.xyz;

            // a szelet forgatasat es a modell forgatasat a normalisra is alkalmazzuk
            mat3 sliceRot3 = mat3(uSliceRotation);
            outNormal = normalize(sliceRot3 * (uNormal * vNormal));

            gl_Position = uProjection * uView * transformedPos;
        }
        ";

        // fragment shader: phong lokalis megvilagitasi modell (ambiens + diffuz + spekularis)
        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        uniform vec3 uLightColor;
        uniform vec3 uLightPos;
        uniform vec3 uViewPos;
        uniform float uShininess;
        uniform float uAmbientStrength;
        uniform float uDiffuseStrength;
        uniform float uSpecularStrength;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPos;

        void main()
        {
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(uLightPos - outWorldPos);
            vec3 viewDir = normalize(uViewPos - outWorldPos);
            vec3 reflectDir = reflect(-lightDir, norm);

            // kornyezeti (ambient) komponens
            vec3 ambient = uAmbientStrength * uLightColor;

            // diffuz (diffuse) komponens
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * uDiffuseStrength * uLightColor;

            // spekularis (specular) csillogas komponens
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
            vec3 specular = spec * uSpecularStrength * uLightColor;

            vec3 litColor = (ambient + diffuse + specular) * outCol.rgb;
            FragColor = vec4(litColor, outCol.a);
        }
        ";

        private static uint program;

        static void Main(string[] args)
        {
            // ablak letrehozasa es esemenyek bekotese
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "lab3_3 rubik kocka - phong";
            windowOptions.Size = new Vector2D<int>(1000, 800);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            imGuiController?.Dispose();
            Gl.DeleteProgram(program);
        }

        // legeneralja a 27 kiskocka csucsszineit
        private static float[] GenerateRubikColors(int x, int y, int z)
        {
            float[] colors = new float[24 * 4];

            float[] black = { 0.08f, 0.08f, 0.08f, 1.0f };
            float[] red = { 1.0f, 0.0f, 0.0f, 1.0f };       // elso lap (+z)
            float[] yellow = { 1.0f, 1.0f, 0.0f, 1.0f };    // hatso lap (-z)
            float[] white = { 1.0f, 1.0f, 1.0f, 1.0f };     // bal lap (-x)
            float[] orange = { 1.0f, 0.5f, 0.0f, 1.0f };    // jobb lap (+x)
            float[] blue = { 0.0f, 0.0f, 1.0f, 1.0f };      // felso lap (+y)
            float[] green = { 0.0f, 1.0f, 0.0f, 1.0f };     // also lap (-y)

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

            SetFace(0, z == 1 ? red : black);
            SetFace(1, z == -1 ? orange : black);
            SetFace(2, x == -1 ? blue : black);
            SetFace(3, x == 1 ? green : black);
            SetFace(4, y == 1 ? white : black);
            SetFace(5, y == -1 ? yellow : black);

            return colors;
        }

        // elindit egy +90 vagy -90 fokos szeletforgatast, ha epp nem fut animacio
        private static void TriggerSliceRotation(float deltaAngleRad)
        {
            if (!isAnimating)
            {
                targetAngle += deltaAngleRad;
                isAnimating = true;
            }
        }

        // kamera iranyvektoranak frissitese yaw es pitch szogek alapjan
        private static void UpdateCameraFront()
        {
            if (pitch > 89.0f) pitch = 89.0f;
            if (pitch < -89.0f) pitch = -89.0f;

            Vector3D<float> front;
            front.X = (float)(Math.Cos(yaw * Math.PI / 180.0) * Math.Cos(pitch * Math.PI / 180.0));
            front.Y = (float)Math.Sin(pitch * Math.PI / 180.0);
            front.Z = (float)(Math.Sin(yaw * Math.PI / 180.0) * Math.Cos(pitch * Math.PI / 180.0));
            cameraFront = Vector3D.Normalize(front);
        }

        private static unsafe void GraphicWindow_Load()
        {
            IInputContext input = graphicWindow.CreateInput();
            primaryKeyboard = input.Keyboards.FirstOrDefault();

            if (primaryKeyboard != null)
            {
                primaryKeyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = graphicWindow.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.LightGray);

            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            graphicWindow.FramebufferResize += s =>
            {
                Gl.Viewport(s);
            };

            imGuiController = new ImGuiController(Gl, graphicWindow, input);

            // vertex es fragment shader forditasa es linkelese
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("vertex shader hiba: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("fragment shader hiba: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            // kiskocka csucsai (6 lap x 4 csucs)
            float[] vertices = {
                // front (+z)
                -0.5f, -0.5f,  0.5f,   0.5f, -0.5f,  0.5f,   0.5f,  0.5f,  0.5f,  -0.5f,  0.5f,  0.5f,
                // back (-z)
                -0.5f, -0.5f, -0.5f,  -0.5f,  0.5f, -0.5f,   0.5f,  0.5f, -0.5f,   0.5f, -0.5f, -0.5f,
                // left (-x)
                -0.5f, -0.5f, -0.5f,  -0.5f, -0.5f,  0.5f,  -0.5f,  0.5f,  0.5f,  -0.5f,  0.5f, -0.5f,
                // right (+x)
                 0.5f, -0.5f, -0.5f,   0.5f,  0.5f, -0.5f,   0.5f,  0.5f,  0.5f,   0.5f, -0.5f,  0.5f,
                // top (+y)
                -0.5f,  0.5f, -0.5f,  -0.5f,  0.5f,  0.5f,   0.5f,  0.5f,  0.5f,   0.5f,  0.5f, -0.5f,
                // bottom (-y)
                -0.5f, -0.5f, -0.5f,   0.5f, -0.5f, -0.5f,   0.5f, -0.5f,  0.5f,  -0.5f, -0.5f,  0.5f
            };

            // laponkent azonos normalisvektorok a phong megvilagitashoz (6 lap x 4 csucs x 3 komponens)
            float[] normals = {
                 0f,  0f,  1f,   0f,  0f,  1f,   0f,  0f,  1f,   0f,  0f,  1f,
                 0f,  0f, -1f,   0f,  0f, -1f,   0f,  0f, -1f,   0f,  0f, -1f,
                -1f,  0f,  0f,  -1f,  0f,  0f,  -1f,  0f,  0f,  -1f,  0f,  0f,
                 1f,  0f,  0f,   1f,  0f,  0f,   1f,  0f,  0f,   1f,  0f,  0f,

                 0f,  1f,  0f,   0f,  1f,  0f,   0f,  1f,  0f,   0f,  1f,  0f,
                 0f, -1f,  0f,   0f, -1f,  0f,   0f, -1f,  0f,   0f, -1f,  0f
            };

            uint[] indices = {
                 0,  1,  2,  0,  2,  3,
                 4,  5,  6,  4,  6,  7,
                 8,  9, 10,  8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };

            // kozos pozicio vbo
            uint posVbo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, posVbo);
            fixed (float* v = vertices)
                Gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, GLEnum.StaticDraw);

            // kozos normalis vbo
            uint normalVbo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, normalVbo);
            fixed (float* n = normals)
                Gl.BufferData(GLEnum.ArrayBuffer, (nuint)(normals.Length * sizeof(float)), n, GLEnum.StaticDraw);

            // kozos index buffer (ebo)
            uint ebo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
            fixed (uint* i = indices)
                Gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), i, GLEnum.StaticDraw);

            // a 27 kiskocka vao-janak es poziciojanak letrehozasa
            float offset = 1.05f;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        uint vao = Gl.GenVertexArray();
                        Gl.BindVertexArray(vao);

                        // 0 = pozicio
                        Gl.BindBuffer(GLEnum.ArrayBuffer, posVbo);
                        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(0);

                        // 1 = szin
                        float[] colors = GenerateRubikColors(x, y, z);
                        uint colorVbo = Gl.GenBuffer();
                        Gl.BindBuffer(GLEnum.ArrayBuffer, colorVbo);
                        fixed (float* c = colors)
                            Gl.BufferData(GLEnum.ArrayBuffer, (nuint)(colors.Length * sizeof(float)), c, GLEnum.StaticDraw);
                        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(1);

                        // 2 = normalis
                        Gl.BindBuffer(GLEnum.ArrayBuffer, normalVbo);
                        Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(2);

                        // index buffer hozzaferes a vao-hoz
                        Gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);

                        rubikCubes.Add(new SmallCubeData
                        {
                            Vao = vao,
                            Position = new Vector3D<float>(x * offset, y * offset, z * offset),
                            LogicalX = x
                        });
                    }
                }
            }
            Gl.BindVertexArray(0);

            Gl.UseProgram(program);

            // vetitesi matrix beallitasa
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 4), 1000f / 800f, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);

            UpdateCameraFront();
        }

        // billentyuzetes szeletforgatas (+90 / -90 fok)
        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key == Key.Space)
            {
                TriggerSliceRotation((float)(Math.PI / 2));
            }
            else if (key == Key.Backspace)
            {
                TriggerSliceRotation(-(float)(Math.PI / 2));
            }
        }

        // kamera mozgas, forgatas es szelet animacio frissitese
        private static void GraphicWindow_Update(double deltaTime)
        {
            imGuiController.Update((float)deltaTime);

            if (primaryKeyboard != null)
            {
                // kamera mozgatas nyilakkal
                float cameraSpeed = 3.5f * (float)deltaTime;
                if (primaryKeyboard.IsKeyPressed(Key.Up)) cameraPos += cameraFront * cameraSpeed;
                if (primaryKeyboard.IsKeyPressed(Key.Down)) cameraPos -= cameraFront * cameraSpeed;
                if (primaryKeyboard.IsKeyPressed(Key.Left)) cameraPos -= Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp)) * cameraSpeed;
                if (primaryKeyboard.IsKeyPressed(Key.Right)) cameraPos += Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp)) * cameraSpeed;

                // kamera forgatas wasd billentyukkel
                float turnSpeed = 60f * (float)deltaTime;
                if (primaryKeyboard.IsKeyPressed(Key.A)) yaw -= turnSpeed;
                if (primaryKeyboard.IsKeyPressed(Key.D)) yaw += turnSpeed;
                if (primaryKeyboard.IsKeyPressed(Key.W)) pitch += turnSpeed;
                if (primaryKeyboard.IsKeyPressed(Key.S)) pitch -= turnSpeed;

                UpdateCameraFront();
            }

            // szelet animacio leptetese a celszog fele
            if (isAnimating)
            {
                if (currentAngle < targetAngle)
                {
                    currentAngle += animationSpeed * (float)deltaTime;
                    if (currentAngle >= targetAngle)
                    {
                        currentAngle = targetAngle;
                        isAnimating = false;
                    }
                }
                else if (currentAngle > targetAngle)
                {
                    currentAngle -= animationSpeed * (float)deltaTime;
                    if (currentAngle <= targetAngle)
                    {
                        currentAngle = targetAngle;
                        isAnimating = false;
                    }
                }
            }
        }

        // kepkocka kirajzolasa phong uniformokkal es imgui felulettel
        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            // phong feny- es kamera uniformok atadasa
            SetUniform3(LightColorVariableName, lightColor);
            SetUniform3(LightPositionVariableName, lightPos);
            SetUniform3(ViewPositionVariableName, new Vector3(cameraPos.X, cameraPos.Y, cameraPos.Z));
            SetUniform1(ShininessVariableName, shininess);
            SetUniform1(AmbientStrengthVariableName, ambientStrength);
            SetUniform1(DiffuseStrengthVariableName, diffuseStrength);
            SetUniform1(SpecularStrengthVariableName, specularStrength);

            // nezetmatrix frissitese
            var viewMatrix = Matrix4X4.CreateLookAt(cameraPos, cameraPos + cameraFront, cameraUp);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            // 27 kiskocka kirajzolasa
            foreach (var cube in rubikCubes)
            {
                var modelMatrix = Matrix4X4.CreateTranslation(cube.Position);
                SetModelMatrix(modelMatrix);

                Matrix4X4<float> sliceMatrix = Matrix4X4<float>.Identity;
                if (cube.LogicalX == 1)
                {
                    sliceMatrix = Matrix4X4.CreateRotationX(currentAngle);
                }

                SetMatrix(sliceMatrix, SliceMatrixVariableName);

                Gl.BindVertexArray(cube.Vao);
                Gl.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, null);
            }
            Gl.BindVertexArray(0);

            // imgui vezerloablak: megvilagitas szin, fenyforras pozicio es forgatas gombok
            ImGui.Begin("lab3_3 phong es forgatas vezerles", ImGuiWindowFlags.AlwaysAutoResize);

            // forgatasok navigacioja gombokkal
            ImGui.Text("szelet forgatas (jobb x=1 oldal)");
            if (ImGui.Button("forgatas +90 fok"))
            {
                TriggerSliceRotation((float)(Math.PI / 2));
            }
            ImGui.SameLine();
            if (ImGui.Button("forgatas -90 fok"))
            {
                TriggerSliceRotation(-(float)(Math.PI / 2));
            }

            ImGui.Separator();
            ImGui.Text("kamera forgatas gombokkal");
            if (ImGui.Button("balra (yaw-)"))
            {
                yaw -= 10f;
                UpdateCameraFront();
            }
            ImGui.SameLine();
            if (ImGui.Button("jobbra (yaw+)"))
            {
                yaw += 10f;
                UpdateCameraFront();
            }
            if (ImGui.Button("fel (pitch+)"))
            {
                pitch += 10f;
                UpdateCameraFront();
            }
            ImGui.SameLine();
            if (ImGui.Button("le (pitch-)"))
            {
                pitch -= 10f;
                UpdateCameraFront();
            }

            // megvilagitas szinosszetetele 3 csuszka segitsegevel
            ImGui.Separator();
            ImGui.Text("megvilagitas szinosszetetele");
            ImGui.SliderFloat("feny r", ref lightColor.X, 0.0f, 1.0f);
            ImGui.SliderFloat("feny g", ref lightColor.Y, 0.0f, 1.0f);
            ImGui.SliderFloat("feny b", ref lightColor.Z, 0.0f, 1.0f);

            // fenyforras terbeli pozicioja 3 szovveges mezovel
            ImGui.Separator();
            ImGui.Text("fenyforras pozicioja (x, y, z)");
            ImGui.InputFloat("feny x", ref lightPos.X);
            ImGui.InputFloat("feny y", ref lightPos.Y);
            ImGui.InputFloat("feny z", ref lightPos.Z);

            // opcionális phong egyutthatok finomhangolasa
            ImGui.Separator();
            ImGui.Text("phong egyutthatok");
            ImGui.SliderFloat("ambiens", ref ambientStrength, 0.0f, 1.0f);
            ImGui.SliderFloat("diffuz", ref diffuseStrength, 0.0f, 1.0f);
            ImGui.SliderFloat("spekularis", ref specularStrength, 0.0f, 1.0f);
            ImGui.SliderFloat("fenyesseg", ref shininess, 1.0f, 128.0f);

            ImGui.End();

            imGuiController.Render();
            CheckError();
        }

        // modellmatrix es normalmatrix atadasa a shadernek
        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform nem talalhato a shaderben.");
            }

            var modelWithoutTranslation = new Matrix4X4<float>(
                modelMatrix.Row1,
                modelMatrix.Row2,
                modelMatrix.Row3,
                new Vector4D<float>(0f, 0f, 0f, 1f)
            );

            Matrix4X4.Invert<float>(modelWithoutTranslation, out var modelInverse);
            var normalMatrix4 = Matrix4X4.Transpose(modelInverse);
            Matrix3X3<float> normalMatrix3 = new Matrix3X3<float>(
                normalMatrix4.M11, normalMatrix4.M12, normalMatrix4.M13,
                normalMatrix4.M21, normalMatrix4.M22, normalMatrix4.M23,
                normalMatrix4.M31, normalMatrix4.M32, normalMatrix4.M33
            );

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix3);
            CheckError();
        }

        // lebegopontos uniform valtozo beallitasa
        private static unsafe void SetUniform1(string uniformName, float value)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform nem talalhato a shaderben.");
            }

            Gl.Uniform1(location, value);
            CheckError();
        }

        // haromelemu vektor uniform valtozo beallitasa
        private static unsafe void SetUniform3(string uniformName, Vector3 value)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform nem talalhato a shaderben.");
            }

            Gl.Uniform3(location, value.X, value.Y, value.Z);
            CheckError();
        }

        // 4x4-es matrix uniform valtozo beallitasa
        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform nem talalhato a shaderben.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("gl.geterror() hibakod: " + error.ToString());
        }
    }
}