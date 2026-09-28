```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,169.2 ns | 2,381.0 ns | 130.51 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   218.0 ns | 1,015.3 ns |  55.65 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,837.7 ns | 4,867.7 ns | 266.82 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   200.5 ns |   316.0 ns |  17.32 ns |         - |
| &#39;Read HasIntent&#39;                       |   231.0 ns |   872.5 ns |  47.82 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   245.7 ns |   493.0 ns |  27.02 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   184.3 ns |   526.7 ns |  28.87 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,804.3 ns | 2,861.4 ns | 156.84 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,646.8 ns | 2,734.8 ns | 149.90 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,438.3 ns | 5,853.5 ns | 320.85 ns |      96 B |
