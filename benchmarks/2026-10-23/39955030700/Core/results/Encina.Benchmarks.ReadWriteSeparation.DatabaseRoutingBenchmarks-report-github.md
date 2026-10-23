```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,465.7 ns | 5,882.3 ns | 322.43 ns | 3,566.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   213.0 ns | 3,006.5 ns | 164.79 ns |   150.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,055.8 ns | 2,903.6 ns | 159.16 ns | 2,969.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   153.2 ns | 2,172.5 ns | 119.08 ns |   100.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   153.7 ns | 1,781.9 ns |  97.67 ns |   130.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   381.0 ns | 3,769.9 ns | 206.64 ns |   441.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   431.3 ns | 2,894.4 ns | 158.65 ns |   395.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,117.8 ns | 7,406.9 ns | 406.00 ns | 3,044.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,101.7 ns | 3,103.2 ns | 170.10 ns | 3,095.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,056.7 ns | 5,854.8 ns | 320.92 ns | 1,963.0 ns |      96 B |
