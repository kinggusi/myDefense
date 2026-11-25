using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    [CreateAssetMenu(menuName = "MyDefense/Battle/Unit Art Catalog")]
    public sealed class BattleUnitArtCatalog : ScriptableObject
    {
        public const string ResourcePath = "Battle/Presentation/BattleUnitArtCatalog";
        [SerializeField] private BattleUnitPoseArt _diad;
        [SerializeField] private BattleUnitPoseArt _mecharon;
        [SerializeField] private BattleUnitPoseArt _puppeteer;
        [SerializeField] private BattleUnitPoseArt _doll;
        [SerializeField] private BattleEffectArt _diadEffects;
        [SerializeField] private BattleEffectArt _mecharonEffects;
        [SerializeField] private BattleEffectArt _puppeteerEffects;
        [SerializeField] private Texture2D _boardTexture;
        [SerializeField] private Shader _reliefShader;
        [SerializeField] private Shader _boardShader;
        public Texture2D BoardTexture => _boardTexture;
        public Shader ReliefShader => _reliefShader;
        public Shader BoardShader => _boardShader;
        public BattleUnitPoseArt Doll => _doll;
        public BattleUnitPoseArt Unit(BattleUnitVisual kind) => kind == BattleUnitVisual.Diad ? _diad
            : kind == BattleUnitVisual.Mecharon ? _mecharon : _puppeteer;
        public BattleEffectArt Effects(BattleUnitVisual kind) => kind == BattleUnitVisual.Diad ? _diadEffects
            : kind == BattleUnitVisual.Mecharon ? _mecharonEffects : _puppeteerEffects;
        public bool Available(BattleUnitVisual kind, bool darkBold) => _reliefShader != null
            && Unit(kind)?.Available(darkBold) == true && Effects(kind)?.Available == true
            && (kind < BattleUnitVisual.Puppeteer1 || _doll?.Available(darkBold) == true);
#if UNITY_EDITOR
        public void Configure(BattleUnitPoseArt diad, BattleUnitPoseArt mecharon, BattleUnitPoseArt puppeteer,
            BattleUnitPoseArt doll, BattleEffectArt diadEffects, BattleEffectArt mecharonEffects,
            BattleEffectArt puppeteerEffects, Texture2D boardTexture, Shader reliefShader, Shader boardShader)
        {
            _diad = diad; _mecharon = mecharon; _puppeteer = puppeteer; _doll = doll;
            _diadEffects = diadEffects; _mecharonEffects = mecharonEffects;
            _puppeteerEffects = puppeteerEffects; _boardTexture = boardTexture; _reliefShader = reliefShader;
            _boardShader = boardShader;
        }
#endif
    }
}
