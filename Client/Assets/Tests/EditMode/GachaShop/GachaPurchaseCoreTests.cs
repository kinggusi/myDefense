using NUnit.Framework;

namespace GachaShop.Core.Tests
{
    public class GachaPurchaseCoreTests
    {
        [Test]
        public void Single_UsesPurchaseEndpointAndProductId()
        {
            string uri = GachaPurchasePresenter.BuildPurchaseUri("sh1", GachaPurchasePresenter.SingleProductId, "uuid-1");
            StringAssert.StartsWith("/shop/gacha/purchase?", uri);
            StringAssert.Contains("productId=ALIEN_GACHA_SINGLE", uri);
        }

        [Test]
        public void Ten_UsesPurchaseEndpointAndProductId()
        {
            string uri = GachaPurchasePresenter.BuildPurchaseUri("sh1", GachaPurchasePresenter.TenProductId, "uuid-1");
            StringAssert.Contains("productId=ALIEN_GACHA_TEN", uri);
        }

        [Test]
        public void PurchaseUri_DoesNotUseLegacyCountParameter()
        {
            string uri = GachaPurchasePresenter.BuildPurchaseUri("sh1", GachaPurchasePresenter.TenProductId, "uuid-1");
            Assert.That(uri.Contains("count="), Is.False);
        }

        [Test]
        public void NewPurchase_CreatesUuid()
        {
            var context = new PurchaseRequestContext(() => "uuid-1");
            Assert.That(context.TryStartNew(GachaPurchasePresenter.SingleProductId), Is.True);
            Assert.That(context.PurchaseRequestId, Is.EqualTo("uuid-1"));
        }

        [Test]
        public void Retry_ReusesSameUuid()
        {
            var context = new PurchaseRequestContext(() => "uuid-1");
            context.TryStartNew(GachaPurchasePresenter.SingleProductId);
            context.MarkRetryableFailure();
            Assert.That(context.TryRetry(), Is.True);
            Assert.That(context.PurchaseRequestId, Is.EqualTo("uuid-1"));
        }

        [Test]
        public void NewPurchaseAfterCompletion_CreatesNewUuid()
        {
            string[] ids = { "uuid-1", "uuid-2" };
            int index = 0;
            var context = new PurchaseRequestContext(() => ids[index++]);
            context.TryStartNew(GachaPurchasePresenter.SingleProductId);
            context.MarkCompleted();
            context.TryStartNew(GachaPurchasePresenter.TenProductId);
            Assert.That(context.PurchaseRequestId, Is.EqualTo("uuid-2"));
        }

        [Test]
        public void Requesting_BlocksDuplicatePurchase()
        {
            var context = new PurchaseRequestContext(() => "uuid-1");
            context.TryStartNew(GachaPurchasePresenter.SingleProductId);
            Assert.That(context.TryStartNew(GachaPurchasePresenter.TenProductId), Is.False);
            Assert.That(context.ProductId, Is.EqualTo(GachaPurchasePresenter.SingleProductId));
        }

        [TestCase(400)]
        [TestCase(404)]
        [TestCase(422)]
        public void Http4xx_IsFatal(long statusCode)
        {
            Assert.That(GachaPurchasePresenter.ClassifyFailure(statusCode, null, false, false),
                Is.EqualTo(GachaPurchaseFailureKind.Fatal));
        }

        [Test]
        public void InsufficientDiamond_IsFatal()
        {
            Assert.That(GachaPurchasePresenter.ClassifyFailure(409, "INSUFFICIENT_DIAMOND", false, false),
                Is.EqualTo(GachaPurchaseFailureKind.Fatal));
        }

        [Test]
        public void PurchaseRequestConflict_IsFatal()
        {
            Assert.That(GachaPurchasePresenter.ClassifyFailure(409, "PURCHASE_REQUEST_CONFLICT", false, false),
                Is.EqualTo(GachaPurchaseFailureKind.Fatal));
        }

        [Test]
        public void PurchaseAlreadyProcessing_IsRetryable()
        {
            Assert.That(GachaPurchasePresenter.ClassifyFailure(409, "PURCHASE_ALREADY_PROCESSING", false, false),
                Is.EqualTo(GachaPurchaseFailureKind.Retryable));
        }

        [Test]
        public void Http5xx_IsRetryable()
        {
            Assert.That(GachaPurchasePresenter.ClassifyFailure(503, null, false, false),
                Is.EqualTo(GachaPurchaseFailureKind.Retryable));
        }

