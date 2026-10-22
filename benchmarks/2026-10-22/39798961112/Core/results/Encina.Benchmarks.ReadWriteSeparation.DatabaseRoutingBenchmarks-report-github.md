```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean        | Error     | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|----------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,607.98 ns | 470.66 ns | 659.80 ns | 3,684.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   123.28 ns |  39.05 ns |  58.44 ns |   124.25 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,325.04 ns | 374.80 ns | 537.53 ns | 3,572.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   126.45 ns |  30.13 ns |  44.16 ns |   123.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   129.05 ns |  35.72 ns |  52.35 ns |   115.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    82.14 ns |  14.62 ns |  19.52 ns |    82.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   453.33 ns | 435.74 ns | 652.20 ns |   149.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,085.57 ns | 256.83 ns | 360.04 ns | 3,018.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,203.24 ns | 445.38 ns | 624.36 ns | 3,147.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,575.46 ns | 100.98 ns | 144.82 ns | 1,544.50 ns |      96 B |
