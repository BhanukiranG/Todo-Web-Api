# A Beginner's Guide to Unit Testing in .NET

Writing tests can feel confusing at first, but it is one of the most valuable skills you can learn as a developer. This guide uses the tests we just wrote for your application to explain exactly how Unit Testing works in .NET.

---

## 1. What is a Unit Test?
A **Unit Test** checks one small, isolated piece of your code (a "unit") to make sure it behaves exactly as expected. 

When you write a test, you almost always follow the **"AAA" Pattern** (Arrange, Act, Assert):
1. **Arrange:** Setup the environment (create fake data, configure settings).
2. **Act:** Run the actual method you want to test.
3. **Assert:** Check if the result of the method matches what you expected.

### Our Toolbelt
In your `TodoApi.UnitTests` project, we are using two main libraries:
*   **xUnit:** The framework that runs the tests (this gives us the `[Fact]` and `Assert` keywords).
*   **Moq:** A "mocking" library. It allows us to create fake versions of databases or caches so we can test our logic *without* needing a real database running.

---

## Example 1: Testing Pure Logic (`JwtServiceTests`)

Your `JwtService` has one job: look at a `User` and generate a JWT string.

Here is how we test it using the **AAA** pattern:

```csharp
[Fact] // This tells xUnit that this method is a test!
public void GenerateToken_For_Admin_Should_Contain_All_Permissions()
{
    // 1. ARRANGE
    // We create a fake "Admin" user.
    var user = new User
    {
        Id = Guid.NewGuid(),
        Email = "admin@test.com",
        Role = "Admin"
    };

    // 2. ACT
    // We pass our fake user into the real GenerateToken method
    var token = _service.GenerateToken(user);
    
    // (We also decode the token so we can read what is inside it)
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    // 3. ASSERT
    // We "Assert" (demand) that the token contains the correct permissions.
    // If the token DOES NOT have these permissions, the test FAILS.
    Assert.Contains(jwtToken.Claims, c => c.Type == "Permission" && c.Value == "Todos.Read");
    Assert.Contains(jwtToken.Claims, c => c.Type == "Permission" && c.Value == "Todos.Create");
    Assert.Contains(jwtToken.Claims, c => c.Type == "Permission" && c.Value == "Todos.Delete");
}
```

### Why use "Mocks" here?
The `JwtService` requires `IConfiguration` to read the Secret Key from `appsettings.json`. But in a test, we don't have an `appsettings.json` file! 
So, in the constructor of our test file, we used **Moq** to create a fake configuration:

```csharp
_configuration = new Mock<IConfiguration>();
// We tell the fake configuration: "If the service asks for Jwt:Key, give it this string"
_configuration.Setup(x => x["Jwt:Key"]).Returns("SuperSecretKeyForJwtAuthentication123456");
```

---

## Example 2: Testing with "Fake Databases" (`TodoServiceTests`)

Your `TodoService` is trickier. Its entire job is to talk to PostgreSQL (`ITodoRepository`) and Redis (`IDistributedCache`). 

**Rule of Unit Testing:** A Unit Test should NEVER connect to a real database. If it does, it becomes an "Integration Test." 

So, we use **Moq** to create a "Fake Database" and a "Fake Cache". Let's look at the `DeleteAsync` test:

```csharp
[Fact]
public async Task DeleteAsync_Should_Delete_Todo_When_Exists()
{
    // 1. ARRANGE
    var id = Guid.NewGuid();
    var todo = new TodoItem { Id = id, Title = "To Delete" };

    // We program our Fake Database (Mock Repository). 
    // We say: "If the service asks to GetByIdAsync with this ID, hand it our fake todo item."
    _repository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(todo);

    // 2. ACT
    // We trigger the actual deletion logic.
    var result = await _service.DeleteAsync(id);

    // 3. ASSERT
    // First, we assert that the method returned 'true' (success).
    Assert.True(result);

    // Next, we use Moq's "Verify" feature. 
    // We demand proof that the TodoService actually asked the Database to delete the item.
    _repository.Verify(x => x.Delete(todo), Times.Once);
    
    // We demand proof that the TodoService actually saved the database changes.
    _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    
    // We demand proof that the TodoService cleared the Redis cache.
    _cache.Verify(x => x.RemoveAsync("todos", It.IsAny<CancellationToken>()), Times.Once);
}
```

### The Power of `Verify`
In `TodoServiceTests`, `Verify` is your best friend. 

If someone accidentally deletes the line `await _cache.RemoveAsync("todos");` from your main application code, the application will still compile and run perfectly fine. But because we wrote `_cache.Verify(x => x.RemoveAsync...`, **the test will immediately fail**, warning you that the cache is no longer being cleared when a Todo is deleted!

## Summary
1. **Arrange** your fake data and "Mocks".
2. **Act** on the service you are testing.
3. **Assert** the results are correct, and use **Verify** to ensure the service talked to the database/cache exactly how you expected it to.
