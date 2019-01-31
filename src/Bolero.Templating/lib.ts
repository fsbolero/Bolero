/// <reference path="blazor-api.ts" />

namespace Bolero.Templating {

  export var connected: Promise<WebSocket> = null;

  export function setup(url: string, dotnetclient: DotNetObjectRef) {
    if (connected === null) {
      let socket = new WebSocket(url);
      connected = new Promise((resolve, reject) => {
        socket.onopen = () => resolve(socket);
        socket.onerror = error => reject(error);
      });
      socket.onmessage = (ev) => {
        let nl = ev.data.indexOf('\n');
        let filename = ev.data.substring(0, nl);
        let content = ev.data.substring(nl + 1);
        dotnetclient.invokeMethodAsync("FileChanged", filename, content);
      };
    }
  }

  export function requestFile(filename: string) {
    connected.then(socket => socket.send(filename));
  }
}
