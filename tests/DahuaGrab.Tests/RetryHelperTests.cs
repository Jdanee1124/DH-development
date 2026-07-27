using CameraSDK;
using Xunit;

namespace DahuaGrab.Tests
{
    public class RetryHelperTests
    {
        [Fact]
        public void RetryAction_SucceedsFirstTime()
        {
            int attempts = 0;
            bool result = RetryHelper.RetryAction(() => { attempts++; }, maxRetries: 3, baseDelayMs: 10);

            Assert.True(result);
            Assert.Equal(1, attempts);
        }

        [Fact]
        public void RetryAction_RetriesOnFailure()
        {
            int attempts = 0;
            bool result = RetryHelper.RetryAction(() =>
            {
                attempts++;
                if (attempts < 3)
                    throw new InvalidOperationException("fail");
            }, maxRetries: 3, baseDelayMs: 10);

            Assert.True(result);
            Assert.Equal(3, attempts);
        }

        [Fact]
        public void RetryAction_FailsAfterMaxRetries()
        {
            int attempts = 0;
            bool result = RetryHelper.RetryAction(() =>
            {
                attempts++;
                throw new InvalidOperationException("always fail");
            }, maxRetries: 3, baseDelayMs: 10);

            Assert.False(result);
            Assert.Equal(3, attempts);
        }

        [Fact]
        public void RetryAction_ShouldRetryFalse_StopsEarly()
        {
            int attempts = 0;
            bool result = RetryHelper.RetryAction(() =>
            {
                attempts++;
                throw new ArgumentException("no retry");
            }, maxRetries: 3, baseDelayMs: 10, shouldRetry: ex => false);

            Assert.False(result);
            Assert.Equal(1, attempts);
        }

        [Fact]
        public async Task RetryAsync_SucceedsFirstTime()
        {
            int attempts = 0;
            var result = await RetryHelper.RetryAsync<string>(() =>
            {
                attempts++;
                return Task.FromResult("ok");
            }, maxRetries: 3, baseDelayMs: 10);

            Assert.Equal("ok", result);
            Assert.Equal(1, attempts);
        }

        [Fact]
        public async Task RetryAsync_ReturnsNullAfterMaxRetries()
        {
            int attempts = 0;
            var result = await RetryHelper.RetryAsync<string>(() =>
            {
                attempts++;
                throw new InvalidOperationException("fail");
            }, maxRetries: 3, baseDelayMs: 10);

            Assert.Null(result);
            Assert.Equal(3, attempts);
        }
    }
}
