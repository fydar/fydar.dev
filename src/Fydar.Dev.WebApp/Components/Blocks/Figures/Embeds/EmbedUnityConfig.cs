namespace Fydar.Dev.WebApp.Components.Blocks.Figures.Embeds;

/// <summary>
/// Configuration settings used to initialize and embed a Unity WebGL build into a web page.
/// </summary>
public class EmbedUnityConfig
{
    /// <summary>
    /// The name of the developer or organization behind the game.
    /// </summary>
    public required string CompanyName { get; set; }

    /// <summary>
    /// The display name of the game product.
    /// </summary>
    public required string ProductName { get; set; }

    /// <summary>
    /// The version of the product, used for cache busting or identifying the build.
    /// </summary>
    public required Version ProductVersion { get; set; }

    /// <summary>
    /// The URL of the <c>.loader.js</c> file, which contains the Unity WebGL loading script.
    /// </summary>
    public required string LoaderUrl { get; set; }

    /// <summary>
    /// The URL of the JavaScript framework file (<c>.framework.js.br</c>), containing the runtime environment.
    /// </summary>
    public required string FrameworkUrl { get; set; }

    /// <summary>
    /// The URL of the WebAssembly code file (<c>.wasm.br</c>), containing the compiled game logic.
    /// </summary>
    public required string CodeUrl { get; set; }

    /// <summary>
    /// The URL of the binary data file (<c>.data.br</c>), containing the game assets and scenes.
    /// </summary>
    public required string DataUrl { get; set; }

    /// <summary>
    /// The base URL for the <c>StreamingAssets</c> folder.
    /// </summary>
    /// <remarks>
    /// This property maps directly to <c>Application.streamingAssetsPath</c> within the Unity runtime.
    /// </remarks>
    public required string StreamingAssetsUrl { get; set; }

    /// <summary>
    /// The URL of the debug symbols file (<c>.symbols.json.br</c>), used for stack trace de-obfuscation.
    /// </summary>
    public string SymbolsUrl { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the memory initialization file (<c>.mem</c> or <c>.data.br</c>), used to pre-allocate heap memory.
    /// </summary>
    public string MemoryUrl { get; set; } = string.Empty;

    /// <summary>
    /// A collection of command-line arguments passed to the game instance on startup.
    /// </summary>
    /// <remarks>
    /// These arguments can be retrieved in C# scripts using <c>Environment.GetCommandLineArgs()</c>.
    /// </remarks>
    public string[] Arguments { get; set; } = [];
}
