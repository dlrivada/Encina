```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error       | StdDev     | Allocated |
|--------------------------------------- |------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,250.67 ns | 3,087.09 ns | 169.214 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   116.50 ns |   390.01 ns |  21.378 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,758.00 ns | 3,254.70 ns | 178.401 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   102.83 ns |   278.68 ns |  15.275 ns |         - |
| &#39;Read HasIntent&#39;                       |    70.67 ns |   173.40 ns |   9.504 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   118.67 ns |   382.83 ns |  20.984 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   133.33 ns |   379.77 ns |  20.817 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,627.33 ns | 4,963.95 ns | 272.091 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,973.33 ns | 6,122.76 ns | 335.609 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,662.67 ns | 4,612.10 ns | 252.805 ns |      96 B |
