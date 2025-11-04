# Build stage
FROM node:18-alpine as build-stage

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage - just store the built files
FROM busybox:latest

# Copy built files
COPY --from=build-stage /app/dist /dist

# Keep container running (files will be accessed via volume)
CMD ["sh", "-c", "tail -f /dev/null"]