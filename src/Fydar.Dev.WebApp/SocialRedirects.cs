using Fydar.AspNetCore.Socials;

namespace Fydar.Dev.WebApp;

internal static class SocialRedirects
{
    public static void MapSocialRedirects(
        this IEndpointRouteBuilder app)
    {
        // GitHub
        app.MapSocialRedirect("/github", new()
        {
            Destination = "https://github.com/Fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on GitHub");
                factory.UseDescription("Explore my open-source projects, code repositories, and contributions to the .NET ecosystem.");
            }
        });

        // LinkedIn
        app.MapSocialRedirect("/linkedin", new()
        {
            Destination = "https://www.linkedin.com/in/fydar/",
            Metadata = factory =>
            {
                factory.UseTitle("Connect with Fydar on LinkedIn");
                factory.UseDescription("Professional profile, project history, and technical insights.");
            }
        });

        // YouTube
        app.MapSocialRedirect("/youtube", new()
        {
            Destination = "https://youtube.com/@fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on YouTube");
                factory.UseDescription("Technical tutorials, project demos, and deep dives into modern software engineering.");
            }
        });

        // Discord
        app.MapSocialRedirect("/discord", new()
        {
            Destination = "https://discord.com/users/172972361954492416",
            Metadata = factory =>
            {
                factory.UseTitle("Connect with Fydar on Discord");
                factory.UseDescription("Let's connect.");
            }
        });

        // Gravatar
        app.MapSocialRedirect("/gravatar", new()
        {
            Destination = "https://gravatar.com/fydardev",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Global Profile");
                factory.UseDescription("The centralized identity and avatar used across the web.");
            }
        });

        // Reddit
        app.MapSocialRedirect("/reddit", new()
        {
            Destination = "https://www.reddit.com/user/Fydarus/",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on Reddit");
                factory.UseDescription("Engaging in communities focused on software engineering and indie development.");
            }
        });

        // Instagram
        app.MapSocialRedirect("/instagram", new()
        {
            Destination = "https://www.instagram.com/fydar.dev/",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on Instagram");
                factory.UseDescription("Personal Instagram.");
            }
        });

        // Threads
        app.MapSocialRedirect("/threads", new()
        {
            Destination = "https://www.threads.com/@fydar.dev",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on Threads");
                factory.UseDescription("Text-based updates and community engagement from the Meta ecosystem.");
            }
        });

        // BlueSky
        app.MapSocialRedirect("/bluesky", new()
        {
            Destination = "https://bsky.app/profile/fydar.bsky.social",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on BlueSky");
                factory.UseDescription("Decentralized social media updates on tech, code, and life.");
            }
        });

        // Twitter
        app.MapSocialRedirect("/twitter", new()
        {
            Destination = "https://twitter.com/Fydarus",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on Twitter");
                factory.UseDescription("Technical insights, .NET updates, and software development thoughts in 280 characters or less.");
            }
        });

        // Spotify
        app.MapSocialRedirect("/spotify", new()
        {
            Destination = "https://open.spotify.com/user/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Playlists");
                factory.UseDescription("Curation of music for focused deep-work and development sessions.");
            }
        });

        // Steam
        app.MapSocialRedirect("/steam", new()
        {
            Destination = "https://steamcommunity.com/id/Fydar/",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on Steam");
                factory.UseDescription("Reviewing games, tracking achievements, and connecting to play.");
            }
        });

        // CurseForge
        app.MapSocialRedirect("/curseforge", new()
        {
            Destination = "https://www.curseforge.com/members/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Mods on CurseForge");
                factory.UseDescription("Downloadable extensions and modifications for popular gaming titles.");
            }
        });

        // Modrinth
        app.MapSocialRedirect("/modrinth", new()
        {
            Destination = "https://modrinth.com/user/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on Modrinth");
                factory.UseDescription("A collection of open-source and high-performance game modifications.");
            }
        });

        // TikTok
        app.MapSocialRedirect("/tiktok", new()
        {
            Destination = "https://tiktok.com/@fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on TikTok");
                factory.UseDescription("Short-form dev logs and rapid-fire technical tips.");
            }
        });

        // Docker
        app.MapSocialRedirect("/docker", new()
        {
            Destination = "https://hub.docker.com/u/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Docker Hub");
                factory.UseDescription("Containerized applications and optimized images for .NET deployments.");
            }
        });

        // Notion
        app.MapSocialRedirect("/notion", new()
        {
            Destination = "https://notion.so/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Knowledge Base");
                factory.UseDescription("Public documentation, roadmaps, and project management workspaces.");
            }
        });

        // Twitch
        app.MapSocialRedirect("/twitch", new()
        {
            Destination = "https://twitch.tv/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar Live on Twitch");
                factory.UseDescription("Join my live coding sessions to see real-time software builds and C# development.");
            }
        });

        // Itch.IO
        app.MapSocialRedirect("/itch", new()
        {
            Destination = "https://fydar.itch.io",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Games & Tools");
                factory.UseDescription("Browse my indie game projects, experimental software, and digital tools on Itch.io.");
            }
        });

        // Unity Asset Store
        app.MapSocialRedirect("/unity", new()
        {
            Destination = "https://assetstore.unity.com/publishers/13236",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Unity Assets");
                factory.UseDescription("High-quality scripts and tools designed to accelerate your game development workflow.");
            }
        });

        // SketchFab
        app.MapSocialRedirect("/sketchfab", new()
        {
            Destination = "https://sketchfab.com/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's 3D Gallery");
                factory.UseDescription("Interactive 3D models, assets, and environmental art for game development.");
            }
        });

        // ArtStation
        app.MapSocialRedirect("/artstation", new()
        {
            Destination = "https://www.artstation.com/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Portfolio on ArtStation");
                factory.UseDescription("Visual design, digital art, and creative direction projects.");
            }
        });

        // OpenCollective
        app.MapSocialRedirect("/opencollective", new()
        {
            Destination = "https://opencollective.com/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on OpenCollective");
                factory.UseDescription("Helping to sustain open-source development and community-driven software projects.");
            }
        });

        // Stack Overflow
        app.MapSocialRedirect("/stackoverflow", new()
        {
            Destination = "https://stackoverflow.com/users/9726948/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's StackOverflow Profile");
                factory.UseDescription("Review my technical contributions, answers, and reputation within the StackOverflow community.");
            }
        });

        // GitLab
        app.MapSocialRedirect("/gitlab", new()
        {
            Destination = "https://gitlab.com/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar on GitLab");
                factory.UseDescription("DevOps, CI/CD pipelines, and private repository management.");
            }
        });

        // Trello
        app.MapSocialRedirect("/trello", new()
        {
            Destination = "https://trello.com/fydar",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Project Boards on Trello");
                factory.UseDescription("Transparent task tracking and development workflow management.");
            }
        });

        // XBOX
        app.MapSocialRedirect("/xbox", new()
        {
            Destination = "https://www.xbox.com/en-GB/play/user/Fydar6121",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Xbox Profile");
                factory.UseDescription("Gaming activity and achievements across the Microsoft ecosystem.");
            }
        });

        // Zoom
        app.MapSocialRedirect("/zoom", new()
        {
            Destination = "https://community.zoom.com/t5/user/viewprofilepage/user-id/771284",
            Metadata = factory =>
            {
                factory.UseTitle("Fydar's Zoom Room");
                factory.UseDescription("Personal meeting space for technical consultations and remote collaboration.");
            }
        });

        // Atlassian / Community
        // app.MapSocialRedirect("/atlassian", new()
        // {
        // 	Destination = "https://community.atlassian.com/t5/user/viewprofilepage/user-id/your-id",
        // 	Metadata = factory =>
        // 	{
        // 		factory.UseTitle("Fydar on Atlassian Community");
        // 		factory.UseDescription("Insights and discussions on Jira, Confluence, and team collaboration tools.");
        // 	}
        // });
        //
        // // Meta Stack Overflow
        // app.MapSocialRedirect("/metastackoverflow", new()
        // {
        // 	Destination = "https://meta.stackoverflow.com/users/9726948/fydar",
        // 	Metadata = factory =>
        // 	{
        // 		factory.UseTitle("Fydar on Meta Stack Overflow");
        // 		factory.UseDescription("Participating in discussions regarding the future of the developer community.");
        // 	}
        // });
        //
        // // Stack Exchange
        // app.MapSocialRedirect("/stackexchange", new()
        // {
        // 	Destination = "https://stackexchange.com/users/13481916/fydar",
        // 	Metadata = factory =>
        // 	{
        // 		factory.UseTitle("Fydar's StackExchange Profile");
        // 		factory.UseDescription("Review my technical contributions, answers, and reputation within the StackExchange community.");
        // 	}
        // });
        //
        // // Game Dev Stack Exchange
        // app.MapSocialRedirect("/gamedevstackexchange", new()
        // {
        // 	Destination = "https://gamedev.stackexchange.com/users/142990/fydar",
        // 	Metadata = factory =>
        // 	{
        // 		factory.UseTitle("Fydar's Game Development StackExchange Profile");
        // 		factory.UseDescription("Review my technical contributions, answers, and reputation within the StackExchange community.");
        // 	}
        // });
        //
        // // Unity Discussions
        // app.MapSocialRedirect("/unitydiscussions", new()
        // {
        // 	Destination = "https://discussions.unity.com/u/fydar",
        // 	Metadata = factory =>
        // 	{
        // 		factory.UseTitle("Fydar on the Unity Community");
        // 		factory.UseDescription("Active member of the Unity engine discussion forums and technical troubleshooting.");
        // 	}
        // });
    }
}
