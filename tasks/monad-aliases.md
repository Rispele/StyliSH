# Aliases Feature

Нужно реализовать алиасы для монад и трансформеров. 
Алиасы должны работать полностью повторять логику монад и трансформеров, алиасами которых они являются.

Для монад, на примере алиаса DomainResult:
- DomainResult является алиасом для Either<DomainError, TValue>, где DomainError - произвольный тип ошибки;
- DomainResult является монадой и подчиняется её законам. Её логика идентична логике Either<DomainError, TValue>;
- У DomainResult доступны все методы, доступные для Either<DomainError, TValue>: Pure, Map, Bind, FromError, FromResult;
- Действия над DomainResult возвращают MonadWrapper, MonadWrapper может быть приведен к типу DomainResult и только к нему.

Для трансформеров, на примере алиаса DomainResult:
- DomainResult является алиасом для EitherT<TaskMarker, DomainError, TValue>, где DomainError - произвольный тип ошибки;
- DomainResult является трансформером и подчиняется её законам. Её логика идентична логике EitherT<TaskMarker, DomainError, TValue>;
- У DomainResult доступны все методы, доступные для EitherT<TaskMarker, DomainError, TValue>: Pure, Map, Bind, FromError, FromResult;
- Действия над DomainResult возвращают MonadWrapper, MonadWrapper может быть приведен к типу DomainResult и только к нему.

Не зацикливайся на примере с DomainResult. Правила, описанные выше, должны быть справедливы для любого алиаса.

Общие требования:
- Алиасы должны быть похожи по реализации на newtype из haskell (в идеале, но я понимаю, что это трудно достижимо);
- Должен быть единый механизм алиасов для монад и трансформеров;
- Алиас должен быть прост в реализации;
- Алиас не должен конфликтовать с монадой, алиасом для которой является;
- Не должно нарушаться правило: один маркер - одна монада.