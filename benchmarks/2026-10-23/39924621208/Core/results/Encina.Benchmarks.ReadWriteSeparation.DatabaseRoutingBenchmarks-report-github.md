```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,183.7 ns | 2,050.56 ns | 112.40 ns | 4,157.0 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   327.5 ns | 3,701.59 ns | 202.90 ns |   220.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,334.8 ns | 8,632.04 ns | 473.15 ns | 4,478.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   306.3 ns | 2,114.25 ns | 115.89 ns |   266.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   229.0 ns |   631.98 ns |  34.64 ns |   249.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   233.0 ns |    94.80 ns |   5.20 ns |   230.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   227.3 ns |   559.44 ns |  30.66 ns |   220.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,691.0 ns | 1,098.72 ns |  60.22 ns | 3,697.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,637.7 ns | 1,628.74 ns |  89.28 ns | 3,608.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,348.0 ns | 1,485.16 ns |  81.41 ns | 2,395.0 ns |      96 B |
