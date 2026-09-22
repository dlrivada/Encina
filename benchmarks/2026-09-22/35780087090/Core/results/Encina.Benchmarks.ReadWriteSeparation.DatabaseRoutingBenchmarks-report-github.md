```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,169.8 ns | 4,638.1 ns | 254.23 ns | 4,306.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   285.5 ns | 2,534.5 ns | 138.92 ns |   215.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,824.3 ns | 5,507.5 ns | 301.88 ns | 3,851.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   175.2 ns | 1,894.2 ns | 103.83 ns |   125.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   226.0 ns | 2,749.1 ns | 150.69 ns |   139.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   180.7 ns | 1,746.1 ns |  95.71 ns |   131.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   217.3 ns | 3,546.1 ns | 194.37 ns |   170.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,878.3 ns | 6,069.6 ns | 332.69 ns | 3,955.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,509.0 ns | 6,961.3 ns | 381.57 ns | 3,476.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,167.0 ns | 2,125.0 ns | 116.48 ns | 2,224.0 ns |      96 B |
