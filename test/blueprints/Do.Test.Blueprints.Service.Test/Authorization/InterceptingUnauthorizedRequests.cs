﻿using Do.Architecture;
using Do.Authorization;
using System.Net;
using System.Net.Http.Headers;

namespace Do.Test.Authorization;

public class InterceptingUnauthorizedRequests : TestServiceNfr
{
    protected override Func<AuthorizationConfigurator, IFeature<AuthorizationConfigurator>>? Authorization =>
        c => c.ClaimBased(policies:
        [
            new("Default", policy => policy.RequireClaim("Token")),
            new("AdminOnly", policy => policy.RequireClaim("Token", "Admin_Token"))
        ]);

    [Test]
    public async Task Returns_unauthorized_access_response_for_not_authenticated_user()
    {
        var response = await Client.PostAsync("authorization-samples/require-authorization", null);

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Returns_unauthorized_access_response_for_failed_authenticatation()
    {
        Client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse("Wrong_token");

        var response = await Client.PostAsync("authorization-samples/require-authorization", null);

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Returns_forbidden_response_when_policy_requirements__are_not_met()
    {
        Client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse("11111111111111111111111111111111");

        var response = await Client.PostAsync("authorization-samples/require-admin-policy", null);

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}