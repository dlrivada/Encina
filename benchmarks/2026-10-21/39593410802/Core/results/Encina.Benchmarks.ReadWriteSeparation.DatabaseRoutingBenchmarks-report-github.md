```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,150.8 ns |  2,381.0 ns |   130.51 ns | 4,107.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   188.3 ns |    421.3 ns |    23.09 ns |   175.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,827.2 ns |  3,329.7 ns |   182.51 ns | 3,927.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   218.3 ns |    936.2 ns |    51.32 ns |   205.0 ns |         - |
| &#39;Read HasIntent&#39;                       | 1,059.3 ns | 25,072.7 ns | 1,374.32 ns |   291.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   211.0 ns |    482.7 ns |    26.46 ns |   201.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   230.5 ns |    482.7 ns |    26.46 ns |   240.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,616.5 ns |  2,919.0 ns |   160.00 ns | 3,616.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,794.7 ns |  4,088.8 ns |   224.12 ns | 3,841.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,485.0 ns |  4,382.3 ns |   240.21 ns | 2,405.0 ns |      96 B |
