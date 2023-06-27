namespace CorsairLink.Tests
{
    public class ChannelTrackingStoreTests
    {
        [Fact]
        public void ApplyChanges_Should_UpdateStore_When_QueueHasItems()
        {
            // Arrange
            var store = new ChannelTrackingStore();
            store[1] = 10;
            store[2] = 20;

            // Act
            var result = store.ApplyChanges();

            // Assert
            Assert.True(result);
            Assert.Equal(10, store[1]);
            Assert.Equal(20, store[2]);
        }

        [Fact]
        public void ApplyChanges_Should_NotUpdateStore_When_QueueIsEmpty()
        {
            // Arrange
            var store = new ChannelTrackingStore();

            // Act
            var result = store.ApplyChanges();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ApplyChanges_Should_UpdateStore_When_QueueHasItemsAfterChangesApplied()
        {
            // Arrange
            var store = new ChannelTrackingStore();
            store[1] = 10;
            store[2] = 20;
            _ = store.ApplyChanges();
            store[1] = 0;

            // Act
            var result = store.ApplyChanges();

            // Assert
            Assert.True(result);
            Assert.Equal(0, store[1]);
            Assert.Equal(20, store[2]);
        }

        [Fact]
        public void ApplyChanges_Should_NotUpdateStore_When_QueueHasItemsNotYetApplied()
        {
            // Arrange
            var store = new ChannelTrackingStore();
            store[1] = 10;
            store[2] = 20;
            _ = store.ApplyChanges();

            // Act
            store[1] = 0;

            // Assert
            Assert.Equal(10, store[1]);
            Assert.Equal(20, store[2]);
        }

        [Fact]
        public void ApplyChanges_Should_NotUpdateStore_When_QueueDoesNotChangeCurrentValues()
        {
            // Arrange
            var store = new ChannelTrackingStore();
            store[1] = 10;
            store[2] = 20;
            _ = store.ApplyChanges();
            store[1] = 10;

            // Act
            var result = store.ApplyChanges();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Clear_Should_ClearQueueAndStore()
        {
            // Arrange
            var store = new ChannelTrackingStore();
            store[1] = 10;
            store[2] = 20;

            // Act
            store.Clear();

            // Assert
            Assert.Empty(store.Channels);
            Assert.Equal(0, store.QueueLength);
        }
    }
}
