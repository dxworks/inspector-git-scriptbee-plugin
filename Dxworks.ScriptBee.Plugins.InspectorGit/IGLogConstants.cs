using System;

namespace Dxworks.ScriptBee.Plugins.InspectorGit;

public static class IGLogConstants
{
    public const string CommitIdPrefix = "ig#";

    public const string MessagePrefix = "$";

    public const string GitLogMessageEnd = "#{Glme}";

    public const string ChangePrefix = "#";

    public const string HunkPrefixLine = "@";

    public const string GitLogDiffLineStart = "diff --git";

    public const string DevNull = "/dev/null";
}