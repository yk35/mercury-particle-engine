using System;
using System.Collections.Generic;
using System.Reflection;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Mercury.ParticleEngine.Renderers
{
    using System.Runtime.InteropServices;

    using Mercury.ParticleEngine.OpenTK;

    /// <summary>
    /// implement by OpenGL Point Sprite
    /// </summary>
    public class GLPointSpriteRenderer : IDisposable
    {
        private readonly int _size;

        #region OpenTK Resources
        private readonly IReadOnlyDictionary<String, Int32> _textureIndexLookup;
        private readonly int _vertexBufferId;
        private readonly int _progId;
        #endregion

        #region attribute location
        private int _posLoc;

        private int _ageLoc;

        private int _scaleLoc;

        private int _rotationLoc;

        private int _colLoc;

        private int _opacityLoc;
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether fast fade.
        /// </summary>
        public bool FastFade { get; set; }

        /// <summary>
        /// per pixel size in unit of world
        /// </summary>
        public float PixelPerWorld { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GLPointSpriteRenderer"/> class.
        /// </summary>
        /// <param name="textureIndexLookup">
        /// The texture index lookup.
        /// </param>
        /// <param name="size">
        /// The size.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public GLPointSpriteRenderer(IReadOnlyDictionary<String, Int32> textureIndexLookup, int size)
        {
            if (textureIndexLookup == null)
                throw new ArgumentNullException("textureIndexLookup");

            _size = size;
            _textureIndexLookup = textureIndexLookup;
            FastFade = false;
            PixelPerWorld = 1.0f;

            _progId = LoadShaderProgram(ResourcesGL.PointSpriteVertShader, ResourcesGL.PointSpriteFragShader);

            _vertexBufferId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(_size * Particle.SizeInBytes), IntPtr.Zero, BufferUsageHint.StreamDraw);

            _posLoc = GL.GetAttribLocation(_progId, "Position");
            _colLoc = GL.GetAttribLocation(_progId, "Colour");
            _rotationLoc = GL.GetAttribLocation(_progId, "Rotation");
            _ageLoc = GL.GetAttribLocation(_progId, "Age");
            _scaleLoc = GL.GetAttribLocation(_progId, "Scale");
            _opacityLoc = GL.GetAttribLocation(_progId, "Opacity");
        }

        /// <summary>
        /// The render.
        /// </summary>
        /// <param name="effect">
        /// The effect.
        /// </param>
        /// <param name="worldViewProjection">
        /// The world view projection.
        /// </param>
        public void Render(ParticleEffect effect, Matrix4 worldViewProjection)
        {
            ErrorCode e;
            GL.UseProgram(_progId);
            GL.Enable(EnableCap.PointSprite);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.VertexProgramPointSize);
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.Enable(EnableCap.Texture2D);
            for (var i = 0; i < effect.Emitters.Length; i++)
            {
                Render(effect.Emitters[i], worldViewProjection);
            }
            GL.UseProgram(0);
        }

        /// <summary>
        /// The render.
        /// </summary>
        /// <param name="emitter">
        /// The emitter.
        /// </param>
        /// <param name="worldViewProjection">
        /// The world view projection.
        /// </param>
        internal void Render(Emitter emitter, Matrix4 worldViewProjection)
        {
            ErrorCode e;
            GL.UniformMatrix4(GL.GetUniformLocation(_progId, "MVPMatrix"), false, ref worldViewProjection);
            GL.Uniform1(GL.GetUniformLocation(_progId, "FastFade"), FastFade ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(_progId, "tex"), 0);
            GL.Uniform1(GL.GetUniformLocation(_progId, "PixelPerWorld"), PixelPerWorld);
            GL.BindTexture(TextureTarget.Texture2D, _textureIndexLookup[emitter.TextureKey]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);
            var vertexDataPointer = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

            switch (emitter.RenderingOrder)
            {
                case RenderingOrder.FrontToBack:
                    {
                        emitter.Buffer.CopyTo(vertexDataPointer);
                        break;
                    }
                case RenderingOrder.BackToFront:
                    {
                        emitter.Buffer.CopyToReverse(vertexDataPointer);
                        break;
                    }
            }
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);

            GL.EnableVertexAttribArray(_ageLoc);
            GL.VertexAttribPointer(_ageLoc, 1, VertexAttribPointerType.Float, false, Particle.SizeInBytes, Marshal.OffsetOf(typeof(Particle), "Age"));
            GL.EnableVertexAttribArray(_posLoc);
            GL.VertexAttribPointer(_posLoc, 2, VertexAttribPointerType.Float, false, Particle.SizeInBytes, Marshal.OffsetOf(typeof(Particle), "Position"));
            GL.EnableVertexAttribArray(_colLoc);
            GL.VertexAttribPointer(_colLoc, 3, VertexAttribPointerType.Float, false, Particle.SizeInBytes, Marshal.OffsetOf(typeof(Particle), "Colour"));
            GL.EnableVertexAttribArray(_scaleLoc);
            GL.VertexAttribPointer(_scaleLoc, 1, VertexAttribPointerType.Float, false, Particle.SizeInBytes, Marshal.OffsetOf(typeof(Particle), "Scale"));
            GL.EnableVertexAttribArray(_rotationLoc);
            GL.VertexAttribPointer(_rotationLoc, 1, VertexAttribPointerType.Float, false, Particle.SizeInBytes, Marshal.OffsetOf(typeof(Particle), "Rotation"));
            GL.EnableVertexAttribArray(_opacityLoc);
            GL.VertexAttribPointer(_opacityLoc, 1, VertexAttribPointerType.Float, false, Particle.SizeInBytes, Marshal.OffsetOf(typeof(Particle), "Opacity"));
            SetupBlend(emitter.BlendMode);
            GL.DrawArrays(PrimitiveType.Points, 0, emitter.ActiveParticles);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// The load shader program.
        /// </summary>
        /// <param name="vert">
        /// The vert.
        /// </param>
        /// <param name="frag">
        /// The frag.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        static int LoadShaderProgram(byte[] vert, byte[] frag)
        {
            var vertId = GL.CreateShader(ShaderType.VertexShader);
            var fragId = GL.CreateShader(ShaderType.FragmentShader);
            var vertString = System.Text.Encoding.UTF8.GetString(vert);
            var fragString = System.Text.Encoding.UTF8.GetString(frag);
            GL.ShaderSource(vertId, vertString);
            GL.CompileShader(vertId);
            int statusVert;
            GL.GetShader(vertId, ShaderParameter.CompileStatus, out statusVert);
            GL.ShaderSource(fragId, fragString);
            int statusFrag;
            GL.CompileShader(fragId);
            GL.GetShader(vertId, ShaderParameter.CompileStatus, out statusFrag);
            var prog = GL.CreateProgram();
            GL.AttachShader(prog, vertId);
            GL.AttachShader(prog, fragId);
            GL.DeleteShader(vertId);
            GL.DeleteShader(fragId);
            GL.LinkProgram(prog);
            if (statusVert == 1 && statusFrag == 1)
            {
                return prog;
            }
            else
            {
                var info = GL.GetProgramInfoLog(prog);
                GL.DeleteProgram(prog);
                throw new Exception(info);
            }
        }

        /// <summary>
        /// The setup blend.
        /// </summary>
        /// <param name="blendMode">
        /// The blend mode.
        /// </param>
        static void SetupBlend(BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Add:
                    {
                        GL.BlendEquation(BlendEquationMode.FuncAdd);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
                        break;
                    }
                case BlendMode.Subtract:
                    {
                        GL.BlendEquation(BlendEquationMode.FuncSubtract);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
                        break;
                    }
                case BlendMode.Alpha:
                default:
                    {
                        GL.BlendEquation(BlendEquationMode.FuncAdd);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                        break;
                    }
            }
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="disposing">
        /// The disposing.
        /// </param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                GL.DeleteBuffer(_vertexBufferId);
                GL.DeleteProgram(_progId);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="GLPointSpriteRenderer"/> class. 
        /// </summary>
        ~GLPointSpriteRenderer()
        {
            Dispose(false);
        }
    }
}