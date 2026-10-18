FROM node:26-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm,sharing=locked npm ci
COPY . .
# .svelte-kit/ is dockerignored; regenerate it (tsconfig.json extends its tsconfig)
RUN npx svelte-kit sync && npm run build

FROM node:26-alpine
WORKDIR /app
ENV NODE_ENV=production
COPY package.json package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm,sharing=locked npm ci --omit=dev
COPY --from=builder /app/build ./
USER node
EXPOSE 3000
CMD ["node", "index.js"]
