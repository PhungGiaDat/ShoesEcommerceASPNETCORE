# SEO & Render Deployment Guide for ShoesEcommerce

## Part 1: SEO-Friendly URLs Implementation

### Overview
This implementation adds URL-friendly (slug) support for better SEO and user experience.

### Features Added

#### 1. SlugHelper Utility Class (`Helpers/SlugHelper.cs`)
- Converts titles to URL-friendly slugs
- Full Vietnamese character support (?, ?, â, ê, ô, ?, ?, etc.)
- Handles special characters (&, @, #, etc.)
- Removes diacritical marks
- Generates unique slugs with IDs

#### 2. SEO-Friendly Routes

| Page | Vietnamese URL | English URL | Original URL |
|------|---------------|-------------|--------------|
| Product List | `/san-pham` | `/product` | `/Product` |
| Product Detail | `/san-pham/{slug}` | `/product/{slug}` | `/Product/Details/{id}` |
| Discounts | `/khuyen-mai` | `/discounted-products` | `/Product/DiscountedProducts` |
| Cart | `/gio-hang` | - | `/Cart` |
| Checkout | `/thanh-toan` | - | `/Checkout` |
| Login | `/dang-nhap` | - | `/Account/Login` |
| Register | `/dang-ky` | - | `/Account/Register` |
| Orders | `/don-hang` | - | `/Order` |
| Order Detail | `/don-hang/{id}` | - | `/Order/Details/{id}` |

### URL Examples

```
# Original URLs (still work for backward compatibility)
/Product/Details/5

# New SEO-friendly URLs
/san-pham/giay-nike-air-max-90-5
/product/nike-air-max-90-shoes-5

# Automatic redirect: /Product/Details/5 ? /san-pham/giay-nike-air-max-90-5
```

### Usage in Views

```razor
@using ShoesEcommerce.Helpers

<!-- Generate SEO-friendly link -->
<a href="/san-pham/@product.Name.ToSlugWithId(product.Id)">
    @product.Name
</a>

<!-- Or using Url.Action -->
<a href="@Url.Action("Details", "Product", new { slug = product.Name.ToSlugWithId(product.Id) })">
    @product.Name
</a>
```

### Usage in JavaScript/API

```javascript
// Product variants now include slug
fetch('/Product/Search?term=nike')
  .then(r => r.json())
  .then(products => {
    products.forEach(p => {
      console.log(`URL: /san-pham/${p.slug}`);
    });
  });
```

---

## Part 2: Render Deployment

### Prerequisites
1. GitHub repository with your code
2. Render account (https://render.com)
3. PostgreSQL database (Supabase or Render-managed)

### Deployment Steps

#### Step 1: Push to GitHub
```bash
git add .
git commit -m "Add SEO-friendly URLs and Render deployment"
git push origin main
```

#### Step 2: Create Render Web Service

1. Go to https://dashboard.render.com
2. Click "New" ? "Web Service"
3. Connect your GitHub repository
4. Configure:
   - **Name**: `shoes-ecommerce`
   - **Region**: Singapore (or closest to your users)
   - **Branch**: `main`
   - **Runtime**: Docker
   - **Dockerfile Path**: `./ShoesEcommerce/Dockerfile`
   - **Docker Context**: `./ShoesEcommerce`

#### Step 3: Set Environment Variables

In Render Dashboard ? Environment:

```bash
# Required
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:10000

# Database (Supabase PostgreSQL)
ConnectionStrings__DefaultConnection=Host=xxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;Trust Server Certificate=true

# PayPal (if using)
PayPalOptions__ClientId=your_client_id
PayPalOptions__ClientSecret=your_client_secret
PayPalOptions__Mode=Sandbox  # or "Live" for production

# VNPay (if using)
VNPay__TmnCode=your_tmn_code
VNPay__HashSecret=your_hash_secret
VNPay__Url=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
VNPay__ReturnUrl=https://your-app.onrender.com/Payment/VnPayReturn
```

#### Step 4: Deploy

1. Click "Create Web Service"
2. Wait for build (5-10 minutes)
3. Check logs for any errors
4. Access your app at `https://your-app.onrender.com`

### File Structure for Deployment

```
ShoesEcommerce/
??? Dockerfile              # Docker build instructions
??? .dockerignore          # Files to exclude from Docker
??? render.yaml            # Render blueprint (optional)
??? ShoesEcommerce.csproj  # Project file
??? Program.cs             # Entry point with routing
??? Helpers/
?   ??? SlugHelper.cs      # SEO slug utilities
??? ...
```

### Health Check

The app includes a health check endpoint:
- **URL**: `/health`
- **Response**: `{ "status": "healthy", "timestamp": "...", "environment": "Production" }`

### Troubleshooting

#### Build Fails
1. Check Dockerfile path is correct
2. Verify .csproj file name matches
3. Check logs for NuGet restore errors

#### Database Connection Fails
1. Verify connection string format
2. Ensure SSL is enabled for Supabase
3. Check firewall rules

#### 502 Bad Gateway
1. Check application logs
2. Verify ASPNETCORE_URLS=http://+:10000
3. Check health endpoint

### Performance Tips

1. **Static Files**: Consider using a CDN for images
2. **Caching**: Enable response caching for product pages
3. **Database**: Add indexes for frequently queried columns
4. **Logging**: Set log level to Warning in production

---

## Part 3: SEO Best Practices

### Meta Tags (Add to Views)

```razor
@{
    ViewData["Title"] = Model.Name;
    ViewData["MetaDescription"] = Model.Description;
    ViewData["CanonicalUrl"] = $"https://yoursite.com/san-pham/{Model.GetSlug()}";
}
```

### robots.txt

Create `wwwroot/robots.txt`:
```
User-agent: *
Allow: /
Disallow: /Admin/
Disallow: /Account/
Sitemap: https://yoursite.com/sitemap.xml
```

### Sitemap Generation

Consider adding a sitemap controller that generates XML sitemaps for all products.

---

## Quick Reference

### Generate Slug
```csharp
using ShoesEcommerce.Helpers;

// Basic slug
"Giày Nike Air Max 90".ToSlug(); // ? "giay-nike-air-max-90"

// Slug with ID
"Giày Nike Air Max 90".ToSlugWithId(5); // ? "giay-nike-air-max-90-5"

// Extract ID from slug
SlugHelper.ExtractIdFromSlug("giay-nike-air-max-90-5"); // ? 5
```

### Route Attributes
```csharp
[Route("san-pham/{slug}")]
[Route("product/{slug}")]
public async Task<IActionResult> Details(string slug) { ... }
```

---

## Support

For issues:
1. Check Render logs
2. Test locally with `dotnet run`
3. Verify environment variables
4. Check database connectivity
