###################
# BUILD FOR PRODUCTION
###################
FROM node:22-alpine

# Create app directory
WORKDIR /usr/src/app

# Copy application dependency manifests to the container image.
# A wildcard is used to ensure copying both package.json AND package-lock.json (when available).
COPY --chown=node:node package*.json ./

# Install app dependencies 
RUN npm install --only=production

# in CICD pipeline dist is already build and is an artefact
COPY build /usr/src/app/build

# internal port for nestjs server (defined in main.ts)
EXPOSE 3000

# Use the node user from the image (instead of the root user)
USER node

# Start the server using the production build
CMD [ "node", "build/index.js" ]

