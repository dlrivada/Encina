```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.976 ns |   0.0623 ns | 0.0034 ns |   0.49 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.184 ns |   2.2716 ns | 0.1245 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   784.454 ns | 109.1848 ns | 5.9848 ns |  95.87 |    1.42 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,190.795 ns | 125.1810 ns | 6.8616 ns | 145.53 |    2.07 | 0.0286 |     504 B |       12.60 |
