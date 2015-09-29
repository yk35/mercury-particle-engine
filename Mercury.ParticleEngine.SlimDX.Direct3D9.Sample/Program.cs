using System.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mercury.ParticleEngine.Modifiers;
using Mercury.ParticleEngine.Profiles;
using Mercury.ParticleEngine.Renderers;
using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Direct2D;
using SlimDX.DirectInput;
using SlimDX.Windows;
using Device = SlimDX.Direct3D9.Device;
using DeviceType = SlimDX.Direct3D9.DeviceType;
using SlimDX.Multimedia;
using SlimDX.DirectInput;
using SlimDX.XInput;
using DeviceFlags = SlimDX.RawInput.DeviceFlags;

namespace Mercury.ParticleEngine
{

    static class Program
    {
        [STAThread]
        static void Main()
        {
            var worldSize = new Size(1024, 768);
            var renderSize = new Size(1024, 768);
            const bool windowed = true;

            var form = new RenderForm("Mercury Particle Engine - SharpDX.Direct3D9 Sample")
            {
                Size = new System.Drawing.Size(renderSize.Width, renderSize.Height)
            };
            var dl = new DirectInput();
            var keyboard = new Keyboard(dl);
            keyboard.SetCooperativeLevel(form.Handle, CooperativeLevel.Foreground | CooperativeLevel.Nonexclusive);
            keyboard.Properties.BufferSize = 4;
            var direct3d = new Direct3D();
            var device = new Device(
                direct3d,
                0,
                DeviceType.Hardware,
                form.Handle,
                CreateFlags.HardwareVertexProcessing,
                new PresentParameters()
                {
                    BackBufferWidth = renderSize.Width,
                    BackBufferHeight = renderSize.Height,
                    PresentationInterval = PresentInterval.One,
                    Windowed = windowed
                });

            var view = new Matrix();
            view.M11 = 1.0f;
            view.M12 = 0.0f;
            view.M13 = 0.0f;
            view.M14 = 0.0f;
            view.M21 = 0.0f;
            view.M22 = -1.0f;
            view.M23 = 0.0f;
            view.M24 = 0.0f;
            view.M31 = 0.0f;
            view.M32 = 0.0f;
            view.M33 = -1.0f;
            view.M34 = 0.0f;
            view.M41 = 0.0f;
            view.M42 = 0.0f;
            view.M43 = 0.0f;
            view.M44 = 1.0f;
            var proj = Matrix.OrthoOffCenterLH(worldSize.Width * -0.5f, worldSize.Width * 0.5f, worldSize.Height * 0.5f, worldSize.Height * -0.5f, 0f, 1f);
            var wvp = Matrix.Identity * view * proj;

            var smokeEffect = new ParticleEffect
            {
                Emitters = new[] {
                    new Emitter(2000, TimeSpan.FromSeconds(3), Profile.Point()) {
                        Parameters = new ReleaseParameters {
                            Colour   = new Colour(0f, 0f, 0.6f),
                            Opacity  = 1f,
                            Quantity = 5,
                            Speed    = new RangeF(0f, 100f),
                            Scale    = 32f,
                            Rotation = new RangeF((float)-Math.PI, (float)Math.PI),
                            Mass     = new RangeF(8f, 12f)
                        },
                        ReclaimFrequency = 5f,
                        BlendMode = BlendMode.Alpha,
                        RenderingOrder = RenderingOrder.BackToFront,
                        TextureKey = "Cloud",
                        Modifiers = new Modifier[] {
                            new DragModifier {
                                Frequency       = 10f,
                                DragCoefficient = 0.47f,
                                Density         = 0.125f
                            },
                            new ScaleInterpolator2 {
                                Frequency       = 60f,
                                InitialScale    = 32f,
                                FinalScale      = 256f
                            },
                            new RotationModifier {
                                Frequency       = 15f,
                                RotationRate    = 1f
                            },
                            new OpacityInterpolator2 {
                                Frequency       = 25f,
                                InitialOpacity  = 0.3f,
                                FinalOpacity    = 0.0f
                            }
                        },
                    }
                }
            };

            var sparkEffect = new ParticleEffect
            {
                Emitters = new[] {
                    new Emitter(2000, TimeSpan.FromSeconds(2), Profile.Point()) {
                        Parameters = new ReleaseParameters {
                            Colour   = new Colour(50f, 0.8f, 0.5f),
                            Opacity  = 1f,
                            Quantity = 10,
                            Speed    = new RangeF(0f, 100f),
                            Scale    = 64f,
                            Mass     = new RangeF(8f, 12f)
                        },
                        ReclaimFrequency = 5f,
                        BlendMode = BlendMode.Add,
                        RenderingOrder = RenderingOrder.FrontToBack,
                        TextureKey = "Particle",
                        Modifiers = new Modifier[] {
                            new LinearGravityModifier(Axis.Down, 30f) {
                                Frequency = 15f
                            },
                            new OpacityFastFadeModifier() {
                                Frequency = 10f
                            }
                        }
                    }
                }
            };

            var ringEffect = new ParticleEffect
            {
                Emitters = new[] {
                    new Emitter(2000, TimeSpan.FromSeconds(3), Profile.Spray(Axis.Up, 0.5f)) {
                        Parameters = new ReleaseParameters {
                            Colour   = new ColourRange(new Colour(210f, 0.5f, 0.6f), new Colour(230f, 0.7f, 0.8f)),
                            Opacity  = 1f,
                            Quantity = 1,
                            Speed    = new RangeF(300f, 700f),
                            Scale    = 64f,
                            Mass     = new RangeF(4f, 12f),
                        },
                        ReclaimFrequency = 5f,
                        BlendMode = BlendMode.Alpha,
                        RenderingOrder = RenderingOrder.FrontToBack,
                        TextureKey = "Ring",
                        Modifiers = new Modifier[] {
                            new LinearGravityModifier(Axis.Down, 100f) {
                                Frequency              = 20f
                            },
                            new OpacityFastFadeModifier() {
                                Frequency              = 10f,
                            },
                            new ContainerModifier {
                                Frequency              = 15f,
                                Width                  = worldSize.Width,
                                Height                 = worldSize.Height,
                                Position               = new Coordinate(worldSize.Width / 2f, worldSize.Height / 2f),
                                RestitutionCoefficient = 0.75f
                            }
                        }
                    }
                }
            };

            var loadTestEffect = new ParticleEffect
            {
                Emitters = new[] {
                    new Emitter(1000000, TimeSpan.FromSeconds(2), Profile.Point()) {
                        Parameters = new ReleaseParameters {
                            Quantity = 10000,
                            Speed    = new RangeF(0f, 200f),
                            Scale    = 1f,
                            Mass     = new RangeF(4f, 12f),
                            Opacity  = 0.4f
                        },
                        ReclaimFrequency = 5f,
                        BlendMode = BlendMode.Add,
                        TextureKey = "Pixel",
                        Modifiers = new Modifier[] {
                            new LinearGravityModifier(Axis.Down, 30f) {
                                Frequency = 15f
                            },
                            new OpacityFastFadeModifier() {
                                Frequency = 10f
                            },
                            new ContainerModifier {
                                Frequency              = 30f,
                                Width                  = worldSize.Width,
                                Height                 = worldSize.Height,
                                Position               = new Coordinate(worldSize.Width / 2f, worldSize.Height / 2f),
                                RestitutionCoefficient = 0.75f
                            },
                            new DragModifier {
                                Frequency       = 10f,
                                DragCoefficient = 0.47f,
                                Density         = 0.125f
                            },
                            new HueInterpolator2 {
                                Frequency = 10f,
                                InitialHue = 0f,
                                FinalHue = 150f
                            }
                        }
                    }
                }
            };

            var textureLookup = new Dictionary<String, Texture> {
                { "Particle", Texture.FromFile(device, "Particle.dds") },
                { "Pixel",    Texture.FromFile(device, "Pixel.dds")    },
                { "Cloud",    Texture.FromFile(device, "Cloud001.png") },
                { "Ring",     Texture.FromFile(device, "Ring001.png")  }
            };

            var renderer = new SlimDXPointSpriteRenderer(device, 1000000, textureLookup)
            {
                //EnableFastFade = true
            };

            var fontDescription = new FontDescription
            {
                Height = 14,
                FaceName = "Consolas",
                PitchAndFamily = PitchAndFamily.Mono,
                Quality = FontQuality.Draft
            };

            var sysFont = new System.Drawing.Font("Consolas", 14.0f, FontStyle.Regular);

            var font = new global::SlimDX.Direct3D9.Font(device, sysFont);

            var totalTime = 0f;
            var totalTimer = Stopwatch.StartNew();
            var updateTimer = new Stopwatch();
            var renderTimer = new Stopwatch();

            var currentEffect = smokeEffect;

            Vector3 mousePosition = Vector3.Zero;
            Vector3 previousMousePosition = Vector3.Zero;

            MessagePump.Run(form, () =>
            {
                // ReSharper disable AccessToDisposedClosure
                var frameTime = ((float)totalTimer.Elapsed.TotalSeconds) - totalTime;
                totalTime = (float)totalTimer.Elapsed.TotalSeconds;

                var clientMousePosition = form.PointToClient(RenderForm.MousePosition);
                previousMousePosition = mousePosition;
                mousePosition = Vector3.Unproject(new Vector3(clientMousePosition.X, clientMousePosition.Y, 0f), 0, 0, renderSize.Width, renderSize.Height, 0f, 1f, wvp);

                var mouseMovementLine = new LineSegment(new Coordinate(previousMousePosition.X, previousMousePosition.Y), new Coordinate(mousePosition.X, mousePosition.Y));
                if (keyboard.Acquire().IsSuccess)
                {
                    foreach (var state in keyboard.GetBufferedData())
                    {
                        foreach (var key in state.PressedKeys)
                        {
                            switch (key)
                            {
                                case Key.D1:
                                    currentEffect = smokeEffect;
                                    break;
                                case Key.D2:
                                    currentEffect = sparkEffect;
                                    break;
                                case Key.D3:
                                    currentEffect = ringEffect;
                                    break;
                                case Key.D4:
                                    currentEffect = loadTestEffect;
                                    break;
                                case Key.Escape:
                                    Environment.Exit(0);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                if (RenderForm.MouseButtons.HasFlag(System.Windows.Forms.MouseButtons.Left))
                {
                    currentEffect.Trigger(mouseMovementLine);
                }

                updateTimer.Restart();
                smokeEffect.Update(frameTime);
                sparkEffect.Update(frameTime);
                ringEffect.Update(frameTime);
                loadTestEffect.Update(frameTime);
                updateTimer.Stop();

                device.Clear(ClearFlags.Target, Color.Black, 1f, 0);
                device.BeginScene();

                renderTimer.Restart();
                renderer.Render(smokeEffect, wvp);
                renderer.Render(sparkEffect, wvp);
                renderer.Render(ringEffect, wvp);
                renderer.Render(loadTestEffect, wvp);
                renderTimer.Stop();

                var updateTime = (float)updateTimer.Elapsed.TotalSeconds;
                var renderTime = (float)renderTimer.Elapsed.TotalSeconds;

                font.DrawString(null, "1 - Smoke, 2 - Sparks, 3 - Rings, 4 - Load Test", 0, 0, Color.White);
                font.DrawString(null, String.Format("Time:        {0}", totalTimer.Elapsed), 0, 32, Color.White);
                font.DrawString(null, String.Format("Particles:   {0:n0}", currentEffect.ActiveParticles), 0, 48, Color.White);
                font.DrawString(null, String.Format("Update:      {0:n4} ({1,8:P2})", updateTime, updateTime / 0.01666666f), 0, 64, Color.White);
                font.DrawString(null, String.Format("Render:      {0:n4} ({1,8:P2})", renderTime, renderTime / 0.01666666f), 0, 80, Color.White);

                device.EndScene();
                device.Present();

                // ReSharper restore AccessToDisposedClosure
                
            });


            renderer.Dispose();
            form.Dispose();
            font.Dispose();
            device.Dispose();
            direct3d.Dispose();
        }
    }
}
