using System;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public class RenderTreeBuilder
    {
        public void OpenElement(int sequence, string name) => throw new NotImplementedException();
        public void CloseElement() => throw new NotImplementedException();
        public void OpenComponent<T>(int sequence, Type type) where T : IComponent => throw new NotImplementedException();
        public void CloseComponent() => throw new NotImplementedException();
        public void AddAttribute(int sequence, string name, object value) => throw new NotImplementedException();
        public void AddAttribute(int sequence, string name) => throw new NotImplementedException();
        public void AddContent(int sequence, string name) => throw new NotImplementedException();
        public void AddMarkupContent(int sequence, string name) => throw new NotImplementedException();
        public void OpenRegion(int sequence) => throw new NotImplementedException();
        public void CloseRegion() => throw new NotImplementedException();
        public void AddComponentReferenceCapture(int sequence, Action<object> callback) => throw new NotImplementedException();
        public void AddElementReferenceCapture(int sequence, Action<ElementReference> callback) => throw new NotImplementedException();
    }
}
