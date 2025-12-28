# H??ng d?n c?u hình Google OAuth

## 1. T?o Project trên Google Cloud Console

1. Truy c?p [Google Cloud Console](https://console.cloud.google.com/)
2. T?o project m?i ho?c ch?n project hi?n t?i
3. Vào **APIs & Services** > **Credentials**

## 2. T?o OAuth 2.0 Client ID

1. Click **+ CREATE CREDENTIALS** > **OAuth client ID**
2. N?u ch?a c?u hình OAuth consent screen:
   - Ch?n **CONFIGURE CONSENT SCREEN**
   - Ch?n **External** (n?u cho ng??i dùng bên ngoài)
   - ?i?n thông tin:
     - App name: `NHTP Shoes`
     - User support email: email c?a b?n
     - Developer contact: email c?a b?n
   - Click **Save and Continue** cho ??n khi hoàn t?t

3. Quay l?i t?o OAuth client ID:
   - Application type: **Web application**
   - Name: `NHTP Shoes Web Client`
   - Authorized JavaScript origins:
     ```
     https://localhost:7085
     http://localhost:5000
     ```
   - Authorized redirect URIs:
     ```
     https://localhost:7085/signin-google
     http://localhost:5000/signin-google
     ```

4. Click **CREATE**

5. L?u l?i **Client ID** và **Client Secret**

## 3. C?u hình trong Application

### Cách 1: S? d?ng Environment Variables (Khuy?n ngh?)

Trong PowerShell:
```powershell
$env:GOOGLE_CLIENT_ID = "your-client-id.apps.googleusercontent.com"
$env:GOOGLE_CLIENT_SECRET = "your-client-secret"
```

Ho?c trong Command Prompt:
```cmd
set GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
set GOOGLE_CLIENT_SECRET=your-client-secret
```

### Cách 2: S? d?ng appsettings.json

Thêm vào `appsettings.Development.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

?? **L?U Ý**: Không commit file này lên Git n?u ch?a credentials!

## 4. Test Google Login

1. Kh?i ??ng l?i application
2. Truy c?p trang ??ng nh?p: `https://localhost:7085/dang-nhap`
3. Click nút **??ng nh?p v?i Google**
4. ??ng nh?p v?i tài kho?n Google
5. N?u thành công, b?n s? ???c redirect v? trang ch?

## 5. L?u ý Production

Khi deploy lên production:

1. Thêm domain production vào **Authorized JavaScript origins**:
   ```
   https://yourdomain.com
   ```

2. Thêm redirect URI production:
   ```
   https://yourdomain.com/signin-google
   ```

3. C?u hình environment variables trên server:
   - `GOOGLE_CLIENT_ID`
   - `GOOGLE_CLIENT_SECRET`

## 6. Troubleshooting

### Error: "redirect_uri_mismatch"
- Ki?m tra redirect URI trong Google Console kh?p chính xác v?i URL c?a app
- Redirect URI ph?i là: `{BASE_URL}/signin-google`

### Error: "access_denied"
- User ?ã t? ch?i quy?n truy c?p
- Ki?m tra OAuth consent screen ?ã ???c c?u hình ?úng

### Error: "Google OAuth not configured"
- Ki?m tra environment variables ho?c appsettings.json
- Kh?i ??ng l?i application sau khi c?u hình

## 7. Security Best Practices

1. **Không bao gi?** commit Client Secret lên public repository
2. S? d?ng User Secrets cho development:
   ```bash
   dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "your-secret"
   ```
3. S? d?ng environment variables cho production
4. Rotate Client Secret ??nh k?
