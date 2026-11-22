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
        private static IWindow graphicWindow;
        private static GL Gl;
        private static ImGuiController imGuiController;

        private static ModelObjectDescriptor barrelPlankFlat;
        private static ModelObjectDescriptor barrelPlankCurved;

        private static CameraDescriptor camera = new CameraDescriptor();
        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private const int PlankCount = 18;
        private const float PlankWidth = 1.0f;
        private const float PlankHeight = 2.0f;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";

        private static float shininess = 50f;
        private static uint program;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Lab3_1";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(900, 600);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            barrelPlankFlat?.Dispose();
            barrelPlankCurved?.Dispose();
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

            barrelPlankFlat = ModelObjectDescriptor.CreateBarrelPlank(Gl, flatNormals: true, PlankWidth, PlankHeight, PlankCount);
            barrelPlankCurved = ModelObjectDescriptor.CreateBarrelPlank(Gl, flatNormals: false, PlankWidth, PlankHeight, PlankCount);

            for (int i = 0; i < 25; i++)
            {
                camera.IncreaseDistance();
            }

            Gl.ClearColor(System.Drawing.Color.White);
            Gl.Disable(EnableCap.CullFace);
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
        }

        // idealis Z eltolas: (szelesseg / 2) / tan(10 fok)
        private static float ComputeIdealZOffset(float scale = 1.0f)
        {
            float halfWidth = (PlankWidth * scale) * 0.5f;
            float tenDegRad = MathF.PI / 180.0f * (180.0f / PlankCount); // 10 fok
            return halfWidth / MathF.Tan(tenDegRad);
        }

        private static void DrawBarrel(ModelObjectDescriptor plank, Vector3D<float> barrelCenter, float scale = 1.0f)
        {
            float zOffset = ComputeIdealZOffset(scale);
            float stepRad = MathF.PI / 180.0f * (360.0f / PlankCount); // 20 fok

            Matrix4X4<float> scaleMx = Matrix4X4.CreateScale(scale);
            Matrix4X4<float> zShiftMx = Matrix4X4.CreateTranslation(0f, 0f, zOffset);
            Matrix4X4<float> centerShiftMx = Matrix4X4.CreateTranslation(barrelCenter);

            for (int i = 0; i < PlankCount; i++)
            {
                float rotYRad = i * stepRad;
                Matrix4X4<float> rotYMx = Matrix4X4.CreateRotationY(rotYRad);

                Matrix4X4<float> modelMatrix = scaleMx * zShiftMx * rotYMx * centerShiftMx;
                SetModelMatrix(modelMatrix);
                DrawModelObject(plank);
            }
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            Vector3 camPos = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z);

            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            // fenyforras
            SetUniform3(LightPositionVariableName, camPos);
            SetUniform3(ViewPositionVariableName, camPos);
            SetUniform1(ShinenessVariableName, shininess);

            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            float aspect = graphicWindow.Size.Y > 0
                ? (float)graphicWindow.Size.X / graphicWindow.Size.Y
                : 1024f / 768f;
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 2), aspect, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);

            DrawBarrel(barrelPlankFlat, new Vector3D<float>(-3.6f, 0f, 0f), scale: 1.0f);
            DrawBarrel(barrelPlankCurved, new Vector3D<float>(3.6f, 0f, 0f), scale: 1.0f);

            ImGuiNET.ImGui.Begin("Lighting", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);
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