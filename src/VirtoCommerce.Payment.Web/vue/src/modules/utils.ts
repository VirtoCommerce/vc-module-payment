import { i18n } from "@vc-shell/framework";
import { shallowRef, watch } from "vue";
import { api } from "./api";

// The platform's merged module localization (the AngularJS admin's texts): titles of settings and names of
// payment/shipping/tax method types live there, keyed by setting/type name. The server returns only the asked
// language's texts: the UI's language comes first, English fills what it lacks.
type Dict = Record<string, unknown>;
export type LegacyTexts = Dict[];

// the language the UI shows now (vue-i18n's global locale, a string or a ref)
export function currentLocale(): string {
  const locale = i18n.global.locale as unknown;
  return String((locale as { value?: string } | null)?.value ?? locale ?? "") || "en";
}

const legacy = new Map<string, Promise<Dict>>();
function loadLocale(locale: string) {
  let loaded = legacy.get(locale);
  if (!loaded) {
    loaded = api.get<Dict>(`/api/platform/localization?lang=${encodeURIComponent(locale)}`).catch(() => ({}));
    legacy.set(locale, loaded);
  }
  return loaded;
}

export function loadLegacyTexts(locale = currentLocale()): Promise<LegacyTexts> {
  return Promise.all(locale === "en" ? [loadLocale("en")] : [loadLocale(locale), loadLocale("en")]);
}

// the texts of the UI's language, reloaded when the user switches it
export function useLegacyTexts() {
  const texts = shallowRef<LegacyTexts>([]);
  watch(
    currentLocale,
    async (locale) => {
      const loaded = await loadLegacyTexts(locale);
      if (locale === currentLocale()) texts.value = loaded; // a later switch wins
    },
    { immediate: true },
  );
  return texts;
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

export function legacyText(texts: LegacyTexts, ...path: string[]): string | undefined {
  const parts = path.flatMap((p) => p.split("."));
  for (const dict of texts) {
    const value = resolve(dict, parts);
    if (typeof value === "string") return value;
  }
  return undefined;
}
