using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/exercises")]
[ApiController]
public class ExerciseController(ExerciseService exerciseService) : ControllerBase
{
    private readonly ExerciseService _exerciseService = exerciseService;

    [HttpGet("lesson/{lessonId}")]
    public async Task<ActionResult<List<ExerciseDto>>> GetExercisesByLesson(int lessonId)
    {
        var exercises = await _exerciseService.GetExercisesByLessonIdAsync(lessonId);
        return Ok(exercises.Select(e => e.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExerciseDto>> GetExercise(int id)
    {
        var exercise = await _exerciseService.GetExerciseByIdAsync(id);
        if (exercise == null)
            return NotFound();

        return Ok(exercise.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<ExerciseDto>> CreateExercise(CreateExerciseDto dto)
    {
        var exercise = await _exerciseService.CreateExerciseAsync(dto);
        return CreatedAtAction(nameof(GetExercise), new { id = exercise.Id }, exercise.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<ExerciseDto>> UpdateExercise(int id, UpdateExerciseDto dto)
    {
        var exercise = await _exerciseService.UpdateExerciseAsync(id, dto);
        if (exercise == null)
            return NotFound();

        return Ok(exercise.ToDto());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        var result = await _exerciseService.DeleteExerciseAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
