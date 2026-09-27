```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev      | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 3,477.00 ns | 13,107.79 ns |   718.48 ns | 3,320.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    94.83 ns |  1,458.40 ns |    79.94 ns |    64.50 ns |         - |
| DatabaseRoutingScope.ForRead()         | 2,786.00 ns | 26,434.37 ns | 1,448.96 ns | 3,130.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   105.67 ns |  1,266.64 ns |    69.43 ns |    76.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   100.33 ns |  1,769.77 ns |    97.01 ns |    60.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   494.00 ns | 13,558.76 ns |   743.20 ns |    80.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |    92.00 ns |  1,313.93 ns |    72.02 ns |    55.00 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 1,773.00 ns | 14,461.89 ns |   792.70 ns | 1,713.00 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 1,773.00 ns | 15,723.41 ns |   861.85 ns | 1,583.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,108.00 ns |  4,898.70 ns |   268.51 ns | 1,958.00 ns |      96 B |
