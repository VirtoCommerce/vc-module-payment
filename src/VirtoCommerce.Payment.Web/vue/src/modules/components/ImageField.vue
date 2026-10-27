<template>
  <!-- An image URL with preview: upload a file to the platform assets or type the URL -->
  <div class="image-field">
    <VcLabel>{{ label }}</VcLabel>
    <VcImageUpload
      :image="modelValue ? { url: modelValue, name: label } : undefined"
      :loading="uploading"
      @upload="onUpload"
      @remove="emit('update:modelValue', null)"
    />
    <VcInput
      :model-value="modelValue ?? ''"
      placeholder="https://"
      @update:model-value="(v) => emit('update:modelValue', v ? String(v) : null)"
    />
  </div>
</template>

<script setup lang="ts">
import { VcImageUpload, VcInput, VcLabel } from "@vc-shell/framework/ui";
import { ref } from "vue";
import { uploadAsset } from "../api";

const props = defineProps<{
  modelValue?: string | null;
  label: string;
  folder: string;
}>();
const emit = defineEmits<{ "update:modelValue": [value: string | null] }>();

const uploading = ref(false);

async function onUpload(files: FileList) {
  const file = files[0];
  if (!file) return;
  uploading.value = true;
  try {
    emit("update:modelValue", await uploadAsset(file, props.folder));
  } finally {
    uploading.value = false;
  }
}
</script>

<style lang="scss">
.image-field {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
</style>
