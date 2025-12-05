#!/bin/bash

# Script to initialize Let's Encrypt SSL certificates for CosmoChess

set -e

domains=(cosmochess.ru cosmochess.space)
rsa_key_size=4096
data_path="./nginx/certbot"
email="admin@cosmochess.ru" # Change this to your email
staging=0 # Set to 1 if you're testing your setup to avoid hitting request limits

if [ -d "$data_path/conf/live/${domains[0]}" ]; then
  read -p "Existing certificates found for ${domains[0]}. Continue and replace? (y/N) " decision
  if [ "$decision" != "Y" ] && [ "$decision" != "y" ]; then
    exit
  fi
fi

# Download recommended TLS parameters
if [ ! -e "$data_path/conf/options-ssl-nginx.conf" ] || [ ! -e "$data_path/conf/ssl-dhparams.pem" ]; then
  echo "### Downloading recommended TLS parameters..."
  mkdir -p "$data_path/conf"
  curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf > "$data_path/conf/options-ssl-nginx.conf"
  curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem > "$data_path/conf/ssl-dhparams.pem"
  echo
fi

echo "### Creating dummy certificate for ${domains[0]}..."
path="/etc/letsencrypt/live/${domains[0]}"
mkdir -p "$data_path/conf/live/${domains[0]}"
docker compose run --rm --entrypoint "\
  openssl req -x509 -nodes -newkey rsa:$rsa_key_size -days 1\
    -keyout '$path/privkey.pem' \
    -out '$path/fullchain.pem' \
    -subj '/CN=localhost'" certbot
echo

# Backup current nginx config and use init config
echo "### Backing up nginx.conf and using init config..."
cp nginx/nginx.conf nginx/nginx.conf.backup
cp nginx/nginx.conf.init nginx/nginx.conf

echo "### Starting nginx..."
docker compose up -d nginx
echo

echo "### Deleting dummy certificate for ${domains[0]}..."
docker compose run --rm --entrypoint "\
  rm -Rf /etc/letsencrypt/live/${domains[0]} && \
  rm -Rf /etc/letsencrypt/archive/${domains[0]} && \
  rm -Rf /etc/letsencrypt/renewal/${domains[0]}.conf" certbot
echo

echo "### Requesting Let's Encrypt certificate for ${domains[0]}..."
# Join $domains to -d args
domain_args=""
for domain in "${domains[@]}"; do
  domain_args="$domain_args -d $domain"
done

# Select appropriate email arg
case "$email" in
  "") email_arg="--register-unsafely-without-email" ;;
  *) email_arg="--email $email" ;;
esac

# Enable staging mode if needed
if [ $staging != "0" ]; then staging_arg="--staging"; fi

docker compose run --rm --entrypoint "\
  certbot certonly --webroot -w /var/www/certbot \
    $staging_arg \
    $email_arg \
    $domain_args \
    --rsa-key-size $rsa_key_size \
    --agree-tos \
    --force-renewal" certbot
echo

# Restore full nginx config with HTTPS
echo "### Restoring full nginx.conf with HTTPS..."
cp nginx/nginx.conf.backup nginx/nginx.conf

echo "### Reloading nginx..."
docker compose exec nginx nginx -s reload

echo "### Starting certbot for automatic renewal..."
docker compose up -d certbot

echo "### SSL certificates successfully installed!"
echo "### Your site is now available at:"
for domain in "${domains[@]}"; do
  echo "###   https://$domain"
done
