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
        /// Async method overridden
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
        /// Nothing overridden - should throw
        /// </summary>
        private class NoOverridePaymentMethod : PaymentMethod
        {
            public NoOverridePaymentMethod() : base("NoOverride") { }
            public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
            public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.Manual;
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
