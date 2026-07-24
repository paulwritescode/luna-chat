using LunaChat.Models;

namespace LunaChat.Services;

/// <summary>
/// Built-in connector catalogue. Each connector authenticates with a manually
/// supplied token/key (stored in the OS vault). Brand colors mirror OpenWorker's
/// redesign.html connector tints.
/// </summary>
public static class ConnectorRegistry
{
    private static ProviderField Token(string label, string placeholder, string help) => new()
    {
        Key = "token", Label = label, Secret = true, Placeholder = placeholder, Help = help
    };

    private static ProviderField Text(string key, string label, string placeholder) => new()
    {
        Key = key, Label = label, Placeholder = placeholder
    };

    public static readonly IReadOnlyList<ConnectorDef> All = new List<ConnectorDef>
    {
        new()
        {
            Id = "slack", Title = "Slack", Description = "Messages, channels, DMs", TwoWay = true,
            BrandColor = "#611F69", BrandSoft = "#F4ECF5",
            HelpUrl = "https://api.slack.com/apps", HelpLabel = "api.slack.com",
            Fields = { Token("Bot token", "xoxb-…", "Create a Slack app and copy its Bot User OAuth token.") }
        },
        new()
        {
            Id = "github", Title = "GitHub", Description = "Repos, issues, pull requests",
            BrandColor = "#1F2328", BrandSoft = "#EEF0F2",
            HelpUrl = "https://github.com/settings/tokens", HelpLabel = "github.com/settings/tokens",
            Fields = { Token("Personal access token", "ghp_…", "A fine-grained or classic PAT with the scopes you need.") }
        },
        new()
        {
            Id = "notion", Title = "Notion", Description = "Pages, databases",
            BrandColor = "#111111", BrandSoft = "#EEEEEE",
            HelpUrl = "https://www.notion.so/my-integrations", HelpLabel = "notion.so/my-integrations",
            Fields = { Token("Integration token", "secret_…", "Create an internal integration and share pages with it.") }
        },
        new()
        {
            Id = "linear", Title = "Linear", Description = "Issues, projects, cycles",
            BrandColor = "#5E6AD2", BrandSoft = "#ECEEFB",
            HelpUrl = "https://linear.app/settings/api", HelpLabel = "linear.app/settings/api",
            Fields = { Token("API key", "lin_api_…", "Personal API key from Linear settings.") }
        },
        new()
        {
            Id = "atlassian", Title = "Jira", Description = "Issues, boards, sprints",
            BrandColor = "#0052CC", BrandSoft = "#E6EEFB",
            HelpUrl = "https://id.atlassian.com/manage-profile/security/api-tokens", HelpLabel = "id.atlassian.com",
            Fields =
            {
                Text("base_url", "Site URL", "https://your-org.atlassian.net"),
                Text("email", "Account email", "you@example.com"),
                Token("API token", "…", "Create an Atlassian API token for your account.")
            }
        },
        new()
        {
            Id = "hubspot", Title = "HubSpot", Description = "CRM, tickets, contacts",
            BrandColor = "#FF7A59", BrandSoft = "#FFF0EC",
            HelpUrl = "https://developers.hubspot.com/docs/api/private-apps", HelpLabel = "developers.hubspot.com",
            Fields = { Token("Private app token", "pat-…", "Create a private app and copy its access token.") }
        },
        new()
        {
            Id = "gmail", Title = "Gmail", Description = "Read and send email",
            BrandColor = "#EA4335", BrandSoft = "#FDECEA",
            HelpUrl = "https://developers.google.com/gmail/api", HelpLabel = "developers.google.com",
            Fields = { Token("OAuth access token", "ya29.…", "Paste an OAuth access token scoped for Gmail.") }
        },
        new()
        {
            Id = "gcal", Title = "Google Calendar", Description = "Events, scheduling",
            BrandColor = "#4285F4", BrandSoft = "#E8F0FE",
            HelpUrl = "https://developers.google.com/calendar", HelpLabel = "developers.google.com",
            Fields = { Token("OAuth access token", "ya29.…", "Paste an OAuth access token scoped for Calendar.") }
        },
        new()
        {
            Id = "telegram", Title = "Telegram", Description = "Two-way messaging", TwoWay = true,
            BrandColor = "#229ED9", BrandSoft = "#E6F4FB",
            HelpUrl = "https://core.telegram.org/bots#botfather", HelpLabel = "core.telegram.org",
            Fields = { Token("Bot token", "123456:ABC-…", "Create a bot with @BotFather and copy its token.") }
        },
        new()
        {
            Id = "salesforce", Title = "Salesforce", Description = "Cases, records, comments",
            BrandColor = "#00A1E0", BrandSoft = "#E6F6FD",
            HelpUrl = "https://help.salesforce.com", HelpLabel = "help.salesforce.com",
            Fields =
            {
                Text("base_url", "Instance URL", "https://your-org.my.salesforce.com"),
                Token("Access token", "…", "An OAuth access token for your Salesforce instance.")
            }
        },
    };

    public static ConnectorDef? Find(string id) => All.FirstOrDefault(c => c.Id == id);
}
