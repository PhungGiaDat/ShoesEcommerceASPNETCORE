# ?? Quick Testing Guide

## Test Add to Cart Functionality

### 1. Test on Product Index Page
```
URL: https://localhost:7085/Product/Index
OR:  https://localhost:7085/san-pham

Steps:
1. Navigate to products page
2. Find a product with "Thêm vào gi?" button
3. Click the button
4. ? Modal should appear showing variants
5. Select a variant (color + size)
6. Click "Thêm" button
7. ? Toast notification should appear: "?ã thêm s?n ph?m vào gi? hàng!"
8. Go to /Cart/Index or /gio-hang
9. ? Verify product is in cart
```

### 2. Test Stock Validation
```
Steps:
1. Find a product with low stock
2. Add it to cart multiple times
3. ? Should show "Ch? còn X s?n ph?m trong kho" when exceeding stock
4. Try to add out-of-stock variant
5. ? Button should be disabled with "H?t hàng" label
```

### 3. Test Guest vs Logged-in User
```
Guest User:
1. Clear cookies/use incognito
2. Add items to cart
3. ? Items saved in session
4. Close browser
5. Reopen ? ? Cart should be empty (session expired)

Logged-in User:
1. Login first
2. Add items to cart
3. Logout and login again
4. ? Cart items should persist
```

---

## Test SEO Features

### 1. Test Sitemap
```bash
# Access sitemap
https://localhost:7085/sitemap.xml

? Should return XML with:
- Homepage
- Product pages with SEO-friendly URLs (/san-pham/product-name-id)
- Category pages
- Brand pages
- Static pages
```

### 2. Test Robots.txt
```bash
# Access robots.txt
https://localhost:7085/robots.txt

? Should show:
User-agent: *
Allow: /
Disallow: /Admin/
...
Sitemap: https://shoes-ecommerce.onrender.com/sitemap.xml
```

### 3. Test Meta Tags
```
View Page Source on Homepage:
? Title: 50-60 characters
? Description: 140-160 characters  
? Canonical URL present
? Open Graph tags present
? Schema.org structured data present
```

### 4. Test H1 Heading
```
Homepage H1:
? Should be ~55 characters: "Giày Dép Chính Hãng - NHTP ShoesEcommerce"
? Should NOT be 96 characters (old version)
```

---

## Test Subiz Live Chat

### 1. Verify Chat Widget Loads
```
Steps:
1. Go to any page (homepage, products, etc.)
2. Wait 2-3 seconds
3. ? Subiz chat bubble should appear in bottom-right corner
4. Click the bubble
5. ? Chat window should open
```

### 2. Verify User Identification
```
For Logged-in Users:
1. Login to the site
2. Open browser console (F12)
3. Type: localStorage
4. ? Should see Subiz data with your name/email

For Admins:
? userType should be set to 'admin'

For Customers:
? userType should be set to 'customer'
```

### 3. Disable Chat (if needed)
```json
// In appsettings.json
"SubizChat": {
    "AccountId": "acsnanotvuacovnpouky",
    "Enabled": false  // ? Set to false
}
```

---

## Test API Endpoints

### 1. Get Product Variants
```javascript
// Open browser console on product page
fetch('/Product/GetVariants/1')
  .then(r => r.json())
  .then(data => console.log(data));

? Should return array of variants with:
- id, productId, color, size, price
- stockQuantity, availableQuantity, isInStock
```

### 2. Add to Cart
```javascript
// Get anti-forgery token
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

// Add to cart
fetch('/Cart/AddToCart', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'RequestVerificationToken': token
  },
  body: JSON.stringify({ productVariantId: 1, quantity: 1 })
})
.then(r => r.json())
.then(data => console.log(data));

? Should return: { success: true, message: "..." }
```

---

## Browser Compatibility

Test on:
- ? Chrome/Edge (Latest)
- ? Firefox (Latest)
- ? Safari (Latest)
- ? Mobile Chrome/Safari

---

## Performance Checks

### 1. Page Load Speed
```
Use Chrome DevTools ? Network tab
? First paint: < 2 seconds
? Interactive: < 3 seconds
? Fully loaded: < 5 seconds
```

### 2. AJAX Response Time
```
Open Network tab
Click "Thêm vào gi?"
? GetVariants call: < 500ms
? AddToCart call: < 1 second
```

---

## Common Issues & Solutions

### Issue: Modal doesn't open
```
Solution:
1. Check console for JavaScript errors
2. Verify Bootstrap 5 is loaded
3. Check if data-product-id attribute exists
```

### Issue: "Add to cart" fails with 400
```
Solution:
1. Check anti-forgery token exists
2. Verify JSON payload is correct
3. Check ProductVariantId is valid integer
```

### Issue: Toast doesn't show
```
Solution:
1. Check showToast() function exists
2. Verify z-index is high enough (9999+)
3. Check for CSS conflicts
```

### Issue: Sitemap is empty
```
Solution:
1. Check database has products
2. Verify Product table has data
3. Check Categories and Brands tables
```

---

## Testing URLs

```
Homepage:
https://localhost:7085/
https://localhost:7085/Home/Index

Products:
https://localhost:7085/Product/Index
https://localhost:7085/san-pham
https://localhost:7085/product

Product Details:
https://localhost:7085/san-pham/giay-nike-air-max-90-5
https://localhost:7085/Product/Details/5

Cart:
https://localhost:7085/Cart/Index
https://localhost:7085/gio-hang

Promotions:
https://localhost:7085/khuyen-mai
https://localhost:7085/Product/DiscountedProducts

SEO:
https://localhost:7085/sitemap.xml
https://localhost:7085/robots.txt
```

---

## Database Verification

```sql
-- Check carts exist
SELECT * FROM "Carts" ORDER BY "UpdatedAt" DESC;

-- Check cart items
SELECT ci.*, pv."Color", pv."Size", p."Name"
FROM "CartItems" ci
JOIN "ProductVariants" pv ON ci."ProductVarientId" = pv."Id"
JOIN "Products" p ON pv."ProductId" = p."Id";

-- Check product variants
SELECT pv.*, p."Name" as ProductName
FROM "ProductVariants" pv
JOIN "Products" p ON pv."ProductId" = p."Id"
WHERE pv."StockQuantity" > 0;
```

---

## Success Criteria

### Add to Cart ?
- [x] Modal opens on button click
- [x] Variants load correctly
- [x] Stock validation works
- [x] Toast notifications appear
- [x] Items appear in cart
- [x] Works for guests and logged-in users

### SEO ?
- [x] Sitemap.xml accessible and valid
- [x] Robots.txt valid syntax
- [x] Meta titles optimized (50-60 chars)
- [x] Meta descriptions optimized (140-160 chars)
- [x] H1 headings optimized (max 70 chars)
- [x] Canonical URLs present and clean
- [x] Structured data present

### Subiz Chat ?
- [x] Widget loads on all pages
- [x] User identification works
- [x] Can be enabled/disabled via config
- [x] No JavaScript errors

---

## ?? Final Checklist

Before deploying to production:
- [ ] All tests pass
- [ ] No console errors
- [ ] Mobile responsive
- [ ] Cross-browser compatible
- [ ] SEO score improved
- [ ] Database backups configured
- [ ] Error logging enabled
- [ ] SSL certificate installed
- [ ] Production config updated
- [ ] Performance optimized

---

## ?? You're Done!

If all tests pass, your implementation is complete and ready for production!

For any issues, refer to IMPLEMENTATION_GUIDE.md or check the server logs.
