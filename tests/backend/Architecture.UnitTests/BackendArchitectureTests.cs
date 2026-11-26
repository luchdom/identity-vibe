using System.Reflection;
using System.Text.Json;
using AuthServer.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Orders.Controllers;
using Orders.Data.Repositories.Interfaces;
using Orders.Models.Requests;
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
    public void Api_body_models_should_be_top_level_request_records_in_matching_files()
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddControllers().ConfigureApplicationPartManager(parts =>
        {
            parts.ApplicationParts.Clear();
            foreach (var assembly in GetBackendAssemblies())
            {
                parts.ApplicationParts.Add(new AssemblyPart(assembly));
            }
        });

        using var provider = services.BuildServiceProvider();
        var actions = provider.GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items.OfType<ControllerActionDescriptor>();
        var requests = actions
            .SelectMany(action => action.Parameters)
            .Where(parameter => parameter.BindingInfo?.BindingSource?.CanAcceptDataFrom(BindingSource.Body) == true)
            .Select(parameter => parameter.ParameterType)
            .Distinct()
            .ToArray();
        requests.Should().NotBeEmpty("MVC must discover the application's body-bound models");

        GetRequestViolations(requests).Should()
            .BeEmpty("body-bound API parameters must use top-level record classes named *Request in matching files");
    }

    [Theory]
    [InlineData(typeof(JsonElement))]
    [InlineData(typeof(Dictionary<string, string>))]
    [InlineData(typeof(CreateOrderRequest[]))]
    public void Body_request_rule_rejects_raw_payloads_and_arrays(Type parameterType)
    {
        GetRequestViolations([parameterType]).Should().ContainSingle()
            .Which.Should().Contain(parameterType.ToString());
    }

    [Fact]
    public void Backend_source_should_have_at_most_one_top_level_class_or_record_per_file()
    {
        var violations = ReadBackendSources()
            .Select(file => (
                file.RelativePath,
                Classes: file.Types
                    .Where(type => BackendSourceFile.IsTopLevel(type)
                                   && (type is ClassDeclarationSyntax || BackendSourceFile.IsRecordClass(type)))
                    .Select(type => type.Identifier.ValueText)
                    .ToArray()))
            .Where(file => file.Classes.Length > 1)
            .Select(file => $"{file.RelativePath}: {string.Join(", ", file.Classes)}");

        violations.Should().BeEmpty();
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

    private static IEnumerable<string> GetRequestViolations(IEnumerable<Type> requests)
    {
        var declarations = ReadBackendSources()
            .SelectMany(file => file.Types.Select(type => (File: file, Type: type)))
            .ToLookup(item => (item.File.AssemblyName, BackendSourceFile.GetMetadataName(item.Type)));

        return requests.SelectMany(request =>
        {
            var declarationType = request.IsConstructedGenericType ? request.GetGenericTypeDefinition() : request;
            var matches = declarations[(declarationType.Assembly.GetName().Name!, declarationType.FullName!)].ToArray();
            if (matches.Length == 0)
            {
                return [$"{request}: no handwritten source declaration found"];
            }

            return matches
                .Where(match => !BackendSourceFile.IsTopLevel(match.Type)
                                || !BackendSourceFile.IsRecordClass(match.Type)
                                || !match.Type.Identifier.ValueText.EndsWith("Request", StringComparison.Ordinal)
                                || !string.Equals(Path.GetFileName(match.File.RelativePath),
                                    match.Type.Identifier.ValueText + ".cs", StringComparison.Ordinal))
                .Select(match => $"{match.File.RelativePath}: {request}");
        });
    }

    private static BackendSourceFile[] ReadBackendSources()
    {
        var files = BackendSourceFile.ReadAll(ResolveRepositoryRoot()).ToArray();
        files.Select(file => file.AssemblyName).Distinct()
            .Should().BeEquivalentTo(["AuthServer", "Orders", "Shared"]);
        return files;
    }

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
            if (File.Exists(Path.Combine(directory.FullName, "IdentityVibe.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing IdentityVibe.sln.");
    }
}
