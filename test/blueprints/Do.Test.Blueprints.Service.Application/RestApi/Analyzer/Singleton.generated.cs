// This file will be auto-generated

using Microsoft.AspNetCore.Mvc;

namespace Do.Test;

[ApiController]
public class SingletonController
{
    readonly IServiceProvider _serviceProvider;

    public SingletonController(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    [HttpGet]
    [Produces("application/json")]
    [Route("singleton/time")]
    public DateTime GetNow()
    {
        var target = _serviceProvider.GetRequiredService<Singleton>();

        return target.GetNow();
    }

    [HttpPost]
    [Produces("application/json")]
    [Route("singleton/test-transaction-action")]
    public async Task TestTransactionAction()
    {
        var target = _serviceProvider.GetRequiredService<Singleton>();

        await target.TestTransactionAction();
    }

    [HttpPost]
    [Produces("application/json")]
    [Route("singleton/test-transaction-func")]
    public async Task TestTransactionFunc()
    {
        var target = _serviceProvider.GetRequiredService<Singleton>();

        await target.TestTransactionFunc();
    }

    [HttpPost]
    [Produces("application/json")]
    [Route("singleton/test-exception")]
    public void TestException(bool handled)
    {
        var target = _serviceProvider.GetRequiredService<Singleton>();

        target.TestException(handled);
    }
}
