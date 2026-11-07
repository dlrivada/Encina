```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,371.50 ns | 14,960.29 ns | 820.02 ns | 3,033.50 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    57.00 ns |  1,630.44 ns |  89.37 ns |    11.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 1,615.67 ns | 16,123.90 ns | 883.81 ns | 1,542.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   272.33 ns |  6,079.58 ns | 333.24 ns |   106.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   121.50 ns |  1,629.42 ns |  89.31 ns |    74.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   107.00 ns |  1,378.22 ns |  75.54 ns |   110.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   136.67 ns |  1,172.91 ns |  64.29 ns |   110.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,554.33 ns | 15,225.82 ns | 834.58 ns | 1,247.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 2,789.33 ns | 10,485.62 ns | 574.75 ns | 2,619.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,185.67 ns |  4,793.39 ns | 262.74 ns | 2,039.00 ns |      96 B |
