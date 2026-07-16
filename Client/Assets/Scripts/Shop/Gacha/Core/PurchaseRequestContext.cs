using System;

namespace GachaShop.Core
{
    public enum PurchaseRequestState
    {
        Idle,
        Requesting,
        RetryableFailure,
        Completed,
        FatalFailure
    }

    public sealed class PurchaseRequestContext
    {
        private readonly Func<string> requestIdFactory;

        public string ProductId { get; private set; }
        public string PurchaseRequestId { get; private set; }
        public PurchaseRequestState State { get; private set; } = PurchaseRequestState.Idle;

        public PurchaseRequestContext() : this(() => Guid.NewGuid().ToString())
        {
        }

        public PurchaseRequestContext(Func<string> requestIdFactory)
        {
            this.requestIdFactory = requestIdFactory ?? throw new ArgumentNullException(nameof(requestIdFactory));
        }

        public bool TryStartNew(string productId)
        {
            if (State == PurchaseRequestState.Requesting || State == PurchaseRequestState.RetryableFailure)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException("productId is required.", nameof(productId));
            }

            ProductId = productId;
            PurchaseRequestId = requestIdFactory();
            State = PurchaseRequestState.Requesting;
            return true;
        }

        public bool TryRetry()
        {
            if (State != PurchaseRequestState.RetryableFailure ||
                string.IsNullOrWhiteSpace(ProductId) ||
                string.IsNullOrWhiteSpace(PurchaseRequestId))
            {
                return false;
            }

            State = PurchaseRequestState.Requesting;
            return true;
        }

        public void MarkCompleted()
        {
            State = PurchaseRequestState.Completed;
        }

        public void MarkRetryableFailure()
        {
            State = PurchaseRequestState.RetryableFailure;
        }

        public void MarkFatalFailure()
        {
            State = PurchaseRequestState.FatalFailure;
        }
    }
}
