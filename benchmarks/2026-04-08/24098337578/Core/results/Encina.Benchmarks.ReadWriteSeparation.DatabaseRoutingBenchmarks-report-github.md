```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,887.9 ns |  92.13 ns | 135.05 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   171.2 ns |  20.08 ns |  30.05 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,442.1 ns | 102.97 ns | 140.95 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   243.4 ns |  21.43 ns |  30.04 ns |         - |
| &#39;Read HasIntent&#39;                       |   190.9 ns |  12.45 ns |  17.85 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   215.8 ns |  19.56 ns |  28.06 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   209.4 ns |  22.77 ns |  33.37 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,401.6 ns |  41.16 ns |  57.70 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,525.6 ns | 128.94 ns | 184.92 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,151.8 ns |  88.93 ns | 124.67 ns |      96 B |
