import { build } from "esbuild";
import { readFile, writeFile } from "node:fs/promises";
import { relative } from "node:path";
const [shell, css] = await Promise.all(
  ["shell.html", "style.css"].map((path) => readFile(path, "utf8")),
);
const options = {
  entryPoints: ["game.js"],
  bundle: true,
  minify: true,
  write: false,
  legalComments: "inline",
};
const webAssets = {
  name: "web-assets",
  setup(builder) {
    builder.onLoad({ filter: /\.(glb|jpg|m4a|bin)$/ }, ({ path }) => ({
      contents:
        "export default " +
        JSON.stringify(
          "./" + relative(process.cwd(), path).split("\\").join("/"),
        ),
    }));
  },
};
async function makePage(file, assetOptions) {
  const built = await build({ ...options, ...assetOptions });
  const script = built.outputFiles[0].text.replace(/<\/script/gi, "<\\/script");
  await writeFile(
    file,
    shell.replace("/*STYLE*/", () => css).replace("/*SCRIPT*/", () => script),
  );
}
await Promise.all([
  makePage("index.html", {
    loader: {
      ".bin": "dataurl",
      ".glb": "dataurl",
      ".m4a": "dataurl",
      ".jpg": "dataurl",
    },
  }),
  makePage("play.html", { plugins: [webAssets] }),
]);
