using System;

namespace MyDefense.Shop
{
    public enum ShopAdOutcome { Completed, Cancelled, Failed }
    /// <summary>An SDK only reports playback. Spring's verified challenge still decides whether a reroll is allowed.</summary>
    public interface IShopRewardedAdProvider { bool Available { get; } void Show(Action<ShopAdOutcome> completed); }
    public sealed class UnavailableShopAdProvider : IShopRewardedAdProvider
    {
        public bool Available => false;
        public void Show(Action<ShopAdOutcome> completed) => completed(ShopAdOutcome.Failed);
    }
    /// <summary>Only instantiate after local/development, loopback and server opt-in checks.</summary>
    public sealed class DevelopmentShopAdProvider : IShopRewardedAdProvider
    {
        private readonly Action<Action,Action,Action> show;
        public DevelopmentShopAdProvider(Action<Action,Action,Action> showDialog){show=showDialog;}
        public bool Available => true;
        public void Show(Action<ShopAdOutcome> completed)
            =>show(()=>completed(ShopAdOutcome.Completed),()=>completed(ShopAdOutcome.Cancelled),()=>completed(ShopAdOutcome.Failed));
    }
}
