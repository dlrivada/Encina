using Encina.MongoDB;

namespace Encina.UnitTests.MongoDB;

public sealed class MongoDbErrorCodesTests
{
    [Fact]
    public void AllCodes_StartWithMONGODB()
    {
        MongoDbErrorCodes.ConnectionFailed.ShouldStartWith("MONGODB_");
        MongoDbErrorCodes.DocumentNotFound.ShouldStartWith("MONGODB_");
        MongoDbErrorCodes.DuplicateKey.ShouldStartWith("MONGODB_");
        MongoDbErrorCodes.WriteConflict.ShouldStartWith("MONGODB_");
        MongoDbErrorCodes.SerializationFailed.ShouldStartWith("MONGODB_");
        MongoDbErrorCodes.Timeout.ShouldStartWith("MONGODB_");
        MongoDbErrorCodes.GeneralError.ShouldStartWith("MONGODB_");
    }

    [Fact]
    public void AllCodes_AreUnique()
    {
        var codes = new[]
        {
            MongoDbErrorCodes.ConnectionFailed,
            MongoDbErrorCodes.DocumentNotFound,
            MongoDbErrorCodes.DuplicateKey,
            MongoDbErrorCodes.WriteConflict,
            MongoDbErrorCodes.SerializationFailed,
            MongoDbErrorCodes.Timeout,
            MongoDbErrorCodes.GeneralError
        };
        codes.Distinct().Count().ShouldBe(codes.Length);
    }
}
