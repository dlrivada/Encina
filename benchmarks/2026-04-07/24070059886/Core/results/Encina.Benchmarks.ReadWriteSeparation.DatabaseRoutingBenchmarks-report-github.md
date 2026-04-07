```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                 | Mean       | Error | Allocated |
|--------------------------------------- |-----------:|------:|----------:|
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   631.8 μs |    NA |         - |
| &#39;Read HasIntent&#39;                       |   562.9 μs |    NA |         - |
| &#39;Read IsReadIntent&#39;                    |   581.5 μs |    NA |         - |
| &#39;Read IsWriteIntent&#39;                   |   631.6 μs |    NA |         - |
| DatabaseRoutingScope.ForWrite()        | 1,123.3 μs |    NA |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,134.4 μs |    NA |     392 B |
| DatabaseRoutingContext.Clear()         |   299.1 μs |    NA |      96 B |
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 1,750.1 μs |    NA |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   581.5 μs |    NA |         - |
| DatabaseRoutingScope.ForRead()         | 1,103.2 μs |    NA |     392 B |
