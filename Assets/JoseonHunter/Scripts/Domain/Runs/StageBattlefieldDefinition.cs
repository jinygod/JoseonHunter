using System;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Domain.Runs
{
    public readonly struct StageBattlefieldDefinition
    {
        private StageBattlefieldDefinition(bool isBounded, float width, float height, string presentationId)
        {
            if (string.IsNullOrWhiteSpace(presentationId))
                throw new ArgumentException("Battlefield presentation ID is required.", nameof(presentationId));
            if (isBounded && (!IsFinitePositive(width) || !IsFinitePositive(height)))
                throw new ArgumentOutOfRangeException(nameof(width), "Bounded dimensions must be finite and positive.");
            IsBounded = isBounded;
            Width = width;
            Height = height;
            PresentationId = presentationId.Trim();
        }

        public bool IsBounded { get; }
        public float Width { get; }
        public float Height { get; }
        public string PresentationId { get; }

        public static StageBattlefieldDefinition Infinite(string presentationId) =>
            new StageBattlefieldDefinition(false, 0f, 0f, presentationId);

        public static StageBattlefieldDefinition Bounded(float width, float height, string presentationId) =>
            new StageBattlefieldDefinition(true, width, height, presentationId);

        public Float2 ClampPlayer(Float2 position, Float2 cameraHalfExtents)
        {
            if (!IsBounded) return position;
            if (!IsFiniteNonNegative(cameraHalfExtents.X) || !IsFiniteNonNegative(cameraHalfExtents.Y) ||
                cameraHalfExtents.X * 2f > Width || cameraHalfExtents.Y * 2f > Height)
                throw new ArgumentOutOfRangeException(nameof(cameraHalfExtents),
                    "Camera extents must fit inside the battlefield.");

            var maximumX = Width * .5f - cameraHalfExtents.X;
            var maximumY = Height * .5f - cameraHalfExtents.Y;
            return new Float2(
                Clamp(position.X, -maximumX, maximumX),
                Clamp(position.Y, -maximumY, maximumY));
        }

        private static float Clamp(float value, float minimum, float maximum) =>
            value < minimum ? minimum : value > maximum ? maximum : value;

        private static bool IsFinitePositive(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;

        private static bool IsFiniteNonNegative(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
