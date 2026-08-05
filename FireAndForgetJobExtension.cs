using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hangfire.FireAndForget.Extensions;

/// <summary>
/// Prover suporte para enfileiramento e execução dinâmica de jobs do tipo Fire-and-Forget no Hangfire.
/// Permite serializar dados de estado em JSON e enfileirar rotinas para execução imediata em segundo plano.
/// </summary>
public class FireAndForgetJobExtension
{
    /// <summary>
    /// Delegado responsável por definir a assinatura da ação a ser executada dinamicamente quando o job Fire-and-Forget for processado.
    /// </summary>
    /// <param name="destination">Identificador do destino ou rota do serviço/módulo a executar.</param>
    /// <param name="stateSerialized">Estado ou parâmetros da requisição serializados em formato JSON.</param>
    public delegate void DynamicAction(string destination, string? stateSerialized);

    private static DynamicAction? Action { get; set; }
    private static TimeZoneInfo? _defaultTimeZone = TimeZoneInfo.Utc;

    /// <summary>
    /// Fuso horário padrão configurado para a extensão (padrão é UTC).
    /// Permite alteração apenas se o valor atual ainda for UTC.
    /// </summary>
    public static TimeZoneInfo? DefaultTimeZone { 
        get => _defaultTimeZone;
        set {
            if(_defaultTimeZone == TimeZoneInfo.Utc) 
            {
                _defaultTimeZone = value;
            }
        }
    }

    /// <summary>
    /// Configura o manipulador de ação dinâmico que será invocado quando os jobs forem executados.
    /// O registro é realizado uma única vez (não sobrescreve se já estiver definido).
    /// </summary>
    /// <param name="action">Ação a ser executada no processamento dos jobs.</param>
    public static void SetAction(DynamicAction action)
    {
        if (Action == null)
        {
            Action = action;
        }
    }

    /// <summary>
    /// Enfileira um job do tipo Fire-and-Forget no Hangfire, serializando o objeto de estado em JSON.
    /// </summary>
    /// <param name="destination">Destino ou identificador da rota/ação a ser executada.</param>
    /// <param name="state">Objeto de estado a ser serializado em JSON e passado para a execução.</param>
    /// <param name="serializerOptions">Opções de serialização JSON (opcional).</param>
    /// <param name="timeZone">Fuso horário específico (opcional).</param>
    public static void Enqueue(string destination, object? state = null, JsonSerializerOptions? serializerOptions = null, TimeZoneInfo? timeZone = null)
    {
        string objectJson = JsonSerializer.Serialize(state, serializerOptions);
        BackgroundJob.Enqueue(() => DynamicExecution(destination, objectJson));
    }

    /// <summary>
    /// Método invocado internamente pelo Hangfire no momento da execução do job enfileirado.
    /// Responsável por invocar o delegado <see cref="Action"/> previamente configurado.
    /// </summary>
    /// <param name="destination">Identificador do destino da ação.</param>
    /// <param name="stateSerialized">Estado serializado em JSON.</param>
    public static void DynamicExecution(string destination, string stateSerialized)
    {
        Action?.Invoke(destination, stateSerialized);
    }
}
