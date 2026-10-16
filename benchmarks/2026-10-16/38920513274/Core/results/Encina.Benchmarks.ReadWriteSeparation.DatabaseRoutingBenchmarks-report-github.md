```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,372.8 ns | 2,483.65 ns | 136.14 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   100.3 ns |   720.65 ns |  39.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,757.5 ns | 3,771.07 ns | 206.71 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   140.7 ns |    10.53 ns |   0.58 ns |         - |
| &#39;Read HasIntent&#39;                       |   115.3 ns |   374.03 ns |  20.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   150.0 ns |   182.44 ns |  10.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   132.0 ns |   449.48 ns |  24.64 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,771.3 ns | 2,770.42 ns | 151.86 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,844.7 ns | 4,323.04 ns | 236.96 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,749.3 ns | 3,561.98 ns | 195.24 ns |      96 B |
