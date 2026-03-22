# Monad Alias Source Generator — Implementation Plan

## Overview

Source generator that automates creation of named monad alias types. User writes a one-line `partial readonly record struct` with `[MonadAlias(typeof(InnerMarker))]` attribute, generator produces the full `IMonad`/`IMonadUnwrapper` implementation + companion marker type.

## Current State Analysis

**What exists:**
- `MonadAlias<TAliasMarker, TInnerMarker, TValue>` — generic alias struct in `StyliSH.Abstractions/Monads/Aliases/`
- `TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>` — generic transformer alias
- Tests in `StyliSH.Tests/Aliases/` using manually defined `DomainResultMarker` + `MonadAlias<,,>`
- No source generators in the solution

**Problem:** Using aliases in function signatures requires verbose generic types: `MonadAlias<DomainResultMarker, EitherMarker<string>, int>` instead of `DomainResult<int>`.

**Research:** `thoughts/shared/research/2026-03-23-monad-alias-types.md` — C# doesn't support open generic using aliases, so we need named struct types. Manual approach works but has ~25 lines boilerplate per alias.

### Key Discoveries:
- Nested marker type (`DomainResult.Marker`) doesn't work for generic aliases — `DomainResult<int>.Marker` and `DomainResult<string>.Marker` are different types
- Source generators must target `netstandard2.0`
- IEitherLikeMarker detection is out of scope — generated marker implements only `IMonadMarker<T>`

## Desired End State

User writes:
```csharp
[MonadAlias(typeof(EitherMarker<string>))]
public partial readonly record struct DomainResult<TValue>;
```

Generator produces two files:

**1. DomainResult.MonadAlias.g.cs** — struct body:
```csharp
public partial readonly record struct DomainResult<TValue>
    : IMonad<DomainResultMarker, TValue>,
      IMonadUnwrapper<DomainResult<TValue>, DomainResultMarker, TValue>
{
    public required IMonad<EitherMarker<string>, TValue> Inner { get; init; }

    public IMonad<DomainResultMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => new DomainResult<TNewValue> { Inner = Inner.RawMap(map) };

    public IMonad<DomainResultMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<DomainResultMarker, TNewValue>> bind)
        => new DomainResult<TNewValue>
        {
            Inner = Inner.RawBind(value =>
                ((DomainResult<TNewValue>)bind(value)).Inner)
        };

    public static implicit operator DomainResult<TValue>(
        MonadWrapper<DomainResultMarker, TValue> monad)
        => IMonadUnwrapper<DomainResult<TValue>, DomainResultMarker, TValue>
            .CastFrom(monad);
}
```

**2. DomainResultMarker.MonadAlias.g.cs** — marker:
```csharp
public readonly record struct DomainResultMarker : IMonadMarker<DomainResultMarker>
{
    public static IMonad<DomainResultMarker, T> Pure<T>(T value)
        => new DomainResult<T> { Inner = EitherMarker<string>.Pure(value) };
}
```

### Verification:
- `dotnet build StyliSH.sln` compiles without errors
- Existing `MonadAliasTests` adapted to use generated alias pass all tests
- New dedicated generator tests pass
- Monad laws hold for generated aliases

## What We're NOT Doing

- **IEitherLikeMarker generation** — out of scope; user manually extends marker if needed
- **TransformerAlias generation** — Phase 2, separate from this plan
- **Replacing existing MonadAlias<,,>** — generic alias stays for inline use
- **Match method generation** — out of scope
- **Nested marker types** — doesn't work for generic aliases (see research)

## Implementation Approach

Incremental source generator (`IIncrementalGenerator`) in a new `StyliSH.Generators` project targeting `netstandard2.0`. The attribute lives in `StyliSH.Abstractions` (consumed by user code). The generator reads the attribute's `Type` argument via Roslyn semantic model and emits two partial files.

---

## Phase 1: Project Infrastructure

### Overview
Create the `StyliSH.Generators` project and wire it into the solution. Add the `MonadAliasAttribute` to `StyliSH.Abstractions`.

### Changes Required:

#### 1. New project: `StyliSH.Generators`
**File**: `StyliSH.Generators/StyliSH.Generators.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
        <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    </ItemGroup>

</Project>
```

#### 2. Attribute definition
**File**: `StyliSH.Abstractions/Monads/Aliases/MonadAliasAttribute.cs`

```csharp
namespace StyliSH.Abstractions.Monads.Aliases;

[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class MonadAliasAttribute : Attribute
{
    public Type InnerMarkerType { get; }

    public MonadAliasAttribute(Type innerMarkerType)
    {
        InnerMarkerType = innerMarkerType;
    }
}
```

#### 3. Wire projects into solution
**File**: `StyliSH.sln` — add `StyliSH.Generators` project

#### 4. Reference generator from consuming projects
**File**: `StyliSH.Tests/StyliSH.Tests.csproj` — add:

