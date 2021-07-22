using Microsoft.Rest.TransientFaultHandling;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Sales.MSALTokenGenerator
{
    [ExcludeFromCodeCoverage]
    public class ExponentialRetryStrategy : RetryStrategy
    { /// <summary>
      /// Represents the default amount of time used when calculating a random delta in the exponential 
      /// delay between retries.
      /// </summary>
        public static readonly TimeSpan DefaultDeltaBackoff = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Represents the default maximum amount of time used when calculating the exponential 
        /// delay between retries.
        /// </summary>
        public static readonly TimeSpan DefaultMaxBackoff = TimeSpan.FromMilliseconds(1000);

        /// <summary>
        /// Represents the default minimum amount of time used when calculating the exponential 
        /// delay between retries.
        /// </summary>
        public static readonly TimeSpan DefaultMinBackoff = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialRetryStrategy"/> class. 
        /// </summary>
        public ExponentialRetryStrategy()
            : this(
                  ExponentialRetryStrategy.DefaultRetryCount,
                  ExponentialRetryStrategy.DefaultMinBackoff,
                  ExponentialRetryStrategy.DefaultMaxBackoff,
                  ExponentialRetryStrategy.DefaultDeltaBackoff)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialRetryStrategy"/> class.
        /// </summary>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum back off time</param>
        /// <param name="maxBackoff">The maximum back off time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        public ExponentialRetryStrategy(int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
        {
            this.MaxRetryCount = retryCount;
            this.MinBackoff = minBackoff;
            this.MaxBackoff = maxBackoff;
            this.DeltaBackoff = deltaBackoff;
        }

        /// <summary>
        /// Gets the delta back off time.
        /// </summary>
        public TimeSpan DeltaBackoff { get; }

        /// <summary>
        /// Gets the max back off time.
        /// </summary>
        public TimeSpan MaxBackoff { get; }

        /// <summary>
        /// Gets the min back off time.
        /// </summary>
        public TimeSpan MinBackoff { get; }

        /// <summary>
        /// Gets the maximum retry count.
        /// </summary>
        public int MaxRetryCount { get; }

        /// <summary>
        /// Returns the corresponding Should Retry delegate.
        /// </summary>
        /// <returns>The Should Retry delegate.</returns>
        public override Func<int, Exception, RetryCondition> ShouldRetry()
        {
                return (currentRetryCount, exception) =>
                {
                    if (currentRetryCount < this.MaxRetryCount)
                    {
                        Random random = new Random();

                        // This is the suggested way to find delta, 0.8 and 1.2 are values being used overall in Microsoft repos.
                        var delta = (Math.Pow(2.0, currentRetryCount) - 1.0) * random.Next((int)(this.DeltaBackoff.TotalMilliseconds * 0.8), (int)(this.DeltaBackoff.TotalMilliseconds * 1.2));
                        var interval = (int)Math.Min(this.MinBackoff.TotalMilliseconds + delta, this.MaxBackoff.TotalMilliseconds);
                        TimeSpan retryInterval = TimeSpan.FromMilliseconds(interval);

                        return new RetryCondition(true, retryInterval);
                    }

                    return new RetryCondition(false, TimeSpan.Zero);
                };
            }
    }
}
