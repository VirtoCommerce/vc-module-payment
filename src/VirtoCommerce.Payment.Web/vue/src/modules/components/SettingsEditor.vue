<template>
  <!-- Editors for platform ObjectSettingEntry values (store settings, payment/shipping/tax method settings), grouped like the AngularJS settings blade -->
  <VcCard
    v-for="[group, entries] in groups"
    :key="group"
    :header="group"
    is-collapsable
  >
    <div class="settings-editor">
      <template
        v-for="setting in entries"
        :key="setting.name"
      >
        <VcSwitch
          v-if="setting.valueType === 'Boolean'"
          :model-value="Boolean(current(setting))"
          :label="label(setting)"
          :hint="hint(setting)"
          :disabled="setting.isReadOnly"
          @update:model-value="(v?: boolean) => (setting.value = v)"
        />
        <!-- a vee-validate field, so a required setting left empty keeps the blade's Save disabled -->
        <Field
          v-else
          v-slot="{ errorMessage, handleChange, errors }"
          :name="setting.name!"
          :label="label(setting)"
          :rules="setting.isRequired && !setting.isReadOnly ? 'required' : undefined"
          :model-value="current(setting)"
        >
          <VcSelect
            v-if="setting.allowedValues?.length"
            :model-value="current(setting)"
            :label="label(setting)"
            :hint="hint(setting)"
            :options="setting.allowedValues.map((x) => ({ id: x, title: String(x) }))"
            :disabled="setting.isReadOnly"
            :required="setting.isRequired"
            :error="!!errors.length"
            :error-message="errorMessage"
            @update:model-value="(v: unknown) => handleChange((setting.value = v))"
          />
          <VcTextarea
            v-else-if="setting.valueType === 'LongText' || setting.valueType === 'Json'"
            :model-value="text(setting)"
            :label="label(setting)"
            :hint="hint(setting)"
            :disabled="setting.isReadOnly"
            :required="setting.isRequired"
            :error="!!errors.length"
            :error-message="errorMessage"
            @update:model-value="(v?: string) => handleChange((setting.value = v))"
          />
          <VcInput
            v-else
            :model-value="text(setting)"
            :label="label(setting)"
            :hint="hint(setting)"
            :type="inputType(setting.valueType)"
            :disabled="setting.isReadOnly"
            :required="setting.isRequired"
            :error="!!errors.length"
            :error-message="errorMessage"
            @update:model-value="(v: unknown) => handleChange((setting.value = toValue(setting.valueType, v)))"
          />
        </Field>
      </template>
    </div>
  </VcCard>
</template>

<script setup lang="ts">
import { VcCard, VcInput, VcSelect, VcSwitch, VcTextarea } from "@vc-shell/framework/ui";
import { Field } from "vee-validate";
import { computed, onMounted, ref } from "vue";
import { legacyText, loadLegacyTexts } from "../utils";

export interface SettingEntry {
  name?: string;
  displayName?: string;
  groupName?: string;
  valueType?: string;
  value?: unknown;
  defaultValue?: unknown;
  allowedValues?: unknown[];
  isReadOnly?: boolean;
  isRequired?: boolean;
  isHidden?: boolean;
}

const props = defineProps<{ settings: SettingEntry[] }>();

const texts = ref<Record<string, unknown>>({});
onMounted(async () => (texts.value = await loadLegacyTexts()));

// "Store|General" → "General"
const groups = computed(() => {
  const map = new Map<string, SettingEntry[]>();
  for (const s of props.settings.filter((x) => !x.isHidden)) {
    const group = s.groupName?.split("|").pop() || "General";
    map.set(group, [...(map.get(group) ?? []), s]);
  }
  return [...map.entries()];
});

const label = (s: SettingEntry) => legacyText(texts.value, "settings", s.name!, "title") ?? s.displayName ?? s.name ?? "";
const hint = (s: SettingEntry) => legacyText(texts.value, "settings", s.name!, "description");
const current = (s: SettingEntry) => s.value ?? s.defaultValue;
const text = (s: SettingEntry) => {
  const v = current(s);
  return v === null || v === undefined ? "" : typeof v === "object" ? JSON.stringify(v) : String(v);
};

function inputType(valueType?: string) {
  switch (valueType) {
    case "Integer":
    case "PositiveInteger":
    case "Decimal":
      return "number";
    case "DateTime":
      return "datetime-local";
    case "SecureString":
      return "password";
    default:
      return "text";
  }
}

function toValue(valueType: string | undefined, v: unknown) {
  if (inputType(valueType) !== "number") return v;
  return v === "" || v === null || v === undefined ? null : Number(v);
}
</script>

<style lang="scss">
.settings-editor {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}
</style>
