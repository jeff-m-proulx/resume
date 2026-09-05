# syntax=docker/dockerfile:1

# Build the static bundle. tsc runs from devDependencies, so this is a full
# `npm ci` rather than an --omit=dev install.
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# Serve it. nginx also reverse-proxies /api/ to the API container, which is why
# the bundle needs no VITE_API_URL baked in at build time.
FROM nginx:alpine AS final

# The nginx entrypoint runs envsubst over /etc/nginx/templates/*.template. The
# filter limits substitution to these two names so nginx's own runtime
# variables ($uri, $proxy_host, $api_upstream) pass through untouched.
# Opt in to 15-local-resolvers.envsh, which reads the container's resolv.conf
# into NGINX_LOCAL_RESOLVERS. Without this it returns early and the template's
# resolver directive is left unsubstituted.
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
ENV NGINX_ENVSUBST_FILTER="^(API_URL|NGINX_LOCAL_RESOLVERS)"

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf.template /etc/nginx/templates/default.conf.template

EXPOSE 80
