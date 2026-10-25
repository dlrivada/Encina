```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,455.3 ns | 8,408.6 ns | 460.90 ns | 3,225.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   123.3 ns |   737.3 ns |  40.41 ns |   100.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,728.3 ns | 2,658.4 ns | 145.72 ns | 2,665.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   208.5 ns | 2,166.1 ns | 118.73 ns |   144.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   119.3 ns |   182.7 ns |  10.02 ns |   120.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   181.5 ns | 1,166.3 ns |  63.93 ns |   154.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   124.0 ns |   111.0 ns |   6.08 ns |   121.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,729.3 ns | 2,576.2 ns | 141.21 ns | 2,779.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,590.7 ns | 6,538.1 ns | 358.38 ns | 2,534.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,806.3 ns | 1,593.9 ns |  87.37 ns | 1,783.0 ns |      96 B |
