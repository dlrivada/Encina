```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,098.3 ns |  3,359.5 ns |   184.15 ns | 4,018.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   283.8 ns |    759.5 ns |    41.63 ns |   270.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,480.7 ns |  2,044.6 ns |   112.07 ns | 3,437.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   197.3 ns |  1,009.2 ns |    55.32 ns |   170.0 ns |         - |
| &#39;Read HasIntent&#39;                       | 1,090.5 ns | 28,439.3 ns | 1,558.85 ns |   195.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   214.3 ns |  1,053.3 ns |    57.74 ns |   181.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   224.7 ns |  1,179.6 ns |    64.66 ns |   251.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,537.0 ns |  1,109.7 ns |    60.83 ns | 3,507.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,652.2 ns |  3,023.7 ns |   165.74 ns | 3,662.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,314.3 ns |  1,750.9 ns |    95.97 ns | 2,264.0 ns |      96 B |
