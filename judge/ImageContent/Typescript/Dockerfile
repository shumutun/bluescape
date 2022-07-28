FROM node:16-alpine AS builder
WORKDIR /usr/src/app
COPY app/package*.json .
RUN npm ci
COPY COPY app/ .
RUN npm run build

FROM node:16-alpine
ENV NODE_ENV=production
RUN apk add --no-cache tini
WORKDIR /usr/src/app
RUN chown node:node .
USER node
COPY package*.json .
RUN npm install
COPY --from=builder /usr/src/app/build/ lib/
EXPOSE 3000
ENTRYPOINT [ "/sbin/tini","--", "node", "lib/<RunFileName>" ]