let RenderedTree = function(initTree) {
    this.node = this.makeTree(initTree);
}

let eventArgs = (event, element) => {
    switch (event.type) {
        case "change":
        case "input":
            return element.value;
        default:
            return null;
    }
};

RenderedTree.prototype.addEvent = function(node, name, handler) {
    node.addEventListener(name, event => {
        handler.invokeMethodAsync("Handle", eventArgs(event, node))
            .then(diff => {
                console.log("DIFF", diff);
                this.applyDiff(diff, this.node.parentNode, this.node);
            });
    });
}

RenderedTree.prototype.makeTree = function(tree) {
    if (typeof tree == 'string') {
        return document.createTextNode(tree);
    } else {
        let node = document.createElement(tree.n);
        for (let a in tree.a) {
            node.setAttribute(a, tree.a[a]);
        }
        // WTF: why is tree.events an array? it's serialized from Dictionary<string, obj> just like tree.attrs
        for (let i = 0; i < tree.e.length; i++) {
            this.addEvent(node, tree.e[i].key, tree.e[i].value);
        }
        for (let i = 0; i < tree.c.length; i++) {
            node.appendChild(this.makeTree(tree.c[i]));
        }
        return node;
    }
};

RenderedTree.prototype.applyDiff = function(diff, parent, node) {
    let next = node ? node.nextSibling : null;
    if (diff == 's') {
        return next;
    } else if (diff == 'd') {
        parent.removeChild(node);
        return next;
    } else if (diff.r) {
        parent.replaceChild(this.makeTree(diff.r), node);
        return next;
    } else if (diff.i) {
        let newNode = this.makeTree(diff.i);
        if (next === null) {
            parent.appendChild(newNode)
        } else {
            parent.insertBefore(newNode, node);
        }
        return node;
    } else if (diff.a) {
        for (let a in diff.a) {
            if (diff.a[a] === null) {
                node.removeAttribute(a);
            } else {
                node.setAttribute(a, diff.a[a]);
            }
        }
        for (let i = 0; i < diff.e.length; i++) {
            this.addEvent(node, diff.e[i].key, diff.e[i].value);
        }
        let child = node.firstChild;
        for (let i = 0; i < diff.c.length; i++) {
            child = this.applyDiff(diff.c[i], node, child);
        }
        while (child) {
            next = child.nextSibling;
            node.removeChild(child);
            child = next;
        }
        return next;
    }
}

var MiniBlazor = {
    RenderedTree: RenderedTree,
    mount: function(selector, initTree) {
        document.querySelector(selector)
            .appendChild((new RenderedTree(initTree)).node);
    }
}
