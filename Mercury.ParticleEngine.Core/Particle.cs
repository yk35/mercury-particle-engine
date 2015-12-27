namespace Mercury.ParticleEngine {
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Particle
    {
        public uint Id;
        public float Inception;
        public float Age;
        public fixed float Position[2];
        public fixed float Velocity[2];
        public fixed float Colour[3];
        public float Opacity;
        public float Scale;
        public float Rotation;
        // TODO: need to fix Modifier interface if you want to use RotationVelocity
        // public float RotationVelocity;
        public float Mass;

        public float LifeTime;

        static public readonly int SizeInBytes = Marshal.SizeOf(typeof(Particle));
    }
}