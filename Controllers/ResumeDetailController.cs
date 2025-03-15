using Microsoft.AspNetCore.Mvc;
using ResumeSpy.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ResumeSpy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeDetailController : ControllerBase
    {
        private static List<ResumeDetailModel> ResumeDetailModels = new List<ResumeDetailModel>
        {
        };

        [HttpGet]
        public ActionResult<List<ResumeDetailModel>> GetResumeDetailModels([FromQuery] string resumeId)
        {
            var details = ResumeDetailModels.Where(rd => rd.ResumeId == resumeId).ToList();
            if (details.Count == 0)
            {
                details.Add(new ResumeDetailModel
                {
                    Id = string.Empty,
                    ResumeId = resumeId,
                    Name = "Default",
                    Language = string.Empty,
                    Content = string.Empty,
                    IsDefault = true,
                    CreateTime = DateTime.UtcNow,
                    LastModifyTime = DateTime.UtcNow
                });
            }
            return Ok(details);
        }

        [HttpPost]
        public ActionResult<ResumeDetailModel> CreateResumeDetailModel([FromBody] ResumeDetailModel ResumeDetailModel)
        {
            ResumeDetailModel.Id = (ResumeDetailModels.Count + 1).ToString();
            ResumeDetailModel.CreateTime = DateTime.UtcNow;
            ResumeDetailModel.LastModifyTime = DateTime.UtcNow;
            ResumeDetailModels.Add(ResumeDetailModel);
            return CreatedAtAction(nameof(GetResumeDetailModels), new { resumeId = ResumeDetailModel.ResumeId }, ResumeDetailModel);
        }

        [HttpPut("{id}")]
        public ActionResult<ResumeDetailModel> UpdateResumeDetailModel(string id, [FromBody] ResumeDetailModel updatedDetail)
        {
            var existingDetail = ResumeDetailModels.FirstOrDefault(rd => rd.Id == id);
            if (existingDetail == null)
            {
                return NotFound();
            }

            existingDetail.Name = updatedDetail.Name;
            existingDetail.Language = updatedDetail.Language;
            existingDetail.Content = updatedDetail.Content;
            existingDetail.IsDefault = updatedDetail.IsDefault;
            existingDetail.LastModifyTime = DateTime.UtcNow;

            return Ok(existingDetail);
        }

        [HttpPatch("{id}/name")]
        public ActionResult<ResumeDetailModel> UpdateResumeDetailModelName(string id, [FromBody] string newName)
        {
            var existingDetail = ResumeDetailModels.FirstOrDefault(rd => rd.Id == id);
            if (existingDetail == null)
            {
                return NotFound();
            }

            existingDetail.Name = newName;
            existingDetail.LastModifyTime = DateTime.UtcNow;

            return Ok(existingDetail);
        }

        [HttpPatch("{id}/content")]
        public ActionResult<ResumeDetailModel> UpdateResumeDetailModelContent(string id, [FromBody] string content)
        {
            var existingDetail = ResumeDetailModels.FirstOrDefault(rd => rd.Id == id);
            if (existingDetail == null)
            {
                return NotFound();
            }

            existingDetail.Content = content;
            existingDetail.LastModifyTime = DateTime.UtcNow;

            return Ok(existingDetail);
        }

        [HttpPost("copy")]
        public ActionResult<ResumeDetailModel> CreateResumeDetailModelFromExisting([FromBody] CopyRequest request)
        {
            var existingDetail = ResumeDetailModels.FirstOrDefault(rd => rd.Id == request.ExistingResumeDetailId);
            if (existingDetail == null)
            {
                return NotFound();
            }

            var newDetail = new ResumeDetailModel
            {
                Id = (ResumeDetailModels.Count + 1).ToString(),
                ResumeId = existingDetail.ResumeId,
                Name = $"{existingDetail.Name} Copy",
                Language = request.Language,
                Content = existingDetail.Content,
                IsDefault = false,
                CreateTime = DateTime.UtcNow,
                LastModifyTime = DateTime.UtcNow
            };

            ResumeDetailModels.Add(newDetail);
            return CreatedAtAction(nameof(GetResumeDetailModels), new { resumeId = newDetail.ResumeId }, newDetail);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteResumeDetailModel(string id)
        {
            var existingDetail = ResumeDetailModels.FirstOrDefault(rd => rd.Id == id);
            if (existingDetail == null)
            {
                return NotFound();
            }

            ResumeDetailModels.Remove(existingDetail);
            return NoContent();
        }
    }

    public class CopyRequest
    {
        public string ExistingResumeDetailId { get; set; }
        public string Language { get; set; }
    }
}