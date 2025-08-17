# AI Endpoints Troubleshooting Guide

## 🔍 **Common Issues and Solutions**

### **1. AI Status Shows "Unhealthy"**

**Symptoms:**
- `/api/ai/status` returns `"status": "unhealthy"`
- Error message mentions "Connection refused" or "embeddingService": "unavailable"

**Causes & Solutions:**

#### **A. Ollama Not Running**
```bash
# Check if Ollama is running
curl -s http://localhost:11434/api/tags

# If not running, start it
./start-ollama.sh
```

#### **B. Network Connectivity Issues**
```bash
# Test from host
curl -s http://localhost:11434/api/tags

# Test from API container
docker exec procurement_api curl -s http://host.docker.internal:11434/api/tags
```

#### **C. Model Not Loaded**
```bash
# Check available models
curl -s http://localhost:11434/api/tags | jq .

# Pull the model if missing
curl -X POST http://localhost:11434/api/pull -d '{"name": "nomic-embed-text"}'
```

### **2. Search Endpoints Return Empty Results**

**Symptoms:**
- `/api/ai/search/suppliers` returns empty array
- `/api/ai/search/items` returns empty array
- No error, but no results

**Cause:** No vectorized data in the database

**Solution:**
```bash
# Vectorize supplier data
curl -X POST http://localhost:5001/api/ai/vectorize/suppliers

# Vectorize item data  
curl -X POST http://localhost:5001/api/ai/vectorize/items
```

### **3. Database Connection Issues**

**Symptoms:**
- Database-related errors in logs
- "Connection refused" errors

**Solutions:**
```bash
# Check if database is running
docker ps | grep postgres

# Restart database if needed
./start-db.sh

# Check database connectivity
docker exec procurement_api curl -s http://localhost:5001/health/ready
```

### **4. API Not Responding**

**Symptoms:**
- All endpoints return empty responses
- Connection refused errors

**Solutions:**
```bash
# Check if API container is running
docker ps | grep procurement

# Restart API
./start-api.sh

# Check API logs
docker logs procurement_api --tail 20
```

## 🧪 **Testing Commands**

### **Quick Health Check**
```bash
# Test API health
curl -s http://localhost:5001/health/ready

# Test AI status
curl -s http://localhost:5001/api/ai/status | jq .

# Test AI functionality
curl -s http://localhost:5001/api/ai/test | jq .
```

### **Test Search Endpoints**
```bash
# Test supplier search
curl -s "http://localhost:5001/api/ai/search/suppliers?query=electronics&limit=5" | jq .

# Test item search
curl -s "http://localhost:5001/api/ai/search/items?query=components&limit=5" | jq .
```

### **Vectorize Data**
```bash
# Vectorize suppliers
curl -X POST http://localhost:5001/api/ai/vectorize/suppliers

# Vectorize items
curl -X POST http://localhost:5001/api/ai/vectorize/items
```

## 🔧 **Debugging Steps**

### **Step 1: Check Ollama**
```bash
# Verify Ollama is running and accessible
curl -s http://localhost:11434/api/tags | jq .

# Test embedding generation directly
curl -X POST http://localhost:11434/api/embeddings \
  -H "Content-Type: application/json" \
  -d '{"model": "nomic-embed-text", "prompt": "test"}' | jq .
```

### **Step 2: Check API Container**
```bash
# Check container status
docker ps | grep procurement

# Check API logs
docker logs procurement_api --tail 20

# Test API from container
docker exec procurement_api curl -s http://localhost:8080/health/ready
```

### **Step 3: Check Database**
```bash
# Check database container
docker ps | grep postgres

# Test database connection
docker exec postgres_db psql -U postgres -d procurement -c "SELECT COUNT(*) FROM suppliers;"
```

### **Step 4: Test AI Service**
```bash
# Run the test script
./test-ai-endpoints.sh
```

## 📊 **Expected Results**

### **Healthy System Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-08-04T00:00:00.0000000Z",
  "embeddingService": "available",
  "modelName": "nomic-embed-text",
  "embeddingDimension": 768,
  "details": {
    "embeddingTest": "available",
    "modelLoaded": true
  }
}
```

### **Successful Search Response:**
```json
{
  "suppliers": [...],
  "count": 5
}
```

## 🚨 **Emergency Reset**

If everything is broken, try this complete reset:

```bash
# Stop all services
docker-compose -f docker-compose.api.yml down
docker-compose -f docker-compose.db.yml down

# Start fresh
./start-db.sh
./start-ollama.sh
./start-api.sh

# Wait and test
sleep 10
./test-ai-endpoints.sh
```

## 📞 **Getting Help**

If you're still having issues:

1. **Check the logs:**
   ```bash
   docker logs procurement_api --tail 50
   ```

2. **Run the test script:**
   ```bash
   ./test-ai-endpoints.sh
   ```

3. **Verify all services are running:**
   ```bash
   docker ps
   ```

4. **Check the troubleshooting guide above for specific error messages** 