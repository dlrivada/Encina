```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,126.7 ns | 219.93 ns | 322.37 ns | 4,052.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   128.8 ns |  32.02 ns |  47.93 ns |   120.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,603.5 ns | 138.37 ns | 193.98 ns | 3,541.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   412.2 ns | 117.59 ns | 176.01 ns |   298.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   222.6 ns |  23.19 ns |  33.99 ns |   221.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   253.8 ns |  15.71 ns |  23.52 ns |   260.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   209.2 ns |  22.70 ns |  32.55 ns |   210.8 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,686.9 ns | 191.99 ns | 275.35 ns | 3,561.2 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,543.0 ns |  76.68 ns | 109.98 ns | 3,522.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,147.5 ns |  98.45 ns | 147.35 ns | 2,154.0 ns |      96 B |
