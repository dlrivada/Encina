```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error      | StdDev    | Allocated |
|--------------------------------------- |-----------:|-----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,183.7 ns | 2,050.6 ns | 112.40 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   193.0 ns | 1,075.8 ns |  58.97 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,656.3 ns | 4,261.4 ns | 233.58 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   184.3 ns |   557.4 ns |  30.55 ns |         - |
| &#39;Read HasIntent&#39;                       |   254.0 ns |   889.5 ns |  48.75 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   217.3 ns |   996.0 ns |  54.59 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   310.7 ns | 2,213.5 ns | 121.33 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,765.2 ns | 4,382.7 ns | 240.23 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,842.2 ns | 3,982.4 ns | 218.29 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,316.7 ns | 3,224.1 ns | 176.72 ns |      96 B |
