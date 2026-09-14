namespace SfmPackageBuilder.Core.Domain;

public static class ModelReleaseName
{
    public static string EffectiveStem(ModelEntry model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!string.IsNullOrWhiteSpace(model.ReleaseStem))
        {
            return model.ReleaseStem;
        }

        if (!string.IsNullOrWhiteSpace(model.SourceStem))
        {
            return model.SourceStem.Trim();
        }

        return string.IsNullOrWhiteSpace(model.SourceMdlPath)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(model.SourceMdlPath).Trim();
    }
}
