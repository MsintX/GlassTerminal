(function () {
    "use strict";

    var terminal = new Terminal({
        cursorBlink: true,
        convertEol: false,
        scrollback: 5000,
        fontSize: 14,
        fontFamily: '"Microsoft YaHei", "Segoe UI", sans-serif',
        lineHeight: 1.15,
        theme: {
            background: "rgba(0, 0, 0, 0)",
            foreground: "#F0F5F9",
            cursor: "#FFC078",
            cursorAccent: "#14202C",
            selectionBackground: "rgba(124, 169, 200, 0.42)",
            black: "#17212B",
            red: "#E06C75",
            green: "#98C379",
            yellow: "#E5C07B",
            blue: "#61AFEF",
            magenta: "#C678DD",
            cyan: "#56B6C2",
            white: "#D8DEE9",
            brightBlack: "#687887",
            brightRed: "#F07178",
            brightGreen: "#A9D88A",
            brightYellow: "#F0D399",
            brightBlue: "#79C0FF",
            brightMagenta: "#D7A0E8",
            brightCyan: "#70D5DF",
            brightWhite: "#FFFFFF"
        }
    });

    var fitAddon = new FitAddon.FitAddon();
    terminal.loadAddon(fitAddon);
    terminal.open(document.getElementById("terminal"));

    var utf8Decoder = new TextDecoder("utf-8");
    window.writeTerminalBase64 = function (base64) {
        var binary = atob(base64);
        var bytes = new Uint8Array(binary.length);
        for (var i = 0; i < binary.length; i++)
            bytes[i] = binary.charCodeAt(i);

        terminal.write(utf8Decoder.decode(bytes, { stream: true }));
    };

    function fitAndResize() {
        try {
            fitAddon.fit();
            if (window.bridge)
                window.bridge.resize(terminal.cols, terminal.rows);
        } catch (error) {
            // The browser may report a transient zero-sized layout during startup.
        }
    }

    terminal.onData(function (data) {
        if (window.bridge)
            window.bridge.sendInput(data);
    });

    window.addEventListener("resize", fitAndResize);
    if (window.ResizeObserver) {
        new ResizeObserver(fitAndResize).observe(document.getElementById("terminal"));
    }

    CefSharp.BindObjectAsync("bridge").then(function () {
        fitAndResize();
        terminal.focus();
        window.bridge.ready();
    });
})();
