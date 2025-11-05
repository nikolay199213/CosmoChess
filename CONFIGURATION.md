# Configuration Reference

This document describes important configuration details for CosmoChess application.

## Critical Configuration Points

### 1. SignalR Hub Path

**Must be consistent across all configurations:**

- **Backend** ([Program.cs:91](backend/CosmoChess.Api/Program.cs#L91)):
  ```csharp
  app.MapHub<GameHub>("/api/gamehub");
  ```

- **Nginx** ([nginx.conf:41](nginx/nginx.conf#L41)):
  ```nginx
  location /api/gamehub {
      proxy_pass http://backend:5000/api/gamehub;
  }
  ```

- **Frontend**: Client connects to `/api/gamehub`

### 2. Middleware Order (Backend)

**Critical order in Program.cs:**

```csharp
app.UseAuthentication();  // MUST be before UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/api/gamehub");
```

### 3. HTTPS Redirection

**Development only** - disabled in production:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

In production, nginx handles SSL/TLS termination.

### 4. WebSocket Headers

**Only for SignalR endpoint** in nginx.conf:

```nginx
# ✅ Correct - /api/gamehub ONLY
location /api/gamehub {
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
}

# ❌ Wrong - DO NOT add to / or /api/
location / {
    # No Upgrade headers here
}
```

### 5. Environment Variables

#### Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cosmochess;..."
  },
  "Jwt": {
    "Key": "development-key",
    "Issuer": "CosmoChess",
    "Audience": "CosmoChess"
  }
}
```

#### Production (docker-compose.prod.yml)
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - DB_CONNECTION_STRING=Host=postgres;Database=cosmochess;...
  - JWT_KEY=production-secret-key
```

**Note**: Use `DB_CONNECTION_STRING` (not `ConnectionStrings__DefaultConnection`) in production environment variables.

### 6. Frontend Development Proxy

**vite.config.js** - for local development only:

```javascript
proxy: {
  '/api': {
    target: 'http://localhost:5000',
    changeOrigin: true
  }
}
```

This is **not used** in production (Docker build).

### 7. Nginx Buffer Sizes

Increased to prevent connection resets:

```nginx
proxy_buffer_size 128k;
proxy_buffers 8 128k;
proxy_busy_buffers_size 256k;
client_max_body_size 50M;
```

### 8. Database Migrations

**Automatic** in production:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CosmoChessDbContext>();
    await context.Database.MigrateAsync();
}
```

Runs on every application startup.

## Docker Configuration

### Network

All containers must be on the same Docker network:

```yaml
networks:
  cosmochess-network:
```

### Port Exposure

- **Frontend**: `expose: 8080` (internal only)
- **Backend**: `expose: 5000` (internal only)
- **Nginx**: `ports: 80:80` (public)
- **PostgreSQL**: `ports: 5432:5432` (can be internal only in production)

### Container Names

Used for internal DNS resolution:

- `cosmochess-frontend`
- `cosmochess-backend`
- `cosmochess-postgres`
- `cosmochess-nginx`

## Common Mistakes to Avoid

### ❌ Don't Do This:

1. **Adding WebSocket headers to non-WebSocket endpoints**
   ```nginx
   # Wrong!
   location / {
       proxy_set_header Upgrade $http_upgrade;
       proxy_set_header Connection "upgrade";
   }
   ```

2. **Using HTTPS redirect in production backend**
   ```csharp
   // Wrong!
   app.UseHttpsRedirection();  // Should be dev-only
   ```

3. **Wrong middleware order**
   ```csharp
   // Wrong!
   app.UseAuthorization();
   app.UseAuthentication();  // Too late!
   ```

4. **Mismatched SignalR paths**
   ```csharp
   // Backend: /gameHub
   // Nginx: /api/gamehub  ❌ Mismatch!
   ```

5. **Using ConnectionStrings__DefaultConnection in Docker environment**
   ```yaml
   # Wrong!
   environment:
     - ConnectionStrings__DefaultConnection=...
   # Use DB_CONNECTION_STRING instead
   ```

### ✅ Do This:

1. Keep SignalR paths consistent everywhere
2. Use environment-specific HTTPS redirection
3. Maintain correct middleware order
4. Apply WebSocket headers only to WebSocket endpoints
5. Use proper environment variable names

## Testing Configuration

### Backend
```bash
cd backend/CosmoChess.Api
dotnet run
# Check: http://localhost:5000/swagger
```

### Frontend Development
```bash
cd frontend
npm run dev
# Check: http://localhost:8080
```

### Full Docker Stack
```bash
docker-compose up --build
# Check: http://localhost
```

### Production-like
```bash
docker-compose -f docker-compose.prod.yml up -d
# Check: http://localhost
```

## Environment-Specific Behavior

| Feature | Development | Production |
|---------|-------------|------------|
| Swagger UI | ✅ Enabled | ❌ Disabled |
| HTTPS Redirect | ✅ Enabled | ❌ Disabled |
| Database Migrations | Manual | Automatic |
| Vite Proxy | ✅ Used | ❌ Not used |
| Docker Images | Built locally | Pulled from GHCR |
| Environment | Development | Production |

## Troubleshooting Checklist

- [ ] SignalR paths match in backend, nginx, and frontend
- [ ] Backend middleware order is correct
- [ ] WebSocket headers only on `/api/gamehub`
- [ ] HTTPS redirect disabled in production
- [ ] Environment variables set correctly
- [ ] All containers in same Docker network
- [ ] Database connection string valid
- [ ] JWT_KEY is set in production
