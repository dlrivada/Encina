```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,601.5 ns | 48,949.1 ns | 2,683.06 ns | 3,514.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   191.3 ns |  1,644.1 ns |    90.12 ns |   186.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 5,167.5 ns | 15,179.1 ns |   832.02 ns | 5,173.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   261.3 ns |  1,607.7 ns |    88.12 ns |   256.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   794.0 ns | 18,339.9 ns | 1,005.27 ns |   251.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   191.3 ns |  1,880.1 ns |   103.06 ns |   165.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   299.8 ns |  3,364.8 ns |   184.44 ns |   248.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 5,276.3 ns | 29,143.5 ns | 1,597.45 ns | 4,533.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 5,236.0 ns | 28,754.5 ns | 1,576.13 ns | 4,679.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,236.0 ns | 14,755.7 ns |   808.81 ns | 1,822.0 ns |      96 B |
