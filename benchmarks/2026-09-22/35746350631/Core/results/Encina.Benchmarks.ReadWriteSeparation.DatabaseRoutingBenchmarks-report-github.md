```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,992.2 ns | 1,618.5 ns |  88.71 ns | 4,021.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   241.7 ns |   557.4 ns |  30.55 ns |   235.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,184.5 ns | 2,598.5 ns | 142.43 ns | 4,157.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   261.8 ns | 1,795.9 ns |  98.44 ns |   205.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   220.7 ns | 1,682.0 ns |  92.20 ns |   241.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   213.5 ns | 1,013.6 ns |  55.56 ns |   210.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   360.7 ns | 1,129.1 ns |  61.89 ns |   341.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,759.7 ns | 4,598.0 ns | 252.03 ns | 3,656.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,278.0 ns | 1,824.4 ns | 100.00 ns | 4,278.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,288.0 ns |   426.7 ns |  23.39 ns | 2,275.0 ns |      96 B |