```xml
<ItemGroup>
    <ProjectReference Include="..\StyliSH.Generators\StyliSH.Generators.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
</ItemGroup>
```

Also add the same reference to `StyliSH.Implementations/StyliSH.Implementations.csproj` if consumers there will use aliases.

### Success Criteria:

#### Automated Verification:
- [x] `dotnet build StyliSH.sln` succeeds
- [x] `StyliSH.Generators` targets `netstandard2.0` and compiles
- [x] `MonadAliasAttribute` is accessible from `StyliSH.Tests`

---

## Phase 2: MonadAlias Source Generator

### Overview
Implement the `IIncrementalGenerator` that finds `[MonadAlias]`-annotated structs and generates the struct body + marker type.

### Changes Required:

#### 1. Generator implementation
**File**: `StyliSH.Generators/MonadAliasGenerator.cs`

The generator must:

1. **Find candidates**: Filter for `struct` declarations with `[MonadAlias]` attribute
2. **Extract metadata** from semantic model:
   - Struct name (e.g., `DomainResult`)
   - Struct namespace
   - TValue type parameter name (last type parameter on the struct)
   - Inner marker type from attribute argument (e.g., `EitherMarker<string>`)
   - Full type name with namespace for the inner marker
3. **Derive marker name**: `{StructName}Marker` (e.g., `DomainResultMarker`)
4. **Generate struct partial** with:
   - `required IMonad<{InnerMarker}, {TValue}> Inner { get; init; }` property
   - `IMonad<{MarkerName}, TValue>` and `IMonadUnwrapper<{StructName}<TValue>, {MarkerName}, TValue>` interface implementations
   - `RawMap<TNewValue>` method
   - `RawBind<TNewValue>` method
   - `implicit operator` from `MonadWrapper`
5. **Generate marker struct** with:
   - `IMonadMarker<{MarkerName}>` implementation
   - `Pure<T>` method creating the alias struct

**Key implementation details:**

```csharp
[Generator]
public class MonadAliasGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Filter: struct declarations with [MonadAlias] attribute
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "StyliSH.Abstractions.Monads.Aliases.MonadAliasAttribute",
            predicate: (node, _) => node is StructDeclarationSyntax,
            transform: (ctx, ct) => ExtractModel(ctx, ct))
            .Where(m => m is not null);

        // 2. Generate source for each candidate
        context.RegisterSourceOutput(provider, (spc, model) => Generate(spc, model!));
    }
}
```

**Model class** (data extracted from syntax/semantic):
```csharp
record MonadAliasModel(
    string Namespace,
    string StructName,            // "DomainResult"
    string TypeParameterName,     // "TValue"
    string InnerMarkerFullName,   // "StyliSH.Implementations.Monads.Either.EitherMarker<string>"
    string InnerMarkerDisplayName,// "EitherMarker<string>"
    string MarkerName,            // "DomainResultMarker"
    string Accessibility          // "public"
);
```

**Type name resolution**: Use `ITypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` for the inner marker type to get fully qualified names with `global::` prefix. This ensures generated code compiles regardless of using directives.

#### 2. Source output template

**Struct file** (`{StructName}.MonadAlias.g.cs`):
```csharp
// <auto-generated />
#nullable enable

namespace {Namespace};

{Accessibility} partial readonly record struct {StructName}<{TValue}>
    : global::StyliSH.Abstractions.Monads.IMonad<{MarkerFullName}, {TValue}>,
      global::StyliSH.Abstractions.Monads.IMonadUnwrapper<{StructName}<{TValue}>, {MarkerFullName}, {TValue}>
{
    public required global::StyliSH.Abstractions.Monads.IMonad<{InnerMarkerFullName}, {TValue}> Inner { get; init; }

    public global::StyliSH.Abstractions.Monads.IMonad<{MarkerFullName}, TNewValue> RawMap<TNewValue>(
        Func<{TValue}, TNewValue> map)
        => new {StructName}<TNewValue> { Inner = Inner.RawMap(map) };

    public global::StyliSH.Abstractions.Monads.IMonad<{MarkerFullName}, TNewValue> RawBind<TNewValue>(
        Func<{TValue}, global::StyliSH.Abstractions.Monads.IMonad<{MarkerFullName}, TNewValue>> bind)
        => new {StructName}<TNewValue>
        {
            Inner = Inner.RawBind(value =>
                (({StructName}<TNewValue>)bind(value)).Inner)
        };

    public static implicit operator {StructName}<{TValue}>(
        global::StyliSH.Abstractions.Monads.MonadWrapper<{MarkerFullName}, {TValue}> monad)
        => global::StyliSH.Abstractions.Monads.IMonadUnwrapper<{StructName}<{TValue}>, {MarkerFullName}, {TValue}>
            .CastFrom(monad);
}
```

