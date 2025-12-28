# H??ng d?n c?u hình Supabase S3 Storage

## 1. T?o Bucket trong Supabase

1. ??ng nh?p vào [Supabase Dashboard](https://supabase.com/dashboard)
2. Ch?n project c?a b?n
3. Vào **Storage** t? menu bên trái
4. Click **New bucket**
5. Tên bucket: `images`
6. Ch?n **Public bucket** ?? cho phép truy c?p công khai
7. Click **Create bucket**

## 2. L?y S3 Credentials

1. Trong Supabase Dashboard, vào **Settings** > **API**
2. Cu?n xu?ng ph?n **S3 Access Keys**
3. Click **Generate new S3 access key**
4. L?u l?i:
   - **Access Key ID** 
   - **Secret Access Key** (ch? hi?n th? m?t l?n!)

## 3. Xác ??nh S3 Endpoint

S3 Endpoint c?a b?n có format:
```
https://{PROJECT_ID}.storage.supabase.co/storage/v1/s3
```

V?i project ID `wrrlgzyxojhlgwpunpud`, endpoint là:
```
https://wrrlgzyxojhlgwpunpud.storage.supabase.co/storage/v1/s3
```

## 4. C?u hình Application

### Cách 1: Environment Variables (Khuy?n ngh? cho production)

```powershell
# Windows PowerShell
$env:SUPABASE_PROJECT_URL = "https://wrrlgzyxojhlgwpunpud.supabase.co"
$env:SUPABASE_S3_ENDPOINT = "https://wrrlgzyxojhlgwpunpud.storage.supabase.co/storage/v1/s3"
$env:SUPABASE_ACCESS_KEY_ID = "your-access-key-id"
$env:SUPABASE_SECRET_ACCESS_KEY = "your-secret-access-key"
$env:SUPABASE_BUCKET_NAME = "images"
```

```bash
# Linux/macOS
export SUPABASE_PROJECT_URL="https://wrrlgzyxojhlgwpunpud.supabase.co"
export SUPABASE_S3_ENDPOINT="https://wrrlgzyxojhlgwpunpud.storage.supabase.co/storage/v1/s3"
export SUPABASE_ACCESS_KEY_ID="your-access-key-id"
export SUPABASE_SECRET_ACCESS_KEY="your-secret-access-key"
export SUPABASE_BUCKET_NAME="images"
```

### Cách 2: appsettings.json (Development only)

Thêm vào `appsettings.Development.json`:

```json
{
  "SupabaseStorage": {
    "ProjectUrl": "https://wrrlgzyxojhlgwpunpud.supabase.co",
    "S3Endpoint": "https://wrrlgzyxojhlgwpunpud.storage.supabase.co/storage/v1/s3",
    "AccessKeyId": "your-access-key-id",
    "SecretAccessKey": "your-secret-access-key",
    "BucketName": "images"
  }
}
```

?? **KHÔNG commit file này n?u ch?a credentials!**

### Cách 3: User Secrets (Development - An toàn h?n)

```bash
dotnet user-secrets init
dotnet user-secrets set "SupabaseStorage:AccessKeyId" "your-access-key-id"
dotnet user-secrets set "SupabaseStorage:SecretAccessKey" "your-secret-access-key"
```

## 5. C?u hình Bucket Policies (N?u c?n)

N?u bucket không public, b?n c?n c?u hình RLS policies:

```sql
-- Cho phép ??c public
CREATE POLICY "Public Access" ON storage.objects 
FOR SELECT USING (bucket_id = 'images');

-- Cho phép authenticated users upload
CREATE POLICY "Authenticated Upload" ON storage.objects 
FOR INSERT WITH CHECK (bucket_id = 'images');
```

## 6. C?u trúc th? m?c trong Bucket

Khi upload, files s? ???c t? ch?c nh? sau:
```
images/
??? product-variants/
?   ??? product-1/
?   ?   ??? product_1_red_42_20240115_123456.jpg
?   ?   ??? product_1_blue_43_20240115_124000.jpg
?   ??? product-2/
?       ??? ...
??? avatars/
?   ??? user_123_20240115.jpg
??? other/
    ??? ...
```

## 7. URLs sau khi Upload

Files sau khi upload s? có URL format:
```
https://{PROJECT_ID}.supabase.co/storage/v1/object/public/{BUCKET}/{path}
```

Ví d?:
```
https://wrrlgzyxojhlgwpunpud.supabase.co/storage/v1/object/public/images/product-variants/product-1/image.jpg
```

## 8. Test Upload

```csharp
// Trong controller ho?c service
public async Task<IActionResult> TestUpload(IFormFile file)
{
    var result = await _storageService.UploadFileAsync(file, "test");
    
    if (result.Success)
    {
        return Ok(new { url = result.Url });
    }
    
    return BadRequest(result.ErrorMessage);
}
```

## 9. Fallback Behavior

N?u Supabase credentials không ???c c?u hình:
- Application s? t? ??ng fallback v? local file storage
- Files ???c l?u trong `wwwroot/images/`
- Không c?n thay ??i code

## 10. Troubleshooting

### Error: "Access Denied"
- Ki?m tra Access Key ID và Secret Access Key
- ??m b?o bucket policy cho phép upload

### Error: "Bucket not found"
- Ki?m tra tên bucket (`images`)
- ??m b?o bucket ?ã ???c t?o trong Supabase

### Error: "Request timeout"
- Ki?m tra k?t n?i internet
- Ki?m tra S3 endpoint URL

### Files không hi?n th?
- ??m b?o bucket là public
- Ki?m tra URL format trong database

## 11. Security Best Practices

1. **Không bao gi?** commit credentials lên public repository
2. S? d?ng environment variables cho production
3. Rotate access keys ??nh k?
4. S? d?ng presigned URLs cho sensitive files
5. Set file size limits phù h?p
6. Validate file types tr??c khi upload
