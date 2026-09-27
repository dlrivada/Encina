```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean       | Error     | StdDev    | Allocated |
|--------------------------------------- |-----------:|----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,948.1 ns | 121.03 ns | 161.58 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   243.1 ns |  65.75 ns |  94.30 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,801.9 ns | 201.08 ns | 300.97 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   196.0 ns |  16.77 ns |  24.58 ns |         - |
| &#39;Read HasIntent&#39;                       |   238.7 ns |  66.37 ns |  97.29 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   187.4 ns |  27.76 ns |  39.81 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   212.1 ns |  15.84 ns |  22.72 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,984.8 ns | 244.88 ns | 351.20 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,634.6 ns |  68.26 ns |  97.90 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,536.8 ns | 207.64 ns | 310.79 ns |      96 B |
