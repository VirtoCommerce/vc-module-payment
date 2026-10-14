import { api } from "./api";

// The platform's merged module localization (the AngularJS admin's texts): titles of settings and names of
// payment/shipping/tax method types live there, keyed by setting/type name.
type Dict = Record<string, unknown>;
let legacy: Promise<Dict> | undefined;
export function loadLegacyTexts() {
  legacy ??= api.get<Dict>("/api/platform/localization?lang=en").catch(() => ({}));
  return legacy;
}

// keys mix dotted names and nesting ("settings" → "Stores" → "States", or "settings" → "VirtoCommerce.Core.General.TaxTypes")
function resolve(obj: unknown, parts: string[]): unknown {
  if (!parts.length) return obj;
  if (!obj || typeof obj !== "object") return undefined;
  for (let i = parts.length; i > 0; i--) {
    const next = (obj as Dict)[parts.slice(0, i).join(".")];
    if (next !== undefined) {
      const found = resolve(next, parts.slice(i));
      if (found !== undefined) return found;
    }
  }
  return undefined;
}

export function legacyText(texts: Dict, ...path: string[]): string | undefined {
  const value = resolve(texts, path.flatMap((p) => p.split(".")));
  return typeof value === "string" ? value : undefined;
}
