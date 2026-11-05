```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 2,170.00 ns | 20,070.91 ns | 1,100.15 ns | 1,713.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   118.33 ns |  2,009.57 ns |   110.15 ns |    65.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,626.00 ns | 16,820.95 ns |   922.01 ns | 1,312.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |    60.00 ns |  1,518.18 ns |    83.22 ns |    25.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   883.00 ns | 13,065.21 ns |   716.15 ns | 1,187.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |    87.67 ns |  1,466.36 ns |    80.38 ns |    74.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    70.33 ns |  1,769.77 ns |    97.01 ns |    30.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,804.33 ns | 17,621.61 ns |   965.90 ns | 1,698.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,605.17 ns | 18,101.25 ns |   992.19 ns | 1,261.50 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 1,001.67 ns | 13,134.99 ns |   719.97 ns |   591.00 ns |      96 B |
