using SfmPackageBuilder.Core.Mdl;

namespace SfmPackageBuilder.Core.Planning;

public sealed class PlannedModelMaterialDiagnostic
{
    public PlannedModelMaterialDiagnostic(
        MdlMetadataSection section,
        MdlMetadataDiagnosticSeverity severity,
        string code,
        string message)
    {
        Section = section;
        Severity = severity;
        Code = code;
        Message = message;
    }

    public MdlMetadataSection Section { get; }

    public MdlMetadataDiagnosticSeverity Severity { get; }

    public string Code { get; }

    public string Message { get; }
}
