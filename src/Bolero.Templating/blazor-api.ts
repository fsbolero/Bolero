interface DotNetObjectRef {
  invokeMethodAsync(method: string, ...args: any[]);
}

declare namespace DotNet {
  function invokeMethodAsync(method: string, ...args: any[]);
}
