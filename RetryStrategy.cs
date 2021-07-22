//-----------------------------------------------------------------------
// <copyright file="RetryStrategy.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
// <summary>
// This file contains RetryStrategy class.
// </summary>
//-----------------------------------------------------------------------
namespace Microsoft.Sales.MSALTokenGenerator
{
    using Microsoft.Rest.TransientFaultHandling;
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    /// <summary>
    /// The retry strategy that determines the number of retry attempts and the interval between the retries.
    /// </summary>
    public abstract class RetryStrategy
    {
        /// <summary>
        /// The default number of retry attempts.
        /// </summary>
        public static readonly int DefaultRetryCount = 3;

        /// <summary>
        /// Returns the corresponding ShouldRetry delegate.
        /// </summary>
        /// <returns>The ShouldRetry delegate.</returns>
        public abstract Func<int, Exception, RetryCondition> ShouldRetry();
    }
}
