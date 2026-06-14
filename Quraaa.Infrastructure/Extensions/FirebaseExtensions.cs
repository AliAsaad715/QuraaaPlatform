using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Quraaa.Infrastructure.Extensions
{
    public static class FirebaseExtensions
    {
        public static IServiceCollection AddFirebaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            if (FirebaseApp.DefaultInstance is not null)
            {
                return services;
            }

            var credential = CreateCredential(configuration);

            FirebaseApp.Create(new AppOptions
            {
                Credential = credential
            });

            return services;
        }

        private static GoogleCredential CreateCredential(IConfiguration configuration)
        {
            var credentialPath = configuration["Firebase:CredentialsPath"];

            if (!string.IsNullOrWhiteSpace(credentialPath))
            {
                var fullPath = Path.IsPathRooted(credentialPath)
                    ? credentialPath
                    : Path.GetFullPath(credentialPath);

                if (!File.Exists(fullPath))
                {
                    throw new InvalidOperationException($"Firebase credentials file was not found at '{fullPath}'.");
                }

                using var stream = File.OpenRead(fullPath);
                return CredentialFactory.FromStream<ServiceAccountCredential>(stream).ToGoogleCredential();
            }

            try
            {
                return GoogleCredential.GetApplicationDefault();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Firebase credentials are missing. Configure Firebase:CredentialsPath or application default credentials.",
                    ex);
            }
        }

    }
}
