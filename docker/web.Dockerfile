FROM node:22-alpine AS build
WORKDIR /repo

COPY packages/rbac-react packages/rbac-react
COPY samples/Rbac.Sample.React samples/Rbac.Sample.React

WORKDIR /repo/packages/rbac-react
RUN npm ci

WORKDIR /repo/samples/Rbac.Sample.React
RUN npm ci && npm run build

FROM nginx:1.27-alpine AS final
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /repo/samples/Rbac.Sample.React/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=10s --timeout=3s --retries=8 \
    CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
