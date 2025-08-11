# MESK.MediatR

A lightweight, fast, and simple implementation of the Mediator pattern for .NET 8, inspired by MediatR. This library provides in-process messaging with support for request/response, command handling, notification publishing, and pipeline behaviors.

## Features

- 🚀 **Request/Response** - Send requests and receive responses
- 📢 **Notifications** - Publish notifications to multiple handlers
- 🔧 **Pipeline Behaviors** - Add cross-cutting concerns like logging, validation, caching
- 📦 **Dependency Injection** - Built-in support for Microsoft.Extensions.DependencyInjection
- ⚡ **Lightweight** - Minimal dependencies and overhead
- 🎯 **.NET 8** - Built for the latest .NET version

## Installation

You can install the package via NuGet Package Manager or by adding it directly to your project file:

```xml
<PackageReference Include="MESK.MediatR" Version="1.0.8" />
```

## Quick Start

### 1. Register Services

```csharp
using MESK.MediatR;

var builder = WebApplication.CreateBuilder(args);

// Register MediatR services
builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssembly(typeof(Program).Assembly);
    // Register pipeline behaviors if needed
    // options.RegisterPipelineBehavior(typeof(LoggingBehavior<,>));
});

var app = builder.Build();
```

### 2. Create a Request and Handler

```csharp
using MESK.MediatR;

// Define a request
public record GetUserQuery(int Id) : IRequest<User>;

// Define the response model
public record User(int Id, string Name, string Email);

// Implement the handler
public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Your business logic here
        return new User(request.Id, "John Doe", "john@example.com");
    }
}
```

### 3. Send Request

```csharp
public class UserController : ControllerBase
{
    private readonly ISender _sender;

    public UserController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}")]
    public async Task<User> GetUser(int id)
    {
        var query = new GetUserQuery(id);
        return await _sender.Send(query);
    }
}
```

## Usage Examples

### Commands (Request without Response)

```csharp
// Define a command
public record CreateUserCommand(string Name, string Email) : IRequest;

// Implement the handler
public class CreateUserHandler : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Create user logic
        Console.WriteLine($"Creating user: {request.Name}");
    }
}

// Usage
await _sender.Send(new CreateUserCommand("Jane Doe", "jane@example.com"));
```

### Notifications

```csharp
// Define a notification
public record UserCreatedNotification(int UserId, string Name) : INotification;

// Multiple handlers can handle the same notification
public class EmailNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Sending welcome email to user {notification.Name}");
    }
}

public class LoggingNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"User created: {notification.UserId}");
    }
}

// Usage - all handlers will be executed
await _sender.Publish(new UserCreatedNotification(1, "John Doe"));
```

### Pipeline Behaviors

Pipeline behaviors allow you to add cross-cutting concerns like logging, validation, caching, etc.

```csharp
// Create a logging behavior
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Handling {typeof(TRequest).Name}");
        
        var response = await next();
        
        _logger.LogInformation($"Handled {typeof(TRequest).Name}");
        
        return response;
    }
}

// Register the behavior
builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssembly(typeof(Program).Assembly);
    options.RegisterPipelineBehavior(typeof(LoggingBehavior<,>));
});
```

### Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

## API Reference

### Core Interfaces

#### `IRequest` and `IRequest<TResponse>`
Base interfaces for requests.

#### `IRequestHandler<TRequest>` and `IRequestHandler<TRequest, TResponse>`
Interfaces for handling requests.

#### `INotification`
Base interface for notifications.

#### `INotificationHandler<TNotification>`
Interface for handling notifications.

#### `ISender`
Main interface for sending requests and publishing notifications.

#### `IPipelineBehavior<TRequest>` and `IPipelineBehavior<TRequest, TResponse>`
Interfaces for implementing pipeline behaviors.

### Configuration

#### `MediatROptions`
Configuration class with methods:
- `RegisterServicesFromAssembly(Assembly assembly)`
- `RegisterServicesFromAssemblies(params Assembly[] assemblies)`
- `RegisterPipelineBehavior(Type behaviorType)`

## Best Practices

1. **Use Records for Requests**: Records provide immutability and value equality.
2. **Keep Handlers Focused**: Each handler should have a single responsibility.
3. **Use Pipeline Behaviors for Cross-Cutting Concerns**: Logging, validation, caching, etc.
4. **Async All the Way**: Always use async/await for non-blocking operations.
5. **Use CancellationTokens**: Always respect cancellation tokens for better performance.

## Performance Considerations

- Handlers are resolved from DI container on each request
- Pipeline behaviors are executed in reverse order of registration
- Notifications are published to all handlers in parallel using `Task.WhenAll`

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
