this.MiniBlazor = this.MiniBlazor || {};

this.MiniBlazor.RenderedTree = class RenderedTree {
    constructor(root, initTree) {
        let nodes = initTree.map(t => this.makeTree(t));
        for (let i = 0; i < nodes.length; i++) {
            root.appendChild(nodes[i]);
        }
        this.root = root;
    }

    static eventArgs(event, element) {
        switch (event.type) {
            case "change":
            case "input":
                return element.value;
            default:
                return null;
        }
    }

    addEvent(node, name, handler) {
        node.addEventListener(name, event => {
            handler.invokeMethodAsync("Handle", RenderedTree.eventArgs(event, node))
                .then(diff => {
                    console.log("DIFF", diff);
                    this.applyDiff({c:diff}, null, this.root);
                });
        });
    }

    makeTree(tree) {
        if (typeof tree == 'string') {
            return document.createTextNode(tree);
        } else if (tree instanceof Array) {
            let fragment = document.createDocumentFragment();
            for (let i = 0; i < tree.length; i++) {
                fragment.appendChild(this.makeTree(tree[i]));
            }
            return fragment;
        } else {
            let node = document.createElement(tree.n);
            if (tree.a) {
                for (let a in tree.a) {
                    node.setAttribute(a, tree.a[a]);
                }
            }
            if (tree.e) {
                for (let e in tree.e) {
                    this.addEvent(node, e, tree.e[e]);
                }
            }
            if (tree.c) {
                for (let i = 0; i < tree.c.length; i++) {
                    node.appendChild(this.makeTree(tree.c[i]));
                }
            }
            return node;
        }
    }

    applyDiff(diff, parent, node) {
        if (diff.s !== undefined) {
            // Skip
            for (let i = 0; i < diff.s; i++) {
                node = node.nextSibling;
            }
            return node;
        } else if (diff.r !== undefined) {
            // Replace
            let next = node.nextSibling;
            parent.replaceChild(this.makeTree(diff.r), node);
            return next;
        } else if (diff.i !== undefined) {
            // Insert
            let newNode = this.makeTree(diff.i);
            if (node === null) {
                parent.appendChild(newNode);
            } else {
                parent.insertBefore(newNode, node);
            }
            return node;
        } else if (diff.d !== undefined) {
            // Delete
            for (let i = 0; i < diff.d; i++) {
                let next = node.nextSibling;
                parent.removeChild(node);
                node = next;
            }
            return node;
        } else if (diff.f !== undefined) {
            // Move
            for (let i = 0; i < diff.n; i++) {
                parent.insertBefore(parent.children[diff.f], node);
            }
            return node;
        } else {
            // Modify
            if (diff.a) {
                for (let a in diff.a) {
                    if (diff.a[a] === null) {
                        node.removeAttribute(a);
                    } else {
                        node.setAttribute(a, diff.a[a]);
                    }
                }
            }
            if (diff.e) {
                for (let e in diff.e) {
                    this.addEvent(node, e, diff.e[e]);
                }
            }
            if (diff.c) {
                let child = node.firstChild;
                for (let i = 0; i < diff.c.length; i++) {
                    child = this.applyDiff(diff.c[i], node, child);
                }
            }
            return node.nextSibling;
        }
    }
}

this.MiniBlazor.mount = function(selector, initTree) {
    let root = document.querySelector(selector);
    new MiniBlazor.RenderedTree(root, initTree);
}
