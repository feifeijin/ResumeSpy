namespace ResumeSpy.Core.Entities.Export
{
    public sealed class ResumeDocument
    {
        public string Title { get; init; } = string.Empty;
        public IReadOnlyList<ResumeBlock> Blocks { get; init; } = [];
    }

    public abstract class ResumeBlock { }

    public sealed class ResumeHeadingBlock : ResumeBlock
    {
        public int Level { get; init; }
        public string Text { get; init; } = string.Empty;
    }

    public sealed class ResumeParagraphBlock : ResumeBlock
    {
        public string Text { get; init; } = string.Empty;
    }

    public sealed class ResumeBulletListBlock : ResumeBlock
    {
        public IReadOnlyList<string> Items { get; init; } = [];
    }

    public sealed class ResumeDividerBlock : ResumeBlock { }
}
