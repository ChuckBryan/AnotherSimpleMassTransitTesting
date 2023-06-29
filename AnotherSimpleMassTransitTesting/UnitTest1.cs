using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Moq;
using Xunit.Abstractions;

namespace AnotherSimpleMassTransitTesting;
[Collection("MassTransit")]
public class UnitTest1
{
    private readonly MassTransitFixture fixture;
    private readonly ITestOutputHelper output;

    public UnitTest1(MassTransitFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        this.output = output;
    }

    [Fact]
    public async Task Test2()
    {
        var publisherMock = fixture.PublisherMock;
        var harness = fixture.Harness;   
        await harness.Start();
        try
        {
            publisherMock.Setup(x=>x.Publish(It.IsAny<DetailsRequest>()));
            await harness.Bus.Publish<DetailsRequest>(new {InvoiceNbr = 123});
            
           // Assert.True(await harness.Consumed.Any<DetailsRequest>());
            
            publisherMock.Verify(x=>x.Publish(It.IsAny<DetailsRequest>()), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }
    [Fact]
    public async Task Test1()
    {
        var publisherMock = new Mock<IPublishMessage>();
        
        await using var provider = new ServiceCollection()
            .AddMassTransitInMemoryTestHarness(cfg =>
            {
                cfg.AddConsumer<MyConsumer>();
            })
            .AddScoped<IPublishMessage>(x=> publisherMock.Object)
            .BuildServiceProvider(true);
        
        var harness = provider.GetRequiredService<InMemoryTestHarness>();
        
        await harness.Start();
        
        try
        {
          
            await harness.Bus.Publish<DetailsRequest>(new {InvoiceNbr = 123});
            
            Assert.True(await harness.Consumed.Any<DetailsRequest>());
            
            publisherMock.Verify(x=>x.Publish(It.IsAny<DetailsRequest>()), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }
}

public class MassTransitFixture : IAsyncLifetime
{
    public InMemoryTestHarness Harness { get; private set; } = null!;
    public Mock<IPublishMessage> PublisherMock { get; } = new();
    
    public async Task InitializeAsync()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitInMemoryTestHarness(cfg =>
            {
                cfg.AddConsumer<MyConsumer>();
            })
            .AddScoped<IPublishMessage>(x=> PublisherMock.Object)
            .BuildServiceProvider(true);
        
        Harness = provider.GetRequiredService<InMemoryTestHarness>();
        
      //  await Harness.Start();
    }

    public Task DisposeAsync()
    {
        return Harness.Stop();
    }
}

[CollectionDefinition("MassTransit")]
public class MassTransitTestCollection : ICollectionFixture<MassTransitFixture>
{
    
}
public class MyConsumer : IConsumer<DetailsRequest>
{
    private readonly IPublishMessage publishMessage;

    public MyConsumer(IPublishMessage publishMessage)
    {
        this.publishMessage = publishMessage;
    }
    public async Task Consume(ConsumeContext<DetailsRequest> context)
    {
        await publishMessage.Publish(context.Message);
    }
}

public class DetailsRequest
{
    public int InvoiceNbr { get; set; }
}

public interface IPublishMessage
{
    Task Publish<T>(T message);
}

