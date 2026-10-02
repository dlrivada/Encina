```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev    | Allocated |
|--------------------------------------- |------------:|-------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,412.33 ns | 11,256.62 ns | 617.01 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   109.17 ns |    739.34 ns |  40.53 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,329.17 ns | 11,435.97 ns | 626.84 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    87.17 ns |    625.19 ns |  34.27 ns |         - |
| &#39;Read HasIntent&#39;                       |   384.33 ns |  2,942.52 ns | 161.29 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   317.33 ns |  1,827.41 ns | 100.17 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   291.17 ns |  1,306.22 ns |  71.60 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,943.67 ns |  9,325.14 ns | 511.14 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,863.83 ns | 14,146.38 ns | 775.41 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,161.33 ns | 11,718.72 ns | 642.34 ns |      96 B |
