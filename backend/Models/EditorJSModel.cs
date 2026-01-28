namespace Backend.Api.Models
{
    // Main Editor.js data structure
    public class EditorJsData
    {
        public long Time { get; set; }
        public List<EditorJsBlock> Blocks { get; set; } = new();
        public string Version { get; set; } = "2.28.0";
    }

    // Block structure
    public class EditorJsBlock
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object Data { get; set; } = new { };
    }

    // Specific block data types
    public class ParagraphBlockData
    {
        public string Text { get; set; } = string.Empty;
    }

    public class HeaderBlockData
    {
        public string Text { get; set; } = string.Empty;
        public int Level { get; set; } = 2;
    }

    public class ImageBlockData
    {
        public ImageFile File { get; set; } = new();
        public string Caption { get; set; } = string.Empty;
        public bool WithBorder { get; set; }
        public bool Stretched { get; set; }
        public bool WithBackground { get; set; }
    }

    public class ImageFile
    {
        public string Url { get; set; } = string.Empty;
    }

    public class ListBlockData
    {
        public string Style { get; set; } = "unordered"; // "ordered" or "unordered"
        public List<string> Items { get; set; } = new();
    }

    public class QuoteBlockData
    {
        public string Text { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string Alignment { get; set; } = "left";
    }

    public class CodeBlockData
    {
        public string Code { get; set; } = string.Empty;
    }

    public class TableBlockData
    {
        public bool WithHeadings { get; set; }
        public List<List<string>> Content { get; set; } = new();
    }

    public class ChecklistBlockData
    {
        public List<ChecklistItem> Items { get; set; } = new();
    }

    public class ChecklistItem
    {
        public string Text { get; set; } = string.Empty;
        public bool Checked { get; set; }
    }

    public class DelimiterBlockData
    {
        // Empty object, delimiter has no data
    }

    public class WarningBlockData
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class EmbedBlockData
    {
        public string Service { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Embed { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public string Caption { get; set; } = string.Empty;
    }

    // Response format for Editor.js image upload
    public class EditorJsImageUploadResponse
    {
        public int Success { get; set; }
        public ImageFile File { get; set; } = new();
    }

    // Response format for Editor.js file upload
    public class EditorJsFileUploadResponse
    {
        public int Success { get; set; }
        public EditorJsFileData File { get; set; } = new();
    }

    public class EditorJsFileData
    {
        public string Url { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
    }
}