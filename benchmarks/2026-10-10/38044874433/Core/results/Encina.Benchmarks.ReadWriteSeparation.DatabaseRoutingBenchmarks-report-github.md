```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,319.83 ns | 19,907.08 ns | 1,091.17 ns | 2,002.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    77.00 ns |  1,705.28 ns |    93.47 ns |    50.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,724.33 ns | 19,064.07 ns | 1,044.97 ns | 1,267.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; | 1,165.00 ns | 15,900.21 ns |   871.54 ns | 1,622.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   138.67 ns |  1,427.25 ns |    78.23 ns |   115.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   137.00 ns |  1,665.78 ns |    91.31 ns |   100.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   174.00 ns |  1,476.16 ns |    80.91 ns |   160.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,063.33 ns | 24,912.38 ns | 1,365.53 ns | 2,950.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,729.00 ns | 16,355.57 ns |   896.50 ns | 1,342.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,173.67 ns | 12,445.83 ns |   682.20 ns | 1,127.00 ns |      96 B |
