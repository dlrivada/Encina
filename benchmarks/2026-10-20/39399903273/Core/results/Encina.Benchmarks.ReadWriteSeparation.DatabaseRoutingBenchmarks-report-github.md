```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,725.7 ns | 17,405.3 ns | 954.04 ns | 3,485.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   610.2 ns | 15,675.6 ns | 859.23 ns |   149.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,549.0 ns | 17,474.5 ns | 957.84 ns | 1,422.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   120.0 ns |  1,851.5 ns | 101.49 ns |   100.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   140.3 ns |    789.0 ns |  43.25 ns |   120.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   583.0 ns | 15,562.9 ns | 853.05 ns |    96.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   124.5 ns |    795.2 ns |  43.59 ns |   104.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,695.7 ns | 17,585.8 ns | 963.94 ns | 1,432.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,580.7 ns | 18,097.7 ns | 991.99 ns | 1,277.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,004.5 ns |  9,092.0 ns | 498.37 ns |   800.50 ns |      96 B |
