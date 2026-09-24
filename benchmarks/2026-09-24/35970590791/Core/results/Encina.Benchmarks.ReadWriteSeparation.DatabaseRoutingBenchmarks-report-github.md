```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,925.8 ns | 2,074.8 ns | 113.72 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   207.2 ns |   557.4 ns |  30.55 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,577.7 ns | 5,460.2 ns | 299.29 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   271.3 ns |   926.7 ns |  50.80 ns |         - |
| &#39;Read HasIntent&#39;                       |   197.3 ns |   115.9 ns |   6.35 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   173.2 ns | 1,062.3 ns |  58.23 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   250.5 ns |   836.0 ns |  45.83 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,715.2 ns | 3,182.5 ns | 174.44 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,443.7 ns | 2,251.7 ns | 123.42 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,154.3 ns | 1,468.6 ns |  80.50 ns |      96 B |
