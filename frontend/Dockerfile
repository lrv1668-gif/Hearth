FROM node:26-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:26-alpine
WORKDIR /app
COPY --from=builder /app/build ./
COPY --from=builder /app/package.json ./
RUN npm install --omit=dev
EXPOSE 3000
ENV NODE_ENV=production
CMD ["node", "index.js"]
