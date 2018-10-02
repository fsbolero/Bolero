namespace MiniBlazor {

    interface DotNetObjectRef {
        invokeMethodAsync(name: string, ...args: any[]): Promise<any>;
    }

    type RenderedNode = string | RenderedElement[] | RenderedElement;
    interface RenderedElement {
        n: string;
        a?: { [key: string]: string };
        e?: { [key: string]: DotNetObjectRef };
        c?: RenderedNode[];
    }

    type Diff = Skip | Delete | Replace | Insert | InPlace | Move;
    interface Skip { s: number }
    interface Delete { d: number }
    interface Replace { r: RenderedNode }
    interface Insert { i: RenderedNode }
    interface Move { f: number; n: number }
    interface InPlace {
        a?: { [key: string]: string };
        e?: { [key: string]: DotNetObjectRef };
        c?: Diff[];
    }

    export class RenderedTree {
        root: Element;

        constructor(root: Element, initTree: RenderedNode[], hydrateOnly: boolean) {
            if (hydrateOnly) {
                this.hydrate({n: root.nodeName, c: initTree}, root);
            } else {
                const fragment = document.createDocumentFragment();
                initTree.forEach(node => {
                    fragment.appendChild(this.makeTree(node));
                });
                while (root.firstChild) {
                    root.removeChild(root.firstChild);
                }
                root.appendChild(fragment);
            }
            this.root = root;
        }

        static eventArgs(event: Event, element: Element): any {
            switch (event.type) {
                case 'change':
                case 'input':
                    return (element as HTMLInputElement).value;
                default:
                    return null;
            }
        }

        addEvent(node: Element, name: string, handler: DotNetObjectRef): void {
            node.addEventListener(name, (event: Event) => {
                handler.invokeMethodAsync('Handle', RenderedTree.eventArgs(event, node));
            });
        }

        makeTree(tree: RenderedNode): Node {
            if (typeof tree == 'string') {
                return document.createTextNode(tree);
            } else if (tree instanceof Array) {
                const fragment = document.createDocumentFragment();
                tree.forEach(node => {
                    fragment.appendChild(this.makeTree(node));
                });
                return fragment;
            } else {
                const node = document.createElement(tree.n);
                if (tree.a) {
                    for (const a in tree.a) {
                        node.setAttribute(a, tree.a[a]);
                    }
                }
                if (tree.e) {
                    for (const e in tree.e) {
                        this.addEvent(node, e, tree.e[e]);
                    }
                }
                if (tree.c) {
                    tree.c.forEach(child => {
                        node.appendChild(this.makeTree(child));
                    });
                }
                return node;
            }
        }

        hydrate(tree: RenderedNode, node: Node) {
            if (typeof tree == 'string') {
                if (tree == '') {
                    node.parentNode.insertBefore(document.createTextNode(''), node);
                }
                return node.nextSibling;
            } else if (tree instanceof Array) {
                tree.forEach(sub => {
                    node = this.hydrate(sub, node);
                });
                return node;
            } else {
                if (tree.e) {
                    for (const e in tree.e) {
                        this.addEvent(node as Element, e, tree.e[e]);
                    }
                }
                if (tree.c) {
                    let child: Node = node.firstChild;
                    tree.c.forEach(sub => {
                        if (child == null) {
                            // This should only happen if tree.c is [""]
                            node.appendChild(this.makeTree(sub));
                        } else {
                            child = this.hydrate(sub, child);
                        }
                    });
                }
                return node.nextSibling;
            }
        }

        applyRootDiff(diff: Diff[]) {
            this.applyDiff({c:diff}, null, this.root);
        }

        applyDiff(diff: Diff, parent: Element, node: Node) {
            if ('s' in diff) {
                // Skip
                for (let i = 0; i < diff.s; i++) {
                    node = node.nextSibling;
                }
                return node;
            } else if ('r' in diff) {
                // Replace
                const next = node.nextSibling;
                parent.replaceChild(this.makeTree(diff.r), node);
                return next;
            } else if ('i' in diff) {
                // Insert
                const newNode = this.makeTree(diff.i);
                if (node === null) {
                    parent.appendChild(newNode);
                } else {
                    parent.insertBefore(newNode, node);
                }
                return node;
            } else if ('d' in diff) {
                // Delete
                for (let i = 0; i < diff.d; i++) {
                    const next = node.nextSibling;
                    parent.removeChild(node);
                    node = next;
                }
                return node;
            } else if ('f' in diff) {
                // Move
                let firstAdded = node;
                for (let i = 0; i < diff.n; i++) {
                    let n = parent.insertBefore(parent.children[diff.f], node);
                    if (i == 0) {
                        firstAdded = n;
                    }
                }
                return firstAdded;
            } else {
                // Modify
                const element = node as Element;
                if (diff.a) {
                    for (const a in diff.a) {
                        if (diff.a[a] === null) {
                            element.removeAttribute(a);
                        } else {
                            element.setAttribute(a, diff.a[a]);
                        }
                    }
                }
                if (diff.e) {
                    for (const e in diff.e) {
                        this.addEvent(element, e, diff.e[e]);
                    }
                }
                if (diff.c) {
                    let child: Node = element.firstChild;
                    diff.c.forEach(childDiff => {
                        child = this.applyDiff(childDiff, element, child);
                    });
                }
                return element.nextSibling;
            }
        }
    }

    let running: {[key: string]: RenderedTree} = {};

    export function mount(selector: string, initTree: RenderedNode[]): void {
        const root = document.querySelector(selector);
        const hydrate = root.hasAttribute("data-miniblazor-hydrate");
        if (hydrate) root.removeAttribute("data-miniblazor-hydrate");
        running[selector] = new MiniBlazor.RenderedTree(root, initTree, hydrate);
    }

    export function applyDiff(selector: string, diff: Diff[]) {
        running[selector].applyRootDiff(diff);
    }
}

