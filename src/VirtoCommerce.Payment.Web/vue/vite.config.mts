import path from "node:path";
import { getDynamicModuleConfiguration } from "@vc-shell/mf-module";

// Payment methods of stores: a Module Federation remote of the Admin Shell host.
// Output: <module>/plugins/admin-shell/ (remoteEntry.js + public/plugin.json),
// served by the platform at /modules/$(VirtoCommerce.Payment)/plugins/admin-shell/.
const base = "/modules/$(VirtoCommerce.Payment)/plugins/admin-shell/";

export default getDynamicModuleConfiguration({
  appId: "admin-shell",
  moduleRoot: path.resolve(__dirname, ".."),
  entry: "./src/modules/index.ts",
  remoteName: "payment_admin",
  base,
});
