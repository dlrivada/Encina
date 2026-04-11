```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,918.4 ns |  75.13 ns | 107.75 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   176.4 ns |  36.56 ns |  52.43 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,493.9 ns | 160.41 ns | 230.05 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   183.2 ns |  22.90 ns |  33.56 ns |         - |
| &#39;Read HasIntent&#39;                       |   186.1 ns |  16.33 ns |  24.44 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   168.3 ns |  13.12 ns |  17.96 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   202.3 ns |  17.00 ns |  24.91 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,428.7 ns |  68.84 ns | 100.91 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,417.4 ns |  77.20 ns | 105.67 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,145.5 ns | 185.18 ns | 265.57 ns |      96 B |
