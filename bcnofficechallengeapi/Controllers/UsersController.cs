using bcnofficechallengeapi.Data;
using bcnofficechallengeapi.Dtos;
using bcnofficechallengeapi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bcnofficechallengeapi.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(CreateUserRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Conflict(new { error = "Email already registered." });
        }

        var user = new User
        {
            Name = request.FullName,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CompanyName = request.CompanyName,
            JobTitle = request.JobTitle,
            LinkedIn = request.LinkedIn,
            Points = 0
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ToResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        return Ok(ToResponse(user));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await db.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(user));
    }

    [HttpGet("{id}/points")]
    public async Task<IActionResult> GetPoints(Guid id)
    {
        var user = await db.Users.FindAsync(id);

        if (user is null)
            return NotFound(new { error = $"User with id {id} was not found." });

        return Ok(new { userId = user.Id, points = user.Points, pointsTimestamp = user.PointsTimestamp });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, UpdateUserRequest request)
    {
        var user = await db.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        if (request.FullName is not null)
            user.Name = request.FullName;

        if (request.Email is not null)
        {
            if (await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
            {
                return Conflict(new { error = "Email already in use." });
            }
            user.Email = request.Email;
        }

        if (request.Password is not null)
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (request.CompanyName is not null)
            user.CompanyName = request.CompanyName;

        if (request.JobTitle is not null)
            user.JobTitle = request.JobTitle;

        if (request.LinkedIn is not null)
            user.LinkedIn = request.LinkedIn;

        await db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await db.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.Name,
        Email = user.Email,
        CompanyName = user.CompanyName,
        JobTitle = user.JobTitle,
        Points = user.Points,
        PointsTimestamp = user.PointsTimestamp,
        LinkedIn = user.LinkedIn
    };
}
