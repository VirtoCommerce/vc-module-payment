using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Model.Requests;
using Xunit;

namespace VirtoCommerce.Payment.Tests
{
    public class PaymentMethodAsyncTests
    {
        #region Test Payment Method Implementations

        /// <summary>
        /// Legacy: Only sync method overridden
        /// </summary>
        private class SyncOnlyPaymentMethod : PaymentMethod
        {
            public SyncOnlyPaymentMethod() : base("SyncOnly") { }
            public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
            public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.Manual;

            public bool SyncMethodCalled { get; private set; }

            [Obsolete]
            public override ProcessPaymentRequestResult ProcessPayment(ProcessPaymentRequest request)
            {
                SyncMethodCalled = true;
                return new ProcessPaymentRequestResult { IsSuccess = true, OuterId = "sync_123" };
            }
        }

        /// <summary>
        /// Modern: Only async method overridden
        /// </summary>
        private class AsyncOnlyPaymentMethod : PaymentMethod
        {
            public AsyncOnlyPaymentMethod() : base("AsyncOnly") { }
            public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
            public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.Manual;

            public bool AsyncMethodCalled { get; private set; }
            public CancellationToken ReceivedToken { get; private set; }

            public override async Task<ProcessPaymentRequestResult> ProcessPaymentAsync(
                ProcessPaymentRequest request,
                CancellationToken cancellationToken = default)
            {
                AsyncMethodCalled = true;
                ReceivedToken = cancellationToken;
                await Task.Delay(10, cancellationToken);
                return new ProcessPaymentRequestResult { IsSuccess = true, OuterId = "async_456" };
            }
        }

        /// <summary>
        /// Both methods overridden
        /// </summary>
        private class BothOverriddenPaymentMethod : PaymentMethod
        {
            public BothOverriddenPaymentMethod() : base("Both") { }
            public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
            public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.Manual;

            public bool SyncMethodCalled { get; private set; }
            public bool AsyncMethodCalled { get; private set; }

            [Obsolete]
            public override ProcessPaymentRequestResult ProcessPayment(ProcessPaymentRequest request)
            {
                SyncMethodCalled = true;
                return new ProcessPaymentRequestResult { IsSuccess = true, OuterId = "both_sync" };
            }

            public override Task<ProcessPaymentRequestResult> ProcessPaymentAsync(
                ProcessPaymentRequest request,
                CancellationToken cancellationToken = default)
            {
                AsyncMethodCalled = true;
                return Task.FromResult(new ProcessPaymentRequestResult
                {
                    IsSuccess = true,
                    OuterId = "both_async"
                });
            }
        }

        /// <summary>
        /// Nothing overridden - should throw
        /// </summary>
        private class NoOverridePaymentMethod : PaymentMethod
        {
            public NoOverridePaymentMethod() : base("NoOverride") { }
            public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
            public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.Manual;
        }

        #endregion

        #region Sync-Only Tests

        [Fact]
        public void SyncOnly_CallSync_ShouldCallSyncMethod()
        {
            // Arrange
            var method = new SyncOnlyPaymentMethod();
            var request = new ProcessPaymentRequest();

            // Act
#pragma warning disable VC0012 // Type or member is obsolete
            var result = method.ProcessPayment(request);
#pragma warning restore VC0012 // Type or member is obsolete

            // Assert
            method.SyncMethodCalled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
            result.OuterId.Should().Be("sync_123");
        }

        [Fact]
        public async Task SyncOnly_CallAsync_ShouldRoutToSyncMethod()
        {
            // Arrange
            var method = new SyncOnlyPaymentMethod();
            var request = new ProcessPaymentRequest();

            // Act
            var result = await method.ProcessPaymentAsync(request);

            // Assert
            method.SyncMethodCalled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
            result.OuterId.Should().Be("sync_123");
        }

        #endregion

        #region Async-Only Tests

        [Fact]
        public async Task AsyncOnly_CallAsync_ShouldCallAsyncMethod()
        {
            // Arrange
            var method = new AsyncOnlyPaymentMethod();
            var request = new ProcessPaymentRequest();
            var cts = new CancellationTokenSource();

            // Act
            var result = await method.ProcessPaymentAsync(request, cts.Token);

            // Assert
            method.AsyncMethodCalled.Should().BeTrue();
            method.ReceivedToken.Should().Be(cts.Token);
            result.IsSuccess.Should().BeTrue();
            result.OuterId.Should().Be("async_456");
        }

        #endregion

        #region Both-Overridden Tests

        [Fact]
        public async Task Both_CallAsync_ShouldCallAsyncDirectly()
        {
            // Arrange
            var method = new BothOverriddenPaymentMethod();
            var request = new ProcessPaymentRequest();

            // Act
            var result = await method.ProcessPaymentAsync(request);

            // Assert
            method.AsyncMethodCalled.Should().BeTrue();
            method.SyncMethodCalled.Should().BeFalse(); // Should NOT call sync
            result.OuterId.Should().Be("both_async");
        }

        #endregion

        #region No Override Tests

        [Fact]
        public Task NoOverride_CallAsync_ShouldThrow()
        {
            // Arrange
            var method = new NoOverridePaymentMethod();
            var request = new ProcessPaymentRequest();

            // Act & Assert
            var action = async () => await method.ProcessPaymentAsync(request);
            return action.Should().ThrowAsync<NotImplementedException>();
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public Task AsyncOnly_CancellationRequested_ShouldCancel()
        {
            // Arrange
            var method = new AsyncOnlyPaymentMethod();
            var request = new ProcessPaymentRequest();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            var action = async () => await method.ProcessPaymentAsync(request, cts.Token);
            return action.Should().ThrowAsync<TaskCanceledException>();
        }

        #endregion
    }
}
