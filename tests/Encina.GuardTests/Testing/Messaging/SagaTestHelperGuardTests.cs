using Encina.Testing.Fakes.Stores;
using Encina.Testing.Messaging;
using Encina.Testing.Time;

namespace Encina.GuardTests.Testing.Messaging;

public class SagaTestHelperGuardTests : IDisposable
{
    private readonly SagaTestHelper _sut = new();

    public void Dispose() => _sut.Dispose();

    public class ConstructorGuards
    {
        [Fact]
        public void NullStore_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SagaTestHelper(null!, new FakeTimeProvider()));
        }

        [Fact]
        public void NullTimeProvider_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new SagaTestHelper(new FakeSagaStore(), null!));
        }
    }

    public class GivenGuards : SagaTestHelperGuardTests
    {
        [Fact]
        public void GivenNewSaga_NullData_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenNewSaga<TestSaga, TestSagaData>(Guid.NewGuid(), null!));
        }

        [Fact]
        public void GivenRunningSaga_NullData_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenRunningSaga<TestSaga, TestSagaData>(Guid.NewGuid(), null!));
        }

        [Fact]
        public void GivenCompletedSaga_NullData_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenCompletedSaga<TestSaga, TestSagaData>(Guid.NewGuid(), null!));
        }

        [Fact]
        public void GivenCompensatingSaga_NullData_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenCompensatingSaga<TestSaga, TestSagaData>(Guid.NewGuid(), null!));
        }

        [Fact]
        public void GivenFailedSaga_NullData_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.GivenFailedSaga<TestSaga, TestSagaData>(Guid.NewGuid(), null!));
        }
    }

    public class WhenGuards : SagaTestHelperGuardTests
    {
        [Fact]
        public void WhenSagaStarts_NullData_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenSagaStarts<TestSaga, TestSagaData>(Guid.NewGuid(), null!));
        }

        [Fact]
        public void WhenSagaDataUpdated_NullData_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenSagaDataUpdated<TestSagaData>(Guid.NewGuid(), null!));
        }

        [Fact]
        public void When_NullAction_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.When((Action<FakeSagaStore>)null!));
        }

        [Fact]
        public void WhenAsync_NullAction_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                _sut.WhenAsync(null!));
        }
    }

    public class ThenGuards : SagaTestHelperGuardTests
    {
        [Fact]
        public void ThenSagaData_NullPredicate_Throws()
        {
            var sagaId = Guid.NewGuid();
            _sut.GivenRunningSaga<TestSaga, TestSagaData>(sagaId, new TestSagaData())
                .WhenSagaCompletes(sagaId);

            Should.Throw<ArgumentNullException>(() =>
                _sut.ThenSagaData<TestSagaData>(sagaId, null!));
        }
    }

    public class ThenBeforeWhenGuards : SagaTestHelperGuardTests
    {
        [Fact]
        public void ThenNoException_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenNoException());
        }

        [Fact]
        public void ThenThrows_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenThrows<InvalidOperationException>());
        }

        [Fact]
        public void ThenSagaStatus_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenSagaStatus(Guid.NewGuid(), "Running"));
        }

        [Fact]
        public void ThenSagaIsAtStep_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenSagaIsAtStep(Guid.NewGuid(), 1));
        }

        [Fact]
        public void ThenSagaWasStarted_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenSagaWasStarted<TestSaga>());
        }

        [Fact]
        public void ThenSagaHasCompletedAt_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenSagaHasCompletedAt(Guid.NewGuid()));
        }

        [Fact]
        public void ThenSagaHasError_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.ThenSagaHasError(Guid.NewGuid()));
        }

        [Fact]
        public void GetSagaData_BeforeWhen_Throws()
        {
            Should.Throw<InvalidOperationException>(() =>
                _sut.GetSagaData<TestSagaData>(Guid.NewGuid()));
        }
    }

    private sealed class TestSaga;
    private sealed class TestSagaData
    {
        public string Name { get; set; } = "Test";
    }
}
