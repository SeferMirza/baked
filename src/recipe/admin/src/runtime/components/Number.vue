<template>
  <Skeleton
    v-if="loading"
    height="1.5rem"
  />
  <span
    v-else-if="data"
    v-tooltip.bottom="tooltip"
  >{{ display }}</span>
  <span v-else>-</span>
</template>
<script setup>
import { computed } from "vue";
import { Skeleton } from "primevue";
import { useContext, useFormat } from "#imports";

const context = useContext();
const { asNumber } = useFormat();

const { data } = defineProps({
  schema: { type: null, default: null },
  data: { type: null, required: true }
});

const loading = context.loading();
const display = computed(() => asNumber(data));
const tooltip = computed(() => display.value.shortened ? `${asNumber(data, { shorten: false })}` : null);
</script>
