using System.Reflection;
using AuthServer.Controllers;
using FluentAssertions;
using Orders.Controllers;
using Orders.Data.Repositories.Interfaces;
using Xunit;

namespace Architecture.UnitTests;

public sealed class BackendArchitectureTests
{
    [Fact]
    public void Backend_model_types_should_not_use_viewmodel_suffix()
    {
        var viewModelTypes = GetBackendAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        viewModelTypes.Should().BeEmpty();
    }

    [Fact]
    public void Backend_source_should_not_contain_active_models_viewmodels_files()
    {
        var repoRoot = ResolveRepositoryRoot();
        var staleFiles = new[]
            {
                Path.Combine(repoRoot, "src", "backend", "AuthServer", "Models", "ViewModels"),
                Path.Combine(repoRoot, "src", "backend", "Orders", "Models", "ViewModels")
            }
            .Where(Directory.Exists)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            .ToArray();

        staleFiles.Should().BeEmpty();
    }

    [Fact]
    public void Controllers_should_not_depend_on_repository_contracts()
    {
        var controllerTypes = new[]
            {
                typeof(AdminUsersController).Assembly,
                typeof(OrdersController).Assembly
            }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

        var invalidDependencies = controllerTypes
            .SelectMany(
                controller => controller.GetConstructors()
                    .SelectMany(constructor => constructor.GetParameters())
                    .Where(parameter => IsRepositoryDependency(parameter.ParameterType))
                    .Select(parameter => $"{controller.FullName} -> {parameter.ParameterType.FullName}"))
            .ToArray();

        invalidDependencies.Should().BeEmpty();
    }

    [Fact]
    public void Repository_contracts_should_not_return_ardalis_results()
    {
        var invalidMethods = typeof(IOrdersRepository).GetMethods()
            .Where(method => IsArdalisResultType(method.ReturnType))
            .Select(method => $"{typeof(IOrdersRepository).FullName}.{method.Name}")
            .ToArray();

        invalidMethods.Should().BeEmpty();
    }

    private static Assembly[] GetBackendAssemblies() =>
        [typeof(AdminUsersController).Assembly, typeof(OrdersController).Assembly];

    private static bool IsRepositoryDependency(Type type) =>
        type.Name.EndsWith("Repository", StringComparison.Ordinal)
        || (type.Namespace?.Contains(".Repositories", StringComparison.Ordinal) ?? false);

    private static bool IsArdalisResultType(Type type)
    {
        var candidate = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return candidate.FullName is "Ardalis.Result.Result`1" or "Ardalis.Result.Result";
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ArchZ.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing ArchZ.sln.");
    }
}
