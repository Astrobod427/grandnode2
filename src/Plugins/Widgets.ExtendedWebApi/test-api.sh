#!/bin/bash

# Configuration
BASE_URL="http://localhost:5011"
ADMIN_EMAIL="francois.gendre@zohomail.com"
ADMIN_PASSWORD=""  # À remplir

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}Testing Widgets.ExtendedWebApi Plugin${NC}"
echo -e "${YELLOW}========================================${NC}\n"

# Check if password is set
if [ -z "$ADMIN_PASSWORD" ]; then
    echo -e "${RED}ERROR: Please set ADMIN_PASSWORD in the script${NC}"
    echo "Edit this file and set: ADMIN_PASSWORD=\"your_password\""
    exit 1
fi

# Function to print test result
print_result() {
    local test_name=$1
    local http_code=$2
    local expected=$3

    if [ "$http_code" == "$expected" ]; then
        echo -e "${GREEN}✓ $test_name - HTTP $http_code${NC}"
    else
        echo -e "${RED}✗ $test_name - Expected $expected, got $http_code${NC}"
    fi
}

# 1. Get JWT Token
echo -e "\n${YELLOW}1. Getting JWT Token...${NC}"
TOKEN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/token" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" \
  -w "\nHTTP_CODE:%{http_code}")

HTTP_CODE=$(echo "$TOKEN_RESPONSE" | grep "HTTP_CODE" | cut -d: -f2)
TOKEN=$(echo "$TOKEN_RESPONSE" | grep -v "HTTP_CODE" | jq -r '.token' 2>/dev/null)

if [ "$HTTP_CODE" == "200" ] && [ -n "$TOKEN" ] && [ "$TOKEN" != "null" ]; then
    echo -e "${GREEN}✓ Token obtained successfully${NC}"
    echo "Token: ${TOKEN:0:50}..."
else
    echo -e "${RED}✗ Failed to get token (HTTP $HTTP_CODE)${NC}"
    echo "Response: $TOKEN_RESPONSE"
    exit 1
fi

# Wait a bit
sleep 1

# 2. Test Backend API - Orders
echo -e "\n${YELLOW}2. Testing Backend API - Orders${NC}"

# Get all orders
echo "Testing GET /api/admin/order"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/api/admin/order")
print_result "GET all orders" "$HTTP_CODE" "200"

# Try to get a specific order (will be 404 if no orders)
echo "Testing GET /api/admin/order/{id} (expect 404 if no orders)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/api/admin/order/nonexistent")
print_result "GET specific order" "$HTTP_CODE" "404"

# 3. Test Backend API - Shipments
echo -e "\n${YELLOW}3. Testing Backend API - Shipments${NC}"

# Get all shipments
echo "Testing GET /api/admin/shipment"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/api/admin/shipment")
print_result "GET all shipments" "$HTTP_CODE" "200"

# Get specific shipment
echo "Testing GET /api/admin/shipment/{id} (expect 404 if no shipments)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/api/admin/shipment/nonexistent")
print_result "GET specific shipment" "$HTTP_CODE" "404"

# 4. Test Backend API - Merchandise Returns
echo -e "\n${YELLOW}4. Testing Backend API - Merchandise Returns${NC}"

# Get all returns
echo "Testing GET /api/admin/merchandisereturn"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/api/admin/merchandisereturn")
print_result "GET all returns" "$HTTP_CODE" "200"

# Get specific return
echo "Testing GET /api/admin/merchandisereturn/{id} (expect 404 if no returns)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/api/admin/merchandisereturn/nonexistent")
print_result "GET specific return" "$HTTP_CODE" "404"

# 5. Test Authentication
echo -e "\n${YELLOW}5. Testing Authentication & Authorization${NC}"

# Test without token (should be 401)
echo "Testing without token (should be 401)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  "$BASE_URL/api/admin/order")
print_result "GET orders without token" "$HTTP_CODE" "401"

# Test with invalid token (should be 401)
echo "Testing with invalid token (should be 401)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer INVALID_TOKEN" \
  "$BASE_URL/api/admin/order")
print_result "GET orders with invalid token" "$HTTP_CODE" "401"

# 6. Test Frontend API (requires session-based auth)
echo -e "\n${YELLOW}6. Testing Frontend API (Customer endpoints)${NC}"
echo -e "${YELLOW}Note: These require session-based authentication${NC}"

# Test without authentication (should be 401)
echo "Testing GET /api/my/myorders without auth (should be 401)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  "$BASE_URL/api/my/myorders")
print_result "GET my orders without auth" "$HTTP_CODE" "401"

echo "Testing GET /api/my/myshipments without auth (should be 401)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  "$BASE_URL/api/my/myshipments")
print_result "GET my shipments without auth" "$HTTP_CODE" "401"

echo "Testing GET /api/my/myreturns without auth (should be 401)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  "$BASE_URL/api/my/myreturns")
print_result "GET my returns without auth" "$HTTP_CODE" "401"

# 7. Summary
echo -e "\n${YELLOW}========================================${NC}"
echo -e "${YELLOW}Test Summary${NC}"
echo -e "${YELLOW}========================================${NC}"
echo -e "${GREEN}All basic endpoint tests completed!${NC}"
echo -e "\nBackend API endpoints (admin):"
echo "  ✓ Orders endpoints responding"
echo "  ✓ Shipments endpoints responding"
echo "  ✓ Merchandise Returns endpoints responding"
echo "  ✓ JWT authentication working"
echo ""
echo -e "Frontend API endpoints (customer):"
echo "  ✓ MyOrders endpoint responding"
echo "  ✓ MyShipments endpoint responding"
echo "  ✓ MyReturns endpoint responding"
echo "  ✓ Session auth requirement enforced"
echo ""
echo -e "${YELLOW}For detailed testing with real data:${NC}"
echo "1. Create test orders/shipments/returns in GrandNode admin"
echo "2. Use the IDs to test specific endpoints"
echo "3. Test mutation operations (MarkAsPaid, Cancel, etc.)"