**Marker file** (`{MarkerName}.MonadAlias.g.cs`):
```csharp
// <auto-generated />
#nullable enable

namespace {Namespace};

{Accessibility} readonly record struct {MarkerName}
    : global::StyliSH.Abstractions.Monads.IMonadMarker<{MarkerFullName}>
{
    public static global::StyliSH.Abstractions.Monads.IMonad<{MarkerFullName}, T> Pure<T>(T value)
        => new {StructFullName}<T> { Inner = {InnerMarkerFullName}.Pure(value) };
}
```

#### 3. Diagnostics

Report errors for:
- `[MonadAlias]` on non-`partial` struct → `STYL001: MonadAlias requires partial struct`
- `[MonadAlias]` on struct without exactly one type parameter → `STYL002: MonadAlias struct must have exactly one type parameter`
- Inner marker type doesn't implement `IMonadMarker<>` → `STYL003: Inner marker must implement IMonadMarker`

### Success Criteria:

#### Automated Verification:
- [x] `dotnet build StyliSH.sln` succeeds with generated code
- [x] Generated files visible in `obj/` under `StyliSH.Tests`
- [x] No analyzer warnings from the generator

#### Manual Verification:
- [ ] Inspect generated `.g.cs` files — correct namespace, type names, fully qualified references
- [ ] IntelliSense works for the generated `Inner` property and methods in Rider

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the generated code looks correct before proceeding to Phase 3.

---

## Phase 3: Tests

### Overview
Write tests validating the source generator output. Adapt existing `MonadAliasTests` pattern to use generated aliases.

### Changes Required:

#### 1. Generated alias test
**File**: `StyliSH.Tests/Aliases/GeneratedMonadAliasTests.cs`

Define a test alias using the generator:
```csharp
using StyliSH.Abstractions.Monads.Aliases;
using StyliSH.Implementations.Monads.Either;

namespace StyliSH.Tests.Aliases;

[MonadAlias(typeof(EitherMarker<string>))]
public partial readonly record struct GenDomainResult<TValue>;
```

Then write tests mirroring `MonadAliasTests`:
- Pure, FromValue via marker's Pure
- Map, Bind operations
- Type isolation (inner monad is `GenDomainResult`, not bare `Either`)
- Monad laws (left identity, right identity, associativity)
- Verify `Inner` property is accessible and holds the correct inner monad

**Note**: Since `IEitherLikeMarker` is out of scope, `FromError`/`FromValue` tests are skipped — only `Pure` is generated on the marker. The user would manually extend the marker with `FromError`/`FromValue` as a partial if needed.

#### 2. Diagnostic tests (optional, stretch goal)
Test that the generator reports errors for invalid inputs (non-partial struct, missing type parameter, etc.).

### Success Criteria:

#### Automated Verification:
- [x] `dotnet test StyliSH.Tests` — all new tests pass
- [x] Monad laws verified for generated alias
- [x] Existing `MonadAliasTests` and `TransformerAliasTests` still pass (no regressions)

---

## Phase 4: TransformerAlias Source Generator (Future)

### Overview
Extend the generator to support `[TransformerAlias]` attribute for transformer aliases.

**Deferred** — will be designed and planned separately after Phase 1-3 are complete and validated.

Key challenges to resolve:
- `TInnerMonad` type (e.g., `Either<string, TValue>`) can't be expressed as `typeof()` with open TValue
- Need a convention for specifying the outer marker and inner monad type template
- `Run<TOuterMonad>()` method generation with correct constraints

---

## Testing Strategy

### Unit Tests:
- Generated alias struct implements `IMonad<MarkerType, TValue>`
- `Pure` → `Map` → correct value
- `Pure` → `Bind` → correct value
- `Bind` error short-circuiting (via inner monad behavior)
- Type isolation: `MonadWrapper.Monad` is the generated struct, not the inner type

### Monad Law Tests:
- Left Identity: `Pure(a).Bind(f) == f(a)`
- Right Identity: `m.Bind(Pure) == m`
- Associativity: `m.Bind(f).Bind(g) == m.Bind(x => f(x).Bind(g))`

### Regression:
- Existing `MonadAliasTests` unchanged and passing
- Existing `TransformerAliasTests` unchanged and passing

## Performance Considerations

- Incremental generator with `ForAttributeWithMetadataName` — only re-runs when annotated structs change
- Generated code is identical in structure to hand-written `MonadAlias` — no additional runtime overhead
- `record struct` with `required init` properties — zero-cost construction

## References

- Research: `thoughts/shared/research/2026-03-23-monad-alias-types.md` — Approach B (Source Generator)
- Existing aliases: `StyliSH.Abstractions/Monads/Aliases/MonadAlias.cs`
- Existing tests: `StyliSH.Tests/Aliases/MonadAliasTests.cs`
- Previous plan: `thoughts/shared/plans/2026-03-23-monad-aliases.md`
