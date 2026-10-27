# Payment Module

[![CI status](https://github.com/VirtoCommerce/vc-module-payment/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-payment/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-payment&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-payment) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-payment&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-payment) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-payment&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-payment) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-payment&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-payment)

## Overview

The Payment module provides management and integration of various payment methods within the Virto Commerce platform. It defines an extensible abstraction for payment processing — including payment registration, post-processing verification, void, capture, and refund operations — and exposes a REST API and admin UI for configuring which payment methods are active per store. The module ships with a built-in `DefaultManualPaymentMethod` and allows other modules to register custom payment method implementations at runtime through the `IPaymentMethodsRegistrar` interface. It also defines the stored credential (card on file) contract: a payment method can save a customer's card at the provider and charge it later without the customer present, which subscriptions and other merchant-initiated payments build on.

## Key Features

* **Extensible payment method registration** — Register custom payment methods via code using `IPaymentMethodsRegistrar.RegisterPaymentMethod<T>()`; payment methods are resolved through `AbstractTypeFactory` polymorphism
* **Full payment lifecycle** — Abstract `PaymentMethod` base class supports Process, PostProcess, Void, Capture, and Refund operations with both synchronous (obsolete) and async APIs
* **Per-store configuration** — Each payment method is persisted per store with settings for activation, priority, logo, deferred payment, partial payment availability, and localized display names
* **Search and filtering** — Search payment methods by store, code, keyword, and active status; includes both persisted and transient (registered but not yet configured) methods in results
* **Stored credentials (card on file)** — Payment methods that implement `ISupportStoredCredentialFlow` save a card at the provider, during a customer-present payment (`ProcessPaymentRequest.StoreForFutureUse`) or on its own (`StorePaymentMethodAsync`), charge it later without the customer present (`ProcessStoredPaymentAsync`) and delete it (`DeleteStoredPaymentMethodAsync`). Callers check `paymentMethod is ISupportStoredCredentialFlow` before using the flow
* **Provider-independent charge results** — Every stored-credential call returns a `ChargeOutcome` (`Succeeded`, `RequiresAction`, `SoftDecline`, `HardDecline`, `ProviderError`) with the raw provider code and message, the card network advice code, the earliest retry time, the 3-D Secure recovery URL and whether the provider retries on its own, so callers apply one retry and recovery policy to every connector
* **Merchant-initiated transaction data** — Charges carry the usage (`CardOnFile`, `Recurring`, `Unscheduled`), the sequence (`First`, `Subsequent`), the reason (`Recurring`, `Unscheduled`, `Reauthorization`, `Resubmission`, `DelayedCharge`), the capture mode (`AuthorizeOnly`, `AuthorizeAndCapture`), an idempotency key and the network transaction ID of the first charge
* **Saved cards** — `StoredPaymentMethod` per store, customer and organization: provider token and customer reference, brand, last 4 digits, expiry (`IsExpired`), fingerprint, network transaction ID, the order and payment it was saved with, status (`Active`, `NeedsUpdate`, `Expired`, `Revoked`) and a default card flag. No card number is stored
* **Consent records** — `PaymentConsent` keeps the evidence of what the customer agreed to (terms version, text and hash, channel, IP address, user agent, accepted date) and the object it relates to; after it is saved, only the link to the saved card and the revocation date can change
* **Saved card and consent services** — CRUD and search services with change events for both; the main search filters are indexed
* **Built-in manual payment method** — `DefaultManualPaymentMethod` provides an out-of-the-box payment method supporting capture and refund flows for manual/offline payment scenarios
* **Localized display names** — Payment methods support localized names per language through the `PaymentMethodLocalizedName` entity
* **Domain events** — Publishes `PaymentMethodChangeEvent` (before save), `PaymentMethodChangedEvent` (after save), and `PaymentMethodInstancingEvent` (before instantiation) for extensibility, plus `StoredPaymentMethodChangingEvent` / `StoredPaymentMethodChangedEvent` and `PaymentConsentChangingEvent` / `PaymentConsentChangedEvent` for saved cards and consents
* **Export/Import** — Full platform export/import support for payment method configurations with batched processing
* **Multi-database support** — Entity Framework Core migrations for SQL Server, PostgreSQL, and MySQL
* **Polymorphic JSON serialization** — Custom `PaymentMethodsJsonConverter` enables correct deserialization of payment method subtypes via the `TypeName` discriminator

## Configuration

### Application Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `VirtoCommerce.Payment.DefaultManualPaymentMethod.ExampleSetting` | ShortText | *(empty)* | Example setting for the built-in DefaultManualPaymentMethod; serves as a template for custom payment method settings |

### Permissions

This module does not define its own permissions. All API endpoints require authentication (`[Authorize]` attribute). Access control is inherited from the platform's general authorization policies.

## Architecture

The module follows the layered architecture of Virto Commerce modules:

```
┌─────────────────────────────────────────────────────────────┐
│  Web Layer (API Controller, Module initialization, Admin UI)│
├─────────────────────────────────────────────────────────────┤
│  Data Layer (Services, Repository, EF Core, Caching,        │
│  Export/Import, DefaultManualPaymentMethod)                 │
├──────────┬──────────────┬──────────────┬────────────────────┤
│ SqlServer│    MySql     │  PostgreSql  │  DB Providers      │
├──────────┴──────────────┴──────────────┴────────────────────┤
│  Core Layer (PaymentMethod, requests and results, stored    │
│  credential contract, service interfaces, events)           │
└─────────────────────────────────────────────────────────────┘
```

### Key Flow

1. **Registration** — During module initialization (`PostInitialize`), payment methods are registered with `IPaymentMethodsRegistrar.RegisterPaymentMethod<T>()`, which adds them to `AbstractTypeFactory<PaymentMethod>`
2. **Configuration** — Operators configure payment methods per store via the admin UI or REST API; configurations are persisted as `StorePaymentMethodEntity` records with localized names
3. **Search / Discovery** — `PaymentMethodsSearchService` queries persisted methods from the database, then appends transient (registered but unconfigured) methods, filtering by store, code, keyword, and active status
4. **Instantiation** — When a payment method is loaded from the database, `PaymentMethodInstancingEvent` is published so that dependent modules (e.g., NativePaymentMethods) can register their types before `AbstractTypeFactory` resolves the concrete type
5. **Settings hydration** — After entity-to-model conversion, `ISettingsManager.DeepLoadSettingsAsync` loads method-specific settings into the `PaymentMethod.Settings` collection
6. **Payment processing** — The storefront or order module calls the async methods on the resolved `PaymentMethod` instance: `ProcessPaymentAsync` → `PostProcessPaymentAsync` → `CaptureProcessPaymentAsync` / `VoidProcessPaymentAsync` / `RefundProcessPaymentAsync`
7. **Persistence** — Updates flow through `PaymentMethodsService.SaveChangesAsync`, which persists entity data and deep-saves settings, publishing change events before and after

### Stored Credential Flow

1. **Check support** — The caller resolves the payment method and checks `paymentMethod is ISupportStoredCredentialFlow`. The base `PaymentMethod` implementations of the three stored-credential methods throw `NotImplementedException`, so a payment method without the marker is never asked to save or charge a card
2. **Consent** — The storefront records the customer's agreement to save the card and be charged later as a `PaymentConsent` with `IPaymentConsentService` (through the consuming module, for example Product Subscriptions)
3. **Save the card** — The payment method saves the card at the provider and returns it as an unsaved `StoredPaymentMethod`; `IStoredPaymentMethodManager` records it. Payment methods never read or write saved cards themselves:
   * **During checkout**, the storefront posts `storeForFutureUse`, `consentId` (and, to pay with a saved card, `storedPaymentMethodId`) as payment parameters. `IStoredPaymentMethodManager.PrepareAsync` turns them into the typed `StoreForFutureUse`, `StoredCredentialUsage`, `ConsentId` and `StoredPaymentMethod` of the request (`IStoredCredentialPaymentRequest`), checking that a saved card is the customer's, active and of this payment method. When the payment is authorized, the payment method returns the card it saved in the result (`IStoredCredentialPaymentResult.StoredPaymentMethod`) and `SaveFromPaymentAsync` records it with the order and payment, or marks the saved card that paid as used
   * **On its own** (a card added from the customer's account, or a free trial), `IStoredPaymentMethodManager.AddAsync` calls `StorePaymentMethodAsync`, optionally verified with a $0 authorization or 3-D Secure, and records the returned card
4. **Charge later** — A merchant-initiated charge calls `ProcessStoredPaymentAsync` with the stored card, amount, currency, an idempotency key, the usage, the sequence, the reason and the capture mode
5. **Interpret the result** — The caller acts on `ChargeOutcome`: retry a `SoftDecline` after `RetryAfter`, stop on a `HardDecline`, send the customer to `RecoveryUrl` on `RequiresAction`, retry a `ProviderError` with the same idempotency key. When `IsProviderRetryScheduled` is set, the provider retries on its own and the caller must not
6. **Keep the card current** — The caller updates the card's `Status` (`NeedsUpdate`, `Expired`, `Revoked`), `LastUsedDate` and `UpdatedByProviderDate`; `IsExpired(utcNow)` tells whether the card's expiry month has passed
7. **Remove** — `IStoredPaymentMethodManager.RemoveAsync` asks every `IStoredPaymentMethodUsageProvider` whether the card is in use (for example by a subscription), then calls `DeleteStoredPaymentMethodAsync` to delete it at the provider and revokes it; the default passes to the customer's most recent active card

## Components

### Projects

| Project | Layer | Purpose |
|---------|-------|---------|
| `VirtoCommerce.PaymentModule.Core` | Core/Domain | Domain models (`PaymentMethod`, request/result types, enums), the stored credential contract (`ISupportStoredCredentialFlow`, `StoredPaymentMethod`, `PaymentConsent`, `ChargeOutcome`), service interfaces (`IPaymentMethodsRegistrar`, `IPaymentMethodsService`, `IPaymentMethodsSearchService`, `IStoredPaymentMethodService`, `IPaymentConsentService`), domain events, and module constants/settings |
| `VirtoCommerce.PaymentModule.Data` | Data/Infrastructure | Service implementations (`PaymentMethodsService`, `PaymentMethodsSearchService`, stored payment method and consent services), EF Core entities and repository (`PaymentRepository`, `PaymentDbContext`), caching (`PaymentCacheRegion`), export/import, and `DefaultManualPaymentMethod` |
| `VirtoCommerce.PaymentModule.Web` | Web/Presentation | ASP.NET Core module entry point (`Module`), REST API controller (`PaymentModuleController`), custom JSON converter, and admin UI assets |
| `VirtoCommerce.PaymentModule.Data.SqlServer` | Database Provider | EF Core migrations and configuration for SQL Server |
| `VirtoCommerce.PaymentModule.Data.PostgreSql` | Database Provider | EF Core migrations and configuration for PostgreSQL |
| `VirtoCommerce.PaymentModule.Data.MySql` | Database Provider | EF Core migrations and configuration for MySQL |
| `VirtoCommerce.Payment.Tests` | Tests | Unit and integration tests for payment method functionality |

### Key Services

| Service | Interface | Responsibility |
|---------|-----------|---------------|
| `PaymentMethodsService` | `IPaymentMethodsService`, `IPaymentMethodsRegistrar` | CRUD operations for payment methods with settings deep-load/save; registers payment method types into `AbstractTypeFactory`; publishes instancing events during entity-to-model conversion |
| `PaymentMethodsSearchService` | `IPaymentMethodsSearchService` | Searches persisted payment methods with filtering (store, code, keyword, active status) and appends transient registered-but-unconfigured methods to results |
| `PaymentRepository` | `IPaymentRepository` | EF Core data access for `StorePaymentMethodEntity` with eager loading of localized names |
| `StoredPaymentMethodService` / `StoredPaymentMethodSearchService` | `IStoredPaymentMethodService`, `IStoredPaymentMethodSearchService` | CRUD and search for saved cards by store, customer, organization, payment method, statuses, initial order and initial payment (default sort: the default card first, then the newest); publish `StoredPaymentMethodChangingEvent` / `StoredPaymentMethodChangedEvent` |
| `StoredPaymentMethodManager` | `IStoredPaymentMethodManager` | The rules for saved cards: resolves the saved card a payment uses (owner, status, payment method), records the card a payment method saved (same card updates the existing record, the first card becomes the default), adds a card without a payment, removes a card unless an `IStoredPaymentMethodUsageProvider` reports it in use, and keeps one default card per customer and store |
| `PaymentConsentService` / `PaymentConsentSearchService` | `IPaymentConsentService`, `IPaymentConsentSearchService` | CRUD and search for consent records by store, customer, saved card, purpose and related object (default sort: the newest first); publish `PaymentConsentChangingEvent` / `PaymentConsentChangedEvent` |
| `PaymentExportImport` | — | Batched export/import of payment method configurations via platform's export/import infrastructure |
| `DefaultManualPaymentMethod` | — | Built-in manual payment method implementing `ISupportCaptureFlow` and `ISupportRefundFlow`; auto-approves Process, Capture, Void, and Refund operations |

### REST API

Base route: `api/payment`

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `api/payment` | Get all registered payment method types (returns prototype instances of each registered type) |
| `POST` | `api/payment/search` | Search payment methods by criteria (store, keyword, codes, active status) with paging and sorting |
| `GET` | `api/payment/{id}` | Get a specific payment method configuration by its ID |
| `PUT` | `api/payment` | Create or update a payment method configuration (activation, priority, settings, localized names, etc.) |

Saved cards and consents have no REST endpoints in this module. The storefront manages saved cards through x-cart, and x-order's `authorizePayment` saves or uses them during checkout; both go through `IStoredPaymentMethodManager`.

## Developer Guide: Payment Flow End-to-End

This section explains how payments work across the entire Virto Commerce stack — from the Vue.js storefront, through the XAPI GraphQL layer, down to the `PaymentMethod` abstraction — and provides a step-by-step guide for developing a custom payment method.

### End-to-End Payment Architecture

```
┌──────────────┐     GraphQL      ┌──────────────┐     Domain       ┌──────────────────┐
│   Frontend   │ ──────────────►  │  XAPI Layer  │ ─────────────►   │  Payment Method  │
│  (Vue.js)    │                  │ x-cart/x-order│                  │  (your module)   │
└──────────────┘                  └──────────────┘                  └──────────────────┘
      │                                 │                                    │
      │ 1. addOrUpdateCartPayment       │                                    │
      │ 2. createOrderFromCart          │                                    │
      │ 3. initializePayment      ────►│ ProcessPaymentAsync()         ────►│ Gateway API
      │ 4. [redirect / form / done]     │                                    │
      │ 5. authorizePayment       ────►│ ValidatePostProcessRequestAsync()  │
      │                            ────►│ PostProcessPaymentAsync()     ────►│ Gateway API
      │                                 │                                    │
      │         Back-office only:       │                                    │
      │                            ────►│ CaptureProcessPaymentAsync() ────►│ Gateway API
      │                            ────►│ VoidProcessPaymentAsync()    ────►│ Gateway API
      │                            ────►│ RefundProcessPaymentAsync()  ────►│ Gateway API
```

### Component Responsibilities

| Layer | Module | Responsibility |
|-------|--------|---------------|
| **Frontend** | [vc-frontend](https://github.com/VirtoCommerce/vc-frontend) | Renders payment method selection, handles redirect/form/standard flows, passes callback parameters back to backend |
| **XAPI (Cart)** | [vc-module-x-cart](https://github.com/VirtoCommerce/vc-module-x-cart) | `addOrUpdateCartPayment` mutation — attaches payment method to cart, validates gateway code against active store methods |
| **XAPI (Order)** | [vc-module-x-order](https://github.com/VirtoCommerce/vc-module-x-order) | `createOrderFromCart`, `initializePayment`, `authorizePayment` mutations — orchestrates order creation and calls `PaymentMethod` lifecycle methods |
| **Payment Module** | vc-module-payment (this module) | Abstract `PaymentMethod` base class, registration infrastructure, per-store configuration persistence |
| **Payment Provider** | e.g. [vc-module-cyber-source](https://github.com/VirtoCommerce/vc-module-cyber-source) | Concrete `PaymentMethod` implementation that communicates with the external payment gateway |

### Checkout Flow Step-by-Step

#### Step 1: Payment Method Selection (Frontend → x-cart)

The frontend queries `availablePaymentMethods` from the cart and displays them as radio buttons. When the user selects one, the frontend calls:

```graphql
mutation AddOrUpdateCartPayment($command: InputAddOrUpdateCartPaymentType!) {
  addOrUpdateCartPayment(command: $command) {
    id
    payments { id paymentGatewayCode }
  }
}
```

**x-cart internals:** The `AddOrUpdateCartPaymentCommandHandler` loads the cart aggregate, maps the input to a `Payment` object, validates the `paymentGatewayCode` against active store payment methods via `CartPaymentValidator`, and persists. If the code doesn't match any active method, the error `PAYMENT_METHOD_UNAVAILABLE` is returned.

#### Step 2: Order Creation (Frontend → x-order)

The frontend calls `createOrderFromCart`. This validates the entire cart (including the `"payments"` rule set that re-checks payment method availability), creates the order with payments transferred from the cart, then clears the cart.

```graphql
mutation CreateOrderFromCart($command: InputCreateOrderFromCartType!) {
  createOrderFromCart(command: $command) {
    id
    number
    inPayments { id gatewayCode paymentMethod { code paymentMethodType paymentMethodGroupType } }
  }
}
```

**Important:** Payments are copied to the order but **not processed yet**. The payment status remains `New`.

#### Step 2b: (Alternative) Initialize Payment Before Order Creation (Frontend → x-cart → PaymentMethod)

For payment gateways that must open a session or widget **before** the order is persisted — e.g. Datatrans Lightbox, Klarna widgets, or any flow that needs a gateway-side transaction ID while the user is still on the cart — the frontend calls `initializeCartPayment` on the cart:

```graphql
mutation InitializeCartPayment($command: InputInitializeCartPaymentType!) {
  initializeCartPayment(command: $command) {
    isSuccess
    errorMessage
    paymentActionType
    actionRedirectUrl
    actionHtmlForm
    publicParameters { key value }
    paymentMethodCode
  }
}
```

**x-cart internals:** `InitializeCartPaymentCommandHandler` loads the cart aggregate, resolves the `PaymentMethod` via `ICartAvailMethodsService`, and calls **`PaymentMethod.ProcessPaymentAsync(request)`** with the cart context. The resulting tokens/redirect URL are returned to the frontend to open the widget.

**Important differences vs. order-based `initializePayment`:**

- `request.Store` and `request.Payment` are set, but `request.Order` is **null** — no order exists yet. Providers that need a reference number should read `request.Parameters["CartId"]` (and `"Amount"`, `"Currency"`, which the handler populates).
- Only payment methods with `AllowCartPayment = true` are eligible; otherwise the handler throws.
- This step does NOT save the cart.

The frontend then calls `createOrderFromCart` and `authorizePayment` as usual (steps 2 and 5 below). The gateway's transaction ID obtained here is typically reused on the order side.

#### Step 3: Initialize Payment (Frontend → x-order → PaymentMethod)

The frontend calls `initializePayment` with the `orderId` and `paymentId`:

```graphql
mutation InitializePayment($command: InputInitializePaymentType!) {
  initializePayment(command: $command) {
    isSuccess
    errorMessage
    paymentActionType        # "Standard" | "Redirection" | "PreparedForm" | "Unknown"
    actionRedirectUrl        # URL to redirect to (for Redirection type)
    actionHtmlForm           # HTML form to auto-submit (for PreparedForm type)
    publicParameters { key value }  # Client-side data (tokens, scripts, etc.)
    paymentMethodCode
  }
}
```

**x-order internals:** The `InitializePaymentCommandHandler` validates the payment (status must not be `Paid`, payment method must be active), then calls **`PaymentMethod.ProcessPaymentAsync(request)`**. The result determines what the frontend does next:

- `ProcessPaymentRequestResult.RedirectUrl` → frontend redirects to external gateway
- `ProcessPaymentRequestResult.HtmlForm` → frontend injects and auto-submits form
- `ProcessPaymentRequestResult.PublicParameters` → frontend uses tokens/scripts for client-side SDK
- None of the above → payment is done (manual/standard flow)

**This step does NOT save the order.** It is a read-only initialization.

#### Step 4: Payment Flow Branching (Frontend)

Based on `paymentActionType` returned from Step 3:

```
┌─────────────────┐  ┌───────────────┐  ┌──────────┐  ┌──────────┐
│   Redirection   │  │ PreparedForm  │  │ Standard │  │  Manual  │
└────────┬────────┘  └──────┬────────┘  └────┬─────┘  └────┬─────┘
         │                  │                │              │
  window.location     Inject HTML       Go directly    Go directly
  .href = redirectUrl form + auto-submit to confirmation to confirmation
         │                  │                │              │
         ▼                  ▼                │              │
  ┌────────────────────────────┐             │              │
  │   External Payment Gateway │             │              │
  │   (PayPal, bank, 3DS...)   │             │              │
  │   User completes payment   │             │              │
  └────────────┬───────────────┘             │              │
               │ redirect back               │              │
               ▼                             │              │
  ┌─────────────────────────┐                │              │
  │  /payment-result page   │                │              │
  │  Calls authorizePayment │                │              │
  └────────────┬────────────┘                │              │
               │                             │              │
               ▼                             ▼              ▼
         ┌───────────────────────────────────────────────────┐
         │              Order Confirmation Page               │
         └───────────────────────────────────────────────────┘
```

#### Step 5: Authorize Payment — Post-Processing (Frontend → x-order → PaymentMethod)

After the user returns from an external gateway, the frontend's callback page collects all URL query parameters and calls:

```graphql
mutation AuthorizePayment($command: InputAuthorizePaymentType!) {
  authorizePayment(command: $command) {
    isSuccess
    errorMessage
  }
}
```

**x-order internals:** The `AuthorizePaymentCommandHandler`:
1. Calls **`PaymentMethod.ValidatePostProcessRequestAsync(parameters)`** — verifies the callback parameters (signatures, tokens, etc.)
2. If valid, calls **`PaymentMethod.PostProcessPaymentAsync(request)`** — finalizes the payment with the gateway
3. If successful, updates `payment.Status` to the new status (e.g., `Authorized`, `Paid`) and **saves the order**

#### Step 6: Capture / Void / Refund (Back-Office Only)

These operations are **not exposed via XAPI GraphQL**. They are triggered from the back-office admin UI or through platform REST APIs:

| Operation | Method Called | When Used |
|-----------|-------------|-----------|
| **Capture** | `PaymentMethod.CaptureProcessPaymentAsync()` | Dual-message mode: capture authorized funds (full or partial) |
| **Void** | `PaymentMethod.VoidProcessPaymentAsync()` | Cancel an authorization before capture |
| **Refund** | `PaymentMethod.RefundProcessPaymentAsync()` | Return funds after capture |

Implement `ISupportCaptureFlow` and/or `ISupportRefundFlow` marker interfaces to indicate your payment method supports these operations.

### PaymentMethod Lifecycle Methods

Every payment method extends the abstract `PaymentMethod` class and overrides these async methods:

| Method | Purpose | Called By | When |
|--------|---------|-----------|------|
| `ProcessPaymentAsync` | Initialize payment with gateway. Return redirect URL, HTML form, public parameters, or just success. | `initializePayment` (order) or `initializeCartPayment` (cart) mutation | After order is created, before user pays — or on the cart before order creation for widget/lightbox flows |
| `ValidatePostProcessRequestAsync` | Validate callback parameters from the gateway (signatures, tokens). | `authorizePayment` mutation | When user returns from external gateway |
| `PostProcessPaymentAsync` | Finalize payment: confirm authorization, verify payment, update status. | `authorizePayment` mutation | After callback validation succeeds |
| `CaptureProcessPaymentAsync` | Capture authorized funds (full or partial). | Back-office | After authorization, when ready to settle |
| `VoidProcessPaymentAsync` | Cancel an uncaptured authorization. | Back-office | To release authorized funds |
| `RefundProcessPaymentAsync` | Refund captured funds. | Back-office | After capture, to return money |

### PaymentMethodType and PaymentMethodGroupType

Your payment method must declare two enums that tell the platform and frontend how to handle the flow:

**`PaymentMethodType`** — Determines the technical integration pattern:

| Value | Description | ProcessPaymentAsync Returns |
|-------|-------------|-----------------------------|
| `Standard` | Card data entered on site (tokenized). No redirect needed. | `PublicParameters` (tokens, scripts for client-side SDK) |
| `Redirection` | User redirected to external site to pay. | `RedirectUrl` |
| `PreparedForm` | Gateway provides an HTML form to auto-submit. | `HtmlForm` |
| `Unknown` | No external interaction needed (manual/offline). | Just `IsSuccess = true` |

**`PaymentMethodGroupType`** — Categorizes the payment method for UI display:

| Value | Examples |
|-------|---------|
| `BankCard` | Visa/MC via CyberSource, Stripe, Authorize.Net |
| `Paypal` | PayPal checkout |
| `Alternative` | Klarna, Apple Pay, Google Pay |
| `Manual` | Wire transfer, COD, check |

### Developing a Custom Payment Method

#### Project Structure

Create a new module with the standard three-layer structure:

```
vc-module-my-gateway/
├── src/
│   ├── VirtoCommerce.MyGateway.Core/
│   │   ├── ModuleConstants.cs          # Settings definitions
│   │   ├── Models/                     # Gateway-specific DTOs
│   │   └── Services/
│   │       └── IMyGatewayClient.cs     # Gateway API client interface
│   ├── VirtoCommerce.MyGateway.Data/
│   │   ├── Providers/
│   │   │   └── MyGatewayPaymentMethod.cs  # THE PAYMENT METHOD
│   │   └── Services/
│   │       └── MyGatewayClient.cs      # Gateway API client implementation
│   └── VirtoCommerce.MyGateway.Web/
│       ├── Module.cs                   # DI + registration
│       ├── module.manifest             # Dependencies: VirtoCommerce.Payment
│       └── Localizations/
│           └── en.MyGateway.json       # UI labels
└── tests/
```

#### Step 1: Define Settings

```csharp
// ModuleConstants.cs
public static class ModuleConstants
{
    public static class Settings
    {
        public static class General
        {
            public static SettingDescriptor Sandbox { get; } = new()
            {
                Name = "VirtoCommerce.Payment.MyGateway.Sandbox",
                GroupName = "Payment|MyGateway",
                ValueType = SettingValueType.Boolean,
                DefaultValue = true,
            };

            public static SettingDescriptor ApiKey { get; } = new()
            {
                Name = "VirtoCommerce.Payment.MyGateway.ApiKey",
                GroupName = "Payment|MyGateway",
                ValueType = SettingValueType.SecureString,
            };

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    yield return Sandbox;
                    yield return ApiKey;
                }
            }
        }
    }
}
```

#### Step 2: Implement the Payment Method

```csharp
// MyGatewayPaymentMethod.cs
public class MyGatewayPaymentMethod(IMyGatewayClient gatewayClient)
    : PaymentMethod(nameof(MyGatewayPaymentMethod)), ISupportCaptureFlow, ISupportRefundFlow
{
    // Read settings from admin UI configuration
    private bool Sandbox => Settings.GetValue<bool>(ModuleConstants.Settings.General.Sandbox);

    // Tell the platform what kind of payment this is
    public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.BankCard;
    public override PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

    // --- PROCESS: Initialize payment, return redirect URL or tokens ---
    public override async Task<ProcessPaymentRequestResult> ProcessPaymentAsync(
        ProcessPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var session = await gatewayClient.CreateSessionAsync(new CreateSessionRequest
        {
            Amount = ((PaymentIn)request.Payment).Sum,
            Currency = ((CustomerOrder)request.Order).Currency,
            ReturnUrl = $"{((Store)request.Store).SecureUrl}/payment-result" +
                        $"?orderId={request.OrderId}&paymentId={request.PaymentId}",
        });

        return new ProcessPaymentRequestResult
        {
            IsSuccess = true,
            NewPaymentStatus = PaymentStatus.Pending,
            RedirectUrl = session.CheckoutUrl,    // <-- frontend will redirect here
            OuterId = session.TransactionId,      // <-- store gateway's ID for later use
        };
    }

    // --- VALIDATE: Check callback parameters from gateway ---
    public override Task<ValidatePostProcessRequestResult> ValidatePostProcessRequestAsync(
        NameValueCollection queryString, CancellationToken cancellationToken = default)
    {
        var signature = queryString["signature"];
        var isValid = gatewayClient.VerifySignature(queryString, signature);

        return Task.FromResult(new ValidatePostProcessRequestResult
        {
            IsSuccess = isValid,
            ErrorMessage = isValid ? null : "Invalid signature",
        });
    }

    // --- POST-PROCESS: Confirm payment after user returns from gateway ---
    public override async Task<PostProcessPaymentRequestResult> PostProcessPaymentAsync(
        PostProcessPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var transactionId = request.Parameters["transactionId"];
        var status = await gatewayClient.GetTransactionStatusAsync(transactionId);

        var payment = (PaymentIn)request.Payment;
        var result = new PostProcessPaymentRequestResult();

        if (status == "COMPLETED")
        {
            result.IsSuccess = true;
            result.NewPaymentStatus = PaymentStatus.Paid;
            payment.IsApproved = true;
            payment.CapturedDate = DateTime.UtcNow;
            payment.OuterId = transactionId;
        }
        else
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Payment status: {status}";
            result.NewPaymentStatus = PaymentStatus.Error;
        }

        return result;
    }

    // --- CAPTURE: Settle authorized funds ---
    public override async Task<CapturePaymentRequestResult> CaptureProcessPaymentAsync(
        CapturePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = (PaymentIn)request.Payment;
        await gatewayClient.CaptureAsync(payment.OuterId, request.CaptureAmount);

        return new CapturePaymentRequestResult
        {
            IsSuccess = true,
            NewPaymentStatus = PaymentStatus.Paid,
        };
    }

    // --- VOID: Cancel authorization ---
    public override async Task<VoidPaymentRequestResult> VoidProcessPaymentAsync(
        VoidPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = (PaymentIn)request.Payment;
        await gatewayClient.VoidAsync(payment.OuterId);

        return new VoidPaymentRequestResult
        {
            IsSuccess = true,
            NewPaymentStatus = PaymentStatus.Voided,
        };
    }

    // --- REFUND: Return captured funds ---
    public override async Task<RefundPaymentRequestResult> RefundProcessPaymentAsync(
        RefundPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = (PaymentIn)request.Payment;
        await gatewayClient.RefundAsync(payment.OuterId, request.AmountToRefund);

        return new RefundPaymentRequestResult
        {
            IsSuccess = true,
            NewRefundStatus = RefundStatus.Processed,
        };
    }
}
```

#### Step 3: Register in Module.cs

```csharp
// Module.cs
public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        // Bind gateway credentials from appsettings.json
        serviceCollection.AddOptions<MyGatewayOptions>()
            .Bind(Configuration.GetSection("Payments:MyGateway"))
            .ValidateDataAnnotations();

        // Register services
        serviceCollection.AddTransient<IMyGatewayClient, MyGatewayClient>();
        serviceCollection.AddTransient<MyGatewayPaymentMethod>();
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        // Register settings in admin UI
        var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
        settingsRegistrar.RegisterSettings(ModuleConstants.Settings.General.AllSettings, ModuleInfo.Id);

        // Register the payment method with factory
        var paymentMethodsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPaymentMethodsRegistrar>();
        paymentMethodsRegistrar.RegisterPaymentMethod(
            () => appBuilder.ApplicationServices.GetService<MyGatewayPaymentMethod>());

        // Bind settings to payment method type name (appears in admin UI per-method)
        settingsRegistrar.RegisterSettingsForType(
            ModuleConstants.Settings.General.AllSettings,
            nameof(MyGatewayPaymentMethod));
    }

    public void Uninstall() { }
}
```

#### Step 4: Module Manifest

```xml
<!-- module.manifest -->
<module>
  <id>VirtoCommerce.MyGateway</id>
  <version>1.0.0</version>
  <platformVersion>3.800.0</platformVersion>
  <dependencies>
    <dependency id="VirtoCommerce.Payment" version="3.800.0" />
  </dependencies>
  <title>My Gateway Payment Module</title>
  <description>Integrates My Gateway payment processing</description>
  <assemblyFile>VirtoCommerce.MyGateway.Web.dll</assemblyFile>
  <moduleType>VirtoCommerce.MyGateway.Web.Module, VirtoCommerce.MyGateway.Web</moduleType>
</module>
```

### Key Implementation Notes

1. **No frontend code needed.** The storefront handles all four `PaymentMethodType` patterns generically. Your payment method just returns the right data from `ProcessPaymentAsync` and the frontend knows what to do.

2. **`PublicParameters` are your client-side bridge.** For `Standard` type methods (like tokenized card entry), return JavaScript library URLs, session tokens, and keys via `PublicParameters` — the frontend renders them.

3. **`OuterId` is critical.** Store the gateway's transaction ID in `payment.OuterId` during `PostProcessPaymentAsync`. This ID is used by `CaptureProcessPaymentAsync`, `VoidProcessPaymentAsync`, and `RefundProcessPaymentAsync` to reference the original transaction.

4. **Cloned objects in request.** The XAPI layer may clone `request.Order` and `request.Payment` before passing them to your method. Write results to the result object, not directly to the request objects. The XAPI handler reads `result.NewPaymentStatus`, `result.OuterId`, etc. and applies them to the real entities.

5. **`initializePayment` does NOT save.** It's read-only. Only `authorizePayment` persists status changes. Design accordingly. The same applies to `initializeCartPayment`.

6. **Localization context.** `PaymentRequestBase.CultureName` carries the shopper's current culture (e.g. `en-US`) into every lifecycle call. It is populated by the X-API handlers from the mutation input when provided, otherwise from `Store.DefaultLanguage`. Use this to localize hosted widgets, error messages, and email receipts when the gateway supports it.

7. **Store validation.** `initializePayment`, `initializeCartPayment`, and `authorizePayment` accept an optional `storeId` input. When supplied, X-API verifies it matches the order/cart's store and throws otherwise — use this in the storefront to fail fast on cross-store requests.

8. **Single-message vs. Dual-message.** If your gateway supports both:
   - **Single-message** (auth + capture in one call): Set `NewPaymentStatus = PaymentStatus.Paid` in `PostProcessPaymentAsync`.
   - **Dual-message** (auth first, capture later): Set `NewPaymentStatus = PaymentStatus.Authorized` in `PostProcessPaymentAsync`. Implement `ISupportCaptureFlow` for the separate capture step.

9. **Gateway credentials** should go in `appsettings.json` (via `IOptions<T>`), not in module settings. Use module settings for user-facing toggles (sandbox mode, payment mode, accepted card types).

10. **Validation status rules:**
   - `ProcessPaymentAsync` is called when payment status is `New` or `Custom`.
   - `PostProcessPaymentAsync` is called when payment status is NOT `Paid`.
   - Always set `NewPaymentStatus` in your result — the XAPI uses it to update the persisted status.

### Supporting Stored Credentials (Card on File)

To let other modules save a card and charge it later, implement `ISupportStoredCredentialFlow` on your payment method and override three methods. The marker is how callers find out that your payment method supports the flow; without it, the base implementations throw `NotImplementedException`.

| Method | Called when | Returns |
|--------|-------------|---------|
| `StorePaymentMethodAsync(StorePaymentMethodRequest)` | A card is saved without a payment: added from the customer's account, or for a free trial. `Verify` asks for a $0 authorization | `StorePaymentMethodRequestResult` with the `StoredPaymentMethod` to persist (provider token and customer reference, brand, last 4 digits, expiry, network transaction ID); the caller saves it |
| `ProcessStoredPaymentAsync(ProcessStoredPaymentRequest)` | A charge without the customer present (renewal, delayed charge, reauthorization, resubmission) | `ProcessStoredPaymentRequestResult` with `Outcome`, transaction and network transaction IDs, authorization code and amount |
| `DeleteStoredPaymentMethodAsync(DeleteStoredPaymentMethodRequest)` | The customer removes the card | `DeleteStoredPaymentMethodRequestResult` |

```csharp
public class MyGatewayPaymentMethod : PaymentMethod, ISupportStoredCredentialFlow
{
    public override async Task<ProcessStoredPaymentRequestResult> ProcessStoredPaymentAsync(
        ProcessStoredPaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Charge request.StoredPaymentMethod.Token as a merchant-initiated transaction:
        // send request.IdempotencyKey, request.Reason and request.StoredPaymentMethod.NetworkTransactionId.
        var response = await _gateway.ChargeAsync(/* ... */);

        return new ProcessStoredPaymentRequestResult
        {
            IsSuccess = response.Approved,
            Outcome = MapOutcome(response), // Succeeded, RequiresAction, SoftDecline, HardDecline or ProviderError
            TransactionId = response.TransactionId,
            NetworkTransactionId = response.NetworkTransactionId,
            AuthorizationCode = response.AuthorizationCode,
            RawCode = response.Code,
            RawMessage = response.Message,
            AdviceCode = response.MerchantAdviceCode,
        };
    }

    // StorePaymentMethodAsync and DeleteStoredPaymentMethodAsync are overridden the same way.
}
```

**Saving the card during checkout.** When `StoreForFutureUse` is set on the `ProcessPaymentRequest` or `PostProcessPaymentRequest`, save the card at the provider together with the payment. Once the payment is authorized, return the card in the result's `StoredPaymentMethod`: the provider token and customer reference, brand, last 4 digits and expiry, `NetworkTransactionId` of this first charge, `InitialTransactionRef`, and `IsAuthenticated` when the customer completed 3-D Secure. Don't persist it and don't look up the customer's other cards: the caller records it with `IStoredPaymentMethodManager`, which adds the customer, store, order, payment, consent and default card.

**Paying with a saved card.** When the request's `StoredPaymentMethod` is set, charge that card as a customer-initiated stored credential transaction instead of new card data. The caller has already checked that the card belongs to the customer and is active.

**Results.** Map the provider's response to `ChargeOutcome` and fill `RawCode`, `RawMessage` and `AdviceCode`, so callers can apply card network retry rules without knowing the provider. Never show `RawCode` or `RawMessage` to customers. Set `RetryAfter` on a `SoftDecline` when the provider says when to retry, `RecoveryUrl` on `RequiresAction`, and `IsProviderRetryScheduled` when the provider runs its own retries (the caller then does not retry).

**Merchant-initiated charges.** Send the network transaction ID of the first charge with every later charge, and honour `IdempotencyKey` so that a retried charge is never taken twice. `CaptureMode = AuthorizeOnly` asks for an authorization to capture later; `AuthorizeAndCapture` captures at once.

**Security.** `Token` and `ProviderCustomerRef` are provider references, not card numbers, but together they can charge the card through the merchant's provider account. Keep them out of storefront and public API responses.

The Product Subscriptions module ships a connector test kit (`VirtoCommerce.ProductSubscription.ConnectorTestKit`) with contract tests for this flow: success, soft and hard declines, 3-D Secure on save and on renewal, and provider errors.

### Reference: CyberSource Implementation

The [vc-module-cyber-source](https://github.com/VirtoCommerce/vc-module-cyber-source) module is the reference implementation demonstrating:

- `PaymentMethodType.Standard` + `PaymentMethodGroupType.BankCard` — tokenized card entry via Flex Microform
- `ProcessPaymentAsync` returns `PublicParameters` with JWT capture context and client library URL
- `PostProcessPaymentAsync` receives the transient token and calls CyberSource's Payments API
- Single-message (auth+capture) and Dual-message (auth only) modes via settings
- Full Capture, Void, and Refund implementations
- Separated `ICyberSourceClient` for testability
- `appsettings.json` configuration for merchant credentials

## Documentation

* [Payment module user documentation](https://docs.virtocommerce.org/platform/user-guide/payment/overview/)
* [REST API](https://virtostart-demo-admin.govirto.com/docs/index.html?urls.primaryName=VirtoCommerce.Payment)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-payment)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-payment/releases/latest)

## License

Copyright (c) Virto Solutions LTD. All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
