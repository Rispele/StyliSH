# Aliases Feature

Мы реализовали базовый вариант для алиасов - появились маркеры алиасов:
- Исследование: @thoughts/shared/research/2026-03-23-monad-aliases.md
- План: @thoughts/shared/plans/2026-03-23-monad-aliases.md

- Теперь я хочу, чтобы появлись непосредственно типы алиасов. 
Это важно, т.к. я хочу использовать алиасы в сигнатурах функций. 
В текущей реализации это будет выглядеть так:
```csharp
MonadAlias<DomainResultMarker, EitherMarker<string>, int> Foo(Type1 arg1, Type2 arg2...)
{ ... }
```
а должно:
```csharp
DomainResult<int> Foo(Type1 arg1, Type2 arg2...)
{ ... }
```