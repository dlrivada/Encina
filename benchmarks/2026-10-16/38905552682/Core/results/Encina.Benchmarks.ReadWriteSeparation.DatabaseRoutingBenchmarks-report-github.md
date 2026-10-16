```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    |  4,224.2 ns |   1,417.1 ns |     77.67 ns | 4,247.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    253.3 ns |     105.3 ns |      5.77 ns |   250.0 ns |         - |
| DatabaseRoutingScope.ForRead()         |  4,152.8 ns |   4,764.4 ns |    261.15 ns | 4,022.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    231.0 ns |     729.7 ns |     40.00 ns |   231.0 ns |         - |
| &#39;Read HasIntent&#39;                       |    226.8 ns |   1,108.3 ns |     60.75 ns |   220.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    276.8 ns |     216.1 ns |     11.85 ns |   270.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    253.8 ns |     586.5 ns |     32.15 ns |   240.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  3,829.2 ns |   3,270.3 ns |    179.26 ns | 3,922.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   |  3,773.8 ns |   3,918.0 ns |    214.76 ns | 3,686.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 10,847.7 ns | 265,501.6 ns | 14,553.04 ns | 2,486.0 ns |      96 B |
