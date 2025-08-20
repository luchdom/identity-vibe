using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceA.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class DataController : ControllerBase
{
    private static readonly List<string> _data = new()
    {
        "Sample Data 1",
        "Sample Data 2",
        "Sample Data 3"
    };

    [HttpGet]
    [Authorize(Policy = "ServiceIdentityRead")]
    public ActionResult<IEnumerable<string>> Get()
    {
        return Ok(_data);
    }

    [HttpGet("user")]
    [Authorize(Policy = "UserIdentity")]
    public ActionResult<IEnumerable<string>> GetForUser()
    {
        var userData = _data.Select(item => $"User: {item}").ToList();
        return Ok(userData);
    }

    [HttpPost]
    [Authorize(Policy = "ServiceIdentityCreate")]
    public ActionResult<string> Create([FromBody] string data)
    {
        _data.Add(data);
        return CreatedAtAction(nameof(Get), new { id = _data.Count - 1 }, data);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "ServiceIdentityUpdate")]
    public ActionResult<string> Update(int id, [FromBody] string data)
    {
        if (id < 0 || id >= _data.Count)
        {
            return NotFound();
        }

        _data[id] = data;
        return Ok(data);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "ServiceIdentityDelete")]
    public ActionResult Delete(int id)
    {
        if (id < 0 || id >= _data.Count)
        {
            return NotFound();
        }

        _data.RemoveAt(id);
        return NoContent();
    }

    [HttpGet("info")]
    public ActionResult<object> GetInfo()
    {
        var user = User;
        return Ok(new
        {
            UserId = user.FindFirst("sub")?.Value,
            Name = user.FindFirst("name")?.Value,
            Email = user.FindFirst("email")?.Value,
            ClientId = user.FindFirst("client_id")?.Value,
            Scopes = user.FindFirst("scope")?.Value,
            Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }
} 