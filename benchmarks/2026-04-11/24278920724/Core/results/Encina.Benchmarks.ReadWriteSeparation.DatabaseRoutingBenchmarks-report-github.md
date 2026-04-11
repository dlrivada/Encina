```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,965.1 ns | 176.72 ns | 259.03 ns | 3,926.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   149.6 ns |  24.97 ns |  35.82 ns |   139.8 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,288.8 ns | 127.84 ns | 179.22 ns | 3,275.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   138.7 ns |  80.94 ns | 110.79 ns |   106.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   194.8 ns |  80.90 ns | 121.09 ns |   152.2 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   189.2 ns |  42.34 ns |  62.07 ns |   165.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   126.6 ns |  29.54 ns |  42.37 ns |   134.2 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,528.9 ns | 114.15 ns | 163.72 ns | 3,575.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,485.2 ns | 238.87 ns | 342.58 ns | 3,380.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,078.6 ns |  64.51 ns |  94.56 ns | 2,113.0 ns |      96 B |
