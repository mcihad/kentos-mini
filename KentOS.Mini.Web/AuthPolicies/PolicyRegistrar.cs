using KentOS.Mini.Application.Identity;
using System.Security.Claims;

namespace KentOS.Mini.Web.AuthPolicies
{
    public static class PolicyRegistrar
    {
        /**
         * This method is used to add default policies to the application.
         * Add Baskan and Admin role to all policies
         * @param services The service collection to add the policies to.
         */
        public static void AddDefaultPolicies(this IServiceCollection services)
        {
            services.AddAuthorizationBuilder()
                .AddPolicy(AuthPolicyNames.OzelKalem, policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        return context.User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == UserRoles.Admin || c.Value == UserRoles.Sekreter || c.Value == UserRoles.Yonetici || c.Value == UserRoles.Baskan || c.Value == UserRoles.Cicek || c.Value==UserRoles.Medya));
                    });
                })
                .AddPolicy(AuthPolicyNames.Ajanda, policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        return context.User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == UserRoles.Admin || c.Value == UserRoles.Sekreter || c.Value == UserRoles.Yonetici || c.Value==UserRoles.Baskan));
                    });
                })
                .AddPolicy(AuthPolicyNames.Sibeski, policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        return context.User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == UserRoles.Admin || c.Value == UserRoles.Baskan || c.Value == UserRoles.Sibeski));
                    });
                })
                .AddPolicy(AuthPolicyNames.Medya, policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        return context.User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == UserRoles.Admin || c.Value == UserRoles.Baskan || c.Value == UserRoles.Medya));
                    });
                }).AddPolicy(AuthPolicyNames.Cicek, policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        return context.User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == UserRoles.Admin || c.Value == UserRoles.Baskan || c.Value == UserRoles.Cicek));
                    });
                });
        }
    }
}
