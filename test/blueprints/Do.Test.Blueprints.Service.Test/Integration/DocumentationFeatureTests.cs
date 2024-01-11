﻿namespace Do.Test.Integration;

public class DocumentationFeatureTests : IntegrationSpec<DocumentationFeatureTests>
{
    public override void Run()
    {
        Forge.New
            .Service(
                business: c => c.Default(),
                database: c => c.MySql().ForDevelopment(c.Sqlite()),
                exceptionHandling: ex => ex.Default(typeUrlFormat: "https://do.mouseless.codes/errors/{0}"),
                configure: app => app.Features.AddConfigurationOverrider()
            )
            .Run();
    }

    [Test]
    public async Task Application_root_is_swagger_index_page()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        response.RequestMessage.ShouldNotBeNull();
        response.RequestMessage.RequestUri.ShouldBe("http://localhost/swagger/index.html");
    }
}
