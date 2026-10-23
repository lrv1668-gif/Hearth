# Headless Chromium for the Display service: it screenshots the /frame route, and
# Display dithers the result. The browser lives here, not in Display's chiseled
# image, because Playwright needs Node and Chromium's system libraries.
# The image tag must match the playwright version in package.json.
FROM mcr.microsoft.com/playwright:v1.63.0-noble
WORKDIR /app
ENV NODE_ENV=production
COPY package.json package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm,sharing=locked npm ci --omit=dev
COPY server.mjs dither.mjs rotate.mjs timezone.mjs ./
USER pwuser
EXPOSE 3000
CMD ["node", "server.mjs"]
