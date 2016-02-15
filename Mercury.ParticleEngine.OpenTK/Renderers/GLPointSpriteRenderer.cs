﻿using System;
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

        public bool FastFade { get; set; }

        /// <summary>
        /// per pixel size in unit of world
        /// </summary>
        public float PixelPerWorld { get; set; }

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

        public void Render(IEnumerable<ParticleEffect> effects, Matrix4 worldViewProjection, string textureKey, BlendMode blendMode)
        {
            ErrorCode e;
            GL.UseProgram(_progId);
            GL.Enable(EnableCap.PointSprite);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.VertexProgramPointSize);
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.Enable(EnableCap.Texture2D);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);
            GL.UniformMatrix4(GL.GetUniformLocation(_progId, "MVPMatrix"), false, ref worldViewProjection);
            GL.Uniform1(GL.GetUniformLocation(_progId, "FastFade"), FastFade ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(_progId, "tex"), 0);
            GL.Uniform1(GL.GetUniformLocation(_progId, "PixelPerWorld"), PixelPerWorld);
            GL.BindTexture(TextureTarget.Texture2D, _textureIndexLookup[textureKey]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            var vertexDataPointer = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);
            int totalActiveParticles = 0;
            foreach (var effect in effects)
            {
                for (var i = 0; i < effect.Emitters.Length; i++)
                {
                    RenderImpl(effect.Emitters[i], vertexDataPointer, totalActiveParticles);
                    totalActiveParticles += effect.Emitters[i].ActiveParticles;
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
            SetupBlend(blendMode);
            GL.DrawArrays(BeginMode.Points, 0, totalActiveParticles);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.UseProgram(0);
        }

        private void RenderImpl(Emitter emitter, IntPtr vertexDataPointer, int offsetPartlceCount)
        {
            switch (emitter.RenderingOrder)
            {
                case RenderingOrder.FrontToBack:
                    {
                        emitter.Buffer.CopyTo(IntPtr.Add(vertexDataPointer, Particle.SizeInBytes*offsetPartlceCount));
                        break;
                    }
                case RenderingOrder.BackToFront:
                    {
                        emitter.Buffer.CopyToReverse(IntPtr.Add(vertexDataPointer, Particle.SizeInBytes * offsetPartlceCount));
                        break;
                    }
            }
        }


        public void Render(IEnumerable<ParticleEffect> effects, Matrix4 worldViewProjection)
        {
            ErrorCode e;
            GL.UseProgram(_progId);
            GL.Enable(EnableCap.PointSprite);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.VertexProgramPointSize);
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.Enable(EnableCap.Texture2D);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);
            GL.UniformMatrix4(GL.GetUniformLocation(_progId, "MVPMatrix"), false, ref worldViewProjection);
            GL.Uniform1(GL.GetUniformLocation(_progId, "FastFade"), FastFade ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(_progId, "tex"), 0);
            GL.Uniform1(GL.GetUniformLocation(_progId, "PixelPerWorld"), PixelPerWorld);
            foreach (var effect in effects)
            {
                for (var i = 0; i < effect.Emitters.Length; i++)
                {
                    Render(effect.Emitters[i]);
                }
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.UseProgram(0);
        }

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
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferId);
            GL.UniformMatrix4(GL.GetUniformLocation(_progId, "MVPMatrix"), false, ref worldViewProjection);
            GL.Uniform1(GL.GetUniformLocation(_progId, "FastFade"), FastFade ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(_progId, "tex"), 0);
            GL.Uniform1(GL.GetUniformLocation(_progId, "PixelPerWorld"), PixelPerWorld);
            for (var i = 0; i < effect.Emitters.Length; i++)
            {
                Render(effect.Emitters[i]);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.UseProgram(0);
        }
        internal void Render(Emitter emitter)
        {
            ErrorCode e;
            GL.BindTexture(TextureTarget.Texture2D, _textureIndexLookup[emitter.TextureKey]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
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
            GL.DrawArrays(BeginMode.Points, 0, emitter.ActiveParticles);
        }

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                GL.DeleteBuffer(_vertexBufferId);
                GL.DeleteProgram(_progId);
            }
        }

        ~GLPointSpriteRenderer()
        {
            Dispose(false);
        }
    }
}