using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow = null!;
        private static GL Gl = null!;
        private static ImGuiController imGuiController = null!;

        private static ModelObjectDescriptor cube = null!;
        private static CameraDescriptor camera = new CameraDescriptor();
        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";

        private const string AmbientStrengthVariableName = "uAmbientStrength";
        private const string DiffuseStrengthVariableName = "uDiffuseStrength";
        private const string SpecularStrengthVariableName = "uSpecularStrength";

        private static float shininess = 50f;
        private static Vector3 lightColor = new Vector3(1f, 1f, 1f);
        private static Vector3 ambientStrength = new Vector3(0.15f, 0.15f, 0.15f);
        private static Vector3 diffuseStrength = new Vector3(0.6f, 0.6f, 0.6f);
        private static Vector3 specularStrength = new Vector3(0.6f, 0.6f, 0.6f);

        private static Vector3 topFaceColor = new Vector3(1f, 0f, 0f);
        private static Vector3 lastTopFaceColor = new Vector3(1f, 0f, 0f);

        private static int selectedPresetIndex = 0;
        private static readonly string[] MaterialPresets =
        {
            "Custom / Default",
            "Brass",
            "Bronze",
            "Copper",
            "Gold",
            "Emerald",
            "Ruby"
        };

        private static uint program;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Lab3_2";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(900, 650);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            cube?.Dispose();
            Gl.DeleteProgram(program);
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();

            var inputContext = graphicWindow.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            graphicWindow.FramebufferResize += s =>
            {
                Gl.Viewport(s);
            };

            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);

            cube = ModelObjectDescriptor.CreateCube(Gl);

            for (int i = 0; i < 12; i++)
            {
                camera.IncreaseDistance();
            }

            Gl.ClearColor(System.Drawing.Color.White);
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
        }

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;

            using var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath);
            using var resStreamReader = new StreamReader(resStream!);
            return resStreamReader.ReadToEnd();
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    camera.DecreaseZYAngle();
                    break;
                case Key.Right:
                    camera.IncreaseZYAngle();
                    break;
                case Key.Down:
                    camera.IncreaseDistance();
                    break;
                case Key.Up:
                    camera.DecreaseDistance();
                    break;
                case Key.U:
                    camera.IncreaseZXAngle();
                    break;
                case Key.D:
                    camera.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled;
                    break;
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            cubeArrangementModel.AdvanceTime(deltaTime);
            imGuiController.Update((float)deltaTime);

            if (topFaceColor != lastTopFaceColor)
            {
                cube.UpdateTopFaceColor(topFaceColor);
                lastTopFaceColor = topFaceColor;
            }
        }

        private static void ApplyMaterialPreset(int index)
        {
            switch (index)
            {
                case 0:
                    ambientStrength = new Vector3(0.15f, 0.15f, 0.15f);
                    diffuseStrength = new Vector3(0.6f, 0.6f, 0.6f);
                    specularStrength = new Vector3(0.6f, 0.6f, 0.6f);
                    shininess = 50f;
                    break;
                case 1: // Brass
                    ambientStrength = new Vector3(0.329412f, 0.223529f, 0.027451f);
                    diffuseStrength = new Vector3(0.780392f, 0.568627f, 0.113725f);
                    specularStrength = new Vector3(0.992157f, 0.941176f, 0.807843f);
                    shininess = 27.8974f;
                    break;
                case 2: // Bronze
                    ambientStrength = new Vector3(0.2125f, 0.1275f, 0.054f);
                    diffuseStrength = new Vector3(0.714f, 0.4284f, 0.18144f);
                    specularStrength = new Vector3(0.393548f, 0.271906f, 0.166721f);
                    shininess = 25.6f;
                    break;
                case 3: // Copper
                    ambientStrength = new Vector3(0.19125f, 0.0735f, 0.0225f);
                    diffuseStrength = new Vector3(0.7038f, 0.27048f, 0.0828f);
                    specularStrength = new Vector3(0.256777f, 0.137622f, 0.086014f);
                    shininess = 12.8f;
                    break;
                case 4: // Gold
                    ambientStrength = new Vector3(0.24725f, 0.1995f, 0.0745f);
                    diffuseStrength = new Vector3(0.75164f, 0.60648f, 0.22648f);
                    specularStrength = new Vector3(0.628281f, 0.555802f, 0.366065f);
                    shininess = 51.2f;
                    break;
                case 5: // Emerald
                    ambientStrength = new Vector3(0.0215f, 0.1745f, 0.0215f);
                    diffuseStrength = new Vector3(0.07568f, 0.61424f, 0.07568f);
                    specularStrength = new Vector3(0.633f, 0.727811f, 0.633f);
                    shininess = 76.8f;
                    break;
                case 6: // Ruby
                    ambientStrength = new Vector3(0.1745f, 0.01175f, 0.01175f);
                    diffuseStrength = new Vector3(0.61424f, 0.04136f, 0.04136f);
                    specularStrength = new Vector3(0.727811f, 0.626959f, 0.626959f);
                    shininess = 76.8f;
                    break;
            }
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetUniform3(LightColorVariableName, lightColor);
            SetUniform3(LightPositionVariableName, new Vector3(0f, 1.2f, 0f));
            SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);

            SetUniform3(AmbientStrengthVariableName, ambientStrength);
            SetUniform3(DiffuseStrengthVariableName, diffuseStrength);
            SetUniform3(SpecularStrengthVariableName, specularStrength);

            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            float aspect = graphicWindow.Size.Y > 0
                ? (float)graphicWindow.Size.X / graphicWindow.Size.Y
                : 1024f / 768f;
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 2), aspect, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);

            // Központi kocka
            var modelMatrixCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            SetModelMatrix(modelMatrixCenterCube);
            DrawModelObject(cube);

            // Keringő / gyémánt kocka
            Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(0.25f);
            Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
            Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
            Matrix4X4<float> roty = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeLocalAngle);
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(1f, 1f, 0f);
            Matrix4X4<float> rotGlobalY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeGlobalYAngle);
            Matrix4X4<float> dimondCubeModelMatrix = diamondScale * rotx * rotz * roty * trans * rotGlobalY;
            SetModelMatrix(dimondCubeModelMatrix);
            DrawModelObject(cube);

            ImGuiNET.ImGui.Begin("Lab3_2 Lighting & Material", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);

            if (ImGuiNET.ImGui.Combo("Material Preset", ref selectedPresetIndex, MaterialPresets, MaterialPresets.Length))
            {
                ApplyMaterialPreset(selectedPresetIndex);
            }

            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5f, 128f);

            ImGuiNET.ImGui.SeparatorText("Light Color (3 sliders)");
            ImGuiNET.ImGui.SliderFloat("Light R", ref lightColor.X, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Light G", ref lightColor.Y, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Light B", ref lightColor.Z, 0f, 1f);

            ImGuiNET.ImGui.SeparatorText("Ambient Strength (3 sliders)");
            ImGuiNET.ImGui.SliderFloat("Ambient R", ref ambientStrength.X, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Ambient G", ref ambientStrength.Y, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Ambient B", ref ambientStrength.Z, 0f, 1f);

            ImGuiNET.ImGui.SeparatorText("Diffuse Strength (3 sliders)");
            ImGuiNET.ImGui.SliderFloat("Diffuse R", ref diffuseStrength.X, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Diffuse G", ref diffuseStrength.Y, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Diffuse B", ref diffuseStrength.Z, 0f, 1f);

            ImGuiNET.ImGui.SeparatorText("Specular Strength (3 sliders)");
            ImGuiNET.ImGui.SliderFloat("Specular R", ref specularStrength.X, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Specular G", ref specularStrength.Y, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Specular B", ref specularStrength.Z, 0f, 1f);

            ImGuiNET.ImGui.SeparatorText("Central Cube Face Color");
            ImGuiNET.ImGui.ColorEdit3("Top Face Color", ref topFaceColor);

            ImGuiNET.ImGui.End();

            imGuiController.Render();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
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