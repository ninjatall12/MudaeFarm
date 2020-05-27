﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octokit;

namespace MudaeFarm
{
    public static class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                   .ConfigureLogging(logger =>
                    {
                        if (args.Contains("-v") || args.Contains("--verbose"))
                            logger.SetMinimumLevel(LogLevel.Trace);

                        logger.AddFile($"log_{DateTime.Now.ToString("u").Replace(':', '.')}.txt");
                    })
                   .ConfigureAppConfiguration(config => config.Add(new DiscordConfigurationSource()))
                   .ConfigureServices((builder, services) =>
                    {
                        // configuration
                        services.AddSingleton((IConfigurationRoot) builder.Configuration)
                                .Configure<GeneralOptions>(builder.Configuration.GetSection(GeneralOptions.Section))
                                .Configure<ClaimingOptions>(builder.Configuration.GetSection(ClaimingOptions.Section))
                                .Configure<RollingOptions>(builder.Configuration.GetSection(RollingOptions.Section))
                                .Configure<CharacterWishlist>(builder.Configuration.GetSection(CharacterWishlist.Section))
                                .Configure<AnimeWishlist>(builder.Configuration.GetSection(AnimeWishlist.Section))
                                .Configure<BotChannelList>(builder.Configuration.GetSection(BotChannelList.Section))
                                .Configure<ClaimReplyList>(builder.Configuration.GetSection(ClaimReplyList.Section))
                                .Configure<UserWishlistList>(builder.Configuration.GetSection(UserWishlistList.Section));

                        // discord client
                        services.AddSingleton<IDiscordClientService, DiscordClientService>()
                                .AddTransient<IHostedService>(s => s.GetService<IDiscordClientService>());

                        services.AddSingleton<ICredentialManager, CredentialManager>();

                        // mudae services
                        services.AddSingleton<IMudaeUserFilter, MudaeUserFilter>()
                                .AddSingleton<IMudaeCommandHandler, MudaeCommandHandler>()
                                .AddSingleton<IMudaeOutputParser, EnglishMudaeOutputParser>(); //todo: this should be configurable

                        services.AddSingleton<IMudaeRoller, MudaeRoller>()
                                .AddTransient<IHostedService>(s => s.GetService<IMudaeRoller>());

                        // auto updater
                        services.AddHostedService<Updater>()
                                .AddSingleton<HttpClient>()
                                .AddSingleton(s => new GitHubClient(new ProductHeaderValue("MudaeFarm")));
                    });
    }
}