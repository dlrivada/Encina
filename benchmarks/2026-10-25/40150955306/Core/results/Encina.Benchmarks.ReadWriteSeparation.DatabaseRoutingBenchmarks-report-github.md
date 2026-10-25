```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                                 | Mean        | Error     | StdDev    | Allocated |
|--------------------------------------- |------------:|----------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,971.26 ns | 102.83 ns | 144.15 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    96.50 ns |  28.42 ns |  42.54 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,638.17 ns | 123.75 ns | 181.40 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    92.98 ns |  18.26 ns |  27.33 ns |         - |
| &#39;Read HasIntent&#39;                       |   117.76 ns |  18.53 ns |  27.16 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   107.29 ns |  16.70 ns |  23.95 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   108.71 ns |  24.53 ns |  35.18 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,640.75 ns | 109.61 ns | 157.20 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,627.67 ns | 115.95 ns | 162.54 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,458.80 ns |  81.79 ns | 117.30 ns |      96 B |
