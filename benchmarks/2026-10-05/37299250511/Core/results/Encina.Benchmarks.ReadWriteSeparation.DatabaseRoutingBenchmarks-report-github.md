```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 10,630.0 ns | 209,022.7 ns | 11,457.24 ns | 4,247.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    207.0 ns |   1,109.3 ns |     60.80 ns |   200.0 ns |         - |
| DatabaseRoutingScope.ForRead()         |  3,675.7 ns |   5,385.5 ns |    295.20 ns | 3,836.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    257.3 ns |   2,285.2 ns |    125.26 ns |   200.0 ns |         - |
| &#39;Read HasIntent&#39;                       |    251.2 ns |   1,037.4 ns |     56.86 ns |   234.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    133.0 ns |     426.7 ns |     23.39 ns |   120.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    200.3 ns |   1,444.6 ns |     79.19 ns |   171.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  3,578.0 ns |   5,949.4 ns |    326.11 ns | 3,555.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |  3,662.3 ns |   4,590.4 ns |    251.62 ns | 3,726.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         |  2,012.3 ns |   3,961.5 ns |    217.14 ns | 1,892.0 ns |      96 B |
