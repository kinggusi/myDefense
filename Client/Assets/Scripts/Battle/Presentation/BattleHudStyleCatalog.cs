using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    public sealed class BattleHudStyleCatalog:ScriptableObject
    {
        public const string ResourcePath="Battle/Presentation/BattleHudStyleCatalog";
        [SerializeField] private Font _korean,_numbers;
        [SerializeField] private Sprite _kidnap;
        public Font Korean=>_korean;
        public Font Numbers=>_numbers;
        public Sprite Kidnap=>_kidnap;
        public bool Available=>_korean!=null&&_numbers!=null&&_kidnap!=null;
#if UNITY_EDITOR
        public void Configure(Font korean,Font numbers,Sprite kidnap){_korean=korean;_numbers=numbers;_kidnap=kidnap;}
#endif
    }
}
