<template>
  <div
    v-if="data"
    data-testid="error-page"
    class="p-8"
  >
    <div class="pt-8 space-y-4">
      <Tag
        severity="danger"
        :value="statusCode"
        class="!text-4xl"
      />
      <h1 class="text-6xl">
        {{ l(errorInfo.title) }}
      </h1>
      <div class="text-2xl">
        {{ l(errorInfo.message) }}
      </div>
      <AuthorizedContent>
        <div class="text-2xl">
          {{ l(safeLinksMessage) }}
        </div>
      </AuthorizedContent>
    </div>
    <AuthorizedContent>
      <Divider
        type="dashed"
        class="my-8"
      />
      <div class="grid gap-4 grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-6">
        <Bake
          v-for="(link, i) in safeLinks"
          :key="link.schema.route"
          :name="`links/${i}`"
          :descriptor="link"
        />
      </div>
    </AuthorizedContent>
    <Divider
      type="dashed"
      class="my-8"
    />
    <Message severity="warn">
      <i class="pi pi-exclamation-circle mr-2" />
      {{ l(footerInfo) }}
    </Message>
  </div>
</template>
<script setup>
import { computed } from "vue";
import { Divider, Message, Tag } from "primevue";
import { useLocalization } from "#imports";

const { localize: l } = useLocalization();

const { schema, data } = defineProps({
  schema: { type: null, required: true },
  data: { type: null, default: null }
});

const { errorInfos, footerInfo, safeLinks, safeLinksMessage } = schema;

const statusCode = computed(() => data.value?.data?.status ?? data.value?.statusCode ?? 500);
const errorInfo = computed(() => errorInfos[`${statusCode.value}`] ?? errorInfos["500"]);
</script>
