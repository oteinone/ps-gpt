namespace PowershellGpt.Config;

public interface INamedConfigSection
{
    abstract static string GetSectionName();
}

