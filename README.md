# Payment Module

[![CI status](https://github.com/VirtoCommerce/vc-module-payment/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-payment/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-payment&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-payment) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-payment&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-payment) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-payment&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-payment) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-payment&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-payment)

## Overview

The Payment module provides management and integration of various payment methods within the Virto Commerce platform. It defines an extensible abstraction for payment processing — including payment registration, post-processing verification, void, capture, and refund operations — and exposes a REST API and admin UI for configuring which payment methods are active per store. The module ships with a built-in `DefaultManualPaymentMethod` and allows other modules to register custom payment method implementations at runtime through the `IPaymentMethodsRegistrar` interface.

## Key Features

* **Extensible payment method registration** — Register custom payment methods via code using `IPaymentMethodsRegistrar.RegisterPaymentMethod<T>()`; payment methods are resolved through `AbstractTypeFactory` polymorphism
* **Full payment lifecycle** — Abstract `PaymentMethod` base class supports Process, PostProcess, Void, Capture, and Refund operations with both synchronous (obsolete) and async APIs
* **Per-store configuration** — Each payment method is persisted per store with settings for activation, priority, logo, deferred payment, partial payment availability, and localized display names
* **Search and filtering** — Search payment methods by store, code, keyword, and active status; includes both persisted and transient (registered but not yet configured) methods in results
* **Built-in manual payment method** — `DefaultManualPaymentMethod` provides an out-of-the-box payment method supporting capture and refund flows for manual/offline payment scenarios
* **Localized display names** — Payment methods support localized names per language through the `PaymentMethodLocalizedName` entity
* **Domain events** — Publishes `PaymentMethodChangeEvent` (before save), `PaymentMethodChangedEvent` (after save), and `PaymentMethodInstancingEvent` (before instantiation) for extensibility
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

### Key Flow

1. **Registration** — During module initialization (`PostInitialize`), payment methods are registered with `IPaymentMethodsRegistrar.RegisterPaymentMethod<T>()`, which adds them to `AbstractTypeFactory<PaymentMethod>`
2. **Configuration** — Operators configure payment methods per store via the admin UI or REST API; configurations are persisted as `StorePaymentMethodEntity` records with localized names
3. **Search / Discovery** — `PaymentMethodsSearchService` queries persisted methods from the database, then appends transient (registered but unconfigured) methods, filtering by store, code, keyword, and active status
4. **Instantiation** — When a payment method is loaded from the database, `PaymentMethodInstancingEvent` is published so that dependent modules (e.g., NativePaymentMethods) can register their types before `AbstractTypeFactory` resolves the concrete type
5. **Settings hydration** — After entity-to-model conversion, `ISettingsManager.DeepLoadSettingsAsync` loads method-specific settings into the `PaymentMethod.Settings` collection
6. **Payment processing** — The storefront or order module calls the async methods on the resolved `PaymentMethod` instance: `ProcessPaymentAsync` → `PostProcessPaymentAsync` → `CaptureProcessPaymentAsync` / `VoidProcessPaymentAsync` / `RefundProcessPaymentAsync`
7. **Persistence** — Updates flow through `PaymentMethodsService.SaveChangesAsync`, which persists entity data and deep-saves settings, publishing change events before and after

## Components

### Projects

| Project | Layer | Purpose |
|---------|-------|---------|
| `VirtoCommerce.PaymentModule.Core` | Core/Domain | Domain models (`PaymentMethod`, request/result types, enums), service interfaces (`IPaymentMethodsRegistrar`, `IPaymentMethodsService`, `IPaymentMethodsSearchService`), domain events, and module constants/settings |
| `VirtoCommerce.PaymentModule.Data` | Data/Infrastructure | Service implementations (`PaymentMethodsService`, `PaymentMethodsSearchService`), EF Core entities and repository (`PaymentRepository`, `PaymentDbContext`), caching (`PaymentCacheRegion`), export/import, and `DefaultManualPaymentMethod` |
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
| `ProcessPaymentAsync` | Initialize payment with gateway. Return redirect URL, HTML form, public parameters, or just success. | `initializePayment` mutation | After order is created, before user pays |
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

5. **`initializePayment` does NOT save.** It's read-only. Only `authorizePayment` persists status changes. Design accordingly.

6. **Single-message vs. Dual-message.** If your gateway supports both:
   - **Single-message** (auth + capture in one call): Set `NewPaymentStatus = PaymentStatus.Paid` in `PostProcessPaymentAsync`.
   - **Dual-message** (auth first, capture later): Set `NewPaymentStatus = PaymentStatus.Authorized` in `PostProcessPaymentAsync`. Implement `ISupportCaptureFlow` for the separate capture step.

7. **Gateway credentials** should go in `appsettings.json` (via `IOptions<T>`), not in module settings. Use module settings for user-facing toggles (sandbox mode, payment mode, accepted card types).

8. **Validation status rules:**
   - `ProcessPaymentAsync` is called when payment status is `New` or `Custom`.
   - `PostProcessPaymentAsync` is called when payment status is NOT `Paid`.
   - Always set `NewPaymentStatus` in your result — the XAPI uses it to update the persisted status.

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
