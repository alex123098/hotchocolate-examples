using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace StarWars
{
    public class JwtAuthenticator
    {
        private readonly JwtBearerOptions options;
        private OpenIdConnectConfiguration? openIdConnectConfiguration;
        private readonly JwtSecurityTokenHandler tokenHandler;

        public JwtAuthenticator(IOptionsMonitor<JwtBearerOptions> options)
        {
            this.options = options.Get(JwtBearerDefaults.AuthenticationScheme);
            tokenHandler = new JwtSecurityTokenHandler();
        }

        public async Task<ClaimsPrincipal> Authenticate(string token, CancellationToken cancellationToken)
        {
            var validationParameters = await GetValidationParameters(cancellationToken);
            var tokenToValidate = token?.Replace("Bearer ", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            return tokenHandler.ValidateToken(tokenToValidate, validationParameters, out _);
        }

        private async ValueTask<TokenValidationParameters> GetValidationParameters(CancellationToken cancellationToken)
        {
            var parameters = options.TokenValidationParameters.Clone();
            var openIdConfig = await LoadOpenIdConfiguration(cancellationToken);
            if (openIdConfig != null)
            {
                var issuers = new[] { openIdConfig.Issuer };
                parameters.ValidIssuers = parameters.ValidIssuers?.Concat(issuers) ?? issuers;
                parameters.IssuerSigningKeys = 
                    parameters.IssuerSigningKeys?.Concat(openIdConfig.SigningKeys)
                    ?? openIdConfig.SigningKeys;
            }

            return parameters;
        }
        
        private async ValueTask<OpenIdConnectConfiguration> LoadOpenIdConfiguration(CancellationToken cancellationToken)
        {
            if (openIdConnectConfiguration == null)
            {
                openIdConnectConfiguration = await options.ConfigurationManager.GetConfigurationAsync(cancellationToken);
            }

            return openIdConnectConfiguration;
        }
    }
}