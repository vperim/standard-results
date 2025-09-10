namespace StandardResults.UnitTests;

public class IntegrationTests
{
    public record User(int Id, string Email, int Age);
    public record UserRegistrationRequest(string Email, string Password, int Age);
    public record CreateUserCommand(string Email, string Password, int Age);

    [Fact]
    public void UserValidation_MultipleErrors_AggregatedCorrectly()
    {
        var request = new UserRegistrationRequest("", "123", 15);

        var validation = new ValidationErrorsBuilder()
            .Require(!string.IsNullOrWhiteSpace(request.Email), "email", "Email is required")
            .Require(request.Email.Contains("@"), "email", "Email must be valid")
            .Require(request.Password.Length >= 6, "password", "Password must be at least 6 characters")
            .Require(request.Age >= 18, "age", "Must be 18 or older")
            .Build();

        Assert.True(validation.HasErrors);
        Assert.Equal(4, validation.Count);
        Assert.Contains("email", validation.Summary());
        Assert.Contains("password", validation.Summary());
        Assert.Contains("age", validation.Summary());
    }

    [Fact]
    public void UserValidation_ValidRequest_NoErrors()
    {
        var request = new UserRegistrationRequest("user@example.com", "securepassword", 25);

        var validation = new ValidationErrorsBuilder()
            .Require(!string.IsNullOrWhiteSpace(request.Email), "email", "Email is required")
            .Require(request.Email.Contains("@"), "email", "Email must be valid")
            .Require(request.Password.Length >= 6, "password", "Password must be at least 6 characters")
            .Require(request.Age >= 18, "age", "Must be 18 or older")
            .Build();

        Assert.False(validation.HasErrors);
        Assert.Equal(0, validation.Count);
    }

    [Fact]
    public async Task RepositoryPattern_Success_ReturnsUser()
    {
        var userRepository = new MockUserRepository();
        var userService = new UserService(userRepository);

        var result = await userService.GetUserByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("john@example.com", result.Value.Email);
    }

    [Fact]
    public async Task RepositoryPattern_NotFound_ReturnsError()
    {
        var userRepository = new MockUserRepository();
        var userService = new UserService(userRepository);

        var result = await userService.GetUserByIdAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
        Assert.Equal("User with ID 999 not found", result.Error.Message);
        Assert.False(result.Error.IsTransient);
    }

    [Fact]
    public async Task HttpClientPattern_TransientError_RetryLogic()
    {
        var httpClient = new MockHttpClient();
        var apiService = new ApiService(httpClient);

        var result = await apiService.GetDataWithRetryAsync("test-endpoint");

        Assert.True(result.IsSuccess);
        Assert.Equal("retry-success-data", result.Value);
    }

    [Fact]
    public async Task HttpClientPattern_PermanentError_NoRetry()
    {
        var httpClient = new MockHttpClient();
        var apiService = new ApiService(httpClient);

        var result = await apiService.GetDataWithRetryAsync("permanent-error");

        Assert.True(result.IsFailure);
        Assert.False(result.Error.IsTransient);
        Assert.Equal("permanent_error", result.Error.Code);
    }

    [Fact]
    public async Task CommandHandler_ValidationAndProcessing_IntegratedFlow()
    {
        var userRepository = new MockUserRepository();
        var commandHandler = new CreateUserCommandHandler(userRepository);

        var validCommand = new CreateUserCommand("newuser@example.com", "securepassword123", 25);
        var result = await commandHandler.HandleAsync(validCommand);

        Assert.True(result.IsSuccess);
        Assert.Equal("newuser@example.com", result.Value.Email);
        Assert.Equal(25, result.Value.Age);
    }

    [Fact]
    public async Task CommandHandler_ValidationFailure_ReturnsValidationErrors()
    {
        var userRepository = new MockUserRepository();
        var commandHandler = new CreateUserCommandHandler(userRepository);

        var invalidCommand = new CreateUserCommand("", "123", 15);
        var result = await commandHandler.HandleAsync(invalidCommand);

        Assert.True(result.IsFailure);
        Assert.True(result.Error.HasErrors);
        Assert.Contains("Email is required", result.Error.Summary());
        Assert.Contains("Password must be at least 6 characters", result.Error.Summary());
        Assert.Contains("Must be 18 or older", result.Error.Summary());
    }

    [Fact]
    public async Task CommandHandler_DuplicateEmail_ReturnsBusinessError()
    {
        var userRepository = new MockUserRepository();
        var commandHandler = new CreateUserCommandHandler(userRepository);

        var duplicateCommand = new CreateUserCommand("john@example.com", "securepassword123", 25);
        var result = await commandHandler.HandleAsync(duplicateCommand);

        Assert.True(result.IsFailure);
        Assert.Equal("ValidationErrors", result.Error.Code); // ValidationErrors implements IError
        Assert.Contains("A user with this email already exists", result.Error.Message);
    }

