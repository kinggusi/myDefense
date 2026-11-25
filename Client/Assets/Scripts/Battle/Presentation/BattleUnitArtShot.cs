using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Immutable local cosmetics captured when a real projectile is observed.</summary>
    public readonly struct BattleUnitArtShot
    {
        public BattleUnitArtCatalog Catalog { get; }
        public BattleUnitVisual Visual { get; }
        public bool DarkBold { get; }
        public long AlienSpecId { get; }
        public Vector3 LaunchFoot { get; }
        public Vector3 SimulationOrigin { get; }
        public int Ordinal { get; }
        public uint Generation { get; }

        public BattleUnitArtShot(BattleUnitArtCatalog catalog, BattleUnitVisual visual, bool darkBold,
            long alienSpecId, Vector3 launchFoot, Vector3 simulationOrigin, int ordinal, uint generation)
        {
            Catalog = catalog; Visual = visual; DarkBold = darkBold;
            AlienSpecId = alienSpecId; LaunchFoot = launchFoot; SimulationOrigin = simulationOrigin; Ordinal = ordinal;
            Generation = generation;
        }

        public bool Matches(BattleUnitArtView source) => source != null && source.enabled && source.IsArtworkActive
            && source.Catalog == Catalog && source.Visual == Visual && source.DarkBold == DarkBold
            && source.BoundSpecId == AlienSpecId && source.GetComponent<UnitData>()?.specId == AlienSpecId
            && source.ShotGeneration == Generation;

        public Sprite CreateProjectileSprite() => Visual >= BattleUnitVisual.Puppeteer1
            ? Catalog?.Doll.Pose(BattleArtPose.Attack)?.Create(BattleArtFacing.Side, .455f, DarkBold)
            : Catalog?.Effects(Visual)?.Create(Visual == BattleUnitVisual.Mecharon ? 1 : (Ordinal & 1) == 1 ? 1 : 2);

        public Vector3 ResolveEmissionPoint(Vector3 localTarget)
            => BattleUnitArtView.ResolveEmissionPoint(Visual, LaunchFoot, localTarget);
    }
}
