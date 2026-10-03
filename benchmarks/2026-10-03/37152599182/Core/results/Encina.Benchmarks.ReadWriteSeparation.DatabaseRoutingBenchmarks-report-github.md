```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,501.5 ns |  5,498.2 ns | 301.38 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   232.7 ns |  1,063.8 ns |  58.31 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,566.0 ns |  1,904.7 ns | 104.40 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   253.0 ns |    686.7 ns |  37.64 ns |         - |
| &#39;Read HasIntent&#39;                       |   186.7 ns |    822.7 ns |  45.09 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   174.0 ns |    280.9 ns |  15.39 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   227.7 ns |  1,158.6 ns |  63.51 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,770.7 ns |  1,836.2 ns | 100.65 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,934.7 ns | 10,516.1 ns | 576.42 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,150.5 ns |  2,182.9 ns | 119.65 ns |      96 B |
