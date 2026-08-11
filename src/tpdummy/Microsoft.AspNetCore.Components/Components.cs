using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components
{
    public class ChangeEventArgs
    {
        public object Value => new object();
    }

    public interface IComponent {}

    public class ComponentBase : IComponent {}

    public class ElementReference {}

    public struct EventCallback<T> {}

    public class EventCallbackFactory
    {
        public EventCallback<T> Create<T>(object receiver, Action<T> callback) => throw new NotImplementedException();
        public EventCallback<T> Create<T>(object receiver, Func<T, Task> callback) => throw new NotImplementedException();
    }

    public struct EventCallback
    {
        public static readonly EventCallbackFactory Factory = new EventCallbackFactory();
    }

    public delegate void RenderFragment(RenderTreeBuilder builder);

    public delegate RenderFragment RenderFragment<T>(T arg);
}
