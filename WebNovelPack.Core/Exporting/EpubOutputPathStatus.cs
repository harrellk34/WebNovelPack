namespace WebNovelPack.Core.Exporting;

public enum EpubOutputPathStatus
{
    Valid,
    InvalidOutputFolder,
    MissingOutputFolder,
    MissingFileName,
    InvalidFileName,
    AbsoluteFileNameNotAllowed,
    PathTraversalDetected,
    OutputFileAlreadyExists
}
