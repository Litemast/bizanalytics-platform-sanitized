using BizAnalytics.Api.Contracts.Analytics;

namespace BizAnalytics.Api.Contracts.Import;

public class ImportFilesResponse
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public int FileCount { get; set; }
    public List<ImportedFileResponse> Files { get; set; } = [];
    public AnalysisBundleResponse Analytics { get; set; } = new();
}

public class ImportedFileResponse
{
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public int ImportedRecords { get; set; }
}
