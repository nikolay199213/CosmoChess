# SSL/HTTPS Setup Guide for CosmoChess

This guide explains how to set up HTTPS for CosmoChess using Let's Encrypt SSL certificates.

## Prerequisites

1. **Domain names** pointing to your server:
   - cosmochess.ru
   - cosmochess.space

2. **DNS configured**: Both domains must resolve to your server's IP address

3. **Ports open**:
   - Port 80 (HTTP) - required for certificate validation
   - Port 443 (HTTPS) - for secure connections

4. **Docker and Docker Compose** installed on your server

## Configuration Files

The SSL setup consists of several components:

### 1. Nginx Configuration Files

- **`nginx/nginx.conf`** - Production configuration with HTTPS
  - HTTP server (port 80): Redirects to HTTPS
  - HTTPS server (port 443): Serves the application with SSL
  - Configured for both domains: cosmochess.ru and cosmochess.space

- **`nginx/nginx.conf.init`** - Initial configuration for obtaining certificates
  - HTTP only (port 80)
  - Used temporarily during certificate generation

### 2. Docker Compose

Both `docker-compose.yml` and `docker-compose.prod.yml` have been updated with:

- **nginx service**:
  - Exposes ports 80 and 443
  - Mounts certificate volumes
  - Auto-reloads every 6 hours to pick up renewed certificates

- **certbot service**:
  - Automatically renews certificates every 12 hours
  - Shares certificate volumes with nginx

### 3. Certificate Storage

Certificates are stored in:
- `nginx/certbot/conf/` - Certificate files
- `nginx/certbot/www/` - ACME challenge files

## Installation Steps

### Step 1: Update Email Address

Edit `init-letsencrypt.sh` and change the email address:

```bash
email="admin@cosmochess.ru"  # Change to your email
```

This email will receive certificate expiry notifications.

### Step 2: Verify DNS Configuration

Make sure both domains point to your server:

```bash
# Check DNS resolution
nslookup cosmochess.ru
nslookup cosmochess.space
```

Both should return your server's IP address.

### Step 3: Run the Initialization Script

**IMPORTANT**: This script will:
1. Create dummy certificates
2. Start nginx with HTTP-only config
3. Request real certificates from Let's Encrypt
4. Switch to HTTPS configuration
5. Reload nginx

```bash
./init-letsencrypt.sh
```

If you're testing, you can enable staging mode to avoid rate limits:
```bash
# Edit init-letsencrypt.sh and change:
staging=1
```

### Step 4: Verify HTTPS is Working

After the script completes, test your sites:

```bash
curl https://cosmochess.ru
curl https://cosmochess.space
```

Visit in browser:
- https://cosmochess.ru
- https://cosmochess.space

You should see a valid SSL certificate with no warnings.

## Automatic Certificate Renewal

Certificates are automatically renewed by the certbot container:
- Checks for renewal every 12 hours
- Renews certificates that expire within 30 days
- Nginx reloads every 6 hours to pick up renewed certificates

No manual intervention required!

## SSL Security Features

The nginx configuration includes:

### TLS Settings
- **Protocols**: TLS 1.2 and TLS 1.3 only
- **Ciphers**: Modern, secure cipher suites
- **Session caching**: 10-minute session timeout

### Security Headers
- `Strict-Transport-Security`: Forces HTTPS for 1 year
- `X-Frame-Options`: Prevents clickjacking
- `X-Content-Type-Options`: Prevents MIME sniffing
- `X-XSS-Protection`: Enables browser XSS protection

### OCSP Stapling
- Improves SSL handshake performance
- Enhances privacy

## Troubleshooting

### Certificate Generation Failed

1. **Check DNS**: Ensure domains resolve to your server
   ```bash
   nslookup cosmochess.ru
   ```

2. **Check ports**: Ensure ports 80 and 443 are accessible
   ```bash
   netstat -tulpn | grep :80
   netstat -tulpn | grep :443
   ```

3. **Check logs**:
   ```bash
   docker logs cosmochess-nginx
   docker logs cosmochess-certbot
   ```

4. **Rate limits**: If you hit Let's Encrypt rate limits, enable staging mode

### Nginx Won't Start

1. **Check nginx config syntax**:
   ```bash
   docker compose exec nginx nginx -t
   ```

2. **Verify certificate files exist**:
   ```bash
   ls -la nginx/certbot/conf/live/cosmochess.ru/
   ```

### Certificate Renewal Not Working

1. **Check certbot logs**:
   ```bash
   docker logs cosmochess-certbot
   ```

2. **Manually test renewal**:
   ```bash
   docker compose run --rm certbot renew --dry-run
   ```

## Manual Certificate Renewal

If needed, you can manually renew certificates:

```bash
docker compose run --rm certbot renew
docker compose exec nginx nginx -s reload
```

## Reverting to HTTP

If you need to temporarily disable HTTPS:

1. Use the init configuration:
   ```bash
   cp nginx/nginx.conf.init nginx/nginx.conf
   ```

2. Restart nginx:
   ```bash
   docker compose restart nginx
   ```

## Important Notes

- **First generation**: The first certificate generation can take 1-2 minutes
- **Rate limits**: Let's Encrypt has rate limits (50 certificates per domain per week)
- **Staging mode**: Use staging mode for testing to avoid rate limits
- **Both domains**: The certificate covers both cosmochess.ru and cosmochess.space
- **Renewal**: Certificates are valid for 90 days and auto-renew at 60 days

## Maintenance

### View Certificate Expiry

```bash
docker compose run --rm certbot certificates
```

### Force Certificate Renewal

```bash
docker compose run --rm certbot renew --force-renewal
docker compose exec nginx nginx -s reload
```

### Update Configuration

After changing `nginx.conf`:

```bash
docker compose exec nginx nginx -t  # Test config
docker compose restart nginx        # Restart nginx
```

## Production Checklist

- [ ] DNS records point to server
- [ ] Firewall allows ports 80 and 443
- [ ] Email address updated in init script
- [ ] Certificates generated successfully
- [ ] HTTPS sites accessible
- [ ] HTTP redirects to HTTPS
- [ ] SSL certificate valid (check in browser)
- [ ] WebSocket connections work over HTTPS
- [ ] Certbot container running for auto-renewal

## Support

For issues related to:
- **Let's Encrypt**: https://letsencrypt.org/docs/
- **Certbot**: https://certbot.eff.org/docs/
- **Nginx SSL**: https://nginx.org/en/docs/http/configuring_https_servers.html
