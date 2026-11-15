const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..");
const destination = path.join(root, "Terminal", "vendor");
const assets = [
    ["node_modules/@xterm/xterm/lib/xterm.js", "xterm.js"],
    ["node_modules/@xterm/xterm/css/xterm.css", "xterm.css"],
    ["node_modules/@xterm/addon-fit/lib/addon-fit.js", "addon-fit.js"]
];

fs.mkdirSync(destination, { recursive: true });

for (const [sourceRelative, targetName] of assets) {
    const source = path.join(root, sourceRelative);
    const target = path.join(destination, targetName);

    if (!fs.existsSync(source)) {
        throw new Error("Missing xterm asset: " + sourceRelative + ". Run npm install first.");
    }

    fs.copyFileSync(source, target);
    process.stdout.write("Prepared " + targetName + "\n");
}
