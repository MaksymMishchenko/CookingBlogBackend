namespace PostApiService.Tests.Helper
{
    public static class DateAssert
    {
        public static void EqualWithPrecision(DateTime? expected, DateTime? actual, int precisionMilliseconds = 100)
        {
            if (expected == null && actual == null) return;

            Assert.NotNull(expected);
            Assert.NotNull(actual);

            var diff = (expected.Value - actual.Value).Duration();

            Assert.True(diff <= TimeSpan.FromMilliseconds(precisionMilliseconds),
                $"Dates differ by {diff.TotalMilliseconds}ms, which is more than allowed {precisionMilliseconds}ms.");
        }
    }
}
