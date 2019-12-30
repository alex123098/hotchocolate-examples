using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Playground;
using HotChocolate.Server;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using StarWars.Characters;
using StarWars.Repositories;
using StarWars.Reviews;

namespace StarWars
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Add the custom services like repositories etc ...
            services.AddSingleton<ICharacterRepository, CharacterRepository>();
            services.AddSingleton<IReviewRepository, ReviewRepository>();
            services.AddSingleton<JwtAuthenticator>();
            services.AddWebSocketConnectionInterceptor(async (ctx, props, ct) =>
            {
                var tokenValue = props.GetValueOrDefault(HeaderNames.Authorization);
                if (!(tokenValue is string token))
                {
                    return ConnectionStatus.Reject();
                }
                var authenticator = ctx.RequestServices.GetRequiredService<JwtAuthenticator>();
                try
                {
                    var principal = await authenticator.Authenticate(token, ctx.RequestAborted);
                    ctx.User = principal;
                    return ConnectionStatus.Accept();
                }
                catch (Exception e)
                {
                    return ConnectionStatus.Reject(e.Message);
                }
            });
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = "authority here";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuers = new []
                        {
                            "valid issuer here",
                        },
                        ValidAudiences = new[]
                        {
                            "audience here"
                        },
                    };
                });
                
            // Add in-memory event provider
            services.AddInMemorySubscriptionProvider();

            // Add GraphQL Services
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddAuthorizeDirectiveType()
                .AddServices(sp)
                .AddQueryType(d => d.Name("Query"))
                .AddMutationType(d => d.Name("Mutation"))
                .AddSubscriptionType(d => d.Name("Subscription"))
                .AddType<CharacterQueries>()
                .AddType<ReviewQueries>()
                .AddType<ReviewMutations>()
                .AddType<ReviewSubscriptions>()
                .AddType<Human>()
                .AddType<Droid>()
                .AddType<Starship>()
                .Create());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseRouting()
                .UseAuthentication()
                .UseWebSockets()
                .UseGraphQL("/graphql")
                .UsePlayground(new PlaygroundOptions
                {
                    Path = "/playground",
                    QueryPath = "/graphql",
                    EnableSubscription = true,
                    SubscriptionPath = "/graphql"
                });
        }
    }
}