    [Fact]
    public void ErrorAggregation_MultipleServices_CombinedErrors()
    {
        var validationErrors = new ValidationErrorsBuilder()
            .AddField("email", "Invalid format")
            .AddField("password", "Too weak")
            .Build();

        var businessErrors = ErrorCollection.Empty
            .WithError("duplicate_email", "Email already exists")
            .WithError("rate_limit", "Too many requests", transient: true);

        var allErrors = ErrorCollection.Empty
            .Merge(businessErrors)
            .WithError("general", validationErrors.Summary());

        Assert.Equal(3, allErrors.Count);
        Assert.True(allErrors.IsTransient);
        Assert.Contains("duplicate_email", allErrors.Summary());
        Assert.Contains("rate_limit", allErrors.Summary());
        Assert.Contains("email: Invalid format", allErrors.Summary());
    }

    [Fact]
    public async Task ConcurrentOperations_ResultIntegrity_MaintainedCorrectly()
    {
        var userRepository = new MockUserRepository();
        var userService = new UserService(userRepository);

        var tasks = Enumerable.Range(1, 10).Select(async id =>
        {
            await Task.Delay(new Random().Next(1, 10)); // Simulate random delays
            return await userService.GetUserByIdAsync(id <= 3 ? id : 999);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        var successes = results.Where(r => r.IsSuccess).ToArray();
        var failures = results.Where(r => r.IsFailure).ToArray();

        Assert.Equal(3, successes.Length);
        Assert.Equal(7, failures.Length);
        Assert.All(failures, f => Assert.Equal("not_found", f.Error.Code));
    }
}


public class CreateUserCommandHandler
{
    private readonly MockUserRepository userRepository;

    public CreateUserCommandHandler(MockUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<Result<IntegrationTests.User, ValidationErrors>> HandleAsync(IntegrationTests.CreateUserCommand command)
    {
        var validation = new ValidationErrorsBuilder()
            .Require(!string.IsNullOrWhiteSpace(command.Email), "email", "Email is required")
            .Require(command.Email.Contains("@"), "email", "Email must be valid")
            .Require(command.Password.Length >= 6, "password", "Password must be at least 6 characters")
            .Require(command.Age >= 18, "age", "Must be 18 or older")
            .Build();

        if (validation.HasErrors)
        {
            return Result<IntegrationTests.User, ValidationErrors>.Failure(validation);
        }

        var existingUser = await userRepository.GetByEmailAsync(command.Email);
        if (existingUser.IsSuccess)
        {
            var duplicateError = ValidationErrors.Empty
                .WithField("email", "A user with this email already exists");
            return Result<IntegrationTests.User, ValidationErrors>.Failure(duplicateError);
        }

        var newUser = new IntegrationTests.User(
            Id: new Random().Next(1000, 9999),
            Email: command.Email,
            Age: command.Age
        );

        return Result<IntegrationTests.User, ValidationErrors>.Success(newUser);
    }
}

public class UserService
{
    private readonly MockUserRepository repository;

    public UserService(MockUserRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<IntegrationTests.User, Error>> GetUserByIdAsync(int id)
    {
        return await repository.GetByIdAsync(id);
    }
}

public class MockUserRepository
{
    private readonly List<IntegrationTests.User> users =
    [
        new(1, "john@example.com", 30),
        new(2, "jane@example.com", 25),
        new(3, "bob@example.com", 35)
    ];

    public async Task<Result<IntegrationTests.User, Error>> GetByIdAsync(int id)
    {
        await Task.Delay(1); // Simulate async operation

        var user = users.FirstOrDefault(u => u.Id == id);
        return user != null
            ? Result<IntegrationTests.User, Error>.Success(user)
            : Result<IntegrationTests.User, Error>.Failure(Error.Permanent("not_found", $"User with ID {id} not found"));
    }

    public async Task<Result<IntegrationTests.User, Error>> GetByEmailAsync(string email)
    {
        await Task.Delay(1); // Simulate async operation

        var user = users.FirstOrDefault(u => u.Email == email);
        return user != null
            ? Result<IntegrationTests.User, Error>.Success(user)
            : Result<IntegrationTests.User, Error>.Failure(Error.Permanent("not_found", $"User with email {email} not found"));
    }
}

public class ApiService
{
    private readonly MockHttpClient httpClient;

    public ApiService(MockHttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<Result<string, Error>> GetDataWithRetryAsync(string endpoint)
    {
        const int maxRetries = 3;
        
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var result = await httpClient.GetAsync(endpoint);
            
            if (result.IsSuccess)
            {
                return result;
            }

            if (!result.Error.IsTransient || attempt == maxRetries)
            {
                return result;
            }

            await Task.Delay(100 * attempt); // Exponential backoff
        }

        return Result<string, Error>.Failure(Error.Permanent("max_retries", "Maximum retry attempts exceeded"));
    }
}

public class MockHttpClient
{
    private int callCount = 0;

    public async Task<Result<string, Error>> GetAsync(string endpoint)
    {
        await Task.Delay(10); // Simulate network delay
        callCount++;

        return endpoint switch
        {
            "test-endpoint" when callCount < 3 => Result<string, Error>.Failure(
                Error.Transient("network_error", "Temporary network issue")),
            "test-endpoint" => Result<string, Error>.Success("retry-success-data"),
            "permanent-error" => Result<string, Error>.Failure(
                Error.Permanent("permanent_error", "Permanent API error")),
            _ => Result<string, Error>.Failure(
                Error.Permanent("not_found", "Endpoint not found"))
        };
    }
}