using Xunit;

namespace DahuaGrab.Tests
{
    public class TriggerEnumTests
    {
        [Fact]
        public void TriggerModeEnum_HasExpectedValues()
        {
            Assert.Equal(0, (int)TriggerModeEnum.Continuous);
            Assert.Equal(1, (int)TriggerModeEnum.Software);
            Assert.Equal(2, (int)TriggerModeEnum.Hardware);
        }

        [Fact]
        public void TriggerSourceEnum_HasExpectedValues()
        {
            Assert.Equal(0, (int)TriggerSourceEnum.Software);
            Assert.Equal(1, (int)TriggerSourceEnum.Line0);
            Assert.Equal(2, (int)TriggerSourceEnum.Line1);
            Assert.Equal(3, (int)TriggerSourceEnum.Line2);
            Assert.Equal(4, (int)TriggerSourceEnum.Line3);
        }

        [Fact]
        public void TriggerModeEnum_ParsesString()
        {
            Assert.True(Enum.TryParse<TriggerModeEnum>("Software", out var mode));
            Assert.Equal(TriggerModeEnum.Software, mode);
        }

        [Fact]
        public void TriggerSourceEnum_ParsesString()
        {
            Assert.True(Enum.TryParse<TriggerSourceEnum>("Line0", out var source));
            Assert.Equal(TriggerSourceEnum.Line0, source);
        }
    }
}
