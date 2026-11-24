```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.54GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,202.2 ns | 11,601.4 ns | 635.91 ns | 4,334.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   217.8 ns |    438.0 ns |  24.01 ns |   223.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,980.2 ns |  8,210.7 ns | 450.06 ns | 3,840.5 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   315.2 ns |  2,879.8 ns | 157.85 ns |   240.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   164.3 ns |    736.6 ns |  40.38 ns |   183.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   239.5 ns |    667.6 ns |  36.59 ns |   236.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   162.3 ns |    339.0 ns |  18.58 ns |   171.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,795.0 ns | 10,855.1 ns | 595.01 ns | 3,858.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,604.3 ns | 13,916.1 ns | 762.79 ns | 3,686.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,555.8 ns |  5,949.8 ns | 326.13 ns | 2,628.5 ns |      96 B |
