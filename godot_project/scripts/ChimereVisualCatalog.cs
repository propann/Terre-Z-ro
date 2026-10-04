using Godot;

namespace TerreZero.Chimeres
{
    public enum ChimereBodyShape
    {
        Beast,
        Wisp,
        Stag,
        Raptor,
        Serpent,
        Furnace,
        Fungal,
        Prism,
        Golem
    }

    public readonly struct ChimereVisualProfile
    {
        public ChimereBodyShape Shape { get; }
        public Vector3 BodyScale { get; }
        public float CoreScale { get; }
        public int AccentCount { get; }

        public ChimereVisualProfile(
            ChimereBodyShape shape,
            Vector3 bodyScale,
            float coreScale,
            int accentCount)
        {
            Shape = shape;
            BodyScale = bodyScale;
            CoreScale = coreScale;
            AccentCount = accentCount;
        }
    }

    public static class ChimereVisualCatalog
    {
        public static ChimereVisualProfile Get(string speciesId) =>
            speciesId switch
            {
                "mordrail" => new(ChimereBodyShape.Beast, new Vector3(1.35f, 0.8f, 1.55f), 1.05f, 3),
                "nebuli" => new(ChimereBodyShape.Wisp, new Vector3(0.85f, 1.35f, 0.85f), 1.35f, 4),
                "cerf_ecorce" => new(ChimereBodyShape.Stag, new Vector3(1.0f, 1.45f, 0.9f), 1.0f, 4),
                "voltac" => new(ChimereBodyShape.Raptor, new Vector3(0.8f, 1.0f, 1.45f), 0.9f, 5),
                "hydrune" => new(ChimereBodyShape.Serpent, new Vector3(0.82f, 1.55f, 0.82f), 1.15f, 3),
                "cendrex" => new(ChimereBodyShape.Furnace, new Vector3(1.05f, 1.15f, 1.05f), 1.3f, 4),
                "mycoryx" => new(ChimereBodyShape.Fungal, new Vector3(1.25f, 0.95f, 1.25f), 1.1f, 6),
                "prismole" => new(ChimereBodyShape.Prism, new Vector3(0.9f, 1.3f, 0.9f), 1.45f, 5),
                "ferrale" => new(ChimereBodyShape.Golem, new Vector3(1.45f, 1.45f, 1.2f), 0.85f, 4),
                _ => new(ChimereBodyShape.Beast, Vector3.One, 1.0f, 2)
            };
    }
}
