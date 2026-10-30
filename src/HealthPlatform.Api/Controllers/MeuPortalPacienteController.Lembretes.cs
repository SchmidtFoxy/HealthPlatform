using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Push;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record SalvarLembretesRequest(bool Refeicoes, bool Agua, bool Treino, string? HoraTreino, bool Chat, int? Fuso);

// Perfil › Lembretes (v0.80.0): um lugar so para ligar refeicoes, agua, treino e avisos do chat.
// Agua usa o lembrete da v0.76.0 e o chat usa a preferencia de mensagens da conta: nada duplicado.
public sealed partial class MeuPortalPacienteController
{
    [HttpGet("lembretes")]
    public async Task<IActionResult> MeusLembretes(CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });
        return Ok(await MontarLembretes(pacienteId.Value, ct));
    }

    [HttpPut("lembretes")]
    public async Task<IActionResult> SalvarLembretes(SalvarLembretesRequest request, CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var horaTreino = LembretesPacienteService.HoraTreinoPadrao;
        if (!string.IsNullOrWhiteSpace(request.HoraTreino) && !TimeOnly.TryParseExact(request.HoraTreino.Trim(), "HH:mm", out horaTreino))
            return BadRequest(new { message = "Horario do treino invalido. Use HH:mm." });

        var usuario = await db.Users.FirstOrDefaultAsync(x => x.Id == currentUser.UserId && x.Ativo, ct);
        if (usuario is null)
            return NotFound(new { message = "Usuario nao encontrado." });

        var fuso = AguaPacienteService.FusoValido(request.Fuso);
        var antes = await LembretesPacienteService.LerAsync(db, currentUser.UserId, ct);
        var aguaAntes = await AguaPacienteService.LerLembreteAsync(db, currentUser.UserId, ct);
        var chatAntes = usuario.NotificarMensagens;

        await LembretesPacienteService.SalvarAsync(db, currentUser.UserId, request.Refeicoes, request.Treino, horaTreino, fuso, ct);
        usuario.NotificarMensagens = request.Chat;
        Auditar("UPDATE", "LembretesPaciente", currentUser.UserId,
            new { antes.Refeicoes, Agua = aguaAntes?.Ativo == true, antes.Treino, antes.HoraTreino, Chat = chatAntes },
            new { request.Refeicoes, request.Agua, request.Treino, HoraTreino = horaTreino.ToString("HH:mm"), request.Chat });
        await db.SaveChangesAsync(ct);
        // Salva por ultimo: o lembrete de agua grava sozinho.
        if ((aguaAntes?.Ativo == true) != request.Agua || aguaAntes?.FusoMinutos != fuso)
            await AguaPacienteService.SalvarLembreteAsync(db, currentUser.UserId, request.Agua, fuso, ct);

        return Ok(await MontarLembretes(pacienteId.Value, ct));
    }

    private async Task<object> MontarLembretes(Guid pacienteId, CancellationToken ct)
    {
        var hoje = HorarioLocal.Hoje();
        var estado = await LembretesPacienteService.LerAsync(db, currentUser.UserId, ct);
        var agua = await AguaPacienteService.LerLembreteAsync(db, currentUser.UserId, ct);
        var chat = await db.Users.AsNoTracking().Where(x => x.Id == currentUser.UserId).Select(x => x.NotificarMensagens).FirstOrDefaultAsync(ct);
        var refeicoes = await LembretesPacienteService.RefeicoesDoPlanoAsync(db, pacienteId, hoje, ct);
        var treinoHoje = await LembretesPacienteService.TreinoPendenteHojeAsync(db, pacienteId, hoje, ct);
        var aparelhos = await db.Set<IdentityUserToken<Guid>>().AsNoTracking()
            .CountAsync(x => x.UserId == currentUser.UserId && x.LoginProvider == WebPushNotificationService.LoginProvider, ct);

        return new
        {
            refeicoes = estado.Refeicoes,
            agua = agua?.Ativo == true,
            treino = estado.Treino,
            horaTreino = LembretesPacienteService.HoraTreino(estado).ToString("HH:mm"),
            chat,
            aparelhos,
            refeicoesDoPlano = refeicoes.Select(r => new { r.Nome, horario = r.Horario?.ToString("HH:mm") }),
            treinoHoje
        };
    }
}
