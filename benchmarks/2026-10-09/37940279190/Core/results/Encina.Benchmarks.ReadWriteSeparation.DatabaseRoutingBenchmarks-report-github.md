```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median      | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,123.7 ns | 36,836.4 ns | 2,019.13 ns | 4,346.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      | 1,007.7 ns | 29,503.0 ns | 1,617.16 ns |    77.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,089.3 ns | 47,383.7 ns | 2,597.26 ns | 3,508.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   110.0 ns |  1,141.2 ns |    62.55 ns |    79.00 ns |         - |
| &#39;Read HasIntent&#39;                       |   250.8 ns |  2,358.5 ns |   129.28 ns |   234.50 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   156.0 ns |  2,380.9 ns |   130.50 ns |   101.00 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   189.2 ns |  3,528.6 ns |   193.41 ns |    78.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 4,449.5 ns | 41,971.3 ns | 2,300.59 ns | 3,836.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,900.3 ns | 24,904.2 ns | 1,365.08 ns | 4,883.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,592.8 ns | 40,813.1 ns | 2,237.11 ns | 1,527.50 ns |      96 B |
