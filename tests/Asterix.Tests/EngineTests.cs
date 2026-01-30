using Xunit;
using Asterix.Core.Engine;
using Asterix.Core.Models;

namespace Asterix.Tests
{
    public class EngineTests
    {
        [Fact]
        public void Step_ShouldNotThrow()
        {
            var engine = new GameEngine();
            var state = new GameState(0, 0);
            var rng = new SimpleRandom(123);
            var next = engine.Step(state, null, rng);
            Assert.NotNull(next);
        }
    }
}
