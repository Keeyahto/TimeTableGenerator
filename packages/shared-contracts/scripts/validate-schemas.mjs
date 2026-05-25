import { readFileSync, readdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";

const root = dirname(fileURLToPath(import.meta.url));
const pkgRoot = join(root, "..");

const ajv = new Ajv2020({ allErrors: true, strict: false });
addFormats(ajv);

const schemas = readdirSync(pkgRoot)
  .filter((name) => name.endsWith(".schema.json"))
  .sort();

if (schemas.length === 0) {
  console.error("No *.schema.json files found in packages/shared-contracts");
  process.exit(1);
}

for (const file of schemas) {
  const path = join(pkgRoot, file);
  const schema = JSON.parse(readFileSync(path, "utf8"));
  ajv.compile(schema);
  console.log(`OK schema: ${file}`);
}

console.log(
  `All shared contracts schemas are valid JSON Schema documents (${schemas.length} files).`,
);
