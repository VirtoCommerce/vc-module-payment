<template>
  <VcBlade
    :title="$t('PAYMENT.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      :items="rows"
      :state-key="'PAYMENT_METHODS'"
      :active-item-id="activeItemId"
      :show-all-columns="false"
      :empty-state="{ icon: 'lucide-credit-card', title: $t('PAYMENT.EMPTY') }"
      @row-click="(e: { data: Row }) => openDetails(e.data)"
    >
      <VcColumn
        id="logoUrl"
        :title="$t('PAYMENT.FIELDS.LOGO')"
        type="image"
        width="72px"
        always-visible
      />
      <VcColumn
        id="displayName"
        :title="$t('PAYMENT.FIELDS.NAME')"
        always-visible
      >
        <template #body="{ data: m }">
          <div class="payment-methods__name">{{ m.displayName }}</div>
          <div
            v-if="m.displayDescription"
            class="payment-methods__description"
          >
            {{ m.displayDescription }}
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="code"
        :title="$t('PAYMENT.FIELDS.CODE')"
      />
      <VcColumn
        id="priority"
        :title="$t('PAYMENT.FIELDS.PRIORITY')"
        width="90px"
      />
      <VcColumn
        id="isActive"
        :title="$t('PAYMENT.FIELDS.STATUS')"
        always-visible
      >
        <template #body="{ data: m }">
          <div class="tw-inline-flex">
            <VcStatus :variant="m.isActive ? 'success' : 'info'">
              {{ $t(m.isActive ? "PAYMENT.ACTIVE" : "PAYMENT.INACTIVE") }}
            </VcStatus>
          </div>
        </template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { api } from "../api";
import { URL, methodDescription, methodName, type PaymentMethod } from "../methods";
import { loadLegacyTexts } from "../utils";

// The payment methods of a store (param = store id)
defineBlade({
  url: "/payment-methods",
  name: "PaymentMethods",
});

type Row = PaymentMethod & { id: string; displayName: string; displayDescription: string };

const { t } = useI18n({ useScope: "global" });
const { param, openBlade, callParent, exposeToChildren } = useBlade();

const methods = ref<PaymentMethod[]>([]);
const texts = ref<Record<string, unknown>>({});
const activeItemId = ref<string>();

// transient methods (registered, never saved for this store) have no id yet: the code identifies them
const rows = computed<Row[]>(() =>
  methods.value
    .map((m) => ({
      ...m,
      id: (m.id ?? m.code)!,
      displayName: methodName(texts.value, m),
      displayDescription: methodDescription(texts.value, m),
    }))
    .sort((a, b) => (a.priority ?? 0) - (b.priority ?? 0) || a.displayName.localeCompare(b.displayName)),
);

const { loading, action: load } = useAsync(async () => {
  const [result, legacy] = await Promise.all([
    api.post<{ results?: PaymentMethod[] }>(`${URL}/search`, { storeId: param.value, take: 100 }),
    loadLegacyTexts(),
  ]);
  texts.value = legacy;
  methods.value = result.results ?? [];
});

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("PAYMENT.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
]);

function openDetails(row: Row) {
  activeItemId.value = row.id;
  openBlade({
    name: "PaymentMethodDetails",
    param: row.id,
    options: { method: methods.value.find((m) => (m.id ?? m.code) === row.id), storeId: param.value },
    onClose: () => (activeItemId.value = undefined),
  });
}

onMounted(load);

exposeToChildren({
  async reload() {
    await load();
    await callParent("reloadWidgets").catch(() => undefined); // the store blade's widget badges
  },
});
</script>

<style lang="scss">
.payment-methods__name {
  color: var(--neutrals-900);
}
.payment-methods__description {
  color: var(--neutrals-500);
  font-size: 12px;
}
</style>
