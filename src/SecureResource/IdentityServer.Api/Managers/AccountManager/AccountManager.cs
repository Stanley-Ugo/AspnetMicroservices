using IdentityServer.Api.Common;
using IdentityServer.Api.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Api.Managers.AccountManager
{
    public class AccountManager : IAccountManager
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IClientStore _clientStore;
        public AccountManager(IIdentityServerInteractionService interaction,
            IAuthenticationSchemeProvider schemeProvider,IClientStore clientStore)
        {
            _interaction = interaction;
            _schemeProvider = schemeProvider;
            _clientStore = clientStore;
        }

        public async Task<StandardResponse<LoginViewModel>> GetLoginProviders(string returnUrl)
        {
            var response = await BuildLoginViewModelAsync(returnUrl);
            var vm = response.Data;

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return StandardResponse<LoginViewModel>.GenerateResponse(code: "200", message: "Challenge External", data: vm, status: true);
            }

            return StandardResponse<LoginViewModel>.GenerateResponse(code: "200", data: vm, status: true);
        }
        private async Task<StandardResponse<LoginViewModel>> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return StandardResponse<LoginViewModel>.GenerateResponse(code: "200", data: vm, status: true);
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            var response = new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };

            return StandardResponse<LoginViewModel>.GenerateResponse(code: "200", data: response, status: true);
        }
    }
}
