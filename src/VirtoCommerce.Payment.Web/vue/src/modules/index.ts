import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule, registerExternalWidget } from "@vc-shell/framework";
import { markRaw } from "vue";
import StorePaymentMethodsWidget from "./widgets/StorePaymentMethodsWidget.vue";

// The AngularJS module's payment methods widget on the store blade ("storeDetail")
registerExternalWidget({
  id: "PaymentStoreMethods",
  title: "Payment methods",
  component: markRaw(StorePaymentMethodsWidget),
  targetBlades: ["StoreDetails"],
  // not on a store that isn't saved yet
  isVisible: (blade) => !!blade?.param,
});

export default defineAppModule({
  blades,
  locales,
});
