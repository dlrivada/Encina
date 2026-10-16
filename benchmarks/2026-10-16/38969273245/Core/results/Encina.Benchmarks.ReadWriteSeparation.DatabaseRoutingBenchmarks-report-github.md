```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,094.67 ns | 2,659.48 ns | 145.77 ns | 3,105.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   160.50 ns |   631.98 ns |  34.64 ns |   180.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,134.50 ns | 1,796.80 ns |  98.49 ns | 3,104.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    70.33 ns |   191.63 ns |  10.50 ns |    70.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   120.00 ns |   364.87 ns |  20.00 ns |   120.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   194.00 ns | 2,150.67 ns | 117.89 ns |   131.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   130.33 ns |   365.03 ns |  20.01 ns |   131.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,839.67 ns | 2,074.76 ns | 113.72 ns | 2,873.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,841.00 ns | 5,956.43 ns | 326.49 ns | 2,744.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,736.67 ns | 4,850.58 ns | 265.88 ns | 1,753.00 ns |      96 B |
