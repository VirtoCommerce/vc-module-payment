import type { SettingEntry } from "./components/SettingsEditor.vue";
import { legacyText } from "./utils";

// Payment methods of a store (api/payment). The server picks the concrete type from typeName: PUT the whole object back.
export interface PaymentMethod {
  id?: string | null;
  code?: string;
  name?: string;
  description?: string;
  typeName?: string;
  logoUrl?: string | null;
  isActive?: boolean;
  priority?: number;
  storeId?: string;
  isAvailableForPartial?: boolean;
  allowDeferredPayment?: boolean;
  localizedName?: { values?: Record<string, string> };
  settings?: SettingEntry[];
}

export const URL = "/api/payment";

// display name/description: the AngularJS texts per type ("payment.labels.<typeName>.name"), else the method's own
export const methodName = (texts: Record<string, unknown>, m: PaymentMethod) =>
  legacyText(texts, "payment", "labels", m.typeName ?? "", "name") ?? m.name ?? m.typeName ?? m.code ?? "";
export const methodDescription = (texts: Record<string, unknown>, m: PaymentMethod) =>
  legacyText(texts, "payment", "labels", m.typeName ?? "", "description") ?? m.description ?? "";
