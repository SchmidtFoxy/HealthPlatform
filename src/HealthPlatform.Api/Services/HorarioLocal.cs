namespace HealthPlatform.Api.Services;

/// <summary>
/// "Hoje" do app no horario de Brasilia (v0.80.0). O servidor roda em UTC, entao sem isso o dia
/// virava as 21h no Brasil: dieta, treino, diario e sequencias passavam para o dia seguinte.
/// Toda conta de dia (hoje, inicio/fim do dia, agrupar por dia) passa por aqui.
/// </summary>
public static class HorarioLocal
{
    public const string FusoId = "America/Sao_Paulo";

    public static readonly TimeZoneInfo Fuso = CarregarFuso();

    /// <summary>Data de hoje no Brasil.</summary>
    public static DateOnly Hoje() => Dia(DateTime.UtcNow);

    /// <summary>Agora no horario de Brasilia (Kind Unspecified).</summary>
    public static DateTime Agora() => ParaLocal(DateTime.UtcNow);

    public static DateTime ParaLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Fuso);

    /// <summary>Dia no Brasil de um instante UTC.</summary>
    public static DateOnly Dia(DateTime utc) => DateOnly.FromDateTime(ParaLocal(utc));

    /// <summary>Meia-noite de Brasilia do dia, em UTC (inicio do dia para filtrar registros).</summary>
    public static DateTime InicioUtc(DateOnly dia)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), Fuso);

    /// <summary>Fim exclusivo do dia (meia-noite do dia seguinte), em UTC.</summary>
    public static DateTime FimUtc(DateOnly dia) => InicioUtc(dia.AddDays(1));

    private static TimeZoneInfo CarregarFuso()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(FusoId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Sem tzdata no sistema: o Brasil esta fixo em UTC-3 desde 2019.
            return TimeZoneInfo.CreateCustomTimeZone(FusoId, TimeSpan.FromHours(-3), "Brasilia", "Brasilia");
        }
    }
}
