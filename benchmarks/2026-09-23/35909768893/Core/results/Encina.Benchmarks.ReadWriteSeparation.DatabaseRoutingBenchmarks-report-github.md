```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev     | Median      | Allocated |
|--------------------------------------- |------------:|------------:|-----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,159.67 ns | 2,029.92 ns | 111.267 ns | 3,179.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    67.00 ns |   686.75 ns |  37.643 ns |    51.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,714.00 ns | 4,604.51 ns | 252.389 ns | 2,644.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   350.67 ns | 3,428.19 ns | 187.910 ns |   411.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   115.67 ns |   321.39 ns |  17.616 ns |   106.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   130.33 ns |   173.40 ns |   9.504 ns |   130.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   130.00 ns |   364.87 ns |  20.000 ns |   130.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,497.33 ns | 2,828.27 ns | 155.027 ns | 2,494.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,804.00 ns | 5,664.37 ns | 310.483 ns | 2,824.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,815.50 ns | 4,903.69 ns | 268.788 ns | 1,701.50 ns |      96 B |
