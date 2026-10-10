```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev     | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|-----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,994.5 ns | 36,328.9 ns | 1,991.3 ns | 4,199.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   651.7 ns | 18,969.5 ns | 1,039.8 ns |    75.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,878.5 ns | 23,321.8 ns | 1,278.3 ns | 2,480.50 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   210.3 ns |  2,930.5 ns |   160.6 ns |   133.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   143.2 ns |  2,303.4 ns |   126.3 ns |    80.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    | 1,769.3 ns | 48,079.8 ns | 2,635.4 ns |   375.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   243.2 ns |  2,081.5 ns |   114.1 ns |   186.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,464.2 ns | 31,148.2 ns | 1,707.3 ns | 3,801.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,933.3 ns | 31,571.4 ns | 1,730.5 ns | 4,418.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,758.8 ns | 10,020.3 ns |   549.2 ns | 3,937.50 ns |      96 B |
