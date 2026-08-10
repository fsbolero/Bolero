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

    public delegate void RenderFragment(RenderTreeBuilder builder);

    public delegate RenderFragment RenderFragment<T>(T arg);
}
