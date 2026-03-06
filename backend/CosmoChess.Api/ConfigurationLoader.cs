using CosmoChess.Domain;

namespace CosmoChess.Api
{
    public static class ConfigurationLoader
    {
        public static WebApplicationBuilder SetupConfiguration(this WebApplicationBuilder builder)
        {
            if (builder.Environment.IsDevelopment())
            {
                builder.Configuration.AddUserSecrets<Program>();
            }

            builder.Services.AddAppConfiguration(builder.Configuration);

            return builder;
        }
    }
}
