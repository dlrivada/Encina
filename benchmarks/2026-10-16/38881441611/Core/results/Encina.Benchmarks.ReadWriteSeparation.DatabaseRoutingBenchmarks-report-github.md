```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,687.0 ns |  7,666.9 ns | 420.25 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   214.3 ns |  1,381.4 ns |  75.72 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,068.8 ns | 16,525.6 ns | 905.83 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   293.3 ns |  1,319.8 ns |  72.34 ns |         - |
| &#39;Read HasIntent&#39;                       |   223.3 ns |    737.3 ns |  40.41 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   197.2 ns |  1,485.9 ns |  81.45 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   316.0 ns |  2,104.0 ns | 115.33 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,963.8 ns |  3,760.6 ns | 206.13 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,880.5 ns |  2,200.4 ns | 120.61 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,354.5 ns |    795.2 ns |  43.59 ns |      96 B |
