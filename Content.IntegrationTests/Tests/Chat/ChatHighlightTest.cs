#nullable enable
using System; // SD
using System.Collections.Generic;
using System.Linq; // SD
using System.Reflection;
using System.Text.RegularExpressions; // SD
using System.Threading.Tasks;
using Content.Client.CharacterInfo;
using Content.Client.UserInterface.Systems.Chat;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Localization; // SD
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chat;

public sealed class ChatHighlightTest : GameTest
{
    [SidedDependency(Side.Client)] private readonly IConfigurationManager _configManager = null!;
    [SidedDependency(Side.Client)] private readonly IUserInterfaceManager _uiManager = null!;
    [SidedDependency(Side.Client)] private readonly ILocalizationManager _localization = null!; // SD edit
    private static readonly ProtoId<JobPrototype> Captain = "Captain";

    [Test]
    [RunOnSide(Side.Client)]
    public async Task TestCustomHighlightsPreserved()
    {
        var chatController = _uiManager.GetUIController<ChatUIController>();

        // 1. Enable auto-fill highlights
        _configManager.SetCVar(CCVars.ChatAutoFillHighlights, true);

        // 2. Set custom highlights
        var customHighlights = "ling\nrev";
        chatController.UpdateHighlights(customHighlights);

        // Verify they are saved
        Assert.That(_configManager.GetCVar(CCVars.ChatHighlights), Is.EqualTo(customHighlights));

        // 3. Simulate character update
        var characterData = new CharacterInfoSystem.CharacterData(
            default,
            new Dictionary<string, List<Shared.Objectives.ObjectiveInfo>>(),
            null,
            Captain,
            "John Doe"
        );

        var method = chatController.GetType().GetMethod(
            "OnCharacterUpdated",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        Assert.That(method, Is.Not.Null);

        // Set internal state to allow character update processing
        var attachField = chatController.GetType().GetField(
            "_charInfoIsAttach",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        Assert.That(attachField, Is.Not.Null);
        attachField.SetValue(chatController, true);

        // Invoke update
        method.Invoke(chatController, new object[] { characterData });

        // 4. Assertions:
        // - Custom highlights in config must remain unchanged
        Assert.That(_configManager.GetCVar(CCVars.ChatHighlights), Is.EqualTo(customHighlights));

        // - Internal active regex highlights must contain both custom & auto-filled highlights
        var highlightsField = chatController.GetType().GetField(
            "_highlights",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        Assert.That(highlightsField, Is.Not.Null);
        var activeHighlights = (List<string>)highlightsField.GetValue(chatController)!;

        // Check that custom and auto highlights are loaded
        // Custom:
        Assert.That(activeHighlights, Contains.Item("ling"));
        Assert.That(activeHighlights, Contains.Item("rev"));
        AssertContainsJobAutoHighlights(activeHighlights); // SD edit

        // 5. Disable auto-fill highlights and verify auto-filled highlights are removed
        _configManager.SetCVar(CCVars.ChatAutoFillHighlights, false);

        activeHighlights = (List<string>)highlightsField.GetValue(chatController)!;
        Assert.That(activeHighlights, Contains.Item("ling"));
        Assert.That(activeHighlights, Contains.Item("rev"));
        AssertDoesNotContainJobAutoHighlights(activeHighlights); // SD
    }

    [Test]
    [RunOnSide(Side.Client)]
    public async Task TestEnablingAutoFillPreservesCustomHighlights()
    {
        var chatController = _uiManager.GetUIController<ChatUIController>();

        // 1. Start with auto-fill disabled
        _configManager.SetCVar(CCVars.ChatAutoFillHighlights, false);

        // 2. Set custom highlights
        var customHighlights = "ling\nrev";
        chatController.UpdateHighlights(customHighlights);

        // Verify active matches are ONLY custom highlights
        var highlightsField = chatController.GetType().GetField(
            "_highlights",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        Assert.That(highlightsField, Is.Not.Null);
        var activeHighlights = (List<string>)highlightsField.GetValue(chatController)!;

        Assert.That(activeHighlights, Contains.Item("ling"));
        Assert.That(activeHighlights, Contains.Item("rev"));
        Assert.That(activeHighlights.Count, Is.EqualTo(2));

        // 3. Enable auto-fill highlights
        _configManager.SetCVar(CCVars.ChatAutoFillHighlights, true);

        // 4. Simulate character update (spawning into round)
        var characterData = new CharacterInfoSystem.CharacterData(
            default,
            new Dictionary<string, List<Shared.Objectives.ObjectiveInfo>>(),
            null,
            Captain,
            "John Doe"
        );

        var method = chatController.GetType().GetMethod(
            "OnCharacterUpdated",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        Assert.That(method, Is.Not.Null);

        var attachField = chatController.GetType().GetField(
            "_charInfoIsAttach",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        Assert.That(attachField, Is.Not.Null);
        attachField.SetValue(chatController, true);

        // Invoke character update
        method.Invoke(chatController, new object[] { characterData });

        // 5. Assertions:
        // - Config highlights MUST NOT be wiped and remain as custom highlights
        Assert.That(_configManager.GetCVar(CCVars.ChatHighlights), Is.EqualTo(customHighlights));

        // - Active highlights list must now merge both custom and auto-filled ones
        activeHighlights = (List<string>)highlightsField.GetValue(chatController)!;
        Assert.That(activeHighlights, Contains.Item("ling"));
        Assert.That(activeHighlights, Contains.Item("rev"));

        AssertContainsJobAutoHighlights(activeHighlights); // SD
    }

    // SD edit start

    private static readonly Regex StartDoubleQuote = new("\"$");
    private static readonly Regex EndDoubleQuote = new("^\"|(?<=^@)\"");

    /// <summary>
    /// Asserts that captain job auto-fill keywords from the active culture are present.
    /// Mirrors the keyword processing in <c>ChatUIController.ReloadHighlights</c>.
    /// </summary>
    private void AssertContainsJobAutoHighlights(List<string> activeHighlights)
    {
        Assert.That(_localization.TryGetString("highlights-captain", out var jobMatches), Is.True);
        var keywords = jobMatches.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.That(keywords, Is.Not.Empty);

        var unquoted = keywords.FirstOrDefault(k => !k.Contains('"'));
        var quoted = keywords.FirstOrDefault(k => k.Contains('"'));

        Assert.That(unquoted, Is.Not.Null);
        Assert.That(activeHighlights, Contains.Item(Regex.Escape(unquoted!)));

        if (quoted is not null)
        {
            var keyword = Regex.Escape(quoted);
            keyword = StartDoubleQuote.Replace(keyword, "(?!\\w)");
            keyword = EndDoubleQuote.Replace(keyword, "(?<!\\w)");
            Assert.That(activeHighlights, Contains.Item(keyword));
        }
    }

    private void AssertDoesNotContainJobAutoHighlights(List<string> activeHighlights)
    {
        Assert.That(_localization.TryGetString("highlights-captain", out var jobMatches), Is.True);
        var unquoted = jobMatches
            .Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(k => !k.Contains('"'));

        Assert.That(unquoted, Is.Not.Null);
        Assert.That(activeHighlights, Is.Not.Contains(Regex.Escape(unquoted!)));
    // SD edit end
    }
}
