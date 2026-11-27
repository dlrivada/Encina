```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean        | Error        | StdDev    | Median      | Allocated |
|--------------------------------------- |------------:|-------------:|----------:|------------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,443.67 ns |  3,395.16 ns | 186.10 ns | 4,467.00 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      |    59.67 ns |  1,146.65 ns |  62.85 ns |    40.00 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,050.00 ns |  9,400.86 ns | 515.29 ns | 4,317.00 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   173.83 ns |  2,324.43 ns | 127.41 ns |   110.50 ns |         - |
| &#39;Read HasIntent&#39;                       |   224.33 ns |  1,417.07 ns |  77.67 ns |   201.00 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   192.17 ns |    918.25 ns |  50.33 ns |   185.50 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   363.50 ns |  4,850.46 ns | 265.87 ns |   210.50 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,783.83 ns | 11,133.82 ns | 610.28 ns | 3,700.50 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,796.00 ns |  9,918.13 ns | 543.65 ns | 3,565.00 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,056.67 ns |  2,067.29 ns | 113.32 ns | 2,023.00 ns |      96 B |
