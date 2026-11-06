using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PaymentModule.Data.Services
{
    public class StoredPaymentMethodManager : IStoredPaymentMethodManager
    {
        private const int MaxCardsPerCustomer = 100;

        private readonly IStoredPaymentMethodService _storedPaymentMethodService;
        private readonly IStoredPaymentMethodSearchService _storedPaymentMethodSearchService;
        private readonly IPaymentMethodsSearchService _paymentMethodsSearchService;
        private readonly IEnumerable<IStoredPaymentMethodUsageProvider> _usageProviders;

        public StoredPaymentMethodManager(
            IStoredPaymentMethodService storedPaymentMethodService,
            IStoredPaymentMethodSearchService storedPaymentMethodSearchService,
            IPaymentMethodsSearchService paymentMethodsSearchService,
            IEnumerable<IStoredPaymentMethodUsageProvider> usageProviders)
        {
            _storedPaymentMethodService = storedPaymentMethodService;
            _storedPaymentMethodSearchService = storedPaymentMethodSearchService;
            _paymentMethodsSearchService = paymentMethodsSearchService;
            _usageProviders = usageProviders;
        }

        protected virtual DateTime UtcNow => DateTime.UtcNow;

        public virtual async Task<StoredPaymentMethodOperationResult> PrepareAsync<TRequest>(TRequest request, StoredPaymentContext context)
            where TRequest : PaymentRequestBase, IStoredCredentialPaymentRequest
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(context);

            var parameters = request.Parameters ?? new NameValueCollection();

            if (IsTrue(parameters[StoredCredentialParameterNames.StoreForFutureUse]) && !string.IsNullOrEmpty(context.CustomerId))
            {
                request.StoreForFutureUse = true;
            }

            var consentId = parameters[StoredCredentialParameterNames.ConsentId];
            if (!string.IsNullOrEmpty(consentId))
            {
                request.ConsentId = consentId;
            }

            request.StoredCredentialUsage = GetUsage(parameters[StoredCredentialParameterNames.StoredCredentialUsage], request.ConsentId, request.StoredCredentialUsage);

            var storedPaymentMethodId = parameters[StoredCredentialParameterNames.StoredPaymentMethodId];
            if (string.IsNullOrEmpty(storedPaymentMethodId))
            {
                return StoredPaymentMethodOperationResult.Success();
            }

            var card = await _storedPaymentMethodService.GetByIdAsync(storedPaymentMethodId);
            if (!CanPay(card, context))
            {
                return StoredPaymentMethodOperationResult.Fail("This saved card can't be used. Choose another card.");
            }

            request.StoredPaymentMethod = card;
            request.StoreForFutureUse = false;

            return StoredPaymentMethodOperationResult.Success(card);
        }

        public virtual async Task<StoredPaymentMethod> SaveFromPaymentAsync(IStoredCredentialPaymentRequest request, IStoredCredentialPaymentResult result, StoredPaymentContext context)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(context);

            if (!result.IsSuccess)
            {
                return null;
            }

            if (result.StoredPaymentMethod == null)
            {
                if (request.StoredPaymentMethod == null)
                {
                    return null;
                }

                await MarkUsedAsync(request.StoredPaymentMethod);
                return request.StoredPaymentMethod;
            }

            var saved = result.StoredPaymentMethod;
            var cards = await GetCustomerCardsAsync(context.CustomerId, context.StoreId);
            // The card belongs to the payment method the customer paid with. A method that hands the payment on to another
            // (a vault in front of a gateway) gets the card back under the gateway's code, which it couldn't charge later.
            var paymentMethodCode = context.PaymentMethodCode ?? saved.PaymentMethodCode;

            var card = FindSameCard(cards, paymentMethodCode, saved);
            if (card != null)
            {
                card.Status = StoredPaymentMethodStatus.Active;
                card.LastUsedDate = UtcNow;
                card.ConsentId = request.ConsentId ?? saved.ConsentId ?? card.ConsentId;
                card.NetworkTransactionId ??= saved.NetworkTransactionId;
            }
            else
            {
                card = saved;
                card.StoreId = context.StoreId;
                card.CustomerId = context.CustomerId;
                card.OrganizationId ??= context.OrganizationId;
                card.PaymentMethodCode = paymentMethodCode;
                card.InitialOrderId ??= context.OrderId;
                card.InitialPaymentId ??= context.PaymentId;
                card.ConsentId ??= request.ConsentId;
                card.Usage = request.StoredCredentialUsage;
                card.Status = StoredPaymentMethodStatus.Active;
                card.LastUsedDate = UtcNow;
                card.IsDefault = !cards.Any(IsActive);
            }

            await _storedPaymentMethodService.SaveChangesAsync([card]);
            return card;
        }

        public virtual async Task MarkUsedAsync(StoredPaymentMethod storedPaymentMethod, string networkTransactionId = null)
        {
            ArgumentNullException.ThrowIfNull(storedPaymentMethod);

            storedPaymentMethod.LastUsedDate = UtcNow;
            if (string.IsNullOrEmpty(storedPaymentMethod.NetworkTransactionId) && !string.IsNullOrEmpty(networkTransactionId))
            {
                storedPaymentMethod.NetworkTransactionId = networkTransactionId;
            }

            await _storedPaymentMethodService.SaveChangesAsync([storedPaymentMethod]);
        }

        public virtual async Task<StoredPaymentMethodOperationResult> AddAsync(AddStoredPaymentMethodRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(request.CustomerId))
            {
                return StoredPaymentMethodOperationResult.Fail("Sign in to save a card.");
            }

            var paymentMethod = await GetPaymentMethodAsync(request.StoreId, request.PaymentMethodCode, onlyActive: true);
            if (paymentMethod is not ISupportStoredCredentialFlow)
            {
                return StoredPaymentMethodOperationResult.Fail("This payment method can't save cards.");
            }

            var storeResult = await paymentMethod.StorePaymentMethodAsync(new StorePaymentMethodRequest
            {
                StoreId = request.StoreId,
                CustomerId = request.CustomerId,
                OrganizationId = request.OrganizationId,
                Usage = request.Usage,
                Verify = request.Verify,
                ConsentId = request.ConsentId,
                Parameters = request.Parameters ?? new NameValueCollection(),
            });

            if (storeResult == null || !storeResult.IsSuccess || storeResult.StoredPaymentMethod == null)
            {
                return new StoredPaymentMethodOperationResult
                {
                    ErrorMessage = storeResult?.ErrorMessage ?? "The card could not be saved.",
                    Outcome = storeResult?.Outcome,
                    RawCode = storeResult?.RawCode,
                };
            }

            var cards = await GetCustomerCardsAsync(request.CustomerId, request.StoreId);
            var saved = storeResult.StoredPaymentMethod;
            var card = FindSameCard(cards, paymentMethod.Code, saved);
            if (card != null)
            {
                card.Status = StoredPaymentMethodStatus.Active;
                card.ConsentId = request.ConsentId ?? card.ConsentId;
                card.ExpirationMonth = saved.ExpirationMonth ?? card.ExpirationMonth;
                card.ExpirationYear = saved.ExpirationYear ?? card.ExpirationYear;
            }
            else
            {
                card = saved;
                card.StoreId = request.StoreId;
                card.CustomerId = request.CustomerId;
                card.OrganizationId ??= request.OrganizationId;
                card.PaymentMethodCode = paymentMethod.Code;
                card.ConsentId ??= request.ConsentId;
                card.Status = StoredPaymentMethodStatus.Active;
            }

            var toSave = new List<StoredPaymentMethod> { card };
            if (request.SetDefault || !cards.Any(x => IsActive(x) && x.Id != card.Id))
            {
                toSave.AddRange(MakeDefault(card, cards));
            }

            await _storedPaymentMethodService.SaveChangesAsync(toSave);
            return StoredPaymentMethodOperationResult.Success(card);
        }

        public virtual async Task<StoredPaymentMethodOperationResult> RemoveAsync(string customerId, string storedPaymentMethodId)
        {
            var card = await GetOwnedCardAsync(customerId, storedPaymentMethodId);
            if (card == null)
            {
                return StoredPaymentMethodOperationResult.Fail("The card was not found.");
            }

            if (card.Status == StoredPaymentMethodStatus.Revoked)
            {
                return StoredPaymentMethodOperationResult.Success(card);
            }

            foreach (var provider in _usageProviders)
            {
                var usage = await provider.GetUsageAsync(card);
                if (usage?.IsInUse == true)
                {
                    return StoredPaymentMethodOperationResult.Fail(usage.Description ?? "This card is in use and can't be removed.");
                }
            }

            var paymentMethod = await GetPaymentMethodAsync(card.StoreId, card.PaymentMethodCode, onlyActive: false);
            if (paymentMethod is ISupportStoredCredentialFlow)
            {
                var deleteResult = await paymentMethod.DeleteStoredPaymentMethodAsync(new DeleteStoredPaymentMethodRequest
                {
                    StoreId = card.StoreId,
                    StoredPaymentMethod = card,
                });

                if (deleteResult?.IsSuccess == false)
                {
                    return StoredPaymentMethodOperationResult.Fail(deleteResult.ErrorMessage ?? "The card could not be removed. Please try again.");
                }
            }

            var toSave = new List<StoredPaymentMethod> { card };
            if (card.IsDefault)
            {
                var cards = await GetCustomerCardsAsync(card.CustomerId, card.StoreId);
                var next = cards
                    .Where(x => x.Id != card.Id && IsActive(x))
                    .OrderByDescending(x => x.LastUsedDate ?? x.CreatedDate)
                    .FirstOrDefault();

                if (next != null)
                {
                    next.IsDefault = true;
                    toSave.Add(next);
                }
            }

            card.Status = StoredPaymentMethodStatus.Revoked;
            card.IsDefault = false;
            await _storedPaymentMethodService.SaveChangesAsync(toSave);

            return StoredPaymentMethodOperationResult.Success(card);
        }

        public virtual async Task<StoredPaymentMethodOperationResult> SetDefaultAsync(string customerId, string storedPaymentMethodId)
        {
            var card = await GetOwnedCardAsync(customerId, storedPaymentMethodId);
            if (card == null)
            {
                return StoredPaymentMethodOperationResult.Fail("The card was not found.");
            }

            if (!IsActive(card))
            {
                return StoredPaymentMethodOperationResult.Fail("Only an active card can be the default.");
            }

            var cards = await GetCustomerCardsAsync(card.CustomerId, card.StoreId);
            var toSave = new List<StoredPaymentMethod> { card };
            toSave.AddRange(MakeDefault(card, cards));
            await _storedPaymentMethodService.SaveChangesAsync(toSave);

            return StoredPaymentMethodOperationResult.Success(card);
        }

        /// <summary>A card may pay when it is the customer's, active, not expired, and belongs to the payment method of the payment.</summary>
        protected virtual bool CanPay(StoredPaymentMethod card, StoredPaymentContext context)
        {
            return card != null
                && !string.IsNullOrEmpty(context.CustomerId)
                && card.CustomerId == context.CustomerId
                && IsActive(card)
                && (string.IsNullOrEmpty(context.PaymentMethodCode) || card.PaymentMethodCode == context.PaymentMethodCode);
        }

        protected virtual async Task<IList<StoredPaymentMethod>> GetCustomerCardsAsync(string customerId, string storeId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return [];
            }

            var criteria = AbstractTypeFactory<StoredPaymentMethodSearchCriteria>.TryCreateInstance();
            criteria.CustomerId = customerId;
            criteria.StoreId = storeId;
            criteria.Take = MaxCardsPerCustomer;

            return (await _storedPaymentMethodSearchService.SearchAsync(criteria)).Results;
        }

        protected virtual async Task<PaymentMethod> GetPaymentMethodAsync(string storeId, string paymentMethodCode, bool onlyActive)
        {
            if (string.IsNullOrEmpty(paymentMethodCode))
            {
                return null;
            }

            var criteria = AbstractTypeFactory<PaymentMethodsSearchCriteria>.TryCreateInstance();
            criteria.StoreId = storeId;
            criteria.Codes = [paymentMethodCode];
            criteria.IsActive = onlyActive ? true : null;
            criteria.Take = 1;

            return (await _paymentMethodsSearchService.SearchAsync(criteria)).Results.FirstOrDefault();
        }

        private async Task<StoredPaymentMethod> GetOwnedCardAsync(string customerId, string storedPaymentMethodId)
        {
            if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(storedPaymentMethodId))
            {
                return null;
            }

            var card = await _storedPaymentMethodService.GetByIdAsync(storedPaymentMethodId);
            return card?.CustomerId == customerId ? card : null;
        }

        private bool IsActive(StoredPaymentMethod card)
        {
            return card.Status == StoredPaymentMethodStatus.Active && !card.IsExpired(UtcNow);
        }

        /// <summary>The same card saved again: same payment method, provider customer reference and token.</summary>
        private static StoredPaymentMethod FindSameCard(IList<StoredPaymentMethod> cards, string paymentMethodCode, StoredPaymentMethod saved)
        {
            if (string.IsNullOrEmpty(saved.Token))
            {
                return null;
            }

            return cards.FirstOrDefault(x =>
                x.PaymentMethodCode == paymentMethodCode
                && x.Token == saved.Token
                && x.ProviderCustomerRef == saved.ProviderCustomerRef);
        }

        /// <summary>Makes the card the default and returns the other cards that stopped being the default.</summary>
        private static List<StoredPaymentMethod> MakeDefault(StoredPaymentMethod card, IList<StoredPaymentMethod> cards)
        {
            card.IsDefault = true;

            var others = cards.Where(x => x.IsDefault && x.Id != card.Id && x != card).ToList();
            foreach (var other in others)
            {
                other.IsDefault = false;
            }

            return others;
        }

        private static StoredCredentialUsage GetUsage(string value, string consentId, StoredCredentialUsage current)
        {
            if (Enum.TryParse<StoredCredentialUsage>(value, ignoreCase: true, out var usage))
            {
                return usage;
            }

            // A card saved with a consent record (e.g. subscription terms) pays recurring orders.
            return string.IsNullOrEmpty(consentId) ? current : StoredCredentialUsage.Recurring;
        }

        private static bool IsTrue(string value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
