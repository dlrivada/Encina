```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,839.0 ns | 13,575.7 ns | 744.13 ns | 3,535.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   122.0 ns |  1,169.2 ns |  64.09 ns |    85.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,682.3 ns | 16,927.7 ns | 927.86 ns | 1,482.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   142.3 ns |  1,781.9 ns |  97.67 ns |   166.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   137.0 ns |  1,654.8 ns |  90.70 ns |   130.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   167.0 ns |  1,219.3 ns |  66.84 ns |   201.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   160.7 ns |  1,115.8 ns |  61.16 ns |   131.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,009.2 ns | 12,779.5 ns | 700.49 ns | 2,758.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,977.7 ns | 13,945.4 ns | 764.40 ns | 2,614.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,857.2 ns |  6,995.5 ns | 383.45 ns | 1,767.50 ns |      96 B |
