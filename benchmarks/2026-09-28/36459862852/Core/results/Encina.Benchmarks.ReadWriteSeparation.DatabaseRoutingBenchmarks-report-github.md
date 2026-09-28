```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.49GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev    | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|----------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 5,542.8 ns | 12,863.7 ns | 705.10 ns | 5,405.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |   168.5 ns |    698.8 ns |  38.30 ns |   177.5 ns |         - |
| DatabaseRoutingScope.ForRead()         | 3,443.0 ns | 11,095.6 ns | 608.19 ns | 3,356.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   323.5 ns |  3,699.4 ns | 202.78 ns |   246.5 ns |         - |
| &#39;Read HasIntent&#39;                       |   203.0 ns |  1,239.0 ns |  67.91 ns |   171.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   219.7 ns |  1,017.5 ns |  55.77 ns |   206.0 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   297.2 ns |  1,021.4 ns |  55.99 ns |   270.5 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 2,980.5 ns | 13,693.7 ns | 750.60 ns | 2,825.5 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 4,053.5 ns |  4,089.6 ns | 224.17 ns | 4,043.5 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,122.0 ns |  7,738.3 ns | 424.16 ns | 1,971.0 ns |      96 B |
