//-----------------------------------------------------------------------
// <copyright file="RetryHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
// <summary>
// This file contains RetryHelper class.
// </summary>
//-----------------------------------------------------------------------
namespace Microsoft.Sales.MSALTokenGenerator
{
    using Microsoft.Rest.TransientFaultHandling;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    /// <summary>
    /// Enum representing different scenarios encountered while performing retry.
    /// </summary>
    public enum RetryErrorType
    {
        /// <summary>
        /// The retry is not applicable on the error.
        /// </summary>
        NonRetryable,

        /// <summary>
        /// The retries are pending when we encountered the error.
        /// </summary>
        RetriesPending,

        /// <summary>
        /// The Retries exhausted when we encountered the error.
        /// </summary>
        RetriesExhausted
    }

    /// <summary>
    /// The Retry Helper class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class RetryHelper
    {
        /// <summary>
        ///  Repetitively executes the specified function while it satisfies the applied retry strategy.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function.</typeparam>
        /// <param name="functionAsync">The function which need to be executed.</param>
        /// <param name="isTransientException">The function which determines the exception is transient or not.</param>
        /// /// <param name="shouldRetry">The function defining the delegate condition.</param>
        /// <param name="errorCallback">The function which need to be called on exception encounter.</param>
        /// <returns>the return type of the called function.</returns>
        public static async Task<TResult> RetryAsync<TResult>(
            Func<Task<TResult>> functionAsync,
            Predicate<Exception> isTransientException,
            Func<int, Exception, RetryCondition> shouldRetry,
            Action<Exception, RetryErrorType, int> errorCallback = null)
        {
            TResult result = default(TResult);
            await RetryHelper.ExecuteWithRetriesAsync(
                  async () => { result = await functionAsync().ConfigureAwait(false); },
                  isTransientException,
                  shouldRetry,
                  errorCallback).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        ///  Repetitively executes the specified function while it satisfies the applied retry strategy.
        /// </summary>
        /// <param name="functionAsync">The function which need to be executed.</param>
        /// <param name="isTransientException">The function which determines the exception is transient or not.</param>
        /// /// <param name="shouldRetry">The function defining the delegate condition.</param>
        /// <param name="errorCallback">The function which need to be called on exception encounter.</param>
        /// <returns>Task object.</returns>
        public static async Task RetryAsync(
            Func<Task> functionAsync,
            Predicate<Exception> isTransientException,
            Func<int, Exception, RetryCondition> shouldRetry,
            Action<Exception, RetryErrorType, int> errorCallback = null)
        {
            await RetryHelper.ExecuteWithRetriesAsync(
                  async () => { await functionAsync().ConfigureAwait(false); },
                  isTransientException,
                  shouldRetry,
                  errorCallback).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the with retries asynchronous.
        /// </summary>
        /// <param name="functionAsync">The function asynchronous.</param>
        /// <param name="isTransientException">The is transient exception.</param>
        /// <param name="shouldRetry">The should retry.</param>
        /// <param name="errorCallback">The error callback.</param>
        /// <returns>Task object.</returns>
        private static async Task ExecuteWithRetriesAsync(
            Func<Task> functionAsync,
            Predicate<Exception> isTransientException,
            Func<int, Exception, RetryCondition> shouldRetry,
            Action<Exception, RetryErrorType, int> errorCallback)
        {
                int retryCount = 0;
                TimeSpan delay = TimeSpan.Zero;

                while (true)
                {
                    try
                    {
                        await functionAsync().ConfigureAwait(false);
                        return;
                    }
                    catch (Exception exception)
                    {
                        if (!isTransientException(exception))
                        {
                            // Exception is not a transient one.
                            errorCallback?.Invoke(exception, RetryErrorType.NonRetryable, retryCount);
                            throw;
                        }

                        RetryCondition condition = shouldRetry(retryCount, exception);

                        if (!condition.RetryAllowed)
                        {
                            // No more retry allowed scenario.
                            errorCallback?.Invoke(exception, RetryErrorType.RetriesExhausted, retryCount);
                            throw;
                        }

                        errorCallback?.Invoke(exception, RetryErrorType.RetriesPending, retryCount);

                        await Task.Delay(condition.DelayBeforeRetry).ConfigureAwait(false);
                        retryCount++;
                    }
                }
        }
    }
}