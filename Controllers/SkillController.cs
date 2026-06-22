using Microsoft.AspNetCore.Mvc;
using SportHub.Services.Interfaces;

namespace SportHub.Controllers
{
    [ApiController]
    [Route("api/skill")]
    public class SkillController : ControllerBase
    {
        private readonly IAiChatService _ai;

        public SkillController(IAiChatService ai)
        {
            _ai = ai;
        }

        public class AssessRequest
        {
            public string Description { get; set; } = "";
        }

        [HttpPost("assess")]
        public async Task<IActionResult> Assess([FromBody] AssessRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Description))
                return BadRequest(new { error = "Description is required." });

            const string systemPrompt = @"Bạn là chuyên gia đánh giá trình độ cầu lông. Dựa vào bảng trình độ chuẩn sau:

**NEWBIE (~3 tháng):** Mới tập, chưa biết/sơ luật, cần nhắc. Chỉ đánh được cầu trước mặt, hướng khác bỏ. Đánh bản năng, không kỹ thuật. Không di chuyển, đứng im. Đánh nhẹ không dứt điểm.

**YẾU (3-6 tháng):** Nắm luật, biết tính điểm. Tối thiểu biết giao/phòng cầu. Cầm vợt đúng (đôi khi không). Di chuyển được nhưng chậm. Biết đánh dứt điểm, không cú cưa.

**YẾU+ (6 tháng-1 năm):** Biết bộ nhỡ, giao/phòng/đập/chặt nhưng làm chưa tốt. Điều khiển vợt theo ý. Biết phối hợp đồng đội. Lực không ổn định.

**TBY/TB- (1-2 năm):** Gần hoàn thiện bộ nhỡ, giao/phòng/chặt/đập/ve cầu. Hình thành lối đánh, dùng vợt phù hợp, cầm được cầu. Di chuyển theo bộ pháp nhưng chưa chuẩn/linh hoạt. Tâm lý đôi khi không ổn định.

**TRUNG BÌNH (3 năm+):** Hoàn thiện mọi kỹ thuật cơ bản. Di chuyển đúng bộ pháp. Tư duy chiến thuật rõ, tâm lý vững. Đỡ được hầu hết vị trí sân.

**TB+/KHÁ (5 năm+):** Hoàn thiện kỹ thuật cơ bản và nâng cao. Đỡ được mọi quả. Tấn công uy lực/tinh tế. Phòng thủ hiểm, nhanh gọn, trickshot/deception mượt. Chiến thuật rõ ràng, khai thác điểm yếu đối thủ, thể lực cực tốt.

Hãy đánh giá trình độ người chơi dựa trên mô tả của họ:
1. Xác định trình độ phù hợp nhất
2. Giải thích ngắn gọn lý do (2-4 câu), đề cập cụ thể đến những điểm trong mô tả của họ
3. Gợi ý 2-3 điểm cần cải thiện

Trả lời bằng cùng ngôn ngữ với người dùng (Việt/Anh). Format rõ ràng:
**Trình độ: [TÊN]**
[Giải thích]
**Gợi ý cải thiện:**
- [điểm 1]
- [điểm 2]
- [điểm 3]";

            var result = await _ai.ChatAsync(systemPrompt, new List<AiChatHistoryItem>(), req.Description);
            return Ok(new { result });
        }
    }
}
