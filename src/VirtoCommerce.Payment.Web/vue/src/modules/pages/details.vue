<template>
  <VcBlade
    :title="name"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcContainer>
      <VcForm class="payment-form tw-space-y-4">
        <VcCard :header="$t('PAYMENT.GENERAL')">
          <div class="payment-card">
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcField
                  :label="$t('PAYMENT.FIELDS.NAME')"
                  :model-value="name"
                />
              </VcCol>
              <VcCol>
                <VcField
                  :label="$t('PAYMENT.FIELDS.CODE')"
                  :model-value="item.code"
                  copyable
                />
              </VcCol>
            </VcRow>
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcSwitch
                  v-model="item.isActive"
                  :label="$t('PAYMENT.FIELDS.IS_ACTIVE')"
                />
              </VcCol>
              <VcCol>
                <VcInput
                  :model-value="item.priority ?? 0"
                  :label="$t('PAYMENT.FIELDS.PRIORITY')"
                  type="number"
                  @update:model-value="(v) => (item.priority = Number(v) || 0)"
                />
              </VcCol>
            </VcRow>
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcSwitch
                  v-model="item.isAvailableForPartial"
                  :label="$t('PAYMENT.FIELDS.PARTIAL')"
                />
              </VcCol>
              <VcCol>
                <VcSwitch
                  v-model="item.allowDeferredPayment"
                  :label="$t('PAYMENT.FIELDS.DEFERRED')"
                />
              </VcCol>
            </VcRow>
            <VcTextarea
              v-model="item.description"
              :label="$t('PAYMENT.FIELDS.DESCRIPTION')"
            />
            <ImageField
              v-model="item.logoUrl"
              :label="$t('PAYMENT.FIELDS.LOGO')"
              folder="/payment-methods"
            />
          </div>
        </VcCard>

        <VcCard
          v-if="languages.length"
          :header="$t('PAYMENT.FIELDS.LOCALIZED_NAME')"
        >
          <div class="payment-card">
            <VcInput
              v-for="lang in languages"
              :key="lang"
              :model-value="item.localizedName?.values?.[lang] ?? ''"
              :label="lang"
              maxlength="1024"
              @update:model-value="(v) => setLocalizedName(lang, String(v ?? ''))"
            />
          </div>
        </VcCard>

        <SettingsEditor
          v-if="item.settings?.length"
          :settings="item.settings"
        />
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useBladeForm, useLoading, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcCard, VcCol, VcContainer, VcField, VcForm, VcInput, VcRow, VcSwitch, VcTextarea } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { api } from "../api";
import ImageField from "../components/ImageField.vue";
import SettingsEditor from "../components/SettingsEditor.vue";
import { URL, methodName, type PaymentMethod } from "../methods";
import { loadLegacyTexts } from "../utils";

// not routable: the method and its store come only through options, which a page reload doesn't restore
defineBlade({
  url: "/payment-method",
  name: "PaymentMethodDetails",
  routable: false,
});

const { t } = useI18n({ useScope: "global" });
const { options, callParent } = useBlade<{ method?: PaymentMethod; storeId: string }>();

const item = ref<PaymentMethod>(JSON.parse(JSON.stringify(options.value?.method ?? {})));
const texts = ref<Record<string, unknown>>({});
const languages = ref<string[]>([]);
const name = computed(() => methodName(texts.value, item.value));

const form = useBladeForm({
  data: item,
  closeConfirmMessage: computed(() => t("PAYMENT.ALERTS.CLOSE_CONFIRMATION")),
});

function setLocalizedName(lang: string, value: string) {
  item.value.localizedName = { ...item.value.localizedName, values: { ...item.value.localizedName?.values, [lang]: value } };
}

const { loading: fetching, action: load } = useAsync(async () => {
  // a transient method (never saved for this store) has no id: it is created by the first save
  if (item.value.id) item.value = await api.get<PaymentMethod>(`${URL}/${item.value.id}`);
  form.setBaseline();
});

const { loading: saving, action: save } = useAsync(async () => {
  const saved = await api.put<PaymentMethod>(URL, { ...item.value, storeId: options.value?.storeId });
  item.value.id = saved?.id ?? item.value.id;
  await load();
  await callParent("reload");
});

const loading = useLoading(fetching, saving);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: t("PAYMENT.TOOLBAR.SAVE"),
    icon: "lucide-save",
    permissions: "store:update",
    disabled: computed(() => !form.canSave.value),
    clickHandler: () => save(),
  },
  {
    id: "reset",
    title: t("PAYMENT.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
]);

onMounted(() =>
  Promise.all([
    loadLegacyTexts().then((x) => (texts.value = x)),
    load(),
    // the localized names are edited for the store's languages
    options.value?.storeId &&
      api
        .get<{ languages?: string[] }>(`/api/stores/${encodeURIComponent(options.value.storeId)}`)
        .then((x) => (languages.value = x.languages ?? [])),
  ]),
);
</script>

<style lang="scss">
.payment-form {
  padding: 20px 22px 36px;
}

.payment-card {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}
</style>
