# BoneAI - Pediatric Wrist Fracture Detection System

Hệ thống phân tích ảnh X-quang cổ tay trẻ em hỗ trợ phát hiện các vùng nghi ngờ gãy xương bằng trí tuệ nhân tạo (Multi-Model AI Inference), xây dựng trên nền tảng **ASP.NET Core Web API (Onion Architecture)** và **Python AI Service (FastAPI)**.

---

## 1. Kiến trúc hệ thống (Onion Architecture)

```
BoneAI/
├── BoneAI.Domain/           # Entities, Enums, Domain Exceptions, Status Calculators
├── BoneAI.Application/      # Use Cases, DTOs, Service Interfaces, Core Business Logic
├── BoneAI.Infrastructure/   # EF Core DbContext, Repositories, Background Worker, Storage, ImageSharp Rendering
├── BoneAI.API/              # Controllers, Exception Middleware, Swagger/OpenAPI Configuration
├── BoneAI.Tests/            # Comprehensive Unit, Integration, Scenario, & Performance Tests
└── AIService/               # FastAPI Inference Engine, Model Registry, YOLOv8/PyTorch Adapters
```

---

## 2. Các nhóm chức năng chính (Giai đoạn 1 - 6)

1. **Image Management (`/api/images`)**:
   - Upload ảnh X-quang (`.png`, `.jpg`, `.jpeg`, tối đa 10MB) với xác thực Magic Bytes chống giả mạo định dạng.
   - Phân trang, xem metadata và streaming file ảnh gốc.
   - Bảo toàn tính toàn vẹn dữ liệu: Ngăn chặn xóa ảnh nếu đang được tham chiếu bởi các bản ghi Analysis (`409 Conflict`).
2. **Analysis Management (`/api/analysis`)**:
   - Khởi tạo tác vụ phân tích bất đồng bộ theo các chế độ: `SingleModel`, `MultiModel`, `CompareAll`.
   - Hỗ trợ `Idempotency-Key` ngăn chặn gửi trùng lặp tác vụ.
   - Hàng đợi cơ sở dữ liệu bền vững (Durable Database-backed Queue) với cơ chế claim job và lease time, tự động phục hồi tác vụ bỏ dở khi worker gặp sự cố.
3. **Multi-Model Inference & Background Worker**:
   - Phân tán xử lý song song với `SemaphoreSlim`.
   - Cơ chế retry tự động với exponential backoff khi gặp lỗi tạm thời (Transient error).
   - Đảm bảo cách ly kết quả (Detection Isolation): Mỗi model chỉ sở hữu Detections do chính nó tạo ra.
4. **Results & Comparison (`/api/results`)**:
   - `GET /api/results/{analysisId}`: Truy xuất toàn bộ kết quả phân tích, thông tin ảnh gốc và các run.
   - `GET /api/results/runs/{runId}`: Truy xuất chi tiết từng run đơn lẻ và Detections tương ứng.
   - `GET /api/results/{analysisId}/comparison`: Bảng dữ liệu so sánh chuẩn hóa giữa các model (Detection Count, Average Confidence, Processing Time theo giây).
   - `GET /api/results/runs/{runId}/image`: Trả về ảnh kết quả đã được vẽ Bounding Box và tag nhãn độc lập.
5. **Analysis History (`/api/history`)**:
   - `GET /api/history`: Danh sách lịch sử phân trang ở cấp độ Database (`Skip`/`Take`), lọc theo trạng thái (`status`), lọc theo model (`modelId`), và sắp xếp an toàn (`createdAt`, `status`).
   - `GET /api/history/{id}`: Xem chi tiết một lần phân tích cũ (tái sử dụng DTO từ Results API).
   - `DELETE /api/history/{id}`: Soft Delete an toàn, ẩn khỏi danh sách lịch sử, giữ nguyên vẹn ảnh X-quang gốc và AIModel, đồng thời dọn dẹp ảnh kết quả trên Storage mà không ảnh hưởng tới DB transaction.

---

## 3. Hướng dẫn khởi chạy

### 3.1. Yêu cầu môi trường
- **.NET SDK**: 8.0 trở lên
- **SQL Server**: LocalDB hoặc SQL Server 2019/2022
- **Python**: 3.10 hoặc 3.11

### 3.2. Khởi chạy Python AI Service
```bash
cd AIService
python -m venv venv
venv\Scripts\activate
pip install -r requirements.txt
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```
- API Health Check: `GET http://localhost:8000/health`
- API Model Catalog: `GET http://localhost:8000/models`

### 3.3. Khởi chạy Backend Web API
```bash
cd BoneAI.API
dotnet run
```
- Swagger UI: `https://localhost:7123/swagger` hoặc `http://localhost:5241/swagger`
- API Health Check: `GET /api/analysis/ai-service-health`

---

## 4. Kiểm thử (Test Suite)

### Chạy toàn bộ .NET Test Suite (150 tests)
```bash
dotnet test
```

### Chạy toàn bộ Python AI Service Tests (12 tests)
```bash
pytest AIService
```
