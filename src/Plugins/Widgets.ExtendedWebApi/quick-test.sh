#!/bin/bash
# Quick test commands - Copy/paste these one by one

# ====================
# 1. SET YOUR PASSWORD
# ====================
PASSWORD="YOUR_PASSWORD_HERE"
EMAIL="francois.gendre@zohomail.com"

# ====================
# 2. GET JWT TOKEN
# ====================
echo "Getting JWT token..."
TOKEN=$(curl -s -X POST http://localhost:5011/api/token \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" | jq -r '.token')

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Failed to get token. Check your password!"
    exit 1
fi

echo "✅ Token obtained: ${TOKEN:0:50}..."
echo ""

# ====================
# 3. TEST BACKEND ENDPOINTS
# ====================

echo "📦 Testing Orders endpoint..."
curl -s -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/order | jq '.[0:2]'
echo ""

echo "📮 Testing Shipments endpoint..."
curl -s -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/shipment | jq '.[0:2]'
echo ""

echo "↩️  Testing Returns endpoint..."
curl -s -H "Authorization: Bearer $TOKEN" \
  http://localhost:5011/api/admin/merchandisereturn | jq '.[0:2]'
echo ""

# ====================
# 4. TEST AUTHENTICATION
# ====================

echo "🔒 Testing without token (should fail)..."
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  http://localhost:5011/api/admin/order)
echo "Response code: $HTTP_CODE (expected: 401)"
echo ""

# ====================
# 5. GET REAL IDs FROM DATABASE
# ====================

echo "🗄️  Getting real Order IDs from database..."
docker exec mongodb_server_dev mongosh grandnode-data --quiet --eval \
  "db.Order.find({}, {_id: 1, OrderNumber: 1, CustomerEmail: 1}).limit(3)" 2>/dev/null
echo ""

echo "🗄️  Getting real Shipment IDs from database..."
docker exec mongodb_server_dev mongosh grandnode-data --quiet --eval \
  "db.Shipment.find({}, {_id: 1, ShipmentNumber: 1, OrderId: 1}).limit(3)" 2>/dev/null
echo ""

echo "🗄️  Getting real Return IDs from database..."
docker exec mongodb_server_dev mongosh grandnode-data --quiet --eval \
  "db.MerchandiseReturn.find({}, {_id: 1, ReturnNumber: 1, CustomerId: 1}).limit(3)" 2>/dev/null
echo ""

# ====================
# 6. FRONTEND API TEST
# ====================

echo "🌐 Testing Frontend API (login required)..."
curl -s -X POST http://localhost:5011/login \
  -c /tmp/cookies.txt \
  -d "Email=$EMAIL&Password=$PASSWORD&RememberMe=false" > /dev/null

echo "Getting customer orders..."
curl -s -b /tmp/cookies.txt \
  http://localhost:5011/api/my/myorders | jq '.[0:2]'
echo ""

# ====================
# SUMMARY
# ====================

echo "========================================="
echo "✅ All basic tests completed!"
echo "========================================="
echo ""
echo "To test with specific IDs, use:"
echo ""
echo "# Get specific order"
echo "curl -H \"Authorization: Bearer \$TOKEN\" \\"
echo "  http://localhost:5011/api/admin/order/YOUR_ORDER_ID | jq"
echo ""
echo "# Mark order as paid"
echo "curl -X POST -H \"Authorization: Bearer \$TOKEN\" \\"
echo "  http://localhost:5011/api/admin/order/YOUR_ORDER_ID/MarkAsPaid"
echo ""
echo "# Update tracking number"
echo "curl -X POST -H \"Authorization: Bearer \$TOKEN\" \\"
echo "  -H \"Content-Type: application/json\" \\"
echo "  -d '\"TRACK123456\"' \\"
echo "  http://localhost:5011/api/admin/shipment/YOUR_SHIPMENT_ID/SetTrackingNumber"
echo ""
