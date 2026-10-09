```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev       | Median     | Allocated |
|--------------------------------------- |------------:|-------------:|-------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    |  4,404.7 ns |   3,957.9 ns |    216.94 ns | 4,428.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    234.3 ns |   1,004.8 ns |     55.08 ns |   261.0 ns |         - |
| DatabaseRoutingScope.ForRead()         |  5,220.3 ns |  24,219.7 ns |  1,327.56 ns | 4,479.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    289.7 ns |   1,763.2 ns |     96.65 ns |   249.0 ns |         - |
| &#39;Read HasIntent&#39;                       |    226.8 ns |     694.8 ns |     38.08 ns |   209.5 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    190.7 ns |     365.0 ns |     20.01 ns |   190.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    240.0 ns |     547.3 ns |     30.00 ns |   240.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        |  3,756.3 ns |   2,402.4 ns |    131.68 ns | 3,736.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 11,821.7 ns | 253,750.5 ns | 13,908.92 ns | 3,877.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         |  2,753.5 ns |   7,597.1 ns |    416.42 ns | 2,920.5 ns |      96 B |