        [Test]
        public void JsonParseFailure_IsRetryable()
        {
            Assert.That(GachaPurchasePresenter.ClassifyFailure(200, null, true, true),
                Is.EqualTo(GachaPurchaseFailureKind.Retryable));
        }

        [Test]
        public void ViewModel_UsesRemainingDiamond()
        {
            GachaPurchaseViewModel viewModel = GachaPurchasePresenter.CreateViewModel(Response(321));
            Assert.That(viewModel.RemainingDiamond, Is.EqualTo(321));
            StringAssert.Contains("321", viewModel.Summary);
        }

        [Test]
        public void ViewModel_PreservesDrawOrder()
        {
            GachaPurchaseResponse response = Response(0);
            response.draws = new[]
            {
                new GachaDraw { order = 1, alienId = 22, grade = "NORMAL" },
                new GachaDraw { order = 2, alienId = 29, grade = "MYTHIC" }
            };
            string details = GachaPurchasePresenter.CreateViewModel(response).Details;
            Assert.That(details.IndexOf("#1", System.StringComparison.Ordinal),
                Is.LessThan(details.IndexOf("#2", System.StringComparison.Ordinal)));
        }

        [Test]
        public void ViewModel_UsesRewardCurrentLevelAndPieces()
        {
            GachaPurchaseResponse response = Response(0);
            response.rewards = new[]
            {
                new GachaReward { alienId = 22, grade = "NORMAL", occurrenceCount = 2, piecesAdded = 100, currentLevel = 3, currentPieces = 121 }
            };
            string details = GachaPurchasePresenter.CreateViewModel(response).Details;
            StringAssert.Contains("Lv.3", details);
            StringAssert.Contains("현재 조각 121", details);
        }

        [Test]
        public void ViewModel_MarksNewUnlock()
        {
            GachaPurchaseResponse response = Response(0);
            response.rewards = new[]
            {
                new GachaReward { alienId = 22, grade = "NORMAL", occurrenceCount = 1, newlyUnlocked = true }
            };
            StringAssert.Contains("신규", GachaPurchasePresenter.CreateViewModel(response).Details);
        }

        [Test]
        public void ViewModel_PreservesMythicGrade()
        {
            GachaPurchaseResponse response = Response(0);
            response.draws = new[] { new GachaDraw { order = 1, alienId = 29, grade = "MYTHIC" } };
            StringAssert.Contains("MYTHIC", GachaPurchasePresenter.CreateViewModel(response).Details);
        }

        [Test]
        public void RevealState_SingleHasOneCard()
        {
            var state = new GachaRevealState();
            state.Begin(1);
            state.RevealNext();
            Assert.That(state.RevealedCards, Is.EqualTo(1));
            Assert.That(state.IsRevealing, Is.False);
        }

        [Test]
        public void RevealState_TenDrawsAreOrderedByServerOrder()
        {
            GachaDraw[] ordered = GachaRevealState.OrderDraws(new[]
            {
                new GachaDraw { order = 3, alienId = 3 },
                new GachaDraw { order = 1, alienId = 1 },
                new GachaDraw { order = 2, alienId = 2 }
            });

            Assert.That(ordered[0].order, Is.EqualTo(1));
            Assert.That(ordered[1].order, Is.EqualTo(2));
            Assert.That(ordered[2].order, Is.EqualTo(3));
        }

        [Test]
        public void RevealState_SkipRevealsEveryCard()
        {
            var state = new GachaRevealState();
            state.Begin(10);
            state.Skip();
            Assert.That(state.RevealedCards, Is.EqualTo(10));
            Assert.That(state.IsRevealing, Is.False);
        }

        [Test]
        public void RevealState_BlocksPurchaseUntilResultCloses()
        {
            var state = new GachaRevealState();
            state.Begin(10);
            Assert.That(state.CanStartPurchase, Is.False);
            state.Skip();
            Assert.That(state.CanStartPurchase, Is.False);
            state.Close();
            Assert.That(state.CanStartPurchase, Is.True);
        }

        private static GachaPurchaseResponse Response(int remainingDiamond)
        {
            return new GachaPurchaseResponse
            {
                productId = GachaPurchasePresenter.SingleProductId,
                currencyType = "DIAMOND",
                price = 500,
                remainingDiamond = remainingDiamond,
                drawCount = 1,
                draws = new GachaDraw[0],
                rewards = new GachaReward[0]
            };
        }
    }
}
