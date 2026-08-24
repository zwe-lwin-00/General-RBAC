# @general-rbac/react

React helpers for General RBAC. Peer dependencies: `react` and `react-router-dom`.

```tsx
import { HasPermission, PermissionRoute, RbacProvider, usePermission } from "@general-rbac/react";
```

`HasPermission` only hides UI. The API must still check the same permission.
