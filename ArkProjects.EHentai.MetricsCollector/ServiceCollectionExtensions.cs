namespace ArkProjects.EHentai.MetricsCollector;

public static class ServiceCollectionExtensions
{
    public static T AddAndGetOptionsReflex<T>(this IServiceCollection services, IConfiguration configuration) where T : class
    {
        var sectionName = GetSectionName(typeof(T), null);
        if (sectionName == null)
            throw new Exception("Options class must have \"SectionName\" property or ends with \"Options\"");


        var section = configuration.GetSection(sectionName);
        services.Configure<T>(section);
        return section.Get<T>();
    }

    public static IServiceCollection AddAndGetOptionsReflex<T>(this IServiceCollection services, IConfiguration configuration, out T options) where T : class, new()
    {
        var sectionName = GetSectionName(typeof(T), default);
        if (sectionName == null)
            throw new Exception("Options class must have \"SectionName\" property or ends with \"Options\"");

        var section = configuration.GetSection(sectionName);
        services.Configure<T>(section);
        options = section.Get<T>() ?? new T();
        return services;
    }

    public static IConfiguration GetOptionsReflex<T>(this IConfiguration configuration, out T options) where T : class, new()
    {
        var sectionName = GetSectionName(typeof(T), null);
        if (sectionName == null)
            throw new Exception("Options class must have \"SectionName\" property or ends with \"Options\"");

        var section = configuration.GetSection(sectionName);
        options = section.Get<T>() ?? new T();
        return configuration;
    }

    public static T GetOptionsReflex<T>(this IConfiguration configuration) where T : class, new()
    {
        var sectionName = GetSectionName(typeof(T), null);
        if (sectionName == null)
            throw new Exception("Options class must have \"SectionName\" property or ends with \"Options\"");

        return configuration.GetSection(sectionName).Get<T>() ?? new T();
    }

    private static string? GetSectionName(Type type, string? fallback)
    {
        var value = type.GetField("SectionName")?.GetValue(null) as string;
        if (value == null)
        {
            var optIdx = type.Name.IndexOf("Options", StringComparison.InvariantCultureIgnoreCase);
            if (optIdx > 0 && optIdx + "Options".Length == type.Name.Length)
            {
                value = type.Name[..optIdx];
            }
        }

        return value ?? fallback;
    }
}