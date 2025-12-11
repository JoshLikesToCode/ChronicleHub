using ChronicleHub.Domain.Entities;
using FluentAssertions;

namespace ChronicleHub.Domain.Tests.Entities;

public class ActivityEventTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateActivityEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = "page_view";
        var source = "web-app";
        var timestampUtc = new DateTime(2025, 12, 11, 10, 30, 0, DateTimeKind.Utc);
        var payloadJson = "{\"page\":\"/home\"}";

        // Act
        var sut = new ActivityEvent(
            id,
            tenantId,
            userId,
            type,
            source,
            timestampUtc,
            payloadJson
        );

        // Assert
        sut.Id.Should().Be(id);
        sut.TenantId.Should().Be(tenantId);
        sut.UserId.Should().Be(userId);
        sut.Type.Should().Be(type);
        sut.Source.Should().Be(source);
        sut.TimestampUtc.Should().Be(timestampUtc);
        sut.PayloadJson.Should().Be(payloadJson);
        sut.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_ShouldSetCreatedAtUtc_ToCurrentUtcTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var sut = new ActivityEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test_event",
            "test-source",
            DateTime.UtcNow,
            "{}"
        );

        var afterCreation = DateTime.UtcNow;

        // Assert
        sut.CreatedAtUtc.Should().BeOnOrAfter(beforeCreation);
        sut.CreatedAtUtc.Should().BeOnOrBefore(afterCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceType_ShouldStillCreateEvent(string type)
    {
        // Arrange & Act
        var sut = new ActivityEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            "source",
            DateTime.UtcNow,
            "{}"
        );

        // Assert
        sut.Type.Should().Be(type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhiteSpaceSource_ShouldStillCreateEvent(string source)
    {
        // Arrange & Act
        var sut = new ActivityEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "type",
            source,
            DateTime.UtcNow,
            "{}"
        );

        // Assert
        sut.Source.Should().Be(source);
    }

    [Fact]
    public void Constructor_WithEmptyGuidIds_ShouldAcceptEmptyGuids()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var sut = new ActivityEvent(
            emptyGuid,
            emptyGuid,
            emptyGuid,
            "type",
            "source",
            DateTime.UtcNow,
            "{}"
        );

        // Assert
        sut.Id.Should().Be(Guid.Empty);
        sut.TenantId.Should().Be(Guid.Empty);
        sut.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Constructor_WithComplexJsonPayload_ShouldStorePayloadCorrectly()
    {
        // Arrange
        var complexPayload = @"{
            ""user"": {
                ""name"": ""John Doe"",
                ""age"": 30
            },
            ""items"": [1, 2, 3],
            ""metadata"": {
                ""source"": ""mobile"",
                ""version"": ""1.2.3""
            }
        }";

        // Act
        var sut = new ActivityEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "complex_event",
            "mobile-app",
            DateTime.UtcNow,
            complexPayload
        );

        // Assert
        sut.PayloadJson.Should().Be(complexPayload);
    }

    [Fact]
    public void Properties_ShouldHavePrivateSetters()
    {
        // Arrange & Act
        var sut = new ActivityEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "type",
            "source",
            DateTime.UtcNow,
            "{}"
        );

        // Assert - Verify properties are immutable from outside
        var idProperty = typeof(ActivityEvent).GetProperty(nameof(ActivityEvent.Id));
        var tenantIdProperty = typeof(ActivityEvent).GetProperty(nameof(ActivityEvent.TenantId));
        var userIdProperty = typeof(ActivityEvent).GetProperty(nameof(ActivityEvent.UserId));
        var typeProperty = typeof(ActivityEvent).GetProperty(nameof(ActivityEvent.Type));
        var sourceProperty = typeof(ActivityEvent).GetProperty(nameof(ActivityEvent.Source));
        var timestampUtcProperty = typeof(ActivityEvent).GetProperty(nameof(ActivityEvent.TimestampUtc));
        var payloadJsonProperty = typeof(ActivityEvent).GetProperty(nameof(ActivityEvent.PayloadJson));
        var createdAtUtcProperty = typeof(ActivityEvent).GetProperty(nameof(ActivityEvent.CreatedAtUtc));

        idProperty!.SetMethod.Should().NotBeNull();
        idProperty.SetMethod!.IsPrivate.Should().BeTrue();

        tenantIdProperty!.SetMethod.Should().NotBeNull();
        tenantIdProperty.SetMethod!.IsPrivate.Should().BeTrue();

        userIdProperty!.SetMethod.Should().NotBeNull();
        userIdProperty.SetMethod!.IsPrivate.Should().BeTrue();

        typeProperty!.SetMethod.Should().NotBeNull();
        typeProperty.SetMethod!.IsPrivate.Should().BeTrue();

        sourceProperty!.SetMethod.Should().NotBeNull();
        sourceProperty.SetMethod!.IsPrivate.Should().BeTrue();

        timestampUtcProperty!.SetMethod.Should().NotBeNull();
        timestampUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();

        payloadJsonProperty!.SetMethod.Should().NotBeNull();
        payloadJsonProperty.SetMethod!.IsPrivate.Should().BeTrue();

        createdAtUtcProperty!.SetMethod.Should().NotBeNull();
        createdAtUtcProperty.SetMethod!.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithPastTimestamp_ShouldAcceptPastDates()
    {
        // Arrange
        var pastTimestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = new ActivityEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "historical_event",
            "import",
            pastTimestamp,
            "{}"
        );

        // Assert
        sut.TimestampUtc.Should().Be(pastTimestamp);
        sut.CreatedAtUtc.Should().BeAfter(pastTimestamp);
    }

    [Fact]
    public void Constructor_WithFutureTimestamp_ShouldAcceptFutureDates()
    {
        // Arrange
        var futureTimestamp = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var sut = new ActivityEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "scheduled_event",
            "scheduler",
            futureTimestamp,
            "{}"
        );

        // Assert
        sut.TimestampUtc.Should().Be(futureTimestamp);
        sut.CreatedAtUtc.Should().BeBefore(futureTimestamp);
    }
}
