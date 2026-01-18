using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/questions")]
[ApiController]
public class QuestionController(QuestionService questionService) : ControllerBase
{
    private readonly QuestionService _questionService = questionService;

    [HttpGet("exercise/{exerciseId}")]
    public async Task<ActionResult<List<QuestionDto>>> GetQuestionsByExercise(int exerciseId)
    {
        var questions = await _questionService.GetQuestionsByExerciseIdAsync(exerciseId);
        return Ok(questions.Select(q => q.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuestionDto>> GetQuestion(int id)
    {
        var question = await _questionService.GetQuestionByIdAsync(id);
        if (question == null)
            return NotFound();

        return Ok(question.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<QuestionDto>> CreateQuestion(CreateQuestionDto dto)
    {
        try 
        {
            var question = await _questionService.CreateQuestionAsync(dto);
            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, question.ToDto());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var result = await _questionService.DeleteQuestionAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
