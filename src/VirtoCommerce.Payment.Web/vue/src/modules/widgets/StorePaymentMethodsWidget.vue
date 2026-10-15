<template>
  <VcWidget
    icon="lucide-credit-card"
    :title="$t('PAYMENT.TITLE')"
    :value="count"
    :disabled="!storeId"
    @click="open"
  />
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { injectBladeContext, useBlade, useWidgetTrigger } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";
import { api } from "../api";
import { URL } from "../methods";

// the store shared by the Store module's blade via defineBladeContext({ item }); the badge counts its active methods
const ctx = injectBladeContext();
const storeId = computed(() => (ctx.value.item as { id?: string } | undefined)?.id);
const { openBlade } = useBlade();
const count = ref<number | string>("");

async function load() {
  if (!storeId.value) return;
  const result = await api.post<{ totalCount?: number }>(`${URL}/search`, { storeId: storeId.value, isActive: true, take: 0 }).catch(() => undefined);
  count.value = result?.totalCount ?? "";
}
watch(storeId, load, { immediate: true });
useWidgetTrigger({ onRefresh: load });

const open = () => openBlade({ name: "PaymentMethods", param: storeId.value });
</script>
