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
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   578.3 μs |    NA |         - |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   569.2 μs |    NA |         - |
| &#39;Read HasIntent&#39;                       |   548.2 μs |    NA |         - |
| &#39;Read IsReadIntent&#39;                    |   547.6 μs |    NA |         - |
| &#39;Read IsWriteIntent&#39;                   |   614.3 μs |    NA |         - |
| DatabaseRoutingScope.ForRead()         | 1,050.2 μs |    NA |     392 B |
| DatabaseRoutingScope.ForWrite()        | 1,046.0 μs |    NA |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,073.1 μs |    NA |     392 B |
| DatabaseRoutingContext.Clear()         |   232.0 μs |    NA |      96 B |
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 1,704.1 μs |    NA |     840 B |
