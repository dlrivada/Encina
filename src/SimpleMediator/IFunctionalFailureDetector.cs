using System;

namespace SimpleMediator;

/// <summary>
/// Permite a la aplicación identificar fallos funcionales en las respuestas del mediador.
/// </summary>
/// <remarks>
/// SimpleMediator delega en esta abstracción para no acoplarse a tipos de dominio concretos.
/// Las implementaciones pueden inspeccionar <c>Either</c>, resultados discriminados u objetos
/// específicos para obtener códigos y mensajes.
/// </remarks>
/// <example>
/// <code>
/// public sealed class PaymentOutcomeFailureDetector : IFunctionalFailureDetector
/// {
///     public bool TryExtractFailure(object? response, out string reason, out object? error)
///     {
///         if (response is PaymentOutcome outcome && outcome.TryGetError(out var paymentError))
///         {
///             reason = paymentError.Code ?? "payment.failure";
///             error = paymentError;
///             return true;
///         }
///
///         reason = string.Empty;
///         error = null;
///         return false;
///     }
///
///     public string? TryGetErrorCode(object? error)
///         => (error as PaymentError)?.Code;
///
///     public string? TryGetErrorMessage(object? error)
///         => (error as PaymentError)?.Message;
/// }
/// </code>
/// </example>
public interface IFunctionalFailureDetector
{
    /// <summary>
    /// Intenta determinar si la respuesta representa un fallo funcional.
    /// </summary>
    /// <param name="response">Objeto devuelto por el handler.</param>
    /// <param name="reason">Código o descripción del fallo, cuando se detecta.</param>
    /// <param name="error">Instancia de error capturada para inspecciones posteriores.</param>
    /// <returns><c>true</c> cuando se identificó un fallo funcional; en caso contrario <c>false</c>.</returns>
    bool TryExtractFailure(object? response, out string reason, out object? error);

    /// <summary>
    /// Obtiene un código de error estandarizado a partir del objeto capturado.
    /// </summary>
    /// <param name="error">Instancia devuelta previamente por <see cref="TryExtractFailure"/>.</param>
    /// <returns>El código interpretado o <c>null</c> si no aplica.</returns>
    string? TryGetErrorCode(object? error);

    /// <summary>
    /// Obtiene un mensaje o detalle legible a partir del error capturado.
    /// </summary>
    /// <param name="error">Instancia devuelta por <see cref="TryExtractFailure"/>.</param>
    /// <returns>Mensaje a mostrar o <c>null</c> si no está disponible.</returns>
    string? TryGetErrorMessage(object? error);
}
