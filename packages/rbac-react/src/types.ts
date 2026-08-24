export type RbacUser = {
  id: string;
  externalId: string;
  username: string;
  displayName: string;
  email?: string | null;
  isActive: boolean;
  roles: string[];
};

export type RbacMenu = {
  id: string;
  parentId?: string | null;
  programId?: string | null;
  code: string;
  name: string;
  displayName: string;
  route?: string | null;
  icon?: string | null;
  menuType: string;
  sortOrder: number;
  isVisible: boolean;
  isActive: boolean;
  children: RbacMenu[];
};

export type RbacSnapshot = {
  user: RbacUser;
  permissions: string[];
  menus: RbacMenu[];
};
