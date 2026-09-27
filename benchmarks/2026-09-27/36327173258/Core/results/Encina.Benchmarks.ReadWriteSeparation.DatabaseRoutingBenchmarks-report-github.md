```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,188.3 ns | 1,981.0 ns | 108.58 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   215.5 ns |   729.7 ns |  40.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,974.7 ns | 7,103.0 ns | 389.34 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   367.7 ns | 3,010.7 ns | 165.03 ns |         - |
| &#39;Read HasIntent&#39;                       |   213.7 ns |   559.4 ns |  30.66 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   227.7 ns |   379.8 ns |  20.82 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   280.5 ns | 1,196.3 ns |  65.57 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,683.2 ns | 3,854.3 ns | 211.27 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,761.5 ns | 5,416.3 ns | 296.89 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,548.3 ns | 1,369.3 ns |  75.06 ns |      96 B |
