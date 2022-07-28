FROM node:lts as builder
# Create app directory
WORKDIR /app
# Install app dependencies
COPY app/ .
RUN npm install
RUN npm run build
ENTRYPOINT [ "node", "/app/build/<RunFileName>.js" ]