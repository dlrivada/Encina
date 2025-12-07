using System.Threading.Tasks;
using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Representa la continuación del pipeline en un behavior bajo la política Zero Exceptions.
/// </summary>
/// <typeparam name="TResponse">Tipo devuelto por el handler final.</typeparam>
public delegate Task<Either<Error, TResponse>> RequestHandlerDelegate<TResponse>();
