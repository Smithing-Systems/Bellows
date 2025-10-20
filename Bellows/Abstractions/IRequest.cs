namespace Bellows.Abstractions;

/// <summary>
/// Marker interface for requests that return a response
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IRequest<out TResponse>
{
}