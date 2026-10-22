```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,174.7 ns |  7,851.9 ns | 430.39 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   168.5 ns |    666.1 ns |  36.51 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,906.2 ns | 12,548.2 ns | 687.81 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   216.3 ns |    338.5 ns |  18.56 ns |         - |
| &#39;Read HasIntent&#39;                       |   195.0 ns |  1,421.0 ns |  77.89 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   232.5 ns |    770.4 ns |  42.23 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   325.3 ns |    830.1 ns |  45.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 5,500.0 ns | 10,114.4 ns | 554.40 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,930.0 ns |  1,218.2 ns |  66.78 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 3,442.5 ns | 13,141.2 ns | 720.31 ns |      96 B |
